<div align="center">

![PawSharp Banner](assets/pawsharp-banner.svg)

</div>

# PawSharp

A modern Discord API wrapper for .NET 8.0. Clean, fully typed, and built for stability.

**Status:** Alpha — it works, it's tested, ready for serious use but the API might shift before v1.0.

---

## Features

- **Real-time events** via WebSocket with typed event handlers
- **Complete REST API** — everything Discord exposes (messages, channels, guilds, members, roles, webhooks, etc.)
- **Proper error handling** — typed exceptions so you know what went wrong, no null checks
- **Input validation** — catch mistakes before they hit Discord's API
- **Smart caching** — in-memory with per-entity limits, LRU eviction, and automatic cleanup
- **Built-in rate limiting** — respects Discord's buckets and retry-after headers
- **Dependency injection first** — integrates cleanly with `IServiceCollection`
- **Modern async/await** — fully async throughout, nullable reference types enabled

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
- Ready and resume events

**Caching:**
- Automatic in-memory cache for entities
- Configurable per-type size limits (guilds, channels, users, messages, etc.)
- LRU eviction when limits hit
- TTL-based cleanup every 5 minutes

**Error Handling:**
- `ValidationException` — bad input (invalid ID, text too long, etc.)
- `RateLimitException` — Discord rate limit (includes retry-after)
- `DiscordApiException` — API error with status code
- `GatewayException` — WebSocket/connection issues
- `DeserializationException` — JSON parsing failed

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

## What's Not Here (Yet)

- **Voice channels** — separate concern, might be a plugin
- **Sharding** — only needed for 2500+ guilds
- **Redis caching** — memory only for now
- **Advanced reconnection** — coming in Phase 2

See [ROADMAP.md](ROADMAP.md) for the development plan.

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
