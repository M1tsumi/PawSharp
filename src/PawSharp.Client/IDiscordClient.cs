#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.API.RateLimit;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Core.Events;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Interactions;

namespace PawSharp.Client
{
    /// <summary>
    /// Provides a unified interface for interacting with the Discord API.
    /// Combines REST operations, gateway events, caching, and interaction handling.
    /// </summary>
    public interface IDiscordClient
    {
        // ── Properties ──────────────────────────────────────────────────────────

        /// <summary>Gets the current connection state of the client.</summary>
        ClientConnectionState ConnectionState { get; }

        /// <summary>Gets whether the client is currently connected to Discord.</summary>
        bool IsConnected { get; }

        /// <summary>The bot's own user object, populated after ConnectAsync completes.</summary>
        User? CurrentUser { get; }

        /// <summary>Access the gateway client for low-level event handling and presence.</summary>
        IGatewayClient Gateway { get; }

        /// <summary>Access the REST API client for all HTTP operations.</summary>
        IDiscordRestClient Rest { get; }

        /// <summary>Access the entity cache.</summary>
        IEntityCache Cache { get; }

        /// <summary>Access the interaction handler for registering slash commands and components.</summary>
        InteractionHandler Interactions { get; }

        /// <summary>Gets whether the configured REST client exposes rate-limit telemetry events.</summary>
        bool SupportsRateLimitTelemetry { get; }

        // ── Events ──────────────────────────────────────────────────────────────

        /// <summary>Raised when the client's connection state changes.</summary>
        event EventHandler<ClientConnectionState>? ConnectionStateChanged;

        /// <summary>Raised when rate-limit telemetry is emitted by the underlying REST client.</summary>
        event EventHandler<RateLimitTelemetryEvent>? RateLimitObserved;

        /// <summary>Raised when the bot user disconnects from a voice channel.</summary>
        event Func<VoiceState, Task>? OnVoiceDisconnected;

        // ── Connection ──────────────────────────────────────────────────────────

        /// <summary>Opens the WebSocket connection to Discord's gateway.</summary>
        Task ConnectAsync();

        /// <summary>Closes the WebSocket connection gracefully.</summary>
        Task DisconnectAsync();

        /// <summary>Disconnects and reconnects to Discord gracefully.</summary>
        Task ReconnectAsync(int delayMs = 1000);

        // ── Messages ────────────────────────────────────────────────────────────

        Task<Message?> SendMessageAsync(ulong channelId, string content);

        Task<Message?> SendMessageAsync(ulong channelId, string content, Embed embed);

        Task<Message?> SendMessageAsync(ulong channelId, CreateMessageRequest request);

        Task<Message?> ForwardMessageAsync(ulong targetChannelId, ulong sourceChannelId, ulong sourceMessageId, string? content = null, bool failIfNotExists = true);

        Task<Message?> ForwardMessageAsync(ulong targetChannelId, ulong sourceChannelId, ulong sourceMessageId, CreateMessageRequest request, bool failIfNotExists = true);

        Task<User?> GetCurrentUserAsync();

        Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, string content);

        Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request);

        Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId);

        Task<Message?> GetMessageAsync(ulong channelId, ulong messageId);

        Task<bool> TriggerTypingAsync(ulong channelId);

        Task<Message?> SendFileAsync(ulong channelId, Stream fileStream, string fileName, CreateMessageRequest? messageRequest = null, CancellationToken cancellationToken = default);

        Task<Message?> SendFilesAsync(ulong channelId, IEnumerable<(Stream Stream, string FileName)> files, CreateMessageRequest? messageRequest = null, CancellationToken cancellationToken = default);

        Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null);

        Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds);

        Task<bool> PinMessageAsync(ulong channelId, ulong messageId);

        Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId);

        Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId);

        Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId);

        Task<Message?> SendEmbedAsync(ulong channelId, Embed embed);

        Task<Message?> TrySendMessageAsync(ulong channelId, string content);

        Task<Message?> SendDirectMessageAsync(ulong userId, string content);

        // ── Channels ────────────────────────────────────────────────────────────

        Task<Channel?> GetChannelAsync(ulong channelId);

        Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request);

        Task<bool> DeleteChannelAsync(ulong channelId);

        Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request);

        Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId);

        Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request);

        Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId);

        Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request);

        // ── Guilds ──────────────────────────────────────────────────────────────

        Task<Guild?> GetGuildAsync(ulong guildId);

        Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId);

        Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId);

        Task<List<Role>?> GetGuildRolesAsync(ulong guildId);

        Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request);

        Task<Guild?> CreateGuildAsync(CreateGuildRequest request);

        Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request);

        Task<bool> DeleteGuildAsync(ulong guildId);

        Task<int?> ModifyGuildMfaLevelAsync(ulong guildId, int level);

        Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId);

        Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000, ulong? after = null);

        Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request);

        Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request);

        Task<List<Ban>?> GetGuildBansAsync(ulong guildId, ulong? before = null, ulong? after = null, int? limit = null);

        Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId);

        Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null);

        Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId);

        // ── Roles ───────────────────────────────────────────────────────────────

        Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request);

        Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId);

        Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);

        Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);

        // ── Reactions ───────────────────────────────────────────────────────────

        Task<bool> AddReactionAsync(ulong channelId, ulong messageId, string emoji);

        Task<bool> RemoveReactionAsync(ulong channelId, ulong messageId, string emoji);

        Task<bool> RemoveUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId);

        Task<List<User>?> GetReactionsAsync(ulong channelId, ulong messageId, string emoji, int? type = null, ulong? after = null, int? limit = null);

        Task<bool> DeleteAllReactionsAsync(ulong channelId, ulong messageId);

        Task<bool> DeleteAllReactionsForEmojiAsync(ulong channelId, ulong messageId, string emoji);

        // ── Replies ─────────────────────────────────────────────────────────────

        Task<Message?> ReplyAsync(MessageCreateEvent message, string content);

        Task<Message?> ReplyAsync(MessageCreateEvent message, string content, Embed embed);

        Task<Message?> ReplyAsync(MessageCreateEvent message, CreateMessageRequest request);

        Task<Message?> TryReplyAsync(MessageCreateEvent message, string content);

        // ── Users ───────────────────────────────────────────────────────────────

        Task<User?> GetUserAsync(ulong userId);

        Task ModifyCurrentUserAsync(string? username = null, string? avatar = null, string? banner = null, string? avatarDecorationData = null);

        Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null);

        Task<bool> LeaveGuildAsync(ulong guildId);

        // ── DM ──────────────────────────────────────────────────────────────────

        Task<Channel?> CreateDmAsync(ulong recipientId);

        Task<Channel?> CreateGroupDmAsync(List<string> accessTokens, Dictionary<string, string>? nicks = null);

        // ── Threads ─────────────────────────────────────────────────────────────

        Task<Channel?> CreateThreadAsync(ulong channelId, CreateThreadRequest request);

        Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request);

        Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request);

        Task<bool> JoinThreadAsync(ulong channelId);

        Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId);

        Task<bool> LeaveThreadAsync(ulong channelId);

        Task<bool> RemoveThreadMemberAsync(ulong channelId, ulong userId);

        Task<ThreadMember?> GetThreadMemberAsync(ulong channelId, ulong userId);

        Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId, bool withMember = false, ulong? after = null, int? limit = null);

        Task<ActiveThreadsResponse?> GetActiveThreadsAsync(ulong guildId);

        Task<ArchivedThreadsResponse?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);

        Task<ArchivedThreadsResponse?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);

        Task<ArchivedThreadsResponse?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);

        Task<Channel?> GetOrCreateThreadAsync(ulong channelId, string threadName, int autoArchiveDuration = 60);

        // ── Webhooks ────────────────────────────────────────────────────────────

        Task<Webhook?> CreateWebhookAsync(ulong channelId, CreateWebhookRequest request);

        Task<List<Webhook>?> GetChannelWebhooksAsync(ulong channelId);

        Task<List<Webhook>?> GetGuildWebhooksAsync(ulong guildId);

        Task<Webhook?> GetWebhookAsync(ulong webhookId);

        Task<Webhook?> GetWebhookWithTokenAsync(ulong webhookId, string token);

        Task<Webhook?> ModifyWebhookAsync(ulong webhookId, ModifyWebhookRequest request);

        Task<Webhook?> ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request);

        Task<bool> DeleteWebhookAsync(ulong webhookId);

        Task<bool> DeleteWebhookWithTokenAsync(ulong webhookId, string token);

        Task<Message?> ExecuteWebhookAsync(ulong webhookId, string token, ExecuteWebhookRequest request, ulong? threadId = null);

        Task<Message?> GetWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null);

        Task<Message?> EditWebhookMessageAsync(ulong webhookId, string token, ulong messageId, EditMessageRequest request, ulong? threadId = null);

        Task<bool> DeleteWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null);

        Task<bool> ExecuteSlackCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false);

        Task<bool> ExecuteGitHubCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false);

        // ── Scheduled Events ────────────────────────────────────────────────────

        Task<List<GuildScheduledEvent>?> GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount = null);

        Task<GuildScheduledEvent?> GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount = null);

        Task<bool> DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId);

        Task<List<User>?> GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit = null, bool? withMember = null, ulong? before = null, ulong? after = null);

        // ── Audit Log ───────────────────────────────────────────────────────────

        Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, ulong? after = null, int? limit = null);

        // ── Auto-Moderation ─────────────────────────────────────────────────────

        Task<List<AutoModerationRule>?> ListAutoModerationRulesAsync(ulong guildId);

        Task<AutoModerationRule?> GetAutoModerationRuleAsync(ulong guildId, ulong ruleId);

        Task<bool> DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId);

        // ── Stage Instance ──────────────────────────────────────────────────────

        Task<StageInstance?> GetStageInstanceAsync(ulong channelId);

        Task<bool> DeleteStageInstanceAsync(ulong channelId);

        // ── Stickers ────────────────────────────────────────────────────────────

        Task<Sticker?> GetStickerAsync(ulong stickerId);

        Task<List<StickerPack>?> GetNitroStickerPacksAsync();

        Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId);

        Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId);

        Task<bool> DeleteGuildStickerAsync(ulong guildId, ulong stickerId);

        // ── Voice Regions ───────────────────────────────────────────────────────

        Task<List<VoiceRegion>?> GetVoiceRegionsAsync();

        Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId);

        // ── Application Commands ────────────────────────────────────────────────

        Task<List<ApplicationCommand>?> GetGlobalApplicationCommandsAsync(ulong applicationId);

        Task<ApplicationCommand?> CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request);

        Task<List<ApplicationCommand>?> BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands);

        Task<ApplicationCommand?> GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId);

        Task<ApplicationCommand?> EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request);

        Task<bool> DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId);

        Task<List<ApplicationCommand>?> GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId);

        Task<ApplicationCommand?> CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request);

        Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands);

        Task<ApplicationCommand?> GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId);

        Task<ApplicationCommand?> EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request);

        Task<bool> DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId);

        // ── Application Command Permissions ─────────────────────────────────────

        Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId);

        Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId);

        Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions);

        Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions);

        // ── Guild Emoji ─────────────────────────────────────────────────────────

        Task<List<Emoji>?> ListGuildEmojisAsync(ulong guildId);

        Task<Emoji?> GetGuildEmojiAsync(ulong guildId, ulong emojiId);

        Task<bool> DeleteGuildEmojiAsync(ulong guildId, ulong emojiId);

        // ── Application Emoji ───────────────────────────────────────────────────

        Task<List<Emoji>?> ListApplicationEmojisAsync(ulong applicationId);

        Task<Emoji?> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId);

        Task<bool> DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId);

        // ── Guild Integration ───────────────────────────────────────────────────

        Task<List<GuildIntegration>?> GetGuildIntegrationsAsync(ulong guildId);

        Task<bool> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId);

        // ── Guild Invite ────────────────────────────────────────────────────────

        Task<List<Invite>?> GetGuildInvitesAsync(ulong guildId);

        // ── Guild Prune ─────────────────────────────────────────────────────────

        Task<GuildPruneResult?> GetGuildPruneCountAsync(ulong guildId, int? days = null, List<ulong>? includeRoles = null);

        Task<GuildPruneResult?> BeginGuildPruneAsync(ulong guildId, BeginGuildPruneRequest request, string? reason = null);

        // ── Guild Template ──────────────────────────────────────────────────────

        Task<List<GuildTemplate>?> GetGuildTemplatesAsync(ulong guildId);

        Task<GuildTemplate?> GetGuildTemplateAsync(string templateCode);

        Task<GuildTemplate?> SyncGuildTemplateAsync(ulong guildId, string templateCode);

        Task<GuildTemplate?> ModifyGuildTemplateAsync(ulong guildId, string templateCode, ModifyGuildTemplateRequest request);

        Task<GuildTemplate?> DeleteGuildTemplateAsync(ulong guildId, string templateCode);

        // ── OAuth2 ──────────────────────────────────────────────────────────────

        Task<Application?> GetCurrentApplicationAsync();

        Task<Application?> GetCurrentBotApplicationInfoAsync();

        Task<OAuth2Info?> GetCurrentAuthorizationInfoAsync();

        Task<Application?> EditCurrentApplicationAsync(EditCurrentApplicationRequest request);

        Task<OAuth2TokenResponse?> ExchangeCodeAsync(string code, string clientId, string clientSecret, string redirectUri);

        Task<OAuth2TokenResponse?> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret);

        Task<bool> RevokeTokenAsync(string token, string clientId, string clientSecret, string? tokenTypeHint = null);

        // ── Polls ───────────────────────────────────────────────────────────────

        Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null);

        Task<Message?> EndPollAsync(ulong channelId, ulong messageId);

        // ── SKU / Entitlement / Subscription ────────────────────────────────────

        Task<List<Sku>?> ListSkusAsync(ulong applicationId);

        Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null);

        Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId);

        Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request);

        Task<bool> DeleteTestEntitlementAsync(ulong applicationId, ulong entitlementId);

        Task<bool> ConsumeEntitlementAsync(ulong applicationId, ulong entitlementId);

        Task<List<Subscription>?> ListSkuSubscriptionsAsync(ulong skuId, ulong? before = null, ulong? after = null, int? limit = null, ulong? userId = null);

        Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId);

        // ── Soundboard ──────────────────────────────────────────────────────────

        Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync();

        Task<List<SoundboardSound>?> ListGuildSoundboardSoundsAsync(ulong guildId);

        Task<SoundboardSound?> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId);

        Task<bool> DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId);

        Task<bool> SendSoundboardSoundAsync(ulong channelId, SendSoundboardSoundRequest request);

        // ── Guild Onboarding ────────────────────────────────────────────────────

        Task<GuildOnboarding?> GetGuildOnboardingAsync(ulong guildId);

        Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request);

        // ── Application Role Connection ─────────────────────────────────────────

        Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId);

        Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records);

        Task<ApplicationRoleConnection?> GetUserApplicationRoleConnectionAsync(ulong applicationId);

        Task<ApplicationRoleConnection?> UpdateUserApplicationRoleConnectionAsync(ulong applicationId, UpdateUserApplicationRoleConnectionRequest request);

        // ── Widget ──────────────────────────────────────────────────────────────

        Task<GuildWidget?> GetGuildWidgetAsync(ulong guildId);

        Task<GuildWidgetSettings?> ModifyGuildWidgetAsync(ulong guildId, ModifyGuildWidgetRequest request);

        Task<GuildWidgetSettings?> GetGuildWidgetSettingsAsync(ulong guildId);

        // ── Vanity URL ──────────────────────────────────────────────────────────

        Task<VanityUrl?> GetGuildVanityUrlAsync(ulong guildId);

        // ── Welcome Screen ──────────────────────────────────────────────────────

        Task<WelcomeScreen?> GetGuildWelcomeScreenAsync(ulong guildId);

        Task<WelcomeScreen?> ModifyGuildWelcomeScreenAsync(ulong guildId, ModifyGuildWelcomeScreenRequest request);

        // ── Channel / Role Positions ─────────────────────────────────────────────

        Task<bool> ModifyGuildChannelPositionsAsync(ulong guildId, List<ModifyChannelPositionRequest> positions);

        Task<List<Role>?> ModifyGuildRolePositionsAsync(ulong guildId, List<ModifyRolePositionRequest> positions);

        // ── Invite Lookup / Deletion ─────────────────────────────────────────────

        Task<Invite?> GetInviteAsync(string inviteCode, bool? withCounts = null, bool? withExpiration = null, ulong? guildScheduledEventId = null);

        Task<Invite?> DeleteInviteAsync(string inviteCode, string? reason = null);

        // ── Bulk Ban ─────────────────────────────────────────────────────────────

        Task<BulkGuildBanResponse?> BulkGuildBanAsync(ulong guildId, BulkGuildBanRequest request, string? reason = null);

        // ── Guild Role Extras ────────────────────────────────────────────────────

        Task<Role?> GetGuildRoleAsync(ulong guildId, ulong roleId);

        Task<Dictionary<string, int>?> GetGuildRoleMemberCountsAsync(ulong guildId);

        // ── Guild Incident Actions ───────────────────────────────────────────────

        Task<GuildIncidentActionsResponse?> ModifyGuildIncidentActionsAsync(ulong guildId, ModifyGuildIncidentActionsRequest request);

        // ── Current User Guild Member ────────────────────────────────────────────

        Task<GuildMember?> GetCurrentUserGuildMemberAsync(ulong guildId);

        // ── Voice State ──────────────────────────────────────────────────────────

        IReadOnlyDictionary<ulong, VoiceState> VoiceStates { get; }

        VoiceState? GetVoiceState(ulong guildId);

        Task<bool> ModifyCurrentUserVoiceStateAsync(ulong guildId, ModifyCurrentUserVoiceStateRequest request);

        Task<bool> ModifyUserVoiceStateAsync(ulong guildId, ulong userId, ModifyUserVoiceStateRequest request);

        // ── Activity Instance ────────────────────────────────────────────────────

        Task<ActivityInstance?> GetActivityInstanceAsync(ulong applicationId, string instanceId);

        // ── Gateway ──────────────────────────────────────────────────────────────

        Task<GatewayInfo?> GetGatewayAsync();

        Task<GatewayBotInfo?> GetGatewayBotAsync();

        // ── Current User Connections ─────────────────────────────────────────────

        Task<List<UserConnection>?> GetCurrentUserConnectionsAsync();

        // ── Guild Member Search ──────────────────────────────────────────────────

        Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int limit = 25);

        // ── Modify Current Member ────────────────────────────────────────────────

        Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick);

        // ── Additional ───────────────────────────────────────────────────────────

        Task<GuildPreview?> GetGuildPreviewAsync(ulong guildId);

        Task<FollowedChannel?> FollowAnnouncementChannelAsync(ulong channelId, ulong webhookChannelId);

        // ── Gateway Event Subscriptions ──────────────────────────────────────────

        IDisposable OnReady(Func<ReadyEvent, Task> handler);
        IDisposable OnMessageCreated(Func<MessageCreateEvent, Task> handler);
        IDisposable OnMessageUpdated(Func<MessageUpdateEvent, Task> handler);
        IDisposable OnMessageDeleted(Func<MessageDeleteEvent, Task> handler);
        IDisposable OnMessagesBulkDeleted(Func<MessageDeleteBulkEvent, Task> handler);
        IDisposable OnReactionAdded(Func<MessageReactionAddEvent, Task> handler);
        IDisposable OnReactionRemoved(Func<MessageReactionRemoveEvent, Task> handler);
        IDisposable OnAllReactionsRemoved(Func<MessageReactionRemoveAllEvent, Task> handler);
        IDisposable OnEmojiReactionsRemoved(Func<MessageReactionRemoveEmojiEvent, Task> handler);
        IDisposable OnGuildAvailable(Func<GuildCreateEvent, Task> handler);
        IDisposable OnGuildUpdated(Func<GuildUpdateEvent, Task> handler);
        IDisposable OnGuildUnavailable(Func<GuildDeleteEvent, Task> handler);
        IDisposable OnGuildMemberJoined(Func<GuildMemberAddEvent, Task> handler);
        IDisposable OnGuildMemberUpdated(Func<GuildMemberUpdateEvent, Task> handler);
        IDisposable OnGuildMemberLeft(Func<GuildMemberRemoveEvent, Task> handler);
        IDisposable OnChannelCreated(Func<ChannelCreateEvent, Task> handler);
        IDisposable OnChannelUpdated(Func<ChannelUpdateEvent, Task> handler);
        IDisposable OnChannelDeleted(Func<ChannelDeleteEvent, Task> handler);
        IDisposable OnChannelPinsUpdated(Func<ChannelPinsUpdateEvent, Task> handler);
        IDisposable OnRoleCreated(Func<GuildRoleCreateEvent, Task> handler);
        IDisposable OnRoleUpdated(Func<GuildRoleUpdateEvent, Task> handler);
        IDisposable OnRoleDeleted(Func<GuildRoleDeleteEvent, Task> handler);
        IDisposable OnBanAdded(Func<GuildBanAddEvent, Task> handler);
        IDisposable OnBanRemoved(Func<GuildBanRemoveEvent, Task> handler);
        IDisposable OnTypingStarted(Func<TypingStartEvent, Task> handler);
        IDisposable OnPresenceUpdated(Func<PresenceUpdateEvent, Task> handler);
        IDisposable OnVoiceStateUpdated(Func<VoiceStateUpdateEvent, Task> handler);
        IDisposable OnThreadCreated(Func<ThreadCreateEvent, Task> handler);
        IDisposable OnThreadUpdated(Func<ThreadUpdateEvent, Task> handler);
        IDisposable OnThreadDeleted(Func<ThreadDeleteEvent, Task> handler);
        IDisposable OnInteractionCreated(Func<InteractionCreateEvent, Task> handler);
        IDisposable OnInviteCreated(Func<InviteCreateEvent, Task> handler);
        IDisposable OnInviteDeleted(Func<InviteDeleteEvent, Task> handler);
        IDisposable OnScheduledEventCreated(Func<GuildScheduledEventCreateEvent, Task> handler);
        IDisposable OnScheduledEventUpdated(Func<GuildScheduledEventUpdateEvent, Task> handler);
        IDisposable OnScheduledEventDeleted(Func<GuildScheduledEventDeleteEvent, Task> handler);
        IDisposable OnAutoModerationActionExecuted(Func<AutoModerationActionExecutionEvent, Task> handler);
        IDisposable OnVoiceServerUpdated(Func<VoiceServerUpdateEvent, Task> handler);
        IDisposable OnGuildEmojisUpdated(Func<GuildEmojisUpdateEvent, Task> handler);
        IDisposable OnGuildStickersUpdated(Func<GuildStickersUpdateEvent, Task> handler);
        IDisposable OnGuildMembersChunked(Func<GuildMembersChunkEvent, Task> handler);
        IDisposable OnGuildAuditLogEntryCreated(Func<GuildAuditLogEntryCreateEvent, Task> handler);
        IDisposable OnWebhooksUpdated(Func<WebhooksUpdateEvent, Task> handler);
        IDisposable OnStageInstanceCreated(Func<StageInstanceCreateEvent, Task> handler);
        IDisposable OnStageInstanceUpdated(Func<StageInstanceUpdateEvent, Task> handler);
        IDisposable OnStageInstanceDeleted(Func<StageInstanceDeleteEvent, Task> handler);
        IDisposable OnScheduledEventUserAdded(Func<GuildScheduledEventUserAddEvent, Task> handler);
        IDisposable OnScheduledEventUserRemoved(Func<GuildScheduledEventUserRemoveEvent, Task> handler);
        IDisposable OnAutoModerationRuleCreated(Func<AutoModerationRuleCreateEvent, Task> handler);
        IDisposable OnAutoModerationRuleUpdated(Func<AutoModerationRuleUpdateEvent, Task> handler);
        IDisposable OnAutoModerationRuleDeleted(Func<AutoModerationRuleDeleteEvent, Task> handler);
        IDisposable OnIntegrationCreated(Func<IntegrationCreateEvent, Task> handler);
        IDisposable OnIntegrationUpdated(Func<IntegrationUpdateEvent, Task> handler);
        IDisposable OnIntegrationDeleted(Func<IntegrationDeleteEvent, Task> handler);
        IDisposable OnMessagePollVoteAdded(Func<MessagePollVoteAddEvent, Task> handler);
        IDisposable OnMessagePollVoteRemoved(Func<MessagePollVoteRemoveEvent, Task> handler);
        IDisposable OnEntitlementCreated(Func<EntitlementCreateEvent, Task> handler);
        IDisposable OnEntitlementUpdated(Func<EntitlementUpdateEvent, Task> handler);
        IDisposable OnEntitlementDeleted(Func<EntitlementDeleteEvent, Task> handler);
        IDisposable OnThreadListSynced(Func<ThreadListSyncEvent, Task> handler);
        IDisposable OnThreadMemberUpdated(Func<ThreadMemberUpdateEvent, Task> handler);
        IDisposable OnThreadMembersUpdated(Func<ThreadMembersUpdateEvent, Task> handler);
        IDisposable OnApplicationCommandPermissionsUpdated(Func<ApplicationCommandPermissionsUpdateEvent, Task> handler);
        IDisposable OnGuildIntegrationsUpdated(Func<GuildIntegrationsUpdateEvent, Task> handler);
        IDisposable OnUserUpdated(Func<UserUpdateEvent, Task> handler);
        IDisposable OnSoundboardSoundCreated(Func<GuildSoundboardSoundCreateEvent, Task> handler);
        IDisposable OnSoundboardSoundUpdated(Func<GuildSoundboardSoundUpdateEvent, Task> handler);
        IDisposable OnSoundboardSoundDeleted(Func<GuildSoundboardSoundDeleteEvent, Task> handler);
        IDisposable OnSoundboardSoundsUpdated(Func<GuildSoundboardSoundsUpdateEvent, Task> handler);
        IDisposable OnSubscriptionCreated(Func<SubscriptionCreateEvent, Task> handler);
        IDisposable OnSubscriptionUpdated(Func<SubscriptionUpdateEvent, Task> handler);
        IDisposable OnSubscriptionDeleted(Func<SubscriptionDeleteEvent, Task> handler);
        IDisposable OnVoiceChannelEffectSent(Func<VoiceChannelEffectSendEvent, Task> handler);
        IDisposable OnVoiceChannelStatusUpdated(Func<VoiceChannelStatusUpdateEvent, Task> handler);
    }
}
