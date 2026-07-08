# REST Pipeline

The REST pipeline handles every HTTP request to Discord's API with rate limiting, retry, error handling, and serialization.

---

## Request Flow

```
┌──────────┐ ┌──────────────┐ ┌───────────────┐ ┌──────────┐ ┌───────────┐
│ User │───>│ SendRequest │───>│ Rate Limiter │───>│ HttpClient│───>│ Discord │
│ Code │ │ Async │ │ (bucket-based)│ │ │ │ API │
└──────────┘ └──────────────┘ └───────────────┘ └──────────┘ └───────────┘
 │ │ │ │
 │ │ │ │
 v v v v
 ┌──────────────┐ ┌───────────────┐ ┌──────────┐ ┌───────────┐
 │ Buffer Body │ │ Global Rate │ │ Auth │ │ Response │
 │ for Retry │ │ Limit Check │ │ Header │ │ Parsing │
 └──────────────┘ └───────────────┘ └──────────┘ └───────────┘
 │
 v
 ┌──────────────┐
 │ Headers │
 │ Parsed │
 │ (bucket, │
 │ remaining, │
 │ reset) │
 └──────────────┘
```

### SendRequestAsync

`DiscordRestClient.SendRequestAsync` (`src/PawSharp.API/Clients/RestClient.cs:2994-3172`) handles:

1. **Body buffering** - reads request body into byte array for retry safety
2. **Global rate limit check** - waits if `_globalReset` is in the future
3. **Per-route rate limit** - calls `_rateLimiter.WaitForRateLimitAsync(route)`
4. **Request construction** - sets `Authorization: Bot <token>`, `User-Agent`, audit log reason
5. **HTTP send** - via `_httpClient.SendAsync`
6. **Rate limit header parsing** - extracts `X-RateLimit-Bucket`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
7. **429 handling** - parses `Retry-After`, updates bucket, retries up to `MaxRateLimitRetries`
8. **Cleanup** - calls `_rateLimiter.MarkRequestComplete(route, bucketHash)`

---

## Rate Limiting

```csharp
// Global rate limit
if (DateTimeOffset.UtcNow < _globalReset)
{
 delay = _globalReset - DateTimeOffset.UtcNow;
 await Task.Delay(delay, ct);
}

// Per-route bucket
await _rateLimiter.WaitForRateLimitAsync(route, cancellationToken: ct);

// On 429, update and retry
_rateLimiter.UpdateRateLimits(route, bucketHash, 0, resetAt, isGlobal);
return await SendRequestAsync(method, endpoint, null, reason, ct,
 retryCount + 1, bufferedContentBytes, bufferedContentType);
```

---

## Serialization Pipeline

### JSON Serializer Options

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
 PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, // guild_id, channel_id
 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
 NumberHandling = JsonNumberHandling.AllowReadingFromString,
 TypeInfoResolver = JsonTypeInfoResolver.Combine(
 PawSharpApiJsonContext.Default,
 PawSharpJsonContext.Default)
};
```

### Source-Generated Contexts

- `PawSharpApiJsonContext` - request/response models for API calls
- `PawSharpJsonContext` - core entity serialization

---

## Bucket Tracking

```csharp
private void ParseAndUpdateRateLimits(HttpResponseMessage response, string route, ref string? bucketHash)
{
 if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var bucketValues))
 bucketHash = bucketValues.FirstOrDefault();

 int? remaining = null;
 if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remValues))
 remaining = int.Parse(remValues.FirstOrDefault()!);

 DateTimeOffset? resetAt = null;
 if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
 resetAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(resetValues.FirstOrDefault()!));

 _rateLimiter.UpdateRateLimits(route, bucketHash, remaining, resetAt);
}
```

---

## Global Rate Limit Sync

When Discord sends `X-RateLimit-Global: true`, the global reset time is stored and all subsequent requests wait:

```csharp
if (HeaderValueIsTrue(response, "X-RateLimit-Global"))
{
 _globalReset = DateTimeOffset.UtcNow.AddSeconds(retryAfterSecs);
}
```

---

## Extending the Pipeline

1. **Custom HTTP handlers** via `IHttpClientFactory`:
```csharp
services.AddHttpClient<IDiscordRestClient, DiscordRestClient>(client =>
{
 client.BaseAddress = new Uri("https://discord.com/api/v10/");
}).AddHttpMessageHandler<MyLoggingHandler>();
```

2. **Custom rate limiter** - implement `IAdvancedRateLimiter` and register:
```csharp
services.AddSingleton<IAdvancedRateLimiter, MyRateLimiter>();
```

3. **Custom serialization** - modify `_jsonOptions` TypeInfoResolver:
```csharp
// Add your own types to the resolver chain
TypeInfoResolver = JsonTypeInfoResolver.Combine(
 MyCustomContext.Default,
 PawSharpApiJsonContext.Default,
 PawSharpJsonContext.Default)
```

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Disposing `HttpClient` per request | Use `IHttpClientFactory` |
| Not handling `HttpRequestException` | Caught and wrapped in `DiscordException` |
| Ignoring `retryCount >= MaxRateLimitRetries` | Returns 429 instead of infinite loop |
| Not buffering request body for retry | PawSharp buffers body as bytes automatically |
