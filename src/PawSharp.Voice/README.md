# PawSharp.Voice

PawSharp.Voice provides voice connectivity and audio transport for PawSharp bots.

It covers the core building blocks needed for real voice features: voice gateway/session handling, audio frame transport, and Discord voice encryption workflows.

## Why Use This Package

- Voice channel connection lifecycle management
- Audio send/receive pipeline for bot features
- Integrates cleanly with PawSharp.Client
- Suitable for music bots, moderation tools, and voice automations

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Voice --version 1.0.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.Voice;

var voice = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannelId);

await connection.SetSpeakingAsync(true);
await connection.SendAudioAsync(pcmBytes);
await connection.SetSpeakingAsync(false);

await connection.DisconnectAsync();
```

## Typical Use Cases

- Playback and music queue bots
- Voice-based alerts and announcements
- Bots that process incoming voice audio

## Related Packages

- `PawSharp.Client`: high-level client integration
- `PawSharp.Gateway`: real-time transport dependencies
- `PawSharp.Core`: shared models and enums

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
