# Changelog

All notable changes to PawSharp are documented here.

---

## [1.1.0-alpha.4] - 2026-06-24

### Critical Fixes

- **DAVE opcode mapping corrected to Discord's spec** (`PawSharp.Voice`)
  - The entire 21–31 opcode mapping was rewritten — 10 of 11 opcodes were wrong. Old mapping confused opcode purposes (e.g., op 22 was "KeyPackageRequest" which doesn't exist; ops 25/26/28/30/31 were swapped). Now matches Discord's published voice gateway opcode table exactly.
  - Ops 21–24, 31 are **JSON text**; ops 25–30 are **binary** WebSocket messages (previously everything was treated as JSON).
  - `DAVEProtocol.HandleOpcodeAsync` split into `HandleJsonMessageAsync` (ops 21–24, 31) and `HandleBinaryMessageAsync` (ops 25–30).
  - Binary format: server→client `[2-byte seq][1-byte opcode][payload]`, client→server `[1-byte opcode][payload]`.

- **Binary WebSocket messages silently dropped** (`PawSharp.Voice`)
  - `VoiceConnection.ReceiveLoopAsync` was calling `HandleBinaryDaveMessageAsync` but it was a no-op stub. Binary DAVE messages (ops 25–30) were being silently discarded. Now properly parsed and routed to `DAVEProtocol.HandleBinaryMessageAsync`.

- **WebSocket fragmentation not handled** (`PawSharp.Voice`)
  - `ReceiveLoopAsync` only read the first fragment of multi-fragment messages. Added `while (!result.EndOfMessage)` loop to accumulate all fragments before processing.

- **Keep-alive task not tracked as instance field** (`PawSharp.Voice`)
  - `_keepAliveTask` was not stored as a field, so it could not be awaited on disconnect. Added field alongside `_gatewayTask`, `_udpReceiveTask`, etc.

- **Transport encryption nonce comment misleading** (`PawSharp.Voice`)
  - The nonce comment in `DAVEEncryption` suggested a sender-key ratchet would track nonces, but the spec uses deterministic nonce construction (`SSRC(4) || seq(2) || zeros(6)`). Corrected to match Discord's `aead_aes256_gcm_rtpsize` spec.

- **Stale X25519/Ed25519 references in documentation** (`PawSharp.Voice`, `docs/`)
  - `docs/VOICE_GUIDE.md`: 6 stale references (ciphersuite name, opcode sequence, wire format, key derivation label, DH primitive, signing algorithm) updated to P-256.
  - `src/PawSharp.Voice/README.md`: Ciphersuite reference updated.
  - 4 source files had stale X25519/Ed25519 comments — all corrected to P-256.

- **Stale User-Agent version** (`PawSharp.API`)
  - User-Agent string now derives the library version from `AssemblyInformationalVersionAttribute` at runtime, ensuring it always matches `Directory.Build.props`. No more drift between hardcoded version strings and the actual package version.

- **Hardcoded voice WebSocket protocol v8** (`PawSharp.Voice`)
  - `VoiceConnection` no longer hardcodes `?v=8` for the voice WebSocket URI. Introduced `VoiceProtocolVersion` constant (currently `8`) that matches Discord's latest voice gateway protocol requirement for DAVE E2EE.

### Added

- **DAVE E2EE ciphersuite: X25519/Ed25519 → P-256 migration** (`PawSharp.Voice`)
  - Ciphersuite swapped from `MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519` to `MLS_128_DHKEMP256_AES128GCM_SHA256_P256`, aligning with Discord's production DAVE specification.
  - Added `ICryptoProvider` abstraction (`MLS/Crypto/ICryptoProvider.cs`) — interface for P-256 ECDH, ECDSA P-256 signing/verification, HKDF, SHA-256, and AES-128-GCM operations.
  - Added `BouncyCastleCryptoProvider` — full implementation using the BouncyCastle.Cryptography library for P-256 operations (ECDH key agreement, ECDSA signatures, HKDF expand/extract, SHA-256 hashing), with AES-128-GCM delegating to .NET BCL `AesGcm`.
  - Added `CryptoProviderFactory` — thread-safe singleton factory with lazy initialization, defaulting to BouncyCastle.

- **HPKE P-256 implementation** (`PawSharp.Voice`)
  - Added `HpkeP256` — RFC 9180 HPKE BASE mode using DHKEM(P-256, HKDF-SHA256) with AES-128-GCM AEAD. Implements full `LabeledExtract`/`LabeledExpand` with proper suite ID construction, cached public key derivation, and constant-time nonce computation.
  - Used for decrypting `EncryptedGroupSecrets` entries in MLS Welcome messages.

- **MLS key schedule (RFC 9420 §8)** (`PawSharp.Voice`)
  - Added `MLSKeySchedule` — full RFC 9420 §8 key schedule derivation chain:
    - `joiner_secret = HKDF-Extract(init_secret, commit_secret)`
    - `welcome_secret = DeriveSecret(joiner_secret, "welcome")`
    - `epoch_secret = HKDF-Extract(DeriveSecret(joiner_secret, "epoch"), GroupContext)`
    - `exporter_secret = DeriveSecret(epoch_secret, "exporter")` — used as DAVE epoch secret root
    - `confirmation_key = DeriveSecret(epoch_secret, "confirmed")`
    - `init_secret = DeriveSecret(epoch_secret, "init")` — carried forward to next epoch
  - Supports `FromJoinerSecret` factory for Welcome-joiner path and `AdvanceEpoch` for Commit processing.
  - DAVE epoch secret derived via `ExpandWithLabel(exporter_secret, "Discord Secure Frames v0", "", 32)` per MLS-Exporter pattern (RFC 9420 §8.5).

- **ExtractEpochSecret restored as public API** (`PawSharp.Voice`)
  - `DAVEKeyDerivation.ExtractEpochSecret` was accidentally removed during dead-code cleanup. Restored since it is a valid utility for deriving the epoch-level exporter secret from an `MLSKeySchedule` — external consumers (integration tests, logging, diagnostic tools) may depend on it.

- **6 integration tests for DAVE binary message handling** (`PawSharp.Voice.Tests`)
  - `DAVEIntegrationTests.cs` adds wire-format parsing tests (server→client binary), full Welcome handshake simulation, Commit epoch advance, AnnounceCommitTransition processing, InvalidCommitWelcome reset, and empty-payload resilience.
  - All opcode-based tests updated for the corrected 21–31 mapping. 60 `DAVEProtocolTests` tests pass with new signatures.

- **DAVE protocol state machine** (`PawSharp.Voice`)
  - `DAVEProtocol` now handles the full DAVE opcode set (21–31) per Discord's published spec:
    - Ops 21–24, 31: **JSON text** messages on the voice gateway
    - Ops 25–30: **binary** WebSocket messages (`[2-byte seq][1-byte opcode][payload]` server→client)
    - op 21 (JSON): `DavePrepareTransition` — server announces upcoming DAVE activation
    - op 22 (JSON): `DaveExecuteTransition` — server signals DAVE transition is executing
    - op 23 (JSON): `DaveTransitionReady` — client acknowledges DAVE readiness with key_package
    - op 24 (JSON): `DavePrepareEpoch` — server signals upcoming epoch change
    - op 25 (binary): `DaveMlsExternalSender` — server's external sender credential + public key
    - op 26 (binary): `DaveMlsKeyPackage` — client sends MLS key package for group membership
    - op 27 (binary): `DaveMlsProposals` — server sends Add/Remove/Update proposals
    - op 28 (binary): `DaveMlsCommitWelcome` — client sends Commit + optional Welcome
    - op 29 (binary): `DaveMlsAnnounceCommitTransition` — server confirms epoch advancement
    - op 30 (binary): `DaveMlsWelcome` — server distributes Welcome (join/recovery)
    - op 31 (JSON): `DaveMlsInvalidCommitWelcome` — client signals invalid commit/Welcome
  - `VoiceConnection` wires all DAVE opcodes through `HandleJsonMessageAsync` with proper state transitions (`DaveNegotiating` → `DaveEncrypted`).
  - Events: `DaveEncryptionActivated`, `DaveEpochAdvanced`, `DaveError`.
  - Thread-safe design: `volatile bool` for `_active`/`_transitionPending`, `Interlocked` for frame counter.

- **DAVE frame encryption format** (`PawSharp.Voice`)
  - `DAVEEncryption` implements the DAVE v1.1 frame format:
    - Wire layout: `[8-byte monotonic counter][AES-128-GCM ciphertext][16-byte auth tag]`
    - Nonce derivation: `base_nonce = SHA-256(sender_key || I2OSP(ssrc, 4))[0..12]`, then `nonce = base_nonce XOR I2OSP(counter, 12)`
    - The 8-byte counter is transmitted in-band so the receiver can reconstruct the nonce without out-of-band tracking.
    - RTP header (12 bytes) is passed as Additional Authenticated Data (AAD), binding the GCM tag to the full wire metadata (sequence number, timestamp, SSRC).
  - Per-sender key derivation via `DAVEKeyDerivation.DeriveEncryptionKey` using MLS-Exporter with user-ID context: `ExpandWithLabel(epoch_secret, "Discord Secure Frames v0 sender", userID, 16)`.

### Changed

- **MLSGroupState refactored for RFC 9420 compliance** (`PawSharp.Voice`)
  - `MLSGroupState` rewritten as a complete RFC 9420 group state engine:
    - Owns `RatchetTree`, `MLSKeySchedule`, `GroupContext`, local key material, and proposal queue.
    - `ProcessWelcome` — full path: TLS-decodes Welcome, HPKE-decrypts `EncryptedGroupSecrets`, decrypts `GroupInfo` via `welcome_key`/`welcome_nonce`, populates group context, runs key schedule.
    - `ProcessCommit` — decodes commit, applies queued proposals (Add/Remove/Update), merges `UpdatePath` into ratchet tree for commit secret, advances transcript hash and tree hash, advances key schedule.
    - `ProcessProposals` — TLS-decodes and queues incoming proposals for application at next commit.
    - External sender package (op 31) stored and bound as HKDF salt during epoch advances for forward secrecy.
    - HKDF rotation fallback on malformed commits — forward secrecy maintained even if MLS parse fails.
    - Key material zeroed in `Reset()` and `Dispose()` via `Array.Clear` + `try/finally`.

- **Per-SSRC sender key caching** (`PawSharp.Voice`)
  - `MLSState` (public facade) now caches derived sender keys in a `ConcurrentDictionary<uint, byte[]>`.
  - Cache is invalidated on every `ProcessWelcome`/`ProcessCommit`/`Reset()` call.
  - Keys derived lazily on first access via `DAVEKeyDerivation.DeriveEncryptionKey`.

- **Thread safety hardening** (`PawSharp.Voice`)
  - `DAVEProtocol`: `_active` and `_transitionPending` made `volatile`; outgoing frame counter uses `Interlocked.Increment`/`Interlocked.Exchange`.
  - `VoiceConnection`: RTP header building uses `lock (_rtpLock)`; last-frame-sent tick uses `Interlocked.Read`/`Interlocked.Exchange`.
  - `CryptoProviderFactory`: thread-safe double-checked locking for singleton initialization.
  - `MLSState`: `_senderKeyCache` is a `ConcurrentDictionary` — safe for concurrent read/write.

- **VoiceConnection DAVE integration** (`PawSharp.Voice`)
  - `SendAudioAsync` and keep-alive loop now use `dave?.IsActive` before attempting DAVE encryption, falling back to transport encryption or plain Opus.
  - DAVE decryption in `UdpReceiveLoopAsync` — packets that fail DAVE decrypt are logged and skipped (non-fatal).
  - `DisconnectAsync` properly disposes `DAVEProtocol` instance.
  - `ResumeAsync` re-initializes DAVE protocol if enabled.

- **ConfigureAwait(false) added project-wide** (all library projects)
  - Added `.ConfigureAwait(false)` to every `await` call in `DiscordClient.cs` (208 calls), `AdvancedRateLimiter.cs` (2 calls), `RedisCacheProvider.cs`, and `RedisCacheDistributor.cs`. Prevents deadlocks in synchronization-context-sensitive hosts (ASP.NET, WinForms, WPF).

- **Removed obsolete public API surface** (`PawSharp.Client`, `PawSharp.Gateway`)
  - Removed `[Obsolete]` `AddPawSharpClient()` overloads from `PawSharpServiceCollectionExtensions`. Use `SetupPawSharp()` or `AddPawSharpWithMemoryCache()` instead.
  - Removed `[Obsolete]` attribute from `HeartbeatManager.Stop()` — now a documented non-obsolete method alongside `StopAsync()`.

- **Nullable warnings resolved** (all projects)
  - Fixed ~35 CS8600/CS8601/CS8602/CS8604 nullable reference type warnings across `CommandsExtension.cs`, `TypeConverterService.cs`, `VoiceConnection.cs`, `GatewayClient.cs`, `EventDispatcher.cs`, and `InteractionHandler.cs`.

- **HeartbeatManager default aligned** (`PawSharp.Gateway`)
  - `HeartbeatManager` constructor default `maxMissedAcks` changed from `2` to `3`, matching `PawSharpOptions.MaxMissedHeartbeatAcks` default.

- **Thread-safe `RateLimitBucket.Release()`** (`PawSharp.API`)
  - `RateLimitBucket.Release()` now uses `try/catch(SemaphoreFullException)` instead of the racy `CurrentCount == 0` check. Prevents `SemaphoreFullException` under concurrent access.

- **Voice connection hardening** (`PawSharp.Voice`)
  - `PlayAudioAsync()` and `PlayAudioFromPcmAsync()` now throw `ObjectDisposedException` when called on a disposed connection instead of silently succeeding.
  - `UserVoiceStateChanged` event preserved as public API placeholder.

- **`Random.Shared` for heartbeat jitter** (`PawSharp.Gateway`)
  - `HeartbeatManager.RunHeartbeatLoopWithJitterAsync` now uses `Random.Shared` instead of creating a new `Random()` per invocation, avoiding duplicate seeds under high shard counts.

- **XML documentation fixed** (`PawSharp.Interactions`, `PawSharp.Interactivity`)
  - Fixed unresolvable `<see cref="InteractionHandler.EditOriginalResponseAsync"/>` — corrected to `<see cref="PawSharp.API.Interfaces.IDiscordRestClient.EditOriginalInteractionResponseAsync"/>`.
  - Fixed badly-formed XML comment in `MessageFlagExtensions` (unescaped `<` in summary).

- **Unused field cleanup** (`PawSharp.Gateway`)
  - `EventDispatchQueue._disposed` field is now properly set to `true` during `Dispose()`, making the disposal guard in `EnqueueAsync()` functional.

### Testing

- **Fixed xUnit1031 warning** (`PawSharp.Commands.Tests`)
  - `CommandDelegateFactory_Supports_Void_Returning_Command_Methods` now uses `await` instead of `.GetAwaiter().GetResult()` — prevents potential deadlocks in test runners.

- **DAVE test infrastructure** (`PawSharp.Voice.Tests`)
  - `DAVETestData` helper generates structurally valid MLS Welcome/Commit messages using real HPKE, HKDF, and AES-GCM primitives.
  - All 16 previously-skipped DAVE tests now run against cryptographically-generated test data.

### Documentation

- **Root README humanized** (`README.md`)
  - Rewritten with a warmer, more approachable tone throughout.
  - Added conversational intro, friendlier section headings, and clearer "get started" flow.
  - Updated all version references from `alpha.3` to `alpha.4`.
  - Added "Join the community" section with Discord invite.
  - Restructured "What you can build" to read like real use-cases rather than a spec sheet.

- **All 9 package READMEs updated** (`src/*/README.md`)
  - Stale `--version 1.1.0-alpha.3` install commands updated to `1.1.0-alpha.4`.
  - `PawSharp.Commands` README now pins the version in its install command, matching the convention of all other packages.

### Warning Cleanup (Post-Audit)

- **Fixed swapped arguments in `ComponentValidator`** — `ValidationException` was called with `(paramName, null, message)` instead of `(message, paramName)`, causing CS8625 and incorrect error messages.
- **Fixed XML doc cref in `DiscordException`** — unresolvable `<see cref="DiscordApiException"/>` replaced with `<c>DiscordApiException</c>` since `PawSharp.Core` does not reference `PawSharp.API`.
- **Removed dead fields from `MemoryCacheProvider`** — `_options` and `_cleanupTimer` were declared but never assigned, causing CS8618.
- **Fixed obsolete `RedisChannel` implicit conversions** — `RedisCacheDistributor` now uses `RedisChannel.Literal(channel)` instead of the deprecated implicit string-to-`RedisChannel` conversion (3 occurrences, CS0618).
- **Removed unused `catch (Exception ex)` variable names** — `CacheSwapper` propagation blocks and circuit-breaker fallback no longer declare unused `ex` (5 occurrences, CS0168).
- **Added null-forgiving operator in `CacheSwapper.GetProviderOrThrow`** — `_activeProvider.Provider` now uses `_activeProvider!.Provider` (CS8602) since the earlier checks guarantee non-null.
- **Suppressed CS0067 on `RedisCacheProvider.EntityEvicted`** — event is part of the `IEntityCache` contract but unused in the Redis provider.
- **Fixed null-forgiving in `DiscordApiException`** — `innerException` passed to base constructor now uses `innerException!` (CS8604).
- **Fixed null-forgiving in `MessageExtensions`** — `value.GetString()` now uses `value.GetString()!` (CS8604).
- **Fixed `_seqAck` never being assigned in `VoiceConnection`** — RTP sequence numbers are now extracted from received voice packets in both WS and UDP receive loops, fixing CS0649. Stale empty heartbeat-ACK handler (case 9) removed.
- **Fixed null reference in `BuiltInAutocompleteProviders`** — `Name` fallback added for null role/channel names (`?? "unknown role"` / `?? "unknown"`) (CS8601).
- **Added null guard in `RequireRoleAttribute.GetMemberRolesAsync`** — `member.User` is now null-checked before accessing `.Id` (CS8602).
- **Suppressed CS0067 in test mock** — `MockCacheProvider` events required by interface contract but unused in tests.
- **Fixed CS8602 in `CommandsExtensionTests`** — added null-forgiving operators in test assertions.

### Alpha 4 Audit Fixes

- **Migration guide stale version** (`docs/MIGRATION.md`)
  - `VoiceProtocolVersion` constant was documented as `4` — corrected to `8` to match the actual value in `VoiceConnection.cs`.

- **DAVE crypto failure logging** (`PawSharp.Voice`)
  - `DAVEProtocol.EncryptFrame` and `DecryptFrame` now log errors before returning plaintext/ciphertext on failure instead of silently swallowing exceptions.
  - Added `ILogger?` parameter to `DAVEProtocol` constructor; VoiceConnection passes its logger when creating instances.

- **MLS Commit exception narrowed** (`PawSharp.Voice`)
  - `MLSGroupState.ProcessCommit` fallback changed from `catch (Exception)` to `catch (MlsDecodeException)` — programming errors (e.g. `NullReferenceException`) are no longer masked by the HKDF rotation fallback.

- **Generic cache TTL configurable** (`PawSharp.Cache`)
  - Added `GenericCacheExpiration` property to `CacheOptions` (default: 1 hour). The cleanup loop now uses this value instead of a hardcoded `TimeSpan.FromHours(1)`.

- **String-keyed entity expiration** (`PawSharp.Cache`)
  - Added `_lastAccessString` tracking for `_members`, `_roles`, and `_emojis` (which use composite `guildId:entityId` string keys). Expiration cleanup now correctly processes these entity types.

- **Gateway rate limiter leak fix** (`PawSharp.Gateway`)
  - `GatewaySendAsync` now uses the connection `CancellationToken` for the 60-second rate-limit release delay. On disconnect, the token is cancelled and the semaphore releases immediately — preventing leaked permits.

- **Encryption mode selection hardened** (`PawSharp.Voice`)
  - `SelectEncryptionMode` replaced fragile `Enum.TryParse` with a `Dictionary<string, VoiceEncryptionMode>` mapping Discord's wire-format mode strings to enum values.

- **UserVoiceStateChanged wired** (`PawSharp.Voice`)
  - Removed `#pragma warning disable CS0067` placeholder. The event now fires with `(userId, connected: true)` on op 11 (CLIENT_CONNECT) and `(userId, connected: false)` on op 13 (CLIENT_DISCONNECT).

- **ModerationBot error notification** (`examples/ModerationBot`)
  - `BanUserByIdAsync` renamed to `TryBanUserByIdAsync` returning `bool`. The caller now notifies the user on failure instead of silently logging and sending a success message.

### Housekeeping

- Changed `#pragma warning disable IDE0011` (blanket file-level suppression) removed from `RestClient.cs`. EditorConfig brace rules now apply to all new code.
- `Directory.Build.props` version bumped to `1.1.0-alpha.4`.

## [1.1.0-alpha.3] - 2026-06-15

### New Features

- **IDiscordClient interface** (`PawSharp.Client`)
  - New `IDiscordClient` interface with 130+ methods, 8 properties, 2 events, and 74 gateway event subscriptions.
  - `DiscordClient` now implements `IDiscordClient` — all existing code continues to work.
  - `PawSharpClientBuilder.Build()` returns `IDiscordClient`.
  - DI registration registers both `IDiscordClient` and `DiscordClient` for backward compatibility.
  - Enables full mocking via Moq for unit tests.

- **Connection state tracking** (`PawSharp.Client`)
  - Added `ClientConnectionState` enum (`Disconnected`, `Connecting`, `Connected`, `Disconnecting`).
  - `DiscordClient.ConnectionState` property and `ConnectionStateChanged` event.
  - `DiscordClient.IsConnected` helper property.
  - `DiscordClient.ReconnectAsync()` for graceful disconnect-reconnect cycles.
  - State transitions are tracked during `ConnectAsync()` and `DisconnectAsync()`.

- **Global exception handler infrastructure** (`PawSharp.Client`)
  - Added `DiscordClient.SetupGlobalExceptionHandlers()` static method.
  - Wires `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`.
  - Optional logger and custom callback parameters.

- **Command module auto-discovery** (`PawSharp.Commands`)
  - `CommandsExtension.RegisterModulesInAssembly()` — discovers and registers all `BaseCommandModule` subclasses in an assembly.
  - `CommandsExtension.RegisterSlashModulesInAssemblyAsync()` — same, but as slash commands.
  - `UseCommandsWithAutoDiscovery()` extension method for one-liner setup.
  - Modules are resolved via service provider when available, falling back to `Activator.CreateInstance`.

- **Convenience methods on DiscordClient** (`PawSharp.Client`)
  - `SendDirectMessageAsync(ulong userId, string content)` — creates DM channel and sends message.
  - `TrySendMessageAsync(ulong channelId, string content)` — returns null on failure instead of throwing.
  - `TryReplyAsync(MessageCreateEvent, string)` — non-throwing reply helper.
  - `SendEmbedAsync(ulong channelId, Embed embed)` — shorthand for sending a single embed.
  - `GetOrCreateThreadAsync(ulong channelId, string threadName, ...)` — find or create a thread.

- **Builder validation** (`PawSharp.Client`)
  - `PawSharpClientBuilder.Build()` now validates token (not null/empty), intents (not `None`), API version (in supported range).
  - All validation errors include actionable messages.
  - Added `PawSharpClientBuilder.Create()` static factory method.

- **XML documentation with code examples** (across all projects)
  - Added `<example>` blocks to 30+ methods across `DiscordClient`, `PawSharpClientBuilder`, `InteractionHandler`, `CommandsExtension`, `IDiscordRestClient`, `IEntityCache`, `IGatewayClient`.
  - Examples compile, use realistic patterns, and include error handling.

### Changes

- **Exception hierarchy consolidated** (`PawSharp.Core` / `PawSharp.API`)
  - `PawSharp.Core.Exceptions.DiscordApiException` now inherits from `PawSharp.API.Exceptions.DiscordApiException` instead of being a separate class — eliminates catch ambiguity. [Breaking if you caught the Core version by full type name]
  - `PawSharp.Cache.Exceptions.CacheException` now inherits from `DiscordException` instead of `Exception` — all library exceptions are now in the same hierarchy.

- **Error handling hardened** (all modules)
  - Gateway `UpdatePresenceAsync`, `RequestGuildMembersAsync`, `RequestSoundboardSoundsAsync` now re-throw exceptions after logging (previously silent).
  - `GatewaySendAsync` rate-limit release uses `CancellationToken.None` to prevent semaphore leak on cancellation.
  - `EventDispatchQueue.Dispose()` stores disposal task and exposes `WaitForDrainAsync()`.
  - `WebSocketConnection.Dispose()` stores disposal task and exposes `WaitForDisposeAsync()`.
  - 15 empty `catch {}` blocks replaced with `catch (Exception)` across CacheSwapper, ChannelExtensions, WebhookVerifier, InteractionExtensions.
  - `CacheManager.HandleGuildMemberUpdate` null-guards `e.User`.
  - `InteractivityValidation` now uses `ValidationException` from Core instead of `ArgumentException`.

- **Log sanitization and security** (`PawSharp.API` / `PawSharp.Interactions`)
  - `PawSharpClientBuilder` now enforces TLS 1.2+ on the HttpClient via `SslOptions`.
  - `WebhookVerifier` XML docs updated with clear warning about non-constant-time BigInteger implementation.
  - Token security warning added to `PawSharpOptions.Token` XML docs.
  - `ConnectAsync()` XML docs recommend setting up global exception handlers.

### Documentation

- Created `docs/MIGRATION.md` — migration guide covering all breaking changes from 0.x through 1.1.0-alpha versions.
- Rewrote README.md — comprehensive, human-written, with clear getting-started paths, package reference, and real code examples.
- Updated all .NET version references from 8.0 to 10.0 across 4 documentation files.
- Re-reconciled `docs/VOICE_GUIDE.md` and `src/PawSharp.Voice/README.md` — Voice README now accurately describes built-in MLS DAVE E2EE implementation.
- Fixed `src/PawSharp.Gateway/README.md` — event examples now use correct `On<T>("EVENT_NAME", handler)` API.
- Fixed formatting issues in `docs/PATTERNS_GUIDE.md` and `docs/GATEWAY_GUIDE.md`.
- Updated `docs/DEVELOPERS_GUIDE.md`, `docs/TROUBLESHOOTING.md`, `docs/VOICE_GUIDE.md` version and framework references.
- Updated `docs/INDEX.md` to reflect new features and fix version numbers.

### Example Bots

- **ModerationBot** — all 22 `_client.API.*` calls changed to `_client.Rest.*` (property didn't exist).
- **MusicBot** — `AddPawSharpCommands()` → `AddCommands()`, `CommandModule` → `BaseCommandModule`, `MusicService` now DI-injected (no duplicate creation), removed non-existent attributes.
- **DashboardBot** — rewritten to use `InteractionHandler` instead of non-existent `InteractionService`.
- All example bots updated to use `IDiscordClient` instead of `DiscordClient`.

### Bug Fixes

- **Voice — DAVE E2EE fixes** (`PawSharp.Voice`)
  - Fixed DM/GroupDM voice calls crashing on connect: `VoiceClient.ConnectAsync` now accepts DM/GroupDM channel types with `guildId = 0`, and `OnVoiceServerUpdate` matches DM connections by channel type when the gateway sends `guild_id = 0`.
  - Fixed inbound DAVE frames being silently dropped: `UdpReceiveLoopAsync` now checks `_dave?.IsActive` before attempting transport decryption on received audio packets.
  - Fixed keep-alive (NAT timeout) never running: `KeepAliveLoopAsync` is now started during `ConnectInternalAsync` instead of being left as dead code.
  - Fixed forward secrecy gap: external sender packages (op 31) are now stored in `MLSGroupState` and bound as the HKDF salt during commit epoch advances, so future commits benefit from the sender's entropy.

- **Example bot compilation** — fixed `client.API` → `client.Rest` in ModerationBot, fixed `AddPawSharpCommands` → `AddCommands` in MusicBot, removed references to non-existent `InteractionService`, `CommandModule`, `[CommandModule]`, and `Color` enum in example projects.

### Internal / Tooling

- Exposed `DAVEProtocol.MlsState` as internal for test access and added `InternalsVisibleTo` for `PawSharp.Voice.Tests`.
- Created `DAVETestData` helper that generates structurally valid MLS Welcome/Commit messages using real HPKE, HKDF, and AES-GCM primitives.
- Unskipped all 16 previously-skipped DAVE tests — they now run against real cryptographically-generated test data.

## [1.1.0-alpha.2] - 2026-05-03

### New Features

- **Cache System Enhancements** (`PawSharp.Cache`)
  - Added comprehensive telemetry for cache operations (hits, misses, operation durations, evictions)
  - Added `ICacheTelemetry` interface and `CacheTelemetry` implementation for monitoring cache performance
  - Added `ICacheProviderHealthCheckable` interface for provider health checks
  - Implemented health checks on all cache providers (Memory, Redis, Distributed)
  - Added telemetry recording to all cache operations (sync and async)
  - Added eviction recording telemetry in MemoryCacheProvider LRU eviction
  - Updated README with telemetry usage examples and health check documentation

### Bug Fixes

- Fixed health check logic in `CacheSwapper` to properly check provider health on registration
- Fixed duplicate `IsHealthy` method in test mock class

## [1.1.0-alpha.1] - 2026-05-01

### New Features

- **Cache System Enhancements** (`PawSharp.Cache`)
  - Added cache metrics for monitoring and observability.
  - Added per-entity TTL (time-to-live) configuration options.
  - Added cache invalidation events to notify subscribers of cache changes.
  - Added health checks for cache providers (Memory and Redis).
  - `RedisCacheProvider` guild-role and guild-emoji tracking improved with guild-specific cache keys.
  - `MemoryCacheProvider` role and emoji tracking fixed with guild-specific key storage.

- **Commands System** (`PawSharp.Commands`)
  - Added `AutocompleteHandlerAttribute` for autocomplete interaction handlers.
  - Added `SlashContextsAttribute` for specifying interaction contexts.
  - Added `SlashDefaultMemberPermissionsAttribute` for default member permissions on slash commands.
  - Added `SlashIntegrationTypesAttribute` for integration type configuration.
  - Enhanced `RequirePermissionsAttribute` with improved permission checking logic.
  - Added `DiscordPermissions` helper class for permission calculations.
  - Enhanced `TypeConverterService` with additional conversion support.
  - Added `CommandsBuilder` DI improvements for better service registration.

- **Core Entities & Builders** (`PawSharp.Core`)
  - Added new entity types: `VerificationLevel` enum, enhanced `Channel` properties.
  - Enhanced `EmbedBuilder` with new helper methods for embed construction.
  - Added `ComponentBuilder` for easier component creation.
  - Added `DiscordResources` entity for resource management.
  - Enhanced `Guild`, `Message`, `Presence`, `Role`, and `User` entities with additional properties.
  - Added multiple extension methods:
    - `ChannelTypeExtensions` - helper methods for channel types
    - `CollectionExtensions` - utility methods for collections
    - `ColorExtensions` - color manipulation helpers
    - `GuildFeatureExtensions` - guild feature checking
    - `InteractionTypeExtensions` - interaction type helpers
    - `MessageTypeExtensions` - message type utilities
    - `PermissionsExtensions` - permission calculation helpers
    - `SnowflakeExtensions` - Discord snowflake utilities
    - `StringExtensions` - string manipulation helpers
    - `VerificationLevelExtensions` - verification level helpers
  - Enhanced `PerformanceMetrics` with additional telemetry.
  - Enhanced `PawSharpOptions` with new configuration options.

- **Gateway & Events** (`PawSharp.Gateway`)
  - Added `EventFilteringMiddleware` for custom event filtering.
  - Added `EventReplayBuffer` for event replay capabilities.
  - Added `GatewayDiagnostics` for enhanced diagnostics and monitoring.
  - Added `ShardRebalancingManager` for automatic shard rebalancing.
  - Enhanced `ShardManager` with improved sharding logic.
  - Enhanced `WebSocketConnection` with better connection management.
  - Enhanced `EventDispatcher` with improved event routing.
  - Enhanced `HeartbeatManager` with better heartbeat tracking.
  - Enhanced `ReconnectionManager` with improved reconnection logic.

- **Interactions** (`PawSharp.Interactions`)
  - Enhanced `InteractionHandler` with improved handler registration.
  - Enhanced `InteractionExtensions` with additional helper methods.
  - Enhanced `ModalBuilder` with improved modal construction.
  - Added `ComponentBuilders` for various component types.

- **Extensions** (`PawSharp.Extensions`)
  - Enhanced `ChannelExtensions` with additional channel operations.
  - Added `InteractionCreateEventExtensions` for interaction event helpers.
  - Enhanced `MessageExtensions` with additional message operations.

- **API Client** (`PawSharp.API`)
  - Enhanced `RestClient` with additional REST operation support.
  - Enhanced `IDiscordRestClient` interface with new methods.

### Bug Fixes

- Fixed `CS0121` ambiguous method call errors by removing duplicate overloads without `CancellationToken`.
- Fixed build errors: added missing using for `InteractionResponseType` and fixed `WaitForAnyComponentAsync` parameter calls.
- Fixed critical and moderate issues in `PawSharp.Cache` package.
- Fixed stale release references in documentation and examples.
- Fixed `MemoryCacheProvider` and `RedisCacheProvider` guild-role/emoji tracking issues.
- Fixed error logging in empty catch blocks throughout the codebase.

### Documentation

- Updated README with new cache features documentation.
- Updated client README with default memory cache documentation.
- Removed individual version settings from all `.csproj` files to use centralized versioning.

### Internal / Tooling

- Removed PawSharp package txt files from repository.
- Added release-hygiene checks to prevent version drift.
- All package versions now centralized in `Directory.Build.props`.

---

## [1.0.0-alpha.4] - 2026-04-22

### Bug Fixes

- Fixed RedisCacheProvider guild-role and guild-emoji tracking by changing cache keys to guild-specific format and updating related methods to filter by guildId.
- Replaced int properties with enums in Guild class for VerificationLevel, DefaultMessageNotifications, ExplicitContentFilter, and SystemChannelFlags.
- Fixed code formatting inconsistencies in Guild.cs.
- Added property validation for Channel and Guild entities with ValidatedName properties enforcing length constraints.
- Made GuildMember.JoinedAt nullable to handle cases where the join timestamp may be missing.
- Fixed MemoryCacheProvider guild-role/emoji tracking by storing roles and emojis with guild-specific keys.
- Added error logging in empty catch blocks throughout the codebase for better debugging.
- Added Task.Run error handling in Interactivity for async operations.
- Added XML documentation to crypto methods in Curve25519, Ed25519, and HPKE implementations.
- Added caching for derived public keys in HPKE to avoid redundant X25519 scalar multiplication.
- Updated IEntityCache interface to match new method signatures with guildId parameter for roles and emojis.
- Added Debug.WriteLine logging for unrecognized emojis in pagination to improve observability.

### Internal / Tooling

- Removed individual version settings from all .csproj files to use centralized version from Directory.Build.props.
- Updated all documentation and README files to reference version 1.0.0-alpha.4.
- Updated User-Agent string in RestClient to reflect current version.

---

## [1.0.0-alpha.2] - 2026-04-08

### Changes

- Added dedicated alpha hardening coverage for `PawSharp.Client` builder, lifecycle, and DI registration paths.
- Added `SetupPawSharp(options)` as the recommended one-call DI setup entrypoint and added backward-compatible `AddPawSharpClient` overloads.
- Added configurable connect-time intent validation via `PawSharpOptions.IntentValidation` (`Off`, `Warn`, `Strict`).
- Added `EmbedTemplates` helpers (`Success`, `Error`, `Info`, `Warning`) for common embed response patterns.
- Added message forwarding support via `ForwardMessageAsync` in REST and client APIs using Discord's `message_reference` forward model.
- Added explicit `MessageReferenceType` support, including `FORWARD` references for snapshot-based forwards.
- Added structured REST rate-limit telemetry (`IRateLimitTelemetrySource` / `RateLimitTelemetryEvent`) and surfaced it on `DiscordClient` via `RateLimitObserved` for operational diagnostics.

### Fixes

- Updated stale release references in `examples/DashboardBot` and `assets/pawsharp-banner.svg` to `1.0.0-alpha.2`.
- Updated project banner target framework reference from .NET 8.0 to .NET 10.0.
- Fixed gateway intent-validation reflection logic to read registered handlers from the current dispatcher storage shape.
- Added duplicate-registration diagnostics to `InteractionHandler` with optional strict duplicate handling.

### Internal / Tooling

- Added explicit release-hygiene checks to `docs/VERSIONING_POLICY.md` for preventing version drift in docs/examples/assets.
- Added `tools/check-release-hygiene.ps1` and wired it into CI/release workflows to fail builds on stale example/asset version or TFM markers.

### Earlier alpha.2 stabilization updates

- All package project files now declare `1.0.0-alpha.2` directly in `<Version>` metadata, eliminating drift between source metadata and packed artifacts.
- `VoiceConnection` now uses structured `ILogger`-based logging instead of `Console.WriteLine` in receive, heartbeat, keep-alive, and control-payload parsing paths.
- Voice transport error handling no longer emits raw ad-hoc console exception strings; failures are routed through standard logging and connection-failure callbacks.
- `VoiceClient` now passes its logger into `VoiceConnection`, ensuring consistent voice diagnostics and central log configuration behavior.

---

## [1.0.0-alpha.1] - 2026-03-11

> **First .NET 10 release.** Migrates the entire solution — all library, test, and tooling projects — from `net8.0` to `net10.0`. All `Microsoft.Extensions.*` packages updated to `10.0.0`, test toolchain unified to xunit `2.9.2` / `Microsoft.NET.Test.Sdk` `17.12.0` / `FluentAssertions` `7.0.0`. Version bumped to `1.0.0-alpha.1` to reflect the breaking TFM change and start of the stable-API series.

### Breaking Changes

- **Target framework** changed from `net8.0` to `net10.0` across all packages. Consumers must target `net10.0` (or be `net10.0`-compatible) to consume PawSharp NuGet packages from this release onwards.
- `System.Net.Http.Json` package reference removed from `PawSharp.API` — the types are now provided directly by the .NET 10 BCL.

### New Features

- **`GetActivityInstanceAsync(ulong applicationId, string instanceId)`** — Fetches a running Discord embedded-application (Activity) instance.  Maps to `GET /applications/{application.id}/activity-instances/{instance.id}`.  Returns the new `ActivityInstance` entity (with `InstanceId`, `LaunchId`, `Location`, and `Users`) or `null` on a 404/error response.
- **`ActivityInstance`** entity (`PawSharp.Core.Entities`) — models the top-level activity-instance object returned by the endpoint above.
- **`ActivityLocation`** entity — nested object inside `ActivityInstance` describing the channel/guild context.
- **`ActivityLocationKind`** static class — string constants `"gc"` (guild channel) and `"pc"` (private/DM channel) for `ActivityLocation.Kind`.

### Changes

- All `Microsoft.Extensions.*` package references updated: `DependencyInjection`, `Logging`, `Logging.Console`, `Http`, `Options` → `10.0.0`.
- `StackExchange.Redis` updated to `2.8.16` in `PawSharp.Cache`.
- `Newtonsoft.Json` updated to `13.0.3` in `PawSharp.Core` and `PawSharp.API`.
- Test toolchain harmonised: `xunit` `2.9.2`, `xunit.runner.visualstudio` `2.8.2`, `Microsoft.NET.Test.Sdk` `17.12.0`, `Moq` `4.20.72`, `FluentAssertions` `7.0.0`.
- `BenchmarkDotNet` updated to `0.14.0` in `PawSharp.Benchmarks`.

### Fixes

- `DiscordRestClient` `User-Agent` header corrected from `0.11.0-alpha.1` to `1.0.0-alpha.1` to match the published package version.
- `PawSharp.Core` `Newtonsoft.Json` package reference corrected from `13.0.1` to `13.0.3` (consistent with `PawSharp.API`).

---

## [0.11.0-alpha.1] - 2026-03-10

> **Last `0.x.0` release on .NET 8.**  The next major version cycle will target **.NET 10** and take advantage of its new runtime improvements.  No further `0.x.0` versions are planned until that migration is complete.

Complete Opus audio encode/decode, full DAVE E2EE frame pipeline (with proper RTP framing and AAD), Speaking gate (op 5), comprehensive DocFX documentation site, command precondition system, `ReplyAsync` on `CommandContext`, and component-interaction waiting in `PawSharp.Interactivity`.

### New Features

**Command Preconditions** (`PawSharp.Commands`)

A first-class precondition system allows restricting command execution before any module code runs.  Implement `IPrecondition` or use the three built-in attributes:

- **`IPrecondition`** interface — `Task<PreconditionResult> CheckAsync(CommandContext)` contract for custom checks
- **`PreconditionResult`** — `FromSuccess()` / `FromError(string)` factory
- **`[RequireGuild]`** — blocks commands invoked outside a guild (DM invocations receive a `PreconditionFailedException` via `CommandErrored`)
- **`[RequirePermissions(ulong permissions)]`** — parses the computed `member.permissions` bitfield that Discord includes on `MESSAGE_CREATE` gateway events; `IgnoreAdmins = true` (default) lets administrator-bit holders through unconditionally
- **`[Cooldown(int maxUses, double perSeconds, CooldownBucketType)]`** — per-user / per-channel / per-guild / global rolling-window rate limiter backed by a `ConcurrentDictionary`; remaining time is surfaced in the `PreconditionFailedException` message
- **`CooldownBucketType`** enum — `User` (default), `Channel`, `Guild`, `Global`
- **`PreconditionFailedException`** — dedicated exception type delivered to `CommandErrored` when a precondition blocks execution; callers can `catch` on this type to distinguish it from command handler errors

Preconditions are evaluated in attribute declaration order; the first failure short-circuits execution.  Both method-level and class-level attributes are evaluated (class-level checked after method-level).

**`CommandContext.ReplyAsync`** (`PawSharp.Commands`)

- `ReplyAsync(string content)` — sends a Discord reply thread on the triggering message (sets `message_reference` with `message_id` and `channel_id`)
- `ReplyAsync(Embed embed)` — same but with an embed; renders inline under the original message in Discord clients

**`CommandContext.Member`** (`PawSharp.Commands`)

- `CommandContext.Member` — exposes the `GuildMember` received on the `MESSAGE_CREATE` gateway event; `null` for DM commands.  Provides `Member.Permissions` (computed bitfield) consumed by `[RequirePermissions]`, and `Member.Roles` for custom precondition logic.

**Component interaction waiting** (`PawSharp.Interactivity`)

Two new extension methods on `Message` allow waiting for a user to interact with a button or select menu on a specific message, replacing ad-hoc `TaskCompletionSource` boilerplate in command handlers:

- **`WaitForButtonAsync(DiscordClient, user?, customId?, timeout?)`** — registers an ephemeral `INTERACTION_CREATE` listener; resolves when an `INTERACTION_CREATE` event with `type = MessageComponent`, `component_type = Button (2)`, and `message.id` matching the callee arrives.  `user` and `customId` are optional filters.  The subscription is always disposed via `IDisposable` — no leak on timeout.
- **`WaitForSelectAsync(DiscordClient, user?, customId?, timeout?)`** — same semantics but accepts all select-menu component types (String, User, Role, Mentionable, Channel — component types 3 and 5–8).  `evt.Data.Values` contains the selected values.

Both methods return `InteractivityResult<InteractionCreateEvent>`, consistent with the existing `WaitForReactionAsync` pattern.  After receiving the interaction, callers should acknowledge it via `client.Interactions.DeferComponentAsync` or respond directly.

**Bug Fixes**

- **`CommandsExtension`** — `OnMessageCreate` previously built the `Message` manually, silently dropping `GuildId` (and other fields mapped in `ToMessage()`).  Now uses `evt.ToMessage()` so `CommandContext.GuildId` is correctly populated for guild-channel commands.  This also fixes `[RequireGuild]` which previously always returned "Not in a guild" even for guild messages.

**`VoiceConnection`** (`PawSharp.Voice`) — Opus encode/decode now fully functional

- `OpusEncoder` and `OpusDecoder` initialised at construction via **Concentus 1.1.0** (pure .NET, zero P/Invoke, zero extra dependencies)
  - `OpusEncoder.Create(48000, 1, OPUS_APPLICATION_VOIP)` — 64 kbps VOIP mono encoder
  - `OpusDecoder.Create(48000, 1)` — stereo-capable 48 kHz decoder (up to 120 ms per call)
- **PCM frame accumulation buffer** (`_pendingPcm: List<byte>`)
  — NAudio callback bytes are queued and flushed in exact 20 ms / 1 920-byte Opus frames; partial frames are never sent
- **`EncodeFrame(byte[])`** — converts one 1 920-byte (960-sample mono) PCM frame to a variable-length Opus packet via `encoder.Encode(short[], 0, 960, byte[], 0, 4000)`. Returns `Array.Empty<byte>()` on failure; callers skip the packet rather than transmitting silence.
- **`DecodeAudio(byte[])`** — decodes an incoming Opus packet back to 16-bit PCM via `decoder.Decode(byte[], 0, len, short[], 0, 5760, false)`. Supports up to 120 ms per call. Converts the resulting `short[]` to a PCM byte stream via `Buffer.BlockCopy`.

**RTP framing (RFC 3550 §5.1)**

- **`BuildRtpHeader()`** — synthesises the 12-byte fixed RTP header for each outgoing packet:
  - Version = 2, Padding = 0, Extension = 0, CSRC count = 0
  - Payload type = 120 (Opus, per RFC 7587)
  - Sequence number (big-endian `uint16`) — monotonically increases, wraps naturally
  - Timestamp (big-endian `uint32`, 48 kHz clock) — advances by 960 per packet
  - SSRC (big-endian `uint32`) — sourced from `_dave.LocalSsrc` (set from op 2 READY)
- **`TryParseRtpPacket()`** — extracts `ssrc`, `rtpHeader`, and encrypted payload from inbound packets; returns `false` for packets shorter than 12 bytes

**DAVE E2EE — RTP header as Additional Authenticated Data (AAD)**

- Outbound: `_dave.EncryptFrame(opusPacket, rtpHeader)` — the 12-byte RTP header is passed as DAVE AAD so the AES-128-GCM authentication tag covers the full wire metadata (sequence number, timestamp, SSRC)
- Inbound: `_dave.DecryptFrame(encryptedPayload, ssrc, rtpHeader)` — sender SSRC resolves the correct per-epoch key via `MLSState.GetSenderKey(ssrc)`; header bytes verify the AAD
- Wire format: `[12-byte RTP header][DAVE: 12-byte nonce || ciphertext || 16-byte GCM tag]`

**`SetSpeakingAsync(bool)`** (`VoiceConnection`) — Discord Speaking gate (op 5)

- Sends `{"op":5,"d":{"speaking":<0|1>,"delay":0,"ssrc":<ssrc>}}` to the voice gateway
- Idempotent: a `_speaking` guard prevents redundant transmissions
- `StartCapture()` now raises the gate; `StopCapture()` lowers it automatically
- Speaking state resets to `false` on every fresh WebSocket connection

**`VoiceConnection` receive loop hardened**

- Receive buffer enlarged from 4 096 → **8 192 bytes** to accommodate worst-case DAVE ciphertext (max Opus 120 ms frame = ~480 bytes; nonce + tag add 28 bytes; buffer is still generous)
- Binary messages now always parsed through `TryParseRtpPacket` before DAVE decryption, preventing a `CryptographicException` from a malformed packet crashing the loop

**DocFX documentation site** (`docs/`)

- Full `docfx.json` rewrite: `modern` template, custom metadata, global filter config, cross-references, `_appTitle`, `_appFaviconPath`, footer links
- Root `toc.yml` linking Introduction articles and API reference
- `docs/toc.yml` — structured guide navigation
- `index.md` — landing page with installation, quickstart, and package summary table
- `docs/VOICE_GUIDE.md` — end-to-end voice + DAVE guide with Opus send/receive examples
- `docs/API_REFERENCE.md` — per-namespace type index with cross-links
- `filterConfig.yml` — excludes internal implementation types from the API reference

### Changes

- `Concentus` version pinned to **1.1.0** in `PawSharp.Voice.csproj` (was `>= 1.0.4`; 1.0.4 never shipped to NuGet and caused `NU1603` restores)
- Root `README.md` updated: Opus TODO removed, voice quickstart updated with `SendAudioAsync` + `SetSpeakingAsync`, package table updated to 0.11.0-alpha.1
- `src/PawSharp.Voice/README.md` fully rewritten: real Opus codec examples, DAVE AAD diagram, RTP frame layout table, Speaking gate lifecycle
- All `<Version>` elements bumped from `0.10.0-alpha.3` → `0.11.0-alpha.1` in `src/Directory.Build.props` and each individual `.csproj`

### Bug Fixes

- **`CommandsExtension.OnMessageCreate`** — `GuildId` and other `ToMessage()` fields (attachments, embeds, roles) were dropped during manual `Message` construction; replaced with `evt.ToMessage()`
- Removed stale `// Note: OpusEncoder and OpusDecoder don't implement IDisposable` comment in `VoiceConnection.Dispose()` — Concentus encoders/decoders are GC-managed; the comment was misleading
- Removed stale `System.IO` using directive from `VoiceConnection.cs` (was never used)

### Public API Changes

| Symbol | Before | After |
|--------|--------|-------|
| `IPrecondition` | _(missing)_ | new interface in `PawSharp.Commands.Preconditions` |
| `PreconditionResult` | _(missing)_ | new class: `FromSuccess()`, `FromError(string)` |
| `PreconditionFailedException` | _(missing)_ | new exception type |
| `[RequireGuild]` | _(missing)_ | new precondition attribute |
| `[RequirePermissions(ulong)]` | _(missing)_ | new precondition attribute; `IgnoreAdmins` property |
| `[Cooldown(int, double, CooldownBucketType)]` | _(missing)_ | new precondition attribute |
| `CooldownBucketType` | _(missing)_ | new enum: `User`, `Channel`, `Guild`, `Global` |
| `CommandContext.Member` | _(missing)_ | `GuildMember?` from gateway event |
| `CommandContext.ReplyAsync(string)` | _(missing)_ | new method — Discord reply thread |
| `CommandContext.ReplyAsync(Embed)` | _(missing)_ | new method — Discord reply thread with embed |
| `Message.WaitForButtonAsync(…)` | _(missing)_ | new extension method |
| `Message.WaitForSelectAsync(…)` | _(missing)_ | new extension method |

---

## [0.10.0-alpha.3] - 2026-03-08

Full developer-ergonomics pass: interactions, components, embeds, voice, webhooks, presence, and gateway events.

### New Features

**`InteractionHandler`** (`PawSharp.Interactions`)
- `RegisterModal(string customId, Func<InteractionCreateEvent, Task>)` — separate registration for modal submissions (previously shared `RegisterComponent`)
- `DeferAsync(ulong id, string token, bool ephemeral = false)` — defers a slash command interaction (response type 5)
- `DeferComponentAsync(ulong id, string token)` — defers a component update without spinner (type 6)
- `RespondEphemeralAsync(ulong id, string token, string content)` — sends an ephemeral message in one call (type 4, flags=64)

**`InteractionResponseBuilder`** (`PawSharp.Interactions.Builders`)
- New fluent builder for `InteractionResponse` (types 4 and 7)
- `WithContent(string)`, `AddEmbed(Embed)`, `AddActionRow(MessageComponent)`, `AddActionRow(Action<ActionRowBuilder>)`
- `AsEphemeral(bool = true)`, `AsUpdateMessage(bool = true)`, `Build()`

**Component Builders** (`PawSharp.Interactions.Builders`)
- All builders (`ButtonBuilder`, `SelectMenuBuilder`, `ActionRowBuilder`, etc.) now return `PawSharp.Core.Entities` typed objects (`Button`, `SelectMenu`, `ActionRow`, …), making them directly compatible with `InteractionCallbackData.Components` and `CreateMessageRequest.Components`
- `ButtonBuilder.SetCustomEmoji(name, id, animated)` — sets a typed `Emoji` on a button
- `ButtonBuilder.SetSkuId(ulong)` — sets Premium (type 6) button with SKU ID
- `ButtonStyle.Premium = 6` added to `PawSharp.Core.Entities.ButtonStyle`
- `ActionRowBuilder` enforces max-5-component guard with `InvalidOperationException`
- New: `UserSelectMenuBuilder`, `RoleSelectMenuBuilder`, `MentionableSelectMenuBuilder`, `ChannelSelectMenuBuilder` (types 5–8); `ChannelSelectMenuBuilder.SetChannelTypes(params int[])`

**`InteractionExtensions`** (`PawSharp.Interactions.Extensions`)
- `GetOptionValue<T>` now traverses subcommand/subcommand-group option nesting automatically
- `GetSubcommandName()` — returns the name of the active sub-command (or null)

**`EmbedBuilder`** (`PawSharp.Core.Builders`)
- `WithColor(uint color)` overload (hex-friendly: `0xFF5733`)
- `WithoutFooter()`, `WithoutAuthor()`, `WithoutImage()`, `WithoutThumbnail()`, `ClearFields()` — convenience clear methods

**`PawSharpClientBuilder`** (`PawSharp.Client`)
- `WithPresence(activityName?, activityType = 0, status = "online", streamUrl?)` — configures the bot's initial presence/status

**`DiscordClient`** (`PawSharp.Client`)
- Sets presence on `READY` when configured via `WithPresence`
- 26 new `On…` event wrappers covering all gateway dispatch events previously missing:
  `OnVoiceServerUpdated`, `OnGuildEmojisUpdated`, `OnGuildStickersUpdated`, `OnGuildMembersChunked`, `OnGuildAuditLogEntryCreated`, `OnWebhooksUpdated`, `OnStageInstance{Created,Updated,Deleted}`, `OnScheduledEventUser{Added,Removed}`, `OnAutoModerationRule{Created,Updated,Deleted}`, `OnIntegration{Created,Updated,Deleted}`, `OnMessagePollVote{Added,Removed}`, `OnEntitlement{Created,Updated,Deleted}`, `OnThreadListSynced`, `OnThreadMemberUpdated`, `OnThreadMembersUpdated`, `OnApplicationCommandPermissionsUpdated`

**REST — Interaction & Webhook follow-ups** (`PawSharp.API`)
- `GetOriginalInteractionResponseAsync(applicationId, token)`
- `GetWebhookMessageAsync(webhookId, token, messageId, threadId?)`
- `EditWebhookMessageAsync(webhookId, token, messageId, request, threadId?)`
- `DeleteWebhookMessageAsync(webhookId, token, messageId, threadId?)`

**`VoiceConnection`** (`PawSharp.Voice`)
- `IsPlaying` is now automatically reset to `false` when audio playback finishes (`PlaybackStopped` event)
- New `StopPlayback()` method — stops the current stream and resets state cleanly

### Bug Fixes

- `InteractionHandler` was routing `ModalSubmit` interactions to `_componentHandlers` instead of the new `_modalHandlers` — now correctly routed
- `PawSharpOptions.cs` was missing `#nullable enable`, causing spurious CS8632 warnings

### Public API Changes

| Symbol | Before | After |
|--------|--------|-------|
| `InteractionHandler.RegisterModal` | _(missing)_ | new method |
| `InteractionHandler.DeferAsync` | _(missing)_ | new method |
| `InteractionHandler.DeferComponentAsync` | _(missing)_ | new method |
| `InteractionHandler.RespondEphemeralAsync` | _(missing)_ | new method |
| `InteractionResponseBuilder` | _(missing)_ | new class |
| `ButtonBuilder.Build()` return type | `PawSharp.Interactions.Models.MessageComponent` | `PawSharp.Core.Entities.Button` |
| `SelectMenuBuilder.Build()` return type | `PawSharp.Interactions.Models.MessageComponent` | `PawSharp.Core.Entities.SelectMenu` |
| `ActionRowBuilder.Build()` return type | `PawSharp.Interactions.Models.MessageComponent` | `PawSharp.Core.Entities.ActionRow` |
| `UserSelectMenuBuilder` | _(missing)_ | new builder, returns `UserSelectMenu` |
| `RoleSelectMenuBuilder` | _(missing)_ | new builder, returns `RoleSelectMenu` |
| `MentionableSelectMenuBuilder` | _(missing)_ | new builder, returns `MentionableSelectMenu` |
| `ChannelSelectMenuBuilder` | _(missing)_ | new builder, returns `ChannelSelectMenu` |
| `EmbedBuilder.WithColor(uint)` | _(missing)_ | new overload |
| `EmbedBuilder.Without*/ClearFields` | _(missing)_ | 5 new convenience methods |
| `PawSharpClientBuilder.WithPresence` | _(missing)_ | new method |
| `VoiceConnection.StopPlayback()` | _(missing)_ | new method |
| `IDiscordRestClient.GetOriginalInteractionResponseAsync` | _(missing)_ | new method |
| `IDiscordRestClient.Get/Edit/DeleteWebhookMessageAsync` | _(missing)_ | 3 new methods |

---

## [0.10.0-alpha.2] - 2026-03-07

Developer-ergonomics polish: error hooks, GC-safe singletons, and a simpler DI default.

### New Features

**`CommandsExtension.CommandErrored`** (`PawSharp.Commands`)
- New `Func<CommandErrorEventArgs, Task>? CommandErrored` property on `CommandsExtension`
- Assign a handler to receive full context when a command method throws (e.g. to send a user-facing error reply)
- Without a handler, the original behaviour is preserved: exception is logged at `Error` level and swallowed
- `CommandErrorEventArgs` exposes `CommandContext Context` and `Exception Exception`

**`AddPawSharpWithMemoryCache`** (`PawSharp.Client.Extensions`)
- New `services.AddPawSharpWithMemoryCache(options)` convenience overload
- Equivalent to `AddPawSharp(options, _ => new MemoryCacheProvider())`; removes the most common cause of runtime `InvalidOperationException: IEntityCache` on first bot startup

### Bug Fixes / Cleanup

- **`UseCommands()`** switched from `ConcurrentDictionary` to `ConditionalWeakTable` — consistent with `UseVoice()`, prevents the extension table from extending the `DiscordClient` lifetime beyond its own scope
- **`PawSharpClientBuilder.Build()`** — removed dead `InteractionHandler` local variable (`interactions`) that was created but never used (client creates its own instance internally)

### Public API Changes

| Symbol | Before | After |
|--------|--------|-------|
| `CommandsExtension.CommandErrored` | _(missing)_ | `Func<CommandErrorEventArgs, Task>?` |
| `CommandErrorEventArgs` | _(missing)_ | new type: `Context`, `Exception` |
| `AddPawSharpWithMemoryCache` | _(missing)_ | new extension method |

---

## [0.10.0-alpha.1] - 2026-03-07

API correctness, voice connection completion, and developer ergonomics improvements across the whole library. Includes one breaking change in interaction resolved-data types.

### Breaking Changes

- **`InteractionResolvedData` and `ResolvedData` keys changed from `string` to `ulong`** — Discord sends resolved-data maps with snowflake string keys. Both classes in `PawSharp.Gateway.Events` and `PawSharp.Core.Entities` previously used `Dictionary<string, T>`, requiring callers to `.ToString()` every lookup. They now use `Dictionary<ulong, T>` backed by the new `SnowflakeDictionaryJsonConverterFactory`, so lookups use the numeric ID directly.
- **`DeleteInviteAsync` return type changed from `Task<bool>` to `Task<Invite?>`** — Discord's `DELETE /invites/{code}` returns the deleted invite object. The method now returns that object (or `null` on failure) instead of a boolean.
- **`GetActiveThreadsAsync` return type changed from `Task<List<Channel>?>` to `Task<ActiveThreadsResponse?>`** — exposes the `threads` and `members` arrays from the Discord response instead of silently returning only a flat channel list.

### New Features

**Serialization — `SnowflakeDictionaryJsonConverterFactory`** (`PawSharp.Core.Serialization`)
- New `SnowflakeDictionaryJsonConverterFactory` / `SnowflakeDictionaryJsonConverter<TValue>` pair
- Converts any `Dictionary<ulong, TValue>` property to/from Discord's string-keyed JSON maps transparently
- Apply via `[JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]`

**Gateway** (`PawSharp.Gateway`)
- `IGatewayClient.SessionId` — exposes the opaque session ID received in the READY event
- `IGatewayClient.LastHeartbeatLatency` — round-trip `TimeSpan` measured from the last heartbeat/ACK pair; `null` until the first ACK
- `VoiceStateUpdateEvent.GuildId` — exposes the `guild_id` field from `VOICE_STATE_UPDATE` payloads, required for multi-guild voice session tracking
- `GatewayEvents.InteractionData.Components` — exposes modal text-input component data from `INTERACTION_CREATE` events

**REST API** (`PawSharp.API`)
- `DiscordRestClient` secondary constructor accepting `(HttpClient, PawSharpOptions, ILogger)` — creates a default `AdvancedRateLimiter` internally, fixing DI registration without a manually constructed rate limiter
- `BulkOverwriteGlobalApplicationCommandsAsync` now logs the Discord error body at `LogError` level on non-success responses (mirrors the existing behaviour of the guild variant)
- `CreateAutoModerationRuleRequest.EventType` / `TriggerType` — changed from `int` to `AutoModerationEventType` / `AutoModerationTriggerType`
- `ModifyAutoModerationRuleRequest.EventType` / `TriggerType` — changed from `int?` to `AutoModerationEventType?` / `AutoModerationTriggerType?`
- `CreateStageInstanceRequest.PrivacyLevel` — changed from `int?` to `StageInstancePrivacyLevel?`
- `ModifyStageInstanceRequest.PrivacyLevel` — changed from `int?` to `StageInstancePrivacyLevel?`
- New `ActiveThreadsResponse` model with `List<Thread> Threads` and `List<ThreadMember> Members`
- `ArchivedThreadsResponse.Threads` changed from `List<Channel>` to `List<Thread>` (exposes `ThreadMetadata`)

**Cache** (`PawSharp.Cache`)
- `IEntityCache` async overloads: `GetUserAsync`, `GetGuildAsync`, `GetChannelAsync`, `GetMessageAsync`, `GetGuildMemberAsync`, `GetRoleAsync`
- `MemoryCacheProvider` implements all async overloads via `Task.FromResult` (zero overhead for in-process use)
- `RedisCacheProvider` implements all async overloads using `StringGetAsync` for true async Redis I/O

**Voice** (`PawSharp.Voice`)
- `VoiceConnectionState` enum — `Disconnected`, `Connecting`, `Connected`, `Disconnecting`; replaces ad-hoc boolean checks
- `VoiceConnection.State` property tracks current connection state
- `VoiceConnection.ConnectAsync(endpoint, guildId, userId, sessionId, token)` — full implementation: strips `:80` suffix, opens `wss://{host}?v=8`, sends op 0 IDENTIFY, starts heartbeat and receive loops
- `VoiceConnection.ReconnectAsync()` — reconnects using stored handshake parameters without requiring the caller to track them
- `VoiceClient.ActiveConnections` — `IReadOnlyDictionary<ulong, VoiceConnection>` exposing all live connections keyed by channel ID
- `VoiceClient` handshake is now fully event-driven: `ConnectAsync` sends gateway op4 and returns; the WebSocket connection is completed when `VOICE_SERVER_UPDATE` arrives
- `UseVoice()` extension is now idempotent — returns the same `VoiceClient` per `DiscordClient` instance via a `ConditionalWeakTable` singleton cache

**Interactions** (`PawSharp.Interactions`)
- `GetOptionValue<ulong>` now correctly handles Discord snowflakes sent as JSON strings (e.g. User, Role, Channel option values) in addition to numeric JSON values

### Public API Changes

| Symbol | Before | After |
|--------|--------|-------|
| `InteractionResolvedData.Users` | `Dictionary<string, User>?` | `Dictionary<ulong, User>?` |
| `InteractionResolvedData.Members` | `Dictionary<string, GuildMember>?` | `Dictionary<ulong, GuildMember>?` |
| `InteractionResolvedData.Roles` | `Dictionary<string, Role>?` | `Dictionary<ulong, Role>?` |
| `InteractionResolvedData.Channels` | `Dictionary<string, Channel>?` | `Dictionary<ulong, Channel>?` |
| `InteractionResolvedData.Messages` | `Dictionary<string, Message>?` | `Dictionary<ulong, Message>?` |
| `InteractionResolvedData.Attachments` | `Dictionary<string, Attachment>?` | `Dictionary<ulong, Attachment>?` |
| `ResolvedData.*` (Core.Entities) | `Dictionary<string, T>?` | `Dictionary<ulong, T>?` |
| `DeleteInviteAsync` | `Task<bool>` | `Task<Invite?>` |
| `GetActiveThreadsAsync` | `Task<List<Channel>?>` | `Task<ActiveThreadsResponse?>` |
| `CreateAutoModerationRuleRequest.EventType` | `int` | `AutoModerationEventType` |
| `CreateAutoModerationRuleRequest.TriggerType` | `int` | `AutoModerationTriggerType` |
| `ModifyAutoModerationRuleRequest.EventType` | `int?` | `AutoModerationEventType?` |
| `ModifyAutoModerationRuleRequest.TriggerType` | `int?` | `AutoModerationTriggerType?` |
| `CreateStageInstanceRequest.PrivacyLevel` | `int?` | `StageInstancePrivacyLevel?` |
| `ModifyStageInstanceRequest.PrivacyLevel` | `int?` | `StageInstancePrivacyLevel?` |
| `ArchivedThreadsResponse.Threads` | `List<Channel>` | `List<Thread>` |
| `IGatewayClient` | _(no SessionId/Latency)_ | `SessionId`, `LastHeartbeatLatency` |
| `VoiceClient` | _(no ActiveConnections)_ | `ActiveConnections` |
| `IEntityCache` | sync-only | +6 async overloads |

---

## [0.7.0-alpha.1] - 2026-03-05

Full RFC 9420 MLS (Message Layer Security) implementation for Discord's DAVE E2EE protocol — voice connections are now end-to-end encrypted using the MLS ciphersuite `MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519`. This release adds a complete, from-scratch cryptographic stack built on top of .NET 8's built-in primitives with zero new NuGet dependencies.

### New Features

**DAVE E2EE — Full MLS Stack** (`PawSharp.Voice`)

*Cryptographic Primitives (no external dependencies — pure .NET 8)*

---

## [0.6.0-alpha.1] - 2026-03-04

### New Features

- **Voice connection support** (`PawSharp.Voice`)
  - Initial voice gateway integration with WebSocket connection and UDP audio
  - Basic RTP packet handling
  - Voice state management

### Changes

- **Project restructured** for modular package design
  - `PawSharp.Core` — entities, enums, builders, validation
  - `PawSharp.API` — REST client
  - `PawSharp.Gateway` — WebSocket gateway
  - `PawSharp.Client` — top-level client
  - `PawSharp.Voice` — voice support (initial)
  - `PawSharp.Commands` — prefix commands
  - `PawSharp.Interactions` — slash commands and components
  - `PawSharp.Interactivity` — pagination and waits
  - `PawSharp.Cache` — caching layer

---

## [0.5.0-alpha.1] - 2026-03-02

### New Features

- Initial Gateway implementation with WebSocket connection
- Basic REST API client with rate limiting
- Event system for gateway dispatch events
- Entity models for Discord API objects
- Command framework foundation
- Interaction handling basics

---

## [0.1.0-alpha.1] - 2026-02-25

### New Features

- Project scaffolding and initial package structure
- Basic Discord API models and enums
- Solution-wide build configuration

