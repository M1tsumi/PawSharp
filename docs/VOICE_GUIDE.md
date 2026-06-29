# Voice & DAVE E2EE

This guide covers everything you need to know about using `PawSharp.Voice` —
connecting to voice channels, sending and receiving audio, and how the DAVE
end-to-end encryption layer works underneath.

---

## Installation

```bash
dotnet add package PawSharp.Voice  # 1.1.0-alpha.4
```

This pulls in NAudio (audio device I/O) and Concentus (Opus codec). The entire
crypto stack (AES-128-GCM, HKDF-SHA256, P-256 ECDH/ECDSA, MLS key schedule) comes from the
.NET 10 BCL — no extra crypto NuGet packages needed.

---

## Basic connection

```csharp
var voice   = client.UseVoice();
var channel = await client.Rest.GetChannelAsync(voiceChannelId);
var conn    = await voice.ConnectAsync(channel);
```

`ConnectAsync` sends a gateway op-4 (`VOICE_STATE_UPDATE`) and then waits for
Discord to reply with `VOICE_STATE_UPDATE` + `VOICE_SERVER_UPDATE` before
opening the actual voice WebSocket. You get back a `VoiceConnection` that is
already in the `Connected` state by the time the task completes.

### Connection states

```
Disconnected → Connecting → Connected → Disconnecting → Disconnected
```

`VoiceConnection.State` tracks this. Reconnection on errors is automatic
(exponential backoff from 1 s to 30 s, 5 attempts max).

---

## Sending audio from the microphone

```csharp
// Raise the speaking gate — Discord won't route your stream without this
await conn.SetSpeakingAsync(true);

// StartCapture wires up NAudio's WaveInEvent at 48 kHz / 16-bit / mono.
// Whenever the 20 ms capture buffer fills, the PCM is Opus-encoded,
// wrapped in an RTP header, DAVE-encrypted, and dispatched.
conn.StartCapture();

// ... let the bot run ...

conn.StopCapture();
await conn.SetSpeakingAsync(false);
```

The mic pipeline is entirely event-driven. `OnWaveInDataAvailable` fires every
20 ms (~1 920 bytes), the frame goes through `EncodeFrame → BuildRtpHeader →
_dave.EncryptFrame → WebSocket.SendAsync`.

---

## Sending pre-recorded PCM

If you're playing audio from a file or generating it in code:

```csharp
// Read a WAV file that's already 48 kHz 16-bit mono PCM.
// (Use NAudio or FFmpeg to resample if it's anything else.)
byte[] pcm = File.ReadAllBytes("audio.raw");

await conn.SetSpeakingAsync(true);
await conn.SendAudioAsync(pcm);
await conn.SetSpeakingAsync(false);
```

`SendAudioAsync` buffers bytes internally and only sends complete 20 ms frames
(960 samples × 1 channel × 2 bytes = 1 920 bytes each). Whatever's left over
at the end stays in the buffer until the next call. This means you can call it
with any chunk size — pass the whole file, pass 100 bytes at a time, it doesn't
matter.

### Streaming from a file

```csharp
const int ChunkSize = 3840; // 40 ms worth, two Opus frames at once

await conn.SetSpeakingAsync(true);

using var fs = File.OpenRead("audio.raw");
var buf = new byte[ChunkSize];
int read;
while ((read = await fs.ReadAsync(buf)) > 0)
{
    await conn.SendAudioAsync(buf[..read]);

    // Pace the stream at roughly realtime so we don't flood the WebSocket.
    // 40 ms per chunk = 25 chunks per second.
    await Task.Delay(40);
}

await conn.SetSpeakingAsync(false);
```

---

## Receiving audio from other users

Incoming packets arrive as encrypted RTP over the voice WebSocket. The receive
loop decodes them automatically:

```
receive binary WebSocket message
  → TryParseRtpPacket()     extract 12-byte RTP header + payload, read SSRC
  → _dave.DecryptFrame()    AES-128-GCM decrypt using per-SSRC sender key
  → DecodeAudio()           Opus decode → 16-bit PCM
  → PlayAudioAsync()        feed PCM to NAudio BufferedWaveProvider
```

On a headless server (no audio hardware), the `WaveOutEvent` initialisation
fails silently and `PlayAudioAsync` becomes a no-op. You can intercept the
decoded PCM before that point by using the provided playback helpers or by
handling PCM yourself. Note: the library does not currently expose a public
virtual hook inside the receive loop. Two practical options:

- Use `PlayAudioFromPcmAsync(byte[])` to feed decoded PCM into your own
  playback pipeline or analysis code.
- For deep integration (e.g. custom processing of every decoded frame), you
  may need to fork or extend the library to add a receive hook.

Example — play a local audio file (NAudio handles resampling):

```csharp
using NAudio.Wave;

using var reader = new AudioFileReader("music.mp3"); // supports many formats
// PlayAsync accepts a WaveStream and will resample if needed
using var cts = new CancellationTokenSource();
await conn.PlayAsync(reader, cts.Token);
```

Example — intercept PCM and write to disk (via `PlayAudioFromPcmAsync`):

```csharp
// Suppose you want to capture received PCM for analysis
client.Gateway.Events.Use(async (name, data) =>
{
    if (name == "VOICE_PACKET_RECEIVED" && data is byte[] pcm)
    {
        // This is illustrative; actual receive hook may require library change
        await File.WriteAllBytesAsync($"recv_{DateTime.UtcNow:yyyyMMddHHmmss}.pcm", pcm);
    }
});
```

---

## Speaking gate (op 5)

Discord's voice server won't route your RTP stream to other participants until
you send a `Speaking` payload:

```json
{ "op": 5, "d": { "speaking": 1, "delay": 0, "ssrc": 12345678 } }
```

`SetSpeakingAsync(bool)` handles this. It's idempotent — calling it twice with
the same value sends the gateway message only once.

`StartCapture()` calls `SetSpeakingAsync(true)` for you. `StopCapture()` calls
`SetSpeakingAsync(false)`. If you're using `SendAudioAsync` directly you need
to call it yourself.

---

## DAVE E2EE — the full picture

DAVE (Discord's AV Encryption protocol) uses **MLS (RFC 9420)** to establish a
shared key context among all participants. Here's the sequence:

```
- Server sends JSON op 21 (DavePrepareTransition) with dave_protocol_version, dave_transition_id
- Client responds with JSON op 23 (DaveTransitionReady) with dave_transition_id, key_package
- Server sends JSON op 22 (DaveExecuteTransition) — binary DAVE messages follow from here
- Server sends JSON op 24 (DavePrepareEpoch) — signals upcoming group state change
- Server sends BINARY op 25 (DaveMlsExternalSender) — credential + public key
- Client sends BINARY op 26 (DaveMlsKeyPackage) — MLS key package
- Server sends BINARY op 30 (DaveMlsWelcome) — MLS Welcome message
- Client processes Welcome and activates DAVE encryption
```

### Wire format of a single voice packet

```
┌──────────────────────────────────────────────────────────────────────┐
│  12 bytes RTP fixed header  (version, PT=120, seq, ts, SSRC)        │
│  ─── passed as Additional Authenticated Data (AAD) to AES-128-GCM ──│
├─────────────────────────────┬────────────────────┬───────────────────┤
│  8-byte monotonic counter   │  N bytes ciphertext │  16 bytes auth tag │
│  (frame sequence number)    │  (encrypted Opus)   │  (GCM tag)         │
└─────────────────────────────┴────────────────────┴───────────────────┘
```

Including the RTP header as AAD means the GCM tag authenticates the sequence
number, timestamp, and SSRC. A packet with a tampered header will fail
decryption even if the ciphertext is intact.

### Key derivation per sender

Each SSRC gets its own 16-byte AES-128 key derived from the current epoch
secret using HKDF-SHA256:

```
HKDF-Expand(
    prk   = epoch_secret,
    info  = "Discord Secure Frames v0 sender" ++ ssrc_big_endian_4_bytes,
    okm_len = 16
)
```

The label `"Discord Secure Frames v0 sender"` is the ASCII string
defined in Discord's DAVE spec. The four-byte SSRC suffix ensures each sender
in the session has a unique key even within the same epoch.

Keys are cached in `MLSState._senderKeyCache` and wiped on every epoch
transition (Welcome or Commit).

### Epoch transitions

When someone joins or leaves a voice channel, the server sends MLS proposals
(op 27) and the client submits a Commit+Welcome (op 28). The server confirms
the epoch advancement (op 29, binary). The MLS key schedule derives a new epoch
secret from the updated ratchet tree. All cached sender keys are immediately
invalidated, and the outgoing frame counter resets to zero to keep nonce
construction deterministic within each epoch.

### Cryptographic components

All of this runs on .NET 10's built-in `System.Security.Cryptography`. Nothing
in `PawSharp.Voice.DAVE` calls P/Invoke or loads a native crypto DLL.

| Layer | Algorithm | .NET type |
|-------|-----------|-----------|
| Frame encryption | AES-128-GCM | `System.Security.Cryptography.AesGcm` |
| Key derivation | HKDF-SHA256 | `System.Security.Cryptography.HKDF` |
| DH key agreement | P-256 ECDH (NIST SP 800-56A) | System.Security.Cryptography.ECDiffieHellman |
| Signing | ECDSA P-256 (FIPS 186-4) | System.Security.Cryptography.ECDsa |
| Symmetric encryption | AES-128-GCM (HPKE) | `System.Security.Cryptography.AesGcm` |
| Ratchet tree | TreeKEM (RFC 9420) | `PawSharp.Voice.DAVE.MLS.Tree` |
| Key schedule | RFC 9420 §8 | `PawSharp.Voice.DAVE.MLS.State.MLSKeySchedule` |

---

## Disconnecting and reconnecting

```csharp
// Graceful disconnect — sends WebSocket close frame, stops heartbeat,
// tears down audio I/O, transitions to Disconnected.
await conn.DisconnectAsync();

// You can reconnect later; ConnectAsync creates a fresh WebSocket and
// re-runs the full handshake (IDENTIFY, DAVE key exchange, etc.).
conn = await voice.ConnectAsync(channel);
```

When the connection drops unexpectedly (e.g. Discord restarts the voice server),
`VoiceClient` catches the error callback and retries automatically using the
stored handshake parameters. The retry sequence looks like:

```
attempt 1 — wait 1 s
attempt 2 — wait 2 s
attempt 3 — wait 4 s
attempt 4 — wait 8 s
attempt 5 — wait 16 s  →  give up, call DisconnectAsync, notify application
```

---

## Multiple voice connections

A single `VoiceClient` can hold connections to multiple channels
(typically in different guilds, but Discord also allows multiple channels
in the same guild):

```csharp
var c1 = await voice.ConnectAsync(channel1);
var c2 = await voice.ConnectAsync(channel2);

// All active connections, keyed by channel ID
foreach (var (id, c) in voice.ActiveConnections)
    Console.WriteLine($"{id}: {c.State}");

// Disconnect one at a time
await voice.DisconnectAsync(channel1);
```

---

## Known limitations (alpha)

- **Opus FEC** — forward error correction (`decode_fec = true`) is not used on
  the receive path yet. Dropped packets produce silence rather than concealment.
- **Stereo capture** — `WaveInEvent` is initialised as mono. The Opus encoder is
  mono too. Stereo capture will be added in a future release.
- **DAVE group leave** — when the bot disconnects, it does not currently send
  an MLS `Remove` proposal. On reconnect a new Welcome is issued by the server
  anyway, so in practice this is fine.
