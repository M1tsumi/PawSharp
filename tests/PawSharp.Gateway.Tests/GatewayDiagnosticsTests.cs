#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class GatewayDiagnosticsTests
{
    [Fact]
    public void Constructor_InitializesEmptyState()
    {
        var diag = new GatewayDiagnostics();
        var snapshot = diag.GetSnapshot();

        snapshot.CurrentState.Should().Be(GatewayState.Disconnected);
        snapshot.MessagesReceived.Should().Be(0);
        snapshot.MessagesSent.Should().Be(0);
        snapshot.ReconnectCount.Should().Be(0);
        snapshot.MissedAckCount.Should().Be(0);
        snapshot.SessionId.Should().BeNull();
        snapshot.LastError.Should().BeNull();
    }

    [Fact]
    public void RecordStateChange_TracksStateChanges()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordStateChange(GatewayState.Disconnected, GatewayState.Connecting);

        var snapshot = diag.GetSnapshot();
        snapshot.CurrentState.Should().Be(GatewayState.Connecting);
        snapshot.StateChangeHistory.Should().HaveCount(1);
    }

    [Fact]
    public void RecordStateChange_StartsWithUptimeOnReady()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordStateChange(GatewayState.Disconnected, GatewayState.Connecting);
        diag.RecordStateChange(GatewayState.Connecting, GatewayState.Ready);

        var snapshot = diag.GetSnapshot();
        snapshot.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void RecordStateChange_ResetsUptimeOnDisconnect()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordStateChange(GatewayState.Disconnected, GatewayState.Ready);
        diag.RecordStateChange(GatewayState.Ready, GatewayState.Disconnected);

        var snapshot = diag.GetSnapshot();
        snapshot.Uptime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void RecordEventReceived_IncrementsCount()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordEventReceived("MESSAGE_CREATE");
        diag.RecordEventReceived("GUILD_CREATE");
        diag.RecordEventReceived("MESSAGE_CREATE");

        var snapshot = diag.GetSnapshot();
        snapshot.MessagesReceived.Should().Be(3);
        snapshot.TopEvents.Should().ContainKey("MESSAGE_CREATE");
        snapshot.TopEvents["MESSAGE_CREATE"].Should().Be(2);
    }

    [Fact]
    public void RecordMessageSent_IncrementsCount()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordMessageSent();
        diag.RecordMessageSent();

        var snapshot = diag.GetSnapshot();
        snapshot.MessagesSent.Should().Be(2);
    }

    [Fact]
    public void RecordHeartbeatSent_TracksTimestamp()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordHeartbeatSent();

        var snapshot = diag.GetSnapshot();
        snapshot.LastHeartbeatSent.Should().NotBeNull();
    }

    [Fact]
    public void RecordHeartbeatAck_TracksTimestamp()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordHeartbeatAck();

        var snapshot = diag.GetSnapshot();
        snapshot.LastHeartbeatAck.Should().NotBeNull();
    }

    [Fact]
    public void RecordHeartbeatAck_CalculatesLatency()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordHeartbeatSent();
        diag.RecordHeartbeatAck();

        var snapshot = diag.GetSnapshot();
        snapshot.HeartbeatLatency.Should().NotBeNull();
    }

    [Fact]
    public void RecordMissedAck_IncrementsCount()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordMissedAck();
        diag.RecordMissedAck();

        var snapshot = diag.GetSnapshot();
        snapshot.MissedAckCount.Should().Be(2);
    }

    [Fact]
    public void RecordReconnection_IncrementsCount()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordReconnection("Test reason");

        var snapshot = diag.GetSnapshot();
        snapshot.ReconnectCount.Should().Be(1);
    }

    [Fact]
    public void RecordError_TracksLastError()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordError("Connection lost");

        var snapshot = diag.GetSnapshot();
        snapshot.LastError.Should().Be("Connection lost");
    }

    [Fact]
    public void UpdateConnectionInfo_SetsUrlAndSession()
    {
        var diag = new GatewayDiagnostics();
        diag.UpdateConnectionInfo("wss://gateway.discord.gg", "abc123", 42);

        var snapshot = diag.GetSnapshot();
        snapshot.CurrentGatewayUrl.Should().Be("wss://gateway.discord.gg");
        snapshot.SessionId.Should().Be("abc123");
        snapshot.SequenceNumber.Should().Be(42);
    }

    [Fact]
    public void Reset_ClearsAllData()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordEventReceived("MESSAGE_CREATE");
        diag.RecordMessageSent();
        diag.RecordError("Test error");
        diag.RecordReconnection("Test");

        diag.Reset();
        var snapshot = diag.GetSnapshot();

        snapshot.MessagesReceived.Should().Be(0);
        snapshot.MessagesSent.Should().Be(0);
        snapshot.LastError.Should().BeNull();
        snapshot.ReconnectCount.Should().Be(0);
    }

    [Fact]
    public void GetSnapshot_IsThreadSafe()
    {
        var diag = new GatewayDiagnostics();
        var snapshot1 = diag.GetSnapshot();
        diag.RecordEventReceived("TEST");
        var snapshot2 = diag.GetSnapshot();

        snapshot2.MessagesReceived.Should().Be(1);
    }

    [Fact]
    public void DiagnosticsSnapshot_GetSummary_ReturnsFormattedString()
    {
        var diag = new GatewayDiagnostics();
        diag.RecordStateChange(GatewayState.Disconnected, GatewayState.Connecting);
        diag.RecordEventReceived("MESSAGE_CREATE");

        var snapshot = diag.GetSnapshot();
        var summary = snapshot.GetSummary();

        summary.Should().NotBeNullOrEmpty();
        summary.Should().Contain("Gateway Diagnostics");
        summary.Should().Contain("MESSAGE_CREATE");
    }
}
