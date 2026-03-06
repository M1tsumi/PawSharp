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

    // DAVE E2EE protocol handler (null until DAVE is negotiated)
    private readonly DAVEProtocol _dave = new();

    // Audio processing - Opus codec integration planned for future release
    // TODO: Implement Opus encoding/decoding when Concentus API is finalized
    private readonly object _encoder = null; // Placeholder for OpusEncoder
    private readonly object _decoder = null; // Placeholder for OpusDecoder
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _waveProvider;

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
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

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
    }

    /// <summary>
    /// Connects to the voice channel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ConnectAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VoiceConnection));

        _cts = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();

        // WebSocket connection would be established here
        // This is a simplified implementation - full implementation would require
        // voice server negotiation, encryption setup, etc.

        _receiveTask = Task.Run(ReceiveLoopAsync, _cts.Token);
        _heartbeatTask = Task.Run(HeartbeatLoopAsync, _cts.Token);
    }

    /// <summary>
    /// Disconnects from the voice channel.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        if (_disposed)
            return;

        _cts?.Cancel();

        if (_webSocket != null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            _webSocket.Dispose();
            _webSocket = null;
        }

        StopAudio();

        await Task.WhenAll(
            _receiveTask ?? Task.CompletedTask,
            _heartbeatTask ?? Task.CompletedTask
        );

        _cts?.Dispose();
        _cts = null;
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
    /// Updates the voice server.
    /// </summary>
    /// <param name="evt">The voice server update event.</param>
    public void UpdateVoiceServer(VoiceServerUpdateEvent evt)
    {
        // Handle voice server updates
        // This would establish the actual WebSocket connection
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