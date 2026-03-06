#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

/// <summary>
/// Tests for <see cref="DAVEProtocol"/> — the top-level DAVE state machine.
/// </summary>
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

        result.Should().BeSameAs(frame, "no encryption should occur before op 24");
    }

    [Fact]
    public void DecryptFrame_WhenInactive_ReturnsSameBytes()
    {
        var frame = new byte[] { 0x11, 0x22, 0x33 };
        var result = _proto.DecryptFrame(frame, ssrc: 1);

        result.Should().BeSameAs(frame, "no decryption should occur before op 24");
    }

    // ── Op 24 (ProtocolReady) activates encryption ────────────────────────────

    [Fact]
    public async Task HandleOpcode_24_SetsIsActiveTrue()
    {
        // First prime the MLS state via a Welcome (op 25)
        await DispatchWelcomeAsync();

        await DispatchOpcodeAsync(24);

        _proto.IsActive.Should().BeTrue();
    }

    // ── Ops 21–31 are handled without throwing ────────────────────────────────

    [Theory]
    [InlineData(22)]  // KeyPackageRequest  (requires webSocket — we pass null; should not throw)
    [InlineData(23)]  // PrepareTransition
    [InlineData(27)]  // Proposals
    [InlineData(28)]  // PrepareEpoch
    [InlineData(29)]  // AnnounceCommitTransition
    [InlineData(30)]  // InvalidCommitWelcome
    [InlineData(31)]  // ExternalSenderPackage
    public async Task HandleOpcode_KnownDAVEOpcodes_DoNotThrow(int opcode)
    {
        var data = MakeBase64Payload(new byte[] { 0x01, 0x02 });
        Func<Task> act = () => _proto.HandleOpcodeAsync(opcode, data, webSocket: null);
        await act.Should().NotThrowAsync();
    }

    // ── Welcome (op 25) initialises MLS ──────────────────────────────────────

    [Fact]
    public async Task HandleOpcode_25_Welcome_EnablesMlsForProtocol()
    {
        await DispatchWelcomeAsync();

        // After Welcome the protocol is not yet active (op 24 hasn't been sent),
        // but the MLS layer should be initialised so that once op 24 arrives
        // encryption can start immediately.  We verify this indirectly by
        // confirming op 24 successfully activates.
        await DispatchOpcodeAsync(24);
        _proto.IsActive.Should().BeTrue();
    }

    // ── Commit (op 26) advances the MLS epoch ────────────────────────────────

    [Fact]
    public async Task HandleOpcode_26_Commit_DoesNotThrow()
    {
        await DispatchWelcomeAsync();
        var data = MakeBase64Payload(new byte[] { 0xCC, 0xDD });
        Func<Task> act = () => _proto.HandleOpcodeAsync(26, data, null);
        await act.Should().NotThrowAsync();
    }

    // ── End-to-end encrypt/decrypt round trip when active ─────────────────────

    [Fact]
    public async Task ActiveProtocol_EncryptDecrypt_RoundTrip()
    {
        const uint mySSRC    = 0xABCD;
        const uint theirSSRC = 0x1234;

        // Set up the local sender protocol
        _proto.LocalSsrc = mySSRC;
        await DispatchWelcomeAsync();
        await DispatchOpcodeAsync(24);
        _proto.IsActive.Should().BeTrue();

        var plaintext = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        // Encrypt as 'mySSRC' sender
        var encrypted = _proto.EncryptFrame(plaintext);
        encrypted.Should().NotBeEquivalentTo(plaintext, "active protocol must actually encrypt");

        // Build a second protocol instance that simulates the remote side
        // (same Welcome payload → same epoch secret → same sender keys)
        using var remote = new DAVEProtocol();
        remote.LocalSsrc = theirSSRC;

        // Replay the same Welcome and ReadyTransition on the remote side
        var welcomeData = MakeBase64Payload(WelcomeBytes);
        await remote.HandleOpcodeAsync(25, welcomeData, null);
        await remote.HandleOpcodeAsync(24, JsonDocument.Parse("{}").RootElement, null);

        // The remote decrypts using mySSRC as the sender SSRC
        var decrypted = remote.DecryptFrame(encrypted, ssrc: mySSRC);
        decrypted.Should().BeEquivalentTo(plaintext, "remote must recover the original frame");
    }

    // ── Frame counter increments produce unique ciphertexts ────────────────────

    [Fact]
    public async Task ActiveProtocol_TwoFramesFromSamePayload_ProduceDifferentCiphertext()
    {
        _proto.LocalSsrc = 0x01;
        await DispatchWelcomeAsync();
        await DispatchOpcodeAsync(24);

        var payload = new byte[] { 0xFF };
        var enc1 = _proto.EncryptFrame(payload);
        var enc2 = _proto.EncryptFrame(payload);

        enc1.Should().NotBeEquivalentTo(enc2,
            "monotonic frame counter must produce different nonces each call");
    }

    // ── Opcode enum coverage ──────────────────────────────────────────────────

    [Theory]
    [InlineData(DAVEVoiceOpcode.DaveMlsKeyPackage,              21)]
    [InlineData(DAVEVoiceOpcode.DaveMlsKeyPackageRequest,       22)]
    [InlineData(DAVEVoiceOpcode.DaveProtocolPrepareTransition,  23)]
    [InlineData(DAVEVoiceOpcode.DaveProtocolReady,              24)]
    [InlineData(DAVEVoiceOpcode.DaveMlsWelcome,                 25)]
    [InlineData(DAVEVoiceOpcode.DaveMlsCommit,                  26)]
    [InlineData(DAVEVoiceOpcode.DaveMlsProposals,               27)]
    [InlineData(DAVEVoiceOpcode.DaveProtocolPrepareEpoch,       28)]
    [InlineData(DAVEVoiceOpcode.DaveMlsAnnounceCommitTransition,29)]
    [InlineData(DAVEVoiceOpcode.DaveMlsInvalidCommitWelcome,    30)]
    [InlineData(DAVEVoiceOpcode.DaveMlsExternalSenderPackage,   31)]
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

    // Deterministic Welcome payload used across round-trip tests
    private static readonly byte[] WelcomeBytes = new byte[]
        { 0x57, 0x65, 0x6C, 0x63, 0x6F, 0x6D, 0x65 }; // "Welcome" in ASCII

    private static JsonElement MakeBase64Payload(byte[] raw)
    {
        var b64  = Convert.ToBase64String(raw);
        var json = $"\"{b64}\"";
        return JsonDocument.Parse(json).RootElement;
    }

    private async Task DispatchWelcomeAsync()
    {
        var data = MakeBase64Payload(WelcomeBytes);
        await _proto.HandleOpcodeAsync((int)DAVEVoiceOpcode.DaveMlsWelcome, data, null);
    }

    private async Task DispatchOpcodeAsync(int opcode)
    {
        // Use a null/empty JSON object for opcodes that don't need payload data
        var data = JsonDocument.Parse("{}").RootElement;
        await _proto.HandleOpcodeAsync(opcode, data, null);
    }
}
