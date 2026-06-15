# PawSharp.Voice

PawSharp.Voice provides comprehensive voice connectivity and audio transport for PawSharp bots.

It covers the core building blocks needed for real voice features: voice gateway/session handling, UDP audio transport, Opus codec, and Discord transport encryption.

## Why Use This Package

- **Complete Discord Voice Protocol v8 Implementation**: WebSocket control channel + UDP audio transport
- **Opus Codec**: Built-in Opus encoding/decoding via Concentus (pure .NET, no native dependencies)
- **Transport Encryption**: Support for AEAD_AES256_GCM and AEAD_XChaCha20_Poly1305 modes
- **Connection Lifecycle**: Automatic reconnection with exponential backoff
- **Developer Friendly**: Configuration options, state change events, and comprehensive logging
- **Audio I/O**: NAudio integration for microphone capture and speaker playback

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`
- `PawSharp.Gateway`
- `PawSharp.Core`

## Installation

```bash
dotnet add package PawSharp.Voice --version 1.1.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Voice;

// Enable voice support
var voice = client.UseVoice();

// Connect to a voice channel with default options
var connection = await voice.ConnectAsync(channel);

// Subscribe to connection state changes
connection.StateChanged += (oldState, newState) => 
{
    Console.WriteLine($"Voice state changed: {oldState} -> {newState}");
};

// Subscribe to UDP connection establishment
connection.UdpConnected += (ip, port) => 
{
    Console.WriteLine($"UDP connected: {ip}:{port}");
};

// Send audio (PCM bytes, 48kHz mono 16-bit)
await connection.SetSpeakingAsync(true);
await connection.SendAudioAsync(pcmBytes);
await connection.SetSpeakingAsync(false);

// Disconnect
await connection.DisconnectAsync();
```

## Configuration Options

```csharp
var options = new VoiceConnectionOptions
{
    // Preferred encryption mode (default: AEAD_AES256_GCM_RTPSIZE)
    PreferredEncryptionMode = VoiceEncryptionMode.AeadAes256GcmRtpSize,
    
    // Opus encoder bitrate in bps (default: 64000)
    OpusBitrate = 96000,
    
    // Auto-initialize audio I/O (default: true)
    AutoInitializeAudio = true
};

var connection = await voice.ConnectAsync(channel, options);
```

## Playing Audio Files

```csharp
// Play an audio file (supports WAV, MP3, AIFF, etc.)
await connection.PlayAsync("path/to/audio.mp3");

// Play with cancellation support
var cts = new CancellationTokenSource();
await connection.PlayAsync("path/to/audio.mp3", cts.Token);
```

## Microphone Capture

```csharp
// Start capturing from microphone
connection.StartCapture();

// Stop capturing
connection.StopCapture();
```

## Receiving Audio

```csharp
// Subscribe to incoming voice packets (decoded PCM)
connection.VoicePacketReceived += (ssrc, pcmData) => 
{
    // Process incoming audio (e.g., speech-to-text)
    Console.WriteLine($"Received audio from SSRC {ssrc}: {pcmData.Length} bytes");
};
```

## Connection States

The voice connection goes through these states:

- **Disconnected**: No active connection
- **Connecting**: WebSocket connection in progress
- **Discovering**: UDP IP discovery in progress
- **Connected**: WebSocket and UDP are connected, voice session is active
- **DaveNegotiating**: DAVE E2EE key exchange in progress
- **DaveEncrypted**: DAVE E2EE encryption is active
- **Disconnecting**: Graceful disconnect in progress

## Resume Support

```csharp
// Resume a dropped connection
if (connection.State == VoiceConnectionState.Disconnected)
{
    await connection.ResumeAsync();
}
```

## Encryption Modes

The following transport encryption modes are supported:

- `AeadAes256GcmRtpSize`: AEAD_AES256_GCM (RTP size) - **Recommended**
- `AeadXChaCha20Poly1305RtpSize`: AEAD_XChaCha20_Poly1305 (RTP size)
- `XSalsa20Poly1305LiteRtpSize`: XSalsa20-Poly1305-lite (RTP size) - Deprecated
- `XSalsa20Poly1305Suffix`: XSalsa20-Poly1305 (suffix) - Deprecated
- `XSalsa20Poly1305`: XSalsa20-Poly1305 - Deprecated

**Note**: XSalsa20-Poly1305 modes are deprecated by Discord. Use AEAD modes for new implementations.

## Advanced Usage

### Custom Audio Processing

```csharp
// Receive raw PCM for custom processing
connection.VoicePacketReceived += (ssrc, pcmData) => 
{
    // Send to speech recognition, audio analysis, etc.
    ProcessAudio(ssrc, pcmData);
};
```

### Manual PCM Sending

```csharp
// Send pre-encoded PCM data (48kHz mono 16-bit)
var pcmBytes = GetPcmFromSource();
await connection.SendAudioAsync(pcmBytes);
```

### Disable Audio I/O

```csharp
// For headless servers without audio hardware
var options = new VoiceConnectionOptions
{
    AutoInitializeAudio = false
};
var connection = await voice.ConnectAsync(channel, options);
```

## Protocol Implementation Details

PawSharp.Voice implements the Discord Voice Protocol v8:

1. **WebSocket Control Channel**: Handles opcodes 0-20 for session management
2. **UDP Audio Transport**: Sends/receives audio packets via UDP
3. **IP Discovery**: Automatic external IP detection via UDP
4. **Protocol Selection**: Negotiates encryption mode with server
5. **Session Description**: Receives transport encryption keys
6. **Heartbeating**: Maintains connection with periodic heartbeats (V8 includes seq_ack)
7. **Keep-Alive**: Sends silence frames to prevent NAT timeout

### Transport Encryption

The implementation supports AEAD encryption modes as specified in the Discord Voice Protocol v8:

- **AEAD_AES256_GCM_RTPSIZE**: AES-256-GCM with RTP-sized nonce
- **AEAD_XChaCha20_Poly1305_RTPSIZE**: XChaCha20-Poly1305 with RTP-sized nonce

**Note**: XChaCha20-Poly1305 is implemented using pure .NET cryptography primitives. AES-GCM is used as a fallback when available.

## DAVE E2EE Support

PawSharp.Voice includes a full MLS (RFC 9420) implementation for DAVE E2EE. The crypto stack uses X25519, Ed25519, AES-128-GCM, and HKDF-SHA256 — all implemented in pure .NET. No external native libraries required.

## Typical Use Cases

- **Music Bots**: Playback and music queue management
- **Voice Alerts**: Voice-based notifications and announcements
- **Speech Recognition**: Bots that process incoming voice audio
- **Voice Moderation**: Audio analysis and moderation tools
- **Voice Automations**: Custom voice-based features

## Related Packages

- `PawSharp.Client`: High-level client integration
- `PawSharp.Gateway`: Real-time transport dependencies
- `PawSharp.Core`: Shared models and enums

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Discord Voice Documentation: [https://docs.discord.com/developers/topics/voice-connections](https://docs.discord.com/developers/topics/voice-connections)
- DAVE Protocol Whitepaper: [https://daveprotocol.com](https://daveprotocol.com)
- libdave: [https://github.com/discord/libdave](https://github.com/discord/libdave)

## License

MIT. See [../../LICENSE](../../LICENSE).
