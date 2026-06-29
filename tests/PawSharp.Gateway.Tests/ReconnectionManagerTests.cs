#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PawSharp.Core.Models;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class ReconnectionManagerTests
{
    private readonly Mock<ILogger> _loggerMock;

    public ReconnectionManagerTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void Constructor_WithDefaultOptions_SetsDefaults()
    {
        var manager = new ReconnectionManager(_loggerMock.Object);
        manager.MaxAttempts.Should().Be(10);
        manager.AttemptsCount.Should().Be(0);
        manager.CanReconnect.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithCustomOptions_UsesCustomValues()
    {
        var options = new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 3,
            InitialDelayMs = 500,
            MaxDelayMs = 4000,
            JitterFactor = 0.1
        };
        var manager = new ReconnectionManager(_loggerMock.Object, options: options);
        manager.MaxAttempts.Should().Be(3);
        manager.CanReconnect.Should().BeTrue();
    }

    [Fact]
    public async Task ReconnectAsync_IncrementsAttemptCount()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 1,
            MaxDelayMs = 10
        });

        await manager.ReconnectAsync();
        manager.AttemptsCount.Should().Be(1);
        manager.CanReconnect.Should().BeTrue();
    }

    [Fact]
    public async Task ReconnectAsync_ReturnsFalse_WhenMaxAttemptsExceeded()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 1,
            InitialDelayMs = 1,
            MaxDelayMs = 10
        });

        var firstResult = await manager.ReconnectAsync();
        firstResult.Should().BeTrue();
        manager.CanReconnect.Should().BeFalse();

        var secondResult = await manager.ReconnectAsync();
        secondResult.Should().BeFalse();
    }

    [Fact]
    public async Task ReconnectAsync_FiresOnReconnectionAttempt()
    {
        var attemptFired = false;
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 3,
            InitialDelayMs = 1,
            MaxDelayMs = 10
        });

        manager.OnReconnectionAttempt += async (attempt) =>
        {
            attemptFired = true;
            attempt.Should().Be(1);
            await Task.CompletedTask;
        };

        await manager.ReconnectAsync();
        attemptFired.Should().BeTrue();
    }

    [Fact]
    public async Task ReconnectAsync_FiresOnReconnectionFailed_WhenExhausted()
    {
        var failedFired = false;
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 1,
            InitialDelayMs = 1,
            MaxDelayMs = 10
        });

        manager.OnReconnectionFailed += async () =>
        {
            failedFired = true;
            await Task.CompletedTask;
        };

        await manager.ReconnectAsync();
        await manager.ReconnectAsync();
        failedFired.Should().BeTrue();
    }

    [Fact]
    public void Reset_ClearsAttemptsAndBackoff()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 100,
            MaxDelayMs = 1000
        });

        manager.Reset();
        manager.AttemptsCount.Should().Be(0);
        manager.CanReconnect.Should().BeTrue();
    }

    [Fact]
    public void GetCurrentBackoffMs_ReturnsInitialValueAfterReset()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            InitialDelayMs = 500
        });

        manager.GetCurrentBackoffMs().Should().Be(500);
    }

    [Fact]
    public async Task ReconnectAsync_IncreasesBackoff()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 5,
            InitialDelayMs = 100,
            MaxDelayMs = 10000
        });

        var backoff1 = manager.GetCurrentBackoffMs();
        await manager.ReconnectAsync();
        var backoff2 = manager.GetCurrentBackoffMs();

        backoff2.Should().BeGreaterOrEqualTo(backoff1);
    }

    [Fact]
    public async Task ReconnectAsync_RespectsMaxBackoff()
    {
        var manager = new ReconnectionManager(_loggerMock.Object, options: new PawSharpOptions.ReconnectionOptions
        {
            MaxAttempts = 10,
            InitialDelayMs = 1000,
            MaxDelayMs = 2000
        });

        for (int i = 0; i < 5; i++)
        {
            await manager.ReconnectAsync();
        }

        manager.GetCurrentBackoffMs().Should().BeLessOrEqualTo(2000);
    }
}
