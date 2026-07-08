# Error Handling

PawSharp provides a structured exception hierarchy, configurable throw behavior, detailed error context, and automatic reconnection recovery.

---

## Exception Types

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

Base type for all library errors.

```csharp
try { await client.ConnectAsync(); }
catch (DiscordException ex) { _logger.LogError(ex, "Library error"); }
```

### DiscordApiException

Thrown when Discord returns an error HTTP response. Includes:

```csharp
public class DiscordApiException : DiscordException
{
    public HttpStatusCode? StatusCode { get; }
    public int? DiscordErrorCode { get; }
    public string? DiscordErrorMessage { get; }
    public string RequestMethod { get; }
    public string RequestEndpoint { get; }
}
```

```csharp
catch (DiscordApiException ex)
{
    // StatusCode: 403, DiscordErrorCode: 50013, DiscordErrorMessage: "Missing Permissions"
    // RequestMethod: "POST", RequestEndpoint: "/channels/123/messages"
}
```

### GatewayException

WebSocket connection issues with recovery info:

```csharp
public class GatewayException : DiscordException
{
    public int? Opcode { get; }
    public string? EventType { get; }
    public bool IsRecoverable { get; }
}
```

### ValidationException

Input validation that fails before the API call:

```csharp
catch (ValidationException ex)
{
    // ParameterName: "limit", InvalidValue: 500
    // Message: "Limit must be between 1 and 100"
}
```

### RateLimitException

Discord rate limit hit:

```csharp
catch (RateLimitException ex)
{
    // RetryAfter: 5s, IsGlobal: false, Bucket: "abc123"
}
```

### DeserializationException

JSON parsing failures (API schema mismatch):

```csharp
catch (DeserializationException ex)
{
    // TargetType: typeof(Guild), RawJson: "{...}"
}
```

---

## ThrowOnApiError Configuration

```csharp
var options = new PawSharpOptions
{
    RestApi = new PawSharpOptions.RestApiOptions
    {
        ThrowOnApiError = true  // Default: true
    }
};
```

When `false`, methods that return `T?` silently return `null` on API errors.

---

## TryXxx Methods

Some operations use a try-pattern that catches exceptions internally:

```csharp
var message = await rest.CreateMessageAsync(channelId, request);
// Returns null (not throws) on API error when ThrowOnApiError=false
```

---

## Global Exception Handlers

```csharp
client.Gateway.OnStateChanged += async (oldState, newState) =>
{
    if (newState == GatewayState.Failed)
        _logger.LogCritical("Gateway entered failed state");
};
```

Hook into `ReconnectionManager` events:

```csharp
client.Gateway.Events.Use(async (eventName, eventData) =>
{
    try { /* process event */ }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Event handler failed for {Event}", eventName);
    }
});
```

---

## Reconnection Error Recovery

`ReconnectionManager` (`src/PawSharp.Gateway/ReconnectionManager.cs`) uses exponential backoff with jitter:

```csharp
private readonly int _maxReconnectionAttempts;
private readonly int _initialBackoffMs;  // default: 1000
private readonly int _maxBackoffMs;      // default: 30000
private readonly double _jitterFactor;   // default: 0.2
```

```csharp
var delay = _currentBackoffMs;
var jitter = (int)(delay * _jitterFactor * (2.0 * Random.Shared.NextDouble() - 1.0));
delay = Math.Max(0, delay + jitter);
_currentBackoffMs = Math.Min(delay * 2, _maxBackoffMs);
await Task.Delay(delay);
```

---

## Gateway Close Codes

From `GatewayCloseCode` enum in `src/PawSharp.Gateway/GatewayClient.cs`:

| Code | Name | Recoverable? |
|------|------|--------------|
| 4001 | UnknownOpcode | Yes |
| 4002 | DecodeError | Yes |
| 4003 | NotAuthenticated | No |
| 4004 | AuthenticationFailed | No |
| 4005 | AlreadyAuthenticated | Yes |
| 4007 | InvalidSequence | Yes (resets session) |
| 4008 | RateLimited | Yes |
| 4009 | SessionTimedOut | Yes (resets session) |
| 4010 | InvalidShard | No |
| 4011 | ShardingRequired | No |
| 4012 | InvalidApiVersion | No |
| 4013 | InvalidIntent | No |
| 4014 | DisallowedIntent | No |
| 4015 | VoiceServerCrashed | Yes |

---

## Common Discord API Error Codes

| Code | Meaning |
|------|---------|
| 50001 | Missing Access |
| 50013 | Missing Permissions |
| 10003 | Unknown Channel |
| 10004 | Unknown Guild |
| 10007 | Unknown Member |
| 10008 | Unknown Message |
| 10011 | Unknown Role |
| 20012 | Max Guilds Reached |
| 20031 | Rate Limited |
| 50009 | Unauthorized |

---

## Best Practices

```csharp
// ✅ Specific exceptions first
try { await rest.CreateMessageAsync(channelId, request); }
catch (ValidationException ex) { /* fix input */ }
catch (RateLimitException ex) { /* handle backoff */ }
catch (DiscordApiException ex) when (ex.DiscordErrorCode == 50013) { /* no perms */ }
catch (DiscordApiException ex) { /* general API error */ }
catch (GatewayException ex) when (ex.IsRecoverable) { /* reconnect */ }
catch (DiscordException ex) { /* all library errors */ }

// ❌ Catching Exception broadly
catch (Exception ex) { /* hides library-specific context */ }
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Not checking `ex.IsRecoverable` on gateway errors | Auth failures (4004) should not auto-retry |
| Ignoring `ValidationException` | Fix inputs rather than retrying |
| Not handling `RateLimitException` externally | PawSharp retries internally, but you may want custom backoff |
| Forgetting `DeserializationException` | Indicates API schema change — log raw JSON |
| Catching `Exception` and returning null | Swallows programming errors |
