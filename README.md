<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **A modular Discord API wrapper for .NET**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]
  [![Discord][discord-badge]][discord]

  [Documentation][docs] &middot; [Changelog][changelog] &middot; [Examples][examples] &middot; [NuGet][nuget] &middot; [Support][discord]

</div>

---

PawSharp is a Discord API wrapper for C#. It's split into independent packages — grab the full client for a bot, or pick just the pieces you need.

Current release: `1.1.0-alpha.3`. See the [versioning policy][versioning] and [MIGRATION.md][migration] if upgrading from an earlier alpha.

## Quickstart

### 1. Install the packages

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.3
```

### 2. Write a minimal bot

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

string token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("Set DISCORD_TOKEN before running.");

var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .UseConsoleLogging()
    .Build();

client.OnMessageCreated(async evt =>
{
    if (evt.Author?.IsBot == true) return;
    if (evt.Content == "!ping")
        await client.SendMessageAsync(evt.ChannelId, "Pong!");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

That's it. The builder wires up the REST client, gateway, cache, and logging with sensible defaults.

### 3. Run it

```bash
export DISCORD_TOKEN="your-bot-token-here"
dotnet run
```

---

## Packages

PawSharp is modular by design. Install only what you need.

| Package | Purpose | Depends on |
|---------|---------|-----------|
| `PawSharp.Client` | Top-level `DiscordClient` — fluent builder, DI, connection state, 130+ convenience methods | Core, API, Gateway, Cache |
| `PawSharp.Core` | Shared entities, enums, builders, validation, serialization | — |
| `PawSharp.API` | Raw REST client, 140+ endpoints, auto rate limiting, telemetry | Core |
| `PawSharp.Gateway` | WebSocket, heartbeat, resume, reconnection, sharding, 40+ typed events | Core, API, Cache |
| `PawSharp.Commands` | Prefix commands via `[Command]`, preconditions, type conversion, middleware | Core, API, Client |
| `PawSharp.Interactions` | Slash commands, components, modals, autocomplete, context menus | Core, API, Gateway |
| `PawSharp.Interactivity` | Pagination, wait-for-input, polls, confirmation dialogs | Core, Client |
| `PawSharp.Voice` | Voice gateway, UDP audio, Opus encode/decode, DAVE E2EE | Core, API, Gateway, Client |
| `PawSharp.Cache` | In-memory and Redis caching, per-entity TTL, LRU eviction, health checks | Core |

---

## What you can do

**REST API** — 140+ endpoints covering messages, channels, guilds, members, roles, webhooks, threads, reactions, slash commands, audit logs, auto-moderation, scheduled events, stage instances, stickers, soundboard, polls, entitlements, and onboarding. All with typed models and automatic rate limiting.

**Gateway** — WebSocket lifecycle with automatic resume, heartbeat monitoring, and exponential-backoff reconnection. Over 40 typed events with intent filtering. Built-in sharding with auto-rebalancing for large bots.

**Commands** — Attribute-based prefix commands with middleware, type conversion (14 built-in plus custom), and preconditions for permissions, ownership, roles, cooldowns, and scoping. Auto-discover modules with assembly scanning.

**Interactions** — Slash command registration, buttons, select menus, modals, autocomplete, and context menus. The `InteractionHandler` routes interactions with error recovery so users get feedback instead of silent timeouts.

**Interactivity** — Paginated messages (reactions or buttons), confirmations, input prompts, polls. Configurable timeouts.

**Voice** — Discord Voice Protocol v8 with UDP audio, Opus codec (pure .NET via Concentus, no native DLLs), and DAVE E2EE (MLS / RFC 9420) with X25519 + Ed25519 + AES-128-GCM. Multiple simultaneous connections.

**Caching** — Pluggable in-memory or Redis cache with per-entity TTL, LRU eviction, health checks, and telemetry. Dynamic provider swapping with circuit breaker fallback.

---

## Going further

- [DEVELOPERS_GUIDE.md][dev-guide] — Setup, first bot, configuration, best practices
- [REST_API_GUIDE.md][rest-guide] — Full REST endpoint reference with examples
- [GATEWAY_GUIDE.md][gateway-guide] — Events, lifecycle, sharding, middleware
- [CACHING_GUIDE.md][caching-guide] — In-memory and Redis caching strategies
- [PATTERNS_GUIDE.md][patterns-guide] — Moderation, logging, pagination patterns
- [VOICE_GUIDE.md][voice-guide] — Voice connections, Opus, DAVE E2EE
- [ERROR_HANDLING.md][error-handling] — Exception hierarchy, recovery strategies
- [MIGRATION.md][migration] — Breaking changes between alpha versions
- [TROUBLESHOOTING.md][troubleshooting] — Common issues and fixes

---

## Example bots

The [examples/][examples] directory has three working bots:

- **ModerationBot** — Gateway events, REST operations, moderation logic. Uses the low-level API.
- **MusicBot** — DI setup, commands, voice integration. Shows the module pattern.
- **DashboardBot** — ASP.NET integration, interaction handlers, webhook verification. HTTP interaction mode.

Each example has its own README with setup instructions.

---

## Versioning

PawSharp follows [Semantic Versioning](https://semver.org/). Until 1.0.0, minor bumps may include breaking changes. See [VERSIONING_POLICY.md][versioning] for details.

---

## Contributing

Pull requests welcome. Read [CONTRIBUTING.md][contributing] first — it covers code style, testing, docs, and the release process.

---

## License

MIT. See [LICENSE][license].

---

<!-- Reference links -->
[nuget]:             https://www.nuget.org/packages/PawSharp.Client
[nuget-badge]:       https://img.shields.io/nuget/vpre/PawSharp.Client?style=flat-square&color=5865F2&label=nuget
[discord-api-badge]: https://img.shields.io/badge/Discord%20API-v10-5865F2?style=flat-square
[discord-docs]:      https://discord.com/developers/docs
[dotnet-badge]:      https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square
[dotnet-link]:       https://dotnet.microsoft.com/en-us/download/dotnet/10.0
[license-badge]:     https://img.shields.io/badge/license-MIT-22c55e?style=flat-square
[license]:           LICENSE
[build-badge]:       https://github.com/M1tsumi/PawSharp/actions/workflows/ci.yml/badge.svg
[build]:             https://github.com/M1tsumi/PawSharp/actions/workflows/ci.yml
[docs]:              https://github.com/M1tsumi/PawSharp/tree/main/docs
[docs-index]:        docs/INDEX.md
[dev-guide]:         docs/DEVELOPERS_GUIDE.md
[rest-guide]:        docs/REST_API_GUIDE.md
[gateway-guide]:     docs/GATEWAY_GUIDE.md
[caching-guide]:     docs/CACHING_GUIDE.md
[patterns-guide]:    docs/PATTERNS_GUIDE.md
[voice-guide]:       docs/VOICE_GUIDE.md
[error-handling]:    docs/ERROR_HANDLING.md
[migration]:         docs/MIGRATION.md
[troubleshooting]:   docs/TROUBLESHOOTING.md
[changelog]:         CHANGELOG.md
[examples]:          examples/
[contributing]:      CONTRIBUTING.md
[versioning]:        docs/VERSIONING_POLICY.md
[discord]:           https://discord.gg/6Z8X8cCHXs
[discord-badge]:     https://img.shields.io/badge/Discord-5865F2?style=flat-square&logo=discord&logoColor=white
