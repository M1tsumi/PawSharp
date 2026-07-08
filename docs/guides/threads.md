# Threads

Learn how to create, manage, and archive threads using PawSharp's REST API and Gateway events.

## Table of Contents

1. [Thread Types](#thread-types)
2. [Creating Threads](#creating-threads)
3. [Joining & Leaving Threads](#joining--leaving-threads)
4. [Thread Members](#thread-members)
5. [Archiving & Deleting Threads](#archiving--deleting-threads)
6. [Listing Threads](#listing-threads)
7. [Forum Tags & Default Reactions](#forum-tags--default-reactions)
8. [Gateway Events](#gateway-events)
9. [Complete Example](#complete-example)

---

## Thread Types

Discord supports three thread types. The type is set via the `Type` property on `CreateThreadRequest`:

| Value | Constant | Description |
|-------|----------|-------------|
| `10` | `NEWS_THREAD` | Thread in an **Announcement** channel |
| `11` | `PUBLIC_THREAD` | Anyone can view and participate |
| `12` | `PRIVATE_THREAD` | Only invited members can view/participate |

```csharp
public class CreateThreadRequest
{
 public string Name { get; set; } = string.Empty;
 public int? AutoArchiveDuration { get; set; } // 60, 1440, 4320, 10080
 public int Type { get; set; } // 10, 11, or 12
 public bool? Invitable { get; set; } // private threads only
 public int? RateLimitPerUser { get; set; }
 public CreateMessageRequest? Message { get; set; } // forum/media channels
 public List<ulong>? AppliedTags { get; set; } // forum only
}
```

---

## Creating Threads

### From a Message (public threads only)

```csharp
var thread = (Thread?)await client.Rest.CreateThreadFromMessageAsync(
 channelId,
 messageId,
 new CreateThreadRequest
 {
 Name = "Discussion about the announcement",
 AutoArchiveDuration = 1440, // 24 hours
 });
```

 Use this when you want to start a discussion branching from an existing message.

### Without a Message (standalone thread)

```csharp
var thread = (Thread?)await client.Rest.CreateThreadAsync(
 channelId,
 new CreateThreadRequest
 {
 Name = "New Private Thread",
 Type = 12, // PRIVATE_THREAD
 AutoArchiveDuration = 60, // 1 hour
 Invitable = true,
 });
```

 Use `CreateThreadAsync` for text/news channels.
 Set `Invitable = true` so non-moderators can invite other members to private threads.

### In a Forum Channel

Forum threads require an initial **message** and optional **tags**:

```csharp
var threadId = 0UL; // capture from response

var result = await client.Rest.CreateThreadInForumAsync(
 forumChannelId,
 new CreateThreadRequest
 {
 Name = "My Forum Post",
 Message = new CreateMessageRequest
 {
 Content = "This is the first post in this thread!",
 },
 AppliedTags = new List<ulong> { tagId },
 AutoArchiveDuration = 10080, // 7 days
 });

if (result is Thread thread)
 threadId = thread.Id;
```

| Property | Required | Notes |
|----------|----------|-------|
| `Name` | Yes | 1-100 characters |
| `Message` | Yes | The initial post content |
| `AppliedTags` | No | IDs of existing forum tags |
| `AutoArchiveDuration` | No | Defaults to channel setting |

 `CreateThreadInForumAsync` and `CreateThreadAsync` both call `POST /channels/{id}/threads`. The difference is that `CreateThreadInForumAsync` uses `HandleApiResponseAsync` and expects a `Message` property on the request.

---

## Joining & Leaving Threads

### Bot Joining a Thread

```csharp
bool joined = await client.Rest.JoinThreadAsync(threadChannelId);
```

### Adding a Member

```csharp
bool added = await client.Rest.AddThreadMemberAsync(threadChannelId, userId);
```

### Bot Leaving a Thread

```csharp
bool left = await client.Rest.LeaveThreadAsync(threadChannelId);
```

### Removing a Member (requires MANAGE_THREADS)

```csharp
bool removed = await client.Rest.RemoveThreadMemberAsync(threadChannelId, userId);
```

 Rate limit: Adding/removing thread members is rate-limited per-channel. A burst of >5 joins in a few seconds may return `429 Too Many Requests`.

---

## Thread Members

### Get a Specific Member

```csharp
var member = await client.Rest.GetThreadMemberAsync(threadChannelId, userId);
if (member != null)
 Console.WriteLine($"Joined at: {member.JoinTimestamp}");
```

### List All Thread Members

```csharp
var members = await client.Rest.GetThreadMembersAsync(
 threadChannelId,
 withMember: true, // include full GuildMember objects
 after: null,
 limit: 100);

foreach (var m in members ?? new())
{
 Console.WriteLine($"User {m.UserId} joined at {m.JoinTimestamp}");
}
```

 `GetThreadMembersAsync` returns an approximate list. Discord stops counting at 50 members for `member_count` on the thread object.

---

## Archiving & Deleting Threads

### Archive a Thread

Use the generic `ModifyChannelAsync` (or a dedicated `ModifyThreadRequest` helper if you have one):

```csharp
await client.Rest.ModifyChannelAsync(threadChannelId, new ModifyChannelRequest
{
 Archived = true,
 // or use the actual property:
 // (Assuming the request model supports thread-specific fields)
});
```

The actual `ModifyThreadRequest` model:

```csharp
public class ModifyThreadRequest
{
 public string? Name { get; set; }
 public bool? Archived { get; set; }
 public int? AutoArchiveDuration { get; set; }
 public bool? Locked { get; set; }
 public bool? Invitable { get; set; }
 public int? RateLimitPerUser { get; set; }
}
```

### Lock a Thread

```csharp
await client.Rest.ModifyChannelAsync(threadChannelId, new ModifyChannelRequest
{
 Locked = true,
 Archived = true,
});
```

 Locking without archiving is not allowed. Always set `Archived = true` when locking.

### Delete a Thread

```csharp
bool deleted = await client.Rest.DeleteChannelAsync(threadChannelId);
```

 Deleting a thread is permanent and cannot be undone. The thread and all messages are gone.

---

## Listing Threads

### Active Threads (guild-wide)

```csharp
var active = await client.Rest.GetActiveThreadsAsync(guildId);
if (active?.Threads is { } threads)
{
 foreach (var thread in threads.Cast<Thread>())
 Console.WriteLine($"Active: {thread.Name} ({thread.MemberCount} members)");
}
```

### Public Archived Threads

```csharp
var archived = await client.Rest.GetPublicArchivedThreadsAsync(
 channelId,
 before: DateTimeOffset.UtcNow,
 limit: 50);
```

### Private Archived Threads

```csharp
var archived = await client.Rest.GetPrivateArchivedThreadsAsync(
 channelId,
 limit: 50);
```

### Private Archived Threads the Bot Has Joined

```csharp
var joined = await client.Rest.GetJoinedPrivateArchivedThreadsAsync(
 channelId,
 limit: 50);
```

 All archived-thread endpoints are paginated. Use the `before` parameter (an ISO 8601 timestamp of the last thread's `ArchiveTimestamp`) for subsequent pages.

---

## Forum Tags & Default Reactions

Tag and default-reaction data lives on the **forum channel** object, not the thread:

```csharp
public class ForumTag
{
 public ulong Id { get; set; }
 public string Name { get; set; } = string.Empty;
 public bool Moderated { get; set; }
 public ulong? EmojiId { get; set; }
 public string? EmojiName { get; set; }
}

public class DefaultReaction
{
 public ulong? EmojiId { get; set; }
 public string? EmojiName { get; set; }
}
```

 To get available tags, fetch the forum channel and inspect its `AvailableTags` property.

---

## Gateway Events

Listen for thread-related events in real time:

```csharp
// Thread created
client.Gateway.Events.On<ThreadCreateEvent>("THREAD_CREATE", evt =>
{
 Console.WriteLine($"Thread created: {evt.Thread.Name}");
 return Task.CompletedTask;
});

// Thread updated
client.Gateway.Events.On<ThreadUpdateEvent>("THREAD_UPDATE", evt =>
{
 Console.WriteLine($"Thread updated: {evt.Thread.Name}");
 return Task.CompletedTask;
});

// Thread deleted
client.Gateway.Events.On<ThreadDeleteEvent>("THREAD_DELETE", evt =>
{
 Console.WriteLine($"Thread {evt.Id} deleted");
 return Task.CompletedTask;
});

// Thread list synced (on startup / after reconnect)
client.Gateway.Events.On<ThreadListSyncEvent>("THREAD_LIST_SYNC", evt =>
{
 Console.WriteLine($"Synced {evt.Threads?.Count ?? 0} threads");
 return Task.CompletedTask;
});

// Member updated (e.g., joined/left)
client.Gateway.Events.On<ThreadMemberUpdateEvent>("THREAD_MEMBER_UPDATE", evt =>
{
 Console.WriteLine($"Thread member updated for user {evt.Member?.UserId}");
 return Task.CompletedTask;
});
```

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

client.OnMessageCreated(async msg =>
{
 if (msg.Content != "!createthread") return;

 // Create a public thread from the message
 var thread = (Thread?)await client.Rest.CreateThreadFromMessageAsync(
 msg.ChannelId,
 msg.Id,
 new CreateThreadRequest
 {
 Name = "Discussion",
 AutoArchiveDuration = 1440,
 });

 if (thread != null)
 {
 await client.CreateMessageAsync(msg.ChannelId,
 $"Thread created: <#{thread.Id}>");

 // Join the thread
 await client.Rest.JoinThreadAsync(thread.Id);

 // Add the message author to the thread
 await client.Rest.AddThreadMemberAsync(thread.Id, msg.Author.Id);
 }
});

// Handle thread deletion via gateway
client.Gateway.Events.On<ThreadDeleteEvent>("THREAD_DELETE", evt =>
{
 Console.WriteLine($"Thread {evt.Id} was deleted");
 return Task.CompletedTask;
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

## Rate Limit Notes

 **Thread operations are subject to channel-level rate limits:**

| Operation | Notes |
|-----------|-------|
| `CreateThreadAsync` | 1 per 5 seconds per channel (burst of 2) |
| `JoinThreadAsync` | 5 per channel per few seconds |
| `AddThreadMemberAsync` | Same as join |
| `ModifyChannelAsync` (archive/lock) | 2 per 10 minutes per channel |
| Marking as `Archived = false` (unarchive) | Resets auto-archive timer; 1 per 5 seconds |

 Always handle `429 Too Many Requests` responses. PawSharp's built-in `IAdvancedRateLimiter` handles pre-emptive rate limiting automatically when using `DiscordRestClient`.

---

**More guides:** [Webhooks](./webhooks.md) | [REST API](../guides/sending-messages.md) | [Gateway](../guides/gateway.md)
