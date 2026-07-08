# Sharding

Sharding splits your bot's gateway connection across multiple connections to distribute load. Discord requires sharding when your bot is in more than 2,500 guilds.

---

## When to Shard

- **2500+ guilds**: Discord requires sharding
- **< 2500 guilds**: Single shard is sufficient
- **Multi-instance**: Run shards across multiple processes/machines for fault isolation

```csharp
// Auto-detect requirement
var guildCount = 3000;
if (guildCount > 2500)
{
    var shardCount = ShardManager.CalculateRecommendedShardCount(guildCount);
    // ~1000 guilds per shard → 3 shards
}
```

---

## ShardManager Overview

`ShardManager` (`src/PawSharp.Gateway/ShardManager.cs`) manages multiple `GatewayClient` instances:

```csharp
public class ShardManager : IDisposable
{
    private readonly Dictionary<int, GatewayClient> _shards = new();
    private SessionStartLimits? _sessionStartLimits;

    public EventDispatcher Events { get; }
    public int ShardCount { get; }
    public int ConnectedShardCount { get; }
}
```

### Shard Distribution

```mermaid
flowchart TD
    A[ShardManager] --> B[Shard 0\nGatewayClient]
    A --> C[Shard 1\nGatewayClient]
    A --> D[Shard 2\nGatewayClient]
    B --> E[Discord\nGateway]
    C --> E
    D --> E
    B --> F[EventDispatcher\n(aggregated)]
    C --> F
    D --> F
    F --> G[User Handlers]
```

---

## Starting Shards

```csharp
var shardManager = new ShardManager(options, logger, restClient);

// Connect all shards sequentially with delays
await shardManager.ConnectAllAsync();
```

`ConnectAllAsync` connects shards one at a time with calculated delays to respect session start limits:

```csharp
for (int i = 0; i < options.Shards; i++)
{
    var shard = new GatewayClient(options, logger, restClient: restClient, shardId: i, totalShards: options.Shards);
    _shards[i] = shard;
    await shard.ConnectAsync();
    if (i < options.Shards - 1)
        await Task.Delay(effectiveDelay);
}
```

### Session Start Limits

`SessionStartLimits` tracks Discord's rate limit for starting sessions:

```csharp
public class SessionStartLimits
{
    public int Total { get; set; }
    public int Remaining { get; set; }
    public int ResetAfter { get; set; }
    public int MaxConcurrency { get; set; }
}
```

Fetch via `CalculateRecommendedShardCountAsync()`:

```csharp
await shardManager.CalculateRecommendedShardCountAsync();
// Populates shardManager.SessionStartLimits
```

---

## Event Aggregation Across Shards

Shard events are forwarded to a shared `EventDispatcher` via middleware:

```csharp
shard.Events.Use(async (eventName, eventData) =>
{
    await _eventDispatcher.DispatchTypedAsync(eventName, (GatewayEvent)eventData);
});
```

Multi-shard events:

```csharp
shardManager.Events.On<MessageCreateEvent>("MESSAGE_CREATE", msg =>
{
    Console.WriteLine($"Shard {GetShardForGuild(msg.GuildId)}: {msg.Content}");
});
```

---

## Session Start Limits

Discord enforces session start limits per bot (fetched from `GET /gateway/bot`):

```csharp
var info = await restClient.GetGatewayBotAsync();
// SessionStartLimit: { Total, Remaining, ResetAfter, MaxConcurrency }
```

`ShardManager.ValidateSessionStartLimits()` checks before connecting:

```csharp
if (!ValidateSessionStartLimits(options.Shards))
    throw new InvalidOperationException("Insufficient session start limits");
```

---

## Shard Rebalancing

Use `GetShardIdForGuild` to determine which shard owns a guild:

```csharp
public int GetShardIdForGuild(ulong guildId)
{
    return (int)((guildId >> 22) % (ulong)_options.ShardCount);
}
```

Reconnect a single shard:

```csharp
await shardManager.ReconnectShardAsync(2); // Reconnect shard 2 only
```

---

## Auto-Scaling Shards

```csharp
public void AutoConfigureSharding(int guildCount)
{
    var recommended = CalculateRecommendedShardCount(guildCount);
    options.ShardCount = recommended;
}

public static int CalculateRecommendedShardCount(int guildCount)
{
    return Math.Max(1, (int)Math.Ceiling(guildCount / 1000.0));
}
```

---

## Complete Example

```csharp
var options = new PawSharpOptions
{
    Token = token,
    Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent,
    Shards = 3,  // Or use AutoConfigureSharding
    ShardConnectionDelayMs = 5000
};

var restClient = new DiscordRestClient(httpClient, options, logger);
var shardManager = new ShardManager(options, logger, restClient);

// Calculate recommended shard count
var recommended = await shardManager.CalculateRecommendedShardCountAsync();
options.ShardCount = recommended;

// Connect all shards
await shardManager.ConnectAllAsync();

// Subscribe to aggregated events
shardManager.Events.On<MessageCreateEvent>("MESSAGE_CREATE", msg =>
{
    Console.WriteLine($"[Shard {shardManager.GetShardIdForGuild(msg.GuildId ?? 0)}] {msg.Content}");
});

// Monitor shard status
foreach (var (id, status) in shardManager.GetAllShardStatuses())
    Console.WriteLine($"Shard {id}: {status}");

await Task.Delay(-1);
await shardManager.DisconnectAllAsync();
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Not respecting `MaxConcurrency` | `ShardManager` calculates delay; keep default |
| Connecting all shards in parallel | Sequential with 5s delay per Discord spec |
| Ignoring session start limits | Call `CalculateRecommendedShardCountAsync()` first |
| Using same `GatewayClient` for all shards | Each shard needs its own instance |
| Forgetting to aggregate events | Use `shardManager.Events` not individual shard events |
