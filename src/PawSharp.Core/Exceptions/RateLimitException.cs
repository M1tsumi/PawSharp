#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when rate limiting occurs.
/// </summary>
public class RateLimitException : DiscordException
{
    /// <summary>
    /// Gets the number of seconds to wait before retrying the request.
    /// </summary>
    public int RetryAfter { get; }

    /// <summary>
    /// Gets whether this is a global rate limit.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Gets the rate limit bucket identifier, if available.
    /// </summary>
    public string? Bucket { get; }

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
        RetryAfter = retryAfter;
        IsGlobal = isGlobal;
        Bucket = bucket;
    }
}