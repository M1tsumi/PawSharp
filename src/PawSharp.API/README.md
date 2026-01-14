# PawSharp.API

REST API client for Discord with automatic rate limiting and error handling.

PawSharp.API provides a complete, production-ready REST client for Discord's API v10. Built on .NET 8.0 with modern async patterns, comprehensive error handling, and intelligent rate limiting that just works.

## Features

- Complete API coverage for Discord API v10
- Automatic bucket management with zero-config setup
- Configurable timeouts with cancellation support
- Automatic X-Audit-Log-Reason headers
- Automatic retries with exponential backoff
- Smart caching prevents duplicate requests
- Built-in performance tracking
- First-class DI container support

## 📦 Installation

```bash
dotnet add package PawSharp.API --version 0.5.0-alpha9
```

## 🚀 Quick Start

```csharp
using PawSharp.API.Clients;
using PawSharp.Core.Entities;

// Create the REST client
var options = new PawSharpOptions
{
    Token = "your-bot-token-here"
};

var restClient = new DiscordRestClient(options);

// Get current user
User user = await restClient.GetCurrentUserAsync();
Console.WriteLine($"Logged in as: {user.Username}");

// Send a message
var message = await restClient.CreateMessageAsync(channelId, "Hello, Discord!");
```

## 📋 API Coverage

### Core Endpoints
- ✅ **Users** - Get, modify current user
- ✅ **Guilds** - Create, get, modify, delete guilds
- ✅ **Channels** - All channel operations (text, voice, DM)
- ✅ **Messages** - Send, edit, delete, bulk operations
- ✅ **Members** - Guild member management
- ✅ **Roles** - Role creation and management
- ✅ **Emojis** - Custom emoji handling
- ✅ **Webhooks** - Webhook CRUD operations

### Advanced Features
- ✅ **Application Commands** - Slash commands and permissions
- ✅ **Interactions** - Interaction responses and followups
- ✅ **Audit Logs** - Guild audit log retrieval
- ✅ **Invites** - Invite management
- ✅ **Voice** - Voice region and state management

## 🔧 Configuration

```csharp
var options = new PawSharpOptions
{
    Token = "your-bot-token",
    UserAgent = "MyBot/1.0.0",
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 3,
    EnableCompression = true
};

var restClient = new DiscordRestClient(options);
```

## 📖 Usage Examples

### Rate Limiting (Automatic)

```csharp
// Rate limiting happens automatically - no code changes needed!
for (int i = 0; i < 100; i++)
{
    await restClient.CreateMessageAsync(channelId, $"Message {i}");
    // PawSharp handles rate limits transparently
}
```

### Error Handling

```csharp
try
{
    var message = await restClient.CreateMessageAsync(channelId, "Hello!");
}
catch (DiscordRateLimitException ex)
{
    Console.WriteLine($"Rate limited! Retry after: {ex.RetryAfter}");
}
catch (DiscordForbiddenException ex)
{
    Console.WriteLine("Missing permissions!");
}
catch (DiscordNotFoundException ex)
{
    Console.WriteLine("Channel not found!");
}
```

### Audit Logs

```csharp
// Automatic audit log reason support
await restClient.ModifyGuildAsync(guildId, new GuildUpdateModel
{
    Name = "New Guild Name"
}, reason: "Server rebranding");
```

### Application Commands

```csharp
// Create a slash command
var command = await restClient.CreateGuildApplicationCommandAsync(guildId, new ApplicationCommand
{
    Name = "ping",
    Description = "Responds with pong!",
    Type = ApplicationCommandType.ChatInput
});

// Get permissions
var permissions = await restClient.GetGuildApplicationCommandPermissionsAsync(guildId, command.Id);
```

## 🔄 Dependency Injection

```csharp
// Register with DI container
services.AddPawSharp(options => {
    options.Token = configuration["Discord:Token"];
});

// Inject into your services
public class MyService
{
    private readonly IDiscordRestClient _restClient;

    public MyService(IDiscordRestClient restClient)
    {
        _restClient = restClient;
    }
}
```

## 📊 Monitoring & Metrics

```csharp
// Get performance metrics
var metrics = restClient.GetMetrics();
Console.WriteLine($"Requests: {metrics.TotalRequests}");
Console.WriteLine($"Rate Limits Hit: {metrics.RateLimitsHit}");
Console.WriteLine($"Average Response Time: {metrics.AverageResponseTime}ms");
```

## 🤝 Dependencies

- **PawSharp.Core** - Entity models and types
- **.NET 8.0** - Runtime requirements
- **Microsoft.Extensions.Http** - HTTP client factory
- **Microsoft.Extensions.Logging** - Structured logging

## 📚 Related Packages

- **[PawSharp.Core](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Core)** - Entity models
- **[PawSharp.Gateway](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Gateway)** - WebSocket gateway
- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - Combined client

## 🐛 Error Handling

PawSharp.API provides comprehensive error handling:

```csharp
// All exceptions inherit from DiscordException
catch (DiscordException ex)
{
    switch (ex.ErrorCode)
    {
        case 10003: // Unknown Channel
            // Handle missing channel
            break;
        case 50013: // Missing Permissions
            // Handle permission issues
            break;
    }
}
```

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.