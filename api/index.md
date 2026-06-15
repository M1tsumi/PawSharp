# API Reference

This section is generated automatically from the PawSharp source by DocFX.
Below is a quick map of the major namespaces.

---

## PawSharp.Client

The top-level entry point for the library.

| Type | Purpose |
|------|---------|
| `DiscordClient` | Main bot client. Wraps the gateway, REST client, and extension host. |
| `PawSharpClientBuilder` | Fluent builder — configure tokens, intents, caching, logging, and extensions before `Build()`. |
| `CacheManager` | In-memory LRU cache for guilds, channels, users, and messages. Thread-safe. |

---

## PawSharp.Core

Shared primitives and infrastructure used by every other package.

| Namespace | Purpose |
|-----------|---------|
| `PawSharp.Core.Entities` | POCO types for Discord objects — Guild, Channel, User, Message, Role, etc. |
| `PawSharp.Core.Enums` | Flags and values straight from the Discord API docs. |
| `PawSharp.Core.Builders` | `EmbedBuilder`, `AllowedMentionsBuilder`, and friends. |
| `PawSharp.Core.Exceptions` | `DiscordException`, `RateLimitException`, `GatewayException`. |
| `PawSharp.Core.Logging` | Thin wrapper around `Microsoft.Extensions.Logging`. |
| `PawSharp.Core.Metrics` | In-process counters for REST calls, gateway events, and cache hit rates. |
| `PawSharp.Core.Serialization` | System.Text.Json converters for Snowflakes, bit-field enums, ISO-8601. |
| `PawSharp.Core.Validation` | Precondition helpers, embed field limits, file size checks. |

---

## PawSharp.API

Typed HTTP wrappers for every Discord REST endpoint.

| Type | Purpose |
|------|---------|
| `DiscordRestClient` | Monolithic REST client — messages, guilds, members, roles, channels, threads, webhooks, interactions, auto-mod, scheduled events, voice regions, polls, entitlements, and more. |
| `AdvancedRateLimiter` | Bucket-based rate limiter with per-route and global limit tracking. |

---

## PawSharp.Gateway

WebSocket connection to the Discord gateway (v10).

| Type | Purpose |
|------|---------|
| `GatewayClient` | Manages the gateway WebSocket lifecycle — identify, heartbeat, resume, reconnect. |
| `ShardManager` | Spins up and monitors one `GatewayClient` per shard. |
| `ReconnectionManager` | Exponential backoff logic for unexpected disconnects. |
| `GatewayState` | Enum — `Disconnected`, `Connecting`, `Connected`, `Ready`, `Failed`. |

---

## PawSharp.Voice

Real-time voice connection with Opus audio and DAVE E2EE.

| Type | Purpose |
|------|---------|
| `VoiceClient` | Extension entry point. Manages the `ActiveConnections` dictionary. |
| `VoiceConnection` | Single voice channel connection — audio I/O, RTP framing, DAVE crypto via `DAVEProtocol`. |

Internal DAVE/MLS types (`MLSState`, `MLSKeySchedule`, `RatchetTree`, …) are
excluded from this reference by `filterConfig.yml` — they're implementation
details that aren't meant to be called directly.

---

## PawSharp.Interactions

Slash commands, context menus, modals, and autocomplete.

| Type | Purpose |
|------|---------|
| `InteractionHandler` | Registers and dispatches interaction handlers wired via attributes or fluent registration. |
| `SlashCommandBuilder` | Constructs `ApplicationCommand` payloads with options and localisation. |
| `ModalBuilder` | Builds modal JSON with text input components. |
| `ComponentInteractionContext` | Context object for button / select-menu handlers. |

---

## PawSharp.Commands

Prefix-based text commands (legacy, but still supported).

| Type | Purpose |
|------|---------|
| `CommandsExtension` | Scans assemblies for `[Command]`-annotated methods and handles `MESSAGE_CREATE` routing. |
| `CommandContext` | Carries message, guild, channel, author, and parsed arguments. |

---

## PawSharp.Interactivity

Awaitable response after a message or interaction.

| Type | Purpose |
|------|---------|
| `InteractivityExtension` | Pagination helpers (`GeneratePagesInContent`, `GeneratePagesInEmbed`). |
| Extension methods on `Message` | `WaitForMessageAsync`, `WaitForReactionAsync`, `WaitForButtonAsync`, `WaitForModalAsync` — cancellable with a timeout. |
| Extension methods on `Channel` | `SendPaginatedMessageAsync`, `ConfirmAsync`, `GetInputAsync`. |
