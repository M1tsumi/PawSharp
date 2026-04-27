# PawSharp.Cache

PawSharp.Cache provides caching primitives for PawSharp-based applications.

Use it when you need faster reads, fewer REST calls, and a cleaner way to keep frequently accessed Discord data close to your bot or service.

## Why Use This Package

- In-memory caching for low-latency access
- Redis-based distributed caching for scalable deployments
- Pluggable cache provider model for custom backends
- Designed to work with gateway-driven updates
- Configurable entity limits and expiration
- Helpful for large bots that need predictable performance

## Requirements

- .NET 10 (`net10.0`)
- For Redis provider: `StackExchange.Redis` package

## Installation

```bash
dotnet add package PawSharp.Cache --version 1.0.0-alpha.4
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
    DefaultExpiration = TimeSpan.FromHours(1) // Default TTL for cached entities
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
    DefaultExpiry = TimeSpan.FromHours(1) // Default TTL
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
- **Custom cache backends** - Implement IEntityCache for your own caching solution

## Cache Statistics

Both providers expose cache statistics:

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

var totalEntities = cache.GetEntityCount();
Console.WriteLine($"Total entities: {totalEntities}");
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
