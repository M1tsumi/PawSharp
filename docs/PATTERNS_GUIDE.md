# Common Patterns & Recipes

Real-world patterns and code recipes for building Discord bots with PawSharp.

## Table of Contents

1. [Command Handling](#command-handling)
2. [Moderation](#moderation)
3. [Logging & Monitoring](#logging--monitoring)
4. [User Interactions](#user-interactions)
5. [Data Persistence](#data-persistence)
6. [Advanced Techniques](#advanced-techniques)

---

## Command Handling

### Class-Based Commands with `CommandsExtension` (Recommended)

`CommandsExtension` discovers command methods automatically using reflection and wires up the `MESSAGE_CREATE` event internally —
no manual event subscription required.

```csharp
using PawSharp.Commands;

// 1. Define a module
public class GeneralCommands : BaseCommandModule
{
    private readonly IDiscordRestClient _rest;

    public GeneralCommands(IDiscordRestClient rest)
    {
        _rest = rest;
    }

    [Command("ping")]
    [Description("Responds with pong")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("🏓 Pong!");
    }

    [Command("hello")]
    [Aliases("hi", "hey")]
    [Description("Greet the user")]
    public async Task HelloAsync(CommandContext ctx)
    {
        await ctx.RespondAsync($"👋 Hello, {ctx.User.Username}!");
    }

    [Command("echo")]
    [Description("Repeat your message")]
    public async Task EchoAsync(CommandContext ctx)
    {
        var text = ctx.RawArguments;
        if (string.IsNullOrWhiteSpace(text))
        {
            await ctx.RespondAsync("Usage: !echo <text>");
            return;
        }
        await ctx.RespondAsync(text);
    }
}

// 2. Register the module — MESSAGE_CREATE is wired automatically
var commands = new CommandsExtension(prefix: "!");
commands.RegisterModule(client, new GeneralCommands(client.Rest));

// 3. List all registered commands
foreach (var info in commands.GetRegisteredCommands())
    Console.WriteLine($"  !{info.Name}  {info.Description}");
```

**`CommandContext` properties:**

| Property | Type | Description |
|---|---|---|
| `Client` | `DiscordClient` | The Discord client |
| `Message` | `Message` | The triggering message |
| `ChannelId` | `ulong` | Channel where command was run |
| `GuildId` | `ulong?` | Guild (null for DMs) |
| `User` | `User` | User who ran the command |
| `Prefix` | `string` | The prefix used (`!`) |
| `CommandName` | `string` | Command name without prefix |
| `Arguments` | `string[]` | Whitespace-split arguments |
| `RawArguments` | `string` | Everything after the command name |

**`ctx.RespondAsync` overloads:**
```csharp
await ctx.RespondAsync("Simple text response");
await ctx.RespondAsync(embedObject);
```

---

### Manual Command Router (Simple)

```csharp
public class CommandRouter
{
    private readonly IDiscordRestClient _rest;
    private readonly ILogger<CommandRouter> _logger;
    private readonly Dictionary<string, Func<MessageCreateEvent, Task>> _commands;

    public CommandRouter(IDiscordRestClient rest, ILogger<CommandRouter> logger)
    {
        _rest = rest;
        _logger = logger;
        _commands = new();
    }

    public void Register(string command, Func<MessageCreateEvent, Task> handler)
    {
        _commands[command] = handler;
        _logger.LogInformation($"Registered command: {command}");
    }

    public async Task HandleAsync(MessageCreateEvent msg)
    {
        if (msg.Author.IsBot || !msg.Content.StartsWith("!"))
            return;

        var parts = msg.Content.Split(' ');
        var command = parts[0][1..].ToLower();  // Remove "!"

        if (_commands.TryGetValue(command, out var handler))
        {
            try
            {
                await handler(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling command: {command}");
                
                await _rest.CreateMessageAsync(msg.ChannelId, new()
                {
                    Content = "❌ Command failed. Try again later.",
                });
            }
        }
    }
}

// Usage
var router = new CommandRouter(rest, logger);

router.Register("ping", msg => rest.CreateMessageAsync(msg.ChannelId, new()
{
    Content = "🏓 Pong!",
}));

router.Register("hello", msg => rest.CreateMessageAsync(msg.ChannelId, new()
{
    Content = $"👋 Hello, {msg.Author.Username}!",
}));

client.OnMessageCreated(router.HandleAsync);
```

---

### Throttling heavy handlers

When handling high volumes of events (e.g. busy large servers), throttle
expensive processing to avoid overwhelming CPU or I/O. A simple pattern uses
`SemaphoreSlim` to limit concurrency:

```csharp
private readonly SemaphoreSlim _throttle = new SemaphoreSlim(10); // max concurrency

async Task HandleMessageWithThrottle(MessageCreateEvent msg)
{
    await _throttle.WaitAsync();
    try
    {
        await DoCpuOrIoBoundWorkAsync(msg);
    }
    finally
    {
        _throttle.Release();
    }
}

client.OnMessageCreated(HandleMessageWithThrottle);
```

This keeps the gateway handlers responsive while bounding background work.

### Command with Arguments

```csharp
public class AdvancedRouter
{
    public async Task HandleAsync(MessageCreateEvent msg)
    {
        if (msg.Author.IsBot || !msg.Content.StartsWith("!"))
            return;

        var parts = msg.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0][1..].ToLower();
        var args = parts.Skip(1).ToArray();

        switch (command)
        {
            case "echo":
                await HandleEcho(msg, args);
                break;

            case "role":
                await HandleRole(msg, args);
                break;

            default:
                await _rest.CreateMessageAsync(msg.ChannelId, new()
                {
                    Content = $"Unknown command: {command}",
                });
                break;
        }
    }

    private async Task HandleEcho(MessageCreateEvent msg, string[] args)
    {
        var text = string.Join(" ", args);
        if (string.IsNullOrEmpty(text))
        {
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = "Usage: !echo <text>",
            });
            return;
        }

        await _rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = text,
        });
    }

    private async Task HandleRole(MessageCreateEvent msg, string[] args)
    {
        if (args.Length == 0)
        {
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = "Usage: !role <@user> <@role>",
            });
            return;
        }

        // Parse mentions
        var userId = ExtractUserId(args[0]);
        var roleId = ExtractRoleId(args.Skip(1).First());

        if (userId == 0 || roleId == 0)
        {
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = "Invalid user or role",
            });
            return;
        }

        await _rest.AddGuildMemberRoleAsync(msg.GuildId!.Value, userId, roleId);
        await _rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = $"✅ Role assigned to <@{userId}>",
        });
    }

    private ulong ExtractUserId(string mention)
    {
        // Extract from <@123456>
        var text = mention.TrimStart('<').TrimEnd('>').Replace("@", "");
        return ulong.TryParse(text, out var id) ? id : 0;
    }

    private ulong ExtractRoleId(string mention)
    {
        // Extract from <@&123456>
        var text = mention.TrimStart('<').TrimEnd('>').Replace("@&", "");
        return ulong.TryParse(text, out var id) ? id : 0;
    }
}
```

---

## Moderation

### Auto-Moderation

```csharp
public class AutoModerator
{
    private readonly IDiscordRestClient _rest;
    private readonly ILogger<AutoModerator> _logger;

    public async Task HandleMessageAsync(MessageCreateEvent msg)
    {
        // Skip bots
        if (msg.Author.IsBot) return;

        // Check for spam
        if (IsSpam(msg.Content))
        {
            await _rest.DeleteMessageAsync(msg.ChannelId, msg.Id);
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = $"{msg.Author.Mention} please don't spam",
            });
            return;
        }

        // Check for bad words
        if (HasBadWords(msg.Content))
        {
            await _rest.DeleteMessageAsync(msg.ChannelId, msg.Id);
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = $"{msg.Author.Mention} watch your language",
            });
            return;
        }

        // Check for excessive caps
        if (HasExcessiveCaps(msg.Content))
        {
            await _rest.DeleteMessageAsync(msg.ChannelId, msg.Id);
            await _rest.CreateMessageAsync(msg.ChannelId, new()
            {
                Content = $"{msg.Author.Mention} please use normal caps",
            });
        }
    }

    private bool IsSpam(string content)
    {
        // Check for repeated characters
        return System.Text.RegularExpressions.Regex.IsMatch(content, @"(.)\1{10,}");
    }

    private bool HasBadWords(string content)
    {
        var badWords = new[] { "badword1", "badword2" };
        var lower = content.ToLower();
        return badWords.Any(w => lower.Contains(w));
    }

    private bool HasExcessiveCaps(string content)
    {
        var chars = content.Where(char.IsLetter).ToList();
        if (chars.Count < 5) return false;

        var capsCount = chars.Count(char.IsUpper);
        return (double)capsCount / chars.Count > 0.8;  // 80%+
    }
}

client.OnMessageCreated(moderator.HandleMessageAsync);
```

### Kick & Ban with Logging

```csharp
public class ModAction
{
    private readonly IDiscordRestClient _rest;
    private readonly ulong _modLogChannelId;

    public ModAction(IDiscordRestClient rest, ulong modLogChannelId)
    {
        _rest = rest;
        _modLogChannelId = modLogChannelId;
    }

    public async Task KickUserAsync(ulong guildId, ulong userId, string reason)
    {
        await _rest.RemoveGuildMemberAsync(guildId, userId);
        await LogActionAsync("Kick", guildId, userId, reason);
    }

    public async Task BanUserAsync(ulong guildId, ulong userId, string reason, int deleteDays = 0)
    {
        await _rest.CreateGuildBanAsync(guildId, userId, deleteDays, reason);
        await LogActionAsync("Ban", guildId, userId, reason);
    }

    public async Task MuteUserAsync(ulong guildId, ulong userId, ulong muteRoleId)
    {
        await _rest.AddGuildMemberRoleAsync(guildId, userId, muteRoleId);
        await LogActionAsync("Mute", guildId, userId, "");
    }

    private async Task LogActionAsync(string action, ulong guildId, ulong userId, string reason)
    {
        var embed = new Embed
        {
            Title = $"Moderation Action: {action}",
            Color = 0xFF0000,  // Red
            Fields = new List<EmbedField>
            {
                new() { Name = "User", Value = $"<@{userId}>", Inline = true },
                new() { Name = "Action", Value = action, Inline = true },
                new() { Name = "Reason", Value = reason ?? "No reason", Inline = false },
                new() { Name = "Timestamp", Value = DateTime.UtcNow.ToString("O"), Inline = false },
            },
            Timestamp = DateTime.UtcNow,
        };

        await _rest.CreateMessageAsync(_modLogChannelId, new()
        {
            Embeds = new List<Embed> { embed },
        });
    }
}
```

---

## Logging & Monitoring

### Message Logger

```csharp
public class MessageLogger
{
    private readonly ILogger<MessageLogger> _logger;

    public async Task LogMessageAsync(MessageCreateEvent msg)
    {
        _logger.LogInformation(
            "Message: {User} in #{Channel}: {Content}",
            msg.Author.Username,
            msg.ChannelId,
            msg.Content
        );

        // Log to database/file if needed
        await SaveToLogAsync(msg);
    }

    private async Task SaveToLogAsync(MessageCreateEvent msg)
    {
        // Save to database, file, or external service
        using var db = new LogDatabase();
        await db.Messages.AddAsync(new LogMessage
        {
            UserId = msg.Author.Id,
            Username = msg.Author.Username,
            Content = msg.Content,
            ChannelId = msg.ChannelId,
            MessageId = msg.Id,
            Timestamp = msg.Timestamp,
        });
        await db.SaveChangesAsync();
    }
}
```

### Guild Activity Monitor

```csharp
public class GuildActivityMonitor
{
    private Dictionary<ulong, GuildMetrics> _metrics = new();

    public void TrackMessage(MessageCreateEvent msg)
    {
        if (!_metrics.TryGetValue(msg.GuildId ?? 0, out var metrics))
        {
            metrics = new GuildMetrics();
            _metrics[msg.GuildId ?? 0] = metrics;
        }

        metrics.MessageCount++;
        metrics.LastActivity = DateTime.UtcNow;
    }

    public GuildMetrics? GetMetrics(ulong guildId)
    {
        return _metrics.TryGetValue(guildId, out var metrics) ? metrics : null;
    }

    public void PrintStats()
    {
        foreach (var (guildId, metrics) in _metrics)
        {
            Console.WriteLine($"Guild {guildId}: {metrics.MessageCount} messages, last activity {metrics.LastActivity}");
        }
    }

    public class GuildMetrics
    {
        public int MessageCount { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
```

---

## User Interactions

### Reaction Menu

```csharp
public class ReactionMenu
{
    private readonly IDiscordRestClient _rest;
    private readonly Dictionary<ulong, MenuState> _activeMenus = new();

    public async Task ShowMenuAsync(ulong channelId, string[] options)
    {
        var emojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣" };

        var fields = new List<EmbedField>();
        for (int i = 0; i < options.Length; i++)
        {
            fields.Add(new()
            {
                Name = emojis[i],
                Value = options[i],
                Inline = false,
            });
        }

        var embed = new Embed
        {
            Title = "Choose an option:",
            Fields = fields,
        };

        var msg = await _rest.CreateMessageAsync(channelId, new()
        {
            Embeds = new List<Embed> { embed },
        });

        // Add reaction options
        for (int i = 0; i < options.Length; i++)
        {
            await _rest.CreateReactionAsync(channelId, msg.Id, emojis[i]);
        }

        // Track menu
        _activeMenus[msg.Id] = new MenuState
        {
            ChannelId = channelId,
            Options = options,
            Expires = DateTime.UtcNow.AddMinutes(5),
        };
    }

    public MenuState? GetMenu(ulong messageId)
    {
        return _activeMenus.TryGetValue(messageId, out var menu) ? menu : null;
    }

    public void RemoveMenu(ulong messageId)
    {
        _activeMenus.Remove(messageId);
    }

    public class MenuState
    {
        public ulong ChannelId { get; set; }
        public string[] Options { get; set; } = Array.Empty<string>();
        public DateTime Expires { get; set; }
    }
}

// Usage
client.OnReactionAdded(async reaction =>
{
    var menu = reactionMenu.GetMenu(reaction.MessageId);
    if (menu == null) return;

    Console.WriteLine($"User selected: {reaction.Emoji.Name}");
    reactionMenu.RemoveMenu(reaction.MessageId);
});
```

### Button Menu (Modern)

```csharp
public class ButtonMenu
{
    private readonly IDiscordRestClient _rest;

    public async Task ShowMenuAsync(ulong channelId, Dictionary<string, Action<ulong>> options)
    {
        var embed = new Embed
        {
            Title = "Choose an option:",
            Description = string.Join("\n", options.Keys),
        };

        // In a real implementation, you would use Discord's button components
        // This is simplified for this guide
        
        await _rest.CreateMessageAsync(channelId, new()
        {
            Content = "Click a button below:",
            Embeds = new List<Embed> { embed },
        });
    }
}
```

---

## Data Persistence

### Simple Database Integration

```csharp
public class UserDatabase
{
    private readonly IConfiguration _config;
    private string ConnectionString => _config.GetConnectionString("DefaultConnection");

    public async Task LogUserAsync(User user)
    {
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Users (UserId, Username, DiscriminatorValue, AvatarUrl, IsBot, CreatedAt)
            VALUES (@userId, @username, @discriminator, @avatar, @isBot, @createdAt)
            ON CONFLICT(UserId) DO UPDATE SET
                Username = @username,
                UpdatedAt = @createdAt
        """;

        cmd.Parameters.AddWithValue("@userId", user.Id);
        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@discriminator", user.Discriminator ?? "");
        cmd.Parameters.AddWithValue("@avatar", user.Avatar ?? "");
        cmd.Parameters.AddWithValue("@isBot", user.IsBot);
        cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<UserRecord?> GetUserAsync(ulong userId)
    {
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Users WHERE UserId = @userId";
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new UserRecord
            {
                UserId = (ulong)reader["UserId"],
                Username = (string)reader["Username"],
                CreatedAt = (DateTime)reader["CreatedAt"],
            };
        }

        return null;
    }

    public class UserRecord
    {
        public ulong UserId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
```

---

## Advanced Techniques

### Rate Limit Manager

```csharp
public class RateLimitManager
{
    private Dictionary<ulong, UserRateLimit> _limits = new();

    public bool CanExecute(ulong userId, string command, int cooldownSeconds = 5)
    {
        if (!_limits.TryGetValue(userId, out var limit))
        {
            limit = new UserRateLimit();
            _limits[userId] = limit;
        }

        if (limit.LastCommandTime.AddSeconds(cooldownSeconds) > DateTime.UtcNow)
        {
            return false;  // On cooldown
        }

        limit.LastCommandTime = DateTime.UtcNow;
        limit.CommandCount++;

        return true;
    }

    public TimeSpan GetCooldownRemaining(ulong userId, int cooldownSeconds = 5)
    {
        if (!_limits.TryGetValue(userId, out var limit))
            return TimeSpan.Zero;

        var remaining = limit.LastCommandTime.AddSeconds(cooldownSeconds) - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public class UserRateLimit
    {
        public DateTime LastCommandTime { get; set; }
        public int CommandCount { get; set; }
    }
}

// Usage
client.OnMessageCreated(async msg =>
{
    if (!msg.Content.StartsWith("!")) return;

    if (!rateLimiter.CanExecute(msg.Author.Id, "mycommand"))
    {
        var remaining = rateLimiter.GetCooldownRemaining(msg.Author.Id);
        await _rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = $"⏱️ Please wait {remaining.TotalSeconds:F1} seconds",
        });
        return;
    }

    // Execute command
});
```

### Webhook Announcer

```csharp
public class WebhookAnnouncer
{
    private readonly IDiscordRestClient _rest;
    private Dictionary<ulong, ulong> _webhookCache = new();

    public async Task AnnounceAsync(
        ulong guildId,
        ulong channelId,
        string title,
        string message,
        string authorName = "Bot")
    {
        // Get or create webhook
        ulong webhookId;
        if (!_webhookCache.TryGetValue(channelId, out webhookId))
        {
            var webhook = await _rest.CreateWebhookAsync(channelId, new CreateWebhookRequest
            {
                Name = "Announcements",
            });
            webhookId = webhook.Id;
            _webhookCache[channelId] = webhookId;
        }

        // Send via webhook
        var embed = new Embed
        {
            Title = title,
            Description = message,
            Color = 0x3498DB,
            Timestamp = DateTime.UtcNow,
        };

        await _rest.ExecuteWebhookAsync(
            webhookId,
            "webhook-token",  // Get from webhook
            new ExecuteWebhookRequest
            {
                Username = authorName,
                Embeds = new List<Embed> { embed },
            }
        );
    }
}
```

### Status Rotator

```csharp
public class StatusRotator
{
    private readonly string[] _statuses = new[]
    {
        "!help for commands",
        "built with PawSharp",
        "PawSharp is awesome",
        $"in {DateTime.Now.Year}",
    };

    private int _currentStatus = 0;
    private Timer _rotationTimer;

    public void Start(Func<string, Task> updateStatus)
    {
        _rotationTimer = new Timer(
            async _ => await RotateAsync(updateStatus),
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5)
        );
    }

    private async Task RotateAsync(Func<string, Task> updateStatus)
    {
        var status = _statuses[_currentStatus];
        await updateStatus(status);
        _currentStatus = (_currentStatus + 1) % _statuses.Length;
    }
}

// Usage - Note: Status updates via Discord API are limited
// Consider using Gateway presence updates instead
```

---

**More guides:** [REST API](./REST_API_GUIDE.md) | [Gateway Events](./GATEWAY_GUIDE.md) | [Caching](./CACHING_GUIDE.md)
