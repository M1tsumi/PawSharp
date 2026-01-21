# PawSharp Developmental Practices

This guide covers the internal architecture, design patterns, and best practices used in PawSharp development. Understanding these practices will help you contribute effectively and build reliable Discord bots.

## Core Architecture Principles

### 1. Exception-First Error Handling

PawSharp never returns null or empty collections. All error conditions throw typed exceptions:

```csharp
// ✅ PawSharp approach
public async Task<Message> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
{
    if (request.Content.Length > 2000)
        throw new ValidationException("Content exceeds 2000 characters");

    var response = await _httpClient.PostAsync(endpoint, content);
    return await DeserializeAsync<Message>(response);
}

// ❌ Anti-pattern (not used in PawSharp)
public async Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
{
    if (request.Content.Length > 2000)
        return null;

    try
    {
        var response = await _httpClient.PostAsync(endpoint, content);
        return await DeserializeAsync<Message>(response);
    }
    catch
    {
        return null;
    }
}
```

**Benefits:**
- Forces proper error handling
- Clear contract: method either succeeds or throws
- Easier debugging and testing
- Prevents null reference exceptions

### 2. Dependency Injection Everywhere

All components support and prefer DI:

```csharp
// Service registration
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<IDiscordRestClient, RestClient>();
services.AddSingleton<DiscordClient>();

// Constructor injection
public DiscordClient(
    PawSharpOptions options,
    ICacheProvider cache,
    ILogger<DiscordClient> logger,
    IDiscordRestClient restClient)
{
    // Implementation
}
```

**Benefits:**
- Testable components
- Loose coupling
- Configuration flexibility
- Lifetime management

### 3. Async/Await Throughout

All I/O operations are async:

```csharp
// ✅ Async all the way
public async Task<Message> SendMessageAsync(CreateMessageRequest request)
{
    var response = await _httpClient.PostAsync(endpoint, content);
    var message = await DeserializeAsync<Message>(response);
    await _cache.StoreAsync(message);
    return message;
}
```

**Benefits:**
- Non-blocking I/O
- Scalable applications
- Proper resource utilization

## Logging Architecture

PawSharp uses structured logging with Microsoft.Extensions.Logging:

### Logger Injection
```csharp
public class GatewayClient
{
    private readonly ILogger<GatewayClient> _logger;

    public GatewayClient(ILogger<GatewayClient> logger)
    {
        _logger = logger;
    }
}
```

### Logging Patterns
```csharp
// Information logging
_logger.LogInformation("Gateway connected to {ShardCount} shards", shardCount);
_logger.LogInformation("Message {MessageId} created in channel {ChannelId}", message.Id, message.ChannelId);

// Warning logging
_logger.LogWarning("Rate limit hit for {Endpoint}, retrying in {RetryAfter}s", endpoint, retryAfter);

// Error logging
_logger.LogError(ex, "Failed to send message {MessageId} to channel {ChannelId}", messageId, channelId);

// Debug logging (verbose)
_logger.LogDebug("Heartbeat sent, waiting for ACK");
```

### Custom Logging Extensions
```csharp
public static class PawSharpLoggingExtensions
{
    public static ILoggingBuilder AddPawSharpLogging(
        this ILoggingBuilder builder,
        LogLevel minimumLevel = LogLevel.Information)
    {
        builder.AddConsole();
        builder.SetMinimumLevel(minimumLevel);
        return builder;
    }
}
```

## Metrics and Performance Monitoring

PawSharp includes built-in performance tracking:

### Performance Metrics
```csharp
public class PerformanceMetrics
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, TimeSpan> _timings = new();

    public void IncrementCounter(string name) =>
        _counters.AddOrUpdate(name, 1, (_, count) => count + 1);

    public void RecordTiming(string name, TimeSpan duration) =>
        _timings[name] = duration;
}
```

### Memory Metrics
```csharp
public class MemoryMetrics
{
    public long GetCurrentMemoryUsage() =>
        GC.GetTotalMemory(forceFullCollection: false);

    public long GetAllocatedBytesSinceLastGC() =>
        GC.GetAllocatedBytesForCurrentThread();
}
```

### Usage in Components
```csharp
public async Task<Message> CreateMessageAsync(CreateMessageRequest request)
{
    using var timer = _metrics.StartTimer("rest.create_message");

    try
    {
        var message = await _httpClient.PostAsync(endpoint, content);
        _metrics.IncrementCounter("messages.sent");
        return message;
    }
    catch (Exception ex)
    {
        _metrics.IncrementCounter("messages.failed");
        throw;
    }
}
```

## Serialization System

PawSharp uses System.Text.Json with custom converters:

### Snowflake Converter
```csharp
public class SnowflakeConverter : JsonConverter<Snowflake>
{
    public override Snowflake Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return ulong.Parse(reader.GetString()!);
        }
        return reader.GetUInt64();
    }

    public override void Write(Utf8JsonWriter writer, Snowflake value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
```

### Timestamp Converter
```csharp
public class TimestampConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var timestamp = reader.GetString()!;
        return DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O"));
    }
}
```

### Serialization Options
```csharp
public static class PawSharpJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new SnowflakeConverter(),
            new TimestampConverter(),
            new PermissionFlagsConverter()
        }
    };
}
```

## Validation Framework

Input validation prevents invalid API calls:

### Validation Attributes
```csharp
public class CreateMessageRequest
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; }

    [MaxLength(10)]
    public Embed[] Embeds { get; set; }
}
```

### Validation Engine
```csharp
public class RequestValidator
{
    public void Validate(object request)
    {
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(request, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }
    }
}
```

### ID Validation
```csharp
public static class IdValidator
{
    public static void ValidateSnowflake(ulong id, string paramName)
    {
        if (id == 0)
            throw new ValidationException($"{paramName} cannot be zero");

        // Discord IDs should be reasonable (not too old, not in future)
        var createdAt = id.ToSnowflake().CreatedAt;
        var now = DateTimeOffset.UtcNow;

        if (createdAt < new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.Zero))
            throw new ValidationException($"{paramName} appears to be invalid (too old)");

        if (createdAt > now.AddHours(1))
            throw new ValidationException($"{paramName} appears to be invalid (future date)");
    }
}
```

## Caching Strategy

PawSharp implements multi-level caching:

### Cache Interface
```csharp
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task ClearAsync();
}
```

### Memory Cache Implementation
```csharp
public class MemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;

    public MemoryCacheProvider(IOptions<CacheOptions> options)
    {
        _options = options.Value;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxSizeBytes
        });
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return await Task.FromResult(_cache.Get<T>(key));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();

        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;

        options.Size = CalculateSize(value);
        options.Priority = CacheItemPriority.Normal;

        _cache.Set(key, value, options);
        await Task.CompletedTask;
    }
}
```

### Cache Key Strategy
```csharp
public static class CacheKeys
{
    public static string Guild(ulong id) => $"guild:{id}";
    public static string Channel(ulong id) => $"channel:{id}";
    public static string User(ulong id) => $"user:{id}";
    public static string Message(ulong id) => $"message:{id}";
    public static string Member(ulong guildId, ulong userId) => $"member:{guildId}:{userId}";
}
```

## Rate Limiting Implementation

Advanced rate limiting with bucket tracking:

### Rate Limiter Interface
```csharp
public interface IRateLimiter
{
    Task WaitAsync(string bucket, CancellationToken cancellationToken = default);
    void UpdateBucket(string bucket, RateLimitInfo info);
}
```

### Bucket-Based Implementation
```csharp
public class AdvancedRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly ILogger<AdvancedRateLimiter> _logger;

    public async Task WaitAsync(string bucket, CancellationToken cancellationToken = default)
    {
        var rateLimitBucket = _buckets.GetOrAdd(bucket, _ => new RateLimitBucket());

        await rateLimitBucket.WaitAsync(cancellationToken);
    }

    public void UpdateBucket(string bucket, RateLimitInfo info)
    {
        var rateLimitBucket = _buckets.GetOrAdd(bucket, _ => new RateLimitBucket());
        rateLimitBucket.Update(info);
    }
}
```

### Rate Limit Bucket
```csharp
public class RateLimitBucket
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTimeOffset _resetTime;
    private int _remaining;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_remaining <= 0 && DateTimeOffset.UtcNow < _resetTime)
            {
                var delay = _resetTime - DateTimeOffset.UtcNow;
                await Task.Delay(delay, cancellationToken);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Update(RateLimitInfo info)
    {
        _remaining = info.Remaining;
        _resetTime = DateTimeOffset.FromUnixTimeSeconds(info.Reset);
    }
}
```

## Event System Architecture

Typed event dispatching with middleware support:

### Event Dispatcher
```csharp
public class EventDispatcher
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly List<Func<EventContext, Task>> _middleware = new();

    public void On<TEvent>(string eventName, Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();

        _handlers[eventType].Add(handler);
    }

    public async Task DispatchAsync<TEvent>(TEvent eventData)
    {
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlers))
            return;

        var context = new EventContext { Event = eventData, EventType = eventType };

        // Execute middleware
        foreach (var middleware in _middleware)
        {
            await middleware(context);
        }

        // Execute handlers
        foreach (var handler in handlers.Cast<Func<TEvent, Task>>())
        {
            await handler(eventData);
        }
    }

    public void Use(Func<EventContext, Task> middleware)
    {
        _middleware.Add(middleware);
    }
}
```

### Event Context
```csharp
public class EventContext
{
    public object Event { get; set; }
    public Type EventType { get; set; }
    public bool Handled { get; set; }
    public Dictionary<string, object> Items { get; } = new();
}
```

## Testing Patterns

Comprehensive testing strategy:

### Unit Test Example
```csharp
public class RestClientTests
{
    [Fact]
    public async Task CreateMessageAsync_ValidRequest_ReturnsMessage()
    {
        // Arrange
        var mockHttpClient = new Mock<HttpMessageHandler>();
        mockHttpClient.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"123\", \"content\": \"test\"}")
            });

        var client = new RestClient(new HttpClient(mockHttpClient.Object), Options.Create(new PawSharpOptions()));

        // Act
        var result = await client.CreateMessageAsync(456, new CreateMessageRequest { Content = "test" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Content);
        Assert.Equal(123ul, result.Id);
    }
}
```

### Integration Test Example
```csharp
public class DiscordClientIntegrationTests : IAsyncLifetime
{
    private DiscordClient _client;
    private TestServer _testServer;

    public async Task InitializeAsync()
    {
        _testServer = new TestServer();
        var options = new PawSharpOptions { Token = "test-token" };
        _client = new DiscordClient(options, null, null, new RestClient(_testServer.CreateClient(), Options.Create(options)));
    }

    [Fact]
    public async Task ConnectAsync_ValidToken_ConnectsSuccessfully()
    {
        // Test actual connection logic
        await _client.ConnectAsync();
        Assert.True(_client.IsConnected);
    }

    public async Task DisposeAsync()
    {
        await _client.DisconnectAsync();
        _testServer.Dispose();
    }
}
```

## Performance Optimization Techniques

### Memory Pooling
```csharp
public class BufferPool
{
    private readonly ConcurrentBag<byte[]> _buffers = new();
    private readonly int _bufferSize;

    public BufferPool(int bufferSize = 8192)
    {
        _bufferSize = bufferSize;
    }

    public byte[] Rent()
    {
        return _buffers.TryTake(out var buffer) ? buffer : new byte[_bufferSize];
    }

    public void Return(byte[] buffer)
    {
        if (buffer.Length == _bufferSize)
            _buffers.Add(buffer);
    }
}
```

### Object Pooling for Messages
```csharp
public class MessagePool
{
    private readonly ConcurrentQueue<Message> _pool = new();

    public Message Rent()
    {
        return _pool.TryDequeue(out var message) ? message : new Message();
    }

    public void Return(Message message)
    {
        // Reset message properties
        message.Id = 0;
        message.Content = null;
        message.Author = null;
        // ... reset other properties

        _pool.Enqueue(message);
    }
}
```

## Security Considerations

### Token Handling
```csharp
public class PawSharpOptions
{
    private string _token;

    public string Token
    {
        get => _token;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Token cannot be null or empty");

            // Basic validation
            if (value.Length < 50)
                throw new ArgumentException("Token appears to be invalid");

            _token = value;
        }
    }
}
```

### Input Sanitization
```csharp
public static class InputSanitizer
{
    public static string SanitizeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        // Remove potential injection attempts
        return content
            .Replace("@everyone", "@\u200beveryone")
            .Replace("@here", "@\u200bhere");
    }
}
```

## Conclusion

These developmental practices ensure PawSharp is:
- **Reliable**: Exception-first error handling
- **Performant**: Efficient caching, async I/O, metrics
- **Maintainable**: DI, clean architecture, comprehensive testing
- **Secure**: Input validation, proper token handling
- **Extensible**: Middleware, plugin architecture, clean interfaces

Following these patterns when contributing will help maintain code quality and consistency across the codebase.