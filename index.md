---
_disableToc: false
---

# PawSharp

A modular Discord API wrapper for **.NET 10** -- REST, Gateway, caching, slash
commands, prefix commands, interactivity, and voice with full DAVE E2EE.

**Current version:** `1.1.0-alpha.5` | **Discord API:** v10

---

## Getting Started

| Guide | Description |
|-------|-------------|
| [Installation](docs/installation.md) | System requirements, NuGet packages, project setup |
| [Getting Started](docs/getting-started.md) | What PawSharp is, architecture overview, Hello World bot |
| [Your First Bot](docs/guides/first-bot.md) | Step-by-step tutorial from zero to running bot |
| [Configuration](docs/getting-started.md#configuration) | Fluent builder, options, intents, tokens |
| [FAQ](docs/faq.md) | Frequently asked questions and troubleshooting |

## Guides

### Core

| Guide | Topics |
|-------|--------|
| [Gateway](docs/guides/gateway.md) | WebSocket connection, heartbeat, resume, reconnection, sharding |
| [Events](docs/guides/events.md) | 60+ typed events, event filtering, middleware, intent validation |
| [Sending Messages](docs/guides/sending-messages.md) | Text, embeds, components, replies, forwarding, crossposting |
| [Receiving Messages](docs/guides/receiving-messages.md) | Message events, content handling, caching, spam detection |
| [Slash Commands](docs/guides/slash-commands.md) | Registration, options, groups, permissions, autocomplete |

### Interactions

| Guide | Topics |
|-------|--------|
| [Components](docs/guides/components.md) | Buttons, select menus, action rows, Component V2 |
| [Modals](docs/guides/modals.md) | Modal dialogs, text inputs, submission handling |
| [Context Menus](docs/guides/context-menus.md) | User and message context menu commands |
| [Permissions](docs/guides/permissions.md) | Permission model, role hierarchy, channel overwrites |

### Content

| Guide | Topics |
|-------|--------|
| [Embeds](docs/guides/embeds.md) | EmbedBuilder, rich embeds, limits, templates |
| [Attachments](docs/guides/attachments.md) | File uploads, FileBuilder, multiple attachments |
| [Threads](docs/guides/threads.md) | Thread creation, management, forum channels |
| [Polls](docs/guides/polls.md) | Creating polls, voting, results, events |

### Features

| Guide | Topics |
|-------|--------|
| [Voice](docs/guides/voice.md) | Voice connections, Opus audio, DAVE E2EE |
| [Webhooks](docs/guides/webhooks.md) | Creating, executing, and managing webhooks |
| [Auto Moderation](docs/guides/auto-moderation.md) | Rules, triggers, actions, keyword filtering |
| [Scheduled Events](docs/guides/scheduled-events.md) | Guild events, status workflow, event users |

### System

| Guide | Topics |
|-------|--------|
| [Rate Limits](docs/guides/rate-limits.md) | Bucket tracking, global limits, retry logic, telemetry |
| [Caching](docs/guides/caching.md) | In-memory cache, Redis provider, eviction, TTL |
| [Logging](docs/guides/logging.md) | ILogger integration, structured logging, Serilog |
| [Error Handling](docs/guides/error-handling.md) | Exception hierarchy, global handlers, recovery |

### Advanced

| Guide | Topics |
|-------|--------|
| [Sharding](docs/guides/sharding.md) | Multi-shard operation, rebalancing, session limits |
| [Performance](docs/guides/performance.md) | AOT readiness, array pooling, parallelism |
| [Memory Usage](docs/guides/memory-usage.md) | Cache memory, LRU tracking, buffer management |
| [Extension System](docs/guides/extension-system.md) | Custom middleware, converters, cache providers |
| [REST Pipeline](docs/guides/rest-pipeline.md) | Request flow, auth, serialization, bucket tracking |
| [Gateway Pipeline](docs/guides/gateway-pipeline.md) | WebSocket compression, dispatch, heartbeat |
| [Serialization](docs/guides/serialization.md) | Source generation, snowflake converters, AOT |
| [Benchmarks](docs/guides/benchmarks.md) | Performance metrics, running benchmarks |

---

## Quick Start

```bash
dotnet add package PawSharp.Client
```

```csharp
var client = new PawSharpClientBuilder()
 .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
 .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
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

## Package Reference

| Package | What's inside |
|---------|---------------|
| `PawSharp.Core` | Entities, enums, exceptions, `EmbedBuilder`, validators |
| `PawSharp.API` | `IDiscordRestClient`, `AdvancedRateLimiter`, serialization |
| `PawSharp.Gateway` | `GatewayClient`, `EventDispatcher`, `ShardManager` |
| `PawSharp.Cache` | `IEntityCache`, `MemoryCacheProvider`, `RedisCacheProvider` |
| `PawSharp.Client` | `DiscordClient` facade, `PawSharpClientBuilder`, DI |
| `PawSharp.Commands` | `[Command]` attribute modules, type conversion, middleware |
| `PawSharp.Interactions` | `InteractionHandler`, slash command builders, modals |
| `PawSharp.Interactivity` | Pagination, polls, confirmation dialogs |
| `PawSharp.Voice` | Voice WebSocket, Opus codec, DAVE E2EE |

### API Reference

Browse the full [API Reference](api/index.md) for all public types, methods, and members.

---

## Contributing

See the [contributing guides](docs/contributing/building-from-source.md) for build instructions,
coding guidelines, test runner setup, and repository structure.
