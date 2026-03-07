#nullable enable
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concentus.Structs;
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
    private readonly Action<ulong>? _onConnectionFailed;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _heartbeatTask;
    private bool _disposed;
    private int _heartbeatInterval = 5000; // Default 5 seconds, updated from HELLO

    // Stored handshake parameters for reconnects
    private string? _endpoint;
    private ulong _voiceGuildId;
    private ulong _voiceUserId;
    private string? _sessionId;
    private string? _token;

    // DAVE E2EE protocol handler (null until DAVE is negotiated)
    private readonly DAVEProtocol _dave = new();

    // Audio processing - Opus codec integration planned for future release
    // TODO: Implement Opus encoding/decoding when Concentus API is finalized
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
    /// Initializes a new instance of the <see cref="VoiceConnection"/> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="channel">The voice channel.</param>
    /// <param name="onConnectionFailed">Callback for connection failures.</param>
    public VoiceConnection(DiscordClient discordClient, Channel channel, Action<ulong>? onConnectionFailed = null)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _onConnectionFailed = onConnectionFailed;

        // Initialize audio components
        InitializeAudio();
    }

    private void InitializeAudio()
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

        _receiveTask = Task.Run(ReceiveLoopAsync, _cts.Token);
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);

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
            catch { /* ignore close errors on disconnect */ }
            _webSocket.Dispose();
            _webSocket = null;
        }

        StopAudio();
        State = VoiceConnectionState.Disconnected;

        await Task.WhenAll(
            _receiveTask ?? Task.CompletedTask,
            _heartbeatTask ?? Task.CompletedTask
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
    /// Starts capturing audio from the microphone.
    /// </summary>
    public void StartCapture()
    {
        if (_waveIn != null && !_disposed)
        {
            _waveIn.StartRecording();
        }
    }

    /// <summary>
    /// Stops capturing audio from the microphone.
    /// </summary>
    public void StopCapture()
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
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
    /// Sends audio data to the voice channel.
    /// </summary>
    /// <param name="audioData">The PCM audio data to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAudioAsync(byte[] audioData)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open || _disposed)
            return;

        // Encode PCM to Opus
        var opusData = EncodeAudio(audioData);

        // Apply DAVE encryption if the protocol is active
        var payload = _dave.EncryptFrame(opusData);

        // Send via WebSocket (simplified - would need proper voice packet structure)
        await _webSocket.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None);
    }

    private void OnWaveInDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_disposed)
            return;

        // Send captured audio
        _ = SendAudioAsync(e.Buffer);
    }

    private byte[] EncodeAudio(byte[] pcmData)
    {
        // TODO: Implement Opus encoding with Concentus library
        // For now, return PCM data as-is (audio framework ready for codec integration)
        return pcmData;
    }

    private byte[] DecodeAudio(byte[] opusData)
    {
        // TODO: Implement Opus decoding with Concentus library
        // For now, return data as-is (audio framework ready for codec integration)
        return opusData;
    }

    private async Task ReceiveLoopAsync()
    {
        if (_webSocket == null || _cts == null)
            return;

        var buffer = new byte[4096];
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
                    // Handle incoming voice packets — decrypt with DAVE if active
                    var packet = new byte[result.Count];
                    Array.Copy(buffer, packet, result.Count);
                    // SSRC would be extracted from the RTP header in a full implementation
                    // Using 0 as placeholder SSRC for now
                    var decrypted = _dave.DecryptFrame(packet, ssrc: 0);
                    await PlayAudioAsync(decrypted);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Voice receive error: {ex.Message}");
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
                            Console.WriteLine($"Voice heartbeat interval set to: {_heartbeatInterval}ms");
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
            Console.WriteLine($"Error parsing voice JSON message: {ex.Message}");
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

            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending voice heartbeat: {ex.Message}");
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
        // Note: OpusEncoder and OpusDecoder don't implement IDisposable
    }
}