# Logging

PawSharp integrates with `Microsoft.Extensions.Logging` throughout all layers, using structured logging with event IDs, log levels appropriate to severity, and support for custom providers.

---

## ILogger Integration

All major components accept `ILogger<T>` or `ILogger` via constructor injection:

```csharp
public class DiscordRestClient
{
 private readonly ILogger<DiscordRestClient> _logger;
}
```

---

## Log Levels Used by PawSharp

| Level | Usage |
|-------|-------|
| `Trace` | Raw WebSocket frame dumps (rare) |
| `Debug` | Gateway message opcodes, cache hits/misses, state transitions |
| `Information` | Connection established, ready received, shard connected |
| `Warning` | Rate limit applied, 429 responses, reconnect scheduled, invalid session |
| `Error` | API failures, deserialization errors, reconnection failed, zombie connection |
| `Critical` | Unexpected fatal state (rare) |

---

## Console Logging

```csharp
services.AddLogging(builder =>
{
 builder.AddConsole();
 builder.SetMinimumLevel(LogLevel.Information);
 builder.AddFilter("PawSharp.API", LogLevel.Warning);
 builder.AddFilter("PawSharp.Gateway", LogLevel.Information);
});
```

---

## Structured Logging with Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
 .MinimumLevel.Information()
 .Enrich.FromLogContext()
 .WriteTo.Console()
 .WriteTo.File("logs/pawsharp-.log", rollingInterval: RollingInterval.Day)
 .CreateLogger();

services.AddLogging(builder =>
{
 builder.ClearProviders();
 builder.AddSerilog();
});
```

---

## Custom Logger Providers

```csharp
public class DatabaseLoggerProvider : ILoggerProvider
{
 public ILogger CreateLogger(string categoryName)
 => new DatabaseLogger(categoryName);
}

services.AddLogging(builder =>
{
 builder.AddProvider(new DatabaseLoggerProvider());
});
```

---

## Log Event IDs

PawSharp defines reusable event IDs in `PawSharpLogEvents`:

```csharp
public static class PawSharpLogEvents
{
 public static readonly EventId ApiRequestStarted = new(1001, "ApiRequestStarted");
 public static readonly EventId ApiRequestCompleted = new(1002, "ApiRequestCompleted");
 public static readonly EventId ApiRequestFailed = new(1003, "ApiRequestFailed");
 public static readonly EventId CacheHit = new(2001, "CacheHit");
 public static readonly EventId CacheMiss = new(2002, "CacheMiss");
 public static readonly EventId CacheEviction = new(2003, "CacheEviction");
}
```

Filter by event ID:

```csharp
builder.AddFilter("PawSharp", ev => ev < 2000 || ev > 3000);
```

---

## Performance Impact of Logging

- `Debug`/`Trace` level: significant allocation from string interpolation at scale (high-event bots)
- `Information` level: minimal overhead, recommended for production
- Structured logging with named placeholders (`{UserId}`) avoids allocation until the message is actually emitted

```csharp
//  Good - structured, no allocation if level not enabled
_logger.LogDebug(PawSharpLogEvents.CacheHit, "Cache hit for {EntityType} {EntityId}", "Guild", guildId);

//  Bad - string interpolation allocates regardless
_logger.LogDebug($"Cache hit for Guild {guildId}");
```

---

## Complete Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
 builder.AddConsole();
 builder.AddDebug();
 builder.SetMinimumLevel(LogLevel.Information);
 builder.AddFilter("PawSharp.Gateway", LogLevel.Debug);
 builder.AddFilter("PawSharp.API", LogLevel.Warning);
});

services.AddSingleton(options);
services.AddPawSharp();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();

// Output:
// info: PawSharp.Gateway.GatewayClient[0]
// Connected to Discord Gateway.
// dbug: PawSharp.Gateway.GatewayClient[0]
// Received Gateway message: op=0, t=MESSAGE_CREATE, seq=42
// warn: PawSharp.API.Clients.DiscordRestClient[1003]
// API request failed: POST /channels/123/messages 429
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Using `$"string {interpolation}"` in log calls | Use structured `{Placeholder}` syntax |
| Setting `SetMinimumLevel(LogLevel.Trace)` in production | Causes high CPU/allocation overhead |
| Not filtering noisy namespaces | Add `builder.AddFilter("PawSharp.Gateway.EventDispatcher", LogLevel.Warning)` |
| Disposing `ILogger<T>` manually | Let DI handle lifetime |
| Logging sensitive data (tokens, user IPs) | PawSharp sanitizes via `LogSanitizer` |
