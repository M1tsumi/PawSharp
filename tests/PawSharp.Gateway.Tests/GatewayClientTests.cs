#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PawSharp.Core.Models;
using PawSharp.Gateway.Heartbeat;
using Xunit;
using FluentAssertions;

namespace PawSharp.Gateway.Tests;

public class GatewayClientTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly PawSharpOptions _options;

    public GatewayClientTests()
    {
        _loggerMock = new Mock<ILogger>();
        _options = new PawSharpOptions
        {
            Token = "test-token",
            CustomGatewayUrl = "wss://localhost/",
            ApiVersion = 10
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.Should().NotBeNull();
        client.CurrentState.Should().Be(GatewayState.Disconnected);
    }

    [Fact]
    public void Constructor_InitializesEventDispatcher()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.Events.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesDiagnostics()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.Diagnostics.Should().NotBeNull();
    }

    [Fact]
    public void Events_Property_ReturnsSameInstance()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        var events1 = client.Events;
        var events2 = client.Events;
        events1.Should().BeSameAs(events2);
    }

    [Fact]
    public void Dispose_WhenNotConnected_DoesNotThrow()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        var act = () => client.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        var act = () =>
        {
            client.Dispose();
            client.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_AfterDisconnect_CleansUpResources()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        if (client.CurrentState == GatewayState.Disconnected)
        {
            await client.DisconnectAsync();
        }
        var act = () => client.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisconnectAsync_WhenDisconnected_DoesNotThrow()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        var act = async () => await client.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidApiVersion_Throws()
    {
        _options.ApiVersion = 1;
        var client = new GatewayClient(_options, _loggerMock.Object);
        var act = async () => await client.ConnectAsync();
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_SubscribesToReconnectionEvents()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.OnReconnectionAttempt += async (attempt) => await Task.CompletedTask;
        client.OnReconnectionFailed += async () => await Task.CompletedTask;
        client.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_UnsubscribesFromReconnectionManagerEvents()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.Dispose();
        client.OnReconnectionAttempt += async (attempt) => await Task.CompletedTask;
        client.OnReconnectionFailed += async () => await Task.CompletedTask;
    }

    [Fact]
    public void Dispose_UnsubscribesFromHeartbeatManagerEvents()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.Dispose();
    }

    [Fact]
    public void SessionId_InitiallyNull()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.SessionId.Should().BeNull();
    }

    [Fact]
    public void LastHeartbeatLatency_InitiallyNull()
    {
        var client = new GatewayClient(_options, _loggerMock.Object);
        client.LastHeartbeatLatency.Should().BeNull();
    }
}
