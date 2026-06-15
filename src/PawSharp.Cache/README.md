# PawSharp.Cache

PawSharp.Cache provides caching primitives for PawSharp-based applications.

Use it when you need faster reads, fewer REST calls, and a cleaner way to keep frequently accessed Discord data close to your bot or service.

## Why Use This Package

- In-memory caching for low-latency access
- Redis-based distributed caching for scalable deployments
- **Cache swapping with automatic fallback** - Switch between cache providers at runtime
- **Cache distribution** - Share cache invalidations across multiple bot instances
- Pluggable cache provider model for custom backends
- Designed to work with gateway-driven updates
- Configurable entity limits and expiration
- Helpful for large bots that need predictable performance

## Requirements

- .NET 10 (`net10.0`)
- For Redis provider: `StackExchange.Redis` package

## Installation

```bash
dotnet add package PawSharp.Cache --version 1.1.0-alpha.3
```

For Redis support, also add:
```bash
dotnet add package StackExchange.Redis
```

## Quick Start

### Using Memory Cache

```csharp
using PawSharp.Cache;
using PawSharp.Cache.Providers;

// Create cache with default options
var cache = new MemoryCacheProvider();

// Or with custom options
var cache = new MemoryCacheProvider(new CacheOptions
{
    MaxGuilds = 1000,
    MaxChannels = 5000,
    MaxUsers = 20000,
    MaxMessages = 10000,
    MaxMembers = 50000,
    MaxRoles = 10000,
    MaxEmojis = 5000,
    DefaultExpiration = TimeSpan.FromHours(1)
});

// Cache entities
cache.CacheGuild(guild);
cache.CacheUser(user);
cache.CacheChannel(channel);
cache.CacheMessage(message);
cache.CacheGuildMember(guildId, member);
cache.CacheRole(guildId, role);
cache.CacheEmoji(guildId, emoji);

// Retrieve entities
var cachedGuild = cache.GetGuild(guildId);
var cachedUser = cache.GetUser(userId);
var cachedChannel = cache.GetChannel(channelId);
var cachedMessage = cache.GetMessage(messageId);
var cachedMember = cache.GetGuildMember(guildId, userId);
var cachedRole = cache.GetRole(guildId, roleId);
var cachedEmoji = cache.GetEmoji(guildId, emojiId);

// Get collections
var allGuilds = cache.GetAllGuilds();
var guildChannels = cache.GetGuildChannels(guildId);
var channelMessages = cache.GetChannelMessages(channelId, limit: 50);
var guildMembers = cache.GetGuildMembers(guildId);
var guildRoles = cache.GetGuildRoles(guildId);
var guildEmojis = cache.GetGuildEmojis(guildId);

// Bulk operations
cache.CacheGuildData(guild); // Caches guild + all channels, members, roles, emojis
cache.RemoveGuild(guildId); // Removes guild and all related entities

// Statistics
var stats = cache.GetCacheStats();
Console.WriteLine($"Users: {stats.UserCount}, Guilds: {stats.GuildCount}");
Console.WriteLine($"Memory usage: {stats.MemoryUsage} bytes");
```

### Using Redis Cache

```csharp
using PawSharp.Cache.Providers;
using Microsoft.Extensions.Options;

// Using connection string
var cache = new RedisCacheProvider("localhost:6379");

// Or with options
var cache = new RedisCacheProvider(Options.Create(new RedisCacheOptions
{
    ConnectionString = "localhost:6379",
    Password = "your-password",
    Database = 0,
    ConnectTimeout = 5000,
    SyncTimeout = 5000,
    ConnectRetry = 3,
    DefaultExpiry = TimeSpan.FromHours(2)
}));

// Same API as MemoryCacheProvider
cache.CacheGuild(guild);
var cachedGuild = cache.GetGuild(guild.Id);

// Async operations (useful for distributed caches)
var user = await cache.GetUserAsync(userId);
var guild = await cache.GetGuildAsync(guildId);
```

### Integration with PawSharp.Client

The cache is automatically integrated when using PawSharp.Client:

```csharp
using PawSharp.Client;
using PawSharp.Client.Extensions;

// With DI - uses in-memory cache by default
builder.Services.SetupPawSharp(new PawSharpOptions
{
    Token = "Bot YOUR_TOKEN",
    Intents = GatewayIntents.AllNonPrivileged
});

// Or with custom cache
builder.Services.AddPawSharp(new PawSharpOptions
{
    Token = "Bot YOUR_TOKEN",
    Intents = GatewayIntents.AllNonPrivileged
}, serviceProvider => new MemoryCacheProvider(new CacheOptions
{
    MaxGuilds = 2000,
    DefaultExpiration = TimeSpan.FromHours(2)
}));

// Access the cache
var client = serviceProvider.GetRequiredService<DiscordClient>();
var guild = client.Cache.GetGuild(guildId);
```

## Configuration

### CacheOptions

```csharp
var options = new CacheOptions
{
    MaxGuilds = 1000,        // Maximum guilds to cache
    MaxChannels = 5000,      // Maximum channels to cache
    MaxUsers = 20000,        // Maximum users to cache
    MaxMessages = 10000,     // Maximum messages to cache
    MaxMembers = 50000,      // Maximum guild members to cache
    MaxRoles = 10000,        // Maximum roles to cache
    MaxEmojis = 5000,        // Maximum emojis to cache
    DefaultExpiration = TimeSpan.FromHours(1), // Default TTL for cached entities
    // Per-entity TTL (overrides DefaultExpiration if set)
    UserExpiration = TimeSpan.FromHours(2),
    GuildExpiration = TimeSpan.FromHours(3),
    ChannelExpiration = TimeSpan.FromHours(2),
    MessageExpiration = TimeSpan.FromMinutes(30),
    MemberExpiration = TimeSpan.FromHours(1),
    RoleExpiration = TimeSpan.FromHours(6),
    EmojiExpiration = TimeSpan.FromHours(6)
};
```

### RedisCacheOptions

```csharp
var options = new RedisCacheOptions
{
    ConnectionString = "localhost:6379",
    Password = "your-password",        // Optional
    Database = 0,                      // Redis database number
    ConnectTimeout = 5000,             // Connection timeout (ms)
    SyncTimeout = 5000,                // Sync operation timeout (ms)
    ConnectRetry = 3,                  // Number of retry attempts
    DefaultExpiry = TimeSpan.FromHours(1), // Default TTL
    // Per-entity TTL (overrides DefaultExpiry if set)
    UserExpiry = TimeSpan.FromHours(2),
    GuildExpiry = TimeSpan.FromHours(3),
    ChannelExpiry = TimeSpan.FromHours(2),
    MessageExpiry = TimeSpan.FromMinutes(30),
    MemberExpiry = TimeSpan.FromHours(1),
    RoleExpiry = TimeSpan.FromHours(6),
    EmojiExpiry = TimeSpan.FromHours(6)
};
```

## Cache Providers

### MemoryCacheProvider

- **Best for**: Single-instance bots, development, testing
- **Pros**: Fast, no external dependencies, simple setup
- **Cons**: Not distributed, memory limited to single process
- **Features**: 
  - Automatic expiration with priority queue
  - Bounded caching with configurable limits per entity type
  - Automatic cleanup every 5 minutes
  - Thread-safe with ConcurrentDictionary

### RedisCacheProvider

- **Best for**: Distributed bots, multi-instance deployments, large-scale bots
- **Pros**: Distributed caching, shared state across instances, Redis persistence
- **Cons**: Requires Redis server, network latency
- **Features**:
  - Full async API for distributed operations
  - Configurable connection settings
  - Sorted sets for efficient message retrieval
  - Pattern-based key operations for bulk queries
  - Automatic disposal of Redis connection

## Typical Use Cases

- **Reducing repeat API calls** - Cache frequently accessed entities to reduce REST API calls
- **Keeping active data in memory** - Cache guilds, members, channels for quick access
- **Distributed caching** - Use Redis for multi-instance bot deployments
- **Cache swapping** - Switch between cache providers at runtime with automatic fallback
- **Cache distribution** - Share cache invalidations across multiple bot instances via Redis pub/sub
- **Custom cache backends** - Implement IEntityCache for your own caching solution

## Cache Statistics

Both providers expose cache statistics including hit/miss metrics:

```csharp
var stats = cache.GetCacheStats();
Console.WriteLine($"Users: {stats.UserCount}");
Console.WriteLine($"Guilds: {stats.GuildCount}");
Console.WriteLine($"Channels: {stats.ChannelCount}");
Console.WriteLine($"Messages: {stats.MessageCount}");
Console.WriteLine($"Members: {stats.MemberCount}");
Console.WriteLine($"Roles: {stats.RoleCount}");
Console.WriteLine($"Emojis: {stats.EmojiCount}");
Console.WriteLine($"Memory: {stats.MemoryUsage} bytes");
Console.WriteLine($"Hits: {stats.Hits}");
Console.WriteLine($"Misses: {stats.Misses}");
Console.WriteLine($"Hit Ratio: {stats.HitRatio:P2}");

var totalEntities = cache.GetEntityCount();
Console.WriteLine($"Total entities: {totalEntities}");
```

## Cache Swapping

Cache swapping allows you to switch between different cache providers at runtime with automatic fallback support:

```csharp
using PawSharp.Cache.Swapping;

var swapperOptions = new CacheSwapperOptions
{
    AutoFallback = true,
    MaxFailuresBeforeCircuitOpen = 3,
    CircuitOpenDuration = TimeSpan.FromMinutes(5),
    AutoSwapBackToPrimary = true,
    HealthCheckInterval = TimeSpan.FromSeconds(30),
    EnableLogging = true
};

var cacheSwapper = new CacheSwapper(swapperOptions);

// Register multiple cache providers with priorities (lower = higher priority)
var memoryCache = new MemoryCacheProvider();
var redisCache = new RedisCacheProvider("localhost:6379");

cacheSwapper.RegisterProvider("memory", memoryCache, priority: 10); // Fallback
cacheSwapper.RegisterProvider("redis", redisCache, priority: 0);    // Primary

// Start automatic health checks
cacheSwapper.StartHealthChecks();

// Use like any other cache provider
cacheSwapper.CacheUser(user);
var cachedUser = cacheSwapper.GetUser(userId);

// Manually switch providers
cacheSwapper.SetActiveProvider("memory");

// Get provider information
var providers = cacheSwapper.GetProviders();
foreach (var provider in providers)
{
    Console.WriteLine($"Provider: {provider.Name}, Healthy: {provider.IsHealthy}");
}

cacheSwapper.StopHealthChecks();
cacheSwapper.Dispose();
```

### Cache Swapping Features

- **Automatic Fallback**: If the active provider fails, automatically switch to the next healthy provider
- **Circuit Breaker**: Temporarily disable providers that fail repeatedly
- **Health Checks**: Automatic health monitoring with configurable intervals
- **Priority-Based**: Configure provider priority for fallback order
- **Developer-Centric Errors**: Clear exceptions for debugging (CacheSwapException, CacheProviderUnavailableException, etc.)

## Cache Distribution

Cache distribution allows multiple bot instances to share cache invalidations via Redis pub/sub:

```csharp
using PawSharp.Cache.Distribution;
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("localhost:6379");
var distributor = new RedisCacheDistributor(redis, "pawsharp:cache");

var memoryCache = new MemoryCacheProvider();
var distributedCache = new DistributedCacheProvider(memoryCache, distributor);

// Use like any other cache provider
distributedCache.CacheUser(user);
distributedCache.CacheGuild(guild);

// Invalidations are automatically propagated to all instances
distributedCache.RemoveGuild(guildId); // Publishes to Redis

// Check health
if (distributedCache.IsHealthy())
{
    Console.WriteLine("Cache distribution is healthy");
}

distributedCache.Dispose();
```

### Cache Distribution Features

- **Redis Pub/Sub**: Efficient invalidation propagation across instances
- **Automatic Publishing**: Cache invalidations are automatically published
- **Event Handling**: Subscribe to invalidation events from other instances
- **Health Monitoring**: Check distributor health via Redis connection

## Cache Invalidation Events

Both providers support cache invalidation events to monitor when entities are evicted or the cache is cleared:

```csharp
cache.EntityEvicted += (sender, args) =>
{
    Console.WriteLine($"Entity evicted: {args.EntityType} ID: {args.EntityId} Guild: {args.GuildId}");
};

cache.CacheCleared += (sender, args) =>
{
    Console.WriteLine("Cache was cleared");
};
```

## Cache Telemetry

Cache providers support telemetry for monitoring cache performance and health:

```csharp
using PawSharp.Cache.Telemetry;

// Create a telemetry instance
var telemetry = new CacheTelemetry();

// Pass it to the cache provider
var cache = new MemoryCacheProvider(new CacheOptions(), telemetry);

// Get a snapshot of telemetry data
var snapshot = cache.Telemetry?.GetSnapshot();
Console.WriteLine($"Hit Rate: {snapshot?.HitRate:P2}");
Console.WriteLine($"Average Operation Duration: {snapshot?.AverageOperationDuration.TotalMilliseconds}ms");
Console.WriteLine($"Total Hits: {snapshot?.TotalHits}");
Console.WriteLine($"Total Misses: {snapshot?.TotalMisses}");

// Per-entity metrics
foreach (var (entityType, metrics) in snapshot?.EntityMetrics ?? [])
{
    Console.WriteLine($"{entityType}: Hits={metrics.Hits}, Misses={metrics.Misses}, HitRate={metrics.HitRate:P2}");
}

// Per-operation metrics
foreach (var (operation, metrics) in snapshot?.OperationMetrics ?? [])
{
    Console.WriteLine($"{operation}: Count={metrics.Count}, AvgDuration={metrics.AverageDuration.TotalMilliseconds}ms");
}

// Recent evictions
foreach (var eviction in snapshot?.RecentEvictions ?? [])
{
    Console.WriteLine($"Evicted {eviction.EntityType} at {eviction.Timestamp}: {eviction.Reason}");
}

// Reset telemetry
cache.Telemetry?.Reset();
```

## Health Checks

Cache providers support health checks to verify cache availability:

```csharp
if (cache is ICacheProviderHealthCheckable healthCheckable)
{
    bool isHealthy = healthCheckable.IsHealthy();
    Console.WriteLine($"Cache is {(isHealthy ? "healthy" : "unhealthy")}");
}

// For MemoryCacheProvider: checks if cleanup timer is running
// For RedisCacheProvider: checks Redis connection and performs PING
// For DistributedCacheProvider: checks both inner cache and distributor health
```

## Related Packages

- `PawSharp.Client`: High-level client with automatic caching integration
- `PawSharp.Gateway`: Real-time events used to keep cached data fresh via CacheManager
- `PawSharp.Core`: Shared models for cached entities

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
