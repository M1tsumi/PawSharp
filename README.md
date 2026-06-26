<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **A Discord API wrapper for .NET that doesn't get in your way.**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]
  [![Discord][discord-badge]][discord]

  [Docs][docs] &middot; [Examples][examples] &middot; [Changelog][changelog] &middot; [NuGet][nuget] &middot; [Discord][discord]

</div>

---

We're building a Discord API wrapper for .NET that's modular, predictable, and actually pleasant to use. You can grab the full client and be running in a few lines, or pick just the pieces you need if you already have your own setup.

We're currently at **`1.1.0-alpha.4`** — things are still taking shape, but all the core pieces work. Check the [changelog][changelog] and [migration guide][migration] if you're coming from an earlier version.

---

## Starting from scratch

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.4
```

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
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

```bash
export DISCORD_TOKEN="your-bot-token-here"
dotnet run
```

That's it. The builder connects the REST client, gateway, cache, and logging. One event handler. Your bot is online.

---

## What's here

PawSharp is split into separate packages. You can install `PawSharp.Client` and get everything, or just what you need.

| Package | What's in it |
|---------|-------------|
| `PawSharp.Client` | Top-level `DiscordClient`, fluent builder, DI wiring, connection state, 130+ convenience methods |
| `PawSharp.Core` | Entities, enums, builders, validation |
| `PawSharp.API` | REST client with ~140 endpoints, auto rate limiting, telemetry |
| `PawSharp.Gateway` | WebSocket, heartbeat, resume, reconnection, sharding, typed events |
| `PawSharp.Commands` | Prefix commands via `[Command]`, preconditions, type conversion |
| `PawSharp.Interactions` | Slash commands, buttons, modals, autocomplete, context menus |
| `PawSharp.Interactivity` | Pagination, reaction/button waits, polls, confirmations |
| `PawSharp.Voice` | Voice gateway, UDP audio, Opus, DAVE E2EE |
| `PawSharp.Cache` | In-memory and Redis cache, per-entity TTL, health checks |

---

## The gist of each piece

**REST API** covers messages, channels, guilds, members, roles, webhooks, threads, reactions, slash commands, audit logs, auto-moderation, scheduled events, stage instances, stickers, soundboard, polls, and entitlements. Everything's typed and rate limits are handled automatically.

**Gateway** gives you WebSocket lifecycle with resume, heartbeat monitoring, and backoff reconnection. There are ~40 typed events. Sharding is built-in for larger bots.

**Commands** are attribute-based prefix commands. Has middleware, type conversion (14 built-in converters, plus you can write your own), and preconditions for permissions, roles, cooldowns, and the like. Module auto-discovery is one method call.

**Interactions** handles slash commands, buttons, select menus, modals, and context menus. The `InteractionHandler` routes them with error recovery so users don't get the dreaded "This interaction failed" screen.

**Interactivity** gives you paginated messages, confirmation dialogs, input prompts, and polls. Everything's async and timeout-based.

**Voice** does UDP audio with Opus (pure .NET via Concentus, zero native code) and DAVE E2EE (MLS / RFC 9420 with DHKEM(P-256, HKDF-SHA256) ciphersuite and AES-128-GCM frame encryption). Multiple simultaneous connections work.

**Caching** can be in-memory or Redis, with per-entity TTL, eviction, and health checks. You can swap providers at runtime — if Redis goes down, it falls back gracefully.

---

## Working examples

The [examples/][examples] directory has bots you can actually run:

- **ModerationBot** — REST operations, gateway events, basic moderation. Uses the low-level API directly.
- **MusicBot** — DI setup, commands, voice. Shows the module pattern.
- **DashboardBot** — ASP.NET integration, interaction handlers, webhook verification. HTTP interaction mode.

Each has its own README.

---

## Going further

- [Getting Started][dev-guide] — setup, your first bot, config
- [REST API Guide][rest-guide] — endpoint reference
- [Gateway Guide][gateway-guide] — events, lifecycle, sharding
- [Caching Guide][caching-guide] — in-memory and Redis
- [Voice Guide][voice-guide] — voice connections, Opus, DAVE
- [Patterns Guide][patterns-guide] — moderation, logging, pagination
- [Error Handling][error-handling] — exception hierarchy, recovery
- [Migration Guide][migration] — breaking changes between versions
- [Troubleshooting][troubleshooting] — common problems and fixes

---

## A note on versioning

We follow [Semantic Versioning](https://semver.org/). Until we reach 1.0.0 stable, minor bumps may include breaking changes. We keep a clean changelog and migration guide so you're not left guessing.

---

## Contributing

Pull requests are welcome. Read [CONTRIBUTING.md][contributing] first — it covers code style, testing, and how releases work.

---

## Community

Got questions, ideas, or just want to hang out? [Join our Discord][discord].

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
