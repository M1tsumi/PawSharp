# Caching & Scalability Guide

Learn how to efficiently cache and scale Discord bots with PawSharp.

## Table of Contents

1. [Caching Basics](#caching-basics)
2. [In-Memory Cache](#in-memory-cache)
3. [Redis Distributed Cache](#redis-distributed-cache)
4. [Cache Strategies](#cache-strategies)
5. [Monitoring & Statistics](#monitoring--statistics)
6. [Scaling for Large Bots](#scaling-for-large-bots)
7. [Performance Tips](#performance-tips)

---

## Caching Basics

### Why Cache?

Caching is essential for high-performance Discord bots:

```
Without Cache:
  User request → REST API call → Discord servers → Response (100-500ms)
  
With Cache:
  First access → REST API + Cache storage (100-500ms)
  Subsequent accesses → Memory cache only (< 1ms)
```

**Benefits:**
- Dramatically faster data retrieval (100x+ improvement)
- Reduced API rate limiting impact
- Lower bandwidth usage
- Improved bot responsiveness

### Automatic Caching

PawSharp automatically caches entities as they arrive through gateway events:

```csharp
var client = provider.GetRequiredService<DiscordClient>();

// CacheManager subscribes to gateway events automatically
// Messages, guilds, users, channels, members, roles are all cached
client.OnMessageCreated(msg =>
{
    // Message is automatically cached by the CacheManager
    return Task.CompletedTask;
});

// Retrieve from cache (synchronous, very fast)
var cached = client.Cache.GetMessage(msg.Id);
if (cached != null)
{
    Console.WriteLine($"From cache: {cached.Content}");
}
```

---

## In-Memory Cache

**Best for:** Small to medium bots (single instance, < 2500 guilds)  
**Storage:** System RAM  
**Default:** MemoryCacheProvider is used by default

### Setup

The in-memory cache is already configured by default when using `AddPawSharp()`:

```csharp
var services = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton(options)  // PawSharpOptions
    .AddPawSharp();  // Uses MemoryCacheProvider by default

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

### Cache Bounds

The in-memory cache automatically enforces size limits to prevent unbounded memory growth:

```csharp
private const int MaxCacheSize = 10000;          // General cache entries
private const int MaxEntityCacheSize = 5000;     // Per entity type limit

// When limits are exceeded, oldest entries (FIFO) are removed
```

**Typical limits per entity type:**
- Guilds: 5000
- Channels: 5000  
- Messages: 5000
- Users: 5000
- Members: 5000
- Roles: 5000
- Emojis: 5000

### Cache Operations

All cache operations are **synchronous** for maximum performance:

```csharp
var cache = client.Cache;

// Cache entities (automatic from events, but can be manual)
var user = new User { Id = 123456789 };
cache.CacheUser(user);

var guild = new Guild { Id = 987654321 };
cache.CacheGuild(guild);

var message = new Message { Id = 555666777 };
cache.CacheMessage(message);

// Retrieve cached entities
var cachedUser = cache.GetUser(123456789);
var cachedGuild = cache.GetGuild(987654321);
var cachedMessage = cache.GetMessage(555666777);

// Get collections
var allGuilds = cache.GetAllGuilds();
var channelMessages = cache.GetChannelMessages(channelId, limit: 100);
var guildMembers = cache.GetGuildMembers(guildId);

// Check existence
bool exists = cache.Exists(key);

// Remove entities
cache.Remove(key);
cache.RemoveGuild(guildId);

// Clear entire cache
cache.Clear();

// Get statistics
int totalEntities = cache.GetEntityCount();
long memoryBytes = cache.GetMemoryUsage();
var stats = cache.GetCacheStats();
```

### Memory Management

The in-memory cache uses automatic size-based eviction:

| Limit | Value | Behavior |
|-------|-------|----------|
| **Max Cache Size** | 10,000 entries | When exceeded, oldest entries removed (FIFO) |
| **Max Entity Size** | 5,000 per type | Guilds, channels, users, etc. individually limited |
| **Expiration** | Configurable per entity | Optional TTL support |

```csharp
// Monitor memory usage
var stats = cache.GetCacheStats();
var memoryMB = stats.MemoryUsage / 1024 / 1024;

Console.WriteLine($"Cache Memory: {memoryMB}MB");
Console.WriteLine($"Total Entities: {cache.GetEntityCount()}");
```

### Limitations

| Limitation | Impact | Solution |
|-----------|--------|----------|
| **Single process only** | Cache not shared between bot instances | Use Redis for multi-instance |
| **Lost on restart** | Cache cleared when bot stops | Use Redis for persistence |
| **Memory bounded** | Limited to available RAM | Use selective caching or Redis |
| **Not for large bots** | > 2500 guilds may exceed limits | Migrate to Redis + sharding |

---

## Redis Distributed Cache

**Best for:** Large bots (2500+ guilds), multi-instance deployments, persistent cache  
**Storage:** Redis server (network-accessible)  
**Performance:** Sub-millisecond for cache hits

### Installation

First, set up a Redis server:

```bash
# Windows (via Chocolatey)
choco install redis-64

# macOS (via Homebrew)
brew install redis

# Docker (recommended for production)
docker run -d \
  --name redis \
  -p 6379:6379 \
  redis:7-alpine

# Or use managed Redis (AWS ElastiCache, Azure Redis Cache, etc.)
```

Then add the NuGet package:

```bash
dotnet add package StackExchange.Redis
```

### Connect to Redis

```csharp
using PawSharp.Cache.Providers;
using Microsoft.Extensions.Options;

var services = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton(options);  // PawSharpOptions

// Method 1: Simple connection string
var redisCache = new RedisCacheProvider("localhost:6379");
services.AddSingleton<IEntityCache>(redisCache);

// Method 2: With IOptions pattern
var redisCacheOptions = Options.Create(new RedisCacheOptions
{
    ConnectionString = "localhost:6379",
    Password = "optional-password",
    Database = 0,
    DefaultExpiry = TimeSpan.FromHours(24)
});
var cache = new RedisCacheProvider(redisCacheOptions);
services.AddSingleton<IEntityCache>(cache);

services.AddPawSharp();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

### Connection Configuration

```csharp
// Basic connection
var cache = new RedisCacheProvider("localhost:6379");

// With password authentication
var cache = new RedisCacheProvider(
    "redis.example.com:6379,password=MySecurePassword123"
);

// With custom database
var options = new RedisCacheOptions
{
    ConnectionString = "localhost:6379",
    Database = 1,  // Use database 1 instead of default 0
};
var cache = new RedisCacheProvider(Options.Create(options));

// With connection timeouts
var options = new RedisCacheOptions
{
    ConnectionString = "redis.example.com:6379",
    ConnectTimeout = 5000,    // 5 second connection timeout
    SyncTimeout = 2000,       // 2 second command timeout
    ConnectRetry = 3,         // Retry 3 times before failing
};
var cache = new RedisCacheProvider(Options.Create(options));

// High availability setup (multiple nodes)
var cache = new RedisCacheProvider(
    "redis-node1:6379,redis-node2:6379,redis-node3:6379"
);
```

### Shared Cache Across Instances

With Redis, all bot instances automatically share the same cache:

```
┌─────────────────────────────────────┐  ┌─────────────────────────────────────┐  ┌─────────────────────────────────────┐
│   Bot Instance 1                    │  │   Bot Instance 2                    │  │   Bot Instance 3                    │
│  Shard: 0                           │  │  Shard: 1                           │  │  Shard: 2                           │
│  (REST + Gateway)                   │  │  (REST + Gateway)                   │  │  (REST + Gateway)                   │
└──────────────────────┬──────────────┘  └──────────────────────┬──────────────┘  └──────────────────────┬──────────────┘
                       │                                        │                                        │
                       │             Redis SET/GET Commands             │
                       └────────────────────────────────────────┬────────────────────────────────────────┘
                                                                │
                                               ┌────────────────▼─────────────┐
                                               │  Redis Server               │
                                               │  (Centralized)              │
                                               │  Data Store                 │
                                               └─────────────────────────────┘
```

**Benefits:**
- ✅ All instances access identical cache
- ✅ Cache persists across restarts
- ✅ Automatic synchronization (no manual coordination)
- ✅ Horizontal scaling (add more instances anytime)
- ✅ Built-in high availability options

---

## Cache Strategies

### Strategy 1: Event-Driven Caching (Recommended)

Automatically cache entities as gateway events arrive. This is the default PawSharp behavior:

```csharp
// CacheManager subscribes to gateway events automatically on startup
client.OnMessageCreated(msg =>
{
    // Message automatically cached by CacheManager
    // Author (User) automatically cached
    // Guild member automatically cached (if present)
    return Task.CompletedTask;
});

client.OnGuildMemberJoined(member =>
{
    // Member automatically cached
    // User information automatically cached
    return Task.CompletedTask;
});

// Later: retrieve from cache (instant)
var user = cache.GetUser(userId);
var guild = cache.GetGuild(guildId);
var channel = cache.GetChannel(channelId);
```

**When to use:**
- Default strategy for all bots
- Real-time data consistency
- Zero latency on cached data

### Strategy 2: Lazy Loading

Load data from REST API only when needed, then cache it:

```csharp
public Guild? GetGuildWithMembers(ulong guildId)
{
    // Check cache first (fast)
    var guild = cache.GetGuild(guildId);
    if (guild != null)
    {
        return guild;  // Cache hit - instant
    }
    
    // Cache miss - load from REST API
    guild = client.Rest.GetGuildAsync(guildId).Result;
    if (guild != null)
    {
        cache.CacheGuild(guild);
    }
    
    return guild;
}
```

**When to use:**
- Data rarely accessed
- Memory constraints
- Don't need real-time updates

### Strategy 3: Periodic Refresh

Periodically update cache to ensure freshness:

```csharp
private readonly IEntityCache _cache;
private readonly DiscordClient _client;
private Timer? _refreshTimer;

public void StartCacheRefresh()
{
    _refreshTimer = new Timer(
        RefreshCache,
        state: null,
        dueTime: TimeSpan.FromMinutes(5),
        period: TimeSpan.FromMinutes(5)
    );
}

private void RefreshCache(object? state)
{
    try
    {
        Console.WriteLine("[Cache] Refreshing...");
        
        // Refresh all guilds
        var guilds = _cache.GetAllGuilds();
        foreach (var guild in guilds)
        {
            // Refresh guild data from API
            var updated = _client.Rest.GetGuildAsync(guild.Id).Result;
            if (updated != null)
            {
                _cache.CacheGuild(updated);
            }
        }
        
        Console.WriteLine("[Cache] Refresh complete");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Cache] Refresh failed: {ex.Message}");
    }
}
```

**When to use:**
- Data that changes frequently but not in real-time
- Guild settings, member lists
- Ensure consistency across instances

### Strategy 4: Selective Caching

Cache only important or frequently-accessed data:

```csharp
public class SelectiveCache
{
    private readonly IEntityCache _cache;
    private readonly DiscordClient _client;
    private readonly HashSet<ulong> _priorityGuilds = new();
    
    public void SetPriorityGuild(ulong guildId, bool priority)
    {
        if (priority)
            _priorityGuilds.Add(guildId);
        else
            _priorityGuilds.Remove(guildId);
    }
    
    public void Initialize()
    {
        _client.OnMessageCreated(msg =>
        {
            // Only cache messages from priority guilds
            if (msg.GuildId.HasValue && _priorityGuilds.Contains(msg.GuildId.Value))
            {
                _cache.CacheMessage(msg);
            }
            return Task.CompletedTask;
        });
    }
}
```

**When to use:**
- Limited memory (in-memory cache)
- Only specific guilds need caching
- High-frequency events (messages)
- Reduce memory footprint

---

## Monitoring & Statistics

### Cache Statistics

The cache provides detailed statistics about what's cached:

```csharp
// Get comprehensive cache statistics
var stats = cache.GetCacheStats();

// Entity counts
Console.WriteLine($"Guilds: {stats.GuildCount}");
Console.WriteLine($"Channels: {stats.ChannelCount}");
Console.WriteLine($"Messages: {stats.MessageCount}");
Console.WriteLine($"Users: {stats.UserCount}");
Console.WriteLine($"Members: {stats.MemberCount}");
Console.WriteLine($"Roles: {stats.RoleCount}");
Console.WriteLine($"Emojis: {stats.EmojiCount}");

// Overall stats
Console.WriteLine($"Total Entities: {cache.GetEntityCount()}");
Console.WriteLine($"Memory Usage: {cache.GetMemoryUsage() / 1024 / 1024}MB");

// Sample output:
// Guilds: 450
// Channels: 3200
// Messages: 8500
// Users: 125000
// Members: 450000
// Roles: 2300
// Emojis: 1200
// Total Entities: 590750
// Memory Usage: 285MB
```

### Monitoring Helper Class

Create a reusable cache monitor to log statistics periodically:

```csharp
public class CacheMonitor : IDisposable
{
    private readonly IEntityCache _cache;
    private readonly ILogger<CacheMonitor> _logger;
    private Timer? _monitorTimer;

    public CacheMonitor(IEntityCache cache, ILogger<CacheMonitor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void StartMonitoring(TimeSpan interval)
    {
        _monitorTimer = new Timer(
            LogCacheStats,
            state: null,
            dueTime: interval,
            period: interval
        );
        
        _logger.LogInformation("Cache monitoring started, interval: {Interval}s", interval.TotalSeconds);
    }

    private void LogCacheStats(object? state)
    {
        try
        {
            var stats = _cache.GetCacheStats();
            var memoryMB = _cache.GetMemoryUsage() / 1024.0 / 1024.0;
            var totalEntities = _cache.GetEntityCount();
            
            _logger.LogInformation(
                "[Cache Stats] Total: {Total} entities, {Memory}MB | " +
                "Guilds: {Guilds}, Channels: {Channels}, Messages: {Messages}, " +
                "Users: {Users}, Members: {Members}, Roles: {Roles}",
                totalEntities,
                memoryMB.ToString("F2"),
                stats.GuildCount,
                stats.ChannelCount,
                stats.MessageCount,
                stats.UserCount,
                stats.MemberCount,
                stats.RoleCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging cache statistics");
        }
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

// Usage in Program.cs
var services = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton(options)
    .AddPawSharp();

services.AddSingleton<CacheMonitor>();

var provider = services.BuildServiceProvider();
var monitor = provider.GetRequiredService<CacheMonitor>();

// Start logging every minute
monitor.StartMonitoring(TimeSpan.FromMinutes(1));

var client = provider.GetRequiredService<DiscordClient>();
await client.ConnectAsync();
```

---

## Scaling for Large Bots

### Multi-Instance Sharding

For bots in 2500+ servers, run multiple instances with sharding:

```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   Instance 1    │  │   Instance 2    │  │   Instance 3    │
│  Shards: 0-3    │  │  Shards: 4-7    │  │  Shards: 8-11   │
└────────┬────────┘  └────────┬────────┘  └────────┬────────┘
         │                    │                    │
         └────────────────────┼────────────────────┘
                              │
                         ┌────▼────┐
                         │  Redis   │
                         │  Cache   │
                         └──────────┘
```

Each instance handles different shards but shares Redis cache:

```csharp
// Instance 1
var options = new PawSharpOptions
{
    Token = token,
    TotalShards = 12,
    ShardId = 0,  // Start shard
    Shards = ShardingStrategy.Manual,
};

// Instance 2
var options = new PawSharpOptions
{
    Token = token,
    TotalShards = 12,
    ShardId = 4,
    Shards = ShardingStrategy.Manual,
};

// Instance 3
var options = new PawSharpOptions
{
    Token = token,
    TotalShards = 12,
    ShardId = 8,
    Shards = ShardingStrategy.Manual,
};

// All instances use same Redis
services.AddSingleton<IEntityCache>(
    new RedisCacheProvider("redis.example.com:6379")
);
```

### Kubernetes Deployment Example

Deploy multiple bot instances with Redis cache:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: pawsharp-config
data:
  discord-token: "your-bot-token-here"  # Or use Secret for sensitive data
  redis-url: "redis-service:6379"
  total-shards: "12"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pawsharp-bot
  labels:
    app: pawsharp-bot
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 1
      maxSurge: 1
  selector:
    matchLabels:
      app: pawsharp-bot
  template:
    metadata:
      labels:
        app: pawsharp-bot
    spec:
      containers:
      - name: pawsharp-bot
        image: myregistry/pawsharp-bot:latest
        imagePullPolicy: Always
        env:
        - name: DISCORD_TOKEN
          valueFrom:
            configMapKeyRef:
              name: pawsharp-config
              key: discord-token
        - name: REDIS_URL
          valueFrom:
            configMapKeyRef:
              name: pawsharp-config
              key: redis-url
        - name: TOTAL_SHARDS
          valueFrom:
            configMapKeyRef:
              name: pawsharp-config
              key: total-shards
        - name: SHARD_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name  # Pod name becomes shard ID
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10

---
apiVersion: v1
kind: Service
metadata:
  name: redis-service
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
  type: ClusterIP

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        volumeMounts:
        - name: redis-data
          mountPath: /data
      volumes:
      - name: redis-data
        emptyDir: {}  # Or use PersistentVolumeClaim for production
```

In your bot code, read environment variables:

```csharp
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
var totalShards = int.Parse(Environment.GetEnvironmentVariable("TOTAL_SHARDS") ?? "1");
var shardId = int.Parse(Environment.GetEnvironmentVariable("SHARD_ID") ?? "0");

var options = new PawSharpOptions
{
    Token = token,
    TotalShards = totalShards,
    ShardId = shardId,
};

var services = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton(options)
    .AddSingleton<IEntityCache>(new RedisCacheProvider(redisUrl))
    .AddPawSharp();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
await client.ConnectAsync();
```

---

## Performance Tips

### 1. Monitor Memory Usage (In-Memory Cache)

Prevent memory growth on single-instance bots:

```csharp
public class MemoryMonitor
{
    private readonly IEntityCache _cache;
    private readonly ILogger<MemoryMonitor> _logger;
    private const long AlertThreshold = 512 * 1024 * 1024;  // 512MB

    public void Check()
    {
        var memoryBytes = _cache.GetMemoryUsage();
        var memoryMB = memoryBytes / 1024.0 / 1024.0;
        
        _logger.LogInformation($"Cache Memory: {memoryMB:F2}MB");
        
        if (memoryBytes > AlertThreshold)
        {
            _logger.LogWarning(
                "⚠️ High memory usage: {MemoryMB}MB (threshold: {ThresholdMB}MB)",
                memoryMB.ToString("F2"),
                AlertThreshold / 1024.0 / 1024.0
            );
            
            // Recommendations
            _logger.LogWarning("Consider: Migrate to Redis or reduce cache limits");
        }
    }
}
```

### 2. Batch API Operations

Fetch multiple entities in one request:

```csharp
// ❌ Inefficient: 100 individual API calls
foreach (var userId in userIds)
{
    var user = await client.Rest.GetUserAsync(userId);
    cache.CacheUser(user);
}

// ✅ Efficient: One batch request (if available)
var guild = await client.Rest.GetGuildAsync(guildId);
var members = await client.Rest.GetGuildMembersAsync(guildId, limit: 1000);
foreach (var member in members)
{
    cache.CacheGuildMember(guildId, member);
}
```

### 3. Use Selective Caching

For in-memory cache on high-traffic bots, only cache essential data:

```csharp
public class SelectiveCacheManager
{
    private readonly IEntityCache _cache;
    private readonly DiscordClient _client;
    private readonly HashSet<ulong> _cachedGuilds = new();  // Whitelist
    
    public void RegisterGuild(ulong guildId)
    {
        _cachedGuilds.Add(guildId);
    }
    
    public void Initialize()
    {
        _client.OnMessageCreated(msg =>
        {
            // Only cache messages from registered guilds
            if (msg.GuildId.HasValue && _cachedGuilds.Contains(msg.GuildId.Value))
            {
                _cache.CacheMessage(msg);
            }
            return Task.CompletedTask;
        });
    }
}
```

### 4. Efficient Collection Retrieval

When you need collections, use the built-in methods:

```csharp
// ✅ Efficient: Use indexed methods
var guildChannels = cache.GetGuildChannels(guildId);
var guildMembers = cache.GetGuildMembers(guildId);
var recentMessages = cache.GetChannelMessages(channelId, limit: 100);

// Filter in-memory if needed
var activeMembers = guildMembers
    .Where(m => m.JoinedAt > DateTime.UtcNow.AddDays(-30))
    .ToList();
```

### 5. Use Redis for Large Deployments

When scaling to multiple instances:

```csharp
// Automatically handles cache sharing without extra configuration
var cache = new RedisCacheProvider(Environment.GetEnvironmentVariable("REDIS_URL")!);
services.AddSingleton<IEntityCache>(cache);

// All instances automatically share the same cache
// No replication or synchronization code needed
```

### 6. Cache Warm-up Strategy

Preload frequently-accessed data at startup:

```csharp
public class CacheWarmup
{
    private readonly DiscordClient _client;
    private readonly ILogger<CacheWarmup> _logger;
    
    public async Task WarmupAsync()
    {
        _logger.LogInformation("Starting cache warm-up...");
        
        try
        {
            // Preload all guilds
            var guilds = await _client.Rest.GetCurrentUserGuildsAsync(limit: 200);
            foreach (var guild in guilds)
            {
                _client.Cache.CacheGuild(guild);
                await Task.Delay(5);  // Rate limit
            }
            
            _logger.LogInformation("Cache warm-up complete: {Count} guilds", guilds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warm-up failed");
        }
    }
}

// Call during ReadyEvent
client.OnReady(async e =>
{
    var warmup = provider.GetRequiredService<CacheWarmup>();
    await warmup.WarmupAsync();
});
```

---

## Common Patterns

### Pattern: Get with Fallback

Check cache first, fall back to API if needed:

```csharp
public User? GetUser(ulong userId)
{
    // Check cache first
    var cached = _cache.GetUser(userId);
    if (cached != null)
    {
        return cached;  // Instant
    }
    
    // Fallback to REST API if not in cache
    try
    {
        var user = _client.Rest.GetUserAsync(userId).Result;
        if (user != null)
        {
            _cache.CacheUser(user);
        }
        return user;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to fetch user {UserId}", userId);
        return null;
    }
}
```

### Pattern: Automatic Cache Invalidation

Keep cache fresh by responding to updates:

```csharp
public void SetupCacheInvalidation()
{
    // Automatically refresh when guilds update
    _client.OnGuildUpdated(e =>
    {
        _cache.CacheGuild(e);  // Update cache
        _logger.LogDebug("Guild cache updated: {GuildName}", e.Name);
        return Task.CompletedTask;
    });
    
    // Automatically refresh when channels update
    _client.OnChannelUpdated(e =>
    {
        _cache.CacheChannel(e);
        return Task.CompletedTask;
    });
    
    // Remove from cache when guild becomes unavailable or is deleted
    _client.OnGuildUnavailable(e =>
    {
        _cache.RemoveGuild(e.Id);
        _logger.LogDebug("Guild removed from cache: {GuildId}", e.Id);
        return Task.CompletedTask;
    });
}
```

### Pattern: Cache Statistics Dashboard

Expose cache stats for monitoring:

```csharp
public class CacheDashboard
{
    private readonly IEntityCache _cache;
    
    public object GetStats()
    {
        var stats = _cache.GetCacheStats();
        var memoryMB = _cache.GetMemoryUsage() / 1024.0 / 1024.0;
        
        return new
        {
            TotalEntities = _cache.GetEntityCount(),
            MemoryUsageMB = memoryMB.ToString("F2"),
            Entities = new
            {
                Guilds = stats.GuildCount,
                Channels = stats.ChannelCount,
                Messages = stats.MessageCount,
                Users = stats.UserCount,
                Members = stats.MemberCount,
                Roles = stats.RoleCount,
                Emojis = stats.EmojiCount,
            },
            Timestamp = DateTime.UtcNow
        };
    }
}

// Usage in a web API endpoint:
// GET /api/cache/stats
```

---

**More guides:** [REST API](./REST_API_GUIDE.md) | [Gateway Events](./GATEWAY_GUIDE.md) | [Patterns](./PATTERNS_GUIDE.md)
