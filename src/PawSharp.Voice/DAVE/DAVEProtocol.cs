#nullable enable
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// Handles the DAVE E2EE protocol state machine for a Discord voice connection.
///
/// DAVE (Discord's AV1 Encrypted / E2EE) applies end-to-end encryption to voice
/// and video frames inside DMs and Group DMs.  It uses MLS (RFC 9420) for key
/// exchange and AES-128-GCM for media frame encryption.
///
/// Lifecycle:
///   1. Server sends op 23 (PrepareTransition) — note the upcoming transition.
///   2. Client sends op 21 (KeyPackage) — our MLS key package.
///   3. Server sends op 25 (Welcome) OR op 26 (Commit) — establish group.
///   4. Server sends op 24 (ProtocolReady)  — activate encryption.
///   5. All outgoing frames are encrypted; incoming frames are decrypted.
///   6. Epoch advances on op 26 (Commit) / op 28 (PrepareEpoch).
/// </summary>
public sealed class DAVEProtocol : IDisposable
{
    private readonly MLSState _mls = new();
    private uint _localSsrc;
    private ulong _outgoingFrameCounter;
    private bool _disposed;

    // Set when op 24 has been received — encryption is active
    private volatile bool _active;

    /// <summary>True when DAVE encryption is fully active (op 24 received).</summary>
    public bool IsActive => _active;

    /// <summary>The local sender's SSRC (set from the voice Ready payload, op 2).</summary>
    public uint LocalSsrc
    {
        get => _localSsrc;
        set => _localSsrc = value;
    }

    // ── Opcode dispatch ──────────────────────────────────────────────────────

    /// <summary>
    /// Routes an incoming voice gateway DAVE opcode to the appropriate handler.
    /// </summary>
    /// <param name="opcode">The numeric opcode (21–31).</param>
    /// <param name="data">The <c>d</c> field from the gateway JSON payload.</param>
    /// <param name="webSocket">The voice WebSocket (used to send key-package response).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task HandleOpcodeAsync(
        int opcode,
        JsonElement data,
        ClientWebSocket? webSocket,
        CancellationToken ct = default)
    {
        switch ((DAVEVoiceOpcode)opcode)
        {
            case DAVEVoiceOpcode.DaveMlsKeyPackageRequest:
                // Server wants our MLS key package — send op 21 back
                if (webSocket != null)
                    await SendKeyPackageAsync(webSocket, ct);
                break;

            case DAVEVoiceOpcode.DaveProtocolPrepareTransition:
                // Prepare to switch DAVE version / establish a new group; no action yet
                break;

            case DAVEVoiceOpcode.DaveMlsWelcome:
                // New group member: process Welcome message
                var welcomeBytes = ExtractBinaryPayload(data);
                if (welcomeBytes != null)
                    _mls.ProcessWelcome(welcomeBytes);
                break;

            case DAVEVoiceOpcode.DaveMlsCommit:
                // Group state update: apply commit, advance epoch
                var commitBytes = ExtractBinaryPayload(data);
                if (commitBytes != null)
                    _mls.ProcessCommit(commitBytes);
                break;

            case DAVEVoiceOpcode.DaveMlsProposals:
                var proposalBytes = ExtractBinaryPayload(data);
                if (proposalBytes != null)
                    _mls.ProcessProposals(proposalBytes);
                break;

            case DAVEVoiceOpcode.DaveProtocolPrepareEpoch:
                // Server signals an upcoming epoch change; the commit follows shortly
                break;

            case DAVEVoiceOpcode.DaveProtocolReady:
                // Switch to DAVE encryption
                _active = true;
                break;

            case DAVEVoiceOpcode.DaveMlsExternalSenderPackage:
                // Distribute external sender key package
                var extBytes = ExtractBinaryPayload(data);
                _ = extBytes; // stored/forwarded by a real implementation
                break;

            case DAVEVoiceOpcode.DaveMlsAnnounceCommitTransition:
            case DAVEVoiceOpcode.DaveMlsInvalidCommitWelcome:
                // Informational — a real implementation would re-sync if invalid
                break;
        }
    }

    // ── Media frame encryption / decryption ──────────────────────────────────

    /// <summary>
    /// Encrypts an outgoing voice frame when DAVE is active.
    /// Returns the original frame unchanged when DAVE has not yet been activated.
    /// </summary>
    /// <param name="frame">Raw Opus-encoded audio frame.</param>
    /// <param name="additionalData">Optional RTP header bytes for AAD.</param>
    public byte[] EncryptFrame(byte[] frame, byte[]? additionalData = null)
    {
        if (!_active || !_mls.IsInitialized)
            return frame;

        var key = _mls.GetSenderKey(_localSsrc);
        var counter = Interlocked.Increment(ref _outgoingFrameCounter);
        // counter starts at 1 — decrement so the first encrypted frame is 1
        return DAVEEncryption.EncryptFrame(frame, key, _localSsrc, (ulong)counter, additionalData);
    }

    /// <summary>
    /// Decrypts an incoming voice frame when DAVE is active.
    /// Returns the original data unchanged when DAVE is not active.
    /// </summary>
    /// <param name="encryptedFrame">Encrypted frame as received from Discord (nonce+ciphertext+tag).</param>
    /// <param name="ssrc">The sender's SSRC.</param>
    /// <param name="additionalData">Optional AAD (must match what was used during encryption).</param>
    public byte[] DecryptFrame(byte[] encryptedFrame, uint ssrc, byte[]? additionalData = null)
    {
        if (!_active || !_mls.IsInitialized)
            return encryptedFrame;

        var key = _mls.GetSenderKey(ssrc);
        return DAVEEncryption.DecryptFrame(encryptedFrame, key, additionalData);
    }

    // ── MLS key-package generation ────────────────────────────────────────────

    /// <summary>
    /// Builds the DAVE key-package payload and sends it to the voice gateway (op 21).
    /// </summary>
    private async Task SendKeyPackageAsync(ClientWebSocket webSocket, CancellationToken ct)
    {
        var payload = new
        {
            op = (int)DAVEVoiceOpcode.DaveMlsKeyPackage,
            d = Convert.ToBase64String(GenerateKeyPackage()),
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    /// <summary>
    /// Generates a production-quality MLS KeyPackage for this DAVE session.
    /// Returns TLS-encoded KeyPackage bytes per RFC 9420 §10.
    /// </summary>
    private static byte[] GenerateKeyPackage()
    {
        var identity = System.Text.Encoding.UTF8.GetBytes("discord-dave-client");
        var kp       = MLS.Messages.KeyPackage.Generate(identity);
        return kp.Encode();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[]? ExtractBinaryPayload(JsonElement data)
    {
        // Discord sends binary DAVE payloads as Base64 strings in the JSON `d` field
        if (data.ValueKind == JsonValueKind.String)
        {
            var b64 = data.GetString();
            if (!string.IsNullOrEmpty(b64))
                return Convert.FromBase64String(b64);
        }
        return null;
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mls.Dispose();
    }
}
