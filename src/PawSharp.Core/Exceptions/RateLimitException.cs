#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when rate limiting occurs.
/// <para>
/// This exception is thrown when Discord's rate limits are exceeded. It includes information
/// about how long to wait before retrying, whether the rate limit is global, and the rate limit bucket identifier.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     await client.Rest.CreateMessageAsync(channelId, request);
/// }
/// catch (RateLimitException ex)
/// {
///     Console.WriteLine($"Retry After: {ex.RetryAfter.TotalSeconds} seconds");
///     Console.WriteLine($"Is Global: {ex.IsGlobal}");
///     Console.WriteLine($"Bucket: {ex.Bucket}");
///     
///     // Automatic retry with backoff
///     await Task.Delay(ex.RetryAfter);
///     await client.Rest.CreateMessageAsync(channelId, request);
/// }
/// </code>
/// </example>
/// </para>
/// <para>
/// <remarks>
/// PawSharp includes built-in rate limiting that handles most rate limit scenarios automatically.
/// You typically won't see this exception unless you bypass the rate limiter or hit global rate limits.
/// </remarks>
/// </para>
/// </summary>
public class RateLimitException : DiscordException
{
    /// <summary>
    /// Gets the duration to wait before retrying the request.
    /// </summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>
    /// Gets whether this is a global rate limit.
    /// <para>Global rate limits affect all requests to Discord, not just a specific endpoint.</para>
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Gets the rate limit bucket identifier, if available.
    /// <para>Buckets group similar endpoints together for rate limiting purposes.</para>
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