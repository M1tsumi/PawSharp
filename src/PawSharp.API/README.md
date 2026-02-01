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
dotnet add package PawSharp.API --version 0.5.0-alpha10
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

## 📋 API Reference

PawSharp.API provides comprehensive coverage of Discord's REST API v10. Below is a detailed breakdown of all supported endpoints, organized by category.

### 🔐 Authentication & Users
- `GetCurrentUserAsync()` - Get current bot user information
- `GetUserAsync(ulong userId)` - Get user by ID
- `ModifyCurrentUserAsync(string? username, string? avatar)` - Modify current user
- `GetCurrentUserGuildsAsync(int limit, ulong? before, ulong? after)` - Get user's guilds
- `LeaveGuildAsync(ulong guildId)` - Leave a guild

### 🏰 Guilds
- `GetGuildAsync(ulong guildId, bool withCounts)` - Get guild details
- `CreateGuildAsync(CreateGuildRequest request)` - Create a new guild
- `ModifyGuildAsync(ulong guildId, ModifyGuildRequest request)` - Modify guild
- `DeleteGuildAsync(ulong guildId)` - Delete guild
- `GetGuildChannelsAsync(ulong guildId)` - Get guild channels
- `GetGuildMembersAsync(ulong guildId, int limit)` - Get guild members
- `GetGuildMemberAsync(ulong guildId, ulong userId)` - Get specific member
- `AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request)` - Add member
- `ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request)` - Modify member
- `RemoveGuildMemberAsync(ulong guildId, ulong userId)` - Remove member
- `GetGuildBansAsync(ulong guildId)` - Get banned users
- `GetGuildBanAsync(ulong guildId, ulong userId)` - Get ban details
- `CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays, string? reason)` - Ban user
- `RemoveGuildBanAsync(ulong guildId, ulong userId)` - Unban user

### 👥 Guild Roles
- `GetGuildRolesAsync(ulong guildId)` - Get all guild roles
- `CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request)` - Create role
- `ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request)` - Modify role
- `DeleteGuildRoleAsync(ulong guildId, ulong roleId)` - Delete role
- `AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)` - Add role to member
- `RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)` - Remove role from member

### 💬 Channels
- `GetChannelAsync(ulong channelId)` - Get channel details
- `ModifyChannelAsync(ulong channelId, ModifyChannelRequest request)` - Modify channel
- `DeleteChannelAsync(ulong channelId)` - Delete channel
- `CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request)` - Create channel
- `GetChannelInvitesAsync(ulong channelId)` - Get channel invites
- `CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request)` - Create invite
- `DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId)` - Delete permission

### 📝 Messages
- `CreateMessageAsync(ulong channelId, CreateMessageRequest request)` - Send message
- `GetMessageAsync(ulong channelId, ulong messageId)` - Get message
- `EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request)` - Edit message
- `DeleteMessageAsync(ulong channelId, ulong messageId)` - Delete message
- `GetChannelMessagesAsync(ulong channelId, int limit, ulong? around, ulong? before, ulong? after)` - Get messages
- `BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds)` - Bulk delete
- `PinMessageAsync(ulong channelId, ulong messageId)` - Pin message
- `UnpinMessageAsync(ulong channelId, ulong messageId)` - Unpin message
- `GetPinnedMessagesAsync(ulong channelId)` - Get pinned messages
- `TriggerTypingIndicatorAsync(ulong channelId)` - Trigger typing indicator

### 😊 Reactions
- `CreateReactionAsync(ulong channelId, ulong messageId, string emoji)` - Add reaction
- `DeleteOwnReactionAsync(ulong channelId, ulong messageId, string emoji)` - Remove own reaction
- `DeleteUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId)` - Remove user reaction

### ⚡ Interactions
- `CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response)` - Respond to interaction
- `EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)` - Edit response
- `DeleteOriginalInteractionResponseAsync(string applicationId, string interactionToken)` - Delete response

### 🛠️ Application Commands
- `GetGlobalApplicationCommandsAsync(ulong applicationId)` - Get global commands
- `CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request)` - Create global command
- `GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)` - Get global command
- `EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request)` - Edit global command
- `DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)` - Delete global command
- `GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId)` - Get guild commands
- `CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request)` - Create guild command
- `GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)` - Get guild command
- `EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request)` - Edit guild command
- `DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)` - Delete guild command
- `BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands)` - Bulk overwrite global
- `BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands)` - Bulk overwrite guild

### 🔑 Application Command Permissions
- `GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId)` - Get permissions
- `GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId)` - Get command permissions
- `EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions)` - Edit permissions
- `BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions)` - Batch edit

### 🧵 Threads
- `CreateThreadAsync(ulong channelId, CreateThreadRequest request)` - Create thread
- `CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request)` - Create thread from message
- `CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request)` - Create forum thread
- `JoinThreadAsync(ulong channelId)` - Join thread
- `AddThreadMemberAsync(ulong channelId, ulong userId)` - Add member to thread
- `LeaveThreadAsync(ulong channelId)` - Leave thread
- `RemoveThreadMemberAsync(ulong channelId, ulong userId)` - Remove member from thread
- `GetThreadMemberAsync(ulong channelId, ulong userId)` - Get thread member
- `GetThreadMembersAsync(ulong channelId)` - Get thread members
- `GetActiveThreadsAsync(ulong guildId)` - Get active threads
- `GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before, int? limit)` - Get public archived threads
- `GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before, int? limit)` - Get private archived threads
- `GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before, int? limit)` - Get joined private archived threads

### 🪝 Webhooks
- `CreateWebhookAsync(ulong channelId, CreateWebhookRequest request)` - Create webhook
- `GetChannelWebhooksAsync(ulong channelId)` - Get channel webhooks
- `GetGuildWebhooksAsync(ulong guildId)` - Get guild webhooks
- `GetWebhookAsync(ulong webhookId)` - Get webhook
- `GetWebhookWithTokenAsync(ulong webhookId, string token)` - Get webhook with token
- `ModifyWebhookAsync(ulong webhookId, ModifyWebhookRequest request)` - Modify webhook
- `ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request)` - Modify webhook with token
- `DeleteWebhookAsync(ulong webhookId)` - Delete webhook
- `DeleteWebhookWithTokenAsync(ulong webhookId, string token)` - Delete webhook with token
- `ExecuteWebhookAsync(ulong webhookId, string token, ExecuteWebhookRequest request, ulong? threadId)` - Execute webhook

### 📅 Scheduled Events
- `CreateGuildScheduledEventAsync(ulong guildId, CreateGuildScheduledEventRequest request)` - Create event
- `GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount)` - Get events
- `GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount)` - Get event
- `ModifyGuildScheduledEventAsync(ulong guildId, ulong eventId, ModifyGuildScheduledEventRequest request)` - Modify event
- `DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId)` - Delete event
- `GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit, bool? withMember, ulong? before, ulong? after)` - Get event users

### 📋 Audit Logs
- `GetGuildAuditLogsAsync(ulong guildId, ulong? userId, AuditLogEvent? actionType, ulong? before, int? limit)` - Get audit logs

### 🤖 Auto Moderation
- `ListAutoModerationRulesAsync(ulong guildId)` - List rules
- `GetAutoModerationRuleAsync(ulong guildId, ulong ruleId)` - Get rule
- `CreateAutoModerationRuleAsync(ulong guildId, CreateAutoModerationRuleRequest request)` - Create rule
- `ModifyAutoModerationRuleAsync(ulong guildId, ulong ruleId, ModifyAutoModerationRuleRequest request)` - Modify rule
- `DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId)` - Delete rule

### 🔧 Low-Level HTTP Methods
- `GetAsync(string endpoint)` - Raw GET request
- `PostAsync(string endpoint, HttpContent content)` - Raw POST request
- `PutAsync(string endpoint, HttpContent content)` - Raw PUT request
- `DeleteAsync(string endpoint)` - Raw DELETE request
- `PatchAsync(string endpoint, HttpContent content)` - Raw PATCH request

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