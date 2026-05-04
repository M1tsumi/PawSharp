#nullable enable
using PawSharp.Cache.Interfaces;
using System;

namespace PawSharp.Cache.Swapping
{
    /// <summary>
    /// Information about a registered cache provider.
    /// </summary>
    public class CacheProviderInfo
    {
        /// <summary>
        /// The unique name/identifier for this provider.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The cache provider instance.
        /// </summary>
        public IEntityCache Provider { get; set; } = null!;

        /// <summary>
        /// Priority for fallback (lower = higher priority).
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether this provider is currently active.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Whether this provider is healthy (based on last health check).
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Timestamp of last health check.
        /// </summary>
        public DateTime? LastHealthCheck { get; set; }

        /// <summary>
        /// Number of times this provider has failed.
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// Whether this provider is currently in circuit breaker (too many failures).
        /// </summary>
        public bool IsCircuitOpen { get; set; } = false;

        /// <summary>
        /// Timestamp when circuit breaker will reset.
        /// </summary>
        public DateTime? CircuitResetTime { get; set; }
    }
}
