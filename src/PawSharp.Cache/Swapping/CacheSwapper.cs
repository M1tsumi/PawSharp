#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.Cache.Exceptions;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Telemetry;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Swapping
{
    /// <summary>
    /// Manages cache provider swapping with fallback support and circuit breaker pattern.
    /// </summary>
    public class CacheSwapper : IEntityCache, IDisposable
    {
        private readonly Dictionary<string, CacheProviderInfo> _providers;
        private readonly CacheSwapperOptions _options;
        private readonly ICacheTelemetry? _telemetry;
        private readonly object _lock = new();
        private CacheProviderInfo? _activeProvider;
        private Timer? _healthCheckTimer;
        private bool _disposed;

        public ICacheTelemetry? Telemetry
        {
            get => _telemetry;
            set => throw new InvalidOperationException("Telemetry is set at construction time.");
        }

        public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        public event EventHandler? CacheCleared;

        /// <summary>
        /// Creates a new CacheSwapper instance.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        /// <param name="telemetry">Telemetry instance.</param>
        public CacheSwapper(CacheSwapperOptions? options = null, ICacheTelemetry? telemetry = null)
        {
            _providers = new Dictionary<string, CacheProviderInfo>();
            _options = options ?? new CacheSwapperOptions();
            _telemetry = telemetry ?? new CacheTelemetry();
        }

        /// <summary>
        /// Registers a cache provider.
        /// </summary>
        /// <param name="name">Unique name for the provider.</param>
        /// <param name="provider">The cache provider instance.</param>
        /// <param name="priority">Priority for fallback (lower = higher priority).</param>
        /// <exception cref="ArgumentException">Thrown if provider name is empty or already registered.</exception>
        /// <exception cref="ArgumentNullException">Thrown if provider is null.</exception>
        public void RegisterProvider(string name, IEntityCache provider, int priority = 0)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Provider name cannot be empty.", nameof(name));

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (_lock)
            {
                if (_providers.ContainsKey(name))
                    throw new ArgumentException($"Provider '{name}' is already registered.", nameof(name));

                // Check if provider is healthy by calling IsHealthy if available
                bool isHealthy = true;
                try
                {
                    if (provider is ICacheProviderHealthCheckable healthCheckable)
                    {
                        isHealthy = healthCheckable.IsHealthy();
                    }
                }
                catch
                {
                    // If health check fails, assume healthy for now
                    isHealthy = true;
                }

                var info = new CacheProviderInfo
                {
                    Name = name,
                    Provider = provider,
                    Priority = priority,
                    IsActive = false,
                    IsHealthy = isHealthy,
                    LastHealthCheck = DateTime.UtcNow
                };

                _providers[name] = info;

                // Wire up events
                provider.EntityEvicted += (sender, args) => EntityEvicted?.Invoke(sender, args);
                provider.CacheCleared += (sender, args) => CacheCleared?.Invoke(sender, args);

                // If this is the first provider, make it active
                if (_activeProvider == null)
                {
                    SetActiveProvider(name);
                }

                if (_options.EnableLogging)
                {
                    Console.WriteLine($"[CacheSwapper] Registered provider '{name}' with priority {priority}");
                }
            }
        }

        /// <summary>
        /// Unregisters a cache provider.
        /// </summary>
        /// <param name="name">Name of the provider to unregister.</param>
        /// <exception cref="CacheProviderNotRegisteredException">Thrown if provider is not registered.</exception>
        public void UnregisterProvider(string name)
        {
            lock (_lock)
            {
                if (!_providers.TryGetValue(name, out var info))
                    throw new CacheProviderNotRegisteredException(name);

                if (info.IsActive)
                {
                    // Try to switch to another provider if available
                    var nextProvider = _providers.Values
                        .Where(p => p.Name != name)
                        .OrderBy(p => p.Priority)
                        .FirstOrDefault();

                    if (nextProvider != null)
                    {
                        SetActiveProvider(nextProvider.Name);
                    }
                    else if (_providers.Count == 1)
                    {
                        _activeProvider = null;
                    }
                }

                _providers.Remove(name);

                if (_options.EnableLogging)
                {
                    Console.WriteLine($"[CacheSwapper] Unregistered provider '{name}'");
                }
            }
        }

        /// <summary>
        /// Sets the active cache provider.
        /// </summary>
        /// <param name="name">Name of the provider to activate.</param>
        /// <exception cref="CacheProviderNotRegisteredException">Thrown if provider is not registered.</exception>
        /// <exception cref="CacheProviderUnavailableException">Thrown if provider is unhealthy.</exception>
        public void SetActiveProvider(string name)
        {
            lock (_lock)
            {
                if (!_providers.TryGetValue(name, out var info))
                    throw new CacheProviderNotRegisteredException(name);

                if (!info.IsHealthy)
                    throw new CacheProviderUnavailableException(name);

                if (_activeProvider != null)
                {
                    _activeProvider.IsActive = false;
                }

                info.IsActive = true;
                _activeProvider = info;

                if (_options.EnableLogging)
                {
                    Console.WriteLine($"[CacheSwapper] Switched to provider '{name}'");
                }
            }
        }

        /// <summary>
        /// Gets the active cache provider.
        /// </summary>
        /// <returns>The active provider, or null if none is set.</returns>
        public IEntityCache? GetActiveProvider()
        {
            lock (_lock)
            {
                return _activeProvider?.Provider;
            }
        }

        /// <summary>
        /// Gets all registered providers.
        /// </summary>
        public IEnumerable<CacheProviderInfo> GetProviders()
        {
            lock (_lock)
            {
                return _providers.Values.ToList();
            }
        }

        /// <summary>
        /// Performs a health check on all providers and attempts to swap to a healthy one if needed.
        /// </summary>
        public async Task PerformHealthChecksAsync()
        {
            var providersToCheck = new List<CacheProviderInfo>();

            lock (_lock)
            {
                providersToCheck = _providers.Values.ToList();
            }

            foreach (var provider in providersToCheck)
            {
                try
                {
                    var isHealthy = await Task.Run(() => provider.Provider.IsHealthy());
                    
                    lock (_lock)
                    {
                        provider.IsHealthy = isHealthy;
                        provider.LastHealthCheck = DateTime.UtcNow;

                        // Reset circuit breaker if healthy and circuit was open
                        if (isHealthy && provider.IsCircuitOpen && DateTime.UtcNow >= provider.CircuitResetTime)
                        {
                            provider.IsCircuitOpen = false;
                            provider.FailureCount = 0;

                            if (_options.AutoSwapBackToPrimary && provider.Priority == 0 && _activeProvider?.Name != provider.Name)
                            {
                                SetActiveProvider(provider.Name);
                            }
                        }
                    }

                    if (_options.EnableLogging)
                    {
                        Console.WriteLine($"[CacheSwapper] Health check for '{provider.Name}': {(isHealthy ? "Healthy" : "Unhealthy")}");
                    }
                }
                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        provider.IsHealthy = false;
                        provider.LastHealthCheck = DateTime.UtcNow;
                        provider.FailureCount++;

                        // Open circuit breaker if too many failures
                        if (provider.FailureCount >= _options.MaxFailuresBeforeCircuitOpen)
                        {
                            provider.IsCircuitOpen = true;
                            provider.CircuitResetTime = DateTime.UtcNow.Add(_options.CircuitOpenDuration);
                        }
                    }

                    if (_options.EnableLogging)
                    {
                        Console.WriteLine($"[CacheSwapper] Health check failed for '{provider.Name}': {ex.Message}");
                    }

                    // If active provider failed, try to fallback
                    if (_activeProvider?.Name == provider.Name && _options.AutoFallback)
                    {
                        await TryFallbackAsync(provider.Name);
                    }
                }
            }
        }

        private async Task TryFallbackAsync(string failedProviderName)
        {
            lock (_lock)
            {
                var fallbackProviders = _providers.Values
                    .Where(p => p.Name != failedProviderName && !p.IsCircuitOpen)
                    .OrderBy(p => p.Priority)
                    .ToList();

                foreach (var provider in fallbackProviders)
                {
                    try
                    {
                        provider.Provider.IsHealthy();
                        SetActiveProvider(provider.Name);
                        return;
                    }
                    catch
                    {
                        // Try next provider
                        continue;
                    }
                }
            }
        }

        private IEntityCache GetProviderOrThrow()
        {
            lock (_lock)
            {
                if (_activeProvider == null || !_activeProvider.IsHealthy)
                {
                    // Try to find a healthy provider
                    var healthyProvider = _providers.Values
                        .Where(p => p.IsHealthy && !p.IsCircuitOpen)
                        .OrderBy(p => p.Priority)
                        .FirstOrDefault();

                    if (healthyProvider != null)
                    {
                        SetActiveProvider(healthyProvider.Name);
                    }
                    else
                    {
                        throw new CacheProviderUnavailableException(_activeProvider?.Name ?? "No provider");
                    }
                }

                return _activeProvider.Provider;
            }
        }

        // IEntityCache implementation

        public void Add(string key, object entity)
        {
            try
            {
                var provider = GetProviderOrThrow();
                provider.Add(key, entity);

                if (_options.PropagateToAllProviders)
                {
                    lock (_lock)
                    {
                        foreach (var otherProvider in _providers.Values.Where(p => p.Name != _activeProvider?.Name))
                        {
                            try { otherProvider.Provider.Add(key, entity); } catch { /* Ignore */ }
                        }
                    }
                }
            }
            catch (CacheException)
            {
                if (_options.AutoFallback)
                {
                    var provider = GetProviderOrThrow();
                    provider.Add(key, entity);
                }
                throw;
            }
        }

        public object? Get(string key)
        {
            try
            {
                return GetProviderOrThrow().Get(key);
            }
            catch (CacheException)
            {
                if (_options.AutoFallback)
                {
                    return GetProviderOrThrow().Get(key);
                }
                throw;
            }
        }

        public void Remove(string key)
        {
            try
            {
                var provider = GetProviderOrThrow();
                provider.Remove(key);

                if (_options.PropagateToAllProviders)
                {
                    lock (_lock)
                    {
                        foreach (var otherProvider in _providers.Values.Where(p => p.Name != _activeProvider?.Name))
                        {
                            try { otherProvider.Provider.Remove(key); } catch { /* Ignore */ }
                        }
                    }
                }
            }
            catch (CacheException)
            {
                if (_options.AutoFallback)
                {
                    var provider = GetProviderOrThrow();
                    provider.Remove(key);
                }
                throw;
            }
        }

        public void Clear()
        {
            try
            {
                var provider = GetProviderOrThrow();
                provider.Clear();

                if (_options.PropagateToAllProviders)
                {
                    lock (_lock)
                    {
                        foreach (var otherProvider in _providers.Values.Where(p => p.Name != _activeProvider?.Name))
                        {
                            try { otherProvider.Provider.Clear(); } catch { /* Ignore */ }
                        }
                    }
                }
            }
            catch (CacheException)
            {
                if (_options.AutoFallback)
                {
                    var provider = GetProviderOrThrow();
                    provider.Clear();
                }
                throw;
            }
        }

        public bool Exists(string key)
        {
            try
            {
                return GetProviderOrThrow().Exists(key);
            }
            catch (CacheException)
            {
                if (_options.AutoFallback)
                {
                    return GetProviderOrThrow().Exists(key);
                }
                throw;
            }
        }

        // Typed entity operations - delegate to active provider with fallback

        public void CacheUser(User user) => GetProviderOrThrow().CacheUser(user);
        public User? GetUser(ulong userId) => GetProviderOrThrow().GetUser(userId);
        public void CacheGuild(Guild guild) => GetProviderOrThrow().CacheGuild(guild);
        public Guild? GetGuild(ulong guildId) => GetProviderOrThrow().GetGuild(guildId);
        public IEnumerable<Guild> GetAllGuilds() => GetProviderOrThrow().GetAllGuilds();
        public void CacheChannel(Channel channel) => GetProviderOrThrow().CacheChannel(channel);
        public Channel? GetChannel(ulong channelId) => GetProviderOrThrow().GetChannel(channelId);
        public IEnumerable<Channel> GetGuildChannels(ulong guildId) => GetProviderOrThrow().GetGuildChannels(guildId);
        public void CacheMessage(Message message) => GetProviderOrThrow().CacheMessage(message);
        public Message? GetMessage(ulong messageId) => GetProviderOrThrow().GetMessage(messageId);
        public IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50) => GetProviderOrThrow().GetChannelMessages(channelId, limit);
        public void CacheGuildMember(ulong guildId, GuildMember member) => GetProviderOrThrow().CacheGuildMember(guildId, member);
        public GuildMember? GetGuildMember(ulong guildId, ulong userId) => GetProviderOrThrow().GetGuildMember(guildId, userId);
        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId) => GetProviderOrThrow().GetGuildMembers(guildId);
        public void CacheRole(ulong guildId, Role role) => GetProviderOrThrow().CacheRole(guildId, role);
        public Role? GetRole(ulong guildId, ulong roleId) => GetProviderOrThrow().GetRole(guildId, roleId);
        public IEnumerable<Role> GetGuildRoles(ulong guildId) => GetProviderOrThrow().GetGuildRoles(guildId);
        public void CacheEmoji(ulong guildId, Emoji emoji) => GetProviderOrThrow().CacheEmoji(guildId, emoji);
        public Emoji? GetEmoji(ulong guildId, ulong emojiId) => GetProviderOrThrow().GetEmoji(guildId, emojiId);
        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId) => GetProviderOrThrow().GetGuildEmojis(guildId);
        public void CacheGuildData(Guild guild) => GetProviderOrThrow().CacheGuildData(guild);
        public void RemoveGuild(ulong guildId) => GetProviderOrThrow().RemoveGuild(guildId);
        public void RemoveChannel(ulong channelId) => GetProviderOrThrow().RemoveChannel(channelId);
        public void RemoveMessage(ulong messageId) => GetProviderOrThrow().RemoveMessage(messageId);
        public void RemoveGuildMember(ulong guildId, ulong userId) => GetProviderOrThrow().RemoveGuildMember(guildId, userId);
        public void RemoveRole(ulong guildId, ulong roleId) => GetProviderOrThrow().RemoveRole(guildId, roleId);
        public int GetEntityCount() => GetProviderOrThrow().GetEntityCount();
        public long GetMemoryUsage() => GetProviderOrThrow().GetMemoryUsage();
        public CacheStats GetCacheStats() => GetProviderOrThrow().GetCacheStats();
        public bool IsHealthy() => _activeProvider?.Provider.IsHealthy() ?? false;

        // Async operations - delegate to active provider with fallback

        public Task<User?> GetUserAsync(ulong userId) => GetProviderOrThrow().GetUserAsync(userId);
        public Task<Guild?> GetGuildAsync(ulong guildId) => GetProviderOrThrow().GetGuildAsync(guildId);
        public Task<Channel?> GetChannelAsync(ulong channelId) => GetProviderOrThrow().GetChannelAsync(channelId);
        public Task<Message?> GetMessageAsync(ulong messageId) => GetProviderOrThrow().GetMessageAsync(messageId);
        public Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId) => GetProviderOrThrow().GetGuildMemberAsync(guildId, userId);
        public Task<Role?> GetRoleAsync(ulong guildId, ulong roleId) => GetProviderOrThrow().GetRoleAsync(guildId, roleId);
        public Task<Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId) => GetProviderOrThrow().GetEmojiAsync(guildId, emojiId);
        public Task CacheUserAsync(User user) => GetProviderOrThrow().CacheUserAsync(user);
        public Task CacheGuildAsync(Guild guild) => GetProviderOrThrow().CacheGuildAsync(guild);
        public Task CacheChannelAsync(Channel channel) => GetProviderOrThrow().CacheChannelAsync(channel);
        public Task CacheMessageAsync(Message message) => GetProviderOrThrow().CacheMessageAsync(message);
        public Task CacheGuildMemberAsync(ulong guildId, GuildMember member) => GetProviderOrThrow().CacheGuildMemberAsync(guildId, member);
        public Task CacheRoleAsync(ulong guildId, Role role) => GetProviderOrThrow().CacheRoleAsync(guildId, role);
        public Task CacheEmojiAsync(ulong guildId, Emoji emoji) => GetProviderOrThrow().CacheEmojiAsync(guildId, emoji);
        public Task CacheGuildDataAsync(Guild guild) => GetProviderOrThrow().CacheGuildDataAsync(guild);
        public Task RemoveGuildAsync(ulong guildId) => GetProviderOrThrow().RemoveGuildAsync(guildId);
        public Task ClearAsync() => GetProviderOrThrow().ClearAsync();
        public Task RemoveChannelAsync(ulong channelId) => GetProviderOrThrow().RemoveChannelAsync(channelId);
        public Task RemoveMessageAsync(ulong messageId) => GetProviderOrThrow().RemoveMessageAsync(messageId);
        public Task RemoveGuildMemberAsync(ulong guildId, ulong userId) => GetProviderOrThrow().RemoveGuildMemberAsync(guildId, userId);
        public Task RemoveRoleAsync(ulong guildId, ulong roleId) => GetProviderOrThrow().RemoveRoleAsync(guildId, roleId);

        /// <summary>
        /// Starts automatic health checks.
        /// </summary>
        public void StartHealthChecks()
        {
            if (_healthCheckTimer != null)
                return;

            _healthCheckTimer = new Timer(
                async _ => await PerformHealthChecksAsync(),
                null,
                TimeSpan.Zero,
                _options.HealthCheckInterval
            );

            if (_options.EnableLogging)
            {
                Console.WriteLine("[CacheSwapper] Started automatic health checks");
            }
        }

        /// <summary>
        /// Stops automatic health checks.
        /// </summary>
        public void StopHealthChecks()
        {
            if (_healthCheckTimer != null)
            {
                _healthCheckTimer.Dispose();
                _healthCheckTimer = null;

                if (_options.EnableLogging)
                {
                    Console.WriteLine("[CacheSwapper] Stopped automatic health checks");
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopHealthChecks();

            lock (_lock)
            {
                foreach (var provider in _providers.Values)
                {
                    if (provider.Provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _providers.Clear();
            }
        }
    }
}
