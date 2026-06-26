<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **Build Discord bots without fighting your framework.**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]
  [![Discord][discord-badge]][discord]

  [Docs][docs] &middot; [Examples][examples] &middot; [Changelog][changelog] &middot; [NuGet][nuget] &middot; [Discord][discord]

</div>

---

We're at **`1.1.0-alpha.4`** — core pieces work, things are still settling.

## Why PawSharp?

Discord.NET and DSharpPlus are solid, but both started before .NET had things like `System.Text.Json`, nullable reference types, or native AOT. PawSharp is built for modern .NET from the ground up:

- **Modular packages** — install only what you need
- **async-first** — every API call returns `Task<T>`, no sync-over-async
- **Typed events** — gateway events map to strongly-typed C# classes
- **Automatic rate limiting** — built into the REST client
- **Fluent builder** — configure with `.WithX()`, not a wall of constructor args
- **Native AOT ready** — source-generated JSON, trimming safe

[Why PawSharp vs Discord.NET?](#why-not-discordnet)

---

## Quickstart

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

```
$ DISCORD_TOKEN="your-token" dotnet run
> !ping
< Pong!
```

This example shows: configuring a client, connecting to Discord, listening for events, sending a message.

---

## Which package do I need?

| If you want to... | Install |
|---|---|
| Build a normal Discord bot | `PawSharp.Client` |
| Use only the REST API | `PawSharp.API` |
| Add slash commands | `PawSharp.Commands` + `PawSharp.Interactions` |
| Add voice support | `PawSharp.Voice` |
| Add caching | `PawSharp.Cache` |

`PawSharp.Client` already includes the packages most bots need — you're done with one `dotnet add package`.

---

## What's in the box

**REST API** — channels, messages, guilds, members, roles, webhooks, threads, slash commands, audit logs, auto-moderation, scheduled events, stage instances, stickers, soundboard, polls, and entitlements. Rate limits are handled automatically.

**Gateway** — WebSocket client with resume, heartbeat monitoring, and backoff reconnection. Sharding is built-in. Events map to typed C# objects.

**Commands** — attribute-based prefix commands with middleware, type conversion, and preconditions (permissions, roles, cooldowns). Module auto-discovery is one method call.

**Interactions** — slash commands, buttons, select menus, modals, autocomplete, context menus. The interaction handler routes them with error recovery so users don't see "This interaction failed."

**Interactivity** — pagination, confirmation dialogs, input prompts, polls. Async and timeout-based.

**Voice** — join voice channels, play and receive audio. Pure .NET Opus via Concentus, no native dependencies. DAVE E2EE (MLS / RFC 9420) for encrypted voice. [See the voice guide][voice-guide].

**Caching** — in-memory or Redis, per-entity TTL, eviction, health checks. Providers can be swapped at runtime.

---

## Why not Discord.NET?

| Feature | PawSharp | Discord.NET | DSharpPlus |
|---|---|---|---|
| Modular packages | ✅ | ❌ | ❌ |
| async-first API | ✅ | Partial | Partial |
| Native AOT ready | ✅ | ❌ | ❌ |
| Typed gateway events | ✅ | ✅ | ✅ |
| Fluent builder | ✅ | Partial | ❌ |
| Automatic rate limiting | ✅ | ✅ | ✅ |
| Voice (no native deps) | ✅ | ❌ | ❌ |
| Slash commands | ✅ | ✅ | ✅ |

PawSharp isn't trying to replace every library — it's the option if you want modern .NET features, modularity, and clean APIs without fighting a framework that predates them.

---

## Working examples

The [examples/][examples] directory has bots you can run:

- **ModerationBot** — REST operations, gateway events, basic moderation. Uses the low-level API.
- **MusicBot** — DI setup, commands, voice. Shows the module pattern.
- **DashboardBot** — ASP.NET integration, interaction handlers, webhook verification.

Each has its own README.

---

## Learn more

- [Getting Started][dev-guide]
- [REST API][rest-guide]
- [Gateway Events][gateway-guide]
- [Commands & Interactions](docs/INTERACTIONS_GUIDE.md)
- [Voice][voice-guide]
- [Caching][caching-guide]
- [Patterns & Best Practices][patterns-guide]
- [Error Handling][error-handling]
- [Migration Guide][migration]
- [Troubleshooting][troubleshooting]

---

## Versioning

We follow [SemVer](https://semver.org/). Until 1.0.0, minor bumps may include breaking changes. The [changelog][changelog] and [migration guide][migration] track everything.

---

## Contributing

Pull requests welcome. Read [CONTRIBUTING.md][contributing] first.

---

## Community

[Join our Discord][discord] — questions, ideas, or just to hang out.

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
[discord]:           https://discord.gg/6Z8X8cCHXs
[discord-badge]:     https://img.shields.io/badge/Discord-5865F2?style=flat-square&logo=discord&logoColor=white
