<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **A modular Discord API wrapper for .NET**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]

  [Documentation][docs] &middot; [Changelog][changelog] &middot; [Examples][examples] &middot; [NuGet][nuget]

</div>

---

PawSharp is a Discord API wrapper for C#. Instead of one big library you have to accept on its own terms, it's split into independent packages — grab the full client if you're building a bot, or pick just the pieces you need.

Current release: `1.1.0-alpha.3`. Things are moving fast, but the API is stabilizing. See the [versioning policy][versioning] for what to expect, and [MIGRATION.md][migration] if you're upgrading from an earlier alpha.

## Where to start

**You want a bot up and running in five minutes.** Start with the quickstart below using `PawSharpClientBuilder`, then read the [DEVELOPERS_GUIDE.md][dev-guide] when you're ready to go deeper.

**You want to understand how the pieces fit together.** Read the [INDEX.md][docs-index] — it maps out every module, every guide, and links to code examples.

**You're migrating from a previous alpha.** Check [MIGRATION.md][migration] for breaking changes.

**You ran into a problem.** The [TROUBLESHOOTING.md][troubleshooting] guide covers the most common issues.

---

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
| `PawSharp.Client` | Top-level `DiscordClient` — fluent builder, DI, connection state tracking, 130+ convenience methods | Core, API, Gateway, Cache |
| `PawSharp.Core` | Shared entities (`Guild`, `Channel`, `Message`, `User`, `Role`), enums, builders (`EmbedBuilder`, `ComponentBuilder`), validation, serialization | — |
| `PawSharp.API` | Raw REST client with 140+ Discord endpoints, automatic rate limiting, telemetry | Core |
| `PawSharp.Gateway` | WebSocket connection, heartbeat, resume, reconnection, sharding, 40+ typed events | Core, API, Cache |
| `PawSharp.Commands` | Prefix commands via `[Command]` attributes, preconditions (`[RequireOwner]`, `[RequirePermissions]`, `[Cooldown]`), type conversion, middleware pipeline | Core, API, Client |
| `PawSharp.Interactions` | Slash commands, message components (buttons, select menus), modals, autocomplete, context menus, webhook verification | Core, API, Gateway |
| `PawSharp.Interactivity` | Pagination (reactions + buttons), wait-for-input helpers, polls, confirmation dialogs | Core, Client |
| `PawSharp.Voice` | Voice gateway, UDP audio transport, Opus encode/decode, DAVE end-to-end encryption (MLS / RFC 9420) | Core, API, Gateway, Client |
| `PawSharp.Cache` | In-memory and Redis caching with per-entity TTL, LRU eviction, health checks, telemetry, dynamic provider swapping | Core |

---

## What you can do

**REST API** — 140+ endpoints covered. Messages, channels, guilds, members, roles, webhooks, threads, reactions, slash commands, audit logs, auto-moderation, scheduled events, stage instances, stickers, soundboard, polls, entitlements, onboarding — all with typed request/response models. Rate limiting is handled automatically with configurable retry logic and telemetry events.

**Gateway** — WebSocket connection lifecycle with automatic resume, heartbeat monitoring, and exponential-backoff reconnection. Over 40 typed events (`OnMessageCreated`, `OnGuildMemberJoined`, etc.) with an event-interest filtering system that tells you which intents you're missing. Sharding is built in, including auto-rebalancing for large bots.

**Commands** — Attribute-based prefix commands with a full middleware pipeline, type conversion (14 built-in converters plus custom), preconditions for permissions, ownership, roles, cooldowns, guild/DM/NSFW scoping. Modules can be registered manually or auto-discovered with assembly scanning.

**Interactions** — Slash command registration, button and select menu handling, modals, autocomplete, user/message context menus. The `InteractionHandler` routes incoming interactions to the right handler with built-in error recovery that tells the user something went wrong instead of silently timing out.

**Interactivity** — High-level helpers that remove the boilerplate from common patterns: paginated messages (reactions or buttons), confirmation dialogs, input prompts, poll creation and voting. All with configurable timeouts.

**Voice** — Full Discord Voice Protocol v8 with UDP audio transport, Opus encoding/decoding (via Concentus, pure .NET — no native DLLs), and DAVE end-to-end encryption using MLS (RFC 9420) with X25519 key exchange, Ed25519 signatures, and AES-128-GCM frame encryption. Multiple simultaneous voice connections supported.

**Caching** — Pluggable cache layer with in-memory (`MemoryCacheProvider`) and Redis (`RedisCacheProvider`) implementations. Per-entity TTL, LRU eviction, health checks, cache telemetry (hits, misses, operation durations), and dynamic provider swapping with circuit breaker fallback.

---

## Going further

- **[DEVELOPERS_GUIDE.md][dev-guide]** — Installation, first bot, configuration, core concepts, best practices
- **[REST_API_GUIDE.md][rest-guide]** — Full REST endpoint reference with code examples
- **[GATEWAY_GUIDE.md][gateway-guide]** — Events, connection lifecycle, sharding, middleware
- **[CACHING_GUIDE.md][caching-guide]** — In-memory cache, Redis, strategies, monitoring
- **[PATTERNS_GUIDE.md][patterns-guide]** — Real-world patterns: moderation, logging, pagination
- **[VOICE_GUIDE.md][voice-guide]** — Voice connections, Opus, DAVE E2EE deep dive
- **[ERROR_HANDLING.md][error-handling]** — Exception hierarchy, common errors, recovery strategies
- **[MIGRATION.md][migration]** — Breaking changes between alpha versions
- **[TROUBLESHOOTING.md][troubleshooting]** — Common issues and solutions

---

## Example bots

The [examples/][examples] directory has three working bots that show different patterns:

- **ModerationBot** — Gateway events, REST operations, moderation logic. Uses the low-level API directly.
- **MusicBot** — DI setup, commands with `[Command]` attributes, voice integration. Shows the module pattern.
- **DashboardBot** — ASP.NET integration, interaction handlers, webhook verification. Shows HTTP interaction mode.

Each example has its own README with setup instructions.

---

## Versioning

PawSharp follows [Semantic Versioning](https://semver.org/). Until 1.0.0, minor version bumps may include breaking changes. See [VERSIONING_POLICY.md][versioning] for the full policy.

---

## Contributing

Pull requests are welcome. Read [CONTRIBUTING.md][contributing] first — it covers code style, testing, documentation, and the release process.

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
