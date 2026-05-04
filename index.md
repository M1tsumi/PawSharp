---
_disableToc: false
---

# PawSharp

A modular Discord API wrapper for **.NET 10** — REST, Gateway, caching, slash
commands, prefix commands, interactivity, and voice with full DAVE E2EE.

**Current version:** `1.1.0-alpha.2` | **Discord API:** v10

---

## Install

```bash
# The all-in-one package
dotnet add package PawSharp.Client

# Or only what you need
dotnet add package PawSharp.API           # REST client (~140 endpoints)
dotnet add package PawSharp.Gateway       # WebSocket gateway + sharding
dotnet add package PawSharp.Commands      # Prefix text commands
dotnet add package PawSharp.Interactions  # Slash commands, buttons, modals
dotnet add package PawSharp.Interactivity # Reaction waits, polls, pagination
dotnet add package PawSharp.Voice         # Voice + DAVE E2EE (Opus/MLS)
```

---

## Ping bot in ~15 lines

```csharp
var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .WithPresence("pinging", status: "online")
    .UseConsoleLogging()
    .Build();

client.OnMessageCreated(async msg =>
{
    if (msg.Author?.Bot == true) return;
    if (msg.Content == "!ping")
        await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Pong!" });
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

## What's in each package

| Package | What's inside |
|---------|--------------|
| `PawSharp.Core` | Entities, enums, exceptions, `EmbedBuilder`, validators |
| `PawSharp.API` | `IDiscordRestClient` — ~140 typed endpoints, `AdvancedRateLimiter` |
| `PawSharp.Gateway` | WebSocket connection, heartbeat, `ShardManager`, 60+ events |
| `PawSharp.Cache` | `IEntityCache`, `MemoryCacheProvider`, `RedisCacheProvider` |
| `PawSharp.Client` | `DiscordClient` facade, `CacheManager`, DI extensions |
| `PawSharp.Commands` | `[Command]` attribute modules, `CommandContext`, error events |
| `PawSharp.Interactions` | Slash commands, component builders, modal routing |
| `PawSharp.Interactivity` | Reaction waits, polls, pagination helpers |
| `PawSharp.Voice` | Voice WebSocket, Opus codec, DAVE E2EE (RFC 9420 MLS) |

---

## Next steps

- New here? Start with the [Getting Started guide](docs/DEVELOPERS_GUIDE.md).
- Building voice features? See the [Voice & DAVE guide](docs/VOICE_GUIDE.md).
- Looking for a specific type? Browse the [API Reference](api/index.md).
