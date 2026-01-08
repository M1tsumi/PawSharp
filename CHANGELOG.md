# Changelog

All notable changes to PawSharp.

---

## [0.5.0-alpha5] - 2026-01-07

### Added

**Exception Hierarchy**

Never wonder what went wrong again:

- `DiscordException` — base class for all errors
- `DiscordApiException` — API returned an error (includes status code and response body)
- `RateLimitException` — hit Discord's rate limit (includes retry-after in seconds)
- `GatewayException` — WebSocket/connection problems
- `ValidationException` — input validation failed before sending to Discord
- `DeserializationException` — couldn't parse JSON from Discord

All REST methods now throw these instead of returning null. Makes debugging way easier.

**Input Validation**

Catch issues before they get to Discord:

- `SnowflakeValidator` — validates Discord snowflake IDs
- `ContentValidator` — enforces message and embed size limits
- `EmbedValidator` — validates embed structure (field counts, lengths, etc.)
- `UrlValidator` — checks URL format and schemes
- All REST endpoints validate parameters before making API calls

**Bounded Caching**

Cache that doesn't leak memory:

- Per-entity type size limits (10K messages, 1K guilds, 5K users, etc.)
- LRU eviction when limits are hit
- TTL-based cleanup every 5 minutes
- Prevents unbounded memory growth
- Configurable via `MaxCacheSize` constants

**Rate Limiting**

Actually works now:

- `AdvancedRateLimiter` fully integrated into REST client
- Per-route bucket tracking using X-RateLimit-Bucket headers from Discord
- Proper Retry-After parsing (handles both seconds and milliseconds)
- Exponential backoff on 429 responses

### Changed

**Breaking Changes ⚠️**

All REST methods now throw exceptions instead of returning null on failure. If you check for null, update to catch exceptions instead.

**Before:**
```csharp
var message = await client.Rest.CreateMessageAsync(channelId, request);
if (message == null) 
{
    // Something went wrong, but what?
}
```

**After:**
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
- `src/PawSharp.Core/Exceptions/` — 6 exception classes
- `src/PawSharp.Core/Validation/` — 4 validators

Modified files:
- `src/PawSharp.API/Clients/RestClient.cs` — validation calls integrated
- `src/PawSharp.Cache/Providers/MemoryCacheProvider.cs` — bounded cache implementation
- `src/Directory.Build.props` — version bump

All tests updated. New test cases for validation scenarios.

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

- **0.5.x** — Alpha releases, breaking changes happen
- **1.0.0-beta** — Feature complete, fixing bugs
- **1.0.0+** — Stable releases, backwards compatible

---

## Coming Next

See [ROADMAP.md](ROADMAP.md) for what we're building:

- Phase 2: Gateway resilience and auto-reconnection
- Phase 3: Documentation and more tests
- Phase 4: Sharding, Redis caching, voice support
