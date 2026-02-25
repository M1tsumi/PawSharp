# PawSharp.Voice

Professional-grade voice channel connectivity with automatic reconnection and dynamic heartbeat management.

PawSharp.Voice provides complete voice channel support for Discord bots, featuring automatic heartbeat interval detection, exponential backoff reconnection, and a robust audio processing framework ready for Opus codec integration.

## Features

- Voice channel connectivity with automatic negotiation
- Dynamic heartbeat interval detection from Discord's HELLO events
- Exponential backoff reconnection (1s to 30s max, 5 attempts)
- Audio capture and speaker playback with NAudio
- Voice state and server update handling
- Opus codec integration framework
- Real-time voice data transmission
- Thread-safe operations
- Connection resilience with automatic recovery

## 📦 Installation

```bash
dotnet add package PawSharp.Voice --version 0.6.0-alpha1
```

## 🚀 Quick Start

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

## 📋 Voice Connection Lifecycle

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

## 🔧 Advanced Configuration

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

## 🎵 Audio Processing

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

## 🔄 Automatic Reconnection

PawSharp.Voice includes intelligent reconnection logic:

```csharp
// Reconnection happens automatically on:
// - Network interruptions
// - Voice server changes
// - WebSocket connection drops
// - Discord service issues

// Exponential backoff: 1s → 2s → 4s → 8s → 16s → 30s (max)
// Maximum 5 reconnection attempts
// Automatic cleanup on final failure
```

## 📊 Voice Events

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

## 🏗️ Architecture

```
PawSharp.Voice
├── VoiceClient
│   ├── Connection management
│   ├── Reconnection logic
│   └── State tracking
├── VoiceConnection
│   ├── WebSocket communication
│   ├── Heartbeat management
│   ├── Audio capture/playback
│   └── Protocol handling
├── Audio Processing
│   ├── NAudio integration
│   ├── PCM handling
│   └── Opus framework (ready)
└── Events & State
    ├── Voice state updates
    ├── Server updates
    └── Connection events
```

## ⚠️ Error Handling

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

## 🔧 Dependencies

- **PawSharp.Client** - Discord client integration
- **PawSharp.Core** - Entity models
- **NAudio** - Cross-platform audio I/O
- **Concentus** - Opus codec (framework ready)
- **.NET 8.0** - Modern runtime

## 📚 Related Packages

- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - Main Discord client
- **[PawSharp.Gateway](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Gateway)** - Gateway connectivity
- **[PawSharp.Commands](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Commands)** - Command framework

## 🎯 Best Practices

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

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.