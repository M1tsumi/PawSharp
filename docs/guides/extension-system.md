# Extension System

PawSharp provides extension points through DI service registration, middleware, custom converters, and custom cache providers.

---

## Service Collection Extensions

### AddPawSharp

Registers all core services:

```csharp
services.AddPawSharp();
// Registers: DiscordClient, DiscordRestClient, IEntityCache (MemoryCacheProvider),
//            IAdvancedRateLimiter, CacheManager, IPerformanceMetrics
```

### SetupPawSharp

Configure with custom registrations:

```csharp
services.AddPawSharp()
    .AddSingleton<IEntityCache>(new RedisCacheProvider("localhost:6379"));
```

---

## Custom Middleware

The gateway event dispatcher supports middleware for cross-cutting concerns:

```csharp
// Log all events
client.Gateway.Events.Use(async (eventName, eventData) =>
{
    _logger.LogInformation("Event: {EventName}", eventName);
    // No next() call — all middleware and handlers always fire
});
```

Middleware runs before individual handlers. Use it for:

- Logging/monitoring
- Rate limiting user-facing commands
- Filtering events
- Dependency injection scoping

---

## Custom Type Converters

PawSharp uses `SnowflakeJsonConverter` for Discord snowflakes:

```csharp
public class SnowflakeJsonConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return ulong.Parse(reader.GetString()!);
        return reader.GetUInt64();
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

Register custom converters with the `JsonSerializerOptions`:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    Converters = { new SnowflakeJsonConverter() }
};
```

---

## Custom Cache Providers

Implement `IEntityCache` to create your own storage backend:

```csharp
public class MyCustomCacheProvider : IEntityCache
{
    private readonly ConcurrentDictionary<ulong, User> _users = new();

    public void CacheUser(User user) => _users[user.Id] = user;
    public User? GetUser(ulong userId) =>
        _users.TryGetValue(userId, out var u) ? u : null;

    // ... implement remaining methods

    public bool IsHealthy() => true;
    public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
    public event EventHandler? CacheCleared;
}

// Register
services.AddSingleton<IEntityCache, MyCustomCacheProvider>();
```

---

## Custom Event Filters

```csharp
public class MessageContentFilter : IEventFilter<MessageCreateEvent>
{
    public bool ShouldProcess(MessageCreateEvent ev)
        => !ev.Author.IsBot;
}

// Apply via middleware
client.Gateway.Events.Use(async (name, data) =>
{
    if (data is MessageCreateEvent msg && !msg.Author.IsBot)
        await dispatcher.DispatchTypedAsync(name, msg);
});
```

---

## Complete Extension Example

```csharp
// 1. Define a custom logger provider
public class CustomLoggerProvider : ILoggerProvider { /* ... */ }

// 2. Define a custom cache provider
public class MongoCacheProvider : IEntityCache { /* ... */ }

// 3. Define a custom metrics sink
public class PrometheusMetricsSink
{
    public void Report(IPerformanceMetrics metrics)
    {
        var summary = metrics.GetSummary();
        // Push to Prometheus
    }
}

// 4. Wire everything together
var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.AddProvider(new CustomLoggerProvider());
});

services.AddSingleton(options);

// Custom cache
services.AddSingleton<IEntityCache>(new MongoCacheProvider("mongodb://..."));

// Custom metrics monitoring
services.AddSingleton<PrometheusMetricsSink>();
services.AddSingleton<IPerformanceMetrics>(sp =>
{
    var metrics = new PerformanceMetrics();
    var sink = sp.GetRequiredService<PrometheusMetricsSink>();
    var timer = new Timer(_ => sink.Report(metrics), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    return metrics;
});

services.AddPawSharp();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
await client.ConnectAsync();
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Not calling `AddPawSharp()` | Core services won't be registered |
| Replacing `IEntityCache` without implementing all methods | Implement `CacheStats`, `GetMemoryUsage`, `IsHealthy`, and events |
| Blocking in middleware | All middleware is async-capable |
| Not disposing subscription tokens | Store `IDisposable` from `Events.On()` |
