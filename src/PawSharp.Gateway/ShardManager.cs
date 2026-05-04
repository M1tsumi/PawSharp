#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.Core.Models;
using PawSharp.Core.Enums;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway;

/// <summary>
/// Tracks Discord session start limits for shard management.
/// </summary>
public class SessionStartLimits
{
    /// <summary>Total number of session starts allowed.</summary>
    public int Total { get; set; }
    
    /// <summary>Remaining number of session starts allowed.</summary>
    public int Remaining { get; set; }
    
    /// <summary>Milliseconds after which the limit resets.</summary>
    public int ResetAfter { get; set; }
    
    /// <summary>Number of identify requests allowed per 5 seconds (max_concurrency).</summary>
    public int MaxConcurrency { get; set; }
    
    /// <summary>Timestamp when these limits were fetched.</summary>
    public DateTimeOffset FetchedAt { get; set; }
    
    /// <summary>
    /// Checks if there are enough remaining session starts for the requested shard count.
    /// </summary>
    public bool HasEnoughRemaining(int requestedShards) => Remaining >= requestedShards;
    
    /// <summary>
    /// Gets the recommended number of shards to start now based on max_concurrency.
    /// Discord allows starting max_concurrency shards simultaneously.
    /// </summary>
    public int GetRecommendedBatchSize() => Math.Max(1, MaxConcurrency);
}

/// <summary>
/// Manages multiple gateway shards for large bots.
/// Provides automatic shard distribution, reconnection, status monitoring, and event aggregation.
/// </summary>
public class ShardManager
{
    private readonly Dictionary<int, GatewayClient> _shards = new();
    private readonly Dictionary<int, ShardStatus> _shardStatuses = new();
    private readonly PawSharpOptions _options;
    private readonly ILogger _logger;
    private readonly EventDispatcher _eventDispatcher;
    private readonly IDiscordRestClient? _restClient;
    private SessionStartLimits? _sessionStartLimits;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardManager"/> class.
    /// </summary>
    /// <param name="options">The PawSharp configuration options including shard settings.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="restClient">Optional REST client used by <see cref="CalculateRecommendedShardCountAsync"/>.</param>
    public ShardManager(PawSharpOptions options, ILogger logger, IDiscordRestClient? restClient = null)
    {
        _options = options;
        _logger = logger;
        _restClient = restClient;
        _eventDispatcher = new EventDispatcher(
            logger,
            options.EventDispatch.MaxQueueSize,
            options.EventDispatch.EnableParallelDispatch,
            options.EventDispatch.MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Event dispatcher for multi-shard events.
    /// </summary>
    public EventDispatcher Events => _eventDispatcher;

    /// <summary>
    /// Total number of shards.
    /// </summary>
    public int ShardCount => _options.ShardCount;

    /// <summary>
    /// Number of shards currently in the <see cref="ShardStatus.Connected"/> state.
    /// </summary>
    public int ConnectedShardCount =>
        _shardStatuses.Values.Count(s => s == ShardStatus.Connected);

    /// <summary>
    /// Calculates the recommended connection delay based on max_concurrency and shard count.
    /// Discord allows max_concurrency shards to identify per 5 second window per shard group.
    /// </summary>
    private int CalculateRecommendedDelayMs()
    {
        if (_sessionStartLimits == null || _sessionStartLimits.MaxConcurrency <= 0)
            return _options.ShardConnectionDelayMs; // Use configured default
        
        var maxConcurrency = _sessionStartLimits.MaxConcurrency;
        // Each "group" (shard_id % max_concurrency) can identify 1 shard per 5 seconds
        // We need to ensure delay is at least 5000ms / maxConcurrency per shard in same group
        // But since we connect sequentially, 5000ms is always safe
        return Math.Max(5000, _options.ShardConnectionDelayMs);
    }

    /// <summary>
    /// Validates shard count against max_concurrency limits before connecting.
    /// Warns if configuration may cause rate limiting issues.
    /// </summary>
    private void ValidateShardConcurrency()
    {
        if (_sessionStartLimits == null || _sessionStartLimits.MaxConcurrency <= 0)
            return;
        
        var maxConcurrency = _sessionStartLimits.MaxConcurrency;
        
        // Check if user is trying to start too many shards for their delay setting
        // Discord allows max_concurrency simultaneous identifies per 5 second window
        var groups = Math.Min(_options.Shards, maxConcurrency);
        var shardsPerGroup = (int)Math.Ceiling(_options.Shards / (double)maxConcurrency);
        var recommendedDelay = 5000; // 5 seconds per Discord spec
        
        if (_options.ShardConnectionDelayMs < recommendedDelay)
        {
            _logger.LogWarning(
                "Shard connection delay ({DelayMs}ms) is less than Discord's recommended 5000ms. " +
                "With {Shards} shards and max_concurrency of {MaxConcurrency}, this may cause rate limiting. " +
                "Consider increasing ShardConnectionDelayMs to at least 5000ms.",
                _options.ShardConnectionDelayMs, _options.Shards, maxConcurrency);
        }
        
        _logger.LogInformation(
            "Shard distribution: {Shards} shards across {Groups} groups (max_concurrency: {MaxConcurrency}, ~{PerGroup} shards/group)",
            _options.Shards, groups, maxConcurrency, shardsPerGroup);
    }

    /// <summary>
    /// Connect all shards managed by this instance.
    /// Validates against session start limits and uses appropriate delays.
    /// </summary>
    public async Task ConnectAllAsync()
    {
        // Validate session start limits
        if (!ValidateSessionStartLimits(_options.Shards))
        {
            throw new InvalidOperationException(
                "Cannot connect: insufficient session start limits remaining. " +
                "Discord limits how many sessions can be started within a time window. " +
                "Wait for the session start limit to reset (typically 5-10 seconds) or increase ShardConnectionDelayMs.");
        }
        
        // Validate shard concurrency configuration
        ValidateShardConcurrency();
        
        var effectiveDelay = CalculateRecommendedDelayMs();
        _logger.LogInformation("Connecting {ShardCount} shards with {DelayMs}ms delay between each...", _options.Shards, effectiveDelay);

        for (int i = 0; i < _options.Shards; i++)
        {
            var shard = new GatewayClient(_options, _logger, restClient: _restClient);
            _shards[i] = shard;
            _shardStatuses[i] = ShardStatus.Disconnected;
            
            // Subscribe to state changes
            shard.OnStateChanged += async (oldState, newState) => await OnShardStateChangedAsync(i, oldState, newState);
            
            await shard.ConnectAsync();
            
            // Rate limit: Wait calculated delay between shard connections
            if (i < _options.Shards - 1)
            {
                await Task.Delay(effectiveDelay);
            }
        }

        _logger.LogInformation("All shards connected!");
    }

    /// <summary>
    /// Handles shard state changes and triggers reconnection if needed.
    /// </summary>
    private async Task OnShardStateChangedAsync(int shardId, GatewayState oldState, GatewayState newState)
    {
        var newStatus = MapGatewayStateToShardStatus(newState);
        _shardStatuses[shardId] = newStatus;
        
        _logger.LogInformation("Shard {ShardId} state changed from {OldState} to {NewState} (status: {Status})", shardId, oldState, newState, newStatus);
        
        // Dispatch shard events
        if (newState == GatewayState.Ready && oldState != GatewayState.Ready)
        {
            await _eventDispatcher.DispatchAsync("SHARD_CONNECTED", new ShardConnectedEvent { ShardId = shardId });
        }
        else if (newState == GatewayState.Disconnected && oldState != GatewayState.Disconnected)
        {
            await _eventDispatcher.DispatchAsync("SHARD_DISCONNECTED", new ShardDisconnectedEvent { ShardId = shardId });
        }
        else if (newState == GatewayState.Failed)
        {
            await _eventDispatcher.DispatchAsync("SHARD_FAILED", new ShardFailedEvent { ShardId = shardId });
        }
        
        if (newState == GatewayState.Failed)
        {
            _logger.LogWarning("Shard {ShardId} failed. Attempting reconnection...", shardId);
            await ReconnectShardAsync(shardId);
        }
    }

    /// <summary>
    /// Reconnects a specific shard.
    /// </summary>
    public async Task ReconnectShardAsync(int shardId)
    {
        if (!_shards.TryGetValue(shardId, out var shard))
        {
            _logger.LogError("Shard {ShardId} not found for reconnection.", shardId);
            return;
        }

        // Check session start limits before attempting reconnection
        if (_sessionStartLimits != null)
        {
            if (_sessionStartLimits.Remaining <= 0)
            {
                var resetTime = DateTimeOffset.UtcNow.AddMilliseconds(_sessionStartLimits.ResetAfter);
                _logger.LogError(
                    "Cannot reconnect shard {ShardId}: Session start limit exhausted. " +
                    "Resets at {ResetTime} (in {RemainingMs}ms).",
                    shardId,
                    resetTime,
                    _sessionStartLimits.ResetAfter);
                _shardStatuses[shardId] = ShardStatus.Failed;
                return;
            }

            _logger.LogDebug(
                "Session start limits check passed for shard {ShardId}: {Remaining}/{Total} remaining",
                shardId,
                _sessionStartLimits.Remaining,
                _sessionStartLimits.Total);
        }

        _shardStatuses[shardId] = ShardStatus.Reconnecting;
        _logger.LogInformation("Reconnecting shard {ShardId}...", shardId);

        try
        {
            await shard.DisconnectAsync();
            await shard.ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect shard {ShardId}.", shardId);
            _shardStatuses[shardId] = ShardStatus.Failed;
        }
    }

    /// <summary>
    /// Disconnect all shards.
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        _logger.LogInformation("Disconnecting all shards...");

        var tasks = _shards.Values.Select(shard => shard.DisconnectAsync());
        await Task.WhenAll(tasks);

        _shards.Clear();
        _shardStatuses.Clear();
        _logger.LogInformation("All shards disconnected!");
    }

    /// <summary>
    /// Get a specific shard by ID.
    /// </summary>
    public GatewayClient? GetShard(int shardId)
    {
        return _shards.TryGetValue(shardId, out var shard) ? shard : null;
    }

    /// <summary>
    /// Get the status of a specific shard.
    /// </summary>
    public ShardStatus GetShardStatus(int shardId)
    {
        return _shardStatuses.TryGetValue(shardId, out var status) ? status : ShardStatus.Disconnected;
    }

    /// <summary>
    /// Get statuses of all shards.
    /// </summary>
    public Dictionary<int, ShardStatus> GetAllShardStatuses()
    {
        return new Dictionary<int, ShardStatus>(_shardStatuses);
    }

    /// <summary>
    /// Calculate recommended shard count based on guild count.
    /// Discord recommends approximately 1000 guilds per shard.
    /// </summary>
    public static int CalculateRecommendedShardCount(int guildCount)
    {
        return Math.Max(1, (int)Math.Ceiling(guildCount / 1000.0));
    }

    /// <summary>
    /// Automatically configures sharding based on guild count.
    /// This is a convenience method that updates the ShardCount property
    /// based on the calculated recommended shard count.
    /// </summary>
    /// <param name="guildCount">The number of guilds the bot is in</param>
    public void AutoConfigureSharding(int guildCount)
    {
        var recommendedShardCount = CalculateRecommendedShardCount(guildCount);
        _options.ShardCount = recommendedShardCount;
        _logger.LogInformation(
            "Auto-configured sharding: {GuildCount} guilds -> {ShardCount} shards",
            guildCount,
            recommendedShardCount);
    }

    /// <summary>
    /// Gets the current session start limits, if fetched from Discord API.
    /// </summary>
    public SessionStartLimits? SessionStartLimits => _sessionStartLimits;

    /// <summary>
    /// Queries Discord's <c>GET /gateway/bot</c> endpoint and returns the recommended shard count.
    /// Also stores session start limits for later validation.
    /// Falls back to <see cref="CalculateRecommendedShardCount"/> with the configured guild count
    /// when no REST client is available.
    /// </summary>
    public async Task<int> CalculateRecommendedShardCountAsync()
    {
        if (_restClient != null)
        {
            try
            {
                var info = await _restClient.GetGatewayBotAsync();
                if (info != null)
                {
                    // Store session start limits for validation
                    if (info.SessionStartLimit != null)
                    {
                        _sessionStartLimits = new SessionStartLimits
                        {
                            Total = info.SessionStartLimit.Total,
                            Remaining = info.SessionStartLimit.Remaining,
                            ResetAfter = info.SessionStartLimit.ResetAfter,
                            MaxConcurrency = info.SessionStartLimit.MaxConcurrency,
                            FetchedAt = DateTimeOffset.UtcNow
                        };
                        
                        _logger.LogDebug(
                            "Session start limits: {Remaining}/{Total} remaining, resets in {ResetAfter}ms, max_concurrency: {MaxConcurrency}",
                            _sessionStartLimits.Remaining,
                            _sessionStartLimits.Total,
                            _sessionStartLimits.ResetAfter,
                            _sessionStartLimits.MaxConcurrency);
                    }
                    
                    if (info.Shards > 0)
                        return info.Shards;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve recommended shard count from Discord; using fallback.");
            }
        }

        // Fallback: use local guild-count heuristic from options
        return CalculateRecommendedShardCount(_options.Shards);
    }
    
    /// <summary>
    /// Validates that there are enough remaining session starts for the requested shard count.
    /// Logs warnings if limits are approaching or exceeded.
    /// </summary>
    /// <returns>True if connection should proceed, false if limits are exceeded.</returns>
    public bool ValidateSessionStartLimits(int requestedShards)
    {
        if (_sessionStartLimits == null)
        {
            _logger.LogWarning("Session start limits not available. Consider calling CalculateRecommendedShardCountAsync() first.");
            return true; // Allow connection when limits are unknown
        }
        
        if (!_sessionStartLimits.HasEnoughRemaining(requestedShards))
        {
            _logger.LogError(
                "Insufficient session starts remaining. Requested: {Requested}, Remaining: {Remaining}, Resets in: {ResetAfter}ms",
                requestedShards,
                _sessionStartLimits.Remaining,
                _sessionStartLimits.ResetAfter);
            return false;
        }
        
        if (_sessionStartLimits.Remaining < requestedShards * 2)
        {
            _logger.LogWarning(
                "Session starts are running low. Remaining: {Remaining}, Requested: {Requested}, Resets in: {ResetAfter}ms",
                _sessionStartLimits.Remaining,
                requestedShards,
                _sessionStartLimits.ResetAfter);
        }
        
        return true;
    }

    /// <summary>
    /// Calculate which shard a guild belongs to.
    /// </summary>
    public int GetShardIdForGuild(ulong guildId)
    {
        return (int)((guildId >> 22) % (ulong)_options.ShardCount);
    }

    /// <summary>
    /// Register an event handler on all shards.
    /// </summary>
    public void OnAllShards<TEvent>(string eventName, Action<TEvent> handler) where TEvent : GatewayEvent
    {
        foreach (var shard in _shards.Values)
        {
            shard.Events.On(eventName, handler);
        }
    }

    /// <summary>
    /// Maps a GatewayState to a ShardStatus.
    /// </summary>
    private static ShardStatus MapGatewayStateToShardStatus(GatewayState state)
    {
        return state switch
        {
            GatewayState.Disconnected => ShardStatus.Disconnected,
            GatewayState.Connecting => ShardStatus.Connecting,
            GatewayState.Connected => ShardStatus.Connected,
            GatewayState.Ready => ShardStatus.Connected,
            GatewayState.Failed => ShardStatus.Failed,
            _ => ShardStatus.Disconnected
        };
    }
}
