using PawSharp.Cache.Distribution;
using PawSharp.Cache.Providers;
using StackExchange.Redis;

// Example: Cache Distribution with Redis Pub/Sub

// Create Redis connection
var redis = ConnectionMultiplexer.Connect("localhost:6379");

// Create cache distributor
var distributor = new RedisCacheDistributor(redis, "pawsharp:cache");

// Create a cache provider (can be MemoryCacheProvider or RedisCacheProvider)
var memoryCache = new MemoryCacheProvider();

// Wrap the cache provider with distributed support
var distributedCache = new DistributedCacheProvider(memoryCache, distributor);

// Use the distributed cache like any other cache provider
// Cache invalidations are automatically propagated to all instances
distributedCache.CacheUser(user);
distributedCache.CacheGuild(guild);

// When you remove an entity, it's automatically invalidated across all instances
distributedCache.RemoveGuild(guildId); // This publishes invalidation to Redis

// Other instances listening on the same Redis channel will automatically
// invalidate their local cache when they receive the invalidation event

// The distributor also supports cache clear events
distributedCache.Clear(); // Publishes clear event to all instances

// Check if the distributor is healthy
if (distributor.IsHealthy())
{
    Console.WriteLine("Cache distribution is healthy");
}

// Dispose when done
distributedCache.Dispose();
distributor.Dispose();
