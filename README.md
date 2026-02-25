# PawSharp

A modern, stable Discord API wrapper for .NET 8.0. Production-ready with automatic reconnection, proper error handling, and comprehensive Discord API coverage.

**Current Version:** 0.6.0-alpha1
**Status:** Production-ready with advanced features - Gateway reliability, REST resiliency, caching, interactions, commands, voice support, and sharding fully implemented. Alpha13 adds typed message components, a fluent EmbedBuilder, new flags enums, and 20 additional REST endpoints. Complete documentation and examples available.

## Key Features

- **Sharding Support** - Multi-shard management with automatic reconnection, status monitoring, and event aggregation
- **Automatic Reconnection** - Exponential backoff with session resumption, handles network issues gracefully
- **Heartbeat Health Monitoring** - Detects zombie connections and reconnects automatically
- **Complete REST API** - All Discord endpoints (messages, channels, guilds, members, roles, webhooks, etc.)
- **Real-time Events** - WebSocket gateway with typed event handlers
- **Typed Error Handling** - Custom exception hierarchy, no null checks needed
- **Input Validation** - Catch mistakes before hitting Discord's API
- **Smart Caching** - In-memory with per-entity limits, LRU eviction, automatic TTL cleanup
- **Rate Limiting** - Automatic rate limit handling with proper bucket tracking
- **Dependency Injection** - First-class support for .NET DI container
- **Fully Async** - Modern async/await throughout with nullable reference types
- **Typed Components** - Fully typed message component hierarchy (buttons, select menus, text inputs) with polymorphic JSON deserialization
- **EmbedBuilder** - Fluent builder with Discord limit enforcement for constructing embeds

---

## Installation

**Requirements:** .NET 8.0 SDK or later

### NuGet Packages

Install the packages you need:

```bash
# Core functionality
dotnet add package PawSharp.Core

# REST API client
dotnet add package PawSharp.API

# Gateway and events
dotnet add package PawSharp.Gateway

# Caching providers
dotnet add package PawSharp.Cache

# Unified client (includes all above)
dotnet add package PawSharp.Client

# Command framework
dotnet add package PawSharp.Commands

# Slash commands and interactions
dotnet add package PawSharp.Interactions

# Interactivity (pagination, polls)
dotnet add package PawSharp.Interactivity

# Voice support (experimental)
dotnet add package PawSharp.Voice
```

### From Source

```bash
git clone <repository>
cd PawSharp
dotnet build
```

---

## 📁 Project Structure

PawSharp is organized into modular packages for flexibility:

### Core Packages
- **`PawSharp.Core`** - Base entities, enums, exceptions, interfaces, models, serialization, and validation
- **`PawSharp.API`** - REST API client with automatic rate limiting and error handling
- **`PawSharp.Gateway`** - WebSocket gateway client with event handling and reconnection logic
- **`PawSharp.Cache`** - Caching abstractions and providers (memory, Redis)
- **`PawSharp.Client`** - Unified client combining API, Gateway, and Cache functionality

### Extension Packages
- **`PawSharp.Commands`** - Traditional command framework with prefix-based commands
- **`PawSharp.Interactions`** - Slash commands, buttons, select menus, and interaction handling
- **`PawSharp.Interactivity`** - Interactive components like paginators and polls
- **`PawSharp.Voice`** - Voice connection and audio handling (experimental)

### Supporting Components
- **`src/`** - Source code for all packages
- **`tests/`** - Unit and integration tests
- **`examples/`** - Sample applications and usage patterns
- **`docs/`** - Documentation and guides
- **`tools/`** - Build and documentation tools
- **`nupkgs/`** - Published NuGet packages

---

## Quick Example

Here's a bot that responds to `!ping`:

```csharp
using PawSharp.Client;
using PawSharp.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(config => config.AddConsole());
services.AddSingleton(new PawSharpOptions 
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<DiscordClient>();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();

client.Gateway.OnMessageCreate += async message =>
{
    if (message.Author.Bot) return;
    
    if (message.Content == "!ping")
    {
        try
        {
            await client.Rest.CreateMessageAsync(message.ChannelId, new CreateMessageRequest
            {
                Content = "Pong!"
            });
        }
        catch (RateLimitException ex)
        {
            Console.WriteLine($"Rate limited, wait {ex.RetryAfter}s");
        }
        catch (DiscordApiException ex)
        {
            Console.WriteLine($"API error: {ex.Message}");
        }
    }
};

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

More examples in [examples/](examples/).

---

## What's Included

**Gateway Resilience:**
- Automatic reconnection with exponential backoff (1s, 2s, 4s, 8s, 16s maximum)
- Session resumption within 45 seconds of disconnect
- Heartbeat ACK tracking to detect unhealthy connections
- All 12 Discord gateway opcodes handled correctly
- State machine preventing invalid state transitions
- Maximum 10 reconnection attempts before giving up
- Events for monitoring connection state and reconnection attempts

**REST API:**
- Messages (create, edit, delete, fetch, reactions, pins)
- Channels (CRUD, permissions, webhooks, announcement follows)
- Guilds (info, members, roles, bans, audit logs, preview, widget, vanity URL, welcome screen, channel/role position reorder)
- Users and current user endpoints
- Interactions and slash commands
- Scheduled events
- Threads and thread management
- Auto-moderation
- Invites (get with counts/expiration, delete)
- Guild Templates (list, get, create from template, sync, modify, delete)

**Gateway Events:**
- Message create/update/delete
- Guild and member events
- Channel and role events
- Interaction create
- Thread events
- Connection state transitions
- Ready and resume events

**Caching:**
- In-memory and Redis distributed cache providers
- Automatic cache for entities with gateway synchronization
- Configurable per-type size limits (10K messages, 1K guilds, 5K users, etc.)
- LRU eviction when limits hit
- TTL-based cleanup and statistics tracking

**Error Handling:**
- `ValidationException` - bad input (invalid ID, text too long, etc.)
- `RateLimitException` - Discord rate limit with retry-after
- `DiscordApiException` - API error with status code
- `GatewayException` - WebSocket/connection issues
- `DeserializationException` - JSON parsing failed

---

## Architecture

```
PawSharp.Core
├── Entities (Guild, Channel, Message, User, components, etc.)
├── Enums (PermissionFlags, MessageType, MessageFlags, ChannelFlags, etc.)
├── Exceptions (custom exception hierarchy)
├── Validation (input validators)
├── Models (API request/response types)
├── Builders (EmbedBuilder)
└── Interfaces (IDiscordRestClient, etc.)

PawSharp.API
├── REST client (all Discord HTTP endpoints)
├── Rate limiting (per-route bucket tracking)
└── Request/response models

PawSharp.Cache
└── In-memory cache provider (with per-entity limits)

PawSharp.Gateway
├── WebSocket connection management
├── Event dispatch system
├── Heartbeat handling
└── Shard manager (coming Phase 2)

PawSharp.Interactions
└── Slash command and component builders

PawSharp.Client
├── High-level Discord client
└── Event handlers
```

Use the pieces you need. Mix and match.

---

## Error Handling

No more null checks. Everything throws typed exceptions:

```csharp
try 
{
    var message = await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
    {
        Content = userText
    });
    // message exists, no need to check
}
catch (ValidationException ex) when (ex.Message.Contains("2000"))
{
    Console.WriteLine("Text exceeds 2000 characters");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, retry in {ex.RetryAfter}s");
    await Task.Delay(ex.RetryAfter * 1000);
}
catch (DiscordApiException ex)
{
    Console.WriteLine($"Discord error {ex.StatusCode}: {ex.Message}");
}
```

---

## Dependency Injection

Designed to work with .NET's DI from the start:

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(new PawSharpOptions { Token = token });
services.AddSingleton<IDiscordRestClient, RestClient>();
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<DiscordClient>();
services.AddSingleton<GatewayClient>();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

---

## Interactions (Slash Commands & Components)

PawSharp supports Discord's modern interaction system:

```csharp
// Register slash commands
client.Interactions.RegisterCommand("ping", async interaction =>
{
    var response = new InteractionResponse
    {
        Type = (int)InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionCallbackData
        {
            Content = "Pong! 🏓"
        }
    };
    
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token, response);
});

// Register component handlers
client.Interactions.RegisterComponent("my_button", async interaction =>
{
    // Handle button clicks
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token, 
        new InteractionResponse { /* ... */ });
});
```

---

## Voice Support

PawSharp includes comprehensive voice channel connectivity with professional-grade audio processing:

```csharp
using PawSharp.Voice;

// Connect to a voice channel
var voice = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannel);

// Start capturing from microphone and sending audio
connection.StartCapture();

// Play received audio through speakers
await connection.PlayAudioAsync(receivedAudioData);

// Clean up when done
await connection.DisconnectAsync();
```

**Voice Features:**
- WebSocket-based voice connections with Discord's voice gateway
- Real-time microphone capture and speaker playback infrastructure
- Voice state management and automatic server update handling
- Audio processing framework ready for codec integration
- Thread-safe voice operations with comprehensive error handling
- Opus codec preparation ( Concentus library included, encoding/decoding framework in place )

---

## Interactivity Framework

Build engaging interactive experiences with reactions, pagination, and user input collection:

```csharp
using PawSharp.Interactivity.Extensions;

// Enable interactivity on your client
var interactivity = client.UseInteractivity();

// Create paginated content for long messages
var pages = interactivity.GeneratePagesInEmbed(longText);
await channel.SendPaginatedMessageAsync(user, pages);

// Wait for a specific user reaction
var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    await message.RespondAsync("Thanks for the thumbs up!");
}

// Collect multiple reactions for polls
var reactions = await message.CollectReactionsAsync(client, TimeSpan.FromMinutes(5));

// Create interactive polls
await message.CreatePollAsync("What's your favorite programming language?",
    new[] { "C#", "Python", "JavaScript", "Rust" });
```

**Interactivity Features:**
- Reaction waiting and collection with timeout support
- Automatic pagination for large content
- Poll creation with reaction-based voting
- Message-based user input collection
- Built-in cancellation and error handling

---

## Commands Framework

Traditional message-based commands with clean, attribute-driven syntax:

```csharp
using PawSharp.Commands;

// Enable commands with your preferred prefix
var commands = client.UseCommands("!");

// Create a command module
public class UtilityCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Check if the bot is responsive")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync($"🏓 Pong! Latency: {DateTimeOffset.Now - ctx.Message.Timestamp:hh\\:mm\\:ss}");
    }

    [Command("userinfo")]
    [Description("Get information about a user")]
    public async Task UserInfoAsync(CommandContext ctx)
    {
        var user = ctx.Message.Author;
        var embed = new Embed
        {
            Title = $"{user.Username}#{user.Discriminator}",
            Fields = new List<EmbedField>
            {
                new() { Name = "ID", Value = user.Id.ToString(), Inline = true },
                new() { Name = "Joined", Value = user.CreatedAt.ToString("R"), Inline = true }
            }
        };
        await ctx.RespondAsync(embed);
    }

    [Command("say")]
    [Description("Make the bot say something")]
    public async Task SayAsync(CommandContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.RawArguments))
        {
            await ctx.RespondAsync("❌ What do you want me to say?");
            return;
        }
        await ctx.RespondAsync(ctx.RawArguments);
    }
}

// Register your command modules
commands.RegisterModule(new UtilityCommands());
```

**Commands Features:**
- Clean attribute-based command registration
- Automatic argument parsing and validation
- Built-in help system support
- Command aliases and descriptions
- Per-command execution hooks (before/after)
- Guild and channel context awareness

---

## What's Not Implemented Yet

- Distributed clustering (single-machine and sharded bots fully supported)
- Advanced voice features (basic voice connectivity implemented)

See [ROADMAP.md](ROADMAP.md) for the development plan and future phases.

---

## Contributing

Help wanted. Check the [ROADMAP.md](ROADMAP.md) for what we're building.

---

## Resources

- [Discord Developer Portal](https://discord.com/developers/applications) — get your bot token
- [Discord API Documentation](https://discord.com/developers/docs/intro) — the source of truth
- [Examples](examples/) — real code samples

---

## License

MIT License. Do what you want with it.
