using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Cache.Providers;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;

namespace PawSharp.Examples;

/// <summary>
/// Example demonstrating Redis distributed caching with PawSharp.
/// This example shows how to configure and use Redis as a cache provider
/// for scalable bot deployments.
/// </summary>
public class RedisCacheExample
{
    private readonly IEntityCache _cache;
    private readonly ILogger<RedisCacheExample> _logger;

    public RedisCacheExample(IEntityCache cache, ILogger<RedisCacheExample> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates basic Redis cache operations.
    /// </summary>
    public async Task RunCacheOperationsAsync()
    {
        _logger.LogInformation("Starting Redis cache operations example");

        // Create sample entities
        var user = new User
        {
            Id = 123456789012345678,
            Username = "redis_user",
            Discriminator = "1234",
            Avatar = "avatar_hash"
        };

        var guild = new Guild
        {
            Id = 987654321098765432,
            Name = "Redis Test Guild",
            OwnerId = user.Id
        };

        var channel = new Channel
        {
            Id = 555666777888999000,
            GuildId = guild.Id,
            Name = "general",
            Type = Core.Enums.ChannelType.GuildText
        };

        // Cache entities
        _logger.LogInformation("Caching entities...");
        _cache.CacheUser(user);
        _cache.CacheGuild(guild);
        _cache.CacheChannel(channel);

        // Retrieve cached entities
        _logger.LogInformation("Retrieving cached entities...");
        var cachedUser = _cache.GetUser(user.Id);
        var cachedGuild = _cache.GetGuild(guild.Id);
        var cachedChannel = _cache.GetChannel(channel.Id);

        if (cachedUser != null)
            _logger.LogInformation("Retrieved user: {Username}#{Discriminator}", cachedUser.Username, cachedUser.Discriminator);

        if (cachedGuild != null)
            _logger.LogInformation("Retrieved guild: {Name} (ID: {Id})", cachedGuild.Name, cachedGuild.Id);

        if (cachedChannel != null)
            _logger.LogInformation("Retrieved channel: {Name} in guild {GuildId}", cachedChannel.Name, cachedChannel.GuildId);

        // Get cache statistics
        var stats = _cache.GetCacheStats();
        _logger.LogInformation("Cache statistics - Users: {Users}, Guilds: {Guilds}, Channels: {Channels}, Memory: {Memory} bytes",
            stats.UserCount, stats.GuildCount, stats.ChannelCount, stats.MemoryUsage);

        // Demonstrate cache operations
        _logger.LogInformation("Testing cache operations...");
        _cache.Add("custom_key", "custom_value");
        var customValue = _cache.Get("custom_key") as string;
        _logger.LogInformation("Custom cache value: {Value}", customValue);

        _logger.LogInformation("Cache operations completed");
    }

    /// <summary>
    /// Entry point for the Redis cache example.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Configure dependency injection with Redis cache
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure Redis cache options
        var redisOptions = new RedisCacheOptions
        {
            ConnectionString = "localhost:6379", // Change to your Redis server
            // Password = "your-redis-password", // Uncomment if password required
            Database = 0,
            DefaultExpiry = TimeSpan.FromHours(1),
            ConnectTimeout = 5000,
            SyncTimeout = 5000
        };

        // Register Redis cache provider
        services.AddSingleton<IEntityCache>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<RedisCacheProvider>>();
            return new RedisCacheProvider(Microsoft.Extensions.Options.Options.Create(redisOptions));
        });

        // Register example service
        services.AddSingleton<RedisCacheExample>();

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            var example = serviceProvider.GetRequiredService<RedisCacheExample>();
            await example.RunCacheOperationsAsync();
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<RedisCacheExample>>();
            logger.LogError(ex, "Error running Redis cache example");

            Console.WriteLine("Make sure Redis is running on localhost:6379");
            Console.WriteLine("You can start Redis with: redis-server");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

/*
To run this example:

1. Start Redis server:
   redis-server

2. Run the example:
   dotnet run --project examples/RedisCacheExample.cs

3. The example will:
   - Connect to Redis
   - Cache sample Discord entities
   - Retrieve them from cache
   - Display cache statistics
   - Clean up and exit

Configuration Options:
- ConnectionString: Redis server address (default: localhost:6379)
- Password: Redis password (optional)
- Database: Redis database number (default: 0)
- DefaultExpiry: How long cached items live (default: 1 hour)
- ConnectTimeout: Connection timeout in ms (default: 5000)
- SyncTimeout: Sync operation timeout in ms (default: 5000)

For production use:
- Use connection pooling with multiple endpoints
- Configure proper timeouts and retry logic
- Monitor Redis performance and memory usage
- Consider Redis clustering for high availability
*/