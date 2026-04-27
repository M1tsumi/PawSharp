#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;
using PawSharp.Voice.DAVE;

namespace PawSharp.Voice;

/// <summary>Describes the lifecycle state of a <see cref="VoiceConnection"/>.</summary>
public enum VoiceConnectionState
{
    /// <summary>No active WebSocket connection.</summary>
    Disconnected,
    /// <summary>WebSocket connect is in progress.</summary>
    Connecting,
    /// <summary>WebSocket is open and the voice session is active.</summary>
    Connected,
    /// <summary>Graceful close is in progress.</summary>
    Disconnecting,
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
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _heartbeatTask;
    private Task? _keepAliveTask;
    private bool _disposed;
    private int _heartbeatInterval = 5000; // Default 5 seconds, updated from HELLO
    private long _lastFrameSentTick;       // TickCount64 of the last outgoing audio frame (for silence keep-alive)

    // Stored handshake parameters for reconnects
    private string? _endpoint;
    private ulong _voiceGuildId;
    private ulong _voiceUserId;
    private string? _sessionId;
    private string? _token;

    // DAVE E2EE protocol handler
    private readonly DAVEProtocol _dave = new();

    // ── Opus codec constants ─────────────────────────────────────────────────
    private const int OpusSampleRate = 48000;   // Hz
    private const int OpusChannels   = 1;        // mono
    private const int OpusFrameSize  = 960;      // samples — 20 ms at 48 kHz
    private const int PcmFrameBytes  = OpusFrameSize * OpusChannels * sizeof(short); // 1 920 bytes
    private const int MaxOpusBytes   = 4000;     // conservative max packet per RFC 6716

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

    // Outgoing PCM accumulation — holds partial frames until a full 20 ms chunk is ready
    private readonly List<byte> _pendingPcm = new();

    // Speaking gate — prevents redundant op-5 transmissions
    private bool _speaking;

    // NAudio I/O (microphone capture and speaker playback)
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _waveProvider;

    /// <summary>Gets the current connection lifecycle state.</summary>
    public VoiceConnectionState State { get; private set; } = VoiceConnectionState.Disconnected;

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
    public VoiceConnection(
        DiscordClient discordClient,
        Channel channel,
        Action<ulong>? onConnectionFailed = null,
        ILogger? logger = null)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _onConnectionFailed = onConnectionFailed;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        // Initialize audio components
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        // Opus codec is always available (pure managed, no audio hardware needed)
        _opusEncoder = OpusCodecFactory.CreateEncoder(OpusSampleRate, OpusChannels, OpusApplication.OPUS_APPLICATION_VOIP, null);
        _opusEncoder.Bitrate = 64000;
        _opusDecoder = OpusCodecFactory.CreateDecoder(OpusSampleRate, OpusChannels, null);

        try
        {
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
        catch (Exception)
        {
            // Audio hardware is unavailable (e.g. headless server). Audio I/O will
            // be skipped; voice packet send/receive still works.
            _waveIn?.Dispose();
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

        await ConnectInternalAsync();
    }

    /// <summary>
    /// Reconnects using the stored handshake parameters.
    /// </summary>
    internal async Task ReconnectAsync()
    {
        if (_endpoint is null || _sessionId is null || _token is null)
            throw new InvalidOperationException("Cannot reconnect: handshake parameters not stored. Call ConnectAsync first.");

        await ConnectInternalAsync();
    }

    private async Task ConnectInternalAsync()
    {
        if (_disposed)
            return;

        State = VoiceConnectionState.Connecting;

        // Cancel any existing tasks
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        // Strip port suffix — Discord sends "endpoint:80", WebSocket URI needs plain hostname
        var host = _endpoint!.Contains(':') ? _endpoint.Substring(0, _endpoint.LastIndexOf(':')) : _endpoint;
        var uri = new Uri($"wss://{host}?v=8");

        await _webSocket.ConnectAsync(uri, _cts.Token);
        State = VoiceConnectionState.Connected;
        _speaking = false;  // reset speaking gate on fresh connection

        _receiveTask   = Task.Run(ReceiveLoopAsync,   _cts.Token);
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);
        _keepAliveTask = Task.Run(KeepAliveLoopAsync, _cts.Token);

        // Send Opcode 0 IDENTIFY immediately after WebSocket upgrade
        await SendIdentifyAsync();
    }

    private async Task SendIdentifyAsync()
    {
        var payload = new
        {
            op = 0,
            d = new
            {
                server_id = _voiceGuildId.ToString(),
                user_id = _voiceUserId.ToString(),
                session_id = _sessionId,
                token = _token
            }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        await _webSocket!.SendAsync(buffer, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
    }

    /// <summary>
    /// Disconnects from the voice channel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        if (_disposed)
            return;

        State = VoiceConnectionState.Disconnecting;
        _cts?.Cancel();

        if (_webSocket != null &&
            (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebSocket close error during disconnect: {ex.Message}");
            }
            _webSocket.Dispose();
            _webSocket = null;
        }

        StopAudio();
        State = VoiceConnectionState.Disconnected;

        await Task.WhenAll(
            _receiveTask   ?? Task.CompletedTask,
            _heartbeatTask ?? Task.CompletedTask,
            _keepAliveTask ?? Task.CompletedTask
        );

        _cts?.Dispose();
        _cts = null;
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
    /// Stops the current audio playback. Alias for <see cref="StopPlayback"/> with an async signature
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
        await PlayAsync(reader, cancellationToken);
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
            await SetSpeakingAsync(true);

            var buffer = new byte[PcmFrameBytes];
            int bytesRead;
            while (!cancellationToken.IsCancellationRequested &&
                   (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Zero-pad the last (potentially partial) frame so Opus always gets PcmFrameBytes
                if (bytesRead < buffer.Length)
                    Array.Clear(buffer, bytesRead, buffer.Length - bytesRead);

                await SendAudioAsync(buffer);

                // Pace delivery: one 20 ms frame every 20 ms to avoid flooding the UDP socket
                await Task.Delay(18, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* caller cancelled — normal exit */ }
        finally
        {
            await SetSpeakingAsync(false);
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
            _ = SetSpeakingAsync(true);
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
            _ = SetSpeakingAsync(false);
        }
    }

    /// <summary>
    /// Plays audio data.
    /// </summary>
    /// <param name="audioData">The audio data to play.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PlayAudioAsync(byte[] audioData)
    {
        if (_waveProvider == null || _disposed)
            return;

        // Decode Opus data to PCM
        var pcmData = DecodeAudio(audioData);

        // Add to playback buffer
        _waveProvider.AddSamples(pcmData, 0, pcmData.Length);

        if (!IsPlaying)
        {
            _waveOut?.Play();
            IsPlaying = true;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Plays already-decoded 16-bit signed mono PCM data at 48 kHz through the local speaker.
    /// Called internally by the receive loop after Opus decoding; also available for external use.
    /// </summary>
    /// <param name="pcmData">Raw PCM bytes (16-bit signed LE, mono, 48 kHz).</param>
    public async Task PlayAudioFromPcmAsync(byte[] pcmData)
    {
        if (_waveProvider == null || _disposed || pcmData.Length == 0)
            return;

        _waveProvider.AddSamples(pcmData, 0, pcmData.Length);

        if (!IsPlaying)
        {
            _waveOut?.Play();
            IsPlaying = true;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Encodes and sends PCM audio to the voice channel.
    /// Input must be 16-bit signed mono PCM at 48 kHz (matching the capture <see cref="NAudio.Wave.WaveFormat"/>).
    /// Internally the method accumulates samples across calls until a complete 20 ms Opus frame
    /// (<see cref="PcmFrameBytes"/> bytes) is available, then encodes, wraps in an RTP header,
    /// applies DAVE AES-128-GCM encryption, and transmits the packet.
    /// </summary>
    /// <param name="audioData">Raw 16-bit signed PCM bytes to transmit.</param>
    public async Task SendAudioAsync(byte[] audioData)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open || _disposed)
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

            // Build the 12-byte RTP header; also used as AAD for the DAVE cipher
            var rtpHeader = BuildRtpHeader();

            // DAVE-encrypt the Opus payload, cryptographically binding it to the RTP header
            var encryptedPayload = _dave.EncryptFrame(opusPacket, rtpHeader);

            // Wire format: [12-byte RTP header][DAVE: nonce || ciphertext || auth-tag]
            var packet = new byte[rtpHeader.Length + encryptedPayload.Length];
            rtpHeader.CopyTo(packet, 0);
            encryptedPayload.CopyTo(packet, rtpHeader.Length);

            await _webSocket.SendAsync(packet, WebSocketMessageType.Binary, true,
                _cts?.Token ?? CancellationToken.None);
            Interlocked.Exchange(ref _lastFrameSentTick, Environment.TickCount64);
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

        // Reinterpret the 16-bit PCM byte buffer as a short[] sample array
        var pcmSamples = new short[OpusFrameSize * OpusChannels];
        Buffer.BlockCopy(pcmFrameBytes, 0, pcmSamples, 0, pcmFrameBytes.Length);

        var output = new byte[MaxOpusBytes];
        int encodedLength = _opusEncoder.Encode(pcmSamples.AsSpan(), OpusFrameSize, output.AsSpan(), output.Length);
        return encodedLength > 0 ? output[..encodedLength] : Array.Empty<byte>();
    }

    /// <summary>
    /// Decodes an Opus-encoded packet to 16-bit mono PCM at 48 kHz.
    /// Returns the input unchanged when the decoder is unavailable.
    /// </summary>
    private byte[] DecodeAudio(byte[] opusData)
    {
        if (_opusDecoder is null || opusData.Length == 0)
            return opusData;

        // Maximum decoded frame is 120 ms = 5 760 samples at 48 kHz
        var pcmSamples = new short[5760 * OpusChannels];
        int decodedSamples = _opusDecoder.Decode(opusData.AsSpan(), pcmSamples.AsSpan(), 5760, false);

        // Convert the decoded short[] samples back to a 16-bit PCM byte stream
        var pcmBytes = new byte[decodedSamples * OpusChannels * sizeof(short)];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);
        return pcmBytes;
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

        var payload = new
        {
            op = 5,
            d = new
            {
                speaking = speaking ? 1 : 0,
                delay    = 0,
                ssrc     = (int)_dave.LocalSsrc,
            },
        };
        var json  = JsonSerializer.Serialize(payload);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(bytes, WebSocketMessageType.Text, true,
            _cts?.Token ?? CancellationToken.None);
    }

    // ── RTP helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a 12-byte RTP fixed header (RFC 3550 §5.1) for the next outgoing audio packet.
    /// Sequence number (uint16) and timestamp (uint32, 48 kHz clock) advance on every call.
    /// </summary>
    private byte[] BuildRtpHeader()
    {
        var header = new byte[12];

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
        var ssrc = _dave.LocalSsrc;
        header[8]  = (byte)(ssrc >> 24);
        header[9]  = (byte)(ssrc >> 16);
        header[10] = (byte)(ssrc >> 8);
        header[11] = (byte)ssrc;

        return header;
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
                var result = await _webSocket.ReceiveAsync(segment, _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Handle JSON control messages
                    var json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleJsonMessageAsync(json);
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Handle incoming voice packets
                    var packet = new byte[result.Count];
                    Array.Copy(buffer, packet, result.Count);

                    // Parse the RTP header to recover the sender SSRC and raw header bytes.
                    // The header bytes are passed as AAD so DAVE authentication covers the
                    // RTP metadata (sequence, timestamp, SSRC) and not just the payload.
                    if (TryParseRtpPacket(packet, out var ssrc, out var rtpHeader, out var encryptedPayload))
                    {
                        var opusData = _dave.DecryptFrame(encryptedPayload, ssrc, rtpHeader);
                        var pcm = DecodeAudio(opusData);

                        // Fire the receive event before feeding audio to local playback so that
                        // subscribers (e.g. speech-to-text) can process the PCM independently.
                        VoicePacketReceived?.Invoke(ssrc, pcm);

                        await PlayAudioFromPcmAsync(pcm);
                    }
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
                    case 2: // READY — capture our SSRC for DAVE
                        if (root.TryGetProperty("d", out var readyData) &&
                            readyData.TryGetProperty("ssrc", out var ssrcProp))
                        {
                            _dave.LocalSsrc = (uint)ssrcProp.GetInt64();
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
                        // Handle heartbeat acknowledgment if needed
                        break;
                    // DAVE E2EE opcodes 21–31
                    case >= 21 and <= 31:
                        if (root.TryGetProperty("d", out var daveData))
                            await _dave.HandleOpcodeAsync(opCode, daveData, _webSocket, _cts?.Token ?? CancellationToken.None);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse a voice control payload for channel {ChannelId}", _channel.Id);
        }

        await Task.CompletedTask;
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
                await SendHeartbeatAsync();
                await Task.Delay(_heartbeatInterval, _cts.Token);
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
            // Send heartbeat op code 3 with current timestamp
            var heartbeatPayload = new
            {
                op = 3,
                d = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(heartbeatPayload);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, _cts?.Token ?? CancellationToken.None);
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

                if (_webSocket?.State != WebSocketState.Open || _disposed)
                    continue;

                // Recent audio was sent — no need for a synthetic silence frame.
                if (Environment.TickCount64 - Interlocked.Read(ref _lastFrameSentTick) < SilenceThresholdMs)
                    continue;

                // Build a full RTP + DAVE-encrypted silence packet so the server sees a
                // well-formed voice packet and keeps the SSRC / NAT mapping alive.
                var rtpHeader        = BuildRtpHeader();
                var encryptedPayload = _dave.EncryptFrame(SilenceFrame, rtpHeader);
                var packet           = new byte[rtpHeader.Length + encryptedPayload.Length];
                rtpHeader.CopyTo(packet, 0);
                encryptedPayload.CopyTo(packet, rtpHeader.Length);

                await _webSocket.SendAsync(packet, WebSocketMessageType.Binary, true,
                    _cts.Token).ConfigureAwait(false);
                Interlocked.Exchange(ref _lastFrameSentTick, Environment.TickCount64);
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

    /// <summary>
    /// Disposes the voice connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        _webSocket?.Dispose();
        _waveIn?.Dispose();
        _waveOut?.Dispose();
        _dave.Dispose();
        _pendingPcm.Clear();
    }
}