# PawSharp.Client

The complete, high-level Discord client that brings everything together.

PawSharp.Client is the main entry point for Discord bot development with PawSharp. It combines the REST API client, WebSocket gateway, caching, commands, interactions, and voice into a single, easy-to-use interface with full dependency injection support.

## Features

- Unified interface for REST, Gateway, and voice
- Modular extensions for commands, interactions, voice
- Built-in caching with automatic invalidation
- Typed event handling with middleware support
- Automatic reconnection and session resumption
- Flexible configuration options
- Async throughout
- First-class dependency injection support

## 📦 Installation

```bash
dotnet add package PawSharp.Client --version 6.1.0-alpha-1
```

## 🚀 Quick Start

### Basic Bot Setup

```csharp
using PawSharp.Client;

// Create and configure client
var client = new DiscordClient(new PawSharpOptions
{
    Token = "your-bot-token-here"
});

// Connect to Discord
await client.ConnectAsync();

// Your bot is now online!
await Task.Delay(-1); // Keep running
```

### Dependency Injection (Recommended)

```csharp
using Microsoft.Extensions.DependencyInjection;
using PawSharp.Client;

// Configure services
var services = new ServiceCollection();

services.AddPawSharp(options =>
{
    options.Token = configuration["Discord:Token"];
    options.EnableCompression = true;
    options.CacheOptions.MaxGuilds = 1000;
});

// Build provider
var provider = services.BuildServiceProvider();

// Get client
var client = provider.GetRequiredService<DiscordClient>();

// Connect
await client.ConnectAsync();
```

## 📋 Client Features

### REST API Access

```csharp
// Get current user
var user = await client.Rest.GetCurrentUserAsync();

// Send a message
await client.Rest.CreateMessageAsync(channelId, "Hello, world!");

// Get guild information
var guild = await client.Rest.GetGuildAsync(guildId);
```

### Gateway Events

```csharp
// Handle message creation
client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async evt =>
{
    if (evt.Content == "!ping")
    {
        await client.Rest.CreateMessageAsync(evt.ChannelId, "Pong!");
    }
});

// Handle guild member joins
client.Gateway.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", async evt =>
{
    Console.WriteLine($"New member: {evt.User.Username}");
});
```

### Caching

```csharp
// Get cached user (fast)
var user = await client.Cache.GetUserAsync(userId);

// Get cached guild with members
var guild = await client.Cache.GetGuildAsync(guildId, includeMembers: true);

// Cache statistics
var stats = client.Cache.GetStats();
Console.WriteLine($"Cached guilds: {stats.GuildCount}");
```

## 🔧 Extensions

### Commands Extension

```csharp
using PawSharp.Commands.Extensions;

// Enable commands
var commands = client.UseCommands("!");

// Create command module
public class FunCommands : BaseCommandModule
{
    [Command("ping")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }
}

// Register module
await commands.RegisterModuleAsync(client, new FunCommands());
```

### Interactions Extension

```csharp
using PawSharp.Interactions;

// Enable interactions
var interactions = client.UseInteractions();

// Handle slash commands
interactions.OnInteractionCreate += async (interaction) =>
{
    if (interaction.Type == InteractionType.ApplicationCommand)
    {
        await interaction.RespondAsync("Hello from slash command!");
    }
};
```

### Voice Extension

```csharp
using PawSharp.Voice;

// Enable voice
var voice = client.UseVoice();

// Connect to voice channel
var connection = await voice.ConnectAsync(voiceChannel);
connection.StartCapture();
```

## ⚙️ Configuration

```csharp
var options = new PawSharpOptions
{
    // Authentication
    Token = "your-bot-token",

    // Gateway
    Intents = GatewayIntents.All,
    EnableCompression = true,
    MaxMissedHeartbeatAcks = 3,

    // REST
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 3,

    // Caching
    CacheOptions = new CacheOptions
    {
        MaxGuilds = 1000,
        MaxChannels = 5000,
        MaxUsers = 10000,
        MaxMembersPerGuild = 1000
    },

    // Logging
    LogLevel = LogLevel.Information
};

var client = new DiscordClient(options);
```

## 📊 Monitoring & Health Checks

```csharp
// Connection status
Console.WriteLine($"Gateway: {client.Gateway.State}");
Console.WriteLine($"REST Healthy: {client.Rest.IsHealthy}");

// Cache statistics
var cacheStats = client.Cache.GetStats();
Console.WriteLine($"Cache Hit Rate: {cacheStats.HitRate:P}");

// Performance metrics
var metrics = client.GetMetrics();
Console.WriteLine($"Uptime: {metrics.Uptime}");
Console.WriteLine($"Events Processed: {metrics.EventsProcessed}");
```

## 🔄 Event Handling

### Basic Events

```csharp
client.Gateway.Events.On<ReadyEvent>("READY", async evt =>
{
    Console.WriteLine($"Bot ready! Logged in as {evt.User.Username}");
});
```

### Event Middleware

```csharp
// Add logging middleware
client.Gateway.Events.Use(async (evt, next) =>
{
    Console.WriteLine($"Event: {evt.Type}");
    await next();
});
```

### Typed Events

```csharp
client.Gateway.Events.On<MessageCreateEvent>(async evt =>
{
    // Strongly-typed event data
    Console.WriteLine($"Message: {evt.Content}");
});
```

## 🏗️ Architecture

```
PawSharp.Client
├── DiscordClient (main interface)
│   ├── Rest (IDiscordRestClient)
│   ├── Gateway (IGatewayClient)
│   ├── Cache (ICacheProvider)
│   └── Extensions (commands, voice, etc.)
├── PawSharpOptions (configuration)
├── ServiceCollectionExtensions (DI)
└── Health monitoring & metrics
```

## 🛠️ Advanced Usage

### Custom Extensions

```csharp
public static class MyExtensions
{
    public static MyService UseMyService(this DiscordClient client)
    {
        // Add custom functionality
        return new MyService(client);
    }
}

// Usage
var myService = client.UseMyService();
```

### Background Services

```csharp
// Implement background tasks
public class StatusUpdater : BackgroundService
{
    private readonly DiscordClient _client;

    public StatusUpdater(DiscordClient client)
    {
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _client.Gateway.UpdatePresenceAsync(new Presence
            {
                Status = UserStatus.Online,
                Activities = new[] { new Activity { Name = "with PawSharp!" } }
            });

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## 🤝 Dependencies

- **PawSharp.API** - REST API client
- **PawSharp.Gateway** - WebSocket gateway
- **PawSharp.Cache** - Caching provider
- **PawSharp.Core** - Entity models
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Logging** - Structured logging

## 📚 Related Packages

- **[PawSharp.Commands](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Commands)** - Command framework
- **[PawSharp.Interactions](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Interactions)** - Slash commands
- **[PawSharp.Voice](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Voice)** - Voice support

## 🐛 Error Handling

```csharp
try
{
    await client.ConnectAsync();
}
catch (DiscordAuthenticationException ex)
{
    Console.WriteLine("Invalid token!");
}
catch (DiscordGatewayException ex)
{
    Console.WriteLine($"Gateway error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.