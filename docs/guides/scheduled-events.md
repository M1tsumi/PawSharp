# Scheduled Events

Learn how to create and manage Discord guild scheduled events using PawSharp.

## Table of Contents

1. [Event Types](#event-types)
2. [Entity Model](#entity-model)
3. [Event Status Workflow](#event-status-workflow)
4. [Creating Events](#creating-events)
5. [Retrieving Events](#retrieving-events)
6. [Modifying Events](#modifying-events)
7. [Deleting Events](#deleting-events)
8. [User Interest & Subscription](#user-interest--subscription)
9. [External Event Links](#external-event-links)
10. [Gateway Events](#gateway-events)
11. [Complete Example](#complete-example)

---

## Event Types

| Entity Type | Value | Description |
|-------------|-------|-------------|
| `StageInstance` | 1 | Event in a Stage channel |
| `Voice` | 2 | Event in a Voice channel |
| `External` | 3 | Event at an external location (no channel) |

---

## Entity Model

```csharp
public class GuildScheduledEvent : DiscordEntity
{
    public ulong? ChannelId { get; set; }
    public User? Creator { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset ScheduledStartTime { get; set; }
    public DateTimeOffset? ScheduledEndTime { get; set; }
    public GuildScheduledEventPrivacyLevel PrivacyLevel { get; set; }
    public GuildScheduledEventStatus Status { get; set; }
    public GuildScheduledEventEntityType EntityType { get; set; }
    public ulong? EntityId { get; set; }
    public GuildScheduledEventEntityMetadata? EntityMetadata { get; set; }
    public ulong? CreatorId { get; set; }
    public int? UserCount { get; set; }
    public string? Image { get; set; }
}

public class GuildScheduledEventEntityMetadata
{
    public string? Location { get; set; }  // 1-100 chars, external events only
}
```

### Privacy Level

```csharp
public enum GuildScheduledEventPrivacyLevel
{
    GuildOnly = 2   // only guild members can view
}
```

⚠️ `GuildOnly` is currently the only privacy level supported by Discord.

---

## Event Status Workflow

```
                    ┌──────────────┐
                    │  Scheduled   │  (1)
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
             ┌─────►│   Active     │  (2)
             │      └──────┬───────┘
             │             │
             │             ▼
             │      ┌──────────────┐     ┌──────────────┐
             │      │  Completed   │(3)  │  Canceled    │(4)
             │      └──────────────┘     └──────────────┘
             │
             └────── (can go back to Scheduled)

```

```csharp
public enum GuildScheduledEventStatus
{
    Scheduled = 1,   // default on creation
    Active = 2,      // event is live
    Completed = 3,   // event ended
    Canceled = 4     // event canceled
}
```

❌ You cannot transition from `Completed` or `Canceled` back to `Scheduled`.
✅ You can transition from **Scheduled → Active**, **Active → Completed**, or **Scheduled → Canceled**.

---

## Creating Events

### Stage or Voice Event

```csharp
var evt = await client.Rest.CreateGuildScheduledEventAsync(guildId, new()
{
    Name = "Community Game Night",
    Description = "Join us for some fun multiplayer games!",
    ChannelId = voiceChannelId,              // required for Stage/Voice
    EntityType = 2,                          // VOICE
    ScheduledStartTime = DateTimeOffset.UtcNow.AddHours(2),
    ScheduledEndTime = DateTimeOffset.UtcNow.AddHours(4),
    PrivacyLevel = 2,                        // GUILD_ONLY
    Image = "data:image/png;base64,...",     // optional cover image
});
```

| Property | Stage | Voice | External |
|----------|-------|-------|----------|
| `ChannelId` | Required | Required | Not set |
| `EntityType` | 1 | 2 | 3 |
| `ScheduledEndTime` | Optional | Optional | **Required** |
| `EntityMetadataLocation` | Not set | Not set | **Required** |

### External Event

External events take place outside of Discord and require both a location and an end time:

```csharp
var evt = await client.Rest.CreateGuildScheduledEventAsync(guildId, new()
{
    Name = "Meetup at the Park",
    Description = "Annual community picnic!",
    EntityType = 3,                                  // EXTERNAL
    ScheduledStartTime = DateTimeOffset.UtcNow.AddDays(7),
    ScheduledEndTime = DateTimeOffset.UtcNow.AddDays(7).AddHours(3),
    EntityMetadataLocation = "Central Park, NY",
    PrivacyLevel = 2,
});
```

⚠️ External events **must** have both `ScheduledEndTime` and `EntityMetadataLocation`.

### Request Model

```csharp
public class CreateGuildScheduledEventRequest
{
    public ulong ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;       // 1-100 chars
    public string? Description { get; set; }                // 1-1000 chars
    public DateTimeOffset ScheduledStartTime { get; set; }
    public DateTimeOffset? ScheduledEndTime { get; set; }
    public int PrivacyLevel { get; set; }                   // 2 = GUILD_ONLY
    public int EntityType { get; set; }                     // 1, 2, or 3
    public string? EntityMetadataLocation { get; set; }     // external only
    public string? Image { get; set; }                      // cover image (base64)
}
```

---

## Retrieving Events

### List All Guild Events

```csharp
var events = await client.Rest.GetGuildScheduledEventsAsync(
    guildId,
    withUserCount: true);

foreach (var evt in events ?? new())
{
    Console.WriteLine($"{evt.Name} — {evt.UserCount} interested");
}
```

### Get a Single Event

```csharp
var evt = await client.Rest.GetGuildScheduledEventAsync(
    guildId, eventId,
    withUserCount: true);
```

---

## Modifying Events

```csharp
var updated = await client.Rest.ModifyGuildScheduledEventAsync(
    guildId, eventId, new()
    {
        Name = "Updated Event Name",
        Description = "New description",
        ScheduledStartTime = DateTimeOffset.UtcNow.AddDays(1),
        Status = 2,  // set to Active when starting
    });
```

### Changing Status

| Transition | `Status` Value |
|------------|----------------|
| Scheduled → Active | `2` |
| Active → Completed | `3` |
| Scheduled → Canceled | `4` |

```csharp
// Start an event
await client.Rest.ModifyGuildScheduledEventAsync(guildId, eventId, new()
{
    Status = 2  // Active
});

// Complete an event
await client.Rest.ModifyGuildScheduledEventAsync(guildId, eventId, new()
{
    Status = 3  // Completed
});

// Cancel an event
await client.Rest.ModifyGuildScheduledEventAsync(guildId, eventId, new()
{
    Status = 4  // Canceled
});
```

⚠️ You **cannot** change `EntityType` after creation. You also cannot change the event type from `External` to `Voice` (or vice versa).

---

## Deleting Events

```csharp
bool deleted = await client.Rest.DeleteGuildScheduledEventAsync(guildId, eventId);
```

❌ Only the event creator or members with `MANAGE_EVENTS` can delete events.

---

## User Interest & Subscription

### Get Interested Users

```csharp
var users = await client.Rest.GetGuildScheduledEventUsersAsync(
    guildId, eventId,
    limit: 100,
    withMember: true);

foreach (var user in users ?? new())
{
    Console.WriteLine($"{user.Username} is interested");
}
```

Pagination parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `limit` | `int?` | Max users to return (1-100) |
| `withMember` | `bool?` | Include `GuildMember` objects |
| `before` | `ulong?` | Get users before this ID |
| `after` | `ulong?` | Get users after this ID |

---

## External Event Links

External events have no channel. When users click "Interested" in the Discord client, they see the location from `EntityMetadata.Location`.

💡 Use `GuildScheduledEventEntityMetadata` to store a URL for online-external events:

```csharp
new GuildScheduledEventEntityMetadata
{
    Location = "https://twitch.tv/mystream"
}
```

---

## Gateway Events

Listen for scheduled event changes in real time:

```csharp
// Event created
client.Gateway.Events.On<GuildScheduledEventCreateEvent>(
    "GUILD_SCHEDULED_EVENT_CREATE", evt =>
{
    Console.WriteLine($"Event created: {evt.ScheduledEvent.Name}");
});

// Event updated
client.Gateway.Events.On<GuildScheduledEventUpdateEvent>(
    "GUILD_SCHEDULED_EVENT_UPDATE", evt =>
{
    var e = evt.ScheduledEvent;
    Console.WriteLine($"Event updated: {e.Name} (status: {e.Status})");
});

// Event deleted
client.Gateway.Events.On<GuildScheduledEventDeleteEvent>(
    "GUILD_SCHEDULED_EVENT_DELETE", evt =>
{
    Console.WriteLine($"Event deleted: {evt.ScheduledEvent?.Name ?? evt.Id.ToString()}");
});

// User subscribed
client.Gateway.Events.On<GuildScheduledEventUserAddEvent>(
    "GUILD_SCHEDULED_EVENT_USER_ADD", evt =>
{
    Console.WriteLine($"User {evt.UserId} is now interested in event {evt.EventId}");
});

// User unsubscribed
client.Gateway.Events.On<GuildScheduledEventUserRemoveEvent>(
    "GUILD_SCHEDULED_EVENT_USER_REMOVE", evt =>
{
    Console.WriteLine($"User {evt.UserId} is no longer interested in event {evt.EventId}");
});
```

💡 These events use the low-level `client.Gateway.Events.On<T>("EVENT_NAME", handler)` pattern — there are no convenience wrappers.

---

## Complete Example

```csharp
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.API.Models;

var client = new PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN")
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .Build();

const ulong guildId = 123456789;
const ulong voiceChannelId = 987654321;

client.OnReady(async _ =>
{
    // Create a voice event
    var evt = await client.Rest.CreateGuildScheduledEventAsync(guildId, new()
    {
        Name = "PawSharp Launch Party",
        Description = "Celebrating the beta release!",
        ChannelId = voiceChannelId,
        EntityType = 2, // VOICE
        ScheduledStartTime = DateTimeOffset.UtcNow.AddDays(7),
        ScheduledEndTime = DateTimeOffset.UtcNow.AddDays(7).AddHours(3),
    });

    Console.WriteLine($"Event created: {evt?.Name} (ID: {evt?.Id})");
});

// Track created events via gateway
client.Gateway.Events.On<GuildScheduledEventCreateEvent>(
    "GUILD_SCHEDULED_EVENT_CREATE", evt =>
{
    Console.WriteLine($"New event scheduled: {evt.ScheduledEvent.Name}");
});

// Log when users subscribe
client.Gateway.Events.On<GuildScheduledEventUserAddEvent>(
    "GUILD_SCHEDULED_EVENT_USER_ADD", evt =>
{
    Console.WriteLine($"User {evt.UserId} subscribed to event {evt.EventId}");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

**More guides:** [Auto Moderation](./auto-moderation.md) | [Gateway](../guides/gateway.md) | [REST API](../guides/sending-messages.md)
