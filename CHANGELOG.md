# Changelog

All notable changes to PawSharp are documented here.

---

## [0.5.0-alpha6] - 2026-01-08

### Added

**Gateway Resilience and Auto-Healing**

The gateway client now handles network issues gracefully without crashing:

- `GatewayState` enum implementing a proper state machine: Disconnected → Connecting → Connected → Ready → Failed
- `ReconnectionManager` with exponential backoff: starts at 1 second, doubles each attempt, caps at 16 seconds maximum
- Automatic session resumption within 45 seconds of disconnect
- Maximum of 10 reconnection attempts before permanent failure
- Events fired on reconnection attempts and failures for application awareness

**Heartbeat Acknowledgment Tracking**

Connection health is now actively monitored by tracking heartbeat ACKs:

- ACK tracking detects unhealthy connections automatically
- Connection considered zombie after 2 consecutive missed ACKs
- Automatic reconnection triggered when zombie state detected
- Heartbeat state machine properly integrated with overall gateway state

**Complete Discord Gateway Opcode Support**

All 12 Discord gateway opcodes are now properly handled:

- Opcode 0 (Dispatch) - Fully implemented for event distribution
- Opcode 1 (Heartbeat) - Handles server-initiated heartbeat requests
- Opcode 2 (Identify) - Handled internally via SendIdentifyAsync()
- Opcode 3 (Status Update) - Available via public UpdatePresenceAsync() method
- Opcode 5 (Voice Server Ping) - Handled (voice support deferred to future phase)
- Opcode 6 (Resume) - Handled internally via SendResumeAsync()
- Opcode 7 (Reconnect) - Server-requested reconnection fully implemented
- Opcode 8 (Request Guild Members) - Available via public RequestGuildMembersAsync() method
- Opcode 9 (Invalid Session) - Properly handled with session state clearing
- Opcode 10 (Hello) - Server handshake and heartbeat interval setup
- Opcode 11 (Heartbeat ACK) - Server pong response handling

**Error Recovery Mechanisms**

Transient and permanent failures are now handled distinctly:

- Transient network errors automatically trigger reconnection with exponential backoff
- Permanent failures (invalid token, invalid session) are handled cleanly without endless retry loops
- Clear error messages distinguish recoverable from permanent failures
- Comprehensive logging of all connection state transitions

### Changed

**Gateway State Management**

The `GatewayClient` now exposes state information and transitions via events:

- State machine prevents invalid connection state transitions
- `OnStateChanged` event allows applications to monitor connection lifecycle
- `OnReconnectionAttempt` event fires with attempt number for progress tracking
- `OnReconnectionFailed` event fires when all reconnection attempts have been exhausted
- `CurrentState` property allows querying current gateway state at any time

**HeartbeatManager Enhancements**

The heartbeat manager now provides better diagnostics and health monitoring:

- Constructor accepts optional `ILogger` parameter for diagnostic output
- `IsHealthy` property indicates whether connection is healthy based on ACK tracking
- `OnHeartbeatSent` event fired after each heartbeat is transmitted
- `OnHeartbeatAckReceived` event fired when server acknowledges heartbeat
- `OnZombieConnection` event fired when connection becomes unhealthy

**New Public API Methods**

Applications can now directly send opcodes 3 and 8:

- `UpdatePresenceAsync(status, game, streamUrl)` - Change bot presence and status
- `RequestGuildMembersAsync(guildId, limit, query)` - Request guild member lists for chunking

### Technical Details

New files created:
- `src/PawSharp.Gateway/GatewayState.cs` - State machine enum
- `src/PawSharp.Gateway/ReconnectionManager.cs` - Exponential backoff and reconnection logic

Files modified:
- `src/PawSharp.Gateway/GatewayClient.cs` - Major refactor for resilience, all opcodes handled
- `src/PawSharp.Gateway/Heartbeat/HeartbeatManager.cs` - ACK tracking and health monitoring
- `src/Directory.Build.props` - Version bumped to 0.5.0-alpha6

### Breaking Changes

- `HeartbeatManager` constructor now requires `ILogger` parameter (can be null)
- `GatewayClient.ConnectAsync()` now validates state and prevents reconnection while already connected

### Stability Status

Phase 2 is now complete and production-ready for stable connections:

- Automatic reconnection with exponential backoff fully functional
- Heartbeat ACK tracking prevents zombie connections effectively
- All Discord gateway opcodes handled correctly
- State machine prevents invalid transitions and crashes
- Comprehensive logging helps diagnose connection issues
- Can maintain stable connections across network interruptions

---

## [0.5.0-alpha5] - 2026-01-07

### Added

**Exception Hierarchy**

All REST methods now throw typed exceptions instead of returning null on failure:

- `DiscordException` - base class for all errors
- `DiscordApiException` - API returned an error with status code and response body
- `RateLimitException` - hit Discord's rate limit with retry-after information
- `GatewayException` - WebSocket and connection problems
- `ValidationException` - input validation failed before sending to Discord
- `DeserializationException` - couldn't parse JSON from Discord response

**Input Validation**

Input is validated before making API calls:

- `SnowflakeValidator` - validates Discord snowflake IDs
- `ContentValidator` - enforces message and embed size limits
- `EmbedValidator` - validates embed structure with field count and length checks
- `UrlValidator` - checks URL format and schemes
- All REST endpoints validate parameters before making API calls

**Bounded Caching**

In-memory cache with proper memory management:

- Per-entity type size limits (10K messages, 1K guilds, 5K users, etc.)
- LRU eviction when limits are hit
- TTL-based cleanup every 5 minutes
- Prevents unbounded memory growth
- Configurable via `MaxCacheSize` constants

**Rate Limiting**

Rate limiting fully integrated into REST client:

- `AdvancedRateLimiter` with per-route bucket tracking
- Uses X-RateLimit-Bucket headers from Discord for accurate bucket management
- Proper Retry-After parsing handling both seconds and milliseconds
- Exponential backoff on 429 responses

### Changed

**Breaking Changes**

All REST methods now throw exceptions instead of returning null on failure.

Before:
```csharp
var message = await client.Rest.CreateMessageAsync(channelId, request);
if (message == null) 
{
    // Something went wrong, but what?
}
```

After:
```csharp
try
{
    var message = await client.Rest.CreateMessageAsync(channelId, request);
    // message exists, definitely
}
catch (ValidationException ex) when (ex.Message.Contains("too long"))
{
    Console.WriteLine("Message exceeds Discord's 2000 character limit");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, wait {ex.RetryAfter} seconds");
}
catch (DiscordApiException ex)
{
    Console.WriteLine($"API returned {ex.StatusCode}: {ex.Message}");
}
```

### Technical Details

New directories:
- `src/PawSharp.Core/Exceptions/` - 6 exception classes
- `src/PawSharp.Core/Validation/` - 4 validators

Files modified:
- `src/PawSharp.API/Clients/RestClient.cs` - validation calls integrated
- `src/PawSharp.Cache/Providers/MemoryCacheProvider.cs` - bounded cache implementation
- `src/Directory.Build.props` - version bump

---

## [0.5.0-alpha4] - 2026-01-06

### Added

- Audit logs API (`GetGuildAuditLogsAsync`)
- Auto moderation endpoints (list, get, create, modify, delete)
- Request models for auto moderation
- Unit tests for new endpoints

### Changed

- Extended `IDiscordRestClient` with 12 new methods
- Maintained backwards compatibility

---

## [0.5.0-alpha3] - 2026-01-05

### Added

- Webhook support with all webhook entity types
- Audit log entities and entry types
- Comprehensive change tracking models

---

## [0.5.0-alpha2] - 2026-01-04

### Added

- Slash commands foundation
- Interaction model and handling
- Message components (buttons, select menus, modals)

---

## [0.5.0-alpha1] - 2026-01-03

### Added

- Initial public release
- WebSocket gateway connection
- Basic REST API client
- In-memory entity caching
- Message event handling

---

## Versioning

We follow Semantic Versioning:

- **0.5.x** - Alpha releases with potential breaking changes
- **1.0.0-beta** - Feature complete, fixing bugs
- **1.0.0+** - Stable releases, backwards compatible

---

## Coming Next

See [ROADMAP.md](ROADMAP.md) for the development plan:

- Phase 3: Documentation and comprehensive testing
- Phase 4: Sharding, distributed caching, and advanced features
