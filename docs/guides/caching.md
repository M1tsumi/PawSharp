# Caching

PawSharp's caching layer provides in-memory and Redis-backed storage for Discord entities, with automatic population from gateway events, expiration-based eviction, LRU eviction, and distributed invalidation support.

---

## Cache Architecture

```
┌──────────────────────────────────────────────────────┐
│                    IEntityCache                       │
│  (interface defining all cache operations)            │
└──────────────────────────────────────────────────────┘
         ▲                   ▲                    ▲
         │                   │                    │
┌────────┴────────┐  ┌──────┴──────┐  ┌──────────┴──────────┐
│MemoryCacheProvider│  │RedisCacheProv│  │  CacheSwapper       │
│ (in-process RAM)  │  │ (distributed) │  │ (provider fallback) │
│ - ConcurrentDict  │  │ - StackExchng │  │ - circuit breaker   │
│ - LRU eviction    │  │ - TTL expiry  │  │ - health checks     │
│ - expiration timer│  │ - Sorted sets │  └────────────────────┘
└───────────────────┘  └──────────────┘
         ▲
         │
┌────────┴──────────┐
│DistributedCacheProv│
│(Redis pub/sub inval)│
└───────────────────┘
```

### IEntityCache Interface

Defined in `src/PawSharp.Cache/Interfaces/IEntityCache.cs`:

```csharp
public interface IEntityCache
{
    void CacheUser(User user);
    User? GetUser(ulong userId);
    void CacheGuild(Guild guild);
    Guild? GetGuild(ulong guildId);
    IEnumerable<Guild> GetAllGuilds();
    void CacheChannel(Channel channel);
    Channel? GetChannel(ulong channelId);
    IEnumerable<Channel> GetGuildChannels(ulong guildId);
    void CacheMessage(Message message);
    Message? GetMessage(ulong messageId);
    IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50);
    void CacheGuildMember(ulong guildId, GuildMember member);
    GuildMember? GetGuildMember(ulong guildId, ulong userId);
    IEnumerable<GuildMember> GetGuildMembers(ulong guildId);
    void CacheRole(ulong guildId, Role role);
    Role? GetRole(ulong guildId, ulong roleId);
    IEnumerable<Role> GetGuildRoles(ulong guildId);
    void CacheEmoji(ulong guildId, Emoji emoji);
    Emoji? GetEmoji(ulong guildId, ulong emojiId);
    IEnumerable<Emoji> GetGuildEmojis(ulong guildId);
    void CacheGuildData(Guild guild);
    void RemoveGuild(ulong guildId);
    void RemoveChannel(ulong channelId);
    void RemoveMessage(ulong messageId);
    void RemoveGuildMember(ulong guildId, ulong userId);
    void RemoveRole(ulong guildId, ulong roleId);
    int GetEntityCount();
    long GetMemoryUsage();
    CacheStats GetCacheStats();
    bool IsHealthy();
    event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
    event EventHandler? CacheCleared;
}
```

`CacheManager` (`src/PawSharp.Client/CacheManager.cs`) subscribes to gateway events and automatically populates the cache:

```csharp
public void SubscribeToGateway(IGatewayClient gateway)
{
    // READY, GUILD_CREATE/UPDATE/DELETE, CHANNEL_CREATE/UPDATE/DELETE,
    // MESSAGE_CREATE/UPDATE/DELETE, GUILD_MEMBER_ADD/UPDATE/REMOVE,
    // GUILD_ROLE_CREATE/UPDATE/DELETE, THREAD_CREATE/UPDATE/DELETE, etc.
}
```

---

## In-Memory Cache (MemoryCacheProvider)

`MemoryCacheProvider` (`src/PawSharp.Cache/Providers/MemoryCacheProvider.cs`) uses `ConcurrentDictionary` per entity type.

### Configuration

```csharp
var cacheOptions = new CacheOptions
{
    MaxGuilds = 1000,
    MaxChannels = 5000,
    MaxUsers = 20000,
    MaxMessages = 10000,
    MaxMembers = 50000,
    MaxRoles = 10000,
    MaxEmojis = 5000,
    DefaultExpiration = TimeSpan.FromHours(1),
    UserExpiration = TimeSpan.FromHours(2),
    MessageExpiration = TimeSpan.FromMinutes(30)
};

var cache = new MemoryCacheProvider(cacheOptions, telemetry, logger);
```

### LRU Eviction

The `EnforceEntityCacheBounds` method evicts the least recently accessed entries:

```csharp
var keysToRemove = keysWithAccess
    .OrderBy(k => k.access)
    .Take(cache.Count - maxSize)
    .Select(k => k.key);
```

### TTL-based Expiration

A background timer (`CleanupExpiredEntries`) runs every minute and removes stale entries by comparing `_lastAccess` timestamps against per-type expiration.

### Memory Estimation

```csharp
long memoryUsage = cache.GetMemoryUsage(); // ~1KB/user, ~2KB/guild, ~2KB/message
```

---

## Redis Cache (RedisCacheProvider)

`RedisCacheProvider` (`src/PawSharp.Cache/Providers/RedisCacheProvider.cs`) stores entities as JSON strings with hierarchical key patterns:

```
user:{id}              -> User JSON
guild:{id}             -> Guild JSON
channel:{id}           -> Channel JSON
message:{id}           -> Message JSON
member:{guildId}:{id}  -> GuildMember JSON
role:{guildId}:{id}    -> Role JSON
emoji:{guildId}:{id}   -> Emoji JSON
channel:{id}:messages  -> SortedSet of message IDs
guild:{id}:channels    -> Set of channel IDs
```

### Setup

```csharp
services.AddSingleton<IEntityCache>(sp =>
    new RedisCacheProvider("localhost:6379"));
```

With options:

```csharp
var options = Options.Create(new RedisCacheOptions
{
    ConnectionString = "redis.example.com:6379",
    Password = "secret",
    Database = 0,
    DefaultExpiration = TimeSpan.FromHours(1),
    ConnectTimeout = 5000
});
var cache = new RedisCacheProvider(options);
```

---

## CacheSwapper and Provider Fallback

`CacheSwapper` (`src/PawSharp.Cache/Swapping/CacheSwapper.cs`) manages multiple providers with:

- **Priority-based fallback** — auto-switches to next provider on failure
- **Circuit breaker** — opens after `MaxFailuresBeforeCircuitOpen` failures
- **Health checks** — periodic `IsHealthy()` polling via timer

```csharp
var swapper = new CacheSwapper(options, telemetry);
swapper.RegisterProvider("memory", memoryCache, priority: 0);
swapper.RegisterProvider("redis", redisCache, priority: 1);
swapper.StartHealthChecks();
```

---

## RedisCacheDistributor

`RedisCacheDistributor` (`src/PawSharp.Cache/Distribution/RedisCacheDistributor.cs`) uses Redis pub/sub to synchronize cache invalidation across bot instances.

```csharp
var distributor = new RedisCacheDistributor(connectionMultiplexer);
var distributedCache = new DistributedCacheProvider(localCache, distributor);
```

---

## Performance Metrics and Telemetry

```csharp
var stats = cache.GetCacheStats();
Console.WriteLine($"Hits: {stats.Hits}, Misses: {stats.Misses}");
Console.WriteLine($"Hit Rate: {stats.HitRatio:P2}");
Console.WriteLine($"Memory: {stats.MemoryUsage / 1024 / 1024} MB");
```

---

## When to Use Caching vs Live API Calls

| Scenario | Use Cache | Use Live API |
|----------|-----------|--------------|
| Frequently accessed guild info | Yes | No |
| Message content from recent events | Yes | No |
| One-time lookup (rare user) | No | Yes |
| Data that must be current (e.g. ban status) | No | Yes |
| Bot startup (warm-up) | Pre-populate | Fetch initially |

---

## Cache Invalidation Strategies

1. **Event-driven** (default) — `CacheManager` updates cache on `GUILD_UPDATE`, `MESSAGE_UPDATE`, etc.
2. **TTL-based** — set expiration per entity type; stale data auto-evicts
3. **LRU eviction** — oldest entries removed when size limits are hit
4. **Manual invalidation** — call `RemoveGuild`, `RemoveMessage`, etc.

```csharp
cache.EntityEvicted += (sender, args) =>
{
    _logger.LogWarning("Cache eviction: {Type} {Id}", args.EntityType, args.EntityId);
};
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Using in-memory cache for multi-instance bots | Switch to `RedisCacheProvider` |
| Not setting `MaxMessages` / bounds | Memory grows unbounded; always configure limits |
| Expecting real-time consistency from TTL cache | Use event-driven cache for live data |
| Ignoring `EntityEvicted` events | Subscribe to track eviction health |
| Sharing `MemoryCacheProvider` across DI as singleton | Correct — this is intended for singletons |
| Not calling `StartHealthChecks()` on `CacheSwapper` | Fallback won't activate automatically |
