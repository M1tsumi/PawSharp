using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    public void RecordApiRequest(string endpoint, string method, long durationMs, int statusCode)
    {
        string key = $"{method.ToUpper()} {endpoint}";
        
        _apiMetrics.AddOrUpdate(key, 
            new ApiMetric { Count = 1, TotalDurationMs = durationMs, AverageDurationMs = durationMs, LastDurationMs = durationMs },
            (_, metric) =>
            {
                metric.Count++;
                metric.TotalDurationMs += durationMs;
                metric.AverageDurationMs = metric.TotalDurationMs / metric.Count;
                metric.LastDurationMs = durationMs;
                if (statusCode >= 400) metric.ErrorCount++;
                return metric;
            });

        _totalApiRequests++;
        _totalApiDurationMs += durationMs;
        
        if (statusCode >= 400)
            _totalApiErrors++;
    }

    public void RecordCacheOperation(string entityType, bool isHit)
    {
        if (isHit)
        {
            _totalCacheHits++;
            _cacheMetrics.AddOrUpdate(entityType,
                new CacheMetric { Hits = 1 },
                (_, metric) => { metric.Hits++; return metric; });
        }
        else
        {
            _totalCacheMisses++;
            _cacheMetrics.AddOrUpdate(entityType,
                new CacheMetric { Misses = 1 },
                (_, metric) => { metric.Misses++; return metric; });
        }
    }

    public void RecordGatewayMessage(string opcodeName)
    {
        _totalGatewayMessages++;
        _gatewayOpcodes.AddOrUpdate(opcodeName, 1, (_, count) => count + 1);
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
            GatewayOpcodes = _gatewayOpcodes.ToDictionary(x => x.Key, x => x.Value)
        };
    }

    public void Reset()
    {
        _apiMetrics.Clear();
        _cacheMetrics.Clear();
        _gatewayOpcodes.Clear();
        _totalApiRequests = 0;
        _totalApiErrors = 0;
        _totalCacheHits = 0;
        _totalCacheMisses = 0;
        _totalGatewayMessages = 0;
        _totalApiDurationMs = 0;
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
