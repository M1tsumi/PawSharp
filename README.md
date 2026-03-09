# PawSharp

A Discord bot library for .NET 8. Handles the gateway connection, REST calls,
caching, slash commands, and voice with full DAVE E2EE.

**Version:** 0.11.0-alpha.1 | **Discord API:** v10 | **Status:** alpha | [Changelog](CHANGELOG.md)

---

## Install

```bash
# Everything in one package
dotnet add package PawSharp.Client  # 0.11.0-alpha.1

# Or pick only what you need
dotnet add package PawSharp.API           # REST endpoints only
dotnet add package PawSharp.Gateway       # WebSocket / gateway only
dotnet add package PawSharp.Commands      # Prefix-based text commands
dotnet add package PawSharp.Interactions  # Slash commands & components
dotnet add package PawSharp.Interactivity # Reactions, polls, pagination
dotnet add package PawSharp.Voice         # Voice channels + DAVE E2EE
```

---

## Quickstart

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

More examples in [examples/](examples/).

---

## What it does

- **REST** — ~140 Discord API endpoints (messages, channels, guilds, roles, webhooks, threads, AutoMod, polls, stage instances, scheduled events, and more)
- **Gateway** — WebSocket connection with auto-reconnect, session resume, sharding, and all opcodes handled
- **Caching** — in-memory entity cache (guilds, channels, messages, members, roles) kept in sync from gateway events
- **Rate limiting** — per-route bucket tracking, automatic retry on 429s
- **Slash commands & interactions** — routing, response builders, and follow-up helpers
- **Prefix commands** — attribute-based command modules (reflection scanner in progress, see below)
- **Voice + DAVE E2EE** — Opus encode/decode (Concentus), RTP framing, AES-128-GCM per RFC 9420 MLS — zero extra crypto dependencies
- **CDN helpers** — typed URL builders for avatars, guild icons, banners, emojis, and stickers

---

## Error handling

Everything throws typed exceptions:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, new() { Content = text });
}
catch (ValidationException ex)  { /* bad input  */ }
catch (RateLimitException ex)   { /* slow down  */ }
catch (DiscordApiException ex)  { /* API error  */ }
```

---

## Slash commands

```csharp
client.Interactions.RegisterCommand("ping", async interaction =>
{
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = "Pong!" }
        });
});
```

---

## Voice (DAVE E2EE)

```csharp
var voice      = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannel);

// Tell Discord you're about to speak, then start the mic pipeline
await connection.SetSpeakingAsync(true);
connection.StartCapture();   // PCM captured → Opus encoded → DAVE encrypted → RTP packet → sent

// Push pre-recorded PCM (16-bit signed mono 48 kHz) directly
await connection.SendAudioAsync(pcmBytes);

// Incoming packets are automatically decrypted and decoded; push to speaker:
await connection.PlayAudioAsync(receivedPcm);

await connection.SetSpeakingAsync(false);
connection.StopCapture();
await connection.DisconnectAsync();
```

Each outgoing frame is a 20 ms Opus packet wrapped in a 12-byte RTP header
(RFC 3550 §5.1, payload type 120). The header is passed as Additional
Authenticated Data to AES-128-GCM so the auth tag covers the full packet, not
just the payload. Keys are derived per-sender via HKDF-SHA256 from the MLS
epoch secret — the entire crypto stack is built on `System.Security.Cryptography`
primitives without any third-party crypto libraries.

---

## Prefix commands

```csharp
var commands = client.UseCommands("!");

public class MyCommands : BaseCommandModule
{
    [Command("ping")]
    public async Task PingAsync(CommandContext ctx)
        => await ctx.RespondAsync("Pong!");
}

commands.RegisterModule(new MyCommands());
```

> **Note:** The reflection scanner that discovers `[Command]` methods and wires them up is
> not yet implemented. Registration will work once that ships in the next release.

---

## Dependency injection

```csharp
services.AddSingleton(new PawSharpOptions { Token = token });
services.AddSingleton<IDiscordRestClient, DiscordRestClient>();
services.AddSingleton<DiscordClient>();
```

---

## What is not done yet

| Feature | Status |
|---------|--------|
| Command module auto-registration | Attribute system works; reflection scanner not shipped yet |
| Slash command attribute auto-register | Manual registration works; `[SlashCommand]` scanner pending |
| Redis cache as published package | Provider exists and is tested; not yet on NuGet |

---

## Project layout

```
src/
  PawSharp.Core          - entities, enums, exceptions, builders, validators
  PawSharp.API           - REST client and rate limiter
  PawSharp.Gateway       - WebSocket gateway, event dispatcher, heartbeat, sharding
  PawSharp.Cache         - cache abstractions and in-memory provider
  PawSharp.Client        - high-level DiscordClient combining all of the above
  PawSharp.Commands      - prefix command framework
  PawSharp.Interactions  - slash commands, buttons, modals
  PawSharp.Interactivity - reaction waiting, polls, pagination
  PawSharp.Voice         - voice connections + DAVE E2EE (Opus + MLS + RTP)
tests/                   - unit and integration tests (94+ passing)
examples/                - sample bots (DashboardBot, ModerationBot, MusicBot)
docs/                    - developer guides + DocFX API reference
```

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

MIT
