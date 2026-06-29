#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace PawSharp.Voice.Tests;

public class VoiceConnectionTests
{
    [Fact]
    public void TryParseRtpPacket_Valid12ByteHeader_ReturnsTrue()
    {
        byte[] packet = [
            0x80, 0x78, 0x00, 0x01, // V=2, PT=120, seq=1
            0x00, 0x00, 0x00, 0x01, // timestamp=1
            0xDE, 0xAD, 0xBE, 0xEF, // SSRC=0xDEADBEEF
            0x01, 0x02, 0x03        // payload
        ];

        var result = VoiceConnectionTestsAccessor.TryParseRtpPacket(packet, out var ssrc, out var header, out var payload);

        result.Should().BeTrue();
        ssrc.Should().Be(0xDEADBEEF);
        header.Should().HaveCount(12);
        payload.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void TryParseRtpPacket_ShortPacket_ReturnsFalse()
    {
        byte[] packet = [0x80, 0x78, 0x00];

        var result = VoiceConnectionTestsAccessor.TryParseRtpPacket(packet, out var ssrc, out var header, out var payload);

        result.Should().BeFalse();
        ssrc.Should().Be(0);
        header.Should().BeEmpty();
        payload.Should().BeEquivalentTo(packet);
    }

    [Fact]
    public void TryParseRtpPacket_EmptyPacket_ReturnsFalse()
    {
        byte[] packet = [];

        var result = VoiceConnectionTestsAccessor.TryParseRtpPacket(packet, out var ssrc, out var header, out var payload);

        result.Should().BeFalse();
        ssrc.Should().Be(0);
        payload.Should().BeEmpty();
    }

    [Fact]
    public void TryParseRtpPacket_ExtractsCorrectSsrc()
    {
        byte[] packet = [
            0x80, 0x78, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x01, 0x02, 0x03, 0x04  // SSRC = 0x01020304
        ];

        VoiceConnectionTestsAccessor.TryParseRtpPacket(packet, out var ssrc, out _, out _);
        ssrc.Should().Be(0x01020304u);
    }

    [Fact]
    public void TryParseRtpPacket_EmptyPayload_ReturnsTrue()
    {
        byte[] packet = [
            0x80, 0x78, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01
        ];

        var result = VoiceConnectionTestsAccessor.TryParseRtpPacket(packet, out _, out _, out var payload);
        result.Should().BeTrue();
        payload.Should().BeEmpty();
    }

    [Fact]
    public void VoiceConnectionState_Disconnected_HasValueZero()
    {
        ((int)VoiceConnectionState.Disconnected).Should().Be(0);
    }

    [Fact]
    public void VoiceEncryptionMode_AeadXChaCha20Poly1305RtpSize_HasCorrectValue()
    {
        ((int)VoiceEncryptionMode.AeadXChaCha20Poly1305RtpSize).Should().Be(4);
    }
}

/// <summary>
/// Accesses internal/private members of VoiceConnection for testing.
/// </summary>
internal static class VoiceConnectionTestsAccessor
{
    /// <summary>
    /// Calls the private static TryParseRtpPacket method via reflection.
    /// </summary>
    public static bool TryParseRtpPacket(byte[] packet, out uint ssrc, out byte[] rtpHeader, out byte[] payload)
    {
        var method = typeof(VoiceConnection).GetMethod("TryParseRtpPacket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var parameters = new object[] { packet, 0u, null!, null! };
        var result = (bool)method!.Invoke(null, parameters)!;
        ssrc = (uint)parameters[1];
        rtpHeader = (byte[])parameters[2];
        payload = (byte[])parameters[3];
        return result;
    }
}
