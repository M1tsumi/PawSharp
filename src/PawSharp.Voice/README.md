# PawSharp.Voice

Voice channel connectivity for PawSharp bots, with full support for Discord's
**DAVE end-to-end encryption** protocol (RFC 9420 MLS).

As of 1.0.0-alpha.1 the audio pipeline is fully functional end-to-end:

- 16-bit signed mono PCM at 48 kHz captured from the microphone (NAudio)
- Encoded to Opus with Concentus (pure .NET, no P/Invoke)
- Wrapped in a 12-byte RTP header (RFC 3550 §5.1, payload type 120)
- Encrypted with AES-128-GCM; the RTP header is used as Additional Authenticated Data
- Sent over the Discord voice WebSocket

Incoming packets go through the reverse: decrypt → Opus decode → PCM → speaker.

The entire crypto stack (X25519, Ed25519, HPKE, HKDF, ratchet tree, MLS key
schedule) is built on `System.Security.Cryptography` with zero extra NuGet
dependencies.

---

## Installation

```bash
dotnet add package PawSharp.Voice  # 1.0.0-alpha.1
```

The package pulls in `NAudio` (audio I/O) and `Concentus` (Opus codec).
Everything else comes from the .NET 10 BCL.

---

## Quick start

```csharp
using PawSharp.Client;
using PawSharp.Voice;

var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged)
    .Build();

await client.ConnectAsync();

var voice      = client.UseVoice();
var channel    = await client.Rest.GetChannelAsync(voiceChannelId);
var connection = await voice.ConnectAsync(channel);

// Signal Discord that we're about to speak (required before sending audio)
await connection.SetSpeakingAsync(true);

// This starts the mic pipeline — PCM is captured in 20 ms chunks,
// Opus-encoded, DAVE-encrypted, and sent automatically.
connection.StartCapture();

// Do other work here...

connection.StopCapture();
await connection.SetSpeakingAsync(false);
await connection.DisconnectAsync();
```

---

## Sending pre-recorded audio

If you have raw PCM (e.g. decoded from a file) instead of live microphone input:

```csharp
// audioBytes must be 16-bit signed little-endian, mono, 48 kHz
// The method batches internally, so you can pass any number of bytes at once.
await connection.SetSpeakingAsync(true);
await connection.SendAudioAsync(audioBytes);
await connection.SetSpeakingAsync(false);
```

`SendAudioAsync` accumulates bytes in an internal buffer and flushes complete
20 ms frames (1 920 bytes = 960 samples × 2 bytes) as they become available.
Partial frames at the end are held until the next call, so you can stream audio
in any chunk size.

---

## Playing received audio

```csharp
// Incoming packets are decrypted and decoded automatically when they arrive.
// The decoded PCM is passed straight to PlayAudioAsync, which feeds NAudio:
await connection.PlayAudioAsync(pcmBytes);
```

If the process is running on a headless server (no audio hardware), `PlayAudioAsync`
is a no-op — packets are still received and decrypted, you just need to handle
the PCM yourself (e.g. write to a file or process it in-memory).

---

## Speaking gate (op 5)

Discord requires an op-5 `Speaking` payload before the server will route your
RTP stream to other clients. `SetSpeakingAsync` handles this:

```csharp
await connection.SetSpeakingAsync(true);   // raise the gate
// ... send audio ...
await connection.SetSpeakingAsync(false);  // lower the gate when done
```

`StartCapture()` and `StopCapture()` call `SetSpeakingAsync` automatically, so
you only need to call it manually when using `SendAudioAsync` directly.

---

## DAVE E2EE — how it works

Discord's DAVE protocol uses **MLS (Message Layer Security, RFC 9420)** to
establish a shared encryption context among all participants in a voice channel.

Here's the rough lifecycle:

1. Server sends op 22 — requests our MLS key package
2. We send op 21 — our `KeyPackage` (X25519 init key + Ed25519 signing key)
3. Server sends op 25 (Welcome) or op 26 (Commit) — we join the MLS group
4. Server sends op 24 — encryption is now active
5. Every outgoing Opus frame is encrypted; every incoming frame is decrypted

**Wire format for a single voice packet:**

```
[ 12 bytes RTP header ][ 12 bytes DAVE nonce ][ N bytes ciphertext ][ 16 bytes GCM tag ]
  ^-- used as AAD --^   ^---- encrypted payload (nonce + ciphertext + tag) ----^
```

The nonce is constructed from the sender's SSRC (4 bytes, big-endian) and a
monotonically increasing per-connection frame counter (8 bytes, little-endian).

Per-sender AES-128 keys are derived from the epoch secret using HKDF-SHA256
with the label `"Discord DAVE 1.0 sender key\0"` + 4-byte big-endian SSRC.
Keys are cached for the lifetime of an epoch and invalidated on every Commit
or Welcome.

| Operation | Algorithm |
|-----------|-----------|
| Media encryption | AES-128-GCM |
| Key derivation | HKDF-SHA256 |
| Key agreement | X25519 (RFC 7748) |
| Signing | Ed25519 (RFC 8032) |
| HPKE | DHKEM-X25519-AES128GCM (RFC 9180) |
| Ratchet tree | TreeKEM (RFC 9420) |
| Key schedule | RFC 9420 §8 |

---

## Connection lifecycle

```csharp
// Connect returns once the WebSocket handshake is complete.
// DAVE key exchange happens asynchronously in the background.
var conn = await voice.ConnectAsync(channel);

// State machine: Disconnected → Connecting → Connected → Disconnecting
Console.WriteLine(conn.State);   // VoiceConnectionState.Connected

// Reconnection is automatic (exponential backoff, up to 5 attempts).
// If all attempts fail, the connection transitions to Disconnected and
// the onConnectionFailed callback fires.

await conn.DisconnectAsync();
Console.WriteLine(conn.State);   // VoiceConnectionState.Disconnected
```

---

## Working with multiple channels

```csharp
var voice = client.UseVoice();

// Connect to two channels in the same guild (common for music bots with
// a separate staff channel)
var conn1 = await voice.ConnectAsync(publicChannel);
var conn2 = await voice.ConnectAsync(staffChannel);

// ActiveConnections is keyed by channel ID
foreach (var (channelId, conn) in voice.ActiveConnections)
    Console.WriteLine($"{channelId}: {conn.State}");

await conn1.DisconnectAsync();
await conn2.DisconnectAsync();
```

---

## Error handling

Most errors during connection are handled internally via the reconnect logic.
For application-level error handling:

```csharp
try
{
    var conn = await voice.ConnectAsync(channel);
}
catch (ArgumentException ex)
{
    // Channel isn't a voice channel, or not in a guild
    Console.WriteLine(ex.Message);
}

// Crypto failures on inbound frames are swallowed to protect the receive loop
// (a single tampered packet doesn't crash the loop). For outbound errors,
// SendAudioAsync will throw if the WebSocket is closed.
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Concentus | 1.1.0 | Opus audio codec (pure .NET) |
| NAudio | 2.2.1 | Audio device I/O |
| PawSharp.Client | 1.0.0-alpha.1 | DiscordClient integration |
| .NET 10 BCL | — | AES-GCM, HKDF, Ed25519, X25519, WebSocket |

---

## License

MIT — see [LICENSE](../../LICENSE).
