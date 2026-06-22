# Migration Guide

This guide covers breaking changes between major versions of PawSharp and how to update your code.

For detailed per-version changes, see [CHANGELOG.md](../CHANGELOG.md).

---

## Table of Contents

1. [Migrating from 0.x to 1.0.0-alpha](#migrating-from-0x-to-100-alpha)
2. [Migrating from 1.0.0-alpha.x to 1.1.0-alpha.y](#migrating-from-100-alphax-to-110-alphay)
3. [General Migration Notes](#general-migration-notes)

---

## Migrating from 0.x to 1.0.0-alpha

### Target Framework Change (.NET 8 → .NET 10)

The target framework was changed from `net8.0` to `net10.0` across all packages. You must update your project to target `net10.0`:

**Before:**
```xml
<TargetFramework>net8.0</TargetFramework>
```

**After:**
```xml
<TargetFramework>net10.0</TargetFramework>
```

### InteractionResolvedData Keys Changed from `string` to `ulong`

Discord sends resolved-data maps with snowflake string keys. The library previously used `Dictionary<string, T>` but now uses `Dictionary<ulong, T>`, so lookups use the numeric ID directly.

**Before:**
```csharp
var userId = resolvedData.Users["123456789"];
```

**After:**
```csharp
var userId = resolvedData.Users[123456789ul];
```

Affected types:
- `InteractionResolvedData.Users`
- `InteractionResolvedData.Members`
- `InteractionResolvedData.Roles`
- `InteractionResolvedData.Channels`
- `InteractionResolvedData.Messages`
- `InteractionResolvedData.Attachments`
- `ResolvedData.*` (in `PawSharp.Core.Entities`)

### `DeleteInviteAsync` Return Type Changed

**Before:** `Task<bool>`
**After:** `Task<Invite?>`

The method now returns the deleted invite object (or `null` on failure) instead of a boolean.

### `GetActiveThreadsAsync` Return Type Changed

**Before:** `Task<List<Channel>?>`
**After:** `Task<ActiveThreadsResponse?>`

The new return type exposes both `threads` and `members` arrays from the Discord response.

### REST Methods Now Throw Exceptions Instead of Returning Null

All REST methods now throw typed exceptions on failure instead of returning `null`.

**Before:**
```csharp
var message = await client.Rest.CreateMessageAsync(channelId, request);
if (message == null) { /* what went wrong? */ }
```

**After:**
```csharp
try
{
    var message = await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex) { /* input too long, etc. */ }
catch (RateLimitException ex) { /* rate limited */ }
catch (DiscordApiException ex) { /* API error */ }
```

### Archived Threads Return Type Changed

`GetPublicArchivedThreadsAsync`, `GetPrivateArchivedThreadsAsync`, and `GetJoinedPrivateArchivedThreadsAsync` now return `ArchivedThreadsResponse?` instead of `List<Channel>?`.

### Archived Threads Query Format

The `before` query-string parameter format changed from Unix epoch seconds to ISO-8601.

### HeartbeatManager Constructor

The constructor now requires an `ILogger` parameter (can be `null`).

---

## Migrating from 1.0.0-alpha.x to 1.1.0-alpha.y

### 1.0.0-alpha.2 → 1.0.0-alpha.3 — No Breaking Changes

No breaking changes were introduced in this version.

### 1.0.0-alpha.3 → 1.0.0-alpha.4 — No Breaking Changes

No breaking changes were introduced in this version.

### 1.0.0-alpha.4 → 1.1.0-alpha.1 — No Breaking Changes

No breaking changes were introduced in this version.

### 1.1.0-alpha.1 → 1.1.0-alpha.2 — No Breaking Changes

No breaking changes were introduced in this version.

### 1.1.0-alpha.2 → 1.1.0-alpha.3 — No Breaking Changes

No breaking changes were introduced in this version.

### 1.1.0-alpha.3 → 1.1.0-alpha.4

#### Removed Obsolete API Surface

The following `[Obsolete]` methods have been **removed**:

| Removed Method | Replacement |
|---------------|-------------|
| `services.AddPawSharpClient(PawSharpOptions)` | `services.SetupPawSharp(options)` or `services.AddPawSharpWithMemoryCache(options)` |
| `services.AddPawSharpClient()` | `services.SetupPawSharp(options)` or `services.AddPawSharpWithMemoryCache(options)` |

**Before:**
```csharp
services.AddPawSharpClient(options);
```

**After:**
```csharp
services.SetupPawSharp(options);
// or
services.AddPawSharpWithMemoryCache(options);
```

#### `ConfigureAwait(false)` Added Project-Wide

Every `await` call in all library projects now uses `.ConfigureAwait(false)`. This prevents deadlocks in synchronization-context-sensitive hosts (ASP.NET, WinForms, WPF) but may break code that relied on the synchronization context being captured inside event handlers or callbacks.

**If you capture `SynchronizationContext` inside PawSharp event handlers**, wrap your continuation explicitly:

```csharp
client.OnMessageCreated(async evt =>
{
    // Work that needs the original context
    await Task.Run(() => { /* ... */ });
});
```

#### HeartbeatManager `maxMissedAcks` Default Changed

The default `maxMissedAcks` in `HeartbeatManager` changed from `2` to `3`, matching the default in `PawSharpOptions.MaxMissedHeartbeatAcks`. If you were relying on the stricter 2-miss threshold, set `PawSharpOptions.MaxMissedHeartbeatAcks` explicitly.

#### `EventDispatchQueue._disposed` Now Properly Set

The `_disposed` field in `EventDispatchQueue` is now set to `true` during `Dispose()`, making the disposal guard in `EnqueueAsync()` functional. Previously, calling `EnqueueAsync()` after disposal could silently queue events on a disposed object. If you interact directly with `EventDispatchQueue`, be aware that `EnqueueAsync()` will now throw `ObjectDisposedException` after disposal.

#### `RateLimitBucket.Release()` Race Fixed

`RateLimitBucket.Release()` now uses `try/catch(SemaphoreFullException)` instead of checking `CurrentCount == 0`. Under concurrent access, the old approach could throw `SemaphoreFullException`. This is a behavioral fix and should be invisible unless you were catching `SemaphoreFullException` explicitly.

#### Voice WebSocket Protocol

`VoiceConnection` no longer hardcodes `?v=8` for the voice WebSocket URI. The version is now resolved at runtime via the `VoiceProtocolVersion` constant (currently `4`). This should be transparent for most users — the voice gateway negotiation handles versioning internally.

#### Voice `_seqAck` Now Properly Tracked

The `seq_ack` field sent in voice heartbeats (op 3) and resume payloads (op 7) is now updated from the last received RTP packet's sequence number. Previously it was always `null`. This improves resume reliability for bots receiving voice traffic.

#### Exception Handling Changes

- `PlayAudioAsync()` and `PlayAudioFromPcmAsync()` on `VoiceConnection` now throw `ObjectDisposedException` when called on a disposed connection, instead of silently succeeding.

---

## General Migration Notes

### Namespace Changes

- Component model classes (`MessageComponent`, `ActionRow`, `Button`, `SelectMenu`, `SelectOption`, `TextInput`) moved from `PawSharp.API.Models` to `PawSharp.Core.Entities`. The `PawSharp.Core.Entities` namespace is re-exported from `PawSharp.API`, so existing code may only need a `using` update.

### Type Changes

- `Message.Components` changed from `List<object>?` to `List<MessageComponent>?`
- `Message.Flags` changed from `int?` to `MessageFlags?`
- `ModalBuilder.AddTextInput` — `style` parameter changed from `int` to `TextInputStyle`
- `TextInput.Style` changed from `int` to `TextInputStyle`
- `CreateAutoModerationRuleRequest.EventType` / `TriggerType` changed from `int` to `AutoModerationEventType` / `AutoModerationTriggerType`
- `CreateStageInstanceRequest.PrivacyLevel` changed from `int?` to `StageInstancePrivacyLevel?`
- `ArchivedThreadsResponse.Threads` changed from `List<Channel>` to `List<Thread>`

### Event API Changes

- `EventDispatcher.DispatchFromJson()` → `DispatchFromJsonAsync()`
- `EventDispatcher.On()` now returns `IDisposable` for easy unsubscription
- `EventDispatcher.Use()` for middleware (no `next()` delegate — all handlers always execute after middleware completes)

### Package Upgrades

All `Microsoft.Extensions.*` packages are updated to `10.0.0` to match the .NET 10 target framework.

---

## Need Help?

If you encounter any issues during migration:

1. Check the full [CHANGELOG.md](../CHANGELOG.md) for detailed per-version changes
2. Review the documentation index at [INDEX.md](INDEX.md)
3. Open a [GitHub issue](https://github.com/M1tsumi/PawSharp/issues)
