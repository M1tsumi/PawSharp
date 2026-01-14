#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.API.RateLimit;

/// <summary>
/// Abstraction for advanced rate limiting used by REST client integration.
/// </summary>
public interface IAdvancedRateLimiter
{
    Task WaitForRateLimitAsync(string route, string? bucketHash = null);

    void UpdateRateLimits(string route, string? bucketHash, int? remaining, DateTimeOffset? resetAt, bool isGlobal = false);

    void MarkRequestComplete(string route, string? bucketHash = null);
}
