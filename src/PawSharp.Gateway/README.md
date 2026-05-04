# PawSharp.Gateway

PawSharp.Gateway is the real-time WebSocket layer for Discord events in the PawSharp ecosystem.

It handles the moving parts you do not want to rebuild repeatedly: identify/resume flow, heartbeat management, reconnect strategy, and event dispatch.

## Why Use This Package

- Reliable gateway lifecycle management
- Built-in reconnect and session resume behavior
- Typed event handling patterns
- Sharding support for larger bots
- Works well standalone or with PawSharp.Client

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Gateway --version 1.1.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Gateway;

var gateway = new GatewayClient(new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
});

// Strongly-typed event subscription (no string literals!)
gateway.Events.OnMessageCreate(async evt =>
{
    Console.WriteLine($"Message in {evt.ChannelId}: {evt.Content}");
});

await gateway.ConnectAsync();
```

## Sharding Example

For bots in 1000+ guilds, Discord requires sharding. PawSharp.Gateway makes this easy:

```csharp
using PawSharp.Gateway;

// Calculate recommended shard count or use a fixed number
var shardCount = ShardManager.CalculateRecommendedShardCount(guildCount: 2500); // 3 shards

var options = new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.All,
    ShardCount = shardCount,
    ShardConnectionDelayMs = 5000 // Discord recommends 5s between connections
};

var shardManager = new ShardManager(options, logger);

// Connect all shards
await shardManager.ConnectAllAsync();

// Subscribe to events across all shards
shardManager.Events.OnMessageCreate(async evt =>
{
    Console.WriteLine($"[Shard {evt.ShardId}] Message: {evt.Content}");
});

// Monitor shard health
var statuses = shardManager.GetAllShardStatuses();
Console.WriteLine($"Connected shards: {shardManager.ConnectedShardCount}/{shardCount}");
```

## Advanced Configuration

Use the builder pattern for cleaner configuration:

```csharp
using PawSharp.Gateway;
using PawSharp.Core.Models;

var options = PawSharpOptions.Builder.Create()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.Guilds | GatewayIntents.GuildMessages)
    .WithCompression(true) // Enable zlib-stream compression
    .WithWebSocketBufferSizeKb(128) // Larger buffer for large guilds
    .WithMaxMissedHeartbeatAcks(3)
    .ConfigureReconnection(recon =>
    {
        recon.MaxAttempts = 15;
        recon.InitialDelayMs = 1000;
        recon.MaxDelayMs = 30000;
        recon.JitterFactor = 0.25;
    })
    .ConfigureEventDispatch(dispatch =>
    {
        dispatch.MaxQueueSize = 2000;
        dispatch.EnableParallelDispatch = true;
        dispatch.MaxDegreeOfParallelism = 8;
        dispatch.HandlerTimeoutMs = 5000;
    })
    .WithPresence("online", "Playing with PawSharp")
    .Build();

var gateway = new GatewayClient(options);
```

## Reconnection Handling

The library handles reconnection automatically with exponential backoff. You can monitor reconnection events:

```csharp
// The gateway automatically reconnects on disconnect
// You can monitor the connection state
gateway.Events.OnResumed(async evt =>
{
    Console.WriteLine("Session resumed successfully");
});

// For custom reconnection logic, you can use the ReconnectionManager
// This is automatically used by GatewayClient, but you can configure it
```

## Event Filtering

Filter events before they reach your handlers using middleware:

```csharp
// Add middleware to filter events
gateway.Events.UseMiddleware(async (eventName, eventData, next) =>
{
    // Only process events from a specific guild
    if (eventName == "MESSAGE_CREATE")
    {
        var evt = JsonSerializer.Deserialize<MessageCreateEvent>(eventData);
        if (evt?.GuildId == 123456789)
        {
            await next(); // Process this event
            return;
        }
    }
    // Skip all other events
});
```

## Typical Use Cases

- Bots that need direct control over gateway behavior
- Event-driven processing pipelines
- Sharded deployments at scale

## Related Packages

- `PawSharp.API`: REST operations paired with gateway events
- `PawSharp.Client`: higher-level orchestration around gateway + REST
- `PawSharp.Core`: shared event and entity models

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
