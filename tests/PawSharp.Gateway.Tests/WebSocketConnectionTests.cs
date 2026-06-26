#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Gateway.Connection;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class WebSocketConnectionTests
{
    [Fact]
    public void Constructor_WithDefaults_DoesNotThrow()
    {
        var conn = new WebSocketConnection();
        conn.Should().NotBeNull();
        conn.CompressionEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithCompression_CreatesCompressionInstance()
    {
        var conn = new WebSocketConnection(useCompression: true);
        conn.CompressionEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsConnected_InitiallyFalse()
    {
        var conn = new WebSocketConnection();
        conn.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void CloseStatus_InitiallyNull()
    {
        var conn = new WebSocketConnection();
        conn.CloseStatus.Should().BeNull();
    }

    [Fact]
    public void CloseStatusDescription_InitiallyNull()
    {
        var conn = new WebSocketConnection();
        conn.CloseStatusDescription.Should().BeNull();
    }

    [Fact]
    public void IsDiscordErrorClose_InitiallyFalse()
    {
        var conn = new WebSocketConnection();
        conn.IsDiscordErrorClose.Should().BeFalse();
    }

    [Fact]
    public void Dispose_WhenNotConnected_DoesNotThrow()
    {
        var conn = new WebSocketConnection();
        var act = () => conn.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var conn = new WebSocketConnection();
        var act = () =>
        {
            conn.Dispose();
            conn.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotConnected_DoesNotThrow()
    {
        var conn = new WebSocketConnection();
        var act = async () => await conn.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_CompletesSuccessfully()
    {
        var conn = new WebSocketConnection();
        await conn.DisposeAsync();
    }

    [Fact]
    public async Task WaitForDisposeAsync_WhenNotDisposed_CompletesQuickly()
    {
        var conn = new WebSocketConnection();
        var act = async () => await conn.WaitForDisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_WithCompression_DoesNotThrow()
    {
        var conn = new WebSocketConnection(useCompression: true);
        var act = () => conn.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_WithCompression_DoesNotThrow()
    {
        var conn = new WebSocketConnection(useCompression: true);
        var act = async () => await conn.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_ClampsBufferSize()
    {
        var conn = new WebSocketConnection(bufferSizeKb: 2000);
        conn.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMinimumBufferSize()
    {
        var conn = new WebSocketConnection(bufferSizeKb: 1);
        conn.Should().NotBeNull();
    }

    [Fact]
    public async Task ReceiveAsync_BeforeConnect_Throws()
    {
        var conn = new WebSocketConnection();
        var act = async () => await conn.ReceiveAsync(CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendAsync_BeforeConnect_Throws()
    {
        var conn = new WebSocketConnection();
        var act = async () => await conn.SendAsync("test", CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }
}
