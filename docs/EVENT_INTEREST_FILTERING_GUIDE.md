# Event Interest Filtering System

## Overview

The Event Interest Filtering System is a PawSharp feature that enables automatic validation of gateway intents against registered event handlers. It helps developers catch configuration bugs early by warning when a handler expects an event type but the required Discord intent is not enabled.

## The Problem

Without this system, developers might:

```csharp
var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.Guilds) // Oops! Forgot MessageContent
    .Build();

client.OnMessageCreated(async msg =>
{
    // This will NEVER fire because GuildMessages intent is missing
    // And msg.Content will be null anyway (requires MessageContent intent)
    Console.WriteLine(msg.Content);
});

await client.ConnectAsync(); // Silently fails to receive MESSAGE_CREATE events
```

The bot would compile and run without errors, but mysteriously wouldn't receive message events. This is a silent failure that's hard to debug.

## The Solution

### 1. Declare Event Interests

Use the `EventInterestAttribute` to declare which events a handler (or class of handlers) is interested in:

```csharp
[EventInterest("MESSAGE_CREATE", "MESSAGE_UPDATE", "MESSAGE_DELETE")]
public class MyMessageHandlers
{
    public async Task SetupAsync(DiscordClient client)
    {
        client.OnMessageCreated(async msg => { /* ... */ });
        client.OnMessageUpdated(async msg => { /* ... */ });
        client.OnMessageDeleted(async msg => { /* ... */ });
    }
}
```

### 2. Validate Intent Configuration

Call `ValidateIntents()` before connecting:

```csharp
var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged) // Missing: GuildMembers
    .Build();

// Register handlers...
var handlers = new MyMessageHandlers();
await handlers.SetupAsync(client);

// Validate before connecting
var result = client.ValidateIntents();

if (!result.IsValid)
{
    Console.WriteLine($"Intent issues found: {result.Count}");
    foreach (var (eventType, required, missing) in result.Issues)
    {
        Console.WriteLine($"  {eventType} needs {missing}");
    }
}
else
{
    await client.ConnectAsync();
}
```

### 3. Handler-Level Declarations (Optional)

You can also declare interests at the method level for documentation:

```csharp
[EventInterest("MESSAGE_CREATE")]
public async Task OnMessageAsync(MessageCreateEvent msg)
{
    if (msg.Content == "!ping")
        await RespondAsync("Pong! 🏓");
}
```

## How It Works

### Intent Mapping

The system automatically maps event types to required intents:

| Event | Required Intent(s) |
|-------|-------------------|
| `MESSAGE_CREATE` | `GuildMessages`, `MessageContent` |
| `GUILD_MEMBER_ADD` | `GuildMembers` |
| `PRESENCE_UPDATE` | `GuildPresences` |
| `READY` | (none) |

This mapping is built into `EventInterestAttribute` using the official Discord intent documentation.

### Validation Flow

1. **At declaration time:** `EventInterestAttribute` captures event type names
2. **During mapping:** Event types are converted to required `GatewayIntents` using a mapping table
3. **Before connection:** `ValidateIntents()` compares registered handler intents vs enabled intents
4. **On mismatch:** Logs detailed warnings showing which events need which intents
5. **Recommendations:** Provides suggested intent configuration for auto-remediation

## API Reference

### EventInterestAttribute

```csharp
[EventInterest("MESSAGE_CREATE", "MESSAGE_UPDATE")]
public class MyHandler { }
```

**Properties:**
- `EventTypes`: IReadOnlySet<string> — event type names
- `RequiredIntents`: GatewayIntents — calculated required intents

**Supports:**
- Class-level declarations (applies to all methods)
- Method-level declarations (specific to that handler)
- Multiple event types in one attribute

### DiscordClient Extensions

#### ValidateIntents()

```csharp
IntentValidationResult result = client.ValidateIntents();

if (!result.IsValid)
{
    Console.WriteLine($"Found {result.Count} issues");
    foreach (var (event, required, missing) in result.Issues)
        Console.WriteLine($"{event} missing {missing}");
}
```

**Returns:** `IntentValidationResult` with detailed issues

**Use cases:**
- Early validation before `ConnectAsync()`
- Diagnostic checks during testing
- Auto-remediation logic

#### GetRecommendedIntents()

```csharp
GatewayIntents recommended = client.GetRecommendedIntents();
Console.WriteLine($"Suggested intents: {recommended}");
```

**Returns:** Minimal intent set for all registered handlers

**Use cases:**
- Documenting required intents
- Suggesting configuration to users
- Comparing with enabled intents

#### LogIntentSummary()

```csharp
client.LogIntentSummary(); // Writes to IDiscordLogger + Console
```

**Outputs:**
- Enabled vs recommended intents
- List of registered event types
- Status (OK/WARN) for each event

## Best Practices

### 1. Validate Early

Always validate intents after registering handlers but before connecting:

```csharp
await handlers.RegisterAsync(client);

if (!client.ValidateIntents().IsValid)
{
    Console.WriteLine("Fix intent configuration before starting");
    return;
}

await client.ConnectAsync();
```

### 2. Use Class-Level Declarations

Group related handlers and declare their interests at the class level:

```csharp
[EventInterest("GUILD_MEMBER_ADD", "GUILD_MEMBER_REMOVE")]
public class MembershipHandlers
{
    public async Task OnJoinAsync(GuildMemberJoinEvent evt) { }
    public async Task OnLeaveAsync(GuildMemberLeftEvent evt) { }
}
```

### 3. Graceful Degradation

When intents are missing, disable related features rather than failing completely:

```csharp
var result = client.ValidateIntents();

if (result.Issues.Any(i => i.EventType == "GUILD_MEMBER_ADD"))
{
    Console.WriteLine("⚠️  Member tracking disabled (GuildMembers intent missing)");
    // Skip registering member-related handlers
}
else
{
    await memberHandlers.RegisterAsync(client);
}
```

### 4. Log Diagnostics at Startup

Call `LogIntentSummary()` during initialization to help with troubleshooting:

```csharp
await client.ConnectAsync();
client.LogIntentSummary();
Console.WriteLine("Bot started successfully");
```

## Common Issues

### Issue: "Intent validation failed"

**Solution:** Check the detailed issues output:

```csharp
var result = client.ValidateIntents();
foreach (var (event, required, missing) in result.Issues)
    Console.WriteLine($"Fix: {event} needs {missing}");
```

### Issue: Why MessageContent intent?

`MessageContent` is a special privileged intent required to read message text (`msg.Content`). Without it, content is empty even if you have `GuildMessages`.

```csharp
.WithIntents(GatewayIntents.GuildMessages | GatewayIntents.MessageContent)
```

### Issue: Getting multiple warnings for same intent

Multiple event types may require the same intent. This is normal:

```
⚠️  MESSAGE_CREATE needs GuildMessages
⚠️  MESSAGE_UPDATE needs GuildMessages
```

## Future Optimizations

The Event Interest Filtering System is designed to enable these optimizations in future releases:

1. **Handler-level filtering** — Skip dispatching events to handlers that didn't declare interest
2. **Memory optimization** — Don't cache events that no handler is interested in
3. **Event batching** — Group related events for bulk processing
4. **Intent detection** — Automatically detect required intents from handler signatures

## See Also

- [PawSharp Gateway Guide](GATEWAY_GUIDE.md)
- [Discord Intents Documentation](https://discord.com/developers/docs/topics/gateway#gateway-intents)
- [EventInterestAttribute API](../../src/PawSharp.Core/Events/EventInterestAttribute.cs)
- [IntentFilteringExample](../../examples/IntentFilteringExample.cs)
