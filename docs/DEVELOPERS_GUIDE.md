# PawSharp Developer Documentation

Welcome to PawSharp! This documentation will guide you through building Discord bots with .NET 10.0+.

## Table of Contents

1. [Installation & Setup](#installation--setup)
2. [Your First Bot](#your-first-bot)
3. [Configuration](#configuration)
4. [Core Concepts](#core-concepts)
5. [Working with REST API](#working-with-rest-api)
6. [Handling Events](#handling-events)
7. [Creating Commands](#creating-commands)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

---

## Installation & Setup

### Prerequisites

- **.NET 10.0 SDK** or later ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- A Discord bot token ([create application](https://discord.com/developers/applications))
- Basic C# knowledge

### Create a New Project

```bash
# Create a new console application
dotnet new console -n MyDiscordBot
cd MyDiscordBot

# Add PawSharp packages
dotnet add package PawSharp.Client
dotnet add package PawSharp.Commands
dotnet add package PawSharp.Interactions
dotnet add package Microsoft.Extensions.Logging.Console
```

### Minimal Package Setup

If you only need specific features:

```bash
# Just REST API (no events)
dotnet add package PawSharp.API

# Just Gateway (no REST)
dotnet add package PawSharp.Gateway

# Unified client (REST + Gateway + Cache)
dotnet add package PawSharp.Client

# With caching
dotnet add package PawSharp.Cache

# With Redis distributed cache
dotnet add package StackExchange.Redis
```

---

## Your First Bot

Here's a complete, working bot in under 50 lines:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Models;
using PawSharp.Gateway.Events;

// 1. Configure services
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton(new PawSharpOptions
    {
        Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") 
            ?? throw new InvalidOperationException("Set DISCORD_TOKEN env var"),
        Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged 
            | PawSharp.Core.Enums.GatewayIntents.MessageContent,
        ApiVersion = 10,
    })
    .AddPawSharp(); // Registers all services

// 2. Build and get client
var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();

// 3. Subscribe to events
client.OnReady(ready =>
{
    Console.WriteLine($"✅ Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

client.OnMessageCreated(msg =>
{
    if (msg.Content == "!ping")
        return client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "🏓 Pong!",
        });
    return Task.CompletedTask;
});

// 4. Connect and run
Console.WriteLine("🚀 Starting bot...");
await client.ConnectAsync();

// Keep the bot running
Console.WriteLine("✅ Bot is running. Press Ctrl+C to stop.");
await Task.Delay(Timeout.Infinite);
```

**Set your bot token:**
```bash
# Linux/macOS
export DISCORD_TOKEN=your_token_here

# Windows PowerShell
$env:DISCORD_TOKEN="your_token_here"

# Run the bot
dotnet run
```

---

## Non-DI Quickstart (PawSharpClientBuilder)

If you prefer a lightweight, non-DI approach (for small scripts or quick tests), `PawSharpClientBuilder` constructs a fully-wired `DiscordClient` without Microsoft DI:

```csharp
var client = new PawSharp.Client.PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN_HERE")
    .WithIntents(PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged | PawSharp.Core.Enums.GatewayIntents.MessageContent)
    .UseConsoleLogging(Microsoft.Extensions.Logging.LogLevel.Information)
    .UseMemoryCache() // in-memory cache (MemoryCacheProvider)
    .Build();

client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

client.OnMessageCreated(async msg =>
{
    if (!msg.Author.IsBot && msg.Content == "!hello")
        await client.SendMessageAsync(msg.ChannelId, $"Hello, {msg.Author.Username}!");
});

await client.ConnectAsync();
```

`PawSharpClientBuilder` exposes the same configuration options as DI wiring: token, intents, API version, sharding, compression, logging, HTTP client and cache injection (`UseCache(IEntityCache)` / `UseMemoryCache()`).


## Configuration

### PawSharpOptions

All configuration happens through `PawSharpOptions`:

```csharp
var options = new PawSharpOptions
{
    // Bot Configuration
    Token = "your-bot-token",
    ApiVersion = 10,  // Discord API version
    
    // Gateway Configuration
    Intents = GatewayIntents.AllNonPrivileged 
        | GatewayIntents.MessageContent,  // Subscribe to events
    
    // Sharding (for large bots)
    Shards = ShardingStrategy.Auto,  // Auto-calculate or manual
    TotalShards = 2,  // If manual
    ShardId = 0,      // If manual
    
    // Reconnection Strategy
    ReconnectTimeout = TimeSpan.FromSeconds(1),  // Initial backoff
    MaxReconnectAttempts = 5,  // Max attempts before fail
    
    // Caching
    CacheSettings = new CacheSettings
    {
        MaxCachedGuilds = 1000,
        MaxCachedChannelsPerGuild = 100,
        MaxCachedMessages = 10000,
        MessageCacheTTL = TimeSpan.FromHours(1),
    },
};
```

### Gateway Intents

Intents control which events you receive. Request only what you need:

```csharp
// Recommended for most bots
var intents = GatewayIntents.AllNonPrivileged 
    | GatewayIntents.MessageContent;

// Only specific intents
var intents = GatewayIntents.Guilds 
    | GatewayIntents.DirectMessages
    | GatewayIntents.GuildMessages;

// Available Intents:
// - Guilds, GuildMembers, GuildBans, GuildEmojis, etc.
// - DirectMessages, DirectMessageReactions, DirectMessageTyping
// - GuildMessages, GuildMessageReactions, GuildMessageTyping
// - MessageContent (required to read message content)
```

### Dependency Injection Setup

```csharp
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add PawSharp with default in-memory cache
services.AddSingleton(options);
services.AddPawSharp();  // Registers all services

// Or with custom cache
services.AddSingleton(options);
services.AddPawSharp(cache: new MemoryCacheProvider());

// Or with Redis
services.AddSingleton(options);
services.AddPawSharp(cache: new RedisCacheProvider("localhost:6379"));

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

---

## Core Concepts

### The DiscordClient

Your main entry point for everything:

```csharp
var client = provider.GetRequiredService<DiscordClient>();

// Access different components
var restClient = client.Rest;        // Send API requests
var gatewayClient = client.Gateway;  // Listen to events
var cache = client.Cache;            // Access cached data
var interactions = client.Interactions;  // Handle slash commands
```

### REST vs Gateway

| Task | Use |
|------|-----|
| Send messages | `client.Rest` |
| Create channels | `client.Rest` |
| Ban users | `client.Rest` |
| Listen to messages | `client.OnMessageCreated(handler)` |
| Listen to member joins | `client.OnGuildMemberJoined(handler)` |
| Get real-time data | `client.OnXxx(handler)` or `client.Gateway.Events.On<T>(name, handler)` |

### Snowflakes (IDs)

Discord uses 64-bit unsigned integers for all IDs:

```csharp
ulong guildId = 123456789;
ulong channelId = 987654321;
ulong userId = 111111111;

// From Discord UI, copy as integer
// IDs are always `ulong`
```

### Async/Await Throughout

**All I/O operations are async:**

```csharp
// ❌ Wrong
var message = await client.Rest.CreateMessageAsync(channelId, request);
DoSomethingWithMessage(message);  // Blocks if this is synchronous

// ✅ Correct
var message = await client.Rest.CreateMessageAsync(channelId, request);
await DoSomethingAsync(message);  // Async all the way

// ✅ Also correct
await foreach (var msg in GetMessagesAsync(channelId))
{
    await ProcessMessageAsync(msg);
}
```

---

## Working with REST API

### Sending Messages

```csharp
using PawSharp.API.Models;

// Simple text message
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Hello, world!",
});

// With embed
var embed = new Embed
{
    Title = "My Embed",
    Description = "This is an embed",
    Color = 0xFF5733,  // RGB as hex
    Fields = new List<EmbedField>
    {
        new() { Name = "Field 1", Value = "Value 1", Inline = true },
        new() { Name = "Field 2", Value = "Value 2", Inline = true },
    },
};

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Check out this embed:",
    Embeds = new List<Embed> { embed },
});

// Multiple embeds
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Embeds = new List<Embed>
    {
        embed1, embed2, embed3,  // Up to 10
    },
});
```

### Getting Messages

```csharp
// Get message history (most recent first)
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId: channelId,
    limit: 50);  // 1-100

// With pagination
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50,
    before: oldestMessageId  // Get older messages
);

// Around a message
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50,
    around: messageId  // Get messages around this ID
);

// Get specific message
var message = await client.Rest.GetMessageAsync(channelId, messageId);
```

### Editing & Deleting

```csharp
// Edit message
await client.Rest.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
    Content = "Updated content",
});

// Delete message
await client.Rest.DeleteMessageAsync(channelId, messageId);

// Bulk delete (2-100 messages)
await client.Rest.BulkDeleteMessagesAsync(channelId, new List<ulong>
{
    messageId1, messageId2, messageId3,
});

// Pin/Unpin
await client.Rest.PinMessageAsync(channelId, messageId);
await client.Rest.UnpinMessageAsync(channelId, messageId);
```

### Guild Management

```csharp
// Get guild info
var guild = await client.Rest.GetGuildAsync(guildId);
Console.WriteLine($"Guild: {guild.Name} ({guild.MemberCount} members)");

// List members
var members = await client.Rest.GetGuildMembersAsync(guildId, limit: 1000);
foreach (var member in members)
{
    Console.WriteLine($"{member.User.Username}: {member.Nickname}");
}

// Get specific member
var member = await client.Rest.GetGuildMemberAsync(guildId, userId);

// Edit member
await client.Rest.ModifyGuildMemberAsync(guildId, userId, new ModifyGuildMemberRequest
{
    Nickname = "NewNickname",
    RoleIds = new List<ulong> { roleId1, roleId2 },
});

// Kick member
await client.Rest.RemoveGuildMemberAsync(guildId, userId);
```

### Role Management

```csharp
// List roles
var roles = await client.Rest.GetGuildRolesAsync(guildId);
foreach (var role in roles)
{
    Console.WriteLine($"@{role.Name}: {role.Permissions}");
}

// Create role
var role = await client.Rest.CreateGuildRoleAsync(guildId, new CreateRoleRequest
{
    Name = "Moderator",
    Color = 0xFF0000,  // Red
    Permissions = 0x8000,  // Administrator
    Mentionable = true,
});

// Edit role
await client.Rest.ModifyGuildRoleAsync(guildId, roleId, new ModifyRoleRequest
{
    Name = "Senior Mod",
    Color = 0x00FF00,
});

// Assign role to member
await client.Rest.AddGuildMemberRoleAsync(guildId, userId, roleId);

// Remove role from member
await client.Rest.RemoveGuildMemberRoleAsync(guildId, userId, roleId);

// Delete role
await client.Rest.DeleteGuildRoleAsync(guildId, roleId);
```

### Channels

```csharp
// Get channel
var channel = await client.Rest.GetChannelAsync(channelId);
Console.WriteLine($"#{channel.Name} (type: {channel.Type})");

// Create channel
var newChannel = await client.Rest.CreateGuildChannelAsync(guildId, new CreateChannelRequest
{
    Name = "general",
    Type = ChannelType.GuildText,
    Topic = "General discussion",
});

// Edit channel
await client.Rest.ModifyChannelAsync(channelId, new ModifyChannelRequest
{
    Name = "announcements",
    Topic = "Important announcements",
});

// Delete channel
await client.Rest.DeleteChannelAsync(channelId);
```

### Bans

```csharp
// Ban user
await client.Rest.CreateGuildBanAsync(
    guildId, 
    userId,
    deleteMessageDays: 7,  // Delete last 7 days of messages
    reason: "Spam"
);

// Get bans
var bans = await client.Rest.GetGuildBansAsync(guildId);
foreach (var ban in bans)
{
    Console.WriteLine($"Banned: {ban.User.Username} (reason: {ban.Reason})");
}

// Unban
await client.Rest.RemoveGuildBanAsync(guildId, userId);
```

### Reactions

```csharp
// Add reaction
await client.Rest.CreateReactionAsync(
    channelId,
    messageId,
    emoji: "👍"  // Unicode or "custom:id"
);

// Remove your reaction
await client.Rest.DeleteOwnReactionAsync(channelId, messageId, "👍");

// Remove someone else's reaction (mod permission)
await client.Rest.DeleteUserReactionAsync(channelId, messageId, "👍", userId);
```

---

## Handling Events

### Subscribing to Events

PawSharp provides convenience methods on `DiscordClient` for every major event. Each returns `IDisposable` — dispose it to unsubscribe:

```csharp
// Lambda subscription (most common)
client.OnMessageCreated(async msg =>
{
    if (msg.Content == "!hello")
        await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Hello!" });
});

// Multiple events
client.OnMessageCreated(HandleMessageAsync);
client.OnGuildAvailable(HandleGuildJoinAsync);
client.OnGuildMemberJoined(HandleMemberJoinAsync);

// Store subscription for later cleanup
IDisposable sub = client.OnGuildMemberJoined(HandleMemberJoinAsync);
// ...
sub.Dispose(); // stop receiving events
```

For events without a convenience wrapper, or for advanced control, use `client.Gateway.Events`:

```csharp
// Low-level dispatcher: requires both type AND event name string
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Session resumed after reconnect");
    return Task.CompletedTask;
});
```

### Common Events

**Connection events:**
```csharp
client.OnReady(ready =>
{
    Console.WriteLine($"Bot ready as {ready.User.Username}");
    return Task.CompletedTask;
});

// ResumedEvent has no convenience method; use low-level dispatcher
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Connection resumed");
    return Task.CompletedTask;
});
```

**Message events:**
```csharp
client.OnMessageCreated(msg =>
{
    if (!msg.Author.IsBot)
        Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
    return Task.CompletedTask;
});

client.OnMessageUpdated(msg =>
{
    Console.WriteLine($"Message edited: {msg.Id}");
    return Task.CompletedTask;
});

client.OnMessageDeleted(msg =>
{
    Console.WriteLine($"Message deleted: {msg.Id}");
    return Task.CompletedTask;
});
```

**Guild events:**
```csharp
client.OnGuildAvailable(guild =>
{
    Console.WriteLine($"Guild available: {guild.Name}");
    return Task.CompletedTask;
});

client.OnGuildUnavailable(guild =>
{
    Console.WriteLine($"Guild unavailable: {guild.Id}");
    return Task.CompletedTask;
});
```

**Member events:**
```csharp
client.OnGuildMemberJoined(member =>
{
    Console.WriteLine($"Welcome {member.User.Username}!");
    return Task.CompletedTask;
});

client.OnGuildMemberLeft(member =>
{
    Console.WriteLine($"{member.User.Username} left");
    return Task.CompletedTask;
});
```

**Role events:**
```csharp
client.OnRoleCreated(role =>
{
    Console.WriteLine($"Role created: @{role.Role.Name}");
    return Task.CompletedTask;
});

client.OnRoleUpdated(role =>
{
    Console.WriteLine($"Role updated: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

### Accessing Cached Data

```csharp
// Check if guild is in cache
var guild = await client.Cache.GetGuildAsync(guildId);
if (guild != null)
{
    Console.WriteLine($"Guild cached: {guild.Name}");
}

// Get cached messages
var messages = await client.Cache.GetChannelMessagesAsync(channelId, limit: 50);

// Get cached users
var user = await client.Cache.GetUserAsync(userId);
if (user != null)
{
    Console.WriteLine($"User: {user.Username}");
}
```

---

## Creating Commands

### Prefix Commands

Create a command module:

```csharp
using PawSharp.Commands;

public class ModerationCommands : BaseCommandModule
{
    private readonly IDiscordRestClient _rest;

    public ModerationCommands(IDiscordRestClient rest)
    {
        _rest = rest;
    }

    [Command("kick")]
    [Description("Kick a user from the server")]
    public async Task KickCommand(CommandContext ctx)
    {
        // ctx.Arguments[0] is the user ID string
        if (!ulong.TryParse(ctx.Arguments.ElementAtOrDefault(0), out var userId))
        {
            await ctx.RespondAsync("Usage: !kick <userId>");
            return;
        }

        if (ctx.GuildId is null) return;
        await _rest.RemoveGuildMemberAsync(ctx.GuildId.Value, userId);
        await ctx.RespondAsync("✅ User kicked.");
    }

    [Command("ban")]
    [Aliases("b")]
    [Description("Ban a user from the server")]
    public async Task BanCommand(CommandContext ctx)
    {
        if (!ulong.TryParse(ctx.Arguments.ElementAtOrDefault(0), out var userId))
        {
            await ctx.RespondAsync("Usage: !ban <userId>");
            return;
        }

        if (ctx.GuildId is null) return;
        await _rest.CreateGuildBanAsync(ctx.GuildId.Value, userId, reason: "Banned by moderator");
        await ctx.RespondAsync("✅ User banned.");
    }
}
```

Register commands:

```csharp
var commands = new CommandsExtension(prefix: "!");

// RegisterModule wires up MESSAGE_CREATE automatically
commands.RegisterModule(client, new ModerationCommands(client.Rest));

// Or async (allows module InitializeAsync to run first)
await commands.RegisterModuleAsync(client, new ModerationCommands(client.Rest));

// List all registered commands
foreach (var info in commands.GetRegisteredCommands())
    Console.WriteLine($"{info.Name}: {info.Description}");
```

### Slash Commands

Register slash commands:

```csharp
var appId = client.CurrentUser.Id;

await client.Rest.CreateGlobalApplicationCommandAsync(
    appId,
    new CreateApplicationCommandRequest
    {
        Name = "ping",
        Description = "Responds with pong",
        Type = ApplicationCommandType.ChatInput,
    }
);
```

Handle slash commands:

```csharp
// client.Interactions automatically routes interactions registered via RegisterCommand()
client.Interactions.RegisterCommand("ping", async interaction =>
{
    await client.Rest.CreateInteractionResponseAsync(
        interaction.Id,
        interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = "🏓 Pong!" },
        }
    );
});
```

---

## Error Handling

### Exception Types

PawSharp throws specific exceptions you can catch:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)
{
    // Input validation failed
    Console.WriteLine($"Invalid input: {ex.Message}");
}
catch (RateLimitException ex)
{
    // Hit Discord's rate limits
    Console.WriteLine($"Rate limited. Retry after: {ex.RetryAfter}");
}
catch (DiscordApiException ex)
{
    // Discord API returned an error
    Console.WriteLine($"API Error ({ex.StatusCode}): {ex.Message}");
}
catch (GatewayException ex)
{
    // WebSocket connection issue
    Console.WriteLine($"Gateway error: {ex.Message}");
}
catch (Exception ex)
{
    // Unexpected error
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Validation

Validation happens before sending requests:

```csharp
// ❌ This throws ValidationException
try
{
    await client.Rest.GetChannelMessagesAsync(
        channelId,
        limit: 500  // Max is 100
    );
}
catch (ValidationException ex)
{
    Console.WriteLine(ex.Message);  // "Limit must be between 1 and 100"
}

// ✅ This works
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 100  // Valid
);
```

---

## Best Practices

### 1. Environment Variables

Never hardcode tokens:

```csharp
// ✅ Correct
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("DISCORD_TOKEN not set");

var options = new PawSharpOptions { Token = token };

// ❌ Wrong
var options = new PawSharpOptions { Token = "MjM4NDk1NzQ..." };
```

### 2. Proper Async/Await

```csharp
// ❌ Wrong - blocks the thread
Task.Run(() => ProcessMessage(msg)).Wait();

// ✅ Correct - async all the way
await ProcessMessageAsync(msg);

// ❌ Wrong - fire and forget
_ = ProcessMessageAsync(msg);

// ✅ Correct - wait for completion
await ProcessMessageAsync(msg);
```

### 3. Middleware for Pre-Event Processing

Middleware registered on the low-level dispatcher runs *before* all event handlers for every dispatched event. The signature is `Func<string eventName, object eventData, Task>` — there is no `next()` call; all handlers always execute after middleware.

```csharp
// Log every event
client.Gateway.Events.Use(async (eventName, eventData) =>
{
    Console.WriteLine($"[EVENT] {eventName}");
    await Task.CompletedTask;
});
```

### 4. Graceful Shutdown

```csharp
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += async (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Shutting down...");
    await client.DisconnectAsync();
    cts.Cancel();
};

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite, cts.Token);
```

### 5. Logging

```csharp
// Inject ILogger into your services
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task DoSomethingAsync()
    {
        _logger.LogInformation("Starting operation");
        try
        {
            // Do work
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed");
        }
    }
}
```

### 6. Scalability with Sharding

For bots in 2500+ servers:

```csharp
var options = new PawSharpOptions
{
    Token = token,
    Intents = GatewayIntents.AllNonPrivileged,
    
    // Auto-sharding
    Shards = ShardingStrategy.Auto,
    
    // Or manual
    // TotalShards = 4,
    // ShardId = 0,
};
```

With ShardManager:

```csharp
var shardManager = provider.GetRequiredService<ShardManager>();

// Connect all shards
await shardManager.ConnectAllAsync();

// Monitor shard status
var statuses = shardManager.GetAllShardStatuses();
foreach (var (shardId, status) in statuses)
{
    Console.WriteLine($"Shard {shardId}: {status}");
}
```

### 7. Using Redis for Large Bots

```csharp
// In production with many bots/shards
services.AddSingleton<IEntityCache>(sp =>
    new RedisCacheProvider("redis.example.com:6379,password=secretkey")
);

services.AddPawSharp();
```

### 8. Proper Resource Cleanup

```csharp
// Use dependency injection for proper cleanup
public class MyBot
{
    private readonly DiscordClient _client;
    private readonly ILogger<MyBot> _logger;

    public MyBot(DiscordClient client, ILogger<MyBot> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        try
        {
            await _client.ConnectAsync();
            _logger.LogInformation("Bot started");
            await Task.Delay(Timeout.Infinite);
        }
        finally
        {
            await _client.DisconnectAsync();
            _logger.LogInformation("Bot stopped");
        }
    }
}
```

---

## Troubleshooting

### Common Issues

**"Invalid token"**
```
❌ Make sure token is correct
❌ Ensure no extra whitespace
❌ Token should start with MzI...
```

**"Missing intents"**
```
❌ Did you request MessageContent intent?
❌ All required intents enabled in Discord developer portal?

✅ Enable in code:
var intents = GatewayIntents.AllNonPrivileged 
    | GatewayIntents.MessageContent;
```

**"Cannot read message content"**
```
❌ Did you enable MESSAGE_CONTENT intent?

In Discord Developer Portal:
1. Go to Bot settings
2. Scroll to "Privileged Gateway Intents"
3. Enable "Message Content Intent"
```

**"Rate limited"**
```
❌ Making too many requests too fast

✅ Implement request queuing:
var semaphore = new SemaphoreSlim(5, 5);

async Task RateLimitedRequest()
{
    await semaphore.WaitAsync();
    try
    {
        await client.Rest.CreateMessageAsync(...);
    }
    finally
    {
        semaphore.Release();
    }
}
```

**"Connection keeps dropping"**
```
✅ PawSharp auto-reconnects. Subscribe to OnReady to detect session restores:

client.OnReady(ready =>
{
    _logger.LogInformation("Bot session ready: {User}", ready.User.Username);
    return Task.CompletedTask;
});
```

### Debugging

Enable debug logging:

```csharp
services.AddLogging(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);  // Show debug messages
});
```

Check connection state:

```csharp
if (client.Gateway.IsConnected)
{
    Console.WriteLine("Connected");
}
else
{
    Console.WriteLine("Disconnected");
}
```

### Getting Help

1. Check [ERROR_HANDLING.md](./ERROR_HANDLING.md)
2. Review the documentation index at [INDEX.md](./INDEX.md)
3. Check examples in `examples/` folder
4. Open GitHub issue with:
   - Your code snippet
   - Error message
   - Stack trace
   - Steps to reproduce

---

## Next Steps

- ✅ [Working with REST API](./REST_API_GUIDE.md)
- ✅ [Gateway & Real-time Events](./GATEWAY_GUIDE.md)
- ✅ [Advanced Caching](./CACHING_GUIDE.md)
- ✅ [Common Patterns](./PATTERNS_GUIDE.md)
- ✅ [Troubleshooting](./TROUBLESHOOTING.md)

---

**Happy coding! 🎉**
