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
    /// Connect all shards managed by this instance.
    /// </summary>
    public async Task ConnectAllAsync()
    {
        _logger.LogInformation("Connecting {ShardCount} shards...", _options.Shards);

        for (int i = 0; i < _options.Shards; i++)
        {
            var shard = new GatewayClient(_options, _logger);
            _shards[i] = shard;
            _shardStatuses[i] = ShardStatus.Disconnected;
            
            // Subscribe to state changes
            shard.OnStateChanged += async (oldState, newState) => await OnShardStateChangedAsync(i, oldState, newState);
            
            await shard.ConnectAsync();
            
            // Rate limit: Wait 5 seconds between shard connections
            if (i < _options.Shards - 1)
            {
                await Task.Delay(5000);
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
    /// Queries Discord's <c>GET /gateway/bot</c> endpoint and returns the recommended shard count.
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
                if (info != null && info.Shards > 0)
                    return info.Shards;
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
