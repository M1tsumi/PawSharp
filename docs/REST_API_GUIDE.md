# REST API Usage Guide

A comprehensive guide to using PawSharp's REST API for interacting with Discord.

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Messages](#messages)
3. [Channels](#channels)
4. [Guilds](#guilds)
5. [Members & Users](#members--users)
6. [Roles](#roles)
7. [Webhooks](#webhooks)
8. [Threads](#threads)
9. [Reactions & Components](#reactions--components)
10. [Advanced Patterns](#advanced-patterns)

---

## Core Concepts

## Quickstart — run a bot in two minutes

Two common ways to create a working `DiscordClient`: dependency-injection (`AddPawSharp`) or the lightweight `PawSharpClientBuilder` when you don't want to use DI.

### DI (recommended)

```csharp
var services = new ServiceCollection()
    .AddLogging(b => b.AddConsole())
    .AddSingleton(new PawSharp.Core.Models.PawSharpOptions
    {
        Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
        Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged | PawSharp.Core.Enums.GatewayIntents.MessageContent,
    })
    .AddPawSharp(); // Registers DiscordClient, REST, Gateway, Cache, Interactions

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<PawSharp.Client.DiscordClient>();

client.OnReady(_ => { Console.WriteLine("Ready"); return Task.CompletedTask; });
client.OnMessageCreated(async msg =>
{
    if (!msg.Author.IsBot && msg.Content == "!ping")
        await client.SendMessageAsync(msg.ChannelId, "🏓 Pong!"); // convenience helper
});

await client.ConnectAsync();
```

### Non-DI: `PawSharpClientBuilder`

```csharp
var client = new PawSharp.Client.PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN_HERE")
    .WithIntents(PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged | PawSharp.Core.Enums.GatewayIntents.MessageContent)
    .UseConsoleLogging(Microsoft.Extensions.Logging.LogLevel.Information)
    .UseMemoryCache() // default in-memory cache
    .Build();

client.OnMessageCreated(msg => Console.WriteLine($"[{msg.ChannelId}] {msg.Author?.Username}: {msg.Content}"));

await client.ConnectAsync();
```

Notes:
- `client.SendMessageAsync(channelId, string)` and `client.SendMessageAsync(channelId, CreateMessageRequest)` are convenience helpers on `DiscordClient` that call the underlying `client.Rest.CreateMessageAsync(...)`.
- Use `AddPawSharpWithMemoryCache` or supply a `IEntityCache` factory to `AddPawSharp(...)` when registering via DI.

### REST Client Access

```csharp
// Via DiscordClient
var restClient = client.Rest;

// Via dependency injection
public MyService(IDiscordRestClient rest)
{
    _rest = rest;
}
```

### Entity IDs (Snowflakes)

Discord uses 64-bit unsigned integers for all IDs:

```csharp
ulong channelId = 123456789;
ulong userId = 987654321;
ulong guildId = 111222333;

// Always use `ulong`
// Never string IDs unless working with tokens
```

### Audit Log Reasons

Many operations support audit log reasons:

```csharp
// Include reason - appears in audit logs
await client.Rest.RemoveGuildMemberAsync(
    guildId, 
    userId,
    reason: "Spam bot detection"
);

// Without reason
await client.Rest.RemoveGuildMemberAsync(guildId, userId);
```

### Validation

Input is validated before sending to Discord:

```csharp
try
{
    // Message content validated (max 2000 chars)
    await client.Rest.CreateMessageAsync(channelId, new()
    {
        Content = veryLongString,  // Throws if > 2000
    });
}
catch (ValidationException ex)
{
    Console.WriteLine($"Invalid input: {ex.Message}");
}
```

---

## Messages

### Sending Messages

**Simple text:**
```csharp
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Hello, Discord!",
});
```

**With embeds:**
```csharp
var embed = new Embed
{
    Title = "Title",
    Description = "Description",
    Color = 0x3498DB,  // Blue
    Fields = new List<EmbedField>
    {
        new() { Name = "Field 1", Value = "Value 1", Inline = true },
        new() { Name = "Field 2", Value = "Value 2", Inline = true },
    },
    Footer = new EmbedFooter { Text = "Footer text" },
    Timestamp = DateTime.UtcNow,
};

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Check this out:",
    Embeds = new List<Embed> { embed },
});
```

**Multiple embeds (up to 10):**
```csharp
var embeds = new List<Embed>
{
    new() { Title = "Embed 1", Description = "First" },
    new() { Title = "Embed 2", Description = "Second" },
    new() { Title = "Embed 3", Description = "Third" },
};

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Embeds = embeds,
});
```

**Rich embeds with all features:**
```csharp
var embed = new Embed
{
    Title = "Advanced Embed",
    Description = "Demonstrates all features",
    Url = "https://example.com",
    Timestamp = DateTime.UtcNow,
    Color = 0xFF0000,  // Red
    
    Author = new EmbedAuthor
    {
        Name = "Author Name",
        Url = "https://example.com",
        IconUrl = "https://example.com/icon.png",
    },
    
    Fields = new List<EmbedField>
    {
        new()
        {
            Name = "Regular Field",
            Value = "Some content",
            Inline = false,
        },
        new()
        {
            Name = "Inline 1",
            Value = "Left side",
            Inline = true,
        },
        new()
        {
            Name = "Inline 2",
            Value = "Right side",
            Inline = true,
        },
    },
    
    Image = new EmbedImage
    {
        Url = "https://example.com/image.png",
        Height = 500,
        Width = 500,
    },
    
    Thumbnail = new EmbedImage
    {
        Url = "https://example.com/thumb.png",
    },
    
    Footer = new EmbedFooter
    {
        Text = "Footer text",
        IconUrl = "https://example.com/footer-icon.png",
    },
};

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Embeds = new List<Embed> { embed },
});
```

**Helper method for building embeds:**
```csharp
public static Embed BuildSuccessEmbed(string title, string message)
{
    return new Embed
    {
        Title = title,
        Description = message,
        Color = 0x2ECC71,  // Green
        Timestamp = DateTime.UtcNow,
    };
}

public static Embed BuildErrorEmbed(string title, string error)
{
    return new Embed
    {
        Title = title,
        Description = error,
        Color = 0xE74C3C,  // Red
        Timestamp = DateTime.UtcNow,
    };
}

// Usage
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Embeds = new List<Embed>
    {
        BuildSuccessEmbed("Operation", "Success!"),
    },
});
```

### Fluent EmbedBuilder

`PawSharp.Core.Builders.EmbedBuilder` provides a fluent API with built-in Discord limit enforcement:

```csharp
using PawSharp.Core.Builders;

var embed = new EmbedBuilder()
    .WithTitle("My Embed")                          // max 256 chars
    .WithDescription("A description")               // max 4096 chars
    .WithUrl("https://example.com")
    .WithColor(0x5865F2)                            // blurple
    .WithColor(r: 88, g: 101, b: 242)              // or RGB bytes
    .WithAuthor("Author Name", iconUrl: "https://example.com/icon.png")
    .WithThumbnail("https://example.com/thumb.png")
    .WithImage("https://example.com/image.png")
    .AddField("Field 1", "Value 1", inline: true)  // max 25 fields
    .AddField("Field 2", "Value 2", inline: true)
    .AddField("Non-inline field", "Full-width value")
    .WithFooter("PawSharp v1.0.0-alpha.2", iconUrl: "https://example.com/footer.png")
    .WithTimestamp()                                // defaults to now
    .Build();                                       // throws if > 6000 total chars

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Embeds = new List<Embed> { embed },
});
```

**Discord embed limits (all enforced by `EmbedBuilder`):**

| Element | Limit |
|---|---|
| Title | 256 characters |
| Description | 4096 characters |
| Fields | 25 per embed |
| Field name | 256 characters |
| Field value | 1024 characters |
| Footer text | 2048 characters |
| Author name | 256 characters |
| **Total** | **6000 characters** (sum of all text) |

`Build()` throws `InvalidOperationException` if any limit is exceeded, ensuring invalid embeds are never sent to Discord.

**Common embed patterns:**
```csharp
// Success embed
var success = new EmbedBuilder()
    .WithTitle("✅ Success")
    .WithDescription(message)
    .WithColor(0x2ECC71)
    .WithTimestamp()
    .Build();

// Error embed
var error = new EmbedBuilder()
    .WithTitle("❌ Error")
    .WithDescription(errorMessage)
    .WithColor(0xE74C3C)
    .WithTimestamp()
    .Build();

// Info embed with fields
var info = new EmbedBuilder()
    .WithTitle("Server Info")
    .WithColor(0x3498DB)
    .AddField("Members", memberCount.ToString(), inline: true)
    .AddField("Channels", channelCount.ToString(), inline: true)
    .AddField("Roles", roleCount.ToString(), inline: true)
    .WithFooter($"Guild ID: {guildId}")
    .WithTimestamp()
    .Build();
```

### Retrieving Messages

**Get message history:**
```csharp
// Most recent 50 messages
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50  // 1-100
);

foreach (var msg in messages)
{
    Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
}
```

**Pagination:**
```csharp
// Get older messages
var olderMessages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50,
    before: oldestMessageId  // Get messages before this
);

// Get newer messages
var newerMessages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50,
    after: newestMessageId  // Get messages after this
);

// Get around specific message
var surrounding = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50,
    around: middleMessageId  // Get messages around this
);
```

**Get specific message:**
```csharp
var message = await client.Rest.GetMessageAsync(channelId, messageId);
if (message != null)
{
    Console.WriteLine($"Message: {message.Content}");
    Console.WriteLine($"Author: {message.Author.Username}");
    Console.WriteLine($"Created: {message.Timestamp}");
}
```

**Get pinned messages:**
```csharp
var pinned = await client.Rest.GetPinnedMessagesAsync(channelId);
foreach (var msg in pinned)
{
    Console.WriteLine($"Pinned: {msg.Content}");
}
```

### Editing Messages

```csharp
// Edit content
await client.Rest.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
    Content = "Updated message",
});

// Edit embeds
await client.Rest.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
    Embeds = new List<Embed>
    {
        new() { Title = "New embed", Description = "Updated" },
    },
});

// Clear content
await client.Rest.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
    Content = "",  // Empty content
});
```

### Deleting Messages

```csharp
// Delete single message
await client.Rest.DeleteMessageAsync(channelId, messageId);

// Bulk delete (2-100 messages)
var messageIds = new List<ulong>
{
    id1, id2, id3, id4, id5,
};

await client.Rest.BulkDeleteMessagesAsync(channelId, messageIds);

// Common pattern: delete last N messages
var messages = await client.Rest.GetChannelMessagesAsync(
    channelId,
    limit: 50
);

var idsToDelete = messages
    .Take(25)  // Take first 25
    .Select(m => m.Id)
    .ToList();

if (idsToDelete.Count >= 2)  // Need at least 2 for bulk delete
{
    await client.Rest.BulkDeleteMessagesAsync(channelId, idsToDelete);
}
```

### Pinning

```csharp
// Pin message
await client.Rest.PinMessageAsync(channelId, messageId);

// Unpin message
await client.Rest.UnpinMessageAsync(channelId, messageId);

// Get pinned messages
var pinned = await client.Rest.GetPinnedMessagesAsync(channelId);
Console.WriteLine($"Pinned messages: {pinned.Count}");
```

### Typing Indicator

```csharp
// Show "typing..." indicator
await client.Rest.TriggerTypingIndicatorAsync(channelId);

// Common pattern: typing while waiting for response
public async Task LongRunningOperation(ulong channelId)
{
    await client.Rest.TriggerTypingIndicatorAsync(channelId);
    
    // Do work (up to ~10 seconds)
    await Task.Delay(2000);
    
    await client.Rest.CreateMessageAsync(channelId, new()
    {
        Content = "Done!",
    });
}
```

---

## Channels

### Getting Channel Info

```csharp
// Get channel details
var channel = await client.Rest.GetChannelAsync(channelId);
Console.WriteLine($"#{channel.Name} (type: {channel.Type})");

if (channel.Type == ChannelType.GuildText)
{
    Console.WriteLine($"Topic: {channel.Topic}");
    Console.WriteLine($"NSFW: {channel.Nsfw}");
}

if (channel.Type == ChannelType.GuildVoice)
{
    Console.WriteLine($"Bitrate: {channel.Bitrate}");
    Console.WriteLine($"User Limit: {channel.UserLimit}");
}
```

### Creating Channels

```csharp
// Text channel
var textChannel = await client.Rest.CreateGuildChannelAsync(guildId, new CreateChannelRequest
{
    Name = "general",
    Type = ChannelType.GuildText,
    Topic = "General discussion",
});

// Voice channel
var voiceChannel = await client.Rest.CreateGuildChannelAsync(guildId, new CreateChannelRequest
{
    Name = "general-voice",
    Type = ChannelType.GuildVoice,
    Bitrate = 64000,  // 64kbps
    UserLimit = 0,    // Unlimited
});

// Category
var category = await client.Rest.CreateGuildChannelAsync(guildId, new CreateChannelRequest
{
    Name = "Support",
    Type = ChannelType.GuildCategory,
});

// Channel in category
var supportChannel = await client.Rest.CreateGuildChannelAsync(guildId, new CreateChannelRequest
{
    Name = "support-tickets",
    Type = ChannelType.GuildText,
    ParentId = category.Id,  // Put in category
});
```

### Editing Channels

```csharp
await client.Rest.ModifyChannelAsync(channelId, new ModifyChannelRequest
{
    Name = "announcements",
    Topic = "Important announcements",
    Nsfw = false,
});

// Change position in channel list
await client.Rest.ModifyChannelAsync(channelId, new ModifyChannelRequest
{
    Position = 0,  // Move to top
});

// Archive archived thread
if (channel.Type == ChannelType.GuildPublicThread)
{
    await client.Rest.ModifyChannelAsync(channelId, new ModifyChannelRequest
    {
        Archived = true,
    });
}
```

### Deleting Channels

```csharp
await client.Rest.DeleteChannelAsync(channelId);
```

### Channel Lists

```csharp
// List all channels in guild
var channels = await client.Rest.GetGuildChannelsAsync(guildId);
foreach (var ch in channels)
{
    Console.WriteLine($"#{ch.Name} ({ch.Type})");
}
```

### Invites

```csharp
// Create invite
var invite = await client.Rest.CreateChannelInviteAsync(channelId, new CreateInviteRequest
{
    MaxUses = 1,           // One-time use
    MaxAge = 3600,         // 1 hour
    Temporary = false,     // Don't auto-kick
});

Console.WriteLine($"Invite: discord.gg/{invite.Code}");

// Get channel invites
var invites = await client.Rest.GetChannelInvitesAsync(channelId);
foreach (var inv in invites)
{
    Console.WriteLine($"Invite: {inv.Code} ({inv.Uses}/{inv.MaxUses})");
}
```

---

## Guilds

### Getting Guild Info

```csharp
// Get guild details
var guild = await client.Rest.GetGuildAsync(guildId);
Console.WriteLine($"Guild: {guild.Name}");
Console.WriteLine($"Members: {guild.MemberCount}");
Console.WriteLine($"Roles: {guild.Roles.Count}");
Console.WriteLine($"Created: {guild.CreatedAt}");

// With additional counts
var guildWithCounts = await client.Rest.GetGuildAsync(guildId, withCounts: true);
Console.WriteLine($"Approximate member count: {guildWithCounts.ApproximateMemberCount}");
```

### Creating Guilds

```csharp
var newGuild = await client.Rest.CreateGuildAsync(new CreateGuildRequest
{
    Name = "My New Server",
    DefaultMessageNotifications = DefaultMessageNotificationLevel.OnlyMentions,
    VerificationLevel = VerificationLevel.Medium,
});

Console.WriteLine($"Created guild: {newGuild.Id}");
```

### Editing Guilds

```csharp
await client.Rest.ModifyGuildAsync(guildId, new ModifyGuildRequest
{
    Name = "New Name",
    Icon = iconUrl,
    DefaultMessageNotifications = DefaultMessageNotificationLevel.AllMessages,
    VerificationLevel = VerificationLevel.High,
    ExplicitContentFilterLevel = ExplicitContentFilterLevel.Everyone,
});
```

### Audit Logs

```csharp
// Get recent audit logs
var auditLog = await client.Rest.GetGuildAuditLogsAsync(
    guildId,
    limit: 50
);

if (auditLog?.AuditLogEntries != null)
{
    foreach (var entry in auditLog.AuditLogEntries)
    {
        Console.WriteLine($"{entry.ActionType}: {entry.TargetId}");
        Console.WriteLine($"User: {entry.UserId}");
        Console.WriteLine($"Reason: {entry.Reason}");
    }
}

// Get logs for specific action
var banLogs = await client.Rest.GetGuildAuditLogsAsync(
    guildId,
    actionType: AuditLogEvent.MemberBanAdd,
    limit: 10
);

// Get logs by user
var userLogs = await client.Rest.GetGuildAuditLogsAsync(
    guildId,
    userId: moderatorId,
    limit: 50
);
```

---

## Members & Users

### Getting Members

```csharp
// Get specific member
var member = await client.Rest.GetGuildMemberAsync(guildId, userId);
if (member != null)
{
    Console.WriteLine($"Name: {member.User.Username}");
    Console.WriteLine($"Nickname: {member.Nickname}");
    Console.WriteLine($"Joined: {member.JoinedAt}");
    Console.WriteLine($"Roles: {string.Join(", ", member.RoleIds)}");
}

// List members
var members = await client.Rest.GetGuildMembersAsync(
    guildId,
    limit: 1000  // Max per request
);

foreach (var m in members)
{
    Console.WriteLine($"{m.User.Username}#{m.User.Discriminator}");
}
```

### Member Management

```csharp
// Add member to guild (requires oauth token)
await client.Rest.AddGuildMemberAsync(guildId, userId, new AddGuildMemberRequest
{
    AccessToken = oauthToken,
});

// Modify member
await client.Rest.ModifyGuildMemberAsync(guildId, userId, new ModifyGuildMemberRequest
{
    Nickname = "New Nickname",
    Mute = false,
    Deafen = false,
    RoleIds = new List<ulong> { roleId1, roleId2 },
    VoiceChannelId = voiceChannelId,  // Move to voice channel
});

// Kick member
await client.Rest.RemoveGuildMemberAsync(guildId, userId);

// Kick with reason
await client.Rest.RemoveGuildMemberAsync(
    guildId,
    userId,
    reason: "Violating server rules"
);
```

### User Information

```csharp
// Get user info
var user = await client.Rest.GetUserAsync(userId);
if (user != null)
{
    Console.WriteLine($"Username: {user.Username}");
    Console.WriteLine($"ID: {user.Id}");
    Console.WriteLine($"Created: {user.CreatedAt}");
    Console.WriteLine($"Bot: {user.IsBot}");
    Console.WriteLine($"System: {user.System}");
}
```

---

## Roles

### Getting Roles

```csharp
// List all roles
var roles = await client.Rest.GetGuildRolesAsync(guildId);
foreach (var role in roles)
{
    Console.WriteLine($"@{role.Name}");
    Console.WriteLine($"  Color: {role.Color:X}");
    Console.WriteLine($"  Mentionable: {role.Mentionable}");
    Console.WriteLine($"  Position: {role.Position}");
}
```

### Creating Roles

```csharp
var role = await client.Rest.CreateGuildRoleAsync(guildId, new CreateRoleRequest
{
    Name = "Moderator",
    Color = 0xFF0000,  // Red
    Permissions = 0,   // No permissions by default
    Mentionable = true,
    Hoist = true,      // Show role separately
});

Console.WriteLine($"Created role: @{role.Name}");
```

### Editing Roles

```csharp
await client.Rest.ModifyGuildRoleAsync(guildId, roleId, new ModifyRoleRequest
{
    Name = "Senior Moderator",
    Color = 0x00FF00,  // Green
    Hoist = true,      // Show separately
    Mentionable = true,
});
```

### Assigning Roles

```csharp
// Add role to member
await client.Rest.AddGuildMemberRoleAsync(guildId, userId, roleId);

// Remove role from member
await client.Rest.RemoveGuildMemberRoleAsync(guildId, userId, roleId);

// Add multiple roles
var roleIds = new List<ulong> { roleId1, roleId2, roleId3 };
await client.Rest.ModifyGuildMemberAsync(guildId, userId, new ModifyGuildMemberRequest
{
    RoleIds = roleIds,
});
```

### Deleting Roles

```csharp
await client.Rest.DeleteGuildRoleAsync(guildId, roleId);
```

---

## Webhooks

### Creating Webhooks

```csharp
var webhook = await client.Rest.CreateWebhookAsync(channelId, new CreateWebhookRequest
{
    Name = "My Webhook",
    AvatarUrl = "https://example.com/avatar.png",
});

Console.WriteLine($"Webhook ID: {webhook.Id}");
Console.WriteLine($"Token: {webhook.Token}");
```

### Executing Webhooks

```csharp
// Send message via webhook
await client.Rest.ExecuteWebhookAsync(
    webhookId,
    webhookToken,
    new ExecuteWebhookRequest
    {
        Content = "Message from webhook",
        Username = "Custom Name",  // Override webhook name
    }
);

// With embed
await client.Rest.ExecuteWebhookAsync(
    webhookId,
    webhookToken,
    new ExecuteWebhookRequest
    {
        Embeds = new List<Embed>
        {
            new() { Title = "Webhook", Description = "Message" },
        },
    }
);
```

### Managing Webhooks

```csharp
// Get channel webhooks
var webhooks = await client.Rest.GetChannelWebhooksAsync(channelId);
foreach (var wh in webhooks)
{
    Console.WriteLine($"{wh.Name} ({wh.Id})");
}

// Get webhook
var webhook = await client.Rest.GetWebhookAsync(webhookId);

// Modify webhook
await client.Rest.ModifyWebhookAsync(webhookId, new ModifyWebhookRequest
{
    Name = "New Name",
});

// Delete webhook
await client.Rest.DeleteWebhookAsync(webhookId);
```

---

## Threads

### Creating Threads

```csharp
// Create from existing message
var thread = await client.Rest.CreateThreadFromMessageAsync(
    channelId,
    messageId,
    new CreateThreadRequest
    {
        Name = "Discussion",
        AutoArchiveDuration = 3600,  // 1 hour
    }
);

// Create new thread
var newThread = await client.Rest.CreateThreadAsync(channelId, new CreateThreadRequest
{
    Name = "New Thread",
    AutoArchiveDuration = 3600,
});
```

### Managing Thread Membership

```csharp
// Join thread
await client.Rest.JoinThreadAsync(threadChannelId);

// Add member
await client.Rest.AddThreadMemberAsync(threadChannelId, userId);

// Leave thread
await client.Rest.LeaveThreadAsync(threadChannelId);

// Remove member
await client.Rest.RemoveThreadMemberAsync(threadChannelId, userId);
```

### Listing Threads

```csharp
// Active threads
var active = await client.Rest.GetActiveThreadsAsync(guildId);

// Archived public
var archived = await client.Rest.GetPublicArchivedThreadsAsync(channelId);

// Archived private (own)
var private = await client.Rest.GetJoinedPrivateArchivedThreadsAsync(channelId);
```

---

## Reactions & Components

### Reactions

```csharp
// Add reaction
await client.Rest.CreateReactionAsync(
    channelId,
    messageId,
    emoji: "👍"
);

// Unicode emoji
await client.Rest.CreateReactionAsync(channelId, messageId, "🎉");

// Custom emoji
await client.Rest.CreateReactionAsync(channelId, messageId, "custom:id");

// Remove own reaction
await client.Rest.DeleteOwnReactionAsync(channelId, messageId, "👍");

// Remove user reaction (mod)
await client.Rest.DeleteUserReactionAsync(channelId, messageId, "👍", userId);
```

---

## Advanced Patterns

### Rate Limiting Best Practices

```csharp
// Manual rate limiting with semaphore
private readonly SemaphoreSlim _requestSemaphore = new(5, 5);

public async Task<Message?> SafeCreateMessage(
    ulong channelId,
    CreateMessageRequest request)
{
    await _requestSemaphore.WaitAsync();
    try
    {
        return await client.Rest.CreateMessageAsync(channelId, request);
    }
    catch (RateLimitException ex)
    {
        _logger.LogWarning($"Rate limited, retry after {ex.RetryAfter}");
        await Task.Delay(ex.RetryAfter);
        return await client.Rest.CreateMessageAsync(channelId, request);
    }
    finally
    {
        _requestSemaphore.Release();
    }
}
```

### Bulk Operations

```csharp
// Create multiple roles
public async Task CreateRolesAsync(ulong guildId, IEnumerable<string> roleNames)
{
    foreach (var name in roleNames)
    {
        await client.Rest.CreateGuildRoleAsync(guildId, new CreateRoleRequest
        {
            Name = name,
        });
        
        // Small delay to avoid rate limiting
        await Task.Delay(100);
    }
}

// Clean up channels
public async Task DeleteChannelsAsync(ulong guildId, Func<Channel, bool> predicate)
{
    var channels = await client.Rest.GetGuildChannelsAsync(guildId);
    
    foreach (var channel in channels.Where(predicate))
    {
        await client.Rest.DeleteChannelAsync(channel.Id);
    }
}
```

### Caching REST Results

```csharp
private Dictionary<ulong, Guild> _guildCache = new();
private DateTime _lastGuildRefresh = DateTime.MinValue;

public async Task<Guild?> GetGuildCached(ulong guildId)
{
    // Use cache if recent
    if (DateTime.UtcNow - _lastGuildRefresh < TimeSpan.FromMinutes(5))
    {
        if (_guildCache.TryGetValue(guildId, out var cached))
            return cached;
    }
    
    // Fetch and cache
    var guild = await client.Rest.GetGuildAsync(guildId);
    if (guild != null)
    {
        _guildCache[guildId] = guild;
        _lastGuildRefresh = DateTime.UtcNow;
    }
    
    return guild;
}
```

---

## Error Handling

```csharp
public async Task SafeRestOperation()
{
    try
    {
        await client.Rest.CreateMessageAsync(channelId, request);
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning($"Validation failed: {ex.Message}");
        // Invalid input - fix and retry
    }
    catch (RateLimitException ex)
    {
        _logger.LogWarning($"Rate limited, retry after {ex.RetryAfter}ms");
        // Back off and retry
        await Task.Delay(ex.RetryAfter);
    }
    catch (DiscordApiException ex)
    {
        _logger.LogError($"Discord API error ({ex.StatusCode}): {ex.Message}");
        // Handle API-specific error
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        // Handle unexpected error
    }
}
```

---

**More guides:** [Gateway Events](./GATEWAY_GUIDE.md) | [Caching](./CACHING_GUIDE.md) | [Patterns](./PATTERNS_GUIDE.md)
