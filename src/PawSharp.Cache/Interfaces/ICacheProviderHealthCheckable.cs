#nullable enable
namespace PawSharp.Cache.Interfaces;

/// <summary>
/// Interface for cache providers that support health checks.
/// </summary>
public interface ICacheProviderHealthCheckable
{
    /// <summary>
    /// Checks if the cache provider is healthy and operational.
    /// </summary>
    /// <returns>True if the provider is healthy, false otherwise.</returns>
    bool IsHealthy();
}
