#nullable enable
using System;

namespace PawSharp.API.RateLimit;

/// <summary>
/// Indicates what stage of rate-limit handling produced telemetry.
/// </summary>
public enum RateLimitTelemetryKind
{
    /// <summary>Rate-limit headers were parsed from a response and applied to limiter state.</summary>
    HeaderUpdate,

    /// <summary>A 429 response scheduled a retry after a server-provided delay.</summary>
    RetryScheduled,

    /// <summary>A previously learned global limit forced a pre-request delay.</summary>
    GlobalDelayApplied
}

/// <summary>
/// Structured rate-limit telemetry emitted by the REST client.
/// </summary>
public sealed class RateLimitTelemetryEvent
{
    /// <summary>The telemetry kind describing where this event came from.</summary>
    public required RateLimitTelemetryKind Kind { get; init; }

    /// <summary>The normalized API route key (e.g. "POST channels/{id}/messages").</summary>
    public required string Route { get; init; }

    /// <summary>The Discord bucket hash when available.</summary>
    public string? BucketHash { get; init; }

    /// <summary>Remaining requests in this bucket, when provided by Discord.</summary>
    public int? Remaining { get; init; }

    /// <summary>The bucket/global reset time when known.</summary>
    public DateTimeOffset? ResetAt { get; init; }

    /// <summary>Whether this event references a global Discord limit.</summary>
    public bool IsGlobal { get; init; }

    /// <summary>Delay before retrying, if applicable.</summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>Current retry attempt for this request chain (0-based for first attempt).</summary>
    public int RetryCount { get; init; }
}

/// <summary>
/// Optional telemetry source for observing REST API rate-limit behavior.
/// </summary>
public interface IRateLimitTelemetrySource
{
    /// <summary>
    /// Raised when the client observes rate-limit state changes, retries, or global-limit delays.
    /// </summary>
    event EventHandler<RateLimitTelemetryEvent>? RateLimitObserved;
}
