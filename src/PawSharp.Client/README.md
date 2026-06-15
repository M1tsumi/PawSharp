# PawSharp.Client

PawSharp.Client is the high-level entry point for building Discord bots with PawSharp.

It provides a unified client surface for REST API, Gateway WebSocket, entity caching, and interaction handling. Additional functionality like prefix commands, interactivity helpers, and voice connectivity are available as separate extension packages that seamlessly integrate with the client.

## Why Use This Package

- Fastest way to build a Discord bot with PawSharp
- Unified client surface for REST, Gateway, Cache, and Interactions
- Extension model for adding Commands, Interactivity, and Voice
- Clean integration with dependency injection and hosted services
- Comprehensive event subscription API with 50+ typed handlers
- Automatic entity caching from gateway events
- Configurable intent validation for catching configuration errors early

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.3
```

## Quick Start

### Basic Bot Setup

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .Build();

// Subscribe to message events
client.OnMessageCreated(async msg => 
{
    if (msg.Content.StartsWith("!ping"))
        await msg.Channel.SendMessageAsync("Pong!");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

### With Dependency Injection (ASP.NET Core)

`SetupPawSharp` and `AddPawSharp` both use the in-memory cache by default; pass a custom `IEntityCache` factory only when you need a different cache backend.

```csharp
using PawSharp.Client;
using PawSharp.Client.Extensions;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure PawSharp with in-memory cache
builder.Services.SetupPawSharp(new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent
});

var app = builder.Build();

// Get the client and connect
var client = app.Services.GetRequiredService<DiscordClient>();
await client.ConnectAsync();

await app.RunAsync();
```

## Core Features

### REST API Access

```csharp
using PawSharp.Core.Entities;
using PawSharp.Core.Models;

// Send a message
await client.SendMessageAsync(channelId, "Hello, world!");

// Send a message with embeds
await client.SendMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Check out this embed:",
    Embeds = new[] { new Embed { Title = "Example", Description = "Description" } }
});

// Forward a message
await client.ForwardMessageAsync(targetChannelId, sourceChannelId, sourceMessageId);

// Get current user
var user = await client.GetCurrentUserAsync();
```

### Gateway Event Subscriptions

```csharp
// Ready event
client.OnReady(async ready => 
{
    Console.WriteLine($"Bot logged in as {ready.User.Username}");
});

// Message events
client.OnMessageCreated(async msg => { /* ... */ });
client.OnMessageUpdated(async msg => { /* ... */ });
client.OnMessageDeleted(async msg => { /* ... */ });

// Guild events
client.OnGuildAvailable(async guild => { /* ... */ });
client.OnGuildMemberJoined(async member => { /* ... */ });
client.OnRoleCreated(async role => { /* ... */ });

// Voice events (for tracking, not voice connectivity)
client.OnVoiceStateUpdated(async state => { /* ... */ });
client.OnVoiceServerUpdated(async server => { /* ... */ });

// User events
client.OnUserUpdated(async user => { /* ... */ });

// Soundboard events
client.OnSoundboardSoundCreated(async sound => { /* ... */ });
client.OnSoundboardSoundUpdated(async sound => { /* ... */ });
client.OnSoundboardSoundDeleted(async sound => { /* ... */ });
client.OnSoundboardSoundsUpdated(async sounds => { /* ... */ });

// Subscription events
client.OnSubscriptionCreated(async sub => { /* ... */ });
client.OnSubscriptionUpdated(async sub => { /* ... */ });
client.OnSubscriptionDeleted(async sub => { /* ... */ });

// Voice channel effects
client.OnVoiceChannelEffectSent(async effect => { /* ... */ });
client.OnVoiceChannelStatusUpdated(async status => { /* ... */ });

// And 40+ more typed event subscriptions
```

### Entity Cache Access

```csharp
// Access cached entities
var guild = client.Cache.GetGuild(guildId);
var channel = client.Cache.GetChannel(channelId);
var user = client.Cache.GetUser(userId);
var member = client.Cache.GetGuildMember(guildId, userId);
```

### Interaction Handling

Requires `PawSharp.Interactions`.

```csharp
using PawSharp.Gateway.Events;

client.Interactions.RegisterCommand("ping", async interaction =>
{
    await client.Interactions.RespondEphemeralAsync(interaction.Id, interaction.Token, "Pong!");
});

client.Interactions.RegisterComponent("confirm-button", async interaction =>
{
    await client.Interactions.RespondEphemeralAsync(interaction.Id, interaction.Token, "Confirmed.");
});
```

### Intent Validation

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;

// Configure intent validation mode
var options = new PawSharpOptions
{
    Token = "Bot YOUR_TOKEN",
    Intents = GatewayIntents.AllNonPrivileged,
    IntentValidation = IntentValidationMode.Strict // Throws on mismatch
};

// Or validate manually
var result = client.ValidateIntents(enabledIntents);
if (!result.IsValid)
{
    Console.WriteLine($"Missing intents: {result}");
}
```

## Extension Packages

PawSharp.Client provides a solid foundation, and additional functionality is available through extension packages:

### PawSharp.Commands - Prefix Command Framework

Add traditional prefix commands (e.g., `!ping`, `!help`) with attribute-based command definitions.

Requires `PawSharp.Commands`.

```bash
dotnet add package PawSharp.Commands
```

```csharp
using PawSharp.Commands;

var commands = client.UseCommands(prefix: "!");

public sealed class GeneralCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Check whether the bot is responsive")]
    public async Task PingAsync(CommandContext ctx)
        => await ctx.ReplyAsync("Pong!");
}

commands.RegisterModule(client, new GeneralCommands());
```

### PawSharp.Interactivity - User Interaction Helpers

Add pagination, waiters, and multi-step user input flows.

Requires `PawSharp.Interactivity`.

```bash
dotnet add package PawSharp.Interactivity
```

```csharp
using PawSharp.Interactivity.Extensions;

var interactivity = client.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2)
});

// Wait for a reaction
var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    await channel.SendMessageAsync("Thanks for confirming.");
}

// Use pagination for long outputs
var pages = new[] { "Page 1", "Page 2", "Page 3" };
await message.SendPaginatedMessageAsync(interactivity, pages);
```

### PawSharp.Voice - Voice Connectivity

Add voice channel connections and audio streaming for music bots, voice alerts, etc.

Requires `PawSharp.Voice`.

```bash
dotnet add package PawSharp.Voice
```

```csharp
using PawSharp.Voice;

var voice = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannelId);

await connection.SetSpeakingAsync(true);
await connection.SendAudioAsync(pcmBytes);
await connection.SetSpeakingAsync(false);

await connection.DisconnectAsync();
```

## Configuration Options

### PawSharpOptions

```csharp
using PawSharp.Core.Enums;
using PawSharp.Core.Models;

var options = new PawSharpOptions
{
    Token = "Bot YOUR_TOKEN",
    Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent,
    ApiVersion = 10,
    Shards = 0,
    ShardCount = 1,
    EnableCompression = false,
    MaxMissedHeartbeatAcks = 3,
    IntentValidation = IntentValidationMode.Warn,
    Cache = new PawSharpOptions.CacheOptions
    {
        MaxEmojisPerGuild = 100,
        MaxMessagesPerChannel = 100,
        MaxMembersPerGuild = 1000
    },
    Presence = new PawSharpOptions.PresenceOptions
    {
        Status = "online",
        ActivityName = "Playing with PawSharp",
        ActivityType = 0, // 0 = Playing
        StreamUrl = null
    }
};
```

### Builder Configuration

```csharp
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;

var client = new PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN")
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .WithApiVersion(10)
    .WithSharding(shardId: 0, totalShards: 1)
    .UseCompression()
    .UseConsoleLogging(LogLevel.Information)
    .UseMemoryCache()
    .WithPresence("Playing", status: "online")
    .Build();
```

## Typical Use Cases

- **Standalone Discord bots** - Simple bots using the builder pattern
- **Multi-service bot architectures** - ASP.NET Core applications with DI
- **Prefix command bots** - Traditional `!command` style bots (with PawSharp.Commands)
- **Slash command bots** - Modern Discord application commands (with PawSharp.Interactions)
- **Music bots** - Voice connectivity and audio streaming (with PawSharp.Voice)
- **Interactive bots** - Pagination, waiters, multi-step flows (with PawSharp.Interactivity)

## Package Architecture

PawSharp.Client is designed as a facade that composes lower-level PawSharp packages:

- **PawSharp.API** - REST API client for HTTP operations
- **PawSharp.Gateway** - WebSocket client for real-time events
- **PawSharp.Cache** - Entity caching with in-memory and custom providers
- **PawSharp.Interactions** - Slash command and component handling
- **PawSharp.Core** - Shared models, enums, and utilities

Extension packages add optional functionality:
- **PawSharp.Commands** - Prefix command framework (extends DiscordClient)
- **PawSharp.Interactivity** - User interaction helpers (extends DiscordClient)
- **PawSharp.Voice** - Voice connectivity (extends DiscordClient)

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)
- [PawSharp.Commands](../PawSharp.Commands/README.md)
- [PawSharp.Interactivity](../PawSharp.Interactivity/README.md)
- [PawSharp.Voice](../PawSharp.Voice/README.md)

## License

MIT. See [../../LICENSE](../../LICENSE).
