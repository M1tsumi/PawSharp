# Advanced Topics

## Caching

### In-Memory Cache

The default `MemoryCacheProvider` is suitable for small to medium bots (< 2500 guilds). It automatically enforces size limits:

```csharp
var settings = new CacheSettings
{
    MaxCachedGuilds = 1000,
    MaxCachedChannelsPerGuild = 100,
    MaxCachedMessages = 10000,
    MessageCacheTTL = TimeSpan.FromHours(1),
};
```

### Redis Distributed Cache

For larger bots or multi-instance deployments:

```csharp
services.AddPawSharp(options, _ =>
    new RedisCacheProvider("localhost:6379"));
```

### Accessing Cached Data

```csharp
var guild = await client.Cache.GetGuildAsync(guildId);
var messages = await client.Cache.GetChannelMessagesAsync(channelId, limit: 50);
var user = await client.Cache.GetUserAsync(userId);
```

## Dependency Injection

PawSharp integrates with `Microsoft.Extensions.DependencyInjection`:

```csharp
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .SetupPawSharp(options); // Registers everything

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

Inject services into your command modules:

```csharp
public class MyCommands : BaseCommandModule
{
    private readonly IDiscordRestClient _rest;
    private readonly ILogger<MyCommands> _logger;

    public MyCommands(IDiscordRestClient rest, ILogger<MyCommands> logger)
    {
        _rest = rest;
        _logger = logger;
    }
}
```

## Error Handling

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)
{
    // Input validation failed
}
catch (RateLimitException ex)
{
    // Hit rate limits
}
catch (DiscordApiException ex)
{
    // Discord API error
    Console.WriteLine($"API Error ({ex.StatusCode}): {ex.Message}");
}
catch (GatewayException ex)
{
    // WebSocket connection issue
}
```

## Performance Tips

- Offload heavy work to background queues using `System.Threading.Channels`
- Set cache limits to prevent unbounded memory growth
- Use `SemaphoreSlim` to throttle concurrent API requests
- Dispose event subscription tokens to prevent handler leaks
- Prefer async/await throughout; never use `.Result` or `.Wait()`
- Use middleware for cross-cutting concerns like logging
- For large bots, enable sharding via `ShardingStrategy.Auto`

## Debugging

Enable debug logging:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

Check connection state:

```csharp
Console.WriteLine($"Connected: {client.Gateway.IsConnected}");
Console.WriteLine($"Cache: guilds={client.Cache.GetStatistics().CachedGuilds}, messages={client.Cache.GetStatistics().CachedMessages}");
```

## Event Interest Filtering

PawSharp includes an Event Interest Filtering system that validates gateway intents against registered handlers. Use `[EventInterest]` attributes to declare required intents:

```csharp
[EventInterest("MESSAGE_CREATE", "MESSAGE_UPDATE")]
public class MyHandler
{
    public void Setup(DiscordClient client)
    {
        client.OnMessageCreated(async msg => { });
        client.OnMessageUpdated(async msg => { });
    }
}
```

Call `ValidateIntents()` before connecting to catch misconfiguration early.
