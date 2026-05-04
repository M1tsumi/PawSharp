using PawSharp.Cache.Providers;
using PawSharp.Cache.Swapping;
using PawSharp.Cache.Exceptions;

// Example: Cache Swapping with Fallback Support

// Create cache swapper with custom options
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

cacheSwapper.RegisterProvider("memory", memoryCache, priority: 10); // Lower priority (fallback)
cacheSwapper.RegisterProvider("redis", redisCache, priority: 0);    // Higher priority (primary)

// Start automatic health checks
cacheSwapper.StartHealthChecks();

// Use the cache swapper like any other cache provider
// It automatically uses the active provider and falls back if needed
try
{
    cacheSwapper.CacheUser(user);
    var cachedUser = cacheSwapper.GetUser(userId);
    Console.WriteLine($"Retrieved user: {cachedUser?.Username}");
}
catch (CacheProviderUnavailableException ex)
{
    Console.WriteLine($"Cache provider unavailable: {ex.ProviderName}");
    // Swapper will automatically try to fallback to next provider
}
catch (CacheSwapException ex)
{
    Console.WriteLine($"Cache swap failed: {ex.Message}");
}

// Manually switch providers if needed
try
{
    cacheSwapper.SetActiveProvider("memory");
    Console.WriteLine("Switched to memory cache");
}
catch (CacheProviderNotRegisteredException ex)
{
    Console.WriteLine($"Provider not registered: {ex.Message}");
}
catch (CacheProviderUnavailableException ex)
{
    Console.WriteLine($"Provider unhealthy: {ex.ProviderName}");
}

// Get information about all providers
var providers = cacheSwapper.GetProviders();
foreach (var provider in providers)
{
    Console.WriteLine($"Provider: {provider.Name}, Active: {provider.IsActive}, Healthy: {provider.IsHealthy}, Priority: {provider.Priority}");
}

// Stop health checks when done
cacheSwapper.StopHealthChecks();
cacheSwapper.Dispose();
