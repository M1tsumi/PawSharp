#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Gateway.Heartbeat;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class HeartbeatManagerTests
{
    [Fact]
    public void Constructor_WithInterval_SetsProperties()
    {
        var manager = new HeartbeatManager(1000);
        manager.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void Start_BeginsHeartbeatLoop()
    {
        var heartbeatSent = false;
        var manager = new HeartbeatManager(500, () =>
        {
            heartbeatSent = true;
            return Task.CompletedTask;
        });

        manager.Start();
        Thread.Sleep(600);
        heartbeatSent.Should().BeTrue();
        manager.Stop();
    }

    [Fact]
    public void StartWithJitter_BeginsHeartbeatLoop()
    {
        var heartbeatSent = false;
        var manager = new HeartbeatManager(500, () =>
        {
            heartbeatSent = true;
            return Task.CompletedTask;
        });

        manager.StartWithJitter();
        Thread.Sleep(600);
        heartbeatSent.Should().BeTrue();
        manager.Stop();
    }

    [Fact]
    public async Task StopAsync_StopsHeartbeatLoop()
    {
        var heartbeatCount = 0;
        var manager = new HeartbeatManager(100, () =>
        {
            Interlocked.Increment(ref heartbeatCount);
            return Task.CompletedTask;
        });

        manager.Start();
        await Task.Delay(250);
        await manager.StopAsync();
        var countAfterStop = heartbeatCount;
        await Task.Delay(200);
        heartbeatCount.Should().Be(countAfterStop);
    }

    [Fact]
    public void Stop_DoesNotThrow_WhenNotStarted()
    {
        var manager = new HeartbeatManager(1000);
        var act = () => manager.Stop();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_DoesNotThrow()
    {
        var manager = new HeartbeatManager(1000);
        var act = async () => await manager.StopAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Start_MultipleTimes_DisposesPreviousCts()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        manager.Start();
        manager.Start();
        manager.Stop();
    }

    [Fact]
    public void StartWithJitter_MultipleTimes_DisposesPreviousCts()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        manager.StartWithJitter();
        manager.StartWithJitter();
        manager.Stop();
    }

    [Fact]
    public async Task ReceiveAckAsync_ResetsMissedAcks()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        await manager.ReceiveAckAsync();
        manager.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAckAsync_FiresOnHeartbeatAckReceived()
    {
        var ackFired = false;
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        manager.OnHeartbeatAckReceived += () =>
        {
            ackFired = true;
            return Task.CompletedTask;
        };
        await manager.ReceiveAckAsync();
        ackFired.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenRunning_DoesNotThrow()
    {
        var manager = new HeartbeatManager(100, () => Task.CompletedTask);
        manager.Start();
        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenEventHandlersSubscribed()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        manager.OnZombieConnection += () => Task.CompletedTask;
        var act = () => manager.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        var act = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void IsHealthy_InitiallyTrue()
    {
        var manager = new HeartbeatManager(1000);
        manager.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_DisposesCancellationTokenSource()
    {
        var manager = new HeartbeatManager(1000, () => Task.CompletedTask);
        manager.Start();
        await manager.StopAsync();
        manager.Stop();
    }

    [Fact]
    public void OnHeartbeatSent_IsFired()
    {
        var fired = false;
        var manager = new HeartbeatManager(200, () => Task.CompletedTask);
        manager.OnHeartbeatSent += () =>
        {
            fired = true;
            return Task.CompletedTask;
        };
        manager.Start();
        Thread.Sleep(300);
        fired.Should().BeTrue();
        manager.Stop();
    }
}
