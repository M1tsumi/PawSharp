# Voice

PawSharp.Voice implements the Discord Voice Protocol with Opus audio and DAVE end-to-end encryption (MLS / RFC 9420).

## Installation

```bash
dotnet add package PawSharp.Voice
```

## Basic Connection

```csharp
var voice = client.UseVoice();
var channel = await client.Rest.GetChannelAsync(voiceChannelId);
var conn = await voice.ConnectAsync(channel);
```

Connection states: `Disconnected -> Connecting -> Connected -> Disconnecting -> Disconnected`

## Sending Audio from Microphone

```csharp
await conn.SetSpeakingAsync(true);
conn.StartCapture(); // Captures from default mic at 48 kHz / 16-bit / mono

// ... let the bot run ...

conn.StopCapture();
await conn.SetSpeakingAsync(false);
```

## Sending Pre-recorded Audio

```csharp
byte[] pcm = File.ReadAllBytes("audio.raw");

await conn.SetSpeakingAsync(true);
await conn.SendAudioAsync(pcm);
await conn.SetSpeakingAsync(false);
```

`SendAudioAsync` buffers bytes internally and sends complete 20 ms frames (1920 bytes each).

## Streaming from a File

```csharp
const int ChunkSize = 3840; // 40 ms worth

await conn.SetSpeakingAsync(true);
using var fs = File.OpenRead("audio.raw");
var buf = new byte[ChunkSize];
int read;
while ((read = await fs.ReadAsync(buf)) > 0)
{
    await conn.SendAudioAsync(buf[..read]);
}
await conn.SetSpeakingAsync(false);
```

## Receiving Audio

```csharp
conn.OnAudioReceived += (frame) =>
{
    // frame.Data contains Opus-encoded audio
    // frame.Ssrc identifies the speaker
    Console.WriteLine($"Audio frame from SSRC {frame.Ssrc}, {frame.Data.Length} bytes");
};
```

## DAVE E2EE

DAVE (Discord Audio Video End-to-End Encryption) uses MLS (RFC 9420) for key agreement and AES-128-GCM for frame encryption. The entire crypto stack uses .NET 10 BCL APIs. Key exchange is handled automatically when the voice connection is established.
