#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PawSharp.Cache.Telemetry;

/// <summary>
/// Telemetry for cache operations including performance metrics and health monitoring.
/// </summary>
public interface ICacheTelemetry
{
    /// <summary>
    /// Records a cache hit for an entity type.
    /// </summary>
    void RecordHit(string entityType);

    /// <summary>
    /// Records a cache miss for an entity type.
    /// </summary>
    void RecordMiss(string entityType);

    /// <summary>
    /// Records a cache operation duration.
    /// </summary>
    void RecordOperation(string operation, string entityType, TimeSpan duration);

    /// <summary>
    /// Records an eviction event.
    /// </summary>
    void RecordEviction(string entityType, string reason);

    /// <summary>
    /// Gets the current telemetry snapshot.
    /// </summary>
    CacheTelemetrySnapshot GetSnapshot();

    /// <summary>
    /// Resets all telemetry data.
    /// </summary>
    void Reset();
}

/// <summary>
/// Default implementation of cache telemetry.
/// </summary>
public class CacheTelemetry : ICacheTelemetry
{
    private readonly ConcurrentDictionary<string, EntityTypeMetrics> _entityMetrics = new();
    private readonly ConcurrentDictionary<string, OperationMetrics> _operationMetrics = new();
    private readonly ConcurrentBag<EvictionEvent> _evictions = new();

    private long _totalHits;
    private long _totalMisses;
    private long _totalOperations;
    private long _totalOperationDurationTicks;

    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public void RecordHit(string entityType)
    {
        Interlocked.Increment(ref _totalHits);
        _entityMetrics.AddOrUpdate(entityType,
            new EntityTypeMetrics { EntityType = entityType, Hits = 1 },
            (_, metrics) => new EntityTypeMetrics { EntityType = entityType, Hits = metrics.Hits + 1, Misses = metrics.Misses });
    }

    public void RecordMiss(string entityType)
    {
        Interlocked.Increment(ref _totalMisses);
        _entityMetrics.AddOrUpdate(entityType,
            new EntityTypeMetrics { EntityType = entityType, Misses = 1 },
            (_, metrics) => new EntityTypeMetrics { EntityType = entityType, Hits = metrics.Hits, Misses = metrics.Misses + 1 });
    }

    public void RecordOperation(string operation, string entityType, TimeSpan duration)
    {
        Interlocked.Increment(ref _totalOperations);
        Interlocked.Add(ref _totalOperationDurationTicks, duration.Ticks);

        string key = $"{operation}:{entityType}";
        _operationMetrics.AddOrUpdate(key,
            new OperationMetrics { Operation = operation, EntityType = entityType, Count = 1, TotalDuration = duration, AverageDuration = duration, MinDuration = duration, MaxDuration = duration },
            (_, metrics) =>
            {
                long newCount = metrics.Count + 1;
                var newTotal = metrics.TotalDuration.Add(duration);
                return new OperationMetrics
                {
                    Operation = operation,
                    EntityType = entityType,
                    Count = newCount,
                    TotalDuration = newTotal,
                    AverageDuration = TimeSpan.FromTicks(newTotal.Ticks / newCount),
                    MinDuration = duration < metrics.MinDuration ? duration : metrics.MinDuration,
                    MaxDuration = duration > metrics.MaxDuration ? duration : metrics.MaxDuration
                };
            });
    }

    public void RecordEviction(string entityType, string reason)
    {
        _evictions.Add(new EvictionEvent
        {
            EntityType = entityType,
            Reason = reason,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Keep only last 1000 evictions
        if (_evictions.Count > 1000)
        {
            _evictions.TryTake(out _);
        }
    }

    public CacheTelemetrySnapshot GetSnapshot()
    {
        long totalCacheOperations = _totalHits + _totalMisses;
        double hitRate = totalCacheOperations > 0 ? (_totalHits * 100.0) / totalCacheOperations : 0;
        double missRate = totalCacheOperations > 0 ? (_totalMisses * 100.0) / totalCacheOperations : 0;

        return new CacheTelemetrySnapshot
        {
            Uptime = _uptime.Elapsed,
            
            // Overall metrics
            TotalHits = _totalHits,
            TotalMisses = _totalMisses,
            TotalOperations = _totalOperations,
            HitRate = hitRate,
            MissRate = missRate,
            AverageOperationDuration = _totalOperations > 0 
                ? TimeSpan.FromTicks(_totalOperationDurationTicks / _totalOperations) 
                : TimeSpan.Zero,
            
            // Per-entity metrics
            EntityMetrics = _entityMetrics.ToDictionary(x => x.Key, x => x.Value),
            
            // Per-operation metrics
            OperationMetrics = _operationMetrics.ToDictionary(x => x.Key, x => x.Value),
            
            // Recent evictions
            RecentEvictions = _evictions.Take(100).ToList()
        };
    }

    public void Reset()
    {
        _entityMetrics.Clear();
        _operationMetrics.Clear();
        while (_evictions.TryTake(out _)) { }
        _totalHits = 0;
        _totalMisses = 0;
        _totalOperations = 0;
        _totalOperationDurationTicks = 0;
        _uptime.Restart();
    }
}

/// <summary>
/// Snapshot of cache telemetry at a point in time.
/// </summary>
public class CacheTelemetrySnapshot
{
    /// <summary>Time since telemetry collection started.</summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>Total cache hits recorded.</summary>
    public long TotalHits { get; set; }

    /// <summary>Total cache misses recorded.</summary>
    public long TotalMisses { get; set; }

    /// <summary>Total cache operations recorded.</summary>
    public long TotalOperations { get; set; }

    /// <summary>Cache hit rate as percentage.</summary>
    public double HitRate { get; set; }

    /// <summary>Cache miss rate as percentage.</summary>
    public double MissRate { get; set; }

    /// <summary>Average duration of all cache operations.</summary>
    public TimeSpan AverageOperationDuration { get; set; }

    /// <summary>Metrics per entity type.</summary>
    public System.Collections.Generic.Dictionary<string, EntityTypeMetrics> EntityMetrics { get; set; } = new();

    /// <summary>Metrics per operation type.</summary>
    public System.Collections.Generic.Dictionary<string, OperationMetrics> OperationMetrics { get; set; } = new();

    /// <summary>Recent eviction events.</summary>
    public System.Collections.Generic.List<EvictionEvent> RecentEvictions { get; set; } = new();
}

/// <summary>
/// Metrics for a specific entity type.
/// </summary>
public class EntityTypeMetrics
{
    public string EntityType { get; set; } = string.Empty;
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRate => Hits + Misses > 0 ? (Hits * 100.0) / (Hits + Misses) : 0;
}

/// <summary>
/// Metrics for a specific operation type.
/// </summary>
public class OperationMetrics
{
    public string Operation { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long Count { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
}

/// <summary>
/// Represents a cache eviction event.
/// </summary>
public class EvictionEvent
{
    public string EntityType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}
