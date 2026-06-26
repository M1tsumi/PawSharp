#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;

namespace PawSharp.Voice;

/// <summary>Describes the lifecycle state of a <see cref="VoiceConnection"/>.</summary>
public enum VoiceConnectionState
{
    /// <summary>No active WebSocket connection.</summary>
    Disconnected,
    /// <summary>WebSocket connect is in progress.</summary>
    Connecting,
    /// <summary>WebSocket is open and UDP discovery is in progress.</summary>
    Discovering,
    /// <summary>WebSocket is open, UDP is connected, and the voice session is active.</summary>
    Connected,
    /// <summary>DAVE E2EE is being negotiated.</summary>
    DaveNegotiating,
    /// <summary>DAVE E2EE encryption is active.</summary>
    DaveEncrypted,
    /// <summary>Graceful close is in progress.</summary>
    Disconnecting,
}

/// <summary>Transport encryption modes supported by Discord's voice protocol.</summary>
public enum VoiceEncryptionMode
{
    /// <summary>XSalsa20-Poly1305-lite (RTP size).</summary>
    XSalsa20Poly1305LiteRtpSize,
    /// <summary>XSalsa20-Poly1305 (suffix).</summary>
    XSalsa20Poly1305Suffix,
    /// <summary>XSalsa20-Poly1305.</summary>
    XSalsa20Poly1305,
    /// <summary>AEAD_AES256_GCM (RTP size).</summary>
    AeadAes256GcmRtpSize,
    /// <summary>AEAD_XChaCha20_Poly1305 (RTP size).</summary>
    AeadXChaCha20Poly1305RtpSize,
}

/// <summary>Configuration options for voice connections.</summary>
public class VoiceConnectionOptions
{
    /// <summary>Preferred encryption mode. Defaults to AEAD_AES256_GCM_RTPSIZE.</summary>
    public VoiceEncryptionMode PreferredEncryptionMode { get; set; } = VoiceEncryptionMode.AeadAes256GcmRtpSize;

    /// <summary>Opus encoder bitrate in bits per second. Defaults to 64000.</summary>
    public int OpusBitrate { get; set; } = 64000;

    /// <summary>Whether to automatically initialize audio input/output. Defaults to true.</summary>
    public bool AutoInitializeAudio { get; set; } = true;

    /// <summary>
    /// Whether to enable DAVE E2EE for DMs and Group DMs.
    /// DAVE is only used for private calls; server voice channels use standard encryption.
    /// Defaults to true.
    /// </summary>
    public bool EnableDave { get; set; } = true;
}

/// <summary>
/// Represents a voice connection to a Discord voice channel.
/// </summary>
public class VoiceConnection : IDisposable
{
    private readonly DiscordClient _discordClient;
    private readonly Channel _channel;
    private readonly ILogger _logger;
    private readonly Action<ulong>? _onConnectionFailed;
    private readonly VoiceConnectionOptions _options;
    private ClientWebSocket? _webSocket;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _heartbeatTask;
    private Task? _udpReceiveTask;
    private bool _disposed;
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
    private readonly ArrayPool<short> _shortPool = ArrayPool<short>.Shared;
    private Task? _speakingTask;
    private byte[] _rtpHeaderBuffer = new byte[12];
    private int _heartbeatInterval = 5000; // Default 5 seconds, updated from HELLO
    private long _lastFrameSentTick;       // TickCount64 of the last outgoing audio frame (for silence keep-alive)
    
    // UDP connection state
    private string? _udpIp;
    private int _udpPort;
    private byte[]? _secretKey;
    private VoiceEncryptionMode _encryptionMode;

    // Stored handshake parameters for reconnects
    private string? _endpoint;
    private ulong _voiceGuildId;
    private ulong _voiceUserId;
    private string? _sessionId;
    private string? _token;
    
    // Resume support
    private int? _seqAck;

    // ── Opus codec constants ─────────────────────────────────────────────────
    private const int OpusSampleRate = 48000;   // Hz
    private const int OpusChannels   = 1;        // mono
    private const int OpusFrameSize  = 960;      // samples — 20 ms at 48 kHz
    private const int PcmFrameBytes  = OpusFrameSize * OpusChannels * sizeof(short); // 1 920 bytes
    private const int MaxOpusBytes   = 4000;     // conservative max packet per RFC 6716

    // ── Voice WebSocket protocol version ────────────────────────────────────
    // Discord's voice WebSocket protocol version. DAVE E2EE requires v8.
    private const int VoiceProtocolVersion = 8;

    // UDP keep-alive: send an Opus silence frame every 5 s during silence to keep NAT mappings
    // alive and prevent Discord's voice server from timing out the session.
    private static readonly byte[] SilenceFrame = [0xF8, 0xFF, 0xFE];
    private const int KeepAliveIntervalMs = 5_000;  // how often to check / send
    private const int SilenceThresholdMs  = 5_000;  // treat connection as silent after this many ms

    // Opus codec handles (Concentus — pure .NET, zero P/Invoke)
    private IOpusEncoder? _opusEncoder;
    private IOpusDecoder? _opusDecoder;

    // RTP sequencing state (per-connection, monotonically increasing)
    private ushort _rtpSequence;
    private uint   _rtpTimestamp;
    private readonly object _rtpLock = new();

    // Outgoing PCM accumulation — holds partial frames until a full 20 ms chunk is ready
    private readonly List<byte> _pendingPcm = new();

    // Speaking gate — prevents redundant op-5 transmissions
    private bool _speaking;

    // NAudio I/O (microphone capture and speaker playback)
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _waveProvider;

    // DAVE E2EE protocol (for DMs and Group DMs only)
    private DAVE.DAVEProtocol? _dave;
    private bool _daveEnabled;

    // Running tasks tracked for proper disposal
    private Task? _keepAliveTask;

    /// <summary>Gets the current connection lifecycle state.</summary>
    public VoiceConnectionState State { get; private set; } = VoiceConnectionState.Disconnected;
    
    /// <summary>Gets the configuration options for this connection.</summary>
    public VoiceConnectionOptions Options => _options;
    
    /// <summary>Raised when the connection state changes.</summary>
    public event Action<VoiceConnectionState, VoiceConnectionState>? StateChanged;
    
    /// <summary>Raised when the UDP connection is established.</summary>
    public event Action<string, int>? UdpConnected;
    
    /// <summary>Raised when a voice user connects or disconnects.</summary>
    public event Action<ulong, bool>? UserVoiceStateChanged;

    /// <summary>Raised when DAVE E2EE encryption is activated (after op 29 AnnounceCommitTransition or op 30 Welcome).</summary>
    public event Action? DaveEncryptionActivated;

    /// <summary>Raised when a DAVE epoch advances (after processing a Commit message).</summary>
    public event Action<ulong>? DaveEpochAdvanced;

    /// <summary>Raised when a DAVE error occurs.</summary>
    public event Action<Exception>? DaveError;
    
    /// <summary>Gets the SSRC (Synchronization Source) for this connection.</summary>
    public uint Ssrc { get; private set; }

    /// <summary>
    /// Gets the guild ID.
    /// </summary>
    public ulong GuildId => _channel.GuildId ?? 0;

    /// <summary>
    /// Gets the channel ID.
    /// </summary>
    public ulong ChannelId => _channel.Id;

    /// <summary>
    /// Gets the voice channel.
    /// </summary>
    public Channel Channel => _channel;

    /// <summary>
    /// Gets whether the connection is connected.
    /// </summary>
    public bool IsConnected => State == VoiceConnectionState.Connected;

    /// <summary>
    /// Gets whether audio is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Raised whenever a decoded PCM audio frame is received from another speaker.
    /// The first argument is the sender SSRC and the second is the raw 16-bit signed
    /// mono PCM byte array at 48 kHz — identical to what is fed to the local speaker.
    /// Subscribe to this event to capture or process incoming voice audio
    /// (e.g. speech-to-text transcription) without relying on NAudio playback.
    /// </summary>
    public event Action<uint, byte[]>? VoicePacketReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceConnection"/> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="channel">The voice channel.</param>
    /// <param name="onConnectionFailed">Callback for connection failures.</param>
    /// <param name="logger">Logger used for voice diagnostics.</param>
    /// <param name="options">Configuration options for the connection.</param>
    public VoiceConnection(
        DiscordClient discordClient,
        Channel channel,
        Action<ulong>? onConnectionFailed = null,
        ILogger? logger = null,
        VoiceConnectionOptions? options = null)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _onConnectionFailed = onConnectionFailed;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        _options = options ?? new VoiceConnectionOptions();

        _daveEnabled = _options.EnableDave;

        // Initialize audio components
        if (_options.AutoInitializeAudio)
            InitializeAudio();
    }

    private void InitializeAudio()
    {
        // Opus codec is always available (pure managed, no audio hardware needed)
        _opusEncoder = OpusCodecFactory.CreateEncoder(OpusSampleRate, OpusChannels, OpusApplication.OPUS_APPLICATION_VOIP, null);
        _opusEncoder.Bitrate = _options.OpusBitrate;
        _opusDecoder = OpusCodecFactory.CreateDecoder(OpusSampleRate, OpusChannels, null);

        try
        {
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnWaveInDataAvailable;
                _waveIn.Dispose();
                _waveIn = null;
            }
            _waveOut?.Dispose();
            _waveOut = null;

            // Initialize wave input (microphone)
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(48000, 16, 1),
                BufferMilliseconds = 20
            };
            _waveIn.DataAvailable += OnWaveInDataAvailable;

            // Initialize wave output (speakers)
            _waveOut = new WaveOutEvent();
            _waveProvider = new BufferedWaveProvider(new WaveFormat(48000, 16, 1));
            _waveOut.Init(_waveProvider);
            _waveOut.PlaybackStopped += (_, _) => { IsPlaying = false; };
        }
        catch (Exception ex)
        {
            // Audio hardware is unavailable (e.g. headless server). Audio I/O will
            // be skipped; voice packet send/receive still works.
            _logger.LogWarning(ex, "Audio hardware initialization failed. Audio I/O will be disabled.");
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnWaveInDataAvailable;
                _waveIn.Dispose();
            }
            _waveIn = null;
            _waveOut?.Dispose();
            _waveOut = null;
            _waveProvider = null;
        }
    }

    /// <summary>
    /// Connects to the Discord voice WebSocket and sends the IDENTIFY payload.
    /// </summary>
    /// <param name="endpoint">The voice server endpoint from VOICE_SERVER_UPDATE (may have ":80" suffix).</param>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="userId">The bot's user ID.</param>
    /// <param name="sessionId">The session ID from VOICE_STATE_UPDATE.</param>
    /// <param name="token">The voice token from VOICE_SERVER_UPDATE.</param>
    public async Task ConnectAsync(string endpoint, ulong guildId, ulong userId, string sessionId, string token)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VoiceConnection));

        // Store for later reconnects
        _endpoint = endpoint;
        _voiceGuildId = guildId;
        _voiceUserId = userId;
        _sessionId = sessionId;
        _token = token;

        await ConnectInternalAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Reconnects using the stored handshake parameters.
    /// </summary>
    internal async Task ReconnectAsync()
    {
        if (_endpoint is null || _sessionId is null || _token is null)
            throw new InvalidOperationException("Cannot reconnect: handshake parameters not stored. Call ConnectAsync first.");

        await ConnectInternalAsync().ConfigureAwait(false);
    }

    private async Task ConnectInternalAsync()
    {
        if (_disposed)
            return;

        var previousState = State;
        State = VoiceConnectionState.Connecting;
        StateChanged?.Invoke(previousState, State);

        // Cancel any existing tasks (two-phase: create new first, then cancel old)
        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();

        // Reset DAVE protocol if it exists from a previous connection
        _dave?.Reset();

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        // Strip port suffix — Discord sends "endpoint:80", WebSocket URI needs plain hostname
        var host = _endpoint!.Contains(':') ? _endpoint.Substring(0, _endpoint.LastIndexOf(':')) : _endpoint;
        var uri = new Uri($"wss://{host}?v={VoiceProtocolVersion}");

        await _webSocket.ConnectAsync(uri, _cts.Token).ConfigureAwait(false);
        _speaking = false;  // reset speaking gate on fresh connection

        _receiveTask    = Task.Run(ReceiveLoopAsync,    _cts.Token);
        _heartbeatTask  = Task.Run(HeartbeatLoopAsync,  _cts.Token);

        // UDP keep-alive: sends silence frames during idle periods to prevent NAT
        // timeouts and keep the Discord voice server from dropping the session.
        // Note: the _udpClient isn't created until IP discovery, but the loop checks
        // for null internally so it's safe to start early.
        _keepAliveTask = Task.Run(KeepAliveLoopAsync, _cts.Token);

        // Send Opcode 0 IDENTIFY immediately after WebSocket upgrade
        await SendIdentifyAsync().ConfigureAwait(false);
    }

    private async Task SendIdentifyAsync()
    {
        using var ms = new MemoryStream(512);
        using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writer.WriteNumber("op", 0);
        writer.WriteStartObject("d");
        writer.WriteString("server_id", _voiceGuildId.ToString());
        writer.WriteString("user_id", _voiceUserId.ToString());
        writer.WriteString("session_id", _sessionId);
        writer.WriteString("token", _token);
        writer.WriteNumber("max_dave_protocol_version", 1);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        var buffer = ms.GetBuffer();
        var segment = new ArraySegment<byte>(buffer, 0, (int)ms.Length);
        await _webSocket!.SendAsync(segment, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Disconnects from the voice channel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        if (_disposed)
            return;

        var previousState = State;
        State = VoiceConnectionState.Disconnecting;
        StateChanged?.Invoke(previousState, State);
        _cts?.Cancel();

        // Dispose DAVE protocol if active
        _dave?.Dispose();
        _dave = null;

        if (_webSocket != null &&
            (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WebSocket close error during disconnect");
            }
            _webSocket.Dispose();
            _webSocket = null;
        }

        _udpClient?.Close();
        _udpClient = null;

        StopAudio();
        State = VoiceConnectionState.Disconnected;
        StateChanged?.Invoke(previousState, State);

        await Task.WhenAll(
            _receiveTask    ?? Task.CompletedTask,
            _heartbeatTask  ?? Task.CompletedTask,
            _keepAliveTask  ?? Task.CompletedTask,
            _udpReceiveTask ?? Task.CompletedTask
        ).ConfigureAwait(false);

        _cts?.Dispose();
        _cts = null;
    }
    
    /// <summary>
    /// Attempts to resume a dropped voice connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ResumeAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VoiceConnection));
        
        if (_sessionId is null || _token is null)
            throw new InvalidOperationException("Cannot resume: session not established.");
        
        var previousState = State;
        State = VoiceConnectionState.Connecting;
        StateChanged?.Invoke(previousState, State);
        
        // Cancel existing tasks (two-phase: create new first, then cancel old)
        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        oldCts?.Cancel();
        oldCts?.Dispose();
        
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();
        
        var host = _endpoint!.Contains(':') ? _endpoint.Substring(0, _endpoint.LastIndexOf(':')) : _endpoint;
        var uri = new Uri($"wss://{host}?v={VoiceProtocolVersion}");
        
        await _webSocket.ConnectAsync(uri, _cts.Token).ConfigureAwait(false);
        
        // Send Resume (op 7)
        using var ms = new MemoryStream(256);
        using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writer.WriteNumber("op", 7);
        writer.WriteStartObject("d");
        writer.WriteString("server_id", _voiceGuildId.ToString());
        writer.WriteString("session_id", _sessionId);
        writer.WriteString("token", _token);
        writer.WriteNumber("seq_ack", _seqAck ?? 0);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        var buf = ms.GetBuffer();
        var segment = new ArraySegment<byte>(buf, 0, (int)ms.Length);
        await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
        
        _receiveTask = Task.Run(ReceiveLoopAsync, _cts.Token);
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);
        
        _logger.LogInformation("Resume sent for channel {ChannelId}", _channel.Id);
    }

    /// <summary>
    /// Stops the current audio playback, if any.
    /// </summary>
    public void StopPlayback()
    {
        if (!IsPlaying) return;
        _waveOut?.Stop();
        IsPlaying = false;
        _waveProvider?.ClearBuffer();
    }

    /// <summary>
    /// Stops the current audio playback. Alias for <see cref="StopPlayback"/> with a Task return
    /// for use in async pipelines.
    /// </summary>
    public Task StopAsync()
    {
        StopPlayback();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads an audio file, resamples it to 48 kHz mono PCM if necessary, encodes it with Opus,
    /// and streams it to the voice channel. Raises the Discord Speaking gate automatically.
    /// </summary>
    /// <param name="filePath">Path to an audio file readable by NAudio (WAV, MP3, AIFF, etc.).</param>
    /// <param name="cancellationToken">Token to cancel mid-stream playback.</param>
    public async Task PlayAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var reader = new AudioFileReader(filePath);
        await PlayAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads 16-bit signed mono PCM at 48 kHz from <paramref name="pcmStream"/> and streams
    /// it to the voice channel. Use <see cref="PlayAsync(string, CancellationToken)"/> when
    /// you have a file in another format — it handles resampling automatically.
    /// </summary>
    /// <param name="pcmStream">
    /// A <see cref="WaveStream"/> whose format is (or will be resampled to) 48 kHz, mono, 16-bit PCM.
    /// If the format does not match the Opus requirements it is automatically resampled via
    /// <see cref="MediaFoundationResampler"/>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel mid-stream playback.</param>
    public async Task PlayAsync(WaveStream pcmStream, CancellationToken cancellationToken = default)
    {
        // Target format required by Opus: 48 kHz, 1 channel, 16-bit
        var targetFormat = new WaveFormat(OpusSampleRate, 16, OpusChannels);

        // Use the raw stream when it already matches; otherwise wrap in a resampler
        IWaveProvider source = pcmStream.WaveFormat.Equals(targetFormat)
            ? (IWaveProvider)pcmStream
            : new MediaFoundationResampler(pcmStream, targetFormat);
        MediaFoundationResampler? resampler = source as MediaFoundationResampler;

        try
        {
            await SetSpeakingAsync(true).ConfigureAwait(false);

            var buffer = new byte[PcmFrameBytes];
            int bytesRead;
            while (!cancellationToken.IsCancellationRequested &&
                   (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Zero-pad the last (potentially partial) frame so Opus always gets PcmFrameBytes
                if (bytesRead < buffer.Length)
                    Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);

                await SendAudioAsync(buffer).ConfigureAwait(false);

                // Pace delivery: one 20 ms frame every 20 ms to avoid flooding the UDP socket
                await Task.Delay(18, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* caller cancelled — normal exit */ }
        finally
        {
            await SetSpeakingAsync(false).ConfigureAwait(false);
            resampler?.Dispose();
        }
    }

    /// <summary>
    /// Starts capturing audio from the microphone and raises the Discord Speaking gate (op 5).
    /// </summary>
    public void StartCapture()
    {
        if (_waveIn != null && !_disposed)
        {
            _waveIn.StartRecording();
            TrackSpeakingTask(SetSpeakingAsync(true));
        }
    }

    /// <summary>
    /// Stops capturing audio from the microphone and lowers the Discord Speaking gate (op 5).
    /// </summary>
    public void StopCapture()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            TrackSpeakingTask(SetSpeakingAsync(false));
        }
    }

    /// <summary>
    /// Plays audio data.
    /// </summary>
    /// <param name="audioData">The audio data to play.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PlayAudioAsync(byte[] audioData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_waveProvider == null)
            return Task.CompletedTask;

        // Decode Opus data to PCM
        var pcmData = DecodeAudio(audioData);

        // Add to playback buffer
        _waveProvider.AddSamples(pcmData, 0, pcmData.Length);

        if (!IsPlaying)
        {
            _waveOut?.Play();
            IsPlaying = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Plays already-decoded 16-bit signed mono PCM data at 48 kHz through the local speaker.
    /// Called internally by the receive loop after Opus decoding; also available for external use.
    /// </summary>
    /// <param name="pcmData">Raw PCM bytes (16-bit signed LE, mono, 48 kHz).</param>
    public Task PlayAudioFromPcmAsync(byte[] pcmData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_waveProvider == null || pcmData.Length == 0)
            return Task.CompletedTask;

        _waveProvider.AddSamples(pcmData, 0, pcmData.Length);

        if (!IsPlaying)
        {
            _waveOut?.Play();
            IsPlaying = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Encodes and sends PCM audio to the voice channel via UDP.
    /// Input must be 16-bit signed mono PCM at 48 kHz (matching the capture <see cref="NAudio.Wave.WaveFormat"/>).
    /// Internally the method accumulates samples across calls until a complete 20 ms Opus frame
    /// (<see cref="PcmFrameBytes"/> bytes) is available, then encodes, wraps in an RTP header,
    /// applies transport encryption and optionally DAVE E2EE, and transmits the packet via UDP.
    /// </summary>
    /// <param name="audioData">Raw 16-bit signed PCM bytes to transmit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken = default)
    {
        if (_udpClient == null || _disposed)
            return;

        // Buffer incoming PCM and flush complete 20 ms frames as they become available
        _pendingPcm.AddRange(audioData);

        while (_pendingPcm.Count >= PcmFrameBytes)
        {
            // Dequeue exactly one 20 ms frame from the head of the accumulation buffer
            var frameBytes = _pendingPcm.GetRange(0, PcmFrameBytes).ToArray();
            _pendingPcm.RemoveRange(0, PcmFrameBytes);

            // Opus-encode the 20 ms PCM frame to a compact variable-length packet
            var opusPacket = EncodeFrame(frameBytes);
            if (opusPacket.Length == 0) continue;

            // Build the 12-byte RTP header
            var rtpHeader = BuildRtpHeader();

            // Apply transport encryption
            byte[] encryptedPayload;
            var dave = _dave;
            if (dave?.IsActive == true)
            {
                // DAVE E2EE encryption
                try
                {
                    encryptedPayload = dave.EncryptFrame(opusPacket, rtpHeader);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DAVE encryption failed, falling back to standard encryption for channel {ChannelId}", _channel.Id);
                    DaveError?.Invoke(ex);
                    if (_secretKey != null)
                    {
                        encryptedPayload = ApplyTransportEncryption(opusPacket, rtpHeader);
                    }
                    else
                    {
                        encryptedPayload = opusPacket;
                    }
                }
            }
            else if (_secretKey != null)
            {
                // Standard transport encryption
                encryptedPayload = ApplyTransportEncryption(opusPacket, rtpHeader);
            }
            else
            {
                // No transport encryption yet (before Session Description)
                encryptedPayload = opusPacket;
            }

            // Wire format: [12-byte RTP header][encrypted payload]
            var packet = new byte[rtpHeader.Length + encryptedPayload.Length];
            rtpHeader.CopyTo(packet, 0);
            encryptedPayload.CopyTo(packet, rtpHeader.Length);

            try
            {
                await _udpClient.SendAsync(packet, packet.Length, _udpIp, _udpPort)
                    .ConfigureAwait(false);
                Interlocked.Exchange(ref _lastFrameSentTick, Environment.TickCount64);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send audio packet via UDP");
            }
        }
    }

    private void OnWaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_disposed)
            return;

        // Only forward the bytes that NAudio actually filled (e.BytesRecorded),
        // not the entire pre-allocated buffer which contains trailing zeroes.
        _ = SendAudioAsync(e.Buffer[..e.BytesRecorded]);
    }

    /// <summary>
    /// Opus-encodes exactly one 20 ms PCM frame (<see cref="PcmFrameBytes"/> bytes of 16-bit samples).
    /// Returns <see cref="Array.Empty{T}"/> when the encoder is unavailable or encoding fails.
    /// </summary>
    private byte[] EncodeFrame(byte[] pcmFrameBytes)
    {
        if (_opusEncoder is null || pcmFrameBytes.Length != PcmFrameBytes)
            return Array.Empty<byte>();

        var pcmSamples = _shortPool.Rent(OpusFrameSize * OpusChannels);
        try
        {
            Buffer.BlockCopy(pcmFrameBytes, 0, pcmSamples, 0, pcmFrameBytes.Length);

            var output = _bytePool.Rent(MaxOpusBytes);
            try
            {
                int encodedLength = _opusEncoder.Encode(pcmSamples.AsSpan(), OpusFrameSize, output.AsSpan(), output.Length);
                return encodedLength > 0 ? output[..encodedLength] : Array.Empty<byte>();
            }
            finally
            {
                _bytePool.Return(output);
            }
        }
        finally
        {
            _shortPool.Return(pcmSamples);
        }
    }

    /// <summary>
    /// Decodes an Opus-encoded packet to 16-bit mono PCM at 48 kHz.
    /// Returns the input unchanged when the decoder is unavailable.
    /// </summary>
    private byte[] DecodeAudio(byte[] opusData)
    {
        if (_opusDecoder is null || opusData.Length == 0)
            return opusData;

        var pcmSamples = _shortPool.Rent(5760 * OpusChannels);
        try
        {
            int decodedSamples = _opusDecoder.Decode(opusData.AsSpan(), pcmSamples.AsSpan(), 5760, false);

            var pcmBytes = new byte[decodedSamples * OpusChannels * sizeof(short)];
            Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);
            return pcmBytes;
        }
        finally
        {
            _shortPool.Return(pcmSamples);
        }
    }

    private void TrackSpeakingTask(Task task)
    {
        Interlocked.Exchange(ref _speakingTask, task);
    }

    /// <summary>
    /// Sends the Discord Speaking (op 5) payload to gate audio transmission.
    /// Raises or lowers the speaking indicator so that Discord shows the microphone
    /// animation and correctly routes the voice stream.  Call with
    /// <see langword="true"/> before sending the first audio frame and with
    /// <see langword="false"/> after the stream ends.
    /// </summary>
    /// <param name="speaking"><see langword="true"/> to raise; <see langword="false"/> to lower.</param>
    public async Task SetSpeakingAsync(bool speaking)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open || _disposed)
            return;

        // Skip redundant gateway messages
        if (_speaking == speaking)
            return;

        _speaking = speaking;

        using var ms = new MemoryStream(128);
        using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writer.WriteNumber("op", 5);
        writer.WriteStartObject("d");
        writer.WriteNumber("speaking", speaking ? 1 : 0);
        writer.WriteNumber("delay", 0);
        writer.WriteNumber("ssrc", (int)Ssrc);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        var buffer = ms.GetBuffer();
        var segment = new ArraySegment<byte>(buffer, 0, (int)ms.Length);
        await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true,
            _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
    }

    // ── RTP helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a 12-byte RTP fixed header (RFC 3550 §5.1) for the next outgoing audio packet.
    /// Sequence number (uint16) and timestamp (uint32, 48 kHz clock) advance on every call.
    /// </summary>
    private byte[] BuildRtpHeader()
    {
        lock (_rtpLock)
        {
            var header = _rtpHeaderBuffer;

            // Byte 0: V=2, P=0, X=0, CC=0
            header[0] = 0x80;
            // Byte 1: M=0, PT=120 (Opus payload type, RFC 7587)
            header[1] = 0x78;

            // Sequence number — big-endian uint16, wraps naturally
            header[2] = (byte)(_rtpSequence >> 8);
            header[3] = (byte)_rtpSequence;
            _rtpSequence++;

            // Timestamp — big-endian uint32; advances by OpusFrameSize samples per packet
            header[4] = (byte)(_rtpTimestamp >> 24);
            header[5] = (byte)(_rtpTimestamp >> 16);
            header[6] = (byte)(_rtpTimestamp >> 8);
            header[7] = (byte)_rtpTimestamp;
            _rtpTimestamp += OpusFrameSize;  // 960 = 20 ms at 48 kHz

            // SSRC — big-endian uint32 (assigned by the server in op 2 READY)
            var ssrc = Ssrc;
            header[8]  = (byte)(ssrc >> 24);
            header[9]  = (byte)(ssrc >> 16);
            header[10] = (byte)(ssrc >> 8);
            header[11] = (byte)ssrc;

            return header;
        }
    }

    /// <summary>
    /// Parses the fixed 12-byte RTP header from an inbound voice packet.
    /// Returns the sender SSRC (used for DAVE per-sender key derivation),
    /// the raw header bytes (passed as DAVE AAD), and the encrypted payload.
    /// </summary>
    /// <returns><see langword="true"/> when the packet contains a valid 12-byte header.</returns>
    private static bool TryParseRtpPacket(
        byte[] packet,
        out uint   ssrc,
        out byte[] rtpHeader,
        out byte[] payload)
    {
        const int RtpHeaderSize = 12;
        if (packet.Length < RtpHeaderSize)
        {
            ssrc      = 0;
            rtpHeader = Array.Empty<byte>();
            payload   = packet;
            return false;
        }

        rtpHeader = packet[..RtpHeaderSize];
        // SSRC is at bytes 8–11 in big-endian order (RFC 3550 §5.1)
        ssrc = ((uint)packet[8]  << 24)
             | ((uint)packet[9]  << 16)
             | ((uint)packet[10] <<  8)
             |  (uint)packet[11];
        payload = packet[RtpHeaderSize..];
        return true;
    }

    private async Task ReceiveLoopAsync()
    {
        if (_webSocket == null || _cts == null)
            return;

        var buffer  = new byte[8192];  // large enough for max encrypted DAVE voice packet
        var segment = new ArraySegment<byte>(buffer);

        try
        {
            while (!_cts.Token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(segment, _cts.Token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using var ms = new MemoryStream();
                    ms.Write(buffer, 0, result.Count);
                    while (!result.EndOfMessage)
                    {
                        result = await _webSocket.ReceiveAsync(segment, _cts.Token).ConfigureAwait(false);
                        ms.Write(buffer, 0, result.Count);
                    }
                    var json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    await HandleJsonMessageAsync(json).ConfigureAwait(false);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    using var ms = new MemoryStream();
                    ms.Write(buffer, 0, result.Count);
                    while (!result.EndOfMessage)
                    {
                        result = await _webSocket.ReceiveAsync(segment, _cts.Token).ConfigureAwait(false);
                        ms.Write(buffer, 0, result.Count);
                    }
                    await HandleBinaryDaveMessageAsync(ms.ToArray()).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice receive loop failed and the connection will be reset for channel {ChannelId}", _channel.Id);
            State = VoiceConnectionState.Disconnected;
            _onConnectionFailed?.Invoke(_channel.Id);
        }
    }

    private async Task HandleJsonMessageAsync(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("op", out var opProp))
            {
                var opCode = opProp.GetInt32();
                switch (opCode)
                {
                    case 2: // READY — capture SSRC, IP, port, and modes
                        if (root.TryGetProperty("d", out var readyData))
                        {
                            if (readyData.TryGetProperty("ssrc", out var ssrcProp))
                                Ssrc = (uint)ssrcProp.GetInt64();
                            
                            if (readyData.TryGetProperty("ip", out var ipProp))
                                _udpIp = ipProp.GetString();
                            
                            if (readyData.TryGetProperty("port", out var portProp))
                                _udpPort = portProp.GetInt32();
                            
                            if (readyData.TryGetProperty("modes", out var modesProp) && modesProp.ValueKind == JsonValueKind.Array)
                            {
                                // Select the best available encryption mode
                                var availableModes = new List<string>();
                                foreach (var mode in modesProp.EnumerateArray())
                                    availableModes.Add(mode.GetString()!);
                                
                                SelectEncryptionMode(availableModes);
                            }
                            
                            _logger.LogInformation("Voice READY received: IP={Ip}, Port={Port}, SSRC={Ssrc}", _udpIp, _udpPort, Ssrc);

                            // Initialize DAVE if enabled (applies to all channel types)
                            if (_daveEnabled)
                            {
                                _dave?.Dispose();
                                _dave = new DAVE.DAVEProtocol(_voiceUserId.ToString(), _logger);
                                _dave.LocalSsrc = Ssrc;
                                var davePrevState = State;
                                State = VoiceConnectionState.DaveNegotiating;
                                StateChanged?.Invoke(davePrevState, State);
                                _logger.LogInformation("DAVE E2EE protocol initialized");
                            }

                            // Initiate IP discovery and Select Protocol
                            await PerformIpDiscoveryAndSelectProtocolAsync().ConfigureAwait(false);
                        }
                        break;
                    case 4: // SESSION DESCRIPTION — contains secret key for transport encryption
                        if (root.TryGetProperty("d", out var sessionData))
                        {
                            if (sessionData.TryGetProperty("mode", out var modeProp))
                            {
                                Enum.TryParse<VoiceEncryptionMode>((modeProp.GetString() ?? "").Replace("_", ""), true, out var mode);
                                _encryptionMode = mode;
                            }
                            
                            if (sessionData.TryGetProperty("secret_key", out var keyProp))
                            {
                                _secretKey = new byte[keyProp.GetArrayLength()];
                                int i = 0;
                                foreach (var b in keyProp.EnumerateArray())
                                    _secretKey[i++] = (byte)b.GetInt32();
                            }
                            
                            _logger.LogInformation("Session Description received: Mode={Mode}, KeyLength={KeyLength}", _encryptionMode, _secretKey?.Length);
                        }
                        break;
                    case 8: // HELLO
                        if (root.TryGetProperty("d", out var data) &&
                            data.TryGetProperty("heartbeat_interval", out var intervalProp))
                        {
                            _heartbeatInterval = intervalProp.GetInt32();
                            _logger.LogDebug("Voice heartbeat interval updated to {HeartbeatIntervalMs}ms for channel {ChannelId}", _heartbeatInterval, _channel.Id);
                        }
                        break;
                    case 9: // HEARTBEAT ACK
                        break;
                    case 7: // RESUMED
                        _logger.LogInformation("Voice connection resumed for channel {ChannelId}", _channel.Id);
                        // Re-initialize DAVE if enabled (server won't send READY again on resume)
                        if (_daveEnabled && _dave == null)
                        {
                            _dave = new DAVE.DAVEProtocol(_voiceUserId.ToString(), _logger);
                            _dave.LocalSsrc = Ssrc;
                        }
                        var resumePrevState = State;
                        State = VoiceConnectionState.Connected;
                        StateChanged?.Invoke(resumePrevState, State);
                        break;
                    case 11: // CLIENTS CONNECT
                        if (root.TryGetProperty("d", out var clientsData) &&
                            clientsData.TryGetProperty("user_id", out var connUserId))
                        {
                            var userId = connUserId.GetUInt64();
                            _logger.LogDebug("Voice user connected: {UserId}", userId);
                            UserVoiceStateChanged?.Invoke(userId, true);
                        }
                        break;
                    case 13: // CLIENT DISCONNECT
                        if (root.TryGetProperty("d", out var disconnectData) &&
                            disconnectData.TryGetProperty("user_id", out var disconnUserId))
                        {
                            var userId = disconnUserId.GetUInt64();
                            _logger.LogDebug("Voice user disconnected: {UserId}", userId);
                            UserVoiceStateChanged?.Invoke(userId, false);
                        }
                        break;
                    // DAVE E2EE JSON opcodes (21–24, 31 are JSON; 25–30 are binary)
                    case 21: case 22: case 23: case 24: case 31:
                        var dave = _dave;
                        if (dave != null)
                        {
                            try
                            {
                                await dave.HandleJsonMessageAsync(opCode, root.GetProperty("d"), _webSocket, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "DAVE JSON opcode {OpCode} handling failed for channel {ChannelId}", opCode, _channel.Id);
                                DaveError?.Invoke(ex);
                            }
                        }
                        break;
                    default:
                        _logger.LogDebug("Received unknown opcode {OpCode}", opCode);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse a voice control payload for channel {ChannelId}", _channel.Id);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task HandleBinaryDaveMessageAsync(byte[] msg)
    {
        // Server→client binary format: [2-byte big-endian seq][1-byte opcode][payload]
        if (msg.Length < 3) return;
        var seq = (ushort)((msg[0] << 8) | msg[1]);
        var opcode = msg[2];
        var payload = msg.Length > 3 ? msg[3..] : Array.Empty<byte>();
        _seqAck = seq;

        var dave = _dave;
        if (dave != null)
        {
            try
            {
                await dave.HandleBinaryMessageAsync(opcode, payload, _webSocket, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);

                if (opcode == 29 && dave.IsActive && State != VoiceConnectionState.DaveEncrypted)
                {
                    var prev = State;
                    State = VoiceConnectionState.DaveEncrypted;
                    StateChanged?.Invoke(prev, State);
                    DaveEncryptionActivated?.Invoke();
                    DaveEpochAdvanced?.Invoke(dave.EpochNumber);
                }
                else if (opcode == 29 && dave.IsActive)
                {
                    // Already DaveEncrypted — just fire the epoch event
                    DaveEpochAdvanced?.Invoke(dave.EpochNumber);
                }
                else if (opcode == 30 && dave.IsActive && State != VoiceConnectionState.DaveEncrypted)
                {
                    var prev = State;
                    State = VoiceConnectionState.DaveEncrypted;
                    StateChanged?.Invoke(prev, State);
                    DaveEncryptionActivated?.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DAVE binary opcode {OpCode} handling failed for channel {ChannelId}", opcode, _channel.Id);
                DaveError?.Invoke(ex);
            }
        }
    }

    private async Task HeartbeatLoopAsync()
    {
        if (_cts == null)
            return;

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // Send heartbeat
                await SendHeartbeatAsync().ConfigureAwait(false);
                await Task.Delay(_heartbeatInterval, _cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
    }

    private async Task SendHeartbeatAsync()
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            return;

        try
        {
            // Send heartbeat op code 3 with current timestamp and seq_ack (V8+)
            using var ms = new MemoryStream(128);
            using var writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteNumber("op", 3);
            writer.WriteStartObject("d");
            writer.WriteNumber("t", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            writer.WriteNumber("seq_ack", _seqAck ?? 0);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            var buf = ms.GetBuffer();
            var segment = new ArraySegment<byte>(buf, 0, (int)ms.Length);

            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send voice heartbeat for channel {ChannelId}", _channel.Id);
        }
    }

    private async Task KeepAliveLoopAsync()
    {
        if (_cts == null)
            return;

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(KeepAliveIntervalMs, _cts.Token).ConfigureAwait(false);

                if (_udpClient == null || _disposed)
                    continue;

                // Recent audio was sent — no need for a synthetic silence frame.
                if (Environment.TickCount64 - Interlocked.Read(ref _lastFrameSentTick) < SilenceThresholdMs)
                    continue;

                // Build a full RTP + encrypted silence packet so the server sees a
                // well-formed voice packet and keeps the SSRC / NAT mapping alive.
                var rtpHeader = BuildRtpHeader();
                byte[] encryptedPayload;

                var dave = _dave;
                if (dave?.IsActive == true)
                {
                    encryptedPayload = dave.EncryptFrame(SilenceFrame, rtpHeader);
                }
                else if (_secretKey != null)
                {
                    encryptedPayload = ApplyTransportEncryption(SilenceFrame, rtpHeader);
                }
                else
                {
                    encryptedPayload = SilenceFrame;
                }
                
                var packet = new byte[rtpHeader.Length + encryptedPayload.Length];
                rtpHeader.CopyTo(packet, 0);
                encryptedPayload.CopyTo(packet, rtpHeader.Length);

                try
                {
                    await _udpClient.SendAsync(packet, packet.Length, _udpIp, _udpPort)
                        .ConfigureAwait(false);
                    Interlocked.Exchange(ref _lastFrameSentTick, Environment.TickCount64);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send keep-alive packet via UDP");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice keep-alive loop failed for channel {ChannelId}", _channel.Id);
        }
    }

    private void StopAudio()
    {
        if (_waveOut != null)
        {
            _waveOut.Stop();
            IsPlaying = false;
        }

        if (_waveIn != null)
        {
            _waveIn.StopRecording();
        }
        
        _pendingPcm.Clear();
    }

    /// <summary>
    /// Updates the voice state.
    /// </summary>
    /// <param name="evt">The voice state update event.</param>
    public void UpdateVoiceState(VoiceStateUpdateEvent evt)
    {
        // Handle voice state updates
    }

    /// <summary>
    /// Updates the voice server. (Connection is now initiated by <see cref="VoiceClient"/> via <c>ConnectAsync</c>.)
    /// </summary>
    /// <param name="evt">The voice server update event.</param>
    public void UpdateVoiceServer(VoiceServerUpdateEvent evt)
    {
        // Connection is driven by VoiceClient.OnVoiceServerUpdate — no action needed here.
    }

    // ── UDP IP Discovery ───────────────────────────────────────────────────────
    
    /// <summary>
    /// Performs UDP IP discovery and sends Select Protocol to establish the UDP connection.
    /// </summary>
    private async Task PerformIpDiscoveryAndSelectProtocolAsync()
    {
        var previousState = State;
        State = VoiceConnectionState.Discovering;
        StateChanged?.Invoke(previousState, State);
        
        try
        {
            // Create UDP client for IP discovery
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            
            // Send IP discovery packet (70 bytes of null)
            var discoveryPacket = new byte[70];
            // First 4 bytes are SSRC in big-endian
            var ssrcBytes = BitConverter.GetBytes(Ssrc);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ssrcBytes);
            Array.Copy(ssrcBytes, 0, discoveryPacket, 0, 4);
            
            await _udpClient.SendAsync(discoveryPacket, discoveryPacket.Length, _udpIp!, _udpPort)
                .ConfigureAwait(false);
            
            // Receive IP discovery response
            var response = await _udpClient.ReceiveAsync().ConfigureAwait(false);
            var responseBuffer = response.Buffer;
            
            // Parse response: [type(1)][ip_len(2)][ip(var)][port(2)][remaining(padding)]
            var ipLength = BitConverter.ToUInt16(responseBuffer, 2);
            if (BitConverter.IsLittleEndian)
                ipLength = (ushort)((ipLength >> 8) | (ipLength << 8));
            
            var discoveredIp = System.Text.Encoding.UTF8.GetString(responseBuffer, 4, ipLength);
            var discoveredPort = BitConverter.ToUInt16(responseBuffer, 4 + ipLength);
            if (BitConverter.IsLittleEndian)
                discoveredPort = (ushort)((discoveredPort >> 8) | (discoveredPort << 8));
            
            _logger.LogInformation("IP Discovery complete: External IP={Ip}, Port={Port}", discoveredIp, discoveredPort);
            
            // Send Select Protocol (op 1)
            await SendSelectProtocolAsync(discoveredIp, discoveredPort).ConfigureAwait(false);
            
            // Start UDP receive loop
            _udpReceiveTask = Task.Run(UdpReceiveLoopAsync, _cts!.Token);
            
            // Update state
            previousState = State;
            State = VoiceConnectionState.Connected;
            StateChanged?.Invoke(previousState, State);
            UdpConnected?.Invoke(discoveredIp, discoveredPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP discovery failed for channel {ChannelId}", _channel.Id);
            _cts?.Cancel();
            var failPrevState = State;
            State = VoiceConnectionState.Disconnected;
            StateChanged?.Invoke(failPrevState, State);
            _onConnectionFailed?.Invoke(_channel.Id);
        }
    }
    
    /// <summary>
    /// Sends Select Protocol (op 1) to negotiate the UDP connection and encryption mode.
    /// </summary>
    private async Task SendSelectProtocolAsync(string ip, int port)
    {
        var modeString = _encryptionMode.ToString().ToLower().Replace("_", "");
        
        using var ms = new MemoryStream(256);
        using var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writer.WriteNumber("op", 1);
        writer.WriteStartObject("d");
        writer.WriteString("protocol", "udp");
        writer.WriteStartObject("data");
        writer.WriteString("address", ip);
        writer.WriteNumber("port", port);
        writer.WriteString("mode", modeString);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        var buffer = ms.GetBuffer();
        var segment = new ArraySegment<byte>(buffer, 0, (int)ms.Length);
        await _webSocket!.SendAsync(segment, WebSocketMessageType.Text, true, _cts!.Token).ConfigureAwait(false);
        
        _logger.LogInformation("Select Protocol sent: Mode={Mode}, Address={Address}:{Port}", modeString, ip, port);
    }
    
    /// <summary>
    /// Selects the best available encryption mode based on preferences and server support.
    /// </summary>
    private static readonly Dictionary<string, VoiceEncryptionMode> ModeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aead_aes256_gcm_rtpsize"]               = VoiceEncryptionMode.AeadAes256GcmRtpSize,
        ["aead_xchacha20_poly1305_rtpsize"]        = VoiceEncryptionMode.AeadXChaCha20Poly1305RtpSize,
        ["xsalsa20_poly1305_lite_rtpsize"]         = VoiceEncryptionMode.XSalsa20Poly1305LiteRtpSize,
        ["xsalsa20_poly1305_suffix"]               = VoiceEncryptionMode.XSalsa20Poly1305Suffix,
        ["xsalsa20_poly1305"]                      = VoiceEncryptionMode.XSalsa20Poly1305,
    };

    private void SelectEncryptionMode(List<string> availableModes)
    {
        var preferredOrder = new[]
        {
            "aead_aes256_gcm_rtpsize",
            "aead_xchacha20_poly1305_rtpsize",
            "xsalsa20_poly1305_lite_rtpsize",
            "xsalsa20_poly1305_suffix",
            "xsalsa20_poly1305"
        };

        // Try to match preferred mode first
        var preferredString = _options.PreferredEncryptionMode.ToString();
        var preferredLookup = ModeMap.FirstOrDefault(kvp => kvp.Value == _options.PreferredEncryptionMode).Key;
        if (preferredLookup != null && availableModes.Contains(preferredLookup))
        {
            _encryptionMode = _options.PreferredEncryptionMode;
            return;
        }

        // Fall back to first available in preferred order
        foreach (var mode in preferredOrder)
        {
            if (availableModes.Contains(mode) && ModeMap.TryGetValue(mode, out var mapped))
            {
                _encryptionMode = mapped;
                return;
            }
        }

        // Default to first available
        if (availableModes.Count > 0 && ModeMap.TryGetValue(availableModes[0], out var fallback))
        {
            _encryptionMode = fallback;
        }
    }
    
    // ── UDP Receive Loop ───────────────────────────────────────────────────────
    
    /// <summary>
    /// Receives audio packets from the UDP socket and processes them.
    /// </summary>
    private async Task UdpReceiveLoopAsync()
    {
        if (_udpClient == null || _cts == null)
            return;
        
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync().ConfigureAwait(false);
                var packet = result.Buffer;
                
                // Skip IP discovery responses (handled in PerformIpDiscoveryAndSelectProtocolAsync)
                if (packet.Length == 74)
                    continue;
                
                // Parse RTP header and decrypt
                if (TryParseRtpPacket(packet, out var ssrc, out var rtpHeader, out var encryptedPayload))
                {
                    _seqAck = (rtpHeader[2] << 8) | rtpHeader[3];
                    byte[] opusData = encryptedPayload;

                    // Try DAVE E2EE decryption first
                    var dave = _dave;
                    if (dave?.IsActive == true)
                    {
                        try
                        {
                            opusData = dave.DecryptFrame(encryptedPayload, ssrc, rtpHeader);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "DAVE decryption failed for SSRC {Ssrc} in UDP receive loop, dropping packet for channel {ChannelId}", ssrc, _channel.Id);
                            DaveError?.Invoke(ex);
                            continue;
                        }
                    }
                    else if (_secretKey != null)
                    {
                        opusData = RemoveTransportEncryption(encryptedPayload, rtpHeader);
                    }

                    var pcm = DecodeAudio(opusData);

                    // Fire the receive event before feeding audio to local playback
                    VoicePacketReceived?.Invoke(ssrc, pcm);

                    await PlayAudioFromPcmAsync(pcm).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP receive loop failed for channel {ChannelId}", _channel.Id);
        }
    }
    
    // ── Transport Encryption (AEAD modes) ───────────────────────────────────────
    
    /// <summary>
    /// Applies transport encryption to an Opus packet using AEAD encryption mode.
    /// Supports aead_aes256_gcm_rtpsize and aead_xchacha20_poly1305_rtpsize modes.
    /// </summary>
    private byte[] ApplyTransportEncryption(byte[] opusPacket, byte[] rtpHeader)
    {
        if (_secretKey == null || _secretKey.Length == 0)
            return opusPacket;
        
        try
        {
            switch (_encryptionMode)
            {
                case VoiceEncryptionMode.AeadAes256GcmRtpSize:
                    return ApplyAeadAes256Gcm(opusPacket, rtpHeader);
                
                case VoiceEncryptionMode.AeadXChaCha20Poly1305RtpSize:
                    return ApplyXChaCha20Poly1305(opusPacket, rtpHeader);
                
                case VoiceEncryptionMode.XSalsa20Poly1305:
                case VoiceEncryptionMode.XSalsa20Poly1305Suffix:
                case VoiceEncryptionMode.XSalsa20Poly1305LiteRtpSize:
                    // XSalsa20-Poly1305 modes are deprecated, use AES-GCM as fallback
                    _logger.LogWarning("XSalsa20-Poly1305 mode is deprecated, using AES-GCM fallback");
                    return ApplyAeadAes256Gcm(opusPacket, rtpHeader);
                
                default:
                    _logger.LogWarning("Unknown encryption mode, sending unencrypted");
                    return opusPacket;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transport encryption failed, sending unencrypted");
            return opusPacket;
        }
    }
    
    /// <summary>
    /// Applies AEAD_AES256_GCM_RTPSIZE encryption.
    /// Nonce is 12 bytes: first 4 bytes are SSRC, remaining 8 bytes are packet counter.
    /// </summary>
    private byte[] ApplyAeadAes256Gcm(byte[] opusPacket, byte[] rtpHeader)
    {
        // Extract SSRC from RTP header (bytes 8-11)
        var ssrc = BitConverter.ToUInt32(new byte[] { rtpHeader[11], rtpHeader[10], rtpHeader[9], rtpHeader[8] }, 0);
        
        // Nonce: SSRC (4) || RTP sequence (2) || zero-pad (6) = 12 bytes
        // Per aead_aes256_gcm_rtpsize spec — the 16-bit sequence wraps at 65535
        // (~22 min of 20 ms audio), which is the accepted limitation of this mode.
        var nonce = new byte[12];
        Array.Copy(rtpHeader, 8, nonce, 0, 4); // SSRC
        Array.Copy(rtpHeader, 2, nonce, 4, 2); // RTP sequence number
        
        using var aes = new AesGcm(_secretKey!, 16);
        
        var ciphertext = new byte[opusPacket.Length];
        var tag = new byte[16];
        
        aes.Encrypt(nonce, opusPacket, ciphertext, tag, rtpHeader);
        
        // Return: ciphertext + tag
        var result = new byte[ciphertext.Length + tag.Length];
        Array.Copy(ciphertext, 0, result, 0, ciphertext.Length);
        Array.Copy(tag, 0, result, ciphertext.Length, tag.Length);
        
        return result;
    }
    
    /// <summary>
    /// Removes transport encryption from a packet.
    /// </summary>
    private byte[] RemoveTransportEncryption(byte[] encryptedPacket, byte[] rtpHeader)
    {
        if (_secretKey == null || _secretKey.Length == 0)
            return encryptedPacket;
        
        try
        {
            switch (_encryptionMode)
            {
                case VoiceEncryptionMode.AeadAes256GcmRtpSize:
                    return RemoveAeadAes256Gcm(encryptedPacket, rtpHeader);
                
                case VoiceEncryptionMode.AeadXChaCha20Poly1305RtpSize:
                    return RemoveXChaCha20Poly1305(encryptedPacket, rtpHeader);
                case VoiceEncryptionMode.XSalsa20Poly1305:
                case VoiceEncryptionMode.XSalsa20Poly1305Suffix:
                case VoiceEncryptionMode.XSalsa20Poly1305LiteRtpSize:
                    // Fallback to AES-GCM decryption
                    return RemoveAeadAes256Gcm(encryptedPacket, rtpHeader);
                
                default:
                    return encryptedPacket;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transport decryption failed, returning encrypted packet");
            return encryptedPacket;
        }
    }
    
    /// <summary>
    /// Removes AEAD_AES256_GCM_RTPSIZE encryption.
    /// </summary>
    private byte[] RemoveAeadAes256Gcm(byte[] encryptedPacket, byte[] rtpHeader)
    {
        // Assume format: ciphertext + tag (16 bytes)
        if (encryptedPacket.Length < 16)
            return encryptedPacket;
        
        var cipherLen = encryptedPacket.Length - 16;
        var ciphertext = encryptedPacket.AsSpan(0, cipherLen);
        var tag = encryptedPacket.AsSpan(cipherLen, 16);
        
        // Extract SSRC from RTP header
        var ssrc = BitConverter.ToUInt32(new byte[] { rtpHeader[11], rtpHeader[10], rtpHeader[9], rtpHeader[8] }, 0);
        
        // Build nonce same as encryption
        var nonce = new byte[12];
        Array.Copy(rtpHeader, 8, nonce, 0, 4);
        Array.Copy(rtpHeader, 2, nonce, 4, 2);
        
        using var aes = new AesGcm(_secretKey!, 16);
        
        var plaintext = new byte[cipherLen];
        aes.Decrypt(nonce, ciphertext, tag, plaintext, rtpHeader);
        
        return plaintext;
    }
    
    /// <summary>
    /// Applies XChaCha20-Poly1305 AEAD encryption using BouncyCastle's ChaCha20Poly1305
    /// with HChaCha20 key derivation for the 24-byte XChaCha20 nonce.
    /// Nonce is constructed the same as AES-GCM mode: SSRC(4) || RTP sequence(2) || zero-pad(6) = 12 bytes,
    /// then extended to 24 bytes for XChaCha20.
    /// </summary>
    private byte[] ApplyXChaCha20Poly1305(byte[] opusPacket, byte[] rtpHeader)
    {
        var xNonce = new byte[24];
        Array.Copy(rtpHeader, 8, xNonce, 12, 4); // SSRC at bytes 12-15
        Array.Copy(rtpHeader, 2, xNonce, 16, 2); // seq at bytes 16-17

        var subkey = HChaCha20(_secretKey!, xNonce.AsSpan(0, 16));

        var nonceIetf = new byte[12];
        Array.Copy(xNonce, 16, nonceIetf, 4, 8); // last 8 bytes -> offset 4

        var aead = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        aead.Init(true, new AeadParameters(new KeyParameter(subkey), 128, nonceIetf, rtpHeader));

        var output = new byte[aead.GetOutputSize(opusPacket.Length)];
        var len = aead.ProcessBytes(opusPacket, 0, opusPacket.Length, output, 0);
        aead.DoFinal(output, len);
        return output;
    }

    /// <summary>
    /// Removes XChaCha20-Poly1305 AEAD encryption.
    /// Input format: ciphertext + 16-byte tag.
    /// </summary>
    private byte[] RemoveXChaCha20Poly1305(byte[] encryptedPacket, byte[] rtpHeader)
    {
        if (encryptedPacket.Length < 16)
            return encryptedPacket;

        int cipherLen = encryptedPacket.Length - 16;
        var xNonce = new byte[24];
        Array.Copy(rtpHeader, 8, xNonce, 12, 4);
        Array.Copy(rtpHeader, 2, xNonce, 16, 2);

        var subkey = HChaCha20(_secretKey!, xNonce.AsSpan(0, 16));

        var nonceIetf = new byte[12];
        Array.Copy(xNonce, 16, nonceIetf, 4, 8);

        var aead = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();
        aead.Init(false, new AeadParameters(new KeyParameter(subkey), 128, nonceIetf, rtpHeader));

        var plaintext = new byte[aead.GetOutputSize(encryptedPacket.Length)];
        try
        {
            var len = aead.ProcessBytes(encryptedPacket, 0, encryptedPacket.Length, plaintext, 0);
            aead.DoFinal(plaintext, len);
        }
        catch (Org.BouncyCastle.Crypto.InvalidCipherTextException)
        {
            throw new CryptographicException("XChaCha20-Poly1305 authentication failed.");
        }

        return plaintext[..cipherLen];
    }

    /// <summary>
    /// HChaCha20 key derivation: applies the ChaCha20 core to derive a 32-byte subkey
    /// from a 32-byte key and a 16-byte nonce.
    /// </summary>
    private static byte[] HChaCha20(byte[] key, ReadOnlySpan<byte> nonce16)
    {
        uint[] state = new uint[16];
        state[0] = 0x61707865;
        state[1] = 0x3320646E;
        state[2] = 0x79622D32;
        state[3] = 0x6B206574;

        for (int i = 0; i < 8; i++)
            state[4 + i] = BitConverter.ToUInt32(key, i * 4);

        state[12] = BitConverter.ToUInt32(nonce16);
        state[13] = BitConverter.ToUInt32(nonce16.Slice(4));
        state[14] = BitConverter.ToUInt32(nonce16.Slice(8));
        state[15] = BitConverter.ToUInt32(nonce16.Slice(12));

        var working = (uint[])state.Clone();
        for (int i = 0; i < 10; i++)
        {
            QuarterRound(ref working[0], ref working[4], ref working[8], ref working[12]);
            QuarterRound(ref working[1], ref working[5], ref working[9], ref working[13]);
            QuarterRound(ref working[2], ref working[6], ref working[10], ref working[14]);
            QuarterRound(ref working[3], ref working[7], ref working[11], ref working[15]);
            QuarterRound(ref working[0], ref working[5], ref working[10], ref working[15]);
            QuarterRound(ref working[1], ref working[6], ref working[11], ref working[12]);
            QuarterRound(ref working[2], ref working[7], ref working[8], ref working[13]);
            QuarterRound(ref working[3], ref working[4], ref working[9], ref working[14]);
        }

        var result = new byte[32];
        Buffer.BlockCopy(BitConverter.GetBytes(working[0]), 0, result, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[1]), 0, result, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[2]), 0, result, 8, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[3]), 0, result, 12, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[12]), 0, result, 16, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[13]), 0, result, 20, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[14]), 0, result, 24, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(working[15]), 0, result, 28, 4);
        return result;
    }

    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d ^= a; d = (d << 16) | (d >> 16);
        c += d; b ^= c; b = (b << 12) | (b >> 20);
        a += b; d ^= a; d = (d << 8) | (d >> 24);
        c += d; b ^= c; b = (b << 7) | (b >> 25);
    }

    /// <summary>
    /// Disposes the voice connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnWaveInDataAvailable;
            _waveIn.Dispose();
        }
        _waveOut?.Dispose();

        _cts?.Cancel();
        _cts?.Dispose();

        _webSocket?.Dispose();
        _udpClient?.Close();
        _dave?.Dispose();
        _pendingPcm.Clear();
        
        if (_secretKey != null)
        {
            CryptographicOperations.ZeroMemory(_secretKey);
        }
    }
}