using System;
using FluentAssertions;
using PawSharp.Cache.Telemetry;
using Xunit;

namespace PawSharp.Cache.Tests;

public class CacheTelemetryTests
{
    [Fact]
    public void CacheTelemetry_RecordHit_IncreasesHitCount()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordHit("User");
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.TotalHits.Should().Be(1);
        snapshot.TotalMisses.Should().Be(0);
        snapshot.HitRate.Should().Be(100);
    }

    [Fact]
    public void CacheTelemetry_RecordMiss_IncreasesMissCount()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordMiss("User");
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.TotalHits.Should().Be(0);
        snapshot.TotalMisses.Should().Be(1);
        snapshot.HitRate.Should().Be(0);
    }

    [Fact]
    public void CacheTelemetry_RecordOperation_TracksDuration()
    {
        var telemetry = new CacheTelemetry();
        var duration = TimeSpan.FromMilliseconds(50);
        telemetry.RecordOperation("Get", "User", duration);
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.TotalOperations.Should().Be(1);
        snapshot.AverageOperationDuration.Should().Be(duration);
    }

    [Fact]
    public void CacheTelemetry_RecordEviction_TracksEviction()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordEviction("Message", "capacity");
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.RecentEvictions.Should().HaveCount(1);
        snapshot.RecentEvictions[0].EntityType.Should().Be("Message");
        snapshot.RecentEvictions[0].Reason.Should().Be("capacity");
    }

    [Fact]
    public void CacheTelemetry_EntityMetrics_TracksPerEntityStats()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordHit("User");
        telemetry.RecordHit("User");
        telemetry.RecordMiss("User");
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.EntityMetrics.Should().ContainKey("User");
        snapshot.EntityMetrics["User"].Hits.Should().Be(2);
        snapshot.EntityMetrics["User"].Misses.Should().Be(1);
        snapshot.EntityMetrics["User"].HitRate.Should().BeApproximately(66.67, 0.01);
    }

    [Fact]
    public void CacheTelemetry_OperationMetrics_TracksPerOperationStats()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(10));
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(20));
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.OperationMetrics.Should().ContainKey("Get:User");
        var metrics = snapshot.OperationMetrics["Get:User"];
        metrics.Count.Should().Be(2);
        metrics.AverageDuration.Should().Be(TimeSpan.FromMilliseconds(15));
        metrics.MinDuration.Should().Be(TimeSpan.FromMilliseconds(10));
        metrics.MaxDuration.Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public void CacheTelemetry_Reset_ClearsAllData()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordHit("User");
        telemetry.RecordMiss("Message");
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(10));
        
        telemetry.Reset();
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.TotalHits.Should().Be(0);
        snapshot.TotalMisses.Should().Be(0);
        snapshot.TotalOperations.Should().Be(0);
        snapshot.EntityMetrics.Should().BeEmpty();
        snapshot.OperationMetrics.Should().BeEmpty();
        snapshot.RecentEvictions.Should().BeEmpty();
    }

    [Fact]
    public void CacheTelemetry_Uptime_TracksTimeSinceCreation()
    {
        var telemetry = new CacheTelemetry();
        System.Threading.Thread.Sleep(100);
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.Uptime.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void CacheTelemetry_Evictions_LimitedTo1000()
    {
        var telemetry = new CacheTelemetry();
        
        // Record 1100 evictions
        for (int i = 0; i < 1100; i++)
        {
            telemetry.RecordEviction("Message", "capacity");
        }
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.RecentEvictions.Should().HaveCountLessOrEqualTo(1000);
    }

    [Fact]
    public void EntityTypeMetrics_HitRate_CalculatesCorrectly()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordHit("User");
        telemetry.RecordHit("User");
        telemetry.RecordHit("User");
        telemetry.RecordMiss("User");
        telemetry.RecordMiss("User");
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.EntityMetrics["User"].HitRate.Should().Be(60);
    }

    [Fact]
    public void EntityTypeMetrics_HitRate_ZeroWhenNoOperations()
    {
        var telemetry = new CacheTelemetry();
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.EntityMetrics.Should().BeEmpty();
    }

    [Fact]
    public void OperationMetrics_AverageDuration_CalculatesCorrectly()
    {
        var telemetry = new CacheTelemetry();
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(100));
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(200));
        telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(300));
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.OperationMetrics["Get:User"].AverageDuration.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task CacheTelemetry_ThreadSafe_ConcurrentOperations()
    {
        var telemetry = new CacheTelemetry();
        var tasks = new System.Threading.Tasks.Task[100];
        
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                telemetry.RecordHit("User");
                telemetry.RecordMiss("User");
                telemetry.RecordOperation("Get", "User", TimeSpan.FromMilliseconds(10));
            });
        }
        
        await System.Threading.Tasks.Task.WhenAll(tasks);
        
        var snapshot = telemetry.GetSnapshot();
        snapshot.TotalHits.Should().Be(100);
        snapshot.TotalMisses.Should().Be(100);
        snapshot.TotalOperations.Should().Be(100);
    }
}
