#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Models;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway;

/// <summary>
/// Manages multiple gateway shards for large bots.
/// </summary>
public class ShardManager
{
    private readonly Dictionary<int, GatewayClient> _shards = new();
    private readonly PawSharpOptions _options;
    private readonly ILogger _logger;

    public ShardManager(PawSharpOptions options, ILogger logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Total number of shards.
    /// </summary>
    public int ShardCount => _options.ShardCount;

    /// <summary>
    /// Connect all shards managed by this instance.
    /// </summary>
    public async Task ConnectAllAsync()
    {
        _logger.LogInformation($"Connecting {_options.Shards} shards...");

        for (int i = 0; i < _options.Shards; i++)
        {
            var shard = new GatewayClient(_options, _logger);
            _shards[i] = shard;
            
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
    /// Disconnect all shards.
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        _logger.LogInformation("Disconnecting all shards...");

        var tasks = _shards.Values.Select(shard => shard.DisconnectAsync());
        await Task.WhenAll(tasks);

        _shards.Clear();
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
    /// Get all shards.
    /// </summary>
    public IEnumerable<GatewayClient> GetAllShards()
    {
        return _shards.Values;
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
}
