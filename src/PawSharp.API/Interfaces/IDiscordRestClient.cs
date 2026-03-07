#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Core.Entities;

namespace PawSharp.API.Interfaces;

/// <summary>
/// Interface for Discord REST API client.
/// </summary>
public interface IDiscordRestClient
{
    /// <summary>
    /// Sends a GET request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint);
    
    /// <summary>
    /// Sends a GET request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a POST request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent? content);
    
    /// <summary>
    /// Sends a POST request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a PUT request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent? content);
    
    /// <summary>
    /// Sends a PUT request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a DELETE request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint);
    
    /// <summary>
    /// Sends a DELETE request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a PATCH request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content);
    
    /// <summary>
    /// Sends a PATCH request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current bot user information.
    /// </summary>
    Task<HttpResponseMessage> GetCurrentUserAsync();
    
    // User operations
    Task<User?> GetUserAsync(ulong userId);
    Task<HttpResponseMessage> ModifyCurrentUserAsync(string? username = null, string? avatar = null);
    Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null);
    Task<bool> LeaveGuildAsync(ulong guildId);
    
    // Message operations
    Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request);
    Task<Message?> SendFileAsync(ulong channelId, Stream fileStream, string fileName, CreateMessageRequest? messageRequest = null, CancellationToken cancellationToken = default);
    Task<Message?> GetMessageAsync(ulong channelId, ulong messageId);
    Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request);
    Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId);
    Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null);
    Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds);
    Task<bool> PinMessageAsync(ulong channelId, ulong messageId);
    Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId);
    Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId);
    Task<bool> TriggerTypingIndicatorAsync(ulong channelId);
    
    // Channel operations
    Task<Channel?> GetChannelAsync(ulong channelId);
    Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request);
    Task<bool> DeleteChannelAsync(ulong channelId);
    Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request);
    Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId);
    Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request);
    Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId);
    
    // Guild operations
    Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts = false);
    Task<Guild?> CreateGuildAsync(CreateGuildRequest request);
    Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request);
    Task<bool> DeleteGuildAsync(ulong guildId);
    Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId);
    Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000);
    Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId);
    Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request);
    Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request);
    Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId);
    Task<List<Ban>?> GetGuildBansAsync(ulong guildId);
    Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId);
    Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null);
    Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId);
    
    // Role operations
    Task<List<Role>?> GetGuildRolesAsync(ulong guildId);
    Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request);
    Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request);
    Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId);
    Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);
    Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);
    
    // Interaction operations
    Task<bool> CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response);
    Task<Message?> GetOriginalInteractionResponseAsync(string applicationId, string interactionToken);
    Task<HttpResponseMessage> EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request);
    Task<bool> DeleteOriginalInteractionResponseAsync(string applicationId, string interactionToken);
    
    // Interaction follow-up message operations
    Task<Message?> CreateFollowupMessageAsync(string applicationId, string interactionToken, CreateMessageRequest request);
    Task<Message?> GetFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId);
    Task<Message?> EditFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId, EditMessageRequest request);
    Task<bool> DeleteFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId);
    
    // Reaction operations
    Task<bool> CreateReactionAsync(ulong channelId, ulong messageId, string emoji);
    Task<bool> DeleteOwnReactionAsync(ulong channelId, ulong messageId, string emoji);
    Task<bool> DeleteUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId);
    
    // Application Command operations
    Task<List<ApplicationCommand>?> GetGlobalApplicationCommandsAsync(ulong applicationId);
    Task<ApplicationCommand?> CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request);
    Task<ApplicationCommand?> GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId);
    Task<ApplicationCommand?> EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request);
    Task<bool> DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId);
    Task<List<ApplicationCommand>?> GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId);
    Task<ApplicationCommand?> CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request);
    Task<ApplicationCommand?> GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId);
    Task<ApplicationCommand?> EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request);
    Task<bool> DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId);
    Task<List<ApplicationCommand>?> BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands);
    Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands);
    
    // Application Command Permissions operations
    Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId);
    Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId);
    Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions);
    Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions);
    
    // Thread operations
    Task<Channel?> CreateThreadAsync(ulong channelId, CreateThreadRequest request);
    Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request);
    Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request);
    Task<bool> JoinThreadAsync(ulong channelId);
    Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId);
    Task<bool> LeaveThreadAsync(ulong channelId);
    Task<bool> RemoveThreadMemberAsync(ulong channelId, ulong userId);
    Task<ThreadMember?> GetThreadMemberAsync(ulong channelId, ulong userId);
    Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId);
    Task<ActiveThreadsResponse?> GetActiveThreadsAsync(ulong guildId);
    Task<ArchivedThreadsResponse?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);
    Task<ArchivedThreadsResponse?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);
    Task<ArchivedThreadsResponse?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null);
    
    // Webhook operations
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
    
    // Scheduled Event operations
    Task<GuildScheduledEvent?> CreateGuildScheduledEventAsync(ulong guildId, CreateGuildScheduledEventRequest request);
    Task<List<GuildScheduledEvent>?> GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount = null);
    Task<GuildScheduledEvent?> GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount = null);
    Task<GuildScheduledEvent?> ModifyGuildScheduledEventAsync(ulong guildId, ulong eventId, ModifyGuildScheduledEventRequest request);
    Task<bool> DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId);
    Task<List<User>?> GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit = null, bool? withMember = null, ulong? before = null, ulong? after = null);
    
    // Audit Log operations
    Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, int? limit = null);
    
    // Auto Moderation operations
    Task<List<AutoModerationRule>?> ListAutoModerationRulesAsync(ulong guildId);
    Task<AutoModerationRule?> GetAutoModerationRuleAsync(ulong guildId, ulong ruleId);
    Task<AutoModerationRule?> CreateAutoModerationRuleAsync(ulong guildId, CreateAutoModerationRuleRequest request);
    Task<AutoModerationRule?> ModifyAutoModerationRuleAsync(ulong guildId, ulong ruleId, ModifyAutoModerationRuleRequest request);
    Task<bool> DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId);

    // Stage Instance operations
    Task<StageInstance?> CreateStageInstanceAsync(CreateStageInstanceRequest request);
    Task<StageInstance?> GetStageInstanceAsync(ulong channelId);
    Task<StageInstance?> ModifyStageInstanceAsync(ulong channelId, ModifyStageInstanceRequest request);
    Task<bool> DeleteStageInstanceAsync(ulong channelId);

    // Sticker operations
    Task<Sticker?> GetStickerAsync(ulong stickerId);
    Task<List<StickerPack>?> GetNitroStickerPacksAsync();
    Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId);
    Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId);
    Task<Sticker?> CreateGuildStickerAsync(ulong guildId, CreateGuildStickerRequest request);
    Task<Sticker?> ModifyGuildStickerAsync(ulong guildId, ulong stickerId, ModifyGuildStickerRequest request);
    Task<bool> DeleteGuildStickerAsync(ulong guildId, ulong stickerId);

    // DM operations
    Task<Channel?> CreateDmAsync(ulong recipientId);

    // Gateway Bot info
    Task<GatewayBotInfo?> GetGatewayBotAsync();

    // Voice Region operations
    Task<List<VoiceRegion>?> GetVoiceRegionsAsync();
    Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId);

    // Message crosspost
    Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId);

    // Channel permission overwrites
    Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request);

    // Current user connections
    Task<List<UserConnection>?> GetCurrentUserConnectionsAsync();

    // Guild member search
    Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int? limit = null);

    // Modify current member (e.g. nick)
    Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick);

    // Poll operations
    Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null);
    Task<Message?> EndPollAsync(ulong channelId, ulong messageId);

    // SKU operations
    Task<List<Sku>?> ListSkusAsync(ulong applicationId);

    // Entitlement operations
    Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null);
    Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId);
    Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request);
    Task<bool> DeleteTestEntitlementAsync(ulong applicationId, ulong entitlementId);
    Task<bool> ConsumeEntitlementAsync(ulong applicationId, ulong entitlementId);

    // Subscription operations
    Task<List<Subscription>?> ListSkuSubscriptionsAsync(ulong skuId, ulong? before = null, ulong? after = null, int? limit = null, ulong? userId = null);
    Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId);

    // Soundboard operations
    Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync();
    Task<List<SoundboardSound>?> ListGuildSoundboardSoundsAsync(ulong guildId);
    Task<SoundboardSound?> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId);
    Task<SoundboardSound?> CreateGuildSoundboardSoundAsync(ulong guildId, CreateGuildSoundboardSoundRequest request);
    Task<SoundboardSound?> ModifyGuildSoundboardSoundAsync(ulong guildId, ulong soundId, ModifyGuildSoundboardSoundRequest request);
    Task<bool> DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId);

    // Guild Onboarding operations
    Task<GuildOnboarding?> GetGuildOnboardingAsync(ulong guildId);
    Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request);

    // Application Role Connection Metadata
    Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId);
    Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records);

    // ── Alpha13 additions ─────────────────────────────────────────────────────

    // Reaction query (GET reactions on a message)
    Task<List<User>?> GetReactionsAsync(ulong channelId, ulong messageId, string emoji, int? type = null, ulong? after = null, int? limit = null);

    // Announcement channel follow
    Task<FollowedChannel?> FollowAnnouncementChannelAsync(ulong channelId, ulong webhookChannelId);

    // Guild preview
    Task<GuildPreview?> GetGuildPreviewAsync(ulong guildId);

    // Guild widget
    Task<GuildWidgetSettings?> GetGuildWidgetSettingsAsync(ulong guildId);
    Task<GuildWidgetSettings?> ModifyGuildWidgetAsync(ulong guildId, ModifyGuildWidgetRequest request);

    // Guild vanity URL
    Task<VanityUrl?> GetGuildVanityUrlAsync(ulong guildId);

    // Guild welcome screen
    Task<WelcomeScreen?> GetGuildWelcomeScreenAsync(ulong guildId);
    Task<WelcomeScreen?> ModifyGuildWelcomeScreenAsync(ulong guildId, ModifyGuildWelcomeScreenRequest request);

    // Guild channel / role position reorder
    Task<bool> ModifyGuildChannelPositionsAsync(ulong guildId, IEnumerable<ModifyChannelPositionRequest> positions);
    Task<List<Role>?> ModifyGuildRolePositionsAsync(ulong guildId, IEnumerable<ModifyRolePositionRequest> positions);

    // Invite lookup and deletion
    Task<Invite?> GetInviteAsync(string inviteCode, bool? withCounts = null, bool? withExpiration = null, ulong? guildScheduledEventId = null);
    Task<Invite?> DeleteInviteAsync(string inviteCode, string? reason = null);

    // Guild Templates
    Task<List<GuildTemplate>?> GetGuildTemplatesAsync(ulong guildId);
    Task<GuildTemplate?> GetGuildTemplateAsync(string templateCode);
    Task<Guild?> CreateGuildFromTemplateAsync(string templateCode, CreateGuildFromTemplateRequest request);
    Task<GuildTemplate?> CreateGuildTemplateAsync(ulong guildId, CreateGuildTemplateRequest request);
    Task<GuildTemplate?> SyncGuildTemplateAsync(ulong guildId, string templateCode);
    Task<GuildTemplate?> ModifyGuildTemplateAsync(ulong guildId, string templateCode, ModifyGuildTemplateRequest request);
    Task<GuildTemplate?> DeleteGuildTemplateAsync(ulong guildId, string templateCode);

    // OAuth2 operations
    /// <summary>Returns the bot's application object. GET /oauth2/applications/@me</summary>
    Task<Application?> GetCurrentBotApplicationInfoAsync();
    /// <summary>Returns info about the current authorization. Requires a Bearer token. GET /oauth2/@me</summary>
    Task<OAuth2Info?> GetCurrentAuthorizationInfoAsync();

    // Application management
    /// <summary>Returns the current application. GET /applications/@me</summary>
    Task<Application?> GetCurrentApplicationAsync();
    /// <summary>Edits properties of the current application. PATCH /applications/@me</summary>
    Task<Application?> EditCurrentApplicationAsync(EditCurrentApplicationRequest request);

    // Guild emoji operations
    Task<List<Emoji>?> ListGuildEmojisAsync(ulong guildId);
    Task<Emoji?> GetGuildEmojiAsync(ulong guildId, ulong emojiId);
    Task<Emoji?> CreateGuildEmojiAsync(ulong guildId, CreateGuildEmojiRequest request, string? reason = null);
    Task<Emoji?> ModifyGuildEmojiAsync(ulong guildId, ulong emojiId, ModifyGuildEmojiRequest request, string? reason = null);
    Task<bool> DeleteGuildEmojiAsync(ulong guildId, ulong emojiId, string? reason = null);

    // Application emoji operations
    Task<List<Emoji>?> ListApplicationEmojisAsync(ulong applicationId);
    Task<Emoji?> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId);
    Task<Emoji?> CreateApplicationEmojiAsync(ulong applicationId, CreateApplicationEmojiRequest request);
    Task<Emoji?> ModifyApplicationEmojiAsync(ulong applicationId, ulong emojiId, ModifyApplicationEmojiRequest request);
    Task<bool> DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId);

    // Guild integration operations
    Task<List<GuildIntegration>?> GetGuildIntegrationsAsync(ulong guildId);
    Task<bool> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId, string? reason = null);

    // Guild invite operations
    Task<List<Invite>?> GetGuildInvitesAsync(ulong guildId);

    // Guild prune operations
    Task<GuildPruneResult?> GetGuildPruneCountAsync(ulong guildId, int? days = null, List<ulong>? includeRoles = null);
    Task<GuildPruneResult?> BeginGuildPruneAsync(ulong guildId, BeginGuildPruneRequest request, string? reason = null);

    // Bulk ban
    Task<BulkGuildBanResponse?> BulkGuildBanAsync(ulong guildId, BulkGuildBanRequest request, string? reason = null);

    // Guild role extras
    Task<Role?> GetGuildRoleAsync(ulong guildId, ulong roleId);
    Task<Dictionary<string, int>?> GetGuildRoleMemberCountsAsync(ulong guildId);

    // Guild incident actions
    Task<GuildIncidentActionsResponse?> ModifyGuildIncidentActionsAsync(ulong guildId, ModifyGuildIncidentActionsRequest request);

    // Current user guild member
    Task<GuildMember?> GetCurrentUserGuildMemberAsync(ulong guildId);

    // Reaction extras
    Task<bool> DeleteAllReactionsAsync(ulong channelId, ulong messageId);
    Task<bool> DeleteAllReactionsForEmojiAsync(ulong channelId, ulong messageId, string emoji);

    // Soundboard
    Task<bool> SendSoundboardSoundAsync(ulong channelId, SendSoundboardSoundRequest request);

    // Voice states
    Task<bool> ModifyCurrentUserVoiceStateAsync(ulong guildId, ModifyCurrentUserVoiceStateRequest request);
    Task<bool> ModifyUserVoiceStateAsync(ulong guildId, ulong userId, ModifyUserVoiceStateRequest request);

    // User application role connection
    Task<ApplicationRoleConnection?> GetUserApplicationRoleConnectionAsync(ulong applicationId);
    Task<ApplicationRoleConnection?> UpdateUserApplicationRoleConnectionAsync(ulong applicationId, UpdateUserApplicationRoleConnectionRequest request);
}