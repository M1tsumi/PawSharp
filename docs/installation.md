# Installation

## NuGet Packages

PawSharp is distributed as multiple NuGet packages so you can install only what you need.

| Package | What's inside | When to use |
|---------|--------------|-------------|
| `PawSharp.Core` | Entities, enums, exceptions, `EmbedBuilder`, validators | Required by all packages |
| `PawSharp.API` | `IDiscordRestClient` with 140+ typed endpoints, `AdvancedRateLimiter` | REST-only bots |
| `PawSharp.Gateway` | WebSocket connection, heartbeat, `ShardManager`, 60+ events | Real-time event handling |
| `PawSharp.Cache` | `IEntityCache`, `MemoryCacheProvider`, `RedisCacheProvider` | Caching layer |
| `PawSharp.Client` | `DiscordClient` facade, `CacheManager`, DI extensions | All-in-one (recommended) |
| `PawSharp.Commands` | `[Command]` attribute modules, `CommandContext`, error events | Prefix text commands |
| `PawSharp.Interactions` | Slash commands, component builders, modal routing | Slash commands & components |
| `PawSharp.Interactivity` | Reaction waits, polls, pagination helpers | Interactive features |
| `PawSharp.Voice` | Voice WebSocket, Opus codec, DAVE E2EE (RFC 9420 MLS) | Voice features |

## Package Management

```bash
# Everything (recommended)
dotnet add package PawSharp.Client

# Just REST API
dotnet add package PawSharp.API

# Just Gateway
dotnet add package PawSharp.Gateway

# With commands
dotnet add package PawSharp.Commands

# With interactions (slash commands)
dotnet add package PawSharp.Interactions

# With voice
dotnet add package PawSharp.Voice
```

## Building from Source

Clone the repository and build:

```bash
git clone https://github.com/M1tsumi/PawSharp.git
cd PawSharp
dotnet build
```

The built packages will be in the `nupkgs/` directory and can be referenced locally.
