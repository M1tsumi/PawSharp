<div align="center">

![PawSharp Banner](assets/pawsharp-banner.svg)

</div>

# PawSharp

A modern, stable Discord API wrapper for .NET 8.0. Production-ready with automatic reconnection, proper error handling, and comprehensive Discord API coverage.

**Current Version:** 0.5.0-alpha7
**Status:** Phase 3 complete - Testing, documentation, and monitoring fully implemented. Suitable for production bots.

---

## Key Features

- **Automatic Reconnection** - Exponential backoff with session resumption, handles network issues gracefully
- **Heartbeat Health Monitoring** - Detects zombie connections and reconnects automatically
- **Complete REST API** - All Discord endpoints (messages, channels, guilds, members, roles, webhooks, etc.)
- **Real-time Events** - WebSocket gateway with typed event handlers
- **Typed Error Handling** - Custom exception hierarchy, no null checks needed
- **Input Validation** - Catch mistakes before hitting Discord's API
- **Smart Caching** - In-memory with per-entity limits, LRU eviction, automatic TTL cleanup
- **Rate Limiting** - Automatic rate limit handling with proper bucket tracking
- **Dependency Injection** - First-class support for .NET DI container
- **Fully Async** - Modern async/await throughout with nullable reference types

---

## Installation

**Requirements:** .NET 8.0 SDK or later

```bash
git clone <repository>
cd PawSharp
dotnet build
```

NuGet package coming soon.

---

## Quick Example

Here's a bot that responds to `!ping`:

```csharp
using PawSharp.Client;
using PawSharp.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(config => config.AddConsole());
services.AddSingleton(new PawSharpOptions 
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<DiscordClient>();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();

client.Gateway.OnMessageCreate += async message =>
{
    if (message.Author.Bot) return;
    
    if (message.Content == "!ping")
    {
        try
        {
            await client.Rest.CreateMessageAsync(message.ChannelId, new CreateMessageRequest
            {
                Content = "Pong!"
            });
        }
        catch (RateLimitException ex)
        {
            Console.WriteLine($"Rate limited, wait {ex.RetryAfter}s");
        }
        catch (DiscordApiException ex)
        {
            Console.WriteLine($"API error: {ex.Message}");
        }
    }
};

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

More examples in [examples/](examples/).

---

## What's Included

**Gateway Resilience:**
- Automatic reconnection with exponential backoff (1s, 2s, 4s, 8s, 16s maximum)
- Session resumption within 45 seconds of disconnect
- Heartbeat ACK tracking to detect unhealthy connections
- All 12 Discord gateway opcodes handled correctly
- State machine preventing invalid state transitions
- Maximum 10 reconnection attempts before giving up
- Events for monitoring connection state and reconnection attempts

**REST API:**
- Messages (create, edit, delete, fetch, reactions, pins)
- Channels (CRUD, permissions, webhooks)
- Guilds (info, members, roles, bans, audit logs)
- Users and current user endpoints
- Interactions and slash commands
- Scheduled events
- Threads and thread management
- Auto-moderation

**Gateway Events:**
- Message create/update/delete
- Guild and member events
- Channel and role events
- Interaction create
- Thread events
- Connection state transitions
- Ready and resume events

**Caching:**
- Automatic in-memory cache for entities
- Configurable per-type size limits (10K messages, 1K guilds, 5K users, etc.)
- LRU eviction when limits hit
- TTL-based cleanup every 5 minutes

**Error Handling:**
- `ValidationException` - bad input (invalid ID, text too long, etc.)
- `RateLimitException` - Discord rate limit with retry-after
- `DiscordApiException` - API error with status code
- `GatewayException` - WebSocket/connection issues
- `DeserializationException` - JSON parsing failed

---

## Architecture

```
PawSharp.Core
├── Entities (Guild, Channel, Message, User, etc.)
├── Enums (PermissionFlags, MessageType, etc.)
├── Exceptions (custom exception hierarchy)
├── Validation (input validators)
├── Models (API request/response types)
└── Interfaces (IDiscordRestClient, etc.)

PawSharp.API
├── REST client (all Discord HTTP endpoints)
├── Rate limiting (per-route bucket tracking)
└── Request/response models

PawSharp.Cache
└── In-memory cache provider (with per-entity limits)

PawSharp.Gateway
├── WebSocket connection management
├── Event dispatch system
├── Heartbeat handling
└── Shard manager (coming Phase 2)

PawSharp.Interactions
└── Slash command and component builders

PawSharp.Client
├── High-level Discord client
└── Event handlers
```

Use the pieces you need. Mix and match.

---

## Error Handling

No more null checks. Everything throws typed exceptions:

```csharp
try 
{
    var message = await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
    {
        Content = userText
    });
    // message exists, no need to check
}
catch (ValidationException ex) when (ex.Message.Contains("2000"))
{
    Console.WriteLine("Text exceeds 2000 characters");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, retry in {ex.RetryAfter}s");
    await Task.Delay(ex.RetryAfter * 1000);
}
catch (DiscordApiException ex)
{
    Console.WriteLine($"Discord error {ex.StatusCode}: {ex.Message}");
}
```

---

## Dependency Injection

Designed to work with .NET's DI from the start:

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(new PawSharpOptions { Token = token });
services.AddSingleton<IDiscordRestClient, RestClient>();
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<DiscordClient>();
services.AddSingleton<GatewayClient>();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

---

## What's Not Implemented Yet

- Voice channels and audio support
- Sharding (needed for bots in 2500+ guilds)
- Redis caching (in-memory cache only)
- Distributed clustering (single-machine bots fully supported)

See [ROADMAP.md](ROADMAP.md) for the development plan and future phases.

---

## Contributing

Help wanted. Check the [ROADMAP.md](ROADMAP.md) for what we're building.

---

## Resources

- [Discord Developer Portal](https://discord.com/developers/applications) — get your bot token
- [Discord API Documentation](https://discord.com/developers/docs/intro) — the source of truth
- [Examples](examples/) — real code samples

---

## License

MIT License. Do what you want with it.
