#nullable enable
using System;

namespace PawSharp.Cache.Swapping
{
    /// <summary>
    /// Configuration options for cache swapping.
    /// </summary>
    public class CacheSwapperOptions
    {
        /// <summary>
        /// Whether to automatically enable fallback to next provider on failure.
        /// </summary>
        public bool AutoFallback { get; set; } = true;

        /// <summary>
        /// Maximum number of consecutive failures before circuit breaker opens.
        /// </summary>
        public int MaxFailuresBeforeCircuitOpen { get; set; } = 5;

        /// <summary>
        /// Duration to keep circuit breaker open before attempting reset.
        /// </summary>
        public TimeSpan CircuitOpenDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to automatically attempt to swap back to primary provider when it becomes healthy.
        /// </summary>
        public bool AutoSwapBackToPrimary { get; set; } = true;

        /// <summary>
        /// Interval between health checks for inactive providers.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to propagate cache changes to all providers (multi-write).
        /// </summary>
        public bool PropagateToAllProviders { get; set; } = false;

        /// <summary>
        /// Timeout for cache operations before attempting fallback.
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Whether to log cache swap operations.
        /// </summary>
        public bool EnableLogging { get; set; } = true;
    }
}
