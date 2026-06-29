#nullable enable
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PawSharp.Voice.DAVE;

public sealed class DAVEProtocol : IDisposable
{
    private readonly MLSState _mls = new();
    private readonly byte[] _localIdentity;
    private readonly ILogger? _logger;
    private uint _localSsrc;
    private long _outgoingFrameCounter;
    private bool _disposed;

    private volatile bool _active;
    private volatile bool _transitionPending;
    private byte[]? _externalSenderPackage;
    private int _currentTransitionId;

    public DAVEProtocol() : this("discord-dave-client", null) { }

    public DAVEProtocol(string userId, ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("userId must not be empty.", nameof(userId));
        _localIdentity = Encoding.UTF8.GetBytes(userId);
        _logger = logger;
    }

    public bool IsActive => _active;
    public bool IsTransitionPending => _transitionPending;
    public ulong EpochNumber => _mls.EpochNumber;
    public byte[]? EpochSecret => _mls.EpochSecret;
    internal MLSState MlsState => _mls;

    public uint LocalSsrc
    {
        get => _localSsrc;
        set => _localSsrc = value;
    }

    /// <summary>
    /// Handles JSON text DAVE opcodes: 21 (PrepareTransition), 22 (ExecuteTransition),
    /// 23 (TransitionReady — client sent, no action), 24 (PrepareEpoch), 31 (InvalidCommitWelcome).
    /// </summary>
    public async Task HandleJsonMessageAsync(
        int opcode,
        JsonElement data,
        ClientWebSocket? webSocket,
        CancellationToken ct = default)
    {
        switch (opcode)
        {
            case 21: // DavePrepareTransition — server announces upcoming DAVE activation
                if (data.TryGetProperty("dave_transition_id", out var tidProp))
                    _currentTransitionId = tidProp.GetInt32();
                if (webSocket != null)
                    await SendTransitionReadyAsync(webSocket, ct).ConfigureAwait(false);
                _transitionPending = true;
                break;

            case 22: // DaveExecuteTransition — server signals transition is executing
                break;

            case 23: // DaveTransitionReady — we sent this; no action needed
                break;

            case 24: // DavePrepareEpoch — server announces epoch info
                break;

            case 31: // DaveMlsInvalidCommitWelcome — server rejected our commit/welcome
                _active = false;
                _transitionPending = false;
                Interlocked.Exchange(ref _outgoingFrameCounter, 0L);
                _mls.Reset();
                if (webSocket != null)
                    await SendKeyPackageBinaryAsync(webSocket, ct).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>
    /// Handles binary DAVE opcodes: 25 (MlsExternalSender), 26 (MlsKeyPackage — sent by us),
    /// 27 (MlsProposals), 28 (MlsCommitWelcome — sent by us),
    /// 29 (MlsAnnounceCommitTransition), 30 (MlsWelcome).
    /// </summary>
    public async Task HandleBinaryMessageAsync(
        int opcode,
        byte[] payload,
        ClientWebSocket? webSocket,
        CancellationToken ct = default)
    {
        switch (opcode)
        {
            case 25: // DaveMlsExternalSender — server credential for commit validation
                _externalSenderPackage = payload;
                _mls.SetExternalSenderPackage(payload);
                if (webSocket != null)
                    await SendKeyPackageBinaryAsync(webSocket, ct).ConfigureAwait(false);
                break;

            case 26: // DaveMlsKeyPackage — we sent this; no action needed
                break;

            case 27: // DaveMlsProposals — server sends proposals
                if (payload.Length > 0)
                    _mls.ProcessProposals(payload);
                break;

            case 28: // DaveMlsCommitWelcome — we sent this; no action needed
                break;

            case 29: // DaveMlsAnnounceCommitTransition — server confirms commit
                if (payload.Length > 0)
                    _mls.ProcessCommit(payload);
                _active = true;
                _transitionPending = false;
                Interlocked.Exchange(ref _outgoingFrameCounter, 0L);
                break;

            case 30: // DaveMlsWelcome — server sends Welcome (late join or recovery)
                if (payload.Length > 0)
                {
                    try
                    {
                        _mls.ProcessWelcome(payload);
                        _active = true;
                        _transitionPending = false;
                        Interlocked.Exchange(ref _outgoingFrameCounter, 0L);
                    }
                    catch
                    {
                        _mls.Reset();
                        if (webSocket != null)
                            await SendKeyPackageBinaryAsync(webSocket, ct).ConfigureAwait(false);
                    }
                }
                break;
        }
    }

    public byte[] EncryptFrame(byte[] frame, byte[]? additionalData = null)
    {
        if (!_active || !_mls.IsInitialized)
            return frame;

        try
        {
            var key = _mls.GetSenderKey(_localSsrc);
            if (key == null || key.Length == 0)
                return frame;

            var counter = (ulong)Interlocked.Increment(ref _outgoingFrameCounter);
            return DAVEEncryption.EncryptFrame(frame, key, _localSsrc, counter, additionalData);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DAVE encryption failed for SSRC {Ssrc}, returning plaintext", _localSsrc);
            return frame;
        }
    }

    public byte[] DecryptFrame(byte[] encryptedFrame, uint ssrc, byte[]? additionalData = null)
    {
        if (!_active || !_mls.IsInitialized)
            return encryptedFrame;

        try
        {
            var key = _mls.GetSenderKey(ssrc);
            if (key == null || key.Length == 0)
                return encryptedFrame;

            return DAVEEncryption.DecryptFrame(encryptedFrame, key, ssrc, additionalData);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DAVE decryption failed for SSRC {Ssrc}, returning ciphertext", ssrc);
            return encryptedFrame;
        }
    }

    private async Task SendTransitionReadyAsync(ClientWebSocket webSocket, CancellationToken ct)
    {
        var payload = new
        {
            op = (int)DAVEVoiceOpcode.DaveTransitionReady,
            d = new
            {
                dave_transition_id = _currentTransitionId,
                key_package = Convert.ToBase64String(GenerateKeyPackage()),
            },
        };

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
    }

    private async Task SendKeyPackageBinaryAsync(ClientWebSocket webSocket, CancellationToken ct)
    {
        var keyPackage = GenerateKeyPackage();
        var msg = new byte[1 + keyPackage.Length];
        msg[0] = (byte)DAVEVoiceOpcode.DaveMlsKeyPackage;
        keyPackage.CopyTo(msg, 1);
        await webSocket.SendAsync(msg, WebSocketMessageType.Binary, true, ct).ConfigureAwait(false);
    }

    public byte[] GenerateKeyPackage()
        => _mls.GenerateKeyPackage(_localIdentity);

    public void Reset()
    {
        _active = false;
        _transitionPending = false;
        _currentTransitionId = 0;
        var savedExtSender = _externalSenderPackage;
        _externalSenderPackage = null;
        Interlocked.Exchange(ref _outgoingFrameCounter, 0L);
        _mls.Reset();
        if (savedExtSender != null)
        {
            _externalSenderPackage = savedExtSender;
            _mls.SetExternalSenderPackage(savedExtSender);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mls.Dispose();
    }
}
