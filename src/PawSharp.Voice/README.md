# PawSharp.Voice

Professional-grade voice channel connectivity with automatic reconnection, dynamic heartbeat management, and full **Discord DAVE E2EE** (RFC 9420 MLS end-to-end encryption).

PawSharp.Voice provides complete encrypted voice channel support for Discord bots: automatic heartbeat interval detection, exponential backoff reconnection, per-epoch AES-128-GCM frame encryption, and a complete MLS group-state engine built entirely on .NET 8 BCL cryptography primitives.

## Features

- **DAVE E2EE** — full RFC 9420 MLS stack for end-to-end encrypted voice (see below)
- Voice channel connectivity with automatic negotiation
- Dynamic heartbeat interval detection from Discord's HELLO events
- Exponential backoff reconnection (1s to 30s max, 5 attempts)
- Audio capture and speaker playback with NAudio
- Voice state and server update handling
- Opus codec integration framework
- Real-time voice data transmission
- Thread-safe operations
- Connection resilience with automatic recovery

## ?? Installation

```bash
dotnet add package PawSharp.Voice --version 0.7.0-alpha.1
```

## ?? Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Voice;

// Create Discord client
var client = new DiscordClient(new PawSharpOptions { Token = "your-token" });

// Get voice client
var voice = client.UseVoice();

// Connect to voice channel
Channel voiceChannel = await client.Rest.GetChannelAsync(channelId);
var connection = await voice.ConnectAsync(voiceChannel);

// Start voice capture
connection.StartCapture();

// Bot now transmits microphone audio to the channel
// Received audio from other users can be played back

// Stop when done
connection.StopCapture();
await connection.DisconnectAsync();
```

## ?? Voice Connection Lifecycle

### 1. Connection Establishment

```csharp
// Join voice channel
var connection = await voice.ConnectAsync(voiceChannel);

// Connection automatically:
// - Sends voice state update to Discord
// - Receives voice server information
// - Establishes WebSocket connection
// - Negotiates voice protocol
// - Starts heartbeat with proper interval
```

### 2. Audio Transmission

```csharp
// Start microphone capture
connection.StartCapture();

// Audio is automatically:
// - Captured from microphone
// - Encoded (when Opus is implemented)
// - Sent via WebSocket
// - Received by other channel members
```

### 3. Audio Reception & Playback

```csharp
// Handle incoming voice data
connection.OnAudioReceived += async (audioData) =>
{
    // Decode and play received audio
    await connection.PlayAudioAsync(audioData);
};

// Start playback
connection.StartPlayback();
```

## ?? Advanced Configuration

### Voice Client Options

```csharp
var voiceOptions = new VoiceOptions
{
    AudioQuality = AudioQuality.High,
    EnableNoiseSuppression = true,
    EnableEchoCancellation = true,
    JitterBufferSize = 20, // milliseconds
    EncoderComplexity = 10 // Opus encoder setting
};

var voice = client.UseVoice(voiceOptions);
```

### Connection Monitoring

```csharp
// Monitor connection health
connection.OnConnectionLost += async () =>
{
    Console.WriteLine("Voice connection lost - automatic reconnection in progress");
};

connection.OnReconnected += async () =>
{
    Console.WriteLine("Voice connection restored");
};
```

## ?? Audio Processing

### Current Implementation

```csharp
// Audio capture (implemented)
connection.StartCapture();  // Begins microphone recording
connection.StopCapture();   // Stops microphone recording

// Audio playback (implemented)
await connection.PlayAudioAsync(pcmData);  // Plays PCM audio
connection.StopPlayback();  // Stops playback
```

### Opus Codec Integration (Framework Ready)

```csharp
// When Opus is fully integrated:
var opusData = await connection.EncodeAudioAsync(pcmData);
var pcmData = await connection.DecodeAudioAsync(opusData);
```

### Audio Quality Settings

- **Sample Rate**: 48kHz (CD quality)
- **Channels**: Mono (optimized for voice)
- **Frame Size**: 20ms (standard for VoIP)
- **Bitrate**: Variable (configurable)
- **Codec**: Opus ready (when implemented)

## ?? Automatic Reconnection

PawSharp.Voice includes intelligent reconnection logic:

```csharp
// Reconnection happens automatically on:
// - Network interruptions
// - Voice server changes
// - WebSocket connection drops
// - Discord service issues

// Exponential backoff: 1s ? 2s ? 4s ? 8s ? 16s ? 30s (max)
// Maximum 5 reconnection attempts
// Automatic cleanup on final failure
```

## ?? Voice Events

### Voice State Updates

```csharp
client.Gateway.Events.On<VoiceStateUpdateEvent>("VOICE_STATE_UPDATE", async evt =>
{
    if (evt.ChannelId.HasValue)
    {
        Console.WriteLine($"{evt.UserId} joined voice channel {evt.ChannelId}");
    }
    else
    {
        Console.WriteLine($"{evt.UserId} left voice channel");
    }
});
```

### Voice Server Updates

```csharp
client.Gateway.Events.On<VoiceServerUpdateEvent>("VOICE_SERVER_UPDATE", async evt =>
{
    // Voice server information updated
    // Connection automatically renegotiates
    Console.WriteLine($"Voice server updated for guild {evt.GuildId}");
});
```

## Architecture

```
PawSharp.Voice
+-- VoiceClient
|   +-- Connection management
|   +-- Reconnection logic
|   +-- State tracking
+-- VoiceConnection
|   +-- WebSocket communication
|   +-- Heartbeat management
|   +-- Audio capture/playback
|   +-- Protocol handling
+-- DAVE/
|   +-- DAVEProtocol       -- orchestrates MLS handshake
|   +-- DAVEEncryption     -- AES-128-GCM per-frame encryption
|   +-- DAVEKeyDerivation  -- epoch-secret -> sender-key HKDF
|   +-- MLS/
|       +-- Crypto/
|       |   +-- Curve25519   (RFC 7748 X25519)
|       |   +-- Ed25519      (RFC 8032 sign/verify)
|       |   +-- MlsHkdf      (RFC 9420 label functions)
|       |   +-- HpkeX25519   (RFC 9180 HPKE Base mode)
|       +-- Encoding/
|       |   +-- TlsReader    (zero-copy span reader)
|       |   +-- TlsWriter    (MemoryStream writer)
|       +-- Tree/
|       |   +-- TreeMath     (left-balanced tree indexes)
|       |   +-- TreeNode     (leaf/parent with HPKE keys)
|       |   +-- RatchetTree  (TreeKEM operations)
|       +-- Messages/
|       |   +-- Credential, LeafNode, KeyPackage
|       |   +-- GroupContext, Proposal, Welcome
|       +-- State/
|           +-- MLSKeySchedule  (RFC 9420 key schedule)
|           +-- MLSGroupState   (full group state engine)
+-- Audio Processing
|   +-- NAudio integration
|   +-- PCM handling
|   +-- Opus framework (ready)
+-- Events and State
    +-- Voice state updates
    +-- Server updates
    +-- Connection events
```

## DAVE E2EE Details

Discord's DAVE protocol encrypts voice frames using MLS group keys. PawSharp implements the full MLS stack from scratch:

**Ciphersuite:** `MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519`

| Layer | Implementation |
|-------|---------------|
| Key agreement | RFC 7748 X25519 (5x51-bit Montgomery ladder) |
| Signatures | RFC 8032 Ed25519 (twisted Edwards, GF(2^255-19)) |
| HPKE | RFC 9180 Base mode (DHKEM-X25519 + AES-128-GCM) |
| Tree | RFC 9420 TreeKEM ratchet tree |
| Key schedule | RFC 9420 s8 (joiner -> epoch -> exporter) |
| Frame encryption | AES-128-GCM with per-SSRC sender keys |

## ?? Error Handling

```csharp
try
{
    var connection = await voice.ConnectAsync(voiceChannel);
}
catch (VoiceConnectionException ex)
{
    Console.WriteLine($"Voice connection failed: {ex.Message}");
    // Automatic reconnection will be attempted
}
catch (AudioDeviceException ex)
{
    Console.WriteLine($"Audio device error: {ex.Message}");
    // Check microphone/speaker configuration
}
```

## ?? Dependencies

- **PawSharp.Client** - Discord client integration
- **PawSharp.Core** - Entity models
- **NAudio** - Cross-platform audio I/O
- **Concentus** - Opus codec (framework ready)
- **.NET 8.0** - Modern runtime

## ?? Related Packages

- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - Main Discord client
- **[PawSharp.Gateway](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Gateway)** - Gateway connectivity
- **[PawSharp.Commands](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Commands)** - Command framework

## ?? Best Practices

### Connection Management

```csharp
// Always dispose connections properly
using (var connection = await voice.ConnectAsync(channel))
{
    // Use connection
}
// Automatic cleanup
```

### Error Recovery

```csharp
// Let automatic reconnection handle most issues
connection.OnConnectionFailed += async () =>
{
    // Log for monitoring, but reconnection is automatic
    await LogAsync("Voice reconnection initiated");
};
```

### Audio Quality

```csharp
// Configure for voice communication
var options = new VoiceOptions
{
    SampleRate = 48000,
    Channels = 1, // Mono
    FrameSize = 20, // 20ms frames
    Complexity = 5 // Balance quality vs CPU
};
```

## ?? License

MIT License - see [LICENSE](../LICENSE) for details.
