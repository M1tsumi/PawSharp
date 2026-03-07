# PawSharp

A Discord bot library for .NET 8. Handles the gateway connection, REST calls, caching, slash commands, and voice.

**Version:** 0.10.0-alpha.2 | **Discord API:** v10 | **Status:** alpha

---

## Install

```bash
# Everything in one package
dotnet add package PawSharp.Client

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
    .WithToken("YOUR_TOKEN")
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

More examples in [examples/](examples/).

---

## What it does

- **REST** — ~140 Discord API endpoints (messages, channels, guilds, roles, webhooks, threads, AutoMod, polls, stage instances, scheduled events, and more)
- **Gateway** — WebSocket connection with auto-reconnect, session resume, sharding, and all opcodes handled
- **Caching** — in-memory entity cache (guilds, channels, messages, members, roles) kept in sync from gateway events
- **Rate limiting** — per-route bucket tracking, automatic retry on 429s
- **Slash commands & interactions** — routing, response builders, and follow-up helpers
- **Prefix commands** — attribute-based command modules (reflection scanner in progress, see below)
- **DAVE E2EE voice** — full RFC 9420 MLS stack for end-to-end encrypted voice, no extra dependencies
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
var voice = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannel);

connection.StartCapture();                   // mic -> encrypted stream
await connection.PlayAudioAsync(audioData);  // received stream -> speaker

await connection.DisconnectAsync();
```

Voice frames are automatically encrypted with AES-128-GCM keys derived from the MLS group epoch.

> **Note:** Real-time Opus audio encode/decode is not implemented yet. The codec infrastructure
> is in place but the actual encode/decode calls are still TODO. See the table below.

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
| Opus audio encode/decode | Framework in place, actual codec calls are TODO |
| Command module auto-registration | Attribute system exists, reflection scanner missing |
| Slash command auto-registration | Manual registration works; attribute-driven bulk-register pending |
| Redis cache provider | Exists in tests, not yet published as a real package |

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
  PawSharp.Voice         - voice connections + DAVE E2EE
tests/                   - unit and integration tests
examples/                - sample bots
docs/                    - guides
```

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

MIT
