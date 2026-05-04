#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.Cache.Exceptions;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Telemetry;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Distribution
{
    /// <summary>
    /// A cache provider wrapper that distributes cache invalidation events across instances.
    /// </summary>
    public class DistributedCacheProvider : IEntityCache, IDisposable
    {
        private readonly IEntityCache _innerCache;
        private readonly RedisCacheDistributor _distributor;
        private readonly ICacheTelemetry? _telemetry;
        private bool _disposed;

        public ICacheTelemetry? Telemetry
        {
            get => _telemetry;
            set => throw new InvalidOperationException("Telemetry is set at construction time.");
        }

        public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        public event EventHandler? CacheCleared;

        /// <summary>
        /// Creates a new DistributedCacheProvider instance.
        /// </summary>
        /// <param name="innerCache">The underlying cache provider to wrap.</param>
        /// <param name="distributor">The Redis cache distributor for invalidation events.</param>
        /// <param name="telemetry">The cache telemetry instance.</param>
        public DistributedCacheProvider(IEntityCache innerCache, RedisCacheDistributor distributor, ICacheTelemetry? telemetry = null)
        {
            _innerCache = innerCache ?? throw new ArgumentNullException(nameof(innerCache));
            _distributor = distributor ?? throw new ArgumentNullException(nameof(distributor));
            _telemetry = telemetry ?? new CacheTelemetry();

            // Wire up inner cache events
            _innerCache.EntityEvicted += OnInnerCacheEvicted;
            _innerCache.CacheCleared += OnInnerCacheCleared;

            // Wire up distributor events
            _distributor.CacheInvalidationReceived += OnInvalidationReceived;

            // Start listening for invalidations
            _distributor.StartListening();
        }

        private void OnInnerCacheEvicted(object? sender, CacheInvalidationEventArgs args)
        {
            // Publish to other instances
            try
            {
                _ = _distributor.PublishInvalidationAsync(args.EntityType, args.EntityId, args.GuildId);
            }
            catch (CacheDistributionException ex)
            {
                Console.WriteLine($"[DistributedCacheProvider] Failed to publish invalidation: {ex.Message}");
            }

            // Raise local event
            EntityEvicted?.Invoke(sender, args);
        }

        private void OnInnerCacheCleared(object? sender, EventArgs args)
        {
            // Publish to other instances
            try
            {
                _ = _distributor.PublishClearAsync();
            }
            catch (CacheDistributionException ex)
            {
                Console.WriteLine($"[DistributedCacheProvider] Failed to publish clear: {ex.Message}");
            }

            // Raise local event
            CacheCleared?.Invoke(sender, args);
        }

        private void OnInvalidationReceived(object? sender, CacheInvalidationMessage message)
        {
            // Invalidate local cache based on received message
            try
            {
                switch (message.EntityType)
                {
                    case "CLEAR_ALL":
                        _innerCache.Clear();
                        break;

                    case "User":
                        // Users are removed by key, not by ID directly
                        // Skip for now as IEntityCache doesn't have RemoveUser
                        break;

                    case "Guild":
                        _innerCache.RemoveGuild(message.EntityId);
                        break;

                    case "Channel":
                        _innerCache.RemoveChannel(message.EntityId);
                        break;

                    case "Message":
                        _innerCache.RemoveMessage(message.EntityId);
                        break;

                    case "GuildMember" when message.GuildId.HasValue:
                        _innerCache.RemoveGuildMember(message.GuildId.Value, message.EntityId);
                        break;

                    case "Role" when message.GuildId.HasValue:
                        _innerCache.RemoveRole(message.GuildId.Value, message.EntityId);
                        break;

                    case "Emoji" when message.GuildId.HasValue:
                        // Emojis need guild ID, but we don't have entity ID for the emoji itself
                        // This is a limitation of the current message format
                        break;
                }

                // Raise event for local subscribers
                var eventArgs = new CacheInvalidationEventArgs
                {
                    EntityType = message.EntityType,
                    EntityId = message.EntityId,
                    GuildId = message.GuildId
                };

                EntityEvicted?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DistributedCacheProvider] Failed to process invalidation: {ex.Message}");
            }
        }

        // IEntityCache implementation - delegate to inner cache

        public void Add(string key, object entity) => _innerCache.Add(key, entity);
        public object? Get(string key) => _innerCache.Get(key);
        public void Remove(string key) => _innerCache.Remove(key);
        public void Clear() => _innerCache.Clear();
        public bool Exists(string key) => _innerCache.Exists(key);

        public void CacheUser(User user) => _innerCache.CacheUser(user);
        public User? GetUser(ulong userId) => _innerCache.GetUser(userId);

        public void CacheGuild(Guild guild) => _innerCache.CacheGuild(guild);
        public Guild? GetGuild(ulong guildId) => _innerCache.GetGuild(guildId);
        public IEnumerable<Guild> GetAllGuilds() => _innerCache.GetAllGuilds();

        public void CacheChannel(Channel channel) => _innerCache.CacheChannel(channel);
        public Channel? GetChannel(ulong channelId) => _innerCache.GetChannel(channelId);
        public IEnumerable<Channel> GetGuildChannels(ulong guildId) => _innerCache.GetGuildChannels(guildId);

        public void CacheMessage(Message message) => _innerCache.CacheMessage(message);
        public Message? GetMessage(ulong messageId) => _innerCache.GetMessage(messageId);
        public IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50) => _innerCache.GetChannelMessages(channelId, limit);

        public void CacheGuildMember(ulong guildId, GuildMember member) => _innerCache.CacheGuildMember(guildId, member);
        public GuildMember? GetGuildMember(ulong guildId, ulong userId) => _innerCache.GetGuildMember(guildId, userId);
        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId) => _innerCache.GetGuildMembers(guildId);

        public void CacheRole(ulong guildId, Role role) => _innerCache.CacheRole(guildId, role);
        public Role? GetRole(ulong guildId, ulong roleId) => _innerCache.GetRole(guildId, roleId);
        public IEnumerable<Role> GetGuildRoles(ulong guildId) => _innerCache.GetGuildRoles(guildId);

        public void CacheEmoji(ulong guildId, Emoji emoji) => _innerCache.CacheEmoji(guildId, emoji);
        public Emoji? GetEmoji(ulong guildId, ulong emojiId) => _innerCache.GetEmoji(guildId, emojiId);
        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId) => _innerCache.GetGuildEmojis(guildId);

        public void CacheGuildData(Guild guild) => _innerCache.CacheGuildData(guild);
        public void RemoveGuild(ulong guildId) => _innerCache.RemoveGuild(guildId);

        public void RemoveChannel(ulong channelId) => _innerCache.RemoveChannel(channelId);
        public void RemoveMessage(ulong messageId) => _innerCache.RemoveMessage(messageId);
        public void RemoveGuildMember(ulong guildId, ulong userId) => _innerCache.RemoveGuildMember(guildId, userId);
        public void RemoveRole(ulong guildId, ulong roleId) => _innerCache.RemoveRole(guildId, roleId);

        public int GetEntityCount() => _innerCache.GetEntityCount();
        public long GetMemoryUsage() => _innerCache.GetMemoryUsage();
        public CacheStats GetCacheStats() => _innerCache.GetCacheStats();

        public bool IsHealthy() => _innerCache.IsHealthy() && _distributor.IsHealthy();

        // Async operations - delegate to inner cache

        public Task<User?> GetUserAsync(ulong userId) => _innerCache.GetUserAsync(userId);
        public Task<Guild?> GetGuildAsync(ulong guildId) => _innerCache.GetGuildAsync(guildId);
        public Task<Channel?> GetChannelAsync(ulong channelId) => _innerCache.GetChannelAsync(channelId);
        public Task<Message?> GetMessageAsync(ulong messageId) => _innerCache.GetMessageAsync(messageId);
        public Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId) => _innerCache.GetGuildMemberAsync(guildId, userId);
        public Task<Role?> GetRoleAsync(ulong guildId, ulong roleId) => _innerCache.GetRoleAsync(guildId, roleId);
        public Task<Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId) => _innerCache.GetEmojiAsync(guildId, emojiId);

        public Task CacheUserAsync(User user) => _innerCache.CacheUserAsync(user);
        public Task CacheGuildAsync(Guild guild) => _innerCache.CacheGuildAsync(guild);
        public Task CacheChannelAsync(Channel channel) => _innerCache.CacheChannelAsync(channel);
        public Task CacheMessageAsync(Message message) => _innerCache.CacheMessageAsync(message);
        public Task CacheGuildMemberAsync(ulong guildId, GuildMember member) => _innerCache.CacheGuildMemberAsync(guildId, member);
        public Task CacheRoleAsync(ulong guildId, Role role) => _innerCache.CacheRoleAsync(guildId, role);
        public Task CacheEmojiAsync(ulong guildId, Emoji emoji) => _innerCache.CacheEmojiAsync(guildId, emoji);
        public Task CacheGuildDataAsync(Guild guild) => _innerCache.CacheGuildDataAsync(guild);
        public Task RemoveGuildAsync(ulong guildId) => _innerCache.RemoveGuildAsync(guildId);
        public Task ClearAsync() => _innerCache.ClearAsync();

        public Task RemoveChannelAsync(ulong channelId) => _innerCache.RemoveChannelAsync(channelId);
        public Task RemoveMessageAsync(ulong messageId) => _innerCache.RemoveMessageAsync(messageId);
        public Task RemoveGuildMemberAsync(ulong guildId, ulong userId) => _innerCache.RemoveGuildMemberAsync(guildId, userId);
        public Task RemoveRoleAsync(ulong guildId, ulong roleId) => _innerCache.RemoveRoleAsync(guildId, roleId);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _distributor.Dispose();

            if (_innerCache is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
