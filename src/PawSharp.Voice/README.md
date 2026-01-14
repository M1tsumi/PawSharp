# PawSharp.Voice

Professional-grade voice channel connectivity and audio processing for PawSharp Discord bots.

## Features

- **Voice Channel Connectivity**: WebSocket-based connections to Discord voice channels
- **Audio Infrastructure**: Microphone capture and speaker playback with NAudio
- **Voice State Management**: Automatic handling of voice state updates and server changes
- **Audio Processing Framework**: Ready for Opus codec integration (Concentus library included)
- **Real-time Communication**: Low-latency voice data transmission and reception
- **Thread Safety**: All voice operations are thread-safe and async-compatible

## Installation

```bash
dotnet add package PawSharp.Voice
```

## Quick Start

```csharp
using PawSharp.Voice;

// Get the voice client from your Discord client
var voice = client.UseVoice();

// Connect to a voice channel
var connection = await voice.ConnectAsync(voiceChannel);

// Start capturing audio from your microphone
connection.StartCapture();

// The bot will now transmit your voice to the channel
// Other users' voices will be received and can be played back

// Stop capturing when done
connection.StopCapture();

// Disconnect from the voice channel
await connection.DisconnectAsync();
```

## Advanced Usage

### Audio Playback

```csharp
// Play received audio data through speakers
await connection.PlayAudioAsync(audioData);

// Stop playback
connection.StopPlayback();
```

### Voice Events

```csharp
// Handle voice state updates
client.Gateway.Events.On<VoiceStateUpdateEvent>("VOICE_STATE_UPDATE", evt =>
{
    Console.WriteLine($"{evt.UserId} joined voice channel {evt.ChannelId}");
});

// Handle voice server updates
client.Gateway.Events.On<VoiceServerUpdateEvent>("VOICE_SERVER_UPDATE", evt =>
{
    // Voice server information updated
});
```

## Audio Quality

- **Sample Rate**: 48kHz infrastructure ready for high-fidelity audio
- **Channels**: Mono configuration prepared for voice communication
- **Codec**: Framework ready for Opus integration
- **Buffer Size**: 20ms latency optimization
- **Processing**: Real-time audio pipeline with capture and playback support

## Dependencies

- **Concentus**: Cross-platform Opus codec implementation
- **NAudio**: .NET audio library for capture and playback

## Architecture

```
PawSharp.Voice
├── VoiceClient - Main voice client coordinator
├── VoiceConnection - Individual voice channel connections
├── Audio processing (Opus encoding/decoding)
└── WebSocket voice gateway communication
```

## Error Handling

Voice connections include comprehensive error handling for:
- Network interruptions
- Invalid voice servers
- Audio device issues
- Codec failures

## Thread Safety

All voice operations are thread-safe and can be called from any context.