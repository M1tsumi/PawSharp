#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

public class DAVEProtocolTests : IDisposable
{
    private readonly DAVEProtocol _proto = new();

    public void Dispose() => _proto.Dispose();

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_StartsFalse()
    {
        _proto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void LocalSsrc_DefaultsToZero()
    {
        _proto.LocalSsrc.Should().Be(0u);
    }

    [Fact]
    public void LocalSsrc_CanBeSet()
    {
        _proto.LocalSsrc = 0xCAFE;
        _proto.LocalSsrc.Should().Be(0xCAFE);
    }

    // ── Passthrough when inactive ─────────────────────────────────────────────

    [Fact]
    public void EncryptFrame_WhenInactive_ReturnsSameBytes()
    {
        var frame = new byte[] { 0x01, 0x02, 0x03 };
        var result = _proto.EncryptFrame(frame);

        result.Should().BeSameAs(frame, "no encryption should occur before DAVE is active");
    }

    [Fact]
    public void DecryptFrame_WhenInactive_ReturnsSameBytes()
    {
        var frame = new byte[] { 0x11, 0x22, 0x33 };
        var result = _proto.DecryptFrame(frame, ssrc: 1);

        result.Should().BeSameAs(frame, "no decryption should occur before DAVE is active");
    }

    // ── Welcome (op 30) activates encryption ──────────────────────────────────

    [Fact]
    public async Task HandleWelcome_30_SetsIsActiveTrue()
    {
        var (welcomeBytes, _) = DAVETestData.CreateWelcome(_proto.MlsState);

        await _proto.HandleBinaryMessageAsync(30, welcomeBytes, webSocket: null);

        _proto.IsActive.Should().BeTrue();
    }

    // ── Known JSON opcodes do not throw ───────────────────────────────────────

    [Theory]
    [InlineData(21)]  // PrepareTransition
    [InlineData(22)]  // ExecuteTransition
    [InlineData(23)]  // TransitionReady (client sent)
    [InlineData(24)]  // PrepareEpoch
    [InlineData(31)]  // InvalidCommitWelcome
    public async Task HandleJsonMessage_KnownDAVEOpcodes_DoNotThrow(int opcode)
    {
        var data = JsonDocument.Parse("{}").RootElement;
        Func<Task> act = () => _proto.HandleJsonMessageAsync(opcode, data, webSocket: null);
        await act.Should().NotThrowAsync();
    }

    // ── Known binary opcodes do not throw ─────────────────────────────────────

    [Theory]
    [InlineData(25)]  // MlsExternalSender
    [InlineData(26)]  // MlsKeyPackage (client sent)
    [InlineData(27)]  // MlsProposals
    [InlineData(28)]  // MlsCommitWelcome (client sent)
    [InlineData(29)]  // MlsAnnounceCommitTransition
    [InlineData(30)]  // MlsWelcome
    public async Task HandleBinaryMessage_KnownDAVEOpcodes_DoNotThrow(int opcode)
    {
        var payload = Array.Empty<byte>();
        Func<Task> act = () => _proto.HandleBinaryMessageAsync(opcode, payload, webSocket: null);
        await act.Should().NotThrowAsync();
    }

    // ── Commit (op 29) advances the MLS epoch ─────────────────────────────────

    [Fact]
    public async Task HandleCommit_29_AdvancesEpoch()
    {
        var (welcomeBytes, _) = DAVETestData.CreateWelcome(_proto.MlsState);
        await _proto.HandleBinaryMessageAsync(30, welcomeBytes, null);
        _proto.EpochNumber.Should().Be(1);

        var commitBytes = DAVETestData.CreateEmptyCommit();
        await _proto.HandleBinaryMessageAsync(29, commitBytes, null);

        _proto.EpochNumber.Should().Be(2);
    }

    // ── End-to-end encrypt/decrypt round trip when active ─────────────────────

    [Fact]
    public async Task ActiveProtocol_EncryptDecrypt_RoundTrip()
    {
        const uint mySSRC    = 0xABCD;
        const uint theirSSRC = 0x1234;

        using var remote = new DAVEProtocol();
        remote.LocalSsrc = theirSSRC;

        var (welcomeBytes, _) = DAVETestData.CreateMultiWelcome(new MLSState[] { _proto.MlsState, remote.MlsState });

        _proto.LocalSsrc = mySSRC;
        await _proto.HandleBinaryMessageAsync(30, welcomeBytes, null);
        _proto.IsActive.Should().BeTrue();

        var plaintext = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var encrypted = _proto.EncryptFrame(plaintext);
        encrypted.Should().NotBeEquivalentTo(plaintext, "active protocol must actually encrypt");

        await remote.HandleBinaryMessageAsync(30, welcomeBytes, null);

        _proto.EpochSecret.Should().BeEquivalentTo(remote.EpochSecret,
            "both sides must derive identical epoch secrets");

        var decrypted = remote.DecryptFrame(encrypted, ssrc: mySSRC);
        decrypted.Should().BeEquivalentTo(plaintext, "remote must recover the original frame");
    }

    // ── Constructor with explicit user ID ────────────────────────────────────

    [Fact]
    public void Constructor_WithUserId_DoesNotThrow()
    {
        using var proto = new DAVEProtocol("123456789012345678");
        proto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Action act = () => _ = new DAVEProtocol("");
        act.Should().Throw<ArgumentException>();
    }

    // ── EpochNumber ───────────────────────────────────────────────────────────

    [Fact]
    public void EpochNumber_StartsWith_Zero()
    {
        _proto.EpochNumber.Should().Be(0);
    }

    [Fact]
    public async Task EpochNumber_AfterWelcome_IsOne()
    {
        await DispatchWelcomeAsync();
        _proto.EpochNumber.Should().Be(1);
    }

    [Fact]
    public async Task EpochNumber_AfterCommit_IsTwo()
    {
        await DispatchWelcomeAsync();
        var commitBytes = DAVETestData.CreateEmptyCommit();
        await _proto.HandleBinaryMessageAsync(29, commitBytes, null);
        _proto.EpochNumber.Should().Be(2);
    }

    // ── Reset() ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reset_AfterActivation_SetsIsActiveFalse()
    {
        await DispatchWelcomeAsync();
        _proto.IsActive.Should().BeTrue();

        _proto.Reset();

        _proto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Reset_AfterActivation_ResetsEpochNumber()
    {
        await DispatchWelcomeAsync();
        _proto.EpochNumber.Should().Be(1);

        _proto.Reset();

        _proto.EpochNumber.Should().Be(0);
    }

    [Fact]
    public async Task AfterReset_EncryptFrame_PassesThroughUnchanged()
    {
        await DispatchWelcomeAsync();
        _proto.Reset();

        var frame  = new byte[] { 0x01, 0x02, 0x03 };
        var result = _proto.EncryptFrame(frame);

        result.Should().BeSameAs(frame, "reset protocol must not encrypt");
    }

    [Fact]
    public async Task AfterReset_CanReactivateWithNewWelcome()
    {
        await DispatchWelcomeAsync();
        _proto.Reset();

        await DispatchWelcomeAsync();

        _proto.IsActive.Should().BeTrue();
        _proto.EpochNumber.Should().Be(1);
    }

    // ── Epoch advance resets frame counter ───────────────────────────────────

    [Fact]
    public async Task CommitAdvance_ProducesEncryptedFrame_NotPassthrough()
    {
        _proto.LocalSsrc = 0x01;
        await DispatchWelcomeAsync();
        var commitBytes = DAVETestData.CreateEmptyCommit();
        await _proto.HandleBinaryMessageAsync(29, commitBytes, null);

        var plaintext = new byte[] { 0xAA, 0xBB };
        var encrypted = _proto.EncryptFrame(plaintext);
        encrypted.Should().NotBeEquivalentTo(plaintext,
            "encryption must remain active after a commit");
    }

    // ── Frame counter increments produce unique ciphertexts ────────────────────

    [Fact]
    public async Task ActiveProtocol_TwoFramesFromSamePayload_ProduceDifferentCiphertext()
    {
        _proto.LocalSsrc = 0x01;
        await DispatchWelcomeAsync();

        var payload = new byte[] { 0xFF };
        var enc1 = _proto.EncryptFrame(payload);
        var enc2 = _proto.EncryptFrame(payload);

        enc1.Should().NotBeEquivalentTo(enc2,
            "monotonic frame counter must produce different nonces each call");
    }

    // ── Opcode enum coverage ──────────────────────────────────────────────────

    [Theory]
    [InlineData(DAVEVoiceOpcode.DavePrepareTransition,             21)]
    [InlineData(DAVEVoiceOpcode.DaveExecuteTransition,             22)]
    [InlineData(DAVEVoiceOpcode.DaveTransitionReady,               23)]
    [InlineData(DAVEVoiceOpcode.DavePrepareEpoch,                  24)]
    [InlineData(DAVEVoiceOpcode.DaveMlsExternalSender,             25)]
    [InlineData(DAVEVoiceOpcode.DaveMlsKeyPackage,                 26)]
    [InlineData(DAVEVoiceOpcode.DaveMlsProposals,                  27)]
    [InlineData(DAVEVoiceOpcode.DaveMlsCommitWelcome,              28)]
    [InlineData(DAVEVoiceOpcode.DaveMlsAnnounceCommitTransition,   29)]
    [InlineData(DAVEVoiceOpcode.DaveMlsWelcome,                    30)]
    [InlineData(DAVEVoiceOpcode.DaveMlsInvalidCommitWelcome,       31)]
    public void OpcodeEnum_HasCorrectIntegerValue(DAVEVoiceOpcode op, int expected)
    {
        ((int)op).Should().Be(expected);
    }

    [Theory]
    [InlineData(DAVEVoiceOpcode.Identify,           0)]
    [InlineData(DAVEVoiceOpcode.SelectProtocol,     1)]
    [InlineData(DAVEVoiceOpcode.Ready,              2)]
    [InlineData(DAVEVoiceOpcode.Heartbeat,          3)]
    [InlineData(DAVEVoiceOpcode.SessionDescription, 4)]
    [InlineData(DAVEVoiceOpcode.Speaking,           5)]
    [InlineData(DAVEVoiceOpcode.HeartbeatAck,       6)]
    [InlineData(DAVEVoiceOpcode.Resume,             7)]
    [InlineData(DAVEVoiceOpcode.Hello,              8)]
    [InlineData(DAVEVoiceOpcode.Resumed,            9)]
    [InlineData(DAVEVoiceOpcode.ClientsConnect,     11)]
    [InlineData(DAVEVoiceOpcode.ClientDisconnect,   13)]
    [InlineData(DAVEVoiceOpcode.SessionUpdate,      14)]
    [InlineData(DAVEVoiceOpcode.MediaSinkWants,     15)]
    [InlineData(DAVEVoiceOpcode.VoiceBackendVersion,16)]
    [InlineData(DAVEVoiceOpcode.ChannelOptionsUpdate,17)]
    [InlineData(DAVEVoiceOpcode.Flags,              18)]
    [InlineData(DAVEVoiceOpcode.SpeedTest,          19)]
    [InlineData(DAVEVoiceOpcode.Platform,           20)]
    public void StandardOpcodeEnum_HasCorrectIntegerValue(DAVEVoiceOpcode op, int expected)
    {
        ((int)op).Should().Be(expected);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task DispatchWelcomeAsync()
    {
        var (welcomeBytes, _) = DAVETestData.CreateWelcome(_proto.MlsState);
        await _proto.HandleBinaryMessageAsync((int)DAVEVoiceOpcode.DaveMlsWelcome, welcomeBytes, null);
    }
}
