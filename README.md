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

PawSharp is a Discord API wrapper built for C# developers who want modularity without the baggage. Instead of one monolithic library, you get independent packages — grab the full client if you're building a bot, or pick just the pieces you need (REST, Gateway, Voice, Interactions) for more specialized projects.

Current release status: `1.1.0-alpha.3`. It's still early days, but the library is functional and growing fast. See the [versioning policy][versioning] for what to expect.

## Getting Started

Install the packages you need:

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.3
dotnet add package PawSharp.Commands --version 1.1.0-alpha.3
dotnet add package PawSharp.Interactions --version 1.1.0-alpha.3
dotnet add package PawSharp.Interactivity --version 1.1.0-alpha.3
dotnet add package PawSharp.Voice --version 1.1.0-alpha.3
```

Here's a minimal bot that responds to `!ping`:

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("Set DISCORD_TOKEN before starting the bot.");

var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .UseConsoleLogging()
    .Build();

client.OnMessageCreated(async evt =>
{
    if (evt.Author?.Bot == true) return;

    if (evt.Content == "!ping")
        await client.SendMessageAsync(evt.ChannelId, "Pong!");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

## Packages

| Package | What it does |
|---------|-------------|
| **PawSharp.Client** | Entry point — `DiscordClient` with a fluent builder, logging, and DI integration |
| **PawSharp.Core** | Shared entities, enums, exceptions, builders (embeds, components) |
| **PawSharp.API** | Raw REST layer with rate-limit handling (140+ endpoints) |
| **PawSharp.Gateway** | WebSocket connection, heartbeat, reconnect, event dispatch |
| **PawSharp.Commands** | Prefix commands with attributes, preconditions, cooldowns |
| **PawSharp.Interactions** | Slash commands, components, modals |
| **PawSharp.Interactivity** | Wait for reactions, buttons, select menus — no boilerplate |
| **PawSharp.Voice** | Voice transport, Opus codec, Discord DAVE E2EE encryption |

## Dependency Injection

If you're using `Microsoft.Extensions.DependencyInjection`, you can wire everything up in one call:

```csharp
using PawSharp.Client.Extensions;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;

services.SetupPawSharp(new PawSharpOptions
{
    Token = token,
    Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent
});

services.AddPawSharpCommands();
services.AddPawSharpInteractions();
```

## What's Here

- REST coverage for 140+ Discord endpoints with automatic rate-limit handling
- Gateway connection lifecycle with heartbeat, resume, and reconnection
- Prefix commands with preconditions (permissions, cooldowns, guild-only)
- Slash commands, message components, and modals
- Interactivity helpers — wait for reactions, buttons, and select menus without tracking state yourself
- In-memory and Redis caching
- Voice support with Opus audio, RTP framing, and Discord DAVE end-to-end encryption (including DM/GroupDM calls)

## What's Still Cooking

- Slash command auto-registration scanner (manual registration works today)
- Dedicated Redis cache NuGet package (provider implementation exists)

## Documentation

- Start here: [docs/INDEX.md][docs-index]
- REST guide: [docs/REST_API_GUIDE.md][rest-guide]
- Gateway guide: [docs/GATEWAY_GUIDE.md][gateway-guide]
- Voice guide: [docs/VOICE_GUIDE.md][voice-guide]
- Troubleshooting: [docs/TROUBLESHOOTING.md][troubleshooting]
- Working sample bots: [examples][examples]

## Contributing

Pull requests are welcome. Check out [CONTRIBUTING.md][contributing] before opening one.

## License

MIT — see [LICENSE][license].

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
[rest-guide]:        docs/REST_API_GUIDE.md
[gateway-guide]:     docs/GATEWAY_GUIDE.md
[voice-guide]:       docs/VOICE_GUIDE.md
[troubleshooting]:   docs/TROUBLESHOOTING.md
[changelog]:         CHANGELOG.md
[examples]:          examples/
[contributing]:      CONTRIBUTING.md
[versioning]:        docs/VERSIONING_POLICY.md
