#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.API.RateLimit;

/// <summary>
/// Advanced rate limiter with per-route bucket management.
/// </summary>
public class AdvancedRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly SemaphoreSlim _globalLimitSemaphore = new(1, 1);
    private DateTimeOffset _globalResetAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Wait for rate limit clearance before executing a request.
    /// </summary>
    /// <param name="route">The API route (e.g., "POST /channels/{channel.id}/messages")</param>
    /// <param name="bucketHash">The bucket hash from Discord's X-RateLimit-Bucket header</param>
    public async Task WaitForRateLimitAsync(string route, string? bucketHash = null)
    {
        // Check global rate limit first
        if (DateTimeOffset.UtcNow < _globalResetAt)
        {
            var globalDelay = _globalResetAt - DateTimeOffset.UtcNow;
            await Task.Delay(globalDelay);
        }

        // Get or create bucket for this route
        var bucketKey = bucketHash ?? route;
        var bucket = _buckets.GetOrAdd(bucketKey, _ => new RateLimitBucket());

        await bucket.WaitAsync();
    }

    /// <summary>
    /// Update rate limit information from response headers.
    /// </summary>
    public void UpdateRateLimits(string route, string? bucketHash, int? remaining, DateTimeOffset? resetAt, bool isGlobal = false)
    {
        if (isGlobal)
        {
            _globalResetAt = resetAt ?? DateTimeOffset.UtcNow.AddSeconds(5);
            return;
        }

        if (remaining.HasValue && resetAt.HasValue)
        {
            var bucketKey = bucketHash ?? route;
            var bucket = _buckets.GetOrAdd(bucketKey, _ => new RateLimitBucket());
            bucket.UpdateLimits(remaining.Value, resetAt.Value);
        }
    }

    /// <summary>
    /// Mark a request as completed for a bucket.
    /// </summary>
    public void MarkRequestComplete(string route, string? bucketHash = null)
    {
        var bucketKey = bucketHash ?? route;
        if (_buckets.TryGetValue(bucketKey, out var bucket))
        {
            bucket.Release();
        }
    }
}

/// <summary>
/// Represents a single rate limit bucket.
/// </summary>
public class RateLimitBucket
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _remaining = 1;
    private DateTimeOffset _resetAt = DateTimeOffset.MinValue;
    private readonly object _lock = new();

    public async Task WaitAsync()
    {
        await _semaphore.WaitAsync();

        lock (_lock)
        {
            // Check if we're rate limited
            if (_remaining <= 0 && DateTimeOffset.UtcNow < _resetAt)
            {
                var delay = _resetAt - DateTimeOffset.UtcNow;
                Task.Delay(delay).Wait();
            }

            if (_remaining > 0)
            {
                _remaining--;
            }
        }
    }

    public void Release()
    {
        _semaphore.Release();
    }

    public void UpdateLimits(int remaining, DateTimeOffset resetAt)
    {
        lock (_lock)
        {
            _remaining = remaining;
            _resetAt = resetAt;
        }
    }
}
