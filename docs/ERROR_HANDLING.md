# Error Handling Guide

Comprehensive guide to handling errors in PawSharp for developers.

## Table of Contents

1. [Exception Hierarchy](#exception-hierarchy)
2. [Common Error Scenarios](#common-error-scenarios)
3. [Best Practices](#best-practices)
4. [Debugging Errors](#debugging-errors)
5. [Custom Error Handling](#custom-error-handling)
6. [Logging](#logging)
7. [Error Recovery Strategies](#error-recovery-strategies)

---

## Exception Hierarchy

PawSharp provides a structured exception hierarchy for different types of errors:

```
Exception
└── DiscordException (base)
    ├── DiscordApiException (REST API errors)
    ├── GatewayException (WebSocket connection errors)
    ├── ValidationException (Input validation errors)
    ├── RateLimitException (Rate limiting errors)
    └── DeserializationException (JSON parsing errors)
```

### DiscordException

Base exception for all PawSharp-related errors.

```csharp
try
{
    // PawSharp operation
}
catch (DiscordException ex)
{
    // Handle any PawSharp-specific error
    Console.WriteLine($"PawSharp error: {ex.Message}");
}
```

### DiscordApiException

Thrown when Discord's REST API returns an error response. Contains detailed context:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (DiscordApiException ex)
{
    // Access detailed error information
    Console.WriteLine($"Status Code: {ex.StatusCode}");
    Console.WriteLine($"Discord Error Code: {ex.DiscordErrorCode}");
    Console.WriteLine($"Discord Error Message: {ex.DiscordErrorMessage}");
    Console.WriteLine($"Request: {ex.RequestMethod} {ex.RequestEndpoint}");
    
    // Example output:
    // Status Code: 403
    // Discord Error Code: 50001
    // Discord Error Message: Missing Access
    // Request: POST /channels/123/messages
}
```

**Common Discord Error Codes:**
- `50001` - Missing Access
- `50013` - Missing Permissions
- `10003` - Unknown Channel
- `10004` - Unknown Guild
- `10007` - Unknown Member
- `10008` - Unknown Message
- `10011` - Unknown Role
- `20012` - Max Guilds Reached
- `20016` - Max Friends Reached
- `20018` - Max Pins Reached
- `20028` - Invalid API Version
- `20031` - Rate Limited
- `50009` - Unauthorized

### GatewayException

Thrown when WebSocket connection issues occur. Includes recoverability information:

```csharp
try
{
    await client.ConnectAsync();
}
catch (GatewayException ex)
{
    Console.WriteLine($"Gateway error: {ex.Message}");
    Console.WriteLine($"Opcode: {ex.Opcode}");
    Console.WriteLine($"Event Type: {ex.EventType}");
    Console.WriteLine($"Is Recoverable: {ex.IsRecoverable}");
    
    if (ex.IsRecoverable)
    {
        // Attempt reconnection
        await Task.Delay(TimeSpan.FromSeconds(5));
        await client.ConnectAsync();
    }
    else
    {
        // Fatal error - manual intervention required
        throw;
    }
}
```

### ValidationException

Thrown when input validation fails before making API requests:

```csharp
try
{
    await client.Rest.GetChannelMessagesAsync(channelId, limit: 500); // Max is 100
}
catch (ValidationException ex)
{
    Console.WriteLine($"Parameter: {ex.ParameterName}");
    Console.WriteLine($"Invalid Value: {ex.InvalidValue}");
    Console.WriteLine($"Error: {ex.Message}");
    
    // Example output:
    // Parameter: limit
    // Invalid Value: 500
    // Error: Limit must be between 1 and 100
}
```

### RateLimitException

Thrown when rate limiting occurs. Includes retry information:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Retry After: {ex.RetryAfter.TotalSeconds} seconds");
    Console.WriteLine($"Is Global: {ex.IsGlobal}");
    Console.WriteLine($"Bucket: {ex.Bucket}");
    
    // Automatic retry with backoff
    await Task.Delay(ex.RetryAfter);
    await client.Rest.CreateMessageAsync(channelId, request);
}
```

**Note:** PawSharp includes built-in rate limiting. You typically won't see this exception unless you bypass the rate limiter or hit global rate limits.

### DeserializationException

Thrown when JSON deserialization fails. Includes the raw JSON and target type:

```csharp
try
{
    var guild = await client.Rest.GetGuildAsync(guildId);
}
catch (DeserializationException ex)
{
    Console.WriteLine($"Target Type: {ex.TargetType}");
    Console.WriteLine($"Raw JSON: {ex.RawJson}");
    Console.WriteLine($"Error: {ex.Message}");
    
    // This helps diagnose API changes or malformed responses
}
```

---

## Common Error Scenarios

### 1. Authentication Errors

```csharp
try
{
    await client.ConnectAsync();
}
catch (GatewayException ex) when (ex.Message.Contains("Invalid token"))
{
    _logger.LogError("Invalid Discord token. Check your PawSharpOptions.Token configuration.");
    throw new InvalidOperationException("Discord token is invalid or expired. Please update your token.", ex);
}
```

### 2. Permission Errors

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (DiscordApiException ex) when (ex.DiscordErrorCode == "50013")
{
    _logger.LogWarning(ex, "Missing permissions for channel {ChannelId}. Required permissions: {RequiredPermissions}", 
        channelId, "SEND_MESSAGES");
    
    // User-friendly error response
    await client.Rest.CreateMessageAsync(channelId, new()
    {
        Content = "❌ I don't have permission to send messages in this channel."
    });
}
```

### 3. Rate Limiting

```csharp
public async Task<T> WithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    int attempts = 0;
    
    while (attempts < maxRetries)
    {
        try
        {
            return await operation();
        }
        catch (RateLimitException ex)
        {
            attempts++;
            _logger.LogWarning(ex, "Rate limited on attempt {Attempt}/{MaxAttempts}. Waiting {RetryAfter}s", 
                attempts, maxRetries, ex.RetryAfter.TotalSeconds);
            
            if (attempts >= maxRetries)
                throw;
            
            await Task.Delay(ex.RetryAfter);
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}
```

### 4. Network Errors

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Network error while sending message to channel {ChannelId}", channelId);
    
    // Implement retry logic for transient network failures
    if (IsTransientNetworkError(ex))
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        await client.Rest.CreateMessageAsync(channelId, request);
    }
    else
    {
        throw;
    }
}
catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
{
    _logger.LogError(ex, "Request timed out for channel {ChannelId}", channelId);
    throw new TimeoutException("Request timed out. Check your network connection.", ex);
}
```

### 5. Invalid Input

```csharp
try
{
    var messages = await client.Rest.GetChannelMessagesAsync(channelId, limit: 150);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed: {Parameter} = {Value}", ex.ParameterName, ex.InvalidValue);
    
    // Automatically correct common mistakes
    if (ex.ParameterName == "limit" && (int)ex.InvalidValue! > 100)
    {
        _logger.LogInformation("Adjusting limit to maximum allowed value (100)");
        var messages = await client.Rest.GetChannelMessagesAsync(channelId, limit: 100);
    }
}
```

---

## Best Practices

### 1. Always Handle Specific Exceptions First

```csharp
// ❌ Bad - catches all exceptions
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// ✅ Good - handles specific exceptions
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
}
catch (RateLimitException ex)
{
    _logger.LogWarning(ex, "Rate limited. Retry after: {RetryAfter}s", ex.RetryAfter.TotalSeconds);
    await Task.Delay(ex.RetryAfter);
}
catch (DiscordApiException ex)
{
    _logger.LogError(ex, "API error: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
}
catch (Exception ex)
{
    _logger.LogCritical(ex, "Unexpected error");
    throw;
}
```

### 2. Use Structured Logging

```csharp
// ❌ Bad - string interpolation
_logger.LogError($"Error sending message: {ex.Message}");

// ✅ Good - structured logging with context
_logger.LogError(ex, "Error sending message to channel {ChannelId}", channelId);
```

### 3. Provide Context in Error Messages

```csharp
// ❌ Bad - generic error message
throw new InvalidOperationException("Operation failed");

// ✅ Good - specific error with context
throw new InvalidOperationException(
    $"Failed to ban user {userId} from guild {guildId}. " +
    $"Reason: Missing permission BAN_MEMBERS. " +
    $"Bot role position: {botRolePosition}, Target role position: {targetRolePosition}",
    ex);
```

### 4. Don't Swallow Exceptions

```csharp
// ❌ Bad - silently swallows exception
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (Exception)
{
    // Do nothing
}

// ✅ Good - logs and rethrows or handles appropriately
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send message");
    throw; // Rethrow to let caller handle
}
```

### 5. Use Exception Filters

```csharp
// ✅ Good - exception filters for cleaner code
try
{
    await client.ConnectAsync();
}
catch (GatewayException ex) when (ex.IsRecoverable)
{
    _logger.LogWarning(ex, "Recoverable gateway error, retrying...");
    await Task.Delay(TimeSpan.FromSeconds(5));
    await client.ConnectAsync();
}
catch (GatewayException ex)
{
    _logger.LogError(ex, "Fatal gateway error");
    throw;
}
```

---

## Debugging Errors

### Enable Detailed Logging

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug); // Show debug messages
    
    // Enable debug logging for specific namespaces
    builder.AddFilter("PawSharp.API", LogLevel.Debug);
    builder.AddFilter("PawSharp.Gateway", LogLevel.Debug);
    builder.AddFilter("PawSharp.Client", LogLevel.Debug);
});
```

### Capture Full Exception Details

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed with full details:");
    _logger.LogError("Exception Type: {Type}", ex.GetType().FullName);
    _logger.LogError("Message: {Message}", ex.Message);
    _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
    
    if (ex is DiscordApiException apiEx)
    {
        _logger.LogError("Status Code: {StatusCode}", apiEx.StatusCode);
        _logger.LogError("Discord Error Code: {Code}", apiEx.DiscordErrorCode);
        _logger.LogError("Discord Error Message: {DiscordMessage}", apiEx.DiscordErrorMessage);
        _logger.LogError("Request: {Method} {Endpoint}", apiEx.RequestMethod, apiEx.RequestEndpoint);
    }
    
    if (ex.InnerException != null)
    {
        _logger.LogError("Inner Exception: {InnerType} - {InnerMessage}", 
            ex.InnerException.GetType().FullName, ex.InnerException.Message);
    }
    
    throw;
}
```

### Use Developer Exception Page (ASP.NET Core)

If using PawSharp with ASP.NET Core:

```csharp
services.AddControllers()
    .AddNewtonsoftJson();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

### Create Error Handler Middleware

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DiscordApiException ex)
        {
            _logger.LogError(ex, "Discord API error");
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Discord API Error",
                Message = ex.Message,
                StatusCode = ex.StatusCode,
                DiscordErrorCode = ex.DiscordErrorCode
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Validation Error",
                Message = ex.Message,
                Parameter = ex.ParameterName,
                InvalidValue = ex.InvalidValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = "Internal Server Error",
                Message = "An unexpected error occurred"
            });
        }
    }
}
```

---

## Custom Error Handling

### Create Custom Exceptions

```csharp
public class BotConfigurationException : DiscordException
{
    public string ConfigurationKey { get; }
    public object? InvalidValue { get; }

    public BotConfigurationException(string configurationKey, object? invalidValue, string message)
        : base(message)
    {
        ConfigurationKey = configurationKey;
        InvalidValue = invalidValue;
    }
}

// Usage
if (string.IsNullOrEmpty(options.Token))
{
    throw new BotConfigurationException(
        nameof(options.Token),
        options.Token,
        "Discord bot token cannot be null or empty. Set it in PawSharpOptions or DISCORD_TOKEN environment variable.");
}
```

### Create Error Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public Exception? Exception { get; private set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error, Exception? ex = null) 
        => new() { IsSuccess = false, Error = error, Exception = ex };
}

// Usage
public async Task<Result<Message>> TrySendAsync(ulong channelId, CreateMessageRequest request)
{
    try
    {
        var message = await client.Rest.CreateMessageAsync(channelId, request);
        return Result<Message>.Success(message);
    }
    catch (DiscordApiException ex)
    {
        return Result<Message>.Failure($"API error: {ex.Message}", ex);
    }
    catch (Exception ex)
    {
        return Result<Message>.Failure($"Unexpected error: {ex.Message}", ex);
    }
}
```

### Global Error Handler

```csharp
public class GlobalErrorHandler
{
    private readonly ILogger<GlobalErrorHandler> _logger;

    public GlobalErrorHandler(ILogger<GlobalErrorHandler> logger)
    {
        _logger = logger;
    }

    public void HandleException(Exception ex, string context = "")
    {
        switch (ex)
        {
            case ValidationException validationEx:
                _logger.LogWarning(validationEx, "Validation error in {Context}: {Parameter} = {Value}", 
                    context, validationEx.ParameterName, validationEx.InvalidValue);
                break;
                
            case RateLimitException rateLimitEx:
                _logger.LogWarning(rateLimitEx, "Rate limit hit in {Context}. Retry after: {RetryAfter}s", 
                    context, rateLimitEx.RetryAfter.TotalSeconds);
                break;
                
            case DiscordApiException apiEx:
                _logger.LogError(apiEx, "API error in {Context}: {StatusCode} - {DiscordMessage}", 
                    context, apiEx.StatusCode, apiEx.DiscordErrorMessage);
                break;
                
            case GatewayException gatewayEx:
                if (gatewayEx.IsRecoverable)
                {
                    _logger.LogWarning(gatewayEx, "Recoverable gateway error in {Context}", context);
                }
                else
                {
                    _logger.LogError(gatewayEx, "Fatal gateway error in {Context}", context);
                }
                break;
                
            default:
                _logger.LogCritical(ex, "Unexpected error in {Context}", context);
                break;
        }
    }
}
```

---

## Logging

### Configure Log Levels

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    
    // Production: Information and above
    if (builder.Environment.IsProduction())
    {
        builder.SetMinimumLevel(LogLevel.Information);
    }
    // Development: Debug and above
    else
    {
        builder.SetMinimumLevel(LogLevel.Debug);
    }
    
    // Fine-tune specific namespaces
    builder.AddFilter("PawSharp.API", LogLevel.Information);
    builder.AddFilter("PawSharp.Gateway", LogLevel.Information);
    builder.AddFilter("PawSharp.Voice", LogLevel.Warning); // Voice can be noisy
});
```

### Structured Logging Best Practices

```csharp
// ✅ Good - structured with named parameters
_logger.LogInformation("User {UserId} sent command {Command} in channel {ChannelId}", 
    userId, command, channelId);

// ❌ Bad - string interpolation
_logger.LogInformation($"User {userId} sent command {command} in channel {channelId}");
```

### Log Error Context

```csharp
catch (DiscordApiException ex)
{
    _logger.LogError(ex, "Failed to {Operation} for {ResourceType} {ResourceId}: {DiscordError}",
        "CreateMessage",
        "Channel",
        channelId,
        ex.DiscordErrorMessage);
}
```

---

## Error Recovery Strategies

### Exponential Backoff

```csharp
public async Task<T> WithExponentialBackoffAsync<T>(
    Func<Task<T>> operation,
    int maxRetries = 5,
    TimeSpan? initialDelay = null)
{
    int attempts = 0;
    TimeSpan delay = initialDelay ?? TimeSpan.FromSeconds(1);
    
    while (attempts < maxRetries)
    {
        try
        {
            return await operation();
        }
        catch (RateLimitException ex)
        {
            attempts++;
            
            if (attempts >= maxRetries)
                throw;
            
            _logger.LogWarning(ex, "Attempt {Attempt}/{MaxRetries} failed. Waiting {Delay}s", 
                attempts, maxRetries, delay.TotalSeconds);
            
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
        }
        catch (HttpRequestException ex) when (IsTransientNetworkError(ex))
        {
            attempts++;
            
            if (attempts >= maxRetries)
                throw;
            
            _logger.LogWarning(ex, "Network error on attempt {Attempt}/{MaxRetries}. Retrying...", 
                attempts, maxRetries);
            
            await Task.Delay(delay);
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}

private bool IsTransientNetworkError(HttpRequestException ex)
{
    // Add logic to identify transient network errors
    return true;
}
```

### Circuit Breaker Pattern

```csharp
public class CircuitBreaker
{
    private readonly TimeSpan _openTimeout;
    private readonly int _failureThreshold;
    private int _failureCount;
    private DateTime? _lastFailureTime;
    private CircuitState _state = CircuitState.Closed;

    public CircuitBreaker(TimeSpan openTimeout, int failureThreshold = 5)
    {
        _openTimeout = openTimeout;
        _failureThreshold = failureThreshold;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        if (_state == CircuitState.Open)
        {
            if (_lastFailureTime.HasValue && DateTime.UtcNow - _lastFailureTime.Value < _openTimeout)
            {
                throw new InvalidOperationException("Circuit breaker is open");
            }
            
            _state = CircuitState.HalfOpen;
        }

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        _failureCount = 0;
        _state = CircuitState.Closed;
    }

    private void OnFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
        
        if (_failureCount >= _failureThreshold)
        {
            _state = CircuitState.Open;
        }
    }

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }
}
```

### Graceful Degradation

```csharp
public async Task<Message?> SendMessageWithFallback(ulong channelId, CreateMessageRequest request)
{
    try
    {
        // Primary: Send message with embed
        return await client.Rest.CreateMessageAsync(channelId, request);
    }
    catch (DiscordApiException ex) when (ex.DiscordErrorCode == "50013")
    {
        _logger.LogWarning(ex, "Missing permissions for embed, falling back to plain text");
        
        // Fallback: Send plain text message
        return await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
        {
            Content = request.Content ?? "Message could not be displayed due to missing permissions"
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send message");
        return null;
    }
}
```

---

## Additional Resources

- [Troubleshooting Guide](./TROUBLESHOOTING.md) - Common issues and solutions
- [Developers Guide](./DEVELOPERS_GUIDE.md) - General development guide
- [REST API Guide](./REST_API_GUIDE.md) - REST API usage
- [Gateway Guide](./GATEWAY_GUIDE.md) - Gateway and event handling

---

## Quick Reference

| Exception | When Thrown | Key Properties |
|-----------|-------------|----------------|
| `DiscordApiException` | REST API error | `StatusCode`, `DiscordErrorCode`, `DiscordErrorMessage`, `RequestMethod`, `RequestEndpoint` |
| `GatewayException` | WebSocket error | `Opcode`, `EventType`, `IsRecoverable` |
| `ValidationException` | Input validation fails | `ParameterName`, `InvalidValue` |
| `RateLimitException` | Rate limit hit | `RetryAfter`, `IsGlobal`, `Bucket` |
| `DeserializationException` | JSON parse fails | `RawJson`, `TargetType` |

---

**Need more help?** Check the [troubleshooting guide](./TROUBLESHOOTING.md) or open an issue on GitHub.
