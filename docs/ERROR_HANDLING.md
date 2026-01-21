# PawSharp Error Handling Guide

PawSharp uses typed exceptions for all error scenarios. This guide explains the main exception types and how to handle them.

## Exception Types

- **ValidationException**: Thrown for invalid input (IDs, content length, etc.)
- **RateLimitException**: Thrown when Discord's rate limits are hit. Includes `RetryAfter` property.
- **DiscordApiException**: Thrown for API errors (status code, message).
- **GatewayException**: Thrown for WebSocket/gateway issues.
- **DeserializationException**: Thrown for JSON parsing errors.

## Example

```csharp
try {
    var message = await client.Rest.CreateMessageAsync(channelId, request);
} catch (ValidationException ex) {
    Console.WriteLine($"Validation error: {ex.Message}");
} catch (RateLimitException ex) {
    Console.WriteLine($"Rate limited, retry in {ex.RetryAfter}s");
    await Task.Delay(ex.RetryAfter * 1000);
} catch (DiscordApiException ex) {
    Console.WriteLine($"Discord error {ex.StatusCode}: {ex.Message}");
}
```

## Best Practices

- Always catch the most specific exception first.
- Log errors for diagnostics.
- Use retry logic for rate limits and transient gateway errors.

---

For more, see the [README](../README.md#error-handling) and XML comments in code.