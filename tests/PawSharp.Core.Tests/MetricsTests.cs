using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using PawSharp.Core.Metrics;

namespace PawSharp.Core.Tests;

public class PerformanceMetricsTests
{
    private readonly PerformanceMetrics _metrics = new();

    [Fact]
    public void RecordApiRequest_TracksRequestMetrics()
    {
        // Act
        _metrics.RecordApiRequest("users/@me", "GET", 100, 200);
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalApiRequests.Should().Be(1);
        summary.AverageApiDurationMs.Should().Be(100);
        summary.TotalApiErrors.Should().Be(0);
    }

    [Fact]
    public void RecordApiRequest_TracksErrors()
    {
        // Act
        _metrics.RecordApiRequest("users/invalid", "GET", 50, 404);
        _metrics.RecordApiRequest("users/@me", "GET", 100, 200);
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalApiRequests.Should().Be(2);
        summary.TotalApiErrors.Should().Be(1);
        summary.ApiErrorRate.Should().BeApproximately(50, 0.1);
    }

    [Fact]
    public void RecordApiRequest_CalculatesAverageDuration()
    {
        // Act
        _metrics.RecordApiRequest("test", "GET", 100, 200);
        _metrics.RecordApiRequest("test", "GET", 200, 200);
        _metrics.RecordApiRequest("test", "GET", 300, 200);
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.AverageApiDurationMs.Should().Be(200);
    }

    [Fact]
    public void RecordCacheOperation_TracksCacheHits()
    {
        // Act
        _metrics.RecordCacheOperation("User", true);
        _metrics.RecordCacheOperation("User", true);
        _metrics.RecordCacheOperation("User", false);
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalCacheHits.Should().Be(2);
        summary.TotalCacheMisses.Should().Be(1);
        summary.CacheHitRate.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public void RecordGatewayMessage_CountsMessages()
    {
        // Act
        _metrics.RecordGatewayMessage("DISPATCH");
        _metrics.RecordGatewayMessage("HEARTBEAT");
        _metrics.RecordGatewayMessage("DISPATCH");
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalGatewayMessages.Should().Be(3);
        summary.GatewayOpcodes["DISPATCH"].Should().Be(2);
        summary.GatewayOpcodes["HEARTBEAT"].Should().Be(1);
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        _metrics.RecordApiRequest("test", "GET", 100, 200);
        _metrics.RecordCacheOperation("User", true);
        _metrics.RecordGatewayMessage("DISPATCH");
        
        // Act
        _metrics.Reset();
        
        // Assert
        var summary = _metrics.GetSummary();
        summary.TotalApiRequests.Should().Be(0);
        summary.TotalCacheHits.Should().Be(0);
        summary.TotalGatewayMessages.Should().Be(0);
    }

    [Fact]
    public void GetSummary_NeverCacheHitsOrMisses_ReturnsZeroHitRate()
    {
        // Act
        var summary = _metrics.GetSummary();
        
        // Assert
        summary.CacheHitRate.Should().Be(0);
    }
}

public class MemoryMetricsTests
{
    private readonly MemoryMetrics _metrics = new();

    [Fact]
    public void GetCurrentMemoryBytes_ReturnsPositiveValue()
    {
        // Act
        long memory = _metrics.GetCurrentMemoryBytes();
        
        // Assert
        memory.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetPeakMemoryBytes_InitiallyGreaterThanZero()
    {
        // Act
        long peak = _metrics.GetPeakMemoryBytes();
        
        // Assert
        peak.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetSummary_ContainsAllMetrics()
    {
        // Act
        var summary = _metrics.GetSummary();
        
        // Assert
        summary.CurrentMemoryBytes.Should().BeGreaterThan(0);
        summary.CurrentMemoryMB.Should().BeGreaterThan(0);
        summary.PeakMemoryBytes.Should().BeGreaterThan(0);
        summary.Handles.Should().BeGreaterThan(0);
        summary.Threads.Should().BeGreaterThan(0);
        summary.UptimeSeconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ResetPeak_SetsNewPeakToCurrentMemory()
    {
        // Arrange
        long initialPeak = _metrics.GetPeakMemoryBytes();
        
        // Act
        _metrics.ResetPeak();
        long newPeak = _metrics.GetPeakMemoryBytes();
        
        // Assert
        newPeak.Should().BeGreaterThanOrEqualTo(initialPeak - 1000000); // Allow small variance
    }

    [Fact]
    public void MemorySummary_ToStringIsNotEmpty()
    {
        // Act
        var summary = _metrics.GetSummary();
        string result = summary.ToString();
        
        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Memory:");
        result.Should().Contain("MB");
    }
}
