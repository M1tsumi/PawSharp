#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when rate limiting occurs.
/// </summary>
public class RateLimitException : DiscordException
{
    /// <summary>
    /// Gets the duration to wait before retrying the request.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Gets whether this is a global rate limit.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Gets the rate limit bucket identifier, if available.
    /// </summary>
    public string? Bucket { get; }

    /// <summary>
    /// Gets the rate limit bucket identifier, if available. Alias for <see cref="Bucket"/>.
    /// </summary>
    public string? BucketId => Bucket;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">The duration to wait before retrying.</param>
    /// <param name="bucketId">The rate limit bucket identifier.</param>
    public RateLimitException(string message, TimeSpan retryAfter, string bucketId)
        : base(message)
    {
        RetryAfter = retryAfter;
        Bucket = bucketId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="retryAfter">The number of seconds to wait before retrying.</param>
    /// <param name="isGlobal">Whether this is a global rate limit.</param>
    /// <param name="bucket">The rate limit bucket identifier.</param>
    /// <param name="message">The error message.</param>
    public RateLimitException(int retryAfter, bool isGlobal = false, string? bucket = null, string? message = null)
        : base(message ?? $"Rate limit exceeded. Retry after {retryAfter} seconds.")
    {
        RetryAfter = TimeSpan.FromSeconds(retryAfter);
        IsGlobal = isGlobal;
        Bucket = bucket;
    }
}