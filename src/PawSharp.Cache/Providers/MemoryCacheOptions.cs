#nullable enable
namespace PawSharp.Cache.Providers;

/// <summary>
/// Configuration options for the in-memory cache provider.
/// </summary>
public class MemoryCacheOptions
{
    /// <summary>
    /// Maximum number of items in the general cache (default: 10000).
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Maximum entities per type (default: 5000).
    /// </summary>
    public int MaxEntityCacheSize { get; set; } = 5000;

    /// <summary>
    /// Minimum interval between cleanup operations (default: 5 minutes).
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to enable memory-based eviction (default: false).
    /// When enabled, the cache will evict items based on memory pressure
    /// in addition to size limits.
    /// </summary>
    public bool EnableMemoryBasedEviction { get; set; } = false;

    /// <summary>
    /// Memory limit in bytes before eviction begins (default: 100MB).
    /// Only used when EnableMemoryBasedEviction is true.
    /// </summary>
    public long MemoryLimitBytes { get; set; } = 100 * 1024 * 1024;
}
