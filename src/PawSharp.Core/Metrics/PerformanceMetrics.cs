using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PawSharp.Core.Metrics;

/// <summary>
/// Tracks performance metrics for API calls, cache operations, and gateway events.
/// </summary>
public interface IPerformanceMetrics
{
    /// <summary>
    /// Records the duration of an API request.
    /// </summary>
    void RecordApiRequest(string endpoint, string method, long durationMs, int statusCode);

    /// <summary>
    /// Records a cache operation (hit or miss).
    /// </summary>
    void RecordCacheOperation(string entityType, bool isHit);

    /// <summary>
    /// Records a gateway message received.
    /// </summary>
    void RecordGatewayMessage(string opcodeName);

    /// <summary>
    /// Records a gateway reconnection attempt.
    /// </summary>
    void RecordReconnection();

    /// <summary>
    /// Records the heartbeat latency (round-trip time to Discord).
    /// </summary>
    void RecordHeartbeatLatency(long latencyMs);

    /// <summary>
    /// Records event dispatch duration.
    /// </summary>
    void RecordEventDispatch(string eventName, long durationMs);

    /// <summary>
    /// Records current event queue depth.
    /// </summary>
    void RecordQueueDepth(int depth);

    /// <summary>
    /// Gets current metrics summary.
    /// </summary>
    MetricsSummary GetSummary();

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    void Reset();
}

/// <summary>
/// Default implementation of performance metrics tracking.
/// </summary>
public class PerformanceMetrics : IPerformanceMetrics
{
    private readonly ConcurrentDictionary<string, ApiMetric> _apiMetrics = new();
    private readonly ConcurrentDictionary<string, CacheMetric> _cacheMetrics = new();
    private readonly ConcurrentDictionary<string, long> _gatewayOpcodes = new();
    
    private long _totalApiRequests;
    private long _totalApiErrors;
    private long _totalCacheHits;
    private long _totalCacheMisses;
    private long _totalGatewayMessages;
    private long _totalApiDurationMs;
    private long _totalReconnections;
    private long _totalHeartbeatLatencyMs;
    private long _heartbeatCount;
    private long _totalEventDispatchDurationMs;
    private long _eventDispatchCount;
    private long _currentQueueDepth;
    private long _maxQueueDepth;
    
    private readonly ConcurrentDictionary<string, EventMetric> _eventMetrics = new();
    
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public void RecordApiRequest(string endpoint, string method, long durationMs, int statusCode)
    {
        string key = $"{method.ToUpper()} {endpoint}";
        int errorDelta = statusCode >= 400 ? 1 : 0;

        _apiMetrics.AddOrUpdate(key,
            new ApiMetric { Count = 1, TotalDurationMs = durationMs, AverageDurationMs = durationMs, LastDurationMs = durationMs, ErrorCount = errorDelta },
            (_, metric) =>
            {
                long newCount = metric.Count + 1;
                long newTotal = metric.TotalDurationMs + durationMs;
                return new ApiMetric
                {
                    Count          = newCount,
                    TotalDurationMs  = newTotal,
                    AverageDurationMs = newTotal / newCount,
                    LastDurationMs   = durationMs,
                    ErrorCount       = metric.ErrorCount + errorDelta
                };
            });

        Interlocked.Increment(ref _totalApiRequests);
        Interlocked.Add(ref _totalApiDurationMs, durationMs);

        if (errorDelta == 1)
            Interlocked.Increment(ref _totalApiErrors);
    }

    public void RecordCacheOperation(string entityType, bool isHit)
    {
        if (isHit)
        {
            Interlocked.Increment(ref _totalCacheHits);
            _cacheMetrics.AddOrUpdate(entityType,
                new CacheMetric { Hits = 1 },
                (_, metric) => new CacheMetric { Hits = metric.Hits + 1, Misses = metric.Misses });
        }
        else
        {
            Interlocked.Increment(ref _totalCacheMisses);
            _cacheMetrics.AddOrUpdate(entityType,
                new CacheMetric { Misses = 1 },
                (_, metric) => new CacheMetric { Hits = metric.Hits, Misses = metric.Misses + 1 });
        }
    }

    public void RecordGatewayMessage(string opcodeName)
    {
        Interlocked.Increment(ref _totalGatewayMessages);
        _gatewayOpcodes.AddOrUpdate(opcodeName, 1, (_, count) => count + 1);
    }

    public void RecordReconnection()
    {
        Interlocked.Increment(ref _totalReconnections);
    }

    public void RecordHeartbeatLatency(long latencyMs)
    {
        Interlocked.Increment(ref _heartbeatCount);
        Interlocked.Add(ref _totalHeartbeatLatencyMs, latencyMs);
    }

    public void RecordEventDispatch(string eventName, long durationMs)
    {
        Interlocked.Increment(ref _eventDispatchCount);
        Interlocked.Add(ref _totalEventDispatchDurationMs, durationMs);
        
        _eventMetrics.AddOrUpdate(eventName,
            new EventMetric { Name = eventName, Count = 1, TotalDurationMs = durationMs, AverageDurationMs = durationMs, MaxDurationMs = durationMs },
            (_, metric) => new EventMetric
            {
                Name = eventName,
                Count = metric.Count + 1,
                TotalDurationMs = metric.TotalDurationMs + durationMs,
                AverageDurationMs = (metric.TotalDurationMs + durationMs) / (metric.Count + 1),
                MaxDurationMs = Math.Max(metric.MaxDurationMs, durationMs)
            });
    }

    public void RecordQueueDepth(int depth)
    {
        Interlocked.Exchange(ref _currentQueueDepth, depth);
        long currentMax = Interlocked.Read(ref _maxQueueDepth);
        if (depth > currentMax)
            Interlocked.CompareExchange(ref _maxQueueDepth, depth, currentMax);
    }

    public MetricsSummary GetSummary()
    {
        long totalCacheOperations = _totalCacheHits + _totalCacheMisses;
        double cacheHitRate = totalCacheOperations > 0 ? (_totalCacheHits * 100.0) / totalCacheOperations : 0;

        return new MetricsSummary
        {
            UptimeSeconds = (long)_uptime.Elapsed.TotalSeconds,
            
            // API Metrics
            TotalApiRequests = _totalApiRequests,
            TotalApiErrors = _totalApiErrors,
            AverageApiDurationMs = _totalApiRequests > 0 ? _totalApiDurationMs / _totalApiRequests : 0,
            ApiErrorRate = _totalApiRequests > 0 ? (_totalApiErrors * 100.0) / _totalApiRequests : 0,
            ApiMetrics = _apiMetrics.Values.ToList(),
            
            // Cache Metrics
            TotalCacheHits = _totalCacheHits,
            TotalCacheMisses = _totalCacheMisses,
            CacheHitRate = cacheHitRate,
            CacheMetrics = _cacheMetrics.ToDictionary(x => x.Key, x => x.Value),
            
            // Gateway Metrics
            TotalGatewayMessages = _totalGatewayMessages,
            GatewayOpcodes = _gatewayOpcodes.ToDictionary(x => x.Key, x => x.Value),
            TotalReconnections = _totalReconnections,
            AverageHeartbeatLatencyMs = _heartbeatCount > 0 ? _totalHeartbeatLatencyMs / _heartbeatCount : 0,
            AverageEventDispatchMs = _eventDispatchCount > 0 ? _totalEventDispatchDurationMs / _eventDispatchCount : 0,
            CurrentQueueDepth = _currentQueueDepth,
            MaxQueueDepth = _maxQueueDepth,
            EventMetrics = _eventMetrics.Values.ToList()
        };
    }

    public void Reset()
    {
        _apiMetrics.Clear();
        _cacheMetrics.Clear();
        _gatewayOpcodes.Clear();
        _eventMetrics.Clear();
        _totalApiRequests = 0;
        _totalApiErrors = 0;
        _totalCacheHits = 0;
        _totalCacheMisses = 0;
        _totalGatewayMessages = 0;
        _totalApiDurationMs = 0;
        _totalReconnections = 0;
        _totalHeartbeatLatencyMs = 0;
        _heartbeatCount = 0;
        _totalEventDispatchDurationMs = 0;
        _eventDispatchCount = 0;
        _currentQueueDepth = 0;
        _maxQueueDepth = 0;
        _uptime.Restart();
    }
}

/// <summary>
/// Summary of all collected metrics.
/// </summary>
public class MetricsSummary
{
    public long UptimeSeconds { get; set; }
    
    // API Metrics
    public long TotalApiRequests { get; set; }
    public long TotalApiErrors { get; set; }
    public long AverageApiDurationMs { get; set; }
    public double ApiErrorRate { get; set; }
    public List<ApiMetric> ApiMetrics { get; set; } = new();
    
    // Cache Metrics
    public long TotalCacheHits { get; set; }
    public long TotalCacheMisses { get; set; }
    public double CacheHitRate { get; set; }
    public Dictionary<string, CacheMetric> CacheMetrics { get; set; } = new();
    
    // Gateway Metrics
    public long TotalGatewayMessages { get; set; }
    public Dictionary<string, long> GatewayOpcodes { get; set; } = new();
    public long TotalReconnections { get; set; }
    public long AverageHeartbeatLatencyMs { get; set; }
    public long AverageEventDispatchMs { get; set; }
    public long CurrentQueueDepth { get; set; }
    public long MaxQueueDepth { get; set; }
    public List<EventMetric> EventMetrics { get; set; } = new();
}

/// <summary>
/// Metrics for a specific API endpoint.
/// </summary>
public class ApiMetric
{
    public long Count { get; set; }
    public long ErrorCount { get; set; }
    public long TotalDurationMs { get; set; }
    public long AverageDurationMs { get; set; }
    public long LastDurationMs { get; set; }
}

/// <summary>
/// Metrics for a specific cache entity type.
/// </summary>
public class CacheMetric
{
    public long Hits { get; set; }
    public long Misses { get; set; }
    public double HitRate => Hits + Misses > 0 ? (Hits * 100.0) / (Hits + Misses) : 0;
}

/// <summary>
/// Metrics for a specific event type dispatch.
/// </summary>
public class EventMetric
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
    public long TotalDurationMs { get; set; }
    public long AverageDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
}
