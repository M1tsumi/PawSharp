# Gateway & Real-Time Events Guide

Learn how to listen to real-time Discord events using PawSharp's Gateway system.

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Subscribing to Events](#subscribing-to-events)
3. [Connection Management](#connection-management)
4. [Event Reference](#event-reference)
5. [Event Handling Patterns](#event-handling-patterns)
6. [Error Recovery](#error-recovery)
7. [Advanced Usage](#advanced-usage)

---

## Core Concepts

### REST vs Gateway

| Aspect | REST | Gateway |
|--------|------|---------|
| **Use for** | API requests | Real-time events |
| **Examples** | Send messages, create roles | Message received, member joined |
| **Timing** | Request-response | Server pushes data |
| **Connection** | HTTP (stateless) | WebSocket (persistent) |

### Event Dispatcher

The `EventDispatcher` is your gateway to all real-time events:

```csharp
var dispatcher = client.Gateway.EventDispatcher;

// Subscribe to event
dispatcher.On<MessageCreateEvent>(HandleMessage);

// Event fires when message is received
private async Task HandleMessage(MessageCreateEvent msg)
{
    Console.WriteLine($"Message: {msg.Content}");
    return Task.CompletedTask;
}
```

### Connection Lifecycle

```
Connecting → Connected → Ready → Events Flow → Disconnect → Reconnecting
```

---

## Subscribing to Events

### Basic Subscription

```csharp
var dispatcher = client.Gateway.EventDispatcher;

// Method 1: Lambda
dispatcher.On<MessageCreateEvent>(msg =>
{
    Console.WriteLine(msg.Content);
    return Task.CompletedTask;
});

// Method 2: Named method
dispatcher.On<MessageCreateEvent>(HandleMessage);

// Method 3: Async method
dispatcher.On<MessageCreateEvent>(async msg =>
{
    await client.Rest.CreateMessageAsync(msg.ChannelId, new()
    {
        Content = "Received!",
    });
});

private async Task HandleMessage(MessageCreateEvent msg)
{
    // Handle event
    return Task.CompletedTask;
}
```

### Multiple Event Subscriptions

```csharp
// Subscribe to multiple events
dispatcher.On<ReadyEvent>(Ready);
dispatcher.On<MessageCreateEvent>(MessageCreate);
dispatcher.On<GuildMemberAddEvent>(MemberAdd);
dispatcher.On<GuildMemberRemoveEvent>(MemberRemove);

private async Task Ready(ReadyEvent @event)
{
    Console.WriteLine($"Ready as {event.User.Username}");
    return Task.CompletedTask;
}

private async Task MessageCreate(MessageCreateEvent msg)
{
    // Handle message
    return Task.CompletedTask;
}

private async Task MemberAdd(GuildMemberAddEvent member)
{
    // Welcome new member
    return Task.CompletedTask;
}

private async Task MemberRemove(GuildMemberRemoveEvent member)
{
    // Say goodbye
    return Task.CompletedTask;
}
```

### Middleware / Pre-processing

```csharp
// Add middleware that runs before all event handlers
dispatcher.Use(async (context, next) =>
{
    var eventType = context.GetType().Name;
    Console.WriteLine($"[EVENT] {eventType}");
    
    // Call next middleware/handler
    await next();
    
    Console.WriteLine($"[DONE] {eventType}");
});

// Middleware for error handling
dispatcher.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling {context.GetType().Name}: {ex}");
    }
});

// Middleware for filtering
dispatcher.Use(async (context, next) =>
{
    if (context is MessageCreateEvent msg && msg.Author.IsBot)
    {
        return;  // Skip bot messages
    }
    
    await next();
});
```

---

## Connection Management

### Connecting to Gateway

```csharp
// In your main method
try
{
    Console.WriteLine("Connecting...");
    await client.ConnectAsync();
    Console.WriteLine("Connected!");
    
    // Keep running
    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
finally
{
    await client.DisconnectAsync();
}
```

### Connection Events

```csharp
// Bot is ready
dispatcher.On<ReadyEvent>(ready =>
{
    Console.WriteLine($"✅ Ready as {ready.User.Username}");
    Console.WriteLine($"   Guilds: {ready.Guilds.Count}");
    return Task.CompletedTask;
});

// Connection resumed after disconnect
dispatcher.On<ResumedEvent>(resumed =>
{
    Console.WriteLine("✅ Connection resumed");
    return Task.CompletedTask;
});
```

### Manual Reconnection

```csharp
// Manually reconnect if needed
if (!client.Gateway.IsConnected)
{
    await client.Gateway.ConnectAsync();
}

// Check connection status
if (client.Gateway.IsConnected)
{
    Console.WriteLine("Connected");
}
else
{
    Console.WriteLine("Disconnected");
}
```

### Graceful Shutdown

```csharp
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += async (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down...");
    
    // Unsubscribe from events (optional)
    // Disconnect gracefully
    await client.DisconnectAsync();
    
    cts.Cancel();
};

try
{
    await client.ConnectAsync();
    await Task.Delay(Timeout.Infinite, cts.Token);
}
finally
{
    Console.WriteLine("Bot stopped");
}
```

---

## Event Reference

### Connection Events

**ReadyEvent** - Bot is ready
```csharp
dispatcher.On<ReadyEvent>(ready =>
{
    Console.WriteLine($"User: {ready.User.Username}#{ready.User.Discriminator}");
    Console.WriteLine($"Guilds: {ready.Guilds.Count}");
    return Task.CompletedTask;
});
```

**ResumedEvent** - Session resumed
```csharp
dispatcher.On<ResumedEvent>(resumed =>
{
    Console.WriteLine("Reconnected!");
    return Task.CompletedTask;
});
```

### Message Events

**MessageCreateEvent** - Message sent
```csharp
dispatcher.On<MessageCreateEvent>(msg =>
{
    if (!msg.Author.IsBot)
    {
        Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
    }
    return Task.CompletedTask;
});
```

**MessageUpdateEvent** - Message edited
```csharp
dispatcher.On<MessageUpdateEvent>(msg =>
{
    Console.WriteLine($"Message edited: {msg.Id}");
    if (msg.Content != null)
    {
        Console.WriteLine($"New content: {msg.Content}");
    }
    return Task.CompletedTask;
});
```

**MessageDeleteEvent** - Message deleted
```csharp
dispatcher.On<MessageDeleteEvent>(msg =>
{
    Console.WriteLine($"Message deleted: {msg.Id}");
    return Task.CompletedTask;
});
```

### Guild Events

**GuildCreateEvent** - Bot joined guild
```csharp
dispatcher.On<GuildCreateEvent>(guild =>
{
    Console.WriteLine($"Joined: {guild.Name} ({guild.MemberCount} members)");
    return Task.CompletedTask;
});
```

**GuildUpdateEvent** - Guild updated
```csharp
dispatcher.On<GuildUpdateEvent>(guild =>
{
    Console.WriteLine($"Guild updated: {guild.Name}");
    return Task.CompletedTask;
});
```

**GuildDeleteEvent** - Bot left guild
```csharp
dispatcher.On<GuildDeleteEvent>(guild =>
{
    Console.WriteLine($"Left guild: {guild.Id}");
    return Task.CompletedTask;
});
```

### Member Events

**GuildMemberAddEvent** - Member joined
```csharp
dispatcher.On<GuildMemberAddEvent>(member =>
{
    Console.WriteLine($"Welcome {member.User.Username}!");
    return Task.CompletedTask;
});
```

**GuildMemberUpdateEvent** - Member updated
```csharp
dispatcher.On<GuildMemberUpdateEvent>(member =>
{
    Console.WriteLine($"Member updated: {member.User.Username}");
    if (member.Nickname != null)
    {
        Console.WriteLine($"Nickname: {member.Nickname}");
    }
    return Task.CompletedTask;
});
```

**GuildMemberRemoveEvent** - Member left
```csharp
dispatcher.On<GuildMemberRemoveEvent>(member =>
{
    Console.WriteLine($"{member.User.Username} left");
    return Task.CompletedTask;
});
```

### Channel Events

**ChannelCreateEvent** - Channel created
```csharp
dispatcher.On<ChannelCreateEvent>(channel =>
{
    Console.WriteLine($"Channel created: #{channel.Name}");
    return Task.CompletedTask;
});
```

**ChannelUpdateEvent** - Channel updated
```csharp
dispatcher.On<ChannelUpdateEvent>(channel =>
{
    Console.WriteLine($"Channel updated: #{channel.Name}");
    return Task.CompletedTask;
});
```

**ChannelDeleteEvent** - Channel deleted
```csharp
dispatcher.On<ChannelDeleteEvent>(channel =>
{
    Console.WriteLine($"Channel deleted: #{channel.Name}");
    return Task.CompletedTask;
});
```

### Role Events

**GuildRoleCreateEvent** - Role created
```csharp
dispatcher.On<GuildRoleCreateEvent>(role =>
{
    Console.WriteLine($"Role created: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

**GuildRoleUpdateEvent** - Role updated
```csharp
dispatcher.On<GuildRoleUpdateEvent>(role =>
{
    Console.WriteLine($"Role updated: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

**GuildRoleDeleteEvent** - Role deleted
```csharp
dispatcher.On<GuildRoleDeleteEvent>(role =>
{
    Console.WriteLine($"Role deleted: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

### Reaction Events

**MessageReactionAddEvent** - Reaction added
```csharp
dispatcher.On<MessageReactionAddEvent>(reaction =>
{
    Console.WriteLine($"{reaction.Member.User.Username} reacted with {reaction.Emoji.Name}");
    return Task.CompletedTask;
});
```

**MessageReactionRemoveEvent** - Reaction removed
```csharp
dispatcher.On<MessageReactionRemoveEvent>(reaction =>
{
    Console.WriteLine($"Reaction removed: {reaction.Emoji.Name}");
    return Task.CompletedTask;
});
```

### Interaction Events

**InteractionCreateEvent** - Interaction received
```csharp
dispatcher.On<InteractionCreateEvent>(interaction =>
{
    Console.WriteLine($"Interaction: {interaction.Data?.Name}");
    return Task.CompletedTask;
});
```

### Voice Events

**VoiceStateUpdateEvent** - Voice state changed
```csharp
dispatcher.On<VoiceStateUpdateEvent>(voiceState =>
{
    if (voiceState.VoiceState.ChannelId.HasValue)
    {
        Console.WriteLine($"{voiceState.VoiceState.Member.User.Username} joined voice");
    }
    else
    {
        Console.WriteLine($"{voiceState.VoiceState.Member.User.Username} left voice");
    }
    return Task.CompletedTask;
});
```

---

## Event Handling Patterns

### Simple Command Response

```csharp
dispatcher.On<MessageCreateEvent>(msg =>
{
    if (msg.Content == "!ping")
    {
        return client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "🏓 Pong!",
        });
    }
    return Task.CompletedTask;
});
```

### Multiple Commands

```csharp
private async Task HandleMessage(MessageCreateEvent msg)
{
    if (msg.Author.IsBot) return;
    
    return msg.Content switch
    {
        "!ping" => client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "🏓 Pong!",
        }),
        "!hello" => client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = $"Hello, {msg.Author.Username}!",
        }),
        "!help" => SendHelpEmbed(msg.ChannelId),
        _ => Task.CompletedTask,
    };
}

dispatcher.On<MessageCreateEvent>(HandleMessage);
```

### Logging All Events

```csharp
private readonly ILogger<Program> _logger;

dispatcher.On<MessageCreateEvent>(msg =>
{
    _logger.LogInformation(
        "Message from {User} in {Channel}: {Content}",
        msg.Author.Username,
        msg.ChannelId,
        msg.Content
    );
    return Task.CompletedTask;
});

dispatcher.On<GuildMemberAddEvent>(member =>
{
    _logger.LogInformation(
        "Member joined: {User} in {Guild}",
        member.User.Username,
        member.GuildId
    );
    return Task.CompletedTask;
});
```

### Caching Real-Time Data

```csharp
private Dictionary<ulong, User> _memberCache = new();

dispatcher.On<GuildMemberAddEvent>(member =>
{
    _memberCache[member.User.Id] = member.User;
    Console.WriteLine($"Cached: {member.User.Username}");
    return Task.CompletedTask;
});

dispatcher.On<GuildMemberRemoveEvent>(member =>
{
    _memberCache.Remove(member.User.Id);
    Console.WriteLine($"Removed from cache: {member.User.Username}");
    return Task.CompletedTask;
});

public User? GetCachedMember(ulong userId)
{
    return _memberCache.TryGetValue(userId, out var user) ? user : null;
}
```

### Welcome New Members

```csharp
dispatcher.On<GuildMemberAddEvent>(async member =>
{
    // Get welcome channel
    var guild = await client.Cache.GetGuildAsync(member.GuildId);
    if (guild == null) return;
    
    var welcomeChannel = guild.Channels
        ?.FirstOrDefault(c => c.Name == "welcome");
    
    if (welcomeChannel == null) return;
    
    // Send welcome message
    var embed = new Embed
    {
        Title = $"Welcome {member.User.Username}!",
        Description = $"Glad to have you in {guild.Name}",
        Color = 0x00FF00,
        Timestamp = DateTime.UtcNow,
    };
    
    await client.Rest.CreateMessageAsync(welcomeChannel.Id, new()
    {
        Content = $"Welcome <@{member.User.Id}>!",
        Embeds = new List<Embed> { embed },
    });
});
```

---

## Error Recovery

### Automatic Reconnection

PawSharp automatically reconnects on disconnect:

```csharp
// Monitored automatically
dispatcher.On<ReadyEvent>(ready =>
{
    Console.WriteLine("Reconnected!");
    return Task.CompletedTask;
});
```

### Manual Recovery

```csharp
try
{
    await client.ConnectAsync();
}
catch (GatewayException ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
    
    // Retry with exponential backoff
    for (int attempt = 0; attempt < 5; attempt++)
    {
        try
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            Console.WriteLine($"Retrying in {delay.TotalSeconds} seconds...");
            await Task.Delay(delay);
            
            await client.ConnectAsync();
            Console.WriteLine("Connected!");
            break;
        }
        catch (GatewayException) when (attempt < 4)
        {
            // Try again
        }
    }
}
```

### Event Handling Errors

```csharp
// Wrap event handlers in try-catch
dispatcher.On<MessageCreateEvent>(async msg =>
{
    try
    {
        await ProcessMessage(msg);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing message");
        
        // Notify user of error
        await client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "❌ An error occurred",
        });
    }
});
```

---

## Advanced Usage

### Sharded Gateway

For bots in 2500+ servers:

```csharp
var options = new PawSharpOptions
{
    Token = token,
    Shards = ShardingStrategy.Auto,
};

services.AddSingleton(options).AddPawSharp();

var shardManager = provider.GetRequiredService<ShardManager>();
await shardManager.ConnectAllAsync();

// Events work across all shards automatically
dispatcher.On<MessageCreateEvent>(HandleMessage);
```

### Shard-Specific Events

```csharp
var shardManager = provider.GetRequiredService<ShardManager>();

// Get EventDispatcher for specific shard
var shard0Dispatcher = shardManager.GetShard(0)?.Gateway.EventDispatcher;

shard0Dispatcher?.On<ReadyEvent>(ready =>
{
    Console.WriteLine($"Shard 0 ready: {ready.User.Username}");
    return Task.CompletedTask;
});
```

### Performance Optimization

```csharp
// Process events asynchronously without blocking
dispatcher.Use(async (context, next) =>
{
    // Fire and forget for non-critical events
    if (context is GuildMemberAddEvent)
    {
        _ = Task.Run(async () => await next());
    }
    else
    {
        await next();
    }
});

// Batch process events
private Queue<MessageCreateEvent> _messageQueue = new();
private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

dispatcher.On<MessageCreateEvent>(async msg =>
{
    await _queueSemaphore.WaitAsync();
    try
    {
        _messageQueue.Enqueue(msg);
        
        if (_messageQueue.Count >= 10)
        {
            await ProcessBatchAsync();
        }
    }
    finally
    {
        _queueSemaphore.Release();
    }
});

private async Task ProcessBatchAsync()
{
    var batch = new List<MessageCreateEvent>();
    while (_messageQueue.Count > 0)
    {
        batch.Add(_messageQueue.Dequeue());
    }
    
    // Process batch
    foreach (var msg in batch)
    {
        // Handle message
    }
}
```

---

**More guides:** [REST API](./REST_API_GUIDE.md) | [Caching](./CACHING_GUIDE.md) | [Patterns](./PATTERNS_GUIDE.md)
