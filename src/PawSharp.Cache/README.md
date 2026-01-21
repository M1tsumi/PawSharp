# PawSharp.Cache

High-performance caching with in-memory and distributed Redis support for Discord entities.

PawSharp.Cache provides intelligent caching for Discord entities with automatic updates from gateway events, configurable limits, and comprehensive monitoring. Built for high-throughput bot applications with support for both local in-memory and distributed Redis caching.

## Features

- **Multiple Cache Providers**: In-memory and Redis distributed caching
- Automatic caching from gateway events
- Configurable per-entity type limits
- LRU eviction when limits are reached
- Detailed hit/miss rates and memory usage tracking
- Thread-safe concurrent access
- First-class dependency injection support
- Extensible provider interface
- Optimized storage with object pooling

## 📦 Installation

```bash
# Core caching functionality
dotnet add package PawSharp.Cache --version 0.5.0-alpha10

# For Redis support
dotnet add package StackExchange.Redis --version 2.7.33
```

## 🚀 Quick Start

### In-Memory Caching

```csharp
using PawSharp.Cache.Providers;

// Create in-memory cache provider
var cache = new MemoryCacheProvider(new CacheOptions
{
    MaxGuilds = 1000,
    MaxUsers = 10000,
    MaxChannels = 5000
});

// Cache entities
cache.CacheGuild(guild);
cache.CacheUser(user);

// Retrieve from cache
var cachedGuild = cache.GetGuild(guildId);
var cachedUser = cache.GetUser(userId);
```

### Redis Distributed Caching

```csharp
using PawSharp.Cache.Providers;
using Microsoft.Extensions.Options;

// Configure Redis options
var redisOptions = Options.Create(new RedisCacheOptions
{
    ConnectionString = "localhost:6379",
    Password = "your-redis-password", // optional
    Database = 0,
    DefaultExpiry = TimeSpan.FromHours(1)
});

// Create Redis cache provider
var cache = new RedisCacheProvider(redisOptions);

// Or use connection string directly
var cache = new RedisCacheProvider("localhost:6379,password=your-password");

// Use same interface as in-memory cache
cache.CacheGuild(guild);
var cachedGuild = cache.GetGuild(guildId);
```

### Dependency Injection Setup

```csharp
using Microsoft.Extensions.DependencyInjection;

// Register in-memory cache
services.AddSingleton<IEntityCache>(provider =>
{
    var options = new CacheOptions { MaxGuilds = 1000 };
    return new MemoryCacheProvider(options);
});

// Register Redis cache
services.AddSingleton<IEntityCache>(provider =>
{
    var options = Options.Create(new RedisCacheOptions
    {
        ConnectionString = "localhost:6379"
    });
    return new RedisCacheProvider(options);
});

// Inject into your services
public class MyBotService
{
    private readonly IEntityCache _cache;

    public MyBotService(IEntityCache cache)
    {
        _cache = cache;
    }
}
```

## 📋 Cache Configuration

```csharp
var options = new CacheOptions
{
    // Entity limits
    MaxGuilds = 1000,
    MaxUsers = 10000,
    MaxChannels = 5000,
    MaxMembersPerGuild = 1000,
    MaxEmojisPerGuild = 100,

    // Time-based expiration (optional)
    DefaultExpiration = TimeSpan.FromHours(24),

    // Memory management
    EnableMemoryTracking = true,
    MemoryLimitBytes = 100 * 1024 * 1024, // 100MB

    // Statistics
    EnableStatistics = true
};

var cache = new MemoryCacheProvider(options);
```

## 🔧 Automatic Cache Updates

### Gateway Event Integration

```csharp
// Cache automatically updates from gateway events
client.Gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", async evt =>
{
    // Guild is automatically cached
    var guild = await cache.GetGuildAsync(evt.Guild.Id);
});

client.Gateway.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", async evt =>
{
    // Member is automatically cached
    var member = await cache.GetGuildMemberAsync(evt.GuildId, evt.User.Id);
});
```

### Manual Cache Management

```csharp
// Explicit caching
await cache.CacheGuildAsync(guild);
await cache.CacheChannelAsync(channel);
await cache.CacheUserAsync(user);

// Bulk operations
await cache.CacheGuildsAsync(guildList);
await cache.CacheUsersAsync(userList);
```

## 📊 Cache Statistics

```csharp
// Get comprehensive statistics
var stats = cache.GetStats();

Console.WriteLine($"Cache Hit Rate: {stats.HitRate:P}");
Console.WriteLine($"Total Entries: {stats.TotalEntries}");
Console.WriteLine($"Memory Usage: {stats.MemoryUsageBytes / 1024 / 1024}MB");

// Per-entity statistics
Console.WriteLine($"Guilds: {stats.GuildCount} ({stats.GuildHitRate:P} hit rate)");
Console.WriteLine($"Users: {stats.UserCount} ({stats.UserHitRate:P} hit rate)");
Console.WriteLine($"Channels: {stats.ChannelCount} ({stats.ChannelHitRate:P} hit rate)");
```

## 🔍 Cache Queries

### Basic Retrieval

```csharp
// Get single entities
var user = await cache.GetUserAsync(userId);
var guild = await cache.GetGuildAsync(guildId);
var channel = await cache.GetChannelAsync(channelId);

// Get with related data
var guildWithMembers = await cache.GetGuildAsync(guildId, includeMembers: true);
var channelWithPermissions = await cache.GetChannelAsync(channelId, includePermissions: true);
```

### Bulk Operations

```csharp
// Get multiple entities
var users = await cache.GetUsersAsync(userIds);
var channels = await cache.GetGuildChannelsAsync(guildId);

// Search and filter
var onlineMembers = await cache.GetGuildMembersAsync(guildId,
    predicate: m => m.User.Presence?.Status == UserStatus.Online);
```

### Cache Inspection

```csharp
// Check cache contents
bool hasUser = await cache.HasUserAsync(userId);
bool hasGuild = await cache.HasGuildAsync(guildId);

// Get cache keys
var guildIds = cache.GetCachedGuildIds();
var userIds = cache.GetCachedUserIds();
```

## 🗂️ Cache Providers

### Memory Cache Provider (Default)

```csharp
// High-performance in-memory cache
var cache = new MemoryCacheProvider(options);
```

### Custom Cache Provider

```csharp
public class RedisCacheProvider : ICacheProvider
{
    public Task CacheGuildAsync(Guild guild)
    {
        // Implement Redis caching
        return Task.CompletedTask;
    }

    // Implement other interface methods...
}

// Use custom provider
var cache = new RedisCacheProvider(connectionString);
```

## 🔄 Cache Invalidation

### Automatic Invalidation

```csharp
// Cache automatically invalidates on:
// - Entity updates (GUILD_UPDATE, USER_UPDATE)
// - Entity deletions (GUILD_DELETE, CHANNEL_DELETE)
// - Member removals (GUILD_MEMBER_REMOVE)
// - Role changes (GUILD_ROLE_UPDATE)
```

### Manual Invalidation

```csharp
// Clear specific entities
await cache.InvalidateGuildAsync(guildId);
await cache.InvalidateUserAsync(userId);

// Clear by type
await cache.ClearGuildsAsync();
await cache.ClearUsersAsync();

// Clear everything
await cache.ClearAsync();
```

## 📈 Performance Monitoring

```csharp
// Real-time metrics
var metrics = cache.GetMetrics();

Console.WriteLine($"Average Lookup Time: {metrics.AverageLookupTime.TotalMilliseconds}ms");
Console.WriteLine($"Cache Evictions: {metrics.EvictionCount}");
Console.WriteLine($"Memory Pressure: {metrics.MemoryPressure:P}");

// Performance alerts
if (metrics.HitRate < 0.8)
{
    Console.WriteLine("Warning: Cache hit rate below 80%");
}
```

## 🏗️ Architecture

```
PawSharp.Cache
├── ICacheProvider (interface)
│   ├── MemoryCacheProvider (default implementation)
│   └── Custom providers (Redis, Database, etc.)
├── CacheOptions (configuration)
├── CacheStatistics (metrics)
├── Entity-specific caches
│   ├── GuildCache
│   ├── UserCache
│   ├── ChannelCache
│   └── MemberCache
└── Automatic invalidation system
```

## ⚙️ Advanced Configuration

### Memory Management

```csharp
var options = new CacheOptions
{
    // Aggressive memory management
    MaxGuilds = 500,
    MaxUsers = 5000,
    EnableMemoryTracking = true,
    MemoryLimitBytes = 50 * 1024 * 1024, // 50MB

    // Eviction policy
    EvictionPolicy = CacheEvictionPolicy.Lru, // Least Recently Used
    EnableBackgroundCleanup = true
};
```

### Cache Warming

```csharp
// Pre-populate cache on startup
public async Task WarmCacheAsync(DiscordClient client)
{
    // Cache frequently accessed guilds
    var guilds = await client.Rest.GetCurrentUserGuildsAsync();
    await cache.CacheGuildsAsync(guilds);

    // Cache bot user
    var user = await client.Rest.GetCurrentUserAsync();
    await cache.CacheUserAsync(user);
}
```

## 🤝 Dependencies

- **PawSharp.Core** - Entity models
- **Microsoft.Extensions.Caching.Memory** - .NET memory cache
- **Microsoft.Extensions.Options** - Configuration options

## 📚 Related Packages

- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - Main client with cache integration
- **[PawSharp.Gateway](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Gateway)** - Gateway events that update cache

## 🐛 Error Handling

```csharp
try
{
    var user = await cache.GetUserAsync(userId);
}
catch (CacheMissException ex)
{
    // Entity not in cache
    Console.WriteLine($"Cache miss for user {ex.EntityId}");
}
catch (CacheFullException ex)
{
    // Cache is full
    Console.WriteLine("Cache is at capacity");
}
```

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.