# Sharding Guide

Sharding allows PawSharp bots to scale to handle thousands of guilds by distributing the load across multiple gateway connections.

## When to Use Sharding

Discord recommends sharding when your bot:
- Is in 1000+ guilds
- Receives high event volumes
- Needs to handle large numbers of users

## Basic Usage

```csharp
using PawSharp.Gateway;
using PawSharp.Core.Models;

// Configure for 2 shards out of 2 total
var options = new PawSharpOptions
{
    Token = "your-bot-token",
    Shards = 2,        // Number of shards for this instance
    ShardCount = 2     // Total shards across all instances
};

var shardManager = new ShardManager(options, logger);

// Connect all shards
await shardManager.ConnectAllAsync();

// Monitor shard status
var status = shardManager.GetShardStatus(0); // ShardStatus.Connected, etc.

// Calculate which shard handles a guild
int shardId = shardManager.GetShardIdForGuild(guildId);
```

## Auto-Sharding

PawSharp provides a helper to calculate recommended shard count:

```csharp
int recommendedShards = ShardManager.CalculateRecommendedShardCount(guildCount);
```

This follows Discord's guideline of approximately 1000 guilds per shard.

## Shard Events

Listen to shard-level events:

```csharp
shardManager.Events.On<ShardConnectedEvent>("ShardConnected", async (evt) =>
{
    Console.WriteLine($"Shard {evt.ShardId} connected!");
});

shardManager.Events.On<ShardFailedEvent>("ShardFailed", async (evt) =>
{
    Console.WriteLine($"Shard {evt.ShardId} failed - attempting reconnection...");
});
```

## Monitoring and Diagnostics

```csharp
// Get status of all shards
var allStatuses = shardManager.GetAllShardStatuses();

// Count connected shards
int connectedCount = shardManager.ConnectedShardCount;

// Get a specific shard
var shard = shardManager.GetShard(0);
```

## Reconnection

Shards automatically reconnect on failure. You can also manually reconnect:

```csharp
await shardManager.ReconnectShardAsync(0);
```

## Best Practices

1. **Rate Limiting**: Space shard connections 5 seconds apart
2. **Monitoring**: Track shard statuses in production
3. **Error Handling**: Handle shard failures gracefully
4. **Resource Management**: Monitor memory and CPU per shard
5. **Load Balancing**: Distribute shards across multiple processes/machines if needed

## Advanced Configuration

For horizontal scaling across multiple processes:

```csharp
// Process 1: Shards 0-4
var options1 = new PawSharpOptions { Shards = 5, ShardCount = 10 };

// Process 2: Shards 5-9
var options2 = new PawSharpOptions { Shards = 5, ShardCount = 10, ShardOffset = 5 };
```

Note: Multi-process sharding requires coordination (planned for future release).