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

### Event Subscription — Two APIs

PawSharp offers two complementary ways to subscribe to gateway events:

**Option A — `DiscordClient` convenience methods (recommended)**  
Strongly-typed, no magic strings, returns `IDisposable` for easy cleanup:

```csharp
// Each method returns IDisposable. Dispose it to unsubscribe.
client.OnMessageCreated(async msg =>
{
    Console.WriteLine($"Message: {msg.Content}");
});

// Store the subscription to unsubscribe later
using var sub = client.OnGuildMemberJoined(async member =>
{
    Console.WriteLine($"Welcome {member.User.Username}!");
});

// Convenience method list (full list in docs/DEVELOPERS_GUIDE.md):
// OnReady, OnMessageCreated, OnMessageUpdated, OnMessageDeleted,
// OnReactionAdded, OnReactionRemoved,
// OnGuildAvailable, OnGuildUpdated, OnGuildUnavailable,
// OnGuildMemberJoined, OnGuildMemberUpdated, OnGuildMemberLeft,
// OnChannelCreated, OnChannelUpdated, OnChannelDeleted,
// OnRoleCreated, OnRoleUpdated, OnRoleDeleted,
// OnBanAdded, OnBanRemoved, OnTypingStarted, OnInteractionCreated, ...
```

**Option B — Low-level `EventDispatcher` (advanced use)**  
Requires both the event *type* and the Discord *event name string*:

```csharp
// Access via client.Gateway.Events (NOT .EventDispatcher)
var dispatcher = client.Gateway.Events;

dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", async msg =>
{
    Console.WriteLine($"Message: {msg.Content}");
});

// Sync handler (no await needed)
dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", msg =>
{
    Console.WriteLine(msg.Content);
});
```

### Connection Lifecycle

```
Connecting → Connected → Ready → Events Flow → Disconnect → Reconnecting
```

---

## Subscribing to Events

> **Recommended:** Use the `DiscordClient` convenience methods shown below. They are strongly-typed,
> require no magic strings, and return `IDisposable` for clean unsubscription. Use the low-level
> `client.Gateway.Events` dispatcher only when you need an event that has no convenience method.

### Recommended — DiscordClient Convenience Methods

```csharp
// Lambda — returns IDisposable; dispose to unsubscribe
client.OnMessageCreated(async msg =>
{
    if (!msg.Author.IsBot)
        Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
});

// Named method
client.OnMessageCreated(HandleMessageAsync);

// Store subscription for later cleanup
IDisposable sub = client.OnGuildMemberJoined(WelcomeMemberAsync);
// ...later:
sub.Dispose();

private async Task HandleMessageAsync(MessageCreateEvent msg)
{
    if (msg.Content == "!ping")
        await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "🏓 Pong!" });
}

private async Task WelcomeMemberAsync(GuildMemberAddEvent member)
{
    Console.WriteLine($"Welcome {member.User.Username}!");
}
```

### Multiple Event Subscriptions

```csharp
// Subscribe to several events — all return IDisposable
client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

client.OnMessageCreated(async msg =>
{
    // Handle new messages
});

client.OnGuildMemberJoined(async member =>
{
    // Welcome new members
});

client.OnGuildMemberLeft(async member =>
{
    // Log departures
});
```

### Low-Level EventDispatcher (advanced)

Use `client.Gateway.Events` when you need fine-grained control or an event with no convenience wrapper:

```csharp
var dispatcher = client.Gateway.Events;  // NOTE: .Events, not .EventDispatcher

// Async handler — provide both the type AND the Discord event name string
dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", async msg =>
{
    await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Received!" });
});

// Sync handler
dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", msg =>
{
    Console.WriteLine(msg.Content);
});

// Named method (sync or async)
dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", HandleMessageAsync);

// Unsubscribe via IDisposable
using var sub = dispatcher.On<ReadyEvent>("READY", HandleReadyAsync);
```

**Common event name strings:**

| Event type | String |
|---|---|
| `ReadyEvent` | `"READY"` |
| `MessageCreateEvent` | `"MESSAGE_CREATE"` |
| `MessageUpdateEvent` | `"MESSAGE_UPDATE"` |
| `MessageDeleteEvent` | `"MESSAGE_DELETE"` |
| `GuildCreateEvent` | `"GUILD_CREATE"` |
| `GuildMemberAddEvent` | `"GUILD_MEMBER_ADD"` |
| `GuildMemberRemoveEvent` | `"GUILD_MEMBER_REMOVE"` |
| `InteractionCreateEvent` | `"INTERACTION_CREATE"` |
| `VoiceStateUpdateEvent` | `"VOICE_STATE_UPDATE"` |

### Middleware (pre-dispatch hook)

Middleware registered with `dispatcher.Use(...)` runs **before** event handlers for every dispatched event. The signature is `Func<string eventName, object eventData, Task>`. Note: middleware does **not** have a `next()` delegate — all registered handlers always execute after middleware completes.

```csharp
var dispatcher = client.Gateway.Events;

// Log every event name before it dispatches
dispatcher.Use(async (eventName, eventData) =>
{
    Console.WriteLine($"[EVENT] {eventName}");
    await Task.CompletedTask;
});

// Filter — record bot messages to audit log (handlers still fire; use this for side effects)
dispatcher.Use(async (eventName, eventData) =>
{
    if (eventName == "MESSAGE_CREATE" && eventData is MessageCreateEvent msg)
    {
        if (msg.Author?.IsBot == true)
            await _auditLog.RecordBotMessageAsync(msg);
    }
});

### Best practices for event handlers

- Keep handlers short and non-blocking — offload heavy work to a background processor or queue.

```csharp
// Background worker using System.Threading.Channels
var workQueue = System.Threading.Channels.Channel.CreateUnbounded<Func<Task>>();
_ = Task.Run(async () =>
{
    await foreach (var work in workQueue.Reader.ReadAllAsync())
    {
        try { await work(); }
        catch (Exception ex) { logger.LogError(ex, "Background task failed"); }
    }
});

client.OnMessageCreated(msg =>
{
    // Enqueue expensive processing; keep handler fast
    workQueue.Writer.TryWrite(() => ProcessMessageAsync(msg));
    return Task.CompletedTask;
});
```

- Dispose subscription tokens to avoid handler leaks:

```csharp
IDisposable sub = client.OnGuildMemberJoined(WelcomeMemberAsync);
// ...later
sub.Dispose();
```

- Avoid blocking calls like `.Result` or `.Wait()` inside handlers — prefer async/await.

- Be mindful of intents: handlers that rely on message content require `GatewayIntents.MessageContent`.

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
client.OnReady(ready =>
{
    Console.WriteLine($"✅ Ready as {ready.User.Username}");
    Console.WriteLine($"   Guilds: {ready.Guilds.Count}");
    return Task.CompletedTask;
});

// Session resumed after disconnect (no convenience wrapper — use low-level dispatcher)
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
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

> All examples below use the `DiscordClient` convenience methods. For events without a convenience
> wrapper, use `client.Gateway.Events.On<TEvent>("EVENT_NAME", handler)`.

### Connection Events

**`ReadyEvent`** — Bot authenticated and ready
```csharp
client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}#{ready.User.Discriminator}");
    Console.WriteLine($"Serving {ready.Guilds.Count} guild(s)");
    return Task.CompletedTask;
});
```

**`ResumedEvent`** — Session resumed after disconnect (no convenience wrapper)
```csharp
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Session resumed — reconnected successfully.");
    return Task.CompletedTask;
});
```

---

### Message Events

**`MessageCreateEvent`** — New message posted
```csharp
client.OnMessageCreated(msg =>
{
    if (!msg.Author.IsBot)
        Console.WriteLine($"{msg.Author.Username}: {msg.Content}");
    return Task.CompletedTask;
});
```

**`MessageUpdateEvent`** — Message edited
```csharp
client.OnMessageUpdated(msg =>
{
    Console.WriteLine($"Message {msg.Id} edited");
    if (msg.Content != null)
        Console.WriteLine($"  New content: {msg.Content}");
    return Task.CompletedTask;
});
```

**`MessageDeleteEvent`** — Single message deleted
```csharp
client.OnMessageDeleted(msg =>
{
    Console.WriteLine($"Message {msg.Id} deleted in channel {msg.ChannelId}");
    return Task.CompletedTask;
});
```

**`MessageDeleteBulkEvent`** — Bulk message delete
```csharp
client.OnMessagesBulkDeleted(bulk =>
{
    Console.WriteLine($"{bulk.Ids.Count} messages bulk-deleted in channel {bulk.ChannelId}");
    return Task.CompletedTask;
});
```

---

### Guild Events

**`GuildCreateEvent`** — Bot joined a guild (or guild became available on startup)
```csharp
client.OnGuildAvailable(guild =>
{
    Console.WriteLine($"Guild available: {guild.Name} ({guild.MemberCount} members)");
    return Task.CompletedTask;
});
```

**`GuildUpdateEvent`** — Guild settings changed
```csharp
client.OnGuildUpdated(guild =>
{
    Console.WriteLine($"Guild updated: {guild.Name}");
    return Task.CompletedTask;
});
```

**`GuildDeleteEvent`** — Bot removed from guild or guild went unavailable
```csharp
client.OnGuildUnavailable(guild =>
{
    Console.WriteLine($"Guild unavailable: {guild.Id}");
    return Task.CompletedTask;
});
```

---

### Member Events

**`GuildMemberAddEvent`** — Member joined
```csharp
client.OnGuildMemberJoined(async member =>
{
    Console.WriteLine($"Welcome {member.User.Username} to guild {member.GuildId}!");
});
```

**`GuildMemberUpdateEvent`** — Member's roles/nickname changed
```csharp
client.OnGuildMemberUpdated(member =>
{
    Console.WriteLine($"Member updated: {member.User.Username}");
    if (member.Nickname != null)
        Console.WriteLine($"  Nickname: {member.Nickname}");
    return Task.CompletedTask;
});
```

**`GuildMemberRemoveEvent`** — Member left or was kicked/banned
```csharp
client.OnGuildMemberLeft(member =>
{
    Console.WriteLine($"{member.User.Username} left guild {member.GuildId}");
    return Task.CompletedTask;
});
```

---

### Channel Events

**`ChannelCreateEvent`** — Channel created
```csharp
client.OnChannelCreated(channel =>
{
    Console.WriteLine($"Channel created: #{channel.Name}");
    return Task.CompletedTask;
});
```

**`ChannelUpdateEvent`** — Channel settings changed
```csharp
client.OnChannelUpdated(channel =>
{
    Console.WriteLine($"Channel updated: #{channel.Name}");
    return Task.CompletedTask;
});
```

**`ChannelDeleteEvent`** — Channel deleted
```csharp
client.OnChannelDeleted(channel =>
{
    Console.WriteLine($"Channel deleted: #{channel.Name}");
    return Task.CompletedTask;
});
```

---

### Role Events

**`GuildRoleCreateEvent`** — Role created
```csharp
client.OnRoleCreated(role =>
{
    Console.WriteLine($"Role created: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

**`GuildRoleUpdateEvent`** — Role settings changed
```csharp
client.OnRoleUpdated(role =>
{
    Console.WriteLine($"Role updated: @{role.Role.Name}");
    return Task.CompletedTask;
});
```

**`GuildRoleDeleteEvent`** — Role deleted
```csharp
client.OnRoleDeleted(role =>
{
    Console.WriteLine($"Role deleted: {role.RoleId}");
    return Task.CompletedTask;
});
```

---

### Reaction Events

**`MessageReactionAddEvent`** — Reaction added to a message
```csharp
client.OnReactionAdded(reaction =>
{
    Console.WriteLine($"{reaction.Member?.User.Username} reacted with {reaction.Emoji.Name}");
    return Task.CompletedTask;
});
```

**`MessageReactionRemoveEvent`** — Reaction removed
```csharp
client.OnReactionRemoved(reaction =>
{
    Console.WriteLine($"Reaction {reaction.Emoji.Name} removed from message {reaction.MessageId}");
    return Task.CompletedTask;
});
```

---

### Interaction Events

**`InteractionCreateEvent`** — Slash command/component/modal interaction received
```csharp
client.OnInteractionCreated(async interaction =>
{
    Console.WriteLine($"Interaction received: {interaction.Data?.Name} (type {interaction.Type})");
    // Normally handled automatically by client.Interactions — see DEVELOPERS_GUIDE.md
});
```

---

### Voice Events

**`VoiceStateUpdateEvent`** — User joined/moved/left a voice channel
```csharp
client.OnVoiceStateUpdated(voiceState =>
{
    if (voiceState.ChannelId.HasValue)
        Console.WriteLine($"User {voiceState.UserId} joined/moved to voice channel {voiceState.ChannelId}");
    else
        Console.WriteLine($"User {voiceState.UserId} left voice");
    return Task.CompletedTask;
});
```

---

### Other Events (low-level access)

For events without a dedicated convenience method, use `client.Gateway.Events` directly:

```csharp
var dispatcher = client.Gateway.Events;

// Typing indicator
dispatcher.On<TypingStartEvent>("TYPING_START", typing =>
{
    Console.WriteLine($"User {typing.UserId} is typing in {typing.ChannelId}");
    return Task.CompletedTask;
});

// Invite created
dispatcher.On<InviteCreateEvent>("INVITE_CREATE", invite =>
{
    Console.WriteLine($"Invite created: {invite.Code}");
    return Task.CompletedTask;
});

// Scheduled event created
dispatcher.On<GuildScheduledEventCreateEvent>("GUILD_SCHEDULED_EVENT_CREATE", evt =>
{
    Console.WriteLine($"Scheduled event: {evt.ScheduledEvent.Name}");
    return Task.CompletedTask;
});

// Auto-moderation action executed
dispatcher.On<AutoModerationActionExecutionEvent>("AUTO_MODERATION_ACTION_EXECUTION", action =>
{
    Console.WriteLine($"Auto-mod fired rule {action.RuleId} in guild {action.GuildId}");
    return Task.CompletedTask;
});
```
---

## Event Handling Patterns

### Simple Command Response

```csharp
client.OnMessageCreated(msg =>
{
    if (msg.Content == "!ping")
        return client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "🏓 Pong!" });
    return Task.CompletedTask;
});
```

### Multiple Commands (switch expression)

```csharp
client.OnMessageCreated(async msg =>
{
    if (msg.Author.IsBot) return;

    switch (msg.Content?.Split(' ')[0])
    {
        case "!ping":
            await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "🏓 Pong!" });
            break;
        case "!hello":
            await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = $"Hello, {msg.Author.Username}!" });
            break;
        case "!help":
            await SendHelpEmbedAsync(msg.ChannelId);
            break;
    }
});
```

### Structured Logging

```csharp
client.OnMessageCreated(msg =>
{
    _logger.LogInformation(
        "Message from {User} in {Channel}: {Content}",
        msg.Author.Username, msg.ChannelId, msg.Content);
    return Task.CompletedTask;
});

client.OnGuildMemberJoined(member =>
{
    _logger.LogInformation(
        "Member joined: {User} in guild {Guild}",
        member.User.Username, member.GuildId);
    return Task.CompletedTask;
});
```

### Welcome New Members

```csharp
client.OnGuildMemberJoined(async member =>
{
    var guild = await client.Rest.GetGuildAsync(member.GuildId);
    if (guild == null) return;

    var welcomeChannel = guild.Channels?.FirstOrDefault(c => c.Name == "welcome");
    if (welcomeChannel == null) return;

    var embed = new EmbedBuilder()
        .WithTitle($"Welcome {member.User.Username}!")
        .WithDescription($"Glad to have you in **{guild.Name}**!")
        .WithColor(0x2ECC71)
        .WithTimestamp()
        .Build();

    await client.Rest.CreateMessageAsync(welcomeChannel.Id, new()
    {
        Content = $"<@{member.User.Id}>",
        Embeds = new List<Embed> { embed },
    });
});
```

### Caching Custom Data from Events

```csharp
private readonly ConcurrentDictionary<ulong, User> _memberCache = new();

// Populate cache as members join
client.OnGuildMemberJoined(member =>
{
    _memberCache[member.User.Id] = member.User;
    return Task.CompletedTask;
});

// Remove from cache when members leave
client.OnGuildMemberLeft(member =>
{
    _memberCache.TryRemove(member.User.Id, out _);
    return Task.CompletedTask;
});

public User? GetCachedMember(ulong userId)
    => _memberCache.TryGetValue(userId, out var user) ? user : null;
```

---

## Error Recovery

### Automatic Reconnection

PawSharp automatically handles reconnection with exponential backoff. Subscribe to `OnReady` to detect when the session is restored:

```csharp
// Fires both on initial connect and after a successful reconnect
client.OnReady(ready =>
{
    Console.WriteLine($"Session ready: {ready.User.Username} in {ready.Guilds.Count} guilds");
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
client.OnMessageCreated(async msg =>
{
    try
    {
        await ProcessMessageAsync(msg);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing message {MessageId}", msg.Id);
        await client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "❌ An unexpected error occurred.",
        });
    }
});
```

---

## Advanced Usage

### Sharded Gateway

For bots in 2500+ servers, enable auto-sharding:

```csharp
var options = new PawSharpOptions
{
    Token = token,
    Shards = ShardingStrategy.Auto,
};

services.AddSingleton(options).AddPawSharp();

var shardManager = provider.GetRequiredService<ShardManager>();
await shardManager.ConnectAllAsync();

// Register events on the unified DiscordClient — they fire for all shards
client.OnMessageCreated(HandleMessageAsync);
client.OnGuildMemberJoined(HandleMemberJoinAsync);
```

### Shard-Specific Events

Access a specific shard's low-level dispatcher via `client.Gateway.Events` on the shard's `GatewayClient`:

```csharp
var shardManager = provider.GetRequiredService<ShardManager>();

// Get the low-level EventDispatcher for shard 0
var shard0Gateway = shardManager.GetShard(0)?.Gateway;
var shard0Dispatcher = shard0Gateway?.Events;  // .Events, not .EventDispatcher

shard0Dispatcher?.On<ReadyEvent>("READY", ready =>
{
    Console.WriteLine($"Shard 0 ready: {ready.User.Username}");
    return Task.CompletedTask;
});
```

### Batch Processing Events

```csharp
private readonly ConcurrentQueue<MessageCreateEvent> _messageQueue = new();
private readonly SemaphoreSlim _batchLock = new(1, 1);

client.OnMessageCreated(async msg =>
{
    _messageQueue.Enqueue(msg);

    if (_messageQueue.Count >= 10)
    {
        await _batchLock.WaitAsync();
        try { await FlushBatchAsync(); }
        finally { _batchLock.Release(); }
    }
});

private async Task FlushBatchAsync()
{
    var batch = new List<MessageCreateEvent>();
    while (_messageQueue.TryDequeue(out var msg))
        batch.Add(msg);

    foreach (var msg in batch)
        await ProcessMessageAsync(msg);
}
```

---

**More guides:** [REST API](./REST_API_GUIDE.md) | [Caching](./CACHING_GUIDE.md) | [Patterns](./PATTERNS_GUIDE.md)
