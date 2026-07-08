# Events

## Subscribing to Events

PawSharp provides convenience methods on `DiscordClient` for every major event. Each returns `IDisposable`:

```csharp
client.OnMessageCreated(async msg =>
{
    Console.WriteLine($"Message: {msg.Content}");
});

using var sub = client.OnGuildMemberJoined(async member =>
{
    Console.WriteLine($"Welcome {member.User.Username}!");
});
// Dispose to unsubscribe
sub.Dispose();
```

### Low-Level EventDispatcher

For events without a convenience wrapper:

```csharp
var dispatcher = client.Gateway.Events;
dispatcher.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Connection resumed");
    return Task.CompletedTask;
});
```

### Middleware

Middleware runs before all event handlers:

```csharp
client.Gateway.Events.Use(async (eventName, eventData) =>
{
    Console.WriteLine($"[EVENT] {eventName}");
    await Task.CompletedTask;
});
```

## Event Reference

Connection events:
- **OnReady** — Bot authenticated and ready
- **ResumedEvent** — Session resumed after disconnect (low-level only)

Message events:
- **OnMessageCreated** — New message
- **OnMessageUpdated** — Message edited
- **OnMessageDeleted** — Message deleted
- **OnMessagesBulkDeleted** — Bulk delete

Guild events:
- **OnGuildAvailable** — Guild available (on startup or join)
- **OnGuildUpdated** — Guild settings changed
- **OnGuildUnavailable** — Guild went unavailable

Member events:
- **OnGuildMemberJoined** — Member joined
- **OnGuildMemberUpdated** — Member's roles/nickname changed
- **OnGuildMemberLeft** — Member left/kicked/banned

Channel events:
- **OnChannelCreated**, **OnChannelUpdated**, **OnChannelDeleted**

Role events:
- **OnRoleCreated**, **OnRoleUpdated**, **OnRoleDeleted**

Reaction events:
- **OnReactionAdded**, **OnReactionRemoved**

Interaction events:
- **OnInteractionCreated** — Slash command/component/modal

Voice events:
- **OnVoiceStateUpdated** — User joined/moved/left voice

## Best Practices

- Keep handlers short and non-blocking
- Offload heavy work to background processors
- Dispose subscription tokens to avoid leaks
- Wrap handlers in try/catch for resilience
- Enable only the intents you need
