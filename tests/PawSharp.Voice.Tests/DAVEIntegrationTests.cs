#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

public class DAVEIntegrationTests : IDisposable
{
    private readonly DAVEProtocol _client = new();

    public void Dispose() => _client.Dispose();

    // ── Helpers for raw message parsing ──────────────────────────────────────

    /// <summary>
    /// Parses a server-to-client binary DAVE message with the format:
    ///   [2-byte big-endian seq][1-byte opcode][payload]
    /// Then dispatches the opcode and payload to HandleBinaryMessageAsync.
    /// </summary>
    private async Task HandleRawBinaryAsync(byte[] rawMessage)
    {
        var opcode = (int)rawMessage[2];
        var payload = new byte[rawMessage.Length - 3];
        if (payload.Length > 0)
            Buffer.BlockCopy(rawMessage, 3, payload, 0, payload.Length);
        await _client.HandleBinaryMessageAsync(opcode, payload, webSocket: null);
    }

    /// <summary>
    /// Parses a JSON text DAVE message containing "op" and "d" fields,
    /// then dispatches the opcode and data to HandleJsonMessageAsync.
    /// </summary>
    private async Task HandleRawJsonAsync(string jsonText)
    {
        using var doc = JsonDocument.Parse(jsonText);
        var op = doc.RootElement.GetProperty("op").GetInt32();
        var d = doc.RootElement.GetProperty("d");
        await _client.HandleJsonMessageAsync(op, d, webSocket: null);
    }

    // ── Welcome flow (full handshake) ────────────────────────────────────────

    [Fact]
    public async Task WelcomeFlow_FullHandshake_ActivatesClient()
    {
        // a. Server sends JSON op 21 (PrepareTransition) with transition_id
        await HandleRawJsonAsync(@"{""op"":21,""d"":{""dave_transition_id"":1}}");
        _client.IsTransitionPending.Should().BeTrue();

        // b. Server sends JSON op 22 (ExecuteTransition)
        await HandleRawJsonAsync(@"{""op"":22,""d"":{}}");

        // c. Server sends JSON op 24 (PrepareEpoch)
        await HandleRawJsonAsync(@"{""op"":24,""d"":{}}");

        // d. Server sends BINARY op 25 (MlsExternalSender) with empty payload
        var extSenderMsg = new byte[] { 0x00, 0x01, 25 };
        await HandleRawBinaryAsync(extSenderMsg);

        // e. Server sends BINARY op 30 (MlsWelcome)
        var (welcomeBytes, _) = DAVETestData.CreateWelcome(_client.MlsState);
        var welcomeMsg = new byte[3 + welcomeBytes.Length];
        welcomeMsg[0] = 0x00;
        welcomeMsg[1] = 0x02;
        welcomeMsg[2] = 30;
        Buffer.BlockCopy(welcomeBytes, 0, welcomeMsg, 3, welcomeBytes.Length);
        await HandleRawBinaryAsync(welcomeMsg);

        // f. Verify final state
        _client.IsActive.Should().BeTrue();
        _client.EpochNumber.Should().Be(1);
    }

    // ── AnnounceCommitTransition flow (op 29) ────────────────────────────────

    [Fact]
    public async Task AnnounceCommitTransition_AfterWelcome_AdvancesEpoch()
    {
        // Activate via Welcome first
        await ActivateWithWelcomeAsync();

        // a. Send BINARY op 27 (MlsProposals) with empty payload
        var proposalsMsg = new byte[] { 0x00, 0x03, 27 };
        await HandleRawBinaryAsync(proposalsMsg);

        // b. Send BINARY op 29 (MlsAnnounceCommitTransition) with commit payload
        var commitBytes = DAVETestData.CreateEmptyCommit();
        var commitMsg = new byte[3 + commitBytes.Length];
        commitMsg[0] = 0x00;
        commitMsg[1] = 0x04;
        commitMsg[2] = 29;
        Buffer.BlockCopy(commitBytes, 0, commitMsg, 3, commitBytes.Length);
        await HandleRawBinaryAsync(commitMsg);

        // c. Verify epoch advanced from 1 to 2
        _client.EpochNumber.Should().Be(2);
        _client.IsActive.Should().BeTrue();
    }

    // ── Binary message format parsing ────────────────────────────────────────

    [Fact]
    public async Task RawBinaryMessage_CorrectlyExtractsOpcodeAndPayload()
    {
        // Build a raw binary message for op 29 (0x1D = 29) with a commit payload
        // Format: [2-byte big-endian seq][1-byte opcode][payload]
        var commitBytes = DAVETestData.CreateEmptyCommit();

        // First, we need an active client to accept the commit
        await ActivateWithWelcomeAsync();

        var rawMessage = new byte[3 + commitBytes.Length];
        rawMessage[0] = 0x00; // seq high byte
        rawMessage[1] = 0x01; // seq low byte  → seq = 1
        rawMessage[2] = 29;   // opcode = MlsAnnounceCommitTransition
        Buffer.BlockCopy(commitBytes, 0, rawMessage, 3, commitBytes.Length);

        await HandleRawBinaryAsync(rawMessage);

        // Op 29 activates the client and commits → epoch should advance to 2
        _client.IsActive.Should().BeTrue();
        _client.EpochNumber.Should().Be(2);
    }

    // ── JSON opcode parsing ──────────────────────────────────────────────────

    [Fact]
    public async Task RawJsonMessage_CorrectlyReadsDaveTransitionId()
    {
        const string json = @"{""op"":21,""d"":{""dave_transition_id"":42}}";

        await HandleRawJsonAsync(json);

        _client.IsTransitionPending.Should().BeTrue();
    }

    // ── Error resilience ─────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidCommitWelcome_ResetsClient()
    {
        // Activate first
        await ActivateWithWelcomeAsync();
        _client.IsActive.Should().BeTrue();

        // Send op 31 (InvalidCommitWelcome) → should reset
        await HandleRawJsonAsync(@"{""op"":31,""d"":{}}");

        _client.IsActive.Should().BeFalse();
        _client.IsTransitionPending.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyWelcomePayload_DoesNotThrow()
    {
        var rawMsg = new byte[] { 0x00, 0x01, 30 };
        Func<Task> act = () => HandleRawBinaryAsync(rawMsg);
        await act.Should().NotThrowAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Performs a minimal Welcome handshake to activate the client.
    /// Sends op 25 (ExternalSender) then op 30 (Welcome).
    /// </summary>
    private async Task ActivateWithWelcomeAsync()
    {
        await _client.HandleBinaryMessageAsync(25, Array.Empty<byte>(), null);
        var (welcomeBytes, _) = DAVETestData.CreateWelcome(_client.MlsState);
        await _client.HandleBinaryMessageAsync(30, welcomeBytes, null);
    }
}
