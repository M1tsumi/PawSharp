#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PawSharp.API.RateLimit;

/// <summary>
/// Advanced rate limiter with per-route bucket management.
/// </summary>
public class AdvancedRateLimiter : IAdvancedRateLimiter, IDisposable
{
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new();
    private readonly SemaphoreSlim _globalLimitSemaphore = new(1, 1);
    private DateTimeOffset _globalResetAt = DateTimeOffset.MinValue;
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    public AdvancedRateLimiter()
    {
        _cleanupTimer = new Timer(_ => CleanupStaleBuckets(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Wait for rate limit clearance before executing a request.
    /// </summary>
    /// <param name="route">The API route (e.g., "POST /channels/{channel.id}/messages")</param>
    /// <param name="bucketHash">The bucket hash from Discord's X-RateLimit-Bucket header</param>
    /// <param name="cancellationToken">Token used to cancel waiting for rate-limit clearance.</param>
    public async Task WaitForRateLimitAsync(string route, string? bucketHash = null, CancellationToken cancellationToken = default)
    {
        // Check global rate limit first
        if (DateTimeOffset.UtcNow < _globalResetAt)
        {
            var globalDelay = _globalResetAt - DateTimeOffset.UtcNow;
            await Task.Delay(globalDelay, cancellationToken).ConfigureAwait(false);
        }

        // Get or create bucket for this route
        var bucketKey = bucketHash ?? route;
        var bucket = _buckets.GetOrAdd(bucketKey, _ => new RateLimitBucket());

        await bucket.WaitAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Removes buckets that have passed their reset time and are no longer in use.
    /// Call periodically to prevent unbounded dictionary growth under long-running
    /// operations with many unique API routes.
    /// </summary>
    public void CleanupStaleBuckets()
    {
        var cutoff = DateTimeOffset.UtcNow;
        foreach (var kvp in _buckets)
        {
            if (kvp.Value.IsExpired(cutoff))
            {
                _buckets.TryRemove(kvp.Key, out _);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer?.Dispose();
        _globalLimitSemaphore.Dispose();
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

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        TimeSpan delay;
        lock (_lock)
        {
            if (_remaining <= 0 && DateTimeOffset.UtcNow < _resetAt)
            {
                delay = _resetAt - DateTimeOffset.UtcNow;
            }
            else
            {
                delay = TimeSpan.Zero;
            }

            if (_remaining > 0)
            {
                _remaining--;
            }
        }

        // Yield outside the lock so we don't block threads while waiting
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Release()
    {
        // Only release if the semaphore was actually acquired.
        // Using try-catch for SemaphoreFullException is the only reliable way
        // since CurrentCount is subject to race conditions between check and release.
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already released — no action needed.
        }
    }

    public void UpdateLimits(int remaining, DateTimeOffset resetAt)
    {
        lock (_lock)
        {
            _remaining = remaining;
            _resetAt = resetAt;
        }
    }

    /// <summary>
    /// Returns true if this bucket has passed its reset time and is not actively
    /// throttled, making it safe to remove from the rate limiter's dictionary.
    /// </summary>
    public bool IsExpired(DateTimeOffset cutoff)
    {
        lock (_lock)
        {
            return _remaining >= 1 && _resetAt < cutoff;
        }
    }
}
