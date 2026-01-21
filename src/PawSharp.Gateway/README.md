# PawSharp.Gateway

Robust WebSocket gateway client with automatic reconnection and event handling.

PawSharp.Gateway provides the WebSocket connection to Discord's Gateway, handling real-time events, heartbeat management, session resumption, and connection reliability with exponential backoff.

## Features

- Reliable WebSocket connections with automatic negotiation
- Dynamic heartbeat interval adjustment from Discord's HELLO
- Session resumption with state preservation
- Typed event dispatching with middleware support
- Single and multi-shard support with auto-sharding
- zlib/zstd compression support
- Gateway-level rate limiting awareness
- Health checks and zombie connection detection
- Full Discord intent system support

## 📦 Installation

```bash
dotnet add package PawSharp.Gateway --version 0.5.0-alpha10
```

## 🚀 Quick Start

```csharp
using PawSharp.Gateway;

// Create gateway client
var gateway = new GatewayClient(new PawSharpOptions
{
    Token = "your-bot-token",
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
});

// Handle connection
await gateway.ConnectAsync();

// Listen for events
gateway.Events.On<ReadyEvent>("READY", async evt =>
{
    Console.WriteLine($"Connected as {evt.User.Username}!");
});

gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async evt =>
{
    Console.WriteLine($"Message: {evt.Content}");
});
```

## 📋 Gateway Lifecycle

### Connection Establishment

```csharp
// Connect with automatic handling of:
// - WebSocket connection establishment
// - HELLO packet processing
// - Heartbeat interval negotiation
// - IDENTIFY/RESUME payload sending
// - Session state initialization
await gateway.ConnectAsync();
```

### Event Handling

```csharp
// Typed event handling
gateway.Events.On<MessageCreateEvent>(async evt =>
{
    Console.WriteLine($"Message from {evt.Author.Username}: {evt.Content}");
});

// Raw event handling
gateway.Events.OnRawEvent("MESSAGE_CREATE", async (type, data) =>
{
    Console.WriteLine($"Raw event: {type}");
});
```

### Event Middleware

```csharp
// Add middleware for all events
gateway.Events.Use(async (evt, next) =>
{
    var startTime = DateTime.UtcNow;
    await next();
    var duration = DateTime.UtcNow - startTime;
    Console.WriteLine($"Event {evt.Type} processed in {duration.TotalMilliseconds}ms");
});
```

## 🔧 Configuration

```csharp
var options = new PawSharpOptions
{
    // Authentication
    Token = "your-bot-token",

    // Gateway settings
    Intents = GatewayIntents.All,
    EnableCompression = true,
    ShardCount = 1, // Or null for auto-sharding
    ShardId = 0,

    // Connection settings
    MaxMissedHeartbeatAcks = 3,
    ReconnectionBackoffMin = 1000,
    ReconnectionBackoffMax = 16000,

    // Presence
    Presence = new Presence
    {
        Status = UserStatus.Online,
        Activities = new[] { new Activity { Name = "with PawSharp!" } }
    }
};

var gateway = new GatewayClient(options);
```

## 📊 Sharding

### Auto-Sharding

```csharp
// Let PawSharp determine optimal shard count
var options = new PawSharpOptions
{
    Token = "your-bot-token",
    ShardCount = null // Auto-shard
};

var gateway = new GatewayClient(options);
```

### Manual Sharding

```csharp
// Manual shard configuration
var shard0 = new GatewayClient(new PawSharpOptions
{
    Token = "your-bot-token",
    ShardId = 0,
    ShardCount = 3
});

var shard1 = new GatewayClient(new PawSharpOptions
{
    Token = "your-bot-token",
    ShardId = 1,
    ShardCount = 3
});
```

## 🔄 Reconnection & Reliability

### Automatic Reconnection

```csharp
// Reconnection happens automatically on:
// - Network interruptions
// - Gateway server restarts
// - Rate limiting
// - Temporary service issues

// Exponential backoff: 1s → 2s → 4s → 8s → 16s (max)
// Session resumption preserves state
// Events are replayed after reconnection
```

### Connection Monitoring

```csharp
gateway.OnConnectionLost += async () =>
{
    Console.WriteLine("Gateway connection lost");
};

gateway.OnReconnected += async () =>
{
    Console.WriteLine("Gateway reconnected successfully");
};

gateway.OnZombieDetected += async () =>
{
    Console.WriteLine("Zombie connection detected - forcing reconnect");
};
```

## 🎯 Event Types

### Lifecycle Events

```csharp
gateway.Events.On<ReadyEvent>("READY", async evt =>
{
    Console.WriteLine($"Bot ready! Guilds: {evt.Guilds.Count}");
});

gateway.Events.On<ResumedEvent>("RESUMED", async evt =>
{
    Console.WriteLine("Session resumed successfully");
});
```

### Guild Events

```csharp
gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", async evt =>
{
    Console.WriteLine($"Joined guild: {evt.Guild.Name}");
});

gateway.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", async evt =>
{
    Console.WriteLine($"Member joined: {evt.User.Username}");
});
```

### Message Events

```csharp
gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async evt =>
{
    if (evt.Content.StartsWith("!"))
    {
        // Handle command
    }
});

gateway.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", async evt =>
{
    Console.WriteLine($"Message edited: {evt.Id}");
});
```

### Voice Events

```csharp
gateway.Events.On<VoiceStateUpdateEvent>("VOICE_STATE_UPDATE", async evt =>
{
    if (evt.ChannelId.HasValue)
    {
        Console.WriteLine($"{evt.UserId} joined voice channel");
    }
});
```

## 📈 Performance & Monitoring

### Health Checks

```csharp
// Connection status
Console.WriteLine($"State: {gateway.State}");
Console.WriteLine($"Uptime: {gateway.Uptime}");
Console.WriteLine($"Heartbeat Ping: {gateway.LastHeartbeatAck - gateway.LastHeartbeatSent}");

// Event statistics
var stats = gateway.GetEventStats();
Console.WriteLine($"Events Received: {stats.TotalEvents}");
Console.WriteLine($"Events Per Second: {stats.EventsPerSecond}");
```

### Metrics

```csharp
// Performance metrics
var metrics = gateway.GetMetrics();
Console.WriteLine($"Reconnections: {metrics.ReconnectionCount}");
Console.WriteLine($"Missed Heartbeats: {metrics.MissedHeartbeats}");
Console.WriteLine($"Compression Ratio: {metrics.CompressionRatio:P}");
```

## 🏗️ Architecture

```
PawSharp.Gateway
├── GatewayClient (main interface)
│   ├── WebSocketConnection (transport layer)
│   ├── HeartbeatManager (heartbeat handling)
│   ├── ReconnectionManager (reconnection logic)
│   ├── ShardManager (sharding support)
│   └── EventDispatcher (event system)
├── Event Types (all Discord events)
├── Payload Handling (JSON serialization)
└── Compression (zlib/zstd support)
```

## ⚠️ Error Handling

```csharp
try
{
    await gateway.ConnectAsync();
}
catch (GatewayConnectionException ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
catch (GatewayAuthenticationException ex)
{
    Console.WriteLine("Invalid token or intents");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## 🔧 Intents

```csharp
// Specify only needed intents for privacy
var intents = GatewayIntents.Guilds |
              GatewayIntents.GuildMessages |
              GatewayIntents.MessageContent;

var gateway = new GatewayClient(new PawSharpOptions
{
    Token = "token",
    Intents = intents
});
```

## 🤝 Dependencies

- **PawSharp.Core** - Entity models and types
- **System.Net.WebSockets** - WebSocket implementation
- **System.IO.Compression** - Compression support
- **Microsoft.Extensions.Logging** - Structured logging

## 📚 Related Packages

- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - High-level client
- **[PawSharp.API](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.API)** - REST API client
- **[PawSharp.Cache](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Cache)** - Caching layer

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.