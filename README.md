<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **A modular Discord API wrapper for .NET — friendly, powerful, and built for real bots.**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]
  [![Discord][discord-badge]][discord]

  [Docs][docs] &middot; [Examples][examples] &middot; [Changelog][changelog] &middot; [NuGet][nuget] &middot; [Support][discord]

</div>

---

Hey there! 👋 Welcome to PawSharp.

Building a Discord bot in .NET should feel good. PawSharp is designed to be **modular, predictable, and a joy to work with** — whether you're hacking together your first bot in an afternoon or scaling a production system across dozens of shards.

Grab the all-in-one client to get started fast, or pick just the packages you need. Everything works together, nothing gets in your way.

Current release: **`1.1.0-alpha.4`**. See the [versioning policy][versioning] and [MIGRATION.md][migration] if you're upgrading from an earlier alpha.

---

## Get started in 60 seconds

### 1. Install the client

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.4
```

### 2. Write your first bot

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

Three lines of config. One event handler. Your bot is alive.

### 3. Run it

```bash
export DISCORD_TOKEN="your-bot-token-here"
dotnet run
```

---

## What's inside

PawSharp is modular by design. Install the full `PawSharp.Client` for batteries-included setup, or cherry-pick what you need.

| Package | What it does | Depends on |
|---------|-------------|-----------|
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

## What you can build with it

**REST API** — 140+ endpoints covering messages, channels, guilds, members, roles, webhooks, threads, reactions, slash commands, audit logs, auto-moderation, scheduled events, stage instances, stickers, soundboard, polls, entitlements, and onboarding. All with typed models and automatic rate limiting so you never have to think about headers.

**Gateway** — WebSocket lifecycle with automatic resume, heartbeat monitoring, and exponential-backoff reconnection. Over 40 typed events with intent filtering. Built-in sharding with auto-rebalancing for bots that outgrow a single connection.

**Commands** — Attribute-based prefix commands with middleware, type conversion (14 built-in converters plus custom ones), and preconditions for permissions, ownership, roles, cooldowns, and scoping. Auto-discover modules with assembly scanning — registration is one line.

**Interactions** — Slash command registration, buttons, select menus, modals, autocomplete, and context menus. The `InteractionHandler` routes every interaction with error recovery baked in, so users get feedback instead of Discord's dreaded "This interaction failed" timeout.

**Interactivity** — Paginated messages (reactions or buttons), confirmation dialogs, input prompts, polls. Configurable timeouts and a clean async `await` API.

**Voice** — Discord Voice Protocol with UDP audio, Opus codec (pure .NET via Concentus — zero native DLLs), and DAVE E2EE (MLS / RFC 9420) with X25519 + Ed25519 + AES-128-GCM. Multiple simultaneous connections.

**Caching** — Pluggable in-memory or Redis cache with per-entity TTL, LRU eviction, health checks, and telemetry. Swap providers at runtime with circuit breaker fallback — your bot keeps running even if Redis goes down.

---

## Real examples to learn from

The [examples/][examples] directory has three working bots you can run and tweak:

- **ModerationBot** — Gateway events, REST operations, moderation logic. Uses the low-level API directly.
- **MusicBot** — DI setup, commands, voice integration. Shows the module pattern in action.
- **DashboardBot** — ASP.NET integration, interaction handlers, webhook verification. HTTP interaction mode for web-first bots.

Each one has its own README with setup instructions. Good code to learn from, even better to hack on.

---

## Diving deeper

- [DEVELOPERS_GUIDE.md][dev-guide] — Setup, first bot, configuration, best practices
- [REST_API_GUIDE.md][rest-guide] — Full REST endpoint reference with examples
- [GATEWAY_GUIDE.md][gateway-guide] — Events, lifecycle, sharding, middleware
- [CACHING_GUIDE.md][caching-guide] — In-memory and Redis caching strategies
- [PATTERNS_GUIDE.md][patterns-guide] — Moderation, logging, pagination patterns
- [VOICE_GUIDE.md][voice-guide] — Voice connections, Opus, DAVE E2EE
- [ERROR_HANDLING.md][error-handling] — Exception hierarchy, recovery strategies
- [MIGRATION.md][migration] — Breaking changes between alpha versions
- [TROUBLESHOOTING.md][troubleshooting] — Common issues and how to fix them

---

## Versioning & stability

PawSharp follows [Semantic Versioning](https://semver.org/). Until 1.0.0, minor bumps may include breaking changes — each one is documented in the changelog and [MIGRATION.md][migration]. See [VERSIONING_POLICY.md][versioning] for the full story.

---

## Contributing

Pull requests are welcome and appreciated. Read [CONTRIBUTING.md][contributing] first — it covers code style, testing, documentation, and how the release process works.

---

## Join the community

Got a question, an idea, or just want to say hi? [Join our Discord][discord]. We're friendly, we promise.

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
