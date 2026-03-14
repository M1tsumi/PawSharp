<div align="center">
  <img src="assets/pawsharp-logo.svg" alt="PawSharp Logo" width="180" /><br/><br/>

  # PawSharp

  **A modular Discord API library for .NET**

  [![NuGet][nuget-badge]][nuget]
  [![Discord API][discord-api-badge]][discord-docs]
  [![.NET][dotnet-badge]][dotnet-link]
  [![License][license-badge]][license]
  [![Build][build-badge]][build]

  [Documentation][docs] &middot; [Changelog][changelog] &middot; [Examples][examples] &middot; [NuGet][nuget]

</div>

---

PawSharp is a feature-complete, modular Discord API library for C# and .NET. It covers the full gateway lifecycle, ~140 REST endpoints, prefix commands with preconditions, slash command routing, in-memory caching, per-route rate limiting, interactivity helpers, and full voice support including Discord's **DAVE end-to-end encryption** — all in a single cohesive package suite with zero mandatory third-party dependencies outside the .NET runtime.

> **Status:** `1.0.0-alpha.1` — public alpha. APIs may change between minor versions. See the [versioning policy][versioning].

---

## Packages

Install only what your bot needs:

| Package | Description |
|---------|-------------|
| `PawSharp.Client` | High-level `DiscordClient` — the recommended starting point |
| `PawSharp.Core` | Entities, enums, exceptions, validators, CDN helpers |
| `PawSharp.API` | Raw REST client with bucket-aware rate limiting |
| `PawSharp.Gateway` | WebSocket gateway, heartbeat, sharding, session resume |
| `PawSharp.Commands` | Attribute-based prefix command framework with preconditions |
| `PawSharp.Interactions` | Slash commands, buttons, select menus, modals |
| `PawSharp.Interactivity` | Reaction waiting, polls, pagination, component waiters |
| `PawSharp.Voice` | Voice connections + Opus + RTP + DAVE E2EE (MLS / AES-128-GCM) |

---

## Installation

```bash
# Full client (recommended)
dotnet add package PawSharp.Client

# Or add individual packages
dotnet add package PawSharp.Commands
dotnet add package PawSharp.Interactions
dotnet add package PawSharp.Voice
```

---

## Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .WithPresence("with .NET", status: "online")
    .UseConsoleLogging()
    .Build();

client.OnMessageCreated(async msg =>
{
    if (msg.Author?.Bot == true) return;
    if (msg.Content == "!ping")
        await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Pong! 🏓" });
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

## Prefix Commands

PawSharp's command framework uses attributes and supports cooldowns, permission checks, and guild-only guards out of the box.

```csharp
var commands = client.UseCommands(prefix: "!");
commands.RegisterModule(client, new ModerationCommands());
commands.CommandErrored = async args =>
{
    if (args.Exception is PreconditionFailedException ex)
        await args.Context.ReplyAsync(ex.Message);
};

// ── Module ────────────────────────────────────────────────────────────────────

public class ModerationCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Latency check")]
    public async Task PingAsync(CommandContext ctx)
        => await ctx.ReplyAsync("Pong! 🏓");

    [Command("ban")]
    [RequireGuild]
    [RequirePermissions(Permissions.BanMembers)]
    [Cooldown(maxUses: 3, perSeconds: 10, CooldownBucketType.User)]
    public async Task BanAsync(CommandContext ctx, ulong userId, string reason = "No reason")
    {
        await ctx.Client.Rest.CreateGuildBanAsync(ctx.GuildId!.Value, userId, reason: reason);
        await ctx.ReplyAsync($"User `{userId}` has been banned.");
    }
}
```

---

## Slash Commands

```csharp
client.Interactions.RegisterCommand("ping", async interaction =>
{
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = "Pong! 🏓" }
        });
});
```

---

## Interactivity

Wait for button clicks or select menu submissions directly on a message:

```csharp
var msg = await ctx.Client.Rest.CreateMessageAsync(ctx.ChannelId, new()
{
    Content    = "Choose an option:",
    Components = ComponentBuilder.ActionRow(
        ComponentBuilder.Button("Confirm", customId: "confirm", style: ButtonStyle.Success),
        ComponentBuilder.Button("Cancel",  customId: "cancel",  style: ButtonStyle.Danger))
});

var result = await msg.WaitForButtonAsync(ctx.Client, user: ctx.User, timeout: TimeSpan.FromSeconds(30));

if (result.TimedOut)
    await ctx.RespondAsync("Timed out.");
else if (result.Result!.Data?.CustomId == "confirm")
    await ctx.RespondAsync("Confirmed!");
```

---

## Voice + DAVE E2EE

```csharp
var voice      = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannelId);

await connection.SetSpeakingAsync(true);

// Stream pre-encoded PCM (16-bit mono 48 kHz)
await connection.SendAudioAsync(pcmBytes);

// Or capture from microphone
connection.StartCapture();   // PCM → Opus → AES-128-GCM (DAVE) → RTP → UDP
// ...
connection.StopCapture();

await connection.DisconnectAsync();
```

Each outgoing frame is a 20 ms Opus packet in a 12-byte RTP header (RFC 3550 §5.1, payload type 120). The RTP header is passed as Additional Authenticated Data to AES-128-GCM, so the auth tag covers the full packet. Keys are derived per-sender via HKDF-SHA256 from the MLS epoch secret. The full crypto stack is built on `System.Security.Cryptography` — no third-party crypto packages required.

---

## Dependency Injection

PawSharp integrates cleanly with `Microsoft.Extensions.DependencyInjection`:

```csharp
services.AddSingleton(new PawSharpOptions { Token = token });
services.AddHttpClient<IDiscordRestClient, DiscordRestClient>();
services.AddSingleton<DiscordClient>();
```

---

## Error Handling

All errors surface as typed exceptions:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)  { /* invalid content / embed   */ }
catch (RateLimitException ex)   { /* still rate-limited        */ }
catch (DiscordApiException ex)  { /* non-2xx response from API */ }
```

---

## Features at a Glance

| Area | Detail |
|------|--------|
| **REST** | ~140 endpoints — messages, channels, guilds, roles, webhooks, threads, AutoMod, polls, stage instances, scheduled events |
| **Gateway** | Auto-reconnect, session resume, configurable sharding, all standard opcodes |
| **Caching** | In-memory entity cache (guilds, channels, messages, members, roles) kept in sync from gateway events |
| **Rate Limiting** | Per-route bucket tracking, global rate limit detection, automatic 429 retry with back-off |
| **Commands** | Attribute-based modules, `[RequireGuild]`, `[RequirePermissions]`, `[Cooldown]`, `ReplyAsync` |
| **Interactions** | Slash command routing, button & select handlers, modal support, follow-up messages |
| **Interactivity** | `WaitForReactionAsync`, `WaitForButtonAsync`, `WaitForSelectAsync`, `CollectReactionsAsync`, polls |
| **Voice** | Full Opus encode/decode (Concentus), RFC 3550 RTP framing, DAVE E2EE via RFC 9420 MLS |
| **CDN** | Typed URL builders for avatars, guild icons, banners, emojis, stickers |

---

## Project Layout

```
src/
  PawSharp.Core          — entities, enums, exceptions, builders, validators, CDN helpers
  PawSharp.API           — REST client + advanced rate limiter
  PawSharp.Gateway       — WebSocket gateway, event dispatcher, heartbeat, sharding
  PawSharp.Cache         — cache abstractions + in-memory provider
  PawSharp.Client        — high-level DiscordClient and fluent builder
  PawSharp.Commands      — prefix command framework with preconditions
  PawSharp.Interactions  — slash commands, components, modals
  PawSharp.Interactivity — reaction/component waiting, polls, pagination
  PawSharp.Voice         — voice connections, Opus codec, RTP, DAVE E2EE
tests/                   — unit and integration tests
examples/                — sample bots: DashboardBot, ModerationBot, MusicBot
docs/                    — developer guides + DocFX API reference
```

---

## What Is Still In Progress

| Feature | Status |
|---------|--------|
| Slash command attribute auto-register | Manual registration works; `[SlashCommand]` scanner coming in a future release |
| Redis cache NuGet package | Provider exists and is tested; publication pending |

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md][contributing] before opening a pull request.

---

## License

PawSharp is distributed under the [MIT License][license].

---

<!-- Reference links -->
[nuget]:             https://www.nuget.org/packages/PawSharp.Client
[nuget-badge]:       https://img.shields.io/nuget/v/PawSharp.Client?style=flat-square&color=5865F2&label=nuget
[discord-api-badge]: https://img.shields.io/badge/Discord%20API-v10-5865F2?style=flat-square
[discord-docs]:      https://discord.com/developers/docs
[dotnet-badge]:      https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square
[dotnet-link]:       https://dotnet.microsoft.com/en-us/download/dotnet/8.0
[license-badge]:     https://img.shields.io/badge/license-MIT-22c55e?style=flat-square
[license]:           LICENSE
[build-badge]:       https://img.shields.io/github/actions/workflow/status/M1tsumi/PawSharp/build.yml?style=flat-square
[build]:             https://github.com/M1tsumi/PawSharp/actions
[docs]:              https://github.com/M1tsumi/PawSharp/tree/main/docs
[changelog]:         CHANGELOG.md
[examples]:          examples/
[contributing]:      CONTRIBUTING.md
[versioning]:        docs/VERSIONING_POLICY.md
