# Changelog

All notable changes to PawSharp are documented here.

---

## [0.6.0-alpha1] - Unreleased

Full Discord API surface coverage: emoji CRUD, application management, extended guild endpoints, gateway integration events, and a type-safe option value extension for slash commands.

### New Features

**Emoji REST Endpoints**
- `ListGuildEmojisAsync(guildId)` → `GET /guilds/{id}/emojis`
- `GetGuildEmojiAsync(guildId, emojiId)` → `GET /guilds/{id}/emojis/{emoji.id}`
- `CreateGuildEmojiAsync(guildId, request, reason?)` → `POST /guilds/{id}/emojis`
- `ModifyGuildEmojiAsync(guildId, emojiId, request, reason?)` → `PATCH /guilds/{id}/emojis/{emoji.id}`
- `DeleteGuildEmojiAsync(guildId, emojiId, reason?)` → `DELETE /guilds/{id}/emojis/{emoji.id}`
- `ListApplicationEmojisAsync(applicationId)` → `GET /applications/{id}/emojis` (unwraps `{ "items": [...] }`)
- `GetApplicationEmojiAsync(applicationId, emojiId)` → `GET /applications/{id}/emojis/{emoji.id}`
- `CreateApplicationEmojiAsync(applicationId, request)` → `POST /applications/{id}/emojis`
- `ModifyApplicationEmojiAsync(applicationId, emojiId, request)` → `PATCH /applications/{id}/emojis/{emoji.id}`
- `DeleteApplicationEmojiAsync(applicationId, emojiId)` → `DELETE /applications/{id}/emojis/{emoji.id}`

**New Request/Response Models**
- `CreateGuildEmojiRequest` — `name`, `image` (base64), `roles`
- `ModifyGuildEmojiRequest` — `name?`, `roles?`
- `CreateApplicationEmojiRequest` — `name`, `image`
- `ModifyApplicationEmojiRequest` — `name`
- `ApplicationEmojiListResponse` — response wrapper for application emoji list endpoint
- `EditCurrentApplicationRequest` — `description`, `icon`, `cover_image`, `tags`, `interactions_endpoint_url`, `event_webhooks_url`, `event_webhooks_status`, `event_webhooks_types`, `flags`, `role_connections_verification_url`, `custom_install_url`
- `GuildPruneResult` — `pruned` count returned by prune endpoints
- `BeginGuildPruneRequest` — `days?`, `compute_prune_count?`, `include_roles?`
- `BulkGuildBanRequest` — `user_ids`, `delete_message_seconds?`
- `BulkGuildBanResponse` — `banned_users`, `failed_users`
- `ModifyGuildIncidentActionsRequest` — `invites_disabled_until?`, `dms_disabled_until?`
- `GuildIntegration` — minimal integration object returned by `GET /guilds/{id}/integrations`

**Application Management REST Endpoints**
- `GetCurrentApplicationAsync()` → `GET /applications/@me` — returns the current application object
- `EditCurrentApplicationAsync(request)` → `PATCH /applications/@me` — edits the current application

**Extended Guild REST Endpoints**
- `GetGuildInvitesAsync(guildId)` → `GET /guilds/{id}/invites`
- `GetGuildIntegrationsAsync(guildId)` → `GET /guilds/{id}/integrations`
- `DeleteGuildIntegrationAsync(guildId, integrationId, reason?)` → `DELETE /guilds/{id}/integrations/{id}`
- `GetGuildPruneCountAsync(guildId, days?, includeRoles?)` → `GET /guilds/{id}/prune`
- `BeginGuildPruneAsync(guildId, request, reason?)` → `POST /guilds/{id}/prune`
- `BulkGuildBanAsync(guildId, request, reason?)` → `POST /guilds/{id}/bulk-ban`
- `GetGuildRoleAsync(guildId, roleId)` → `GET /guilds/{id}/roles/{role.id}`
- `GetGuildRoleMemberCountsAsync(guildId)` → `GET /guilds/{id}/roles/member-counts`
- `ModifyGuildIncidentActionsAsync(guildId, request)` → `PUT /guilds/{id}/incident-actions`
- `GetCurrentUserGuildMemberAsync(guildId)` → `GET /users/@me/guilds/{guild.id}/member`

**Extended Reaction REST Endpoints**
- `DeleteAllReactionsAsync(channelId, messageId)` → `DELETE /channels/{id}/messages/{id}/reactions`
- `DeleteAllReactionsForEmojiAsync(channelId, messageId, emoji)` → `DELETE /channels/{id}/messages/{id}/reactions/{emoji}`

**New Gateway Events**
- `APPLICATION_COMMAND_PERMISSIONS_UPDATE` → dispatched as `ApplicationCommandPermissionsUpdateEvent` (id, applicationId, guildId, permissions)
- `INTEGRATION_CREATE` → dispatched as `IntegrationCreateEvent` (id, guildId, name, type, enabled, applicationId?)
- `INTEGRATION_UPDATE` → dispatched as `IntegrationUpdateEvent` (id, guildId, name, type, enabled, applicationId?)
- `INTEGRATION_DELETE` → dispatched as `IntegrationDeleteEvent` (id, guildId, applicationId?)

**`InteractionExtensions` helper (`PawSharp.Interactions.Extensions`)**
- `interaction.GetOptionValue<T>(name)` — type-safe extraction of a slash command option value by name; supports `string`, `bool`, `int`, `long`, `ulong`, `double`, `float`, and any JSON-deserializable type
- `options.GetOptionValue<T>(name)` — overload for subcommand option lists
- `interaction.FindOption(name)` — returns the raw `ApplicationCommandInteractionDataOption` for advanced access

---

## [0.5.0-alpha14] - Unreleased

Application Command completeness, OAuth2 REST endpoints, interaction follow-up messages, resolved data in interactions, and Testing Bot demonstrations of string/integer options and deferred responses.

### New Features

**Application Command Localization & Contexts**
- `ApplicationCommand` entity: added `NameLocalizations`, `DescriptionLocalizations`, `IntegrationTypes`, `Contexts`
- `ApplicationCommandOption` entity: added `NameLocalizations`, `DescriptionLocalizations`, `Focused` (for autocomplete)
- `ApplicationCommandType` enum: added `PrimaryEntryPoint = 4` (for Activities)
- `CreateApplicationCommandRequest` model: added `NameLocalizations`, `DescriptionLocalizations`, `DefaultMemberPermissions`, `DmPermission`, `IntegrationTypes`, `Contexts`, `Nsfw` — full parity with Discord's Create/Edit Application Command endpoints

**OAuth2 REST Endpoints**
- `GetCurrentBotApplicationInfoAsync()` → `GET /oauth2/applications/@me` — returns the bot's `Application` object
- `GetCurrentAuthorizationInfoAsync()` → `GET /oauth2/@me` — returns `OAuth2Info` with `application`, `scopes`, `expires`, `user`

**Interaction Follow-Up Messages**
- `CreateFollowupMessageAsync(applicationId, token, CreateMessageRequest)` → `POST /webhooks/{id}/{token}`
- `GetFollowupMessageAsync(applicationId, token, messageId)` → `GET /webhooks/{id}/{token}/messages/{msg_id}`
- `EditFollowupMessageAsync(applicationId, token, messageId, EditMessageRequest)` → `PATCH /webhooks/{id}/{token}/messages/{msg_id}`
- `DeleteFollowupMessageAsync(applicationId, token, messageId)` → `DELETE /webhooks/{id}/{token}/messages/{msg_id}`
- `InteractionHandler` convenience wrappers: `CreateFollowupAsync`, `EditFollowupAsync`, `DeleteFollowupAsync` (old `FollowupAsync` marked `[Obsolete]`)

**Resolved Interaction Data**
- New `ResolvedData` class with `Users`, `Members`, `Roles`, `Channels`, `Messages`, `Attachments` dictionaries
- `InteractionData.Resolved` property — enables USER, ROLE, CHANNEL, MENTIONABLE, ATTACHMENT option types to return actual objects
- `InteractionData.Components` property added for modal submit data

**`InteractionCallbackType` Enum (`PawSharp.API.Models`)**
- `Pong = 1`, `ChannelMessageWithSource = 4`, `DeferredChannelMessageWithSource = 5`, `DeferredUpdateMessage = 6`, `UpdateMessage = 7`, `ApplicationCommandAutocompleteResult = 8`, `Modal = 9`, `PremiumRequired = 10`, `LaunchActivity = 12`

**`CreateMessageRequest` Enhancements**
- Added `Flags` (int?) — supports `SUPPRESS_EMBEDS = 4`, `SUPPRESS_NOTIFICATIONS = 4096`, etc.
- Added `StickerIds` (List<ulong>?) — up to 3 server stickers
- Added `Nonce` (string?) and `EnforceNonce` (bool?) — message deduplication support

### Changes

**Testing Bot**
- Added `/greet` slash command demonstrating a required STRING option with `MinLength`/`MaxLength` and optional STRING option with predefined `Choices`
- Added `/roll` slash command demonstrating an INTEGER option with `MinValue`/`MaxValue` and a **deferred response + follow-up message** pattern

---

## [0.5.0-alpha13] - February 22, 2026

Developer Experience & API Completeness — typed message components, a fluent embed builder, missing REST endpoints (reactions, invites, guild templates, widget/welcome-screen controls), and a full set of presence and channel flag enums.

### New Features

**Typed Message Component Hierarchy**
- New `MessageComponent` abstract base class with polymorphic JSON converter (`MessageComponentJsonConverter`) in `PawSharp.Core.Entities`
- Concrete types: `ActionRow`, `Button`, `SelectMenu` / `StringSelectMenu`, `UserSelectMenu`, `RoleSelectMenu`, `MentionableSelectMenu`, `ChannelSelectMenu`, `TextInput`, `UnknownComponent`
- `SelectOption` and `SelectDefaultValue` supporting types
- `ComponentType` enum (ActionRow=1 … ChannelSelect=8), `ButtonStyle` enum (Primary=1 … Premium=6), `TextInputStyle` enum (Short=1, Paragraph=2)
- `Message.Components` is now `List<MessageComponent>?` — previously `List<object>?`

**EmbedBuilder**
- New fluent `EmbedBuilder` in `PawSharp.Core.Builders`
- Methods: `WithTitle`, `WithDescription`, `WithUrl`, `WithColor(int)`, `WithColor(r,g,b)`, `WithTimestamp()`, `WithTimestamp(DateTimeOffset)`, `WithFooter`, `WithImage`, `WithThumbnail`, `WithAuthor`, `AddField`, `Build()`
- Enforces Discord limits at build-time: title ≤ 256, description ≤ 4 096, ≤ 25 fields, field name ≤ 256, field value ≤ 1 024, footer ≤ 2 048, author name ≤ 256
- `Build()` throws `InvalidOperationException` if no visible content is set

**New Flags Enums**
- `MessageFlags` `[Flags]` enum: Crossposted, IsCrosspost, SuppressEmbeds, SourceMessageDeleted, Urgent, HasThread, Ephemeral, Loading, FailedToMentionSomeRoles, SuppressNotifications, IsVoiceMessage
- `ChannelFlags` `[Flags]` enum: Pinned, RequireTag, HideMediaDownloadOptions
- `AttachmentFlags` `[Flags]` enum: IsRemix
- `GuildMemberFlags` `[Flags]` enum: DidRejoin, CompletedOnboarding, BypassesVerification, StartedOnboarding, IsGuest, StartedHomeActions, CompletedHomeActions, AutomodQuarantinedUsername, DmSettingsUpsellAcknowledged
- `Message.Flags` is now `MessageFlags?` — previously `int?`

**New REST Endpoints**
- `GetReactionsAsync(channelId, messageId, emoji, type?, after?, limit?)` — paginated reaction user list with optional type filter and cursor pagination
- `FollowAnnouncementChannelAsync(channelId, webhookChannelId) → FollowedChannel` — POST `channels/{id}/followers`
- `GetGuildPreviewAsync(guildId) → GuildPreview` — public preview for discoverable guilds
- `GetGuildWidgetSettingsAsync(guildId) → GuildWidgetSettings`
- `ModifyGuildWidgetAsync(guildId, request) → GuildWidgetSettings`
- `GetGuildVanityUrlAsync(guildId) → VanityUrl`
- `GetGuildWelcomeScreenAsync(guildId) → WelcomeScreen`
- `ModifyGuildWelcomeScreenAsync(guildId, request) → WelcomeScreen`
- `ModifyGuildChannelPositionsAsync(guildId, positions) → bool`
- `ModifyGuildRolePositionsAsync(guildId, positions) → List<Role>`
- `GetInviteAsync(code, withCounts?, withExpiration?, guildScheduledEventId?) → Invite`
- `DeleteInviteAsync(code, reason?) → bool`
- Guild Templates (7 methods): `GetGuildTemplatesAsync`, `GetGuildTemplateAsync`, `CreateGuildFromTemplateAsync`, `CreateGuildTemplateAsync`, `SyncGuildTemplateAsync`, `ModifyGuildTemplateAsync`, `DeleteGuildTemplateAsync`

**New Entity Types**
- `GuildPreview` — Id, Name, Icon, Splash, DiscoverySplash, Emojis, Features, ApproximateMemberCount, ApproximatePresenceCount, Description, Stickers
- `GuildWidgetSettings` — Enabled, ChannelId
- `WelcomeScreen` — Description, `List<WelcomeScreenChannel>`
- `WelcomeScreenChannel` — ChannelId, Description, EmojiId, EmojiName
- `FollowedChannel` — ChannelId, WebhookId
- `VanityUrl` — Code, Uses

**New Request Models**
- `CreateGuildTemplateRequest`, `ModifyGuildTemplateRequest`, `CreateGuildFromTemplateRequest`
- `ModifyGuildWidgetRequest`
- `ModifyGuildWelcomeScreenRequest`, `WelcomeScreenChannelRequest`
- `ModifyChannelPositionRequest` (Id, Position, LockPermissions, ParentId)
- `ModifyRolePositionRequest` (Id, Position)

### Changes

- **`Message.Components`** — type changed from `List<object>?` to `List<MessageComponent>?`; deserializes automatically via `MessageComponentJsonConverter`
- **`Message.Flags`** — type changed from `int?` to `MessageFlags?`
- **`ModalBuilder.AddTextInput`** — `style` parameter changed from `int` (defaulting to `1`) to `TextInputStyle` (defaulting to `TextInputStyle.Short`); provides compile-time safety
- Component model classes (`MessageComponent`, `ActionRow`, `Button`, `SelectMenu`, `SelectOption`, `TextInput`) have been moved from `PawSharp.API.Models` to `PawSharp.Core.Entities`; the `PawSharp.Core.Entities` namespace is already re-exported from `PawSharp.API` so existing code referencing the old namespace may need a `using` update

### Breaking Changes

| Symbol | Before | After |
|---|---|---|
| `Message.Components` | `List<object>?` | `List<MessageComponent>?` |
| `Message.Flags` | `int?` | `MessageFlags?` |
| `ModalBuilder.AddTextInput(…, style, …)` | `int style = 1` | `TextInputStyle style = TextInputStyle.Short` |
| `TextInput.Style` | `int` | `TextInputStyle` |
| Component models namespace | `PawSharp.API.Models` | `PawSharp.Core.Entities` |

---

## [0.5.0-alpha12] - February 20, 2026

Full Discord API v10 coverage across REST and Gateway — polls, monetization, soundboard, onboarding, role connections, and 28 previously-missing gateway events.

### New Features

**Polls API**
- New `Poll`, `PollMedia`, `PollAnswer`, `PollResults`, `PollAnswerCount`, and `PollLayoutType` entity types in `PawSharp.Core`
- `Message.Poll` property — messages can now carry an attached poll
- `CreateMessageRequest.Poll` field — send a poll with a new message via `CreatePollRequest` / `PollMediaRequest` / `PollAnswerRequest`
- `GetAnswerVotersAsync(channelId, messageId, answerId, limit?, after?)` — paginated list of users who voted on an answer
- `EndPollAsync(channelId, messageId)` — immediately expire a poll and return the final message

**New Gateway Intents (alpha12)**
- `GatewayIntents.GuildMessagePolls` (1 << 24) — guild poll vote events
- `GatewayIntents.DirectMessagePolls` (1 << 25) — DM poll vote events
- `AllNonPrivileged` and `All` composite flags updated to include both new intents

**Monetization (SKUs, Entitlements, Subscriptions)**
- `ListSkusAsync(applicationId)` — list all SKUs for an application
- `ListEntitlementsAsync` — paginated with full filter support (userId, skuIds, before, after, limit, guildId, excludeEnded)
- `GetEntitlementAsync(applicationId, entitlementId)`
- `CreateTestEntitlementAsync` / `DeleteTestEntitlementAsync` — test entitlement management
- `ConsumeEntitlementAsync` — mark a consumable entitlement as consumed
- `ListSkuSubscriptionsAsync` / `GetSkuSubscriptionAsync` — read subscription records for a SKU

**Soundboard API**
- `ListDefaultSoundboardSoundsAsync()` — Discord's built-in default sounds
- `ListGuildSoundboardSoundsAsync(guildId)` — all custom sounds for a guild
- `GetGuildSoundboardSoundAsync(guildId, soundId)`
- `CreateGuildSoundboardSoundAsync` / `ModifyGuildSoundboardSoundAsync` / `DeleteGuildSoundboardSoundAsync`
- New request models: `CreateGuildSoundboardSoundRequest`, `ModifyGuildSoundboardSoundRequest`

**Guild Onboarding API**
- New `GuildOnboarding`, `OnboardingPrompt`, `OnboardingPromptOption`, `OnboardingMode`, `OnboardingPromptType` entity types
- `GetGuildOnboardingAsync(guildId)`
- `ModifyGuildOnboardingAsync(guildId, request)` — update prompts, default channels, mode, and enabled flag
- New request models: `ModifyGuildOnboardingRequest`, `OnboardingPromptRequest`, `OnboardingPromptOptionRequest`

**Application Role Connection Metadata**
- New `ApplicationRoleConnectionMetadata` and `ApplicationRoleConnectionMetadataType` entity types
- `GetApplicationRoleConnectionMetadataAsync(applicationId)`
- `UpdateApplicationRoleConnectionMetadataAsync(applicationId, records)` — PUT up to 5 metadata records

**Guild Member Improvements**
- `SearchGuildMembersAsync(guildId, query, limit?)` — search guild members by username/nickname
- `ModifyCurrentMemberAsync(guildId, nick)` — update the bot's own nickname in a guild

**28 New Gateway Events (alpha12)**

| Event | Class |
|---|---|
| `GUILD_SCHEDULED_EVENT_CREATE` | `GuildScheduledEventCreateEvent` |
| `GUILD_SCHEDULED_EVENT_UPDATE` | `GuildScheduledEventUpdateEvent` |
| `GUILD_SCHEDULED_EVENT_DELETE` | `GuildScheduledEventDeleteEvent` |
| `GUILD_SCHEDULED_EVENT_USER_ADD` | `GuildScheduledEventUserAddEvent` |
| `GUILD_SCHEDULED_EVENT_USER_REMOVE` | `GuildScheduledEventUserRemoveEvent` |
| `AUTO_MODERATION_RULE_CREATE` | `AutoModerationRuleCreateEvent` |
| `AUTO_MODERATION_RULE_UPDATE` | `AutoModerationRuleUpdateEvent` |
| `AUTO_MODERATION_RULE_DELETE` | `AutoModerationRuleDeleteEvent` |
| `AUTO_MODERATION_ACTION_EXECUTION` | `AutoModerationActionExecutionEvent` |
| `STAGE_INSTANCE_CREATE` | `StageInstanceCreateEvent` |
| `STAGE_INSTANCE_UPDATE` | `StageInstanceUpdateEvent` |
| `STAGE_INSTANCE_DELETE` | `StageInstanceDeleteEvent` |
| `GUILD_AUDIT_LOG_ENTRY_CREATE` | `GuildAuditLogEntryCreateEvent` |
| `ENTITLEMENT_CREATE` | `EntitlementCreateEvent` |
| `ENTITLEMENT_UPDATE` | `EntitlementUpdateEvent` |
| `ENTITLEMENT_DELETE` | `EntitlementDeleteEvent` |
| `MESSAGE_POLL_VOTE_ADD` | `MessagePollVoteAddEvent` |
| `MESSAGE_POLL_VOTE_REMOVE` | `MessagePollVoteRemoveEvent` |
| `GUILD_SOUNDBOARD_SOUND_CREATE` | `GuildSoundboardSoundCreateEvent` |
| `GUILD_SOUNDBOARD_SOUND_UPDATE` | `GuildSoundboardSoundUpdateEvent` |
| `GUILD_SOUNDBOARD_SOUND_DELETE` | `GuildSoundboardSoundDeleteEvent` |
| `GUILD_SOUNDBOARD_SOUNDS_UPDATE` | `GuildSoundboardSoundsUpdateEvent` |
| `SUBSCRIPTION_CREATE` | `SubscriptionCreateEvent` |
| `SUBSCRIPTION_UPDATE` | `SubscriptionUpdateEvent` |
| `SUBSCRIPTION_DELETE` | `SubscriptionDeleteEvent` |
| `MESSAGE_DELETE_BULK` | `MessageDeleteBulkEvent` |
| `INVITE_CREATE` | `InviteCreateEvent` |
| `INVITE_DELETE` | `InviteDeleteEvent` |
| `WEBHOOKS_UPDATE` | `WebhooksUpdateEvent` |

All 28 events are fully wired in `GatewayClient.HandleDispatchEventAsync`.

### Changes
- `Version` bumped from `0.5.0-alpha11` to `0.5.0-alpha12` in `Directory.Build.props`
- `GatewayIntents.AllNonPrivileged` now includes `GuildMessagePolls` and `DirectMessagePolls`

### Notes
- Voice integration intentionally excluded — see `PawSharp.Voice` for optional voice support

---

## [0.5.0-alpha11] - 2025

Gateway event coverage, DI hardening, interaction routing, REST endpoint parity, and cache sync improvements.

### New Features

**DI Hardening**
- Introduced `IGatewayClient` interface enabling constructor injection and unit-test mocking of the gateway
- Added `AddPawSharp()` `IServiceCollection` extension for single-call bot DI setup (`PawSharpServiceCollectionExtensions`)
- `DiscordClient` now accepts injected `IGatewayClient` instead of constructing `GatewayClient` internally
- `CacheManager.SubscribeToGateway` updated to accept `IGatewayClient`

**Missing Gateway Events (alpha11)**
- Added 8 new event classes: `GuildRoleCreateEvent`, `GuildRoleUpdateEvent`, `GuildRoleDeleteEvent`, `GuildMembersChunkEvent`, `GuildStickersUpdateEvent`, `MessageReactionRemoveEmojiEvent`, `GuildIntegrationsUpdateEvent`, `UserUpdateEvent`
- All new events dispatched in `GatewayClient.HandleDispatchEventAsync`

**Cache-Gateway Sync**
- `CacheManager` now subscribes to and handles all new role/sticker/thread/user-update events
- Guild role additions/updates/deletions are reflected in both the guild entity and the flat roles cache
- Guild stickers list is kept consistent on `GUILD_STICKERS_UPDATE`
- Thread channels cached and evicted on `THREAD_CREATE`/`THREAD_UPDATE`/`THREAD_DELETE`
- Self-user fields refreshed on `USER_UPDATE`

**REST Endpoint Parity**
- Added Stage Instance endpoints: Create, Get, Modify, Delete
- Added Sticker endpoints: Get, GetNitroStickerPacks, GetGuildStickers, GetGuildSticker, CreateGuildSticker (multipart), ModifyGuildSticker, DeleteGuildSticker
- Added `CreateDmAsync`, `CrosspostMessageAsync`, `EditChannelPermissionsAsync`
- Added `GetGatewayBotAsync` returning `GatewayBotInfo` with `SessionStartLimit`
- Added `GetVoiceRegionsAsync`, `GetGuildVoiceRegionsAsync`
- Added `GetCurrentUserConnectionsAsync` returning `List<UserConnection>`
- New entity classes: `GatewayBotInfo`, `SessionStartLimit`, `VoiceRegion`, `UserConnection`

**Interaction Handler (alpha11)**
- `InteractionHandler` constructor now accepts `IDiscordRestClient` (was `DiscordRestClient`)
- Added `RegisterAutocomplete`, `RegisterUserContextMenu`, `RegisterMessageContextMenu`
- `HandleInteractionAsync` routes on typed `InteractionType` enum (no more magic integers)
- Autocomplete handler auto-responds with `ApplicationCommandAutocompleteResult`
- Added `ModalBuilder` fluent builder (`WithCustomId`, `WithTitle`, `AddTextInput`, `Build`, `BuildResponse`)
- Added `InteractionType`, `InteractionResponseType` enums

**Event Model Improvements**
- `GuildCreateEvent.ToGuild()` now maps all available fields: `Splash`, `Banner`, `Description`, `VanityUrlCode`, `PremiumTier`, `PremiumSubscriptionCount`, `MemberCount`, `ApproximateMemberCount`, `PreferredLocale`, `Stickers`
- `ChannelCreateEvent`/`ChannelUpdateEvent.ToChannel()` now maps all available fields: `Position`, `Topic`, `Nsfw`, `Bitrate`, `UserLimit`, `RateLimitPerUser`, `ParentId`, `LastMessageId`, `RtcRegion`, `LastPinTimestamp`

**ShardManager**
- Added `ConnectedShardCount` property (number of shards in `Connected` state)
- Added `CalculateRecommendedShardCountAsync()` — queries `GET /gateway/bot` to get Discord's recommended shard count; falls back to local heuristic if REST client is unavailable
- `ShardManager` constructor accepts optional `IDiscordRestClient` parameter

**DiscordClient Convenience API**
- `SendMessageAsync(ulong channelId, string content)` and `SendMessageAsync(ulong channelId, CreateMessageRequest)` delegates to REST
- `GetCurrentUserAsync()` returns typed `User?`
- 8 typed event helper methods: `OnMessageCreated`, `OnMessageUpdated`, `OnMessageDeleted`, `OnGuildAvailable`, `OnGuildMemberJoined`, `OnGuildMemberLeft`, `OnInteractionCreated`, `OnReady`

**Test Coverage**
- New `PawSharp.Interactions.Tests` project — 8 tests covering slash commands, components, autocomplete, context menus, modal submit routing
- `PawSharp.API.Tests` — 9 new tests for all alpha11 REST endpoints (Stage Instance, Sticker, DM, GatewayBot, VoiceRegions, Crosspost, Channel Permissions, User Connections)
- `PawSharp.Gateway.Tests` — 9 new tests for alpha11 gateway event deserialization and `EventDispatcher` routing

### Changes
- `Version` bumped from `0.5.0-alpha8` to `0.5.0-alpha11` in `Directory.Build.props`
- `PawSharp.Client.csproj` now explicitly references `Microsoft.Extensions.DependencyInjection` 8.0.0
- `IGatewayClient` exposes `VoiceStateUpdate`, `VoiceServerUpdate` events and `SendVoiceStateUpdateAsync` for use by `VoiceClient`

### Bug Fixes
- Fixed `RestClient.SendRequestAsync` method signature corruption caused by a previous code generation artifact
- Removed duplicate `ApplicationCommandType` enum definition (existed in both `PawSharp.Interactions.Models` and `InteractionHandler.cs`)

---

## [0.5.0-alpha10] - January 20, 2026

Sharding and scalability enhancements for large-scale bot deployments, plus Redis distributed caching implementation.

### New Features

**Sharding Improvements**
- Added `ShardStatus` enum for tracking individual shard states (Disconnected, Connecting, Connected, Reconnecting, Failed)
- Implemented automatic per-shard reconnection with status monitoring
- Added `EventDispatcher` to `ShardManager` for shard-level events (`ShardConnectedEvent`, `ShardDisconnectedEvent`, `ShardFailedEvent`)
- Enhanced `ShardManager` with real-time status tracking and diagnostics methods (`GetShardStatus`, `GetAllShardStatuses`, `ConnectedShardCount`)
- Added `CalculateRecommendedShardCount()` static method for auto-sharding based on guild count
- Improved logging for shard state changes and reconnection attempts

**Redis Distributed Caching**
- Implemented `RedisCacheProvider` with full `IEntityCache` interface support
- Added StackExchange.Redis dependency for high-performance Redis operations
- Configurable Redis connection options (connection string, password, database, timeouts)
- Automatic JSON serialization/deserialization with System.Text.Json
- Sorted set-based message indexing for efficient channel message retrieval
- Comprehensive cache statistics and monitoring
- Thread-safe operations with proper connection management

**Cache Provider Architecture**
- Pluggable cache provider interface (`IEntityCache`) supporting multiple backends
- Unified API for in-memory and Redis caching
- Dependency injection support for both cache providers
- Comprehensive test coverage with `PawSharp.Cache.Tests` project

**Developer Experience**
- Better error handling and diagnostics for sharding operations
- Updated documentation with Redis cache setup and configuration examples
- Added cache provider selection guide in README
- Enhanced PawSharp.Cache README with Redis-specific examples
- Improved error handling and connection resilience
- Structured event system for multi-shard management

### Changes
- `ShardManager` now tracks and reports individual shard statuses
- Automatic reconnection logic integrated into shard lifecycle

### Bug Fixes
- None

---

## [0.5.0-alpha9] - January 14, 2026

Production hardening release with infrastructure improvements, better reliability, and enhanced developer experience.

### New Features

**Gateway Reliability**
- Added zlib compression support with automatic negotiation
- Configurable missed heartbeat acknowledgment limits
- Better error reporting in identify/resume flow
- GUILD_EMOJIS_UPDATE event handling

**REST Client Improvements**
- Integrated AdvancedRateLimiter for per-route bucket management
- Configurable request timeouts and cancellation support
- Audit log reason header support
- Improved 429 handling using rate limiter data

**Cache Enhancements**
- Emoji caching with CacheEmoji() and GetGuildEmojis() methods
- CacheStats class for monitoring and statistics
- Bounds enforcement for emoji cache in MemoryCacheProvider
- GUILD_EMOJIS_UPDATE event integration

**Event System**
- EventDispatcher converted to async with middleware support
- Use() method for registering middleware functions
- IDisposable event subscriptions with cleanup
- DispatchRawAsync() for raw JSON dispatching
- Enhanced error handling and logging

**Application Command Permissions**
- Added permission models: ApplicationCommandPermissions, ApplicationCommandPermission, ApplicationCommandPermissionType
- Permission management endpoints: GetGuildApplicationCommandPermissionsAsync, GetApplicationCommandPermissionsAsync, EditApplicationCommandPermissionsAsync, BatchEditApplicationCommandPermissionsAsync
- Helper methods in InteractionHandler

**Commands Framework**
- RegisterModuleAsync() method for async module initialization
- InitializeAsync() in BaseCommandModule for custom setup
- GetRegisteredCommands() returning CommandInfo list
- Enhanced async command registration

**Voice**
- VoiceConnection now uses dynamic heartbeat intervals from HELLO
- Voice reconnection with exponential backoff (1s-30s, max 5 attempts)
- Automatic reconnection on connection failures
- Improved voice connection reliability

### Technical Improvements

**Configuration**
- EnableCompression boolean in PawSharpOptions
- MaxMissedHeartbeatAcks (default: 3) in PawSharpOptions
- CacheOptions.MaxEmojisPerGuild (default: 100) in PawSharpOptions

**API Changes**
- EventDispatcher.DispatchFromJson() -> DispatchFromJsonAsync()
- EventDispatcher.On() returns IDisposable
- EventDispatcher.Use() for middleware
- RestClient methods support timeout and reason parameters

**Event Handling**
- GuildEmojisUpdateEvent class
- Async event dispatching throughout
- Middleware execution for all events

### Bug Fixes
- Fixed nullable Emoji.Id handling in cache
- Resolved Span compatibility issues in WebSocket compression
- Fixed EventSubscription scoping and async signatures

---

## [0.5.0-alpha8] - January 14, 2026

This release adds voice, interactivity, and commands frameworks, bringing PawSharp to feature parity with established Discord libraries.

### New Features

**Voice Infrastructure**
- WebSocket-based voice channel connectivity
- Audio capture and playback using NAudio
- Voice state and server update event handling
- Opus codec integration with Concentus
- Thread-safe voice operations with error handling
- Real-time audio pipeline framework

**Interactive Experience Framework**
- Reaction-based interactivity with timeout support
- Automatic pagination for large content
- Poll creation with reaction-based voting
- Message collection utilities for user input
- Built-in interactivity extensions
- Event-driven architecture integration

**Traditional Commands System**
- Attribute-based command registration (`[Command]`, `[Aliases]`, `[Description]`)
- Automatic command parsing with argument extraction
- `CommandContext` providing execution context (user, channel, guild, message)
- Modular command organization with `BaseCommandModule`
- Command execution hooks (before/after)
- Guild and channel-aware processing

**Interaction Support (Slash Commands & Components)**
- Added `PawSharp.Interactions` namespace
- `InteractionHandler` class for slash commands and components
- Interaction data models: `InteractionCreateEvent`, `InteractionData`, `InteractionResolvedData`, `ApplicationCommandInteractionDataOption`
- Integrated interaction handling in `DiscordClient`
- `AllowedMentions` entity for message formatting
- Updated `Message.cs` with `SnowflakeJsonConverter`
- Interaction response methods: `RespondAsync`, `EditResponseAsync`, `FollowupAsync`
- Updated examples with interaction registration

### Technical Improvements

**Voice Implementation Details:**
- WebSocket voice connection with heartbeat and state management
- Audio buffer management with 20ms latency optimization
- NAudio integration for microphone and speaker
- Voice state/server update event infrastructure
- Audio data pipeline for encoding/decoding
- Resource cleanup and disposal

**Interactivity Architecture:**
- EventDispatcher integration for reactions and messages
- Task-based async operations with cancellation
- Timeout handling with `CancellationTokenSource` and `TaskCompletionSource`
- Thread-safe reaction collection and user tracking
- Extension method pattern

**Commands Framework:**
- Reflection-based command discovery and registration
- Parameter parsing with quoted argument support
- Context-aware execution with Discord entity access
- Error handling for command failures
- Modular design for multiple command modules

**Performance & Quality**
- All benchmarks passing
- JSON deserialization: ~785ns
- Cache lookups: ~2.7ns
- Rate limiting: ~23ns
- Documentation generated for 36 assemblies
- Build succeeds with expected nullable warnings

### Dependencies Added

- **Concentus 1.0.4**: Cross-platform Opus audio codec
- **NAudio 2.2.1**: .NET audio library for capture/playback

### Architecture Updates

- Updated `PawSharp.Client` to reference new modules
- Maintained backward compatibility
- Consistent API design patterns
- Integration testing with existing suite

### Documentation Updates

- Updated main README with usage examples
- Added voice connection examples
- Enhanced interactivity examples
- Comprehensive commands documentation

### Feature Parity Achieved

PawSharp now provides equivalent functionality to DSharpPlus for:
- Voice channel connections and audio streaming
- Interactive systems with reactions and pagination
- Traditional message-based commands
- Audio processing and codec support
- Modern slash commands and component interactions

---

## [0.5.0-alpha7] - 2026-01-09

### Added

**Testing and Quality Assurance**

- 50+ unit tests covering validation, exceptions, metrics, and error scenarios
- Integration tests with opt-in flag via environment variable
- Error scenario tests for all HTTP status codes (400, 401, 403, 404, 429, 500, 503)
- Cache interaction tests and concurrent request handling tests
- Snowflake entity creation and component extraction tests

**Comprehensive Documentation**

- API reference guide with 4500+ lines of content and examples
- Error handling guide with patterns and solutions for each exception type
- Migration guide for upgrading between versions
- Quick reference card for common tasks and code snippets
- Real-world example bot demonstrating logging, metrics, and event handling
- Documentation index linking all guides and references

**Performance and Memory Monitoring**

- Performance metrics class tracking API calls, cache operations, and gateway events
- Memory usage monitoring with current and peak memory tracking
- Request duration metrics with per-endpoint tracking
- Cache hit rate calculation and error rate monitoring
- Process statistics including handles, threads, and CPU time

**Structured Logging**

- Logging configuration extensions for dependency injection
- Component-specific log level filtering
- 15+ predefined log event templates for common operations
- Support for console and debug output

### Changed

- Version updated to 0.5.0-alpha7
- Example bot updated with command system, metrics, and error handling
- ROADMAP updated to reflect Phase 3 completion status
- Production readiness increased to 80 percent
- Status updated to Phase 3 complete

### Technical Details

New files created:
- `src/PawSharp.Core/Logging/LoggingExtensions.cs` - Logging configuration
- `src/PawSharp.Core/Metrics/PerformanceMetrics.cs` - API and cache metrics
- `src/PawSharp.Core/Metrics/MemoryMetrics.cs` - Memory and process tracking
- `tests/PawSharp.Core.Tests/ValidationAndExceptionTests.cs` - Validation and exception tests
- `tests/PawSharp.Core.Tests/MetricsTests.cs` - Metrics tracking tests
- `tests/PawSharp.API.Tests/IntegrationAndErrorTests.cs` - Integration and error scenario tests
- `docs/API.md` - Complete API reference
- `docs/ERROR_HANDLING.md` - Error handling guide
- `docs/MIGRATION.md` - Version migration guide
- `docs/INDEX.md` - Documentation index
- `PHASE_3_SUMMARY.md` - Phase 3 implementation report
- `QUICK_REFERENCE.md` - Quick reference card

Files modified:
- `README.md` - Updated version and status
- `ROADMAP.md` - Phase 3 marked complete
- `src/Directory.Build.props` - Version bumped to 0.5.0-alpha7
- `examples/AdvancedExample.cs` - Replaced with comprehensive example

### Stability Status

Phase 3 is complete and production-ready:

- All major components have comprehensive test coverage
- Complete documentation for API, errors, and migration
- Performance monitoring built in for optimization
- Memory tracking to prevent unbounded growth
- Production readiness at 80 percent
- Ready for beta testing and real-world deployment

---

## [0.5.0-alpha7] - 2026-01-09

### Added

**Gateway Resilience and Auto-Healing**

The gateway client now handles network issues gracefully without crashing:

- `GatewayState` enum implementing a proper state machine: Disconnected → Connecting → Connected → Ready → Failed
- `ReconnectionManager` with exponential backoff: starts at 1 second, doubles each attempt, caps at 16 seconds maximum
- Automatic session resumption within 45 seconds of disconnect
- Maximum of 10 reconnection attempts before permanent failure
- Events fired on reconnection attempts and failures for application awareness

**Heartbeat Acknowledgment Tracking**

Connection health is now actively monitored by tracking heartbeat ACKs:

- ACK tracking detects unhealthy connections automatically
- Connection considered zombie after 2 consecutive missed ACKs
- Automatic reconnection triggered when zombie state detected
- Heartbeat state machine properly integrated with overall gateway state

**Complete Discord Gateway Opcode Support**

All 12 Discord gateway opcodes are now properly handled:

- Opcode 0 (Dispatch) - Fully implemented for event distribution
- Opcode 1 (Heartbeat) - Handles server-initiated heartbeat requests
- Opcode 2 (Identify) - Handled internally via SendIdentifyAsync()
- Opcode 3 (Status Update) - Available via public UpdatePresenceAsync() method
- Opcode 5 (Voice Server Ping) - Handled (voice support deferred to future phase)
- Opcode 6 (Resume) - Handled internally via SendResumeAsync()
- Opcode 7 (Reconnect) - Server-requested reconnection fully implemented
- Opcode 8 (Request Guild Members) - Available via public RequestGuildMembersAsync() method
- Opcode 9 (Invalid Session) - Properly handled with session state clearing
- Opcode 10 (Hello) - Server handshake and heartbeat interval setup
- Opcode 11 (Heartbeat ACK) - Server pong response handling

**Error Recovery Mechanisms**

Transient and permanent failures are now handled distinctly:

- Transient network errors automatically trigger reconnection with exponential backoff
- Permanent failures (invalid token, invalid session) are handled cleanly without endless retry loops
- Clear error messages distinguish recoverable from permanent failures
- Comprehensive logging of all connection state transitions

### Changed

**Gateway State Management**

The `GatewayClient` now exposes state information and transitions via events:

- State machine prevents invalid connection state transitions
- `OnStateChanged` event allows applications to monitor connection lifecycle
- `OnReconnectionAttempt` event fires with attempt number for progress tracking
- `OnReconnectionFailed` event fires when all reconnection attempts have been exhausted
- `CurrentState` property allows querying current gateway state at any time

**HeartbeatManager Enhancements**

The heartbeat manager now provides better diagnostics and health monitoring:

- Constructor accepts optional `ILogger` parameter for diagnostic output
- `IsHealthy` property indicates whether connection is healthy based on ACK tracking
- `OnHeartbeatSent` event fired after each heartbeat is transmitted
- `OnHeartbeatAckReceived` event fired when server acknowledges heartbeat
- `OnZombieConnection` event fired when connection becomes unhealthy

**New Public API Methods**

Applications can now directly send opcodes 3 and 8:

- `UpdatePresenceAsync(status, game, streamUrl)` - Change bot presence and status
- `RequestGuildMembersAsync(guildId, limit, query)` - Request guild member lists for chunking

### Technical Details

New files created:
- `src/PawSharp.Gateway/GatewayState.cs` - State machine enum
- `src/PawSharp.Gateway/ReconnectionManager.cs` - Exponential backoff and reconnection logic

Files modified:
- `src/PawSharp.Gateway/GatewayClient.cs` - Major refactor for resilience, all opcodes handled
- `src/PawSharp.Gateway/Heartbeat/HeartbeatManager.cs` - ACK tracking and health monitoring
- `src/Directory.Build.props` - Version bumped to 0.5.0-alpha7

### Breaking Changes

- `HeartbeatManager` constructor now requires `ILogger` parameter (can be null)
- `GatewayClient.ConnectAsync()` now validates state and prevents reconnection while already connected

### Stability Status

Phase 2 is now complete and production-ready for stable connections:

- Automatic reconnection with exponential backoff fully functional
- Heartbeat ACK tracking prevents zombie connections effectively
- All Discord gateway opcodes handled correctly
- State machine prevents invalid transitions and crashes
- Comprehensive logging helps diagnose connection issues
- Can maintain stable connections across network interruptions

---

## [0.5.0-alpha5] - 2026-01-07

### Added

**Exception Hierarchy**

All REST methods now throw typed exceptions instead of returning null on failure:

- `DiscordException` - base class for all errors
- `DiscordApiException` - API returned an error with status code and response body
- `RateLimitException` - hit Discord's rate limit with retry-after information
- `GatewayException` - WebSocket and connection problems
- `ValidationException` - input validation failed before sending to Discord
- `DeserializationException` - couldn't parse JSON from Discord response

**Input Validation**

Input is validated before making API calls:

- `SnowflakeValidator` - validates Discord snowflake IDs
- `ContentValidator` - enforces message and embed size limits
- `EmbedValidator` - validates embed structure with field count and length checks
- `UrlValidator` - checks URL format and schemes
- All REST endpoints validate parameters before making API calls

**Bounded Caching**

In-memory cache with proper memory management:

- Per-entity type size limits (10K messages, 1K guilds, 5K users, etc.)
- LRU eviction when limits are hit
- TTL-based cleanup every 5 minutes
- Prevents unbounded memory growth
- Configurable via `MaxCacheSize` constants

**Rate Limiting**

Rate limiting fully integrated into REST client:

- `AdvancedRateLimiter` with per-route bucket tracking
- Uses X-RateLimit-Bucket headers from Discord for accurate bucket management
- Proper Retry-After parsing handling both seconds and milliseconds
- Exponential backoff on 429 responses

### Changed

**Breaking Changes**

All REST methods now throw exceptions instead of returning null on failure.

Before:
```csharp
var message = await client.Rest.CreateMessageAsync(channelId, request);
if (message == null) 
{
    // Something went wrong, but what?
}
```

After:
```csharp
try
{
    var message = await client.Rest.CreateMessageAsync(channelId, request);
    // message exists, definitely
}
catch (ValidationException ex) when (ex.Message.Contains("too long"))
{
    Console.WriteLine("Message exceeds Discord's 2000 character limit");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, wait {ex.RetryAfter} seconds");
}
catch (DiscordApiException ex)
{
    Console.WriteLine($"API returned {ex.StatusCode}: {ex.Message}");
}
```

### Technical Details

New directories:
- `src/PawSharp.Core/Exceptions/` - 6 exception classes
- `src/PawSharp.Core/Validation/` - 4 validators

Files modified:
- `src/PawSharp.API/Clients/RestClient.cs` - validation calls integrated
- `src/PawSharp.Cache/Providers/MemoryCacheProvider.cs` - bounded cache implementation
- `src/Directory.Build.props` - version bump

---

## [0.5.0-alpha4] - 2026-01-06

### Added

- Audit logs API (`GetGuildAuditLogsAsync`)
- Auto moderation endpoints (list, get, create, modify, delete)
- Request models for auto moderation
- Unit tests for new endpoints

### Changed

- Extended `IDiscordRestClient` with 12 new methods
- Maintained backwards compatibility

---

## [0.5.0-alpha3] - 2026-01-05

### Added

- Webhook support with all webhook entity types
- Audit log entities and entry types
- Comprehensive change tracking models

---

## [0.5.0-alpha2] - 2026-01-04

### Added

- Slash commands foundation
- Interaction model and handling
- Message components (buttons, select menus, modals)

---

## [0.5.0-alpha1] - 2026-01-03

### Added

- Initial public release
- WebSocket gateway connection
- Basic REST API client
- In-memory entity caching
- Message event handling

---

## Versioning

We follow Semantic Versioning:

- **0.5.x** - Alpha releases with potential breaking changes
- **1.0.0-beta** - Feature complete, fixing bugs
- **1.0.0+** - Stable releases, backwards compatible

---

## Coming Next

See [ROADMAP.md](ROADMAP.md) for the development plan:

- Phase 3: Documentation and comprehensive testing
- Phase 4: Sharding, distributed caching, and advanced features
