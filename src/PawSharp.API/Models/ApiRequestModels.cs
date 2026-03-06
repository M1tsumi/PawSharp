#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Entities;

namespace PawSharp.API.Models;

// Message Request Models
public class CreateMessageRequest
{
    /// <summary>Message text content (max 2000 chars).</summary>
    public string? Content { get; set; }
    /// <summary>Up to 10 embeds to include with the message.</summary>
    public List<Embed>? Embeds { get; set; }
    /// <summary>Interactive components (buttons, select menus, etc.).</summary>
    public List<MessageComponent>? Components { get; set; }
    /// <summary>Whether to send the message as text-to-speech.</summary>
    public bool? Tts { get; set; }
    /// <summary>
    /// Controls which roles/users/groups Discord will actually notify.
    /// Use <see cref="AllowedMentions.None"/> to send a silent message.
    /// </summary>
    public AllowedMentions? AllowedMentions { get; set; }
    /// <summary>Data showing the source of a crosspost, channel follow add, pin, or reply.</summary>
    public MessageReference? MessageReference { get; set; }
    /// <summary>A poll to include with this message.</summary>
    public CreatePollRequest? Poll { get; set; }
    /// <summary>Message flags combined as a bitfield (SUPPRESS_EMBEDS=4, SUPPRESS_NOTIFICATIONS=4096).</summary>
    public int? Flags { get; set; }
    /// <summary>IDs of up to 3 stickers in the server to send in the message.</summary>
    public List<ulong>? StickerIds { get; set; }
    /// <summary>Can be used to verify a message was sent (up to 25 characters).</summary>
    public string? Nonce { get; set; }
    /// <summary>If true, the nonce is checked for uniqueness in the past few minutes.</summary>
    public bool? EnforceNonce { get; set; }
}

public class EditMessageRequest
{
    /// <summary>New message text content (max 2000 chars). Pass an empty string to clear.</summary>
    public string? Content { get; set; }
    /// <summary>Up to 10 embeds. Pass an empty list to remove all embeds.</summary>
    public List<Embed>? Embeds { get; set; }
    /// <summary>Interactive components. Pass an empty list to remove all components.</summary>
    public List<MessageComponent>? Components { get; set; }
    /// <summary>Allowed mentions for the edited message.</summary>
    public AllowedMentions? AllowedMentions { get; set; }
    /// <summary>Edit flags for the message (e.g. SUPPRESS_EMBEDS = 4).</summary>
    public int? Flags { get; set; }
}

// Channel Request Models
public class CreateChannelRequest
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public string? Topic { get; set; }
    public int? Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public int? RateLimitPerUser { get; set; }
    public int? Position { get; set; }
    public ulong? ParentId { get; set; }
    public bool? Nsfw { get; set; }
}

public class ModifyChannelRequest
{
    public string? Name { get; set; }
    public int? Type { get; set; }
    public int? Position { get; set; }
    public string? Topic { get; set; }
    public bool? Nsfw { get; set; }
    public int? RateLimitPerUser { get; set; }
    public int? Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public ulong? ParentId { get; set; }
}

// Guild Request Models
public class CreateGuildRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Icon { get; set; }
    public int VerificationLevel { get; set; }
    public int DefaultMessageNotifications { get; set; }
    public int ExplicitContentFilter { get; set; }
    public List<Role>? Roles { get; set; }
    public List<Channel>? Channels { get; set; }
}

public class ModifyGuildRequest
{
    public string? Name { get; set; }
    public string? Region { get; set; }
    public int? VerificationLevel { get; set; }
    public int? DefaultMessageNotifications { get; set; }
    public int? ExplicitContentFilter { get; set; }
    public ulong? AfkChannelId { get; set; }
    public int? AfkTimeout { get; set; }
    public string? Icon { get; set; }
    public ulong? OwnerId { get; set; }
    public string? Splash { get; set; }
    public string? Banner { get; set; }
    public ulong? SystemChannelId { get; set; }
}

// Member Request Models
public class ModifyGuildMemberRequest
{
    public string? Nick { get; set; }
    public List<ulong>? Roles { get; set; }
    public bool? Mute { get; set; }
    public bool? Deaf { get; set; }
    public ulong? ChannelId { get; set; }
    public DateTimeOffset? CommunicationDisabledUntil { get; set; }
}

public class AddGuildMemberRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string? Nick { get; set; }
    public List<ulong>? Roles { get; set; }
    public bool? Mute { get; set; }
    public bool? Deaf { get; set; }
}

// Role Request Models
public class CreateRoleRequest
{
    public string? Name { get; set; }
    public string? Permissions { get; set; }
    public int? Color { get; set; }
    public bool? Hoist { get; set; }
    public string? Icon { get; set; }
    public string? UnicodeEmoji { get; set; }
    public bool? Mentionable { get; set; }
}

public class ModifyRoleRequest
{
    public string? Name { get; set; }
    public string? Permissions { get; set; }
    public int? Color { get; set; }
    public bool? Hoist { get; set; }
    public string? Icon { get; set; }
    public string? UnicodeEmoji { get; set; }
    public bool? Mentionable { get; set; }
}

// Interaction Response Models
public class InteractionResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public int Type { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public InteractionCallbackData? Data { get; set; }
}

public class InteractionCallbackData
{
    [System.Text.Json.Serialization.JsonPropertyName("tts")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Tts { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("content")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("embeds")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<Embed>? Embeds { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("allowed_mentions")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public AllowedMentions? AllowedMentions { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("flags")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? Flags { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("components")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<MessageComponent>? Components { get; set; }

    /// <summary>Autocomplete result choices. Only used with ApplicationCommandAutocompleteResult responses.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("choices")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<AutocompleteChoice>? Choices { get; set; }

    /// <summary>Title of the modal. Only used with Modal responses.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>Custom ID of the modal. Only used with Modal responses.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("custom_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomId { get; set; }
}

/// <summary>
/// Represents one autocomplete suggestion choice returned to Discord.
/// </summary>
public class AutocompleteChoice
{
    public string Name { get; set; } = string.Empty;
    public object Value { get; set; } = string.Empty;
}

/// <summary>
/// Interaction callback types for responding to interactions.
/// </summary>
public enum InteractionCallbackType
{
    /// <summary>ACK a Ping.</summary>
    Pong = 1,
    /// <summary>Respond to an interaction with a message.</summary>
    ChannelMessageWithSource = 4,
    /// <summary>ACK an interaction and edit a response later, the user sees a loading state.</summary>
    DeferredChannelMessageWithSource = 5,
    /// <summary>For components, ACK an interaction and edit the original message later; the user does not see a loading state.</summary>
    DeferredUpdateMessage = 6,
    /// <summary>For components, edit the message the component was attached to.</summary>
    UpdateMessage = 7,
    /// <summary>Respond to an autocomplete interaction with suggested choices.</summary>
    ApplicationCommandAutocompleteResult = 8,
    /// <summary>Respond to an interaction with a popup modal.</summary>
    Modal = 9,
    /// <summary>Deprecated. Respond to an interaction with an upgrade button.</summary>
    PremiumRequired = 10,
    /// <summary>Launch the Activity associated with the app. Only for apps with Activities enabled.</summary>
    LaunchActivity = 12
}

// Invite Request Models
public class CreateInviteRequest
{
    public int? MaxAge { get; set; }
    public int? MaxUses { get; set; }
    public bool? Temporary { get; set; }
    public bool? Unique { get; set; }
    public int? TargetType { get; set; }
    public ulong? TargetUserId { get; set; }
    public ulong? TargetApplicationId { get; set; }
}

// Slash Command Models
public class CreateApplicationCommandRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ApplicationCommandOption>? Options { get; set; }
    /// <summary>Deprecated. Use DefaultMemberPermissions instead.</summary>
    public bool? DefaultPermission { get; set; }
    public int? Type { get; set; }
    /// <summary>Localization dictionary for the name field.</summary>
    public Dictionary<string, string>? NameLocalizations { get; set; }
    /// <summary>Localization dictionary for the description field.</summary>
    public Dictionary<string, string>? DescriptionLocalizations { get; set; }
    /// <summary>Set of permissions represented as a bit set. Set to "0" to disable for everyone by default.</summary>
    public string? DefaultMemberPermissions { get; set; }
    /// <summary>Deprecated(use Contexts instead); whether the command is available in DMs with the app.</summary>
    public bool? DmPermission { get; set; }
    /// <summary>Installation context(s) where the command is available (0 = GUILD_INSTALL, 1 = USER_INSTALL).</summary>
    public List<int>? IntegrationTypes { get; set; }
    /// <summary>Interaction context(s) where the command can be used (0 = GUILD, 1 = BOT_DM, 2 = PRIVATE_CHANNEL).</summary>
    public List<int>? Contexts { get; set; }
    /// <summary>Whether the command is age-restricted.</summary>
    public bool? Nsfw { get; set; }
}

// Thread/Forum Models
public class CreateThreadRequest
{
    public string Name { get; set; } = string.Empty;
    public int? AutoArchiveDuration { get; set; }
    public int Type { get; set; } // 10 = NEWS_THREAD, 11 = PUBLIC_THREAD, 12 = PRIVATE_THREAD
    public bool? Invitable { get; set; }
    public int? RateLimitPerUser { get; set; }
}

public class ModifyThreadRequest
{
    public string? Name { get; set; }
    public bool? Archived { get; set; }
    public int? AutoArchiveDuration { get; set; }
    public bool? Locked { get; set; }
    public bool? Invitable { get; set; }
    public int? RateLimitPerUser { get; set; }
}

// Webhook Request Models
public class CreateWebhookRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

public class ModifyWebhookRequest
{
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public ulong? ChannelId { get; set; }
}

public class ExecuteWebhookRequest
{
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? Tts { get; set; }
    public List<MessageComponent>? Components { get; set; }
}

// Scheduled Event Request Models
public class CreateGuildScheduledEventRequest
{
    public ulong ChannelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset ScheduledStartTime { get; set; }
    public DateTimeOffset? ScheduledEndTime { get; set; }
    public int PrivacyLevel { get; set; } // 2 = GUILD_ONLY
    public int EntityType { get; set; } // 1 = STAGE_INSTANCE, 2 = VOICE, 3 = EXTERNAL
    public string? EntityMetadataLocation { get; set; } // For EXTERNAL
    public string? Image { get; set; }
}

public class ModifyGuildScheduledEventRequest
{
    public ulong? ChannelId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? ScheduledStartTime { get; set; }
    public DateTimeOffset? ScheduledEndTime { get; set; }
    public int? PrivacyLevel { get; set; }
    public int? EntityType { get; set; }
    public string? EntityMetadataLocation { get; set; }
    public string? Image { get; set; }
    public int? Status { get; set; } // 1 = SCHEDULED, 2 = ACTIVE, 3 = COMPLETED, 4 = CANCELLED
}

// Auto Moderation Request Models
public class CreateAutoModerationRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public int EventType { get; set; } // 1 = MESSAGE_SEND
    public int TriggerType { get; set; } // 1 = KEYWORD, 3 = SPAM, etc.
    public AutoModerationTriggerMetadata? TriggerMetadata { get; set; }
    public List<AutoModerationAction>? Actions { get; set; }
    public bool? Enabled { get; set; }
    public List<ulong>? ExemptRoles { get; set; }
    public List<ulong>? ExemptChannels { get; set; }
}

public class ModifyAutoModerationRuleRequest
{
    public string? Name { get; set; }
    public int? EventType { get; set; }
    public int? TriggerType { get; set; }
    public AutoModerationTriggerMetadata? TriggerMetadata { get; set; }
    public List<AutoModerationAction>? Actions { get; set; }
    public bool? Enabled { get; set; }
    public List<ulong>? ExemptRoles { get; set; }
    public List<ulong>? ExemptChannels { get; set; }
}

// Stage Instance Request Models
public class CreateStageInstanceRequest
{
    /// <summary>The id of the Stage channel.</summary>
    public ulong ChannelId { get; set; }
    /// <summary>The topic of the Stage instance (1-120 characters).</summary>
    public string Topic { get; set; } = string.Empty;
    /// <summary>1 = PUBLIC, 2 = GUILD_ONLY. Defaults to GUILD_ONLY.</summary>
    public int? PrivacyLevel { get; set; }
    /// <summary>Notify @everyone that a Stage instance has started.</summary>
    public bool? SendStartNotification { get; set; }
    /// <summary>The id of the scheduled event associated with this Stage instance.</summary>
    public ulong? GuildScheduledEventId { get; set; }
}

public class ModifyStageInstanceRequest
{
    public string? Topic { get; set; }
    public int? PrivacyLevel { get; set; }
}

// Sticker Request Models
public class CreateGuildStickerRequest
{
    /// <summary>Name of the sticker (2-30 characters).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Description of the sticker (empty or 2-100 characters).</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Autocomplete/suggestion tags for the sticker (max 200 characters, comma-separated).</summary>
    public string Tags { get; set; } = string.Empty;
    /// <summary>The sticker file bytes. Must be uploaded as multipart/form-data.</summary>
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
}

public class ModifyGuildStickerRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

// Channel Permission Overwrite Request Model
public class EditChannelPermissionsRequest
{
    /// <summary>The bitwise value of all allowed permissions.</summary>
    public string? Allow { get; set; }
    /// <summary>The bitwise value of all disallowed permissions.</summary>
    public string? Deny { get; set; }
    /// <summary>0 = role, 1 = member.</summary>
    public int Type { get; set; }
}

// ── Alpha12 request models ────────────────────────────────────────────────────

// Poll Request Models
public class CreatePollRequest
{
    /// <summary>The question of the poll. Only text is supported.</summary>
    public PollMediaRequest Question { get; set; } = null!;
    /// <summary>Each of the answers available in the poll (max 10).</summary>
    public List<PollAnswerRequest> Answers { get; set; } = new();
    /// <summary>Number of hours the poll should be open for, up to 32 days (768 hours).</summary>
    public int Duration { get; set; }
    /// <summary>Whether a user can select multiple answers.</summary>
    public bool AllowMultiselect { get; set; }
    /// <summary>The layout type of the poll. Defaults to Default (1).</summary>
    public int? LayoutType { get; set; }
}

public class PollMediaRequest
{
    public string? Text { get; set; }
    public object? Emoji { get; set; }
}

public class PollAnswerRequest
{
    public PollMediaRequest PollMedia { get; set; } = null!;
}

// Test Entitlement Request Models
public class CreateTestEntitlementRequest
{
    /// <summary>ID of the SKU to grant the entitlement to.</summary>
    public ulong SkuId { get; set; }
    /// <summary>ID of the guild or user to grant the entitlement to.</summary>
    public ulong OwnerId { get; set; }
    /// <summary>1 = guild, 2 = user.</summary>
    public int OwnerType { get; set; }
}

// Soundboard Request Models
public class CreateGuildSoundboardSoundRequest
{
    /// <summary>Name of the soundboard sound (2-32 characters).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The mp3, ogg, or aac sound file data (base64 encoded), max 512KB.</summary>
    public string Sound { get; set; } = string.Empty;
    /// <summary>Volume of the soundboard sound (0 to 1). Defaults to 1.</summary>
    public double? Volume { get; set; }
    /// <summary>The id of the custom emoji for the soundboard sound.</summary>
    public ulong? EmojiId { get; set; }
    /// <summary>The unicode character of a standard emoji for the soundboard sound.</summary>
    public string? EmojiName { get; set; }
}

public class ModifyGuildSoundboardSoundRequest
{
    /// <summary>Name of the soundboard sound (2-32 characters).</summary>
    public string? Name { get; set; }
    /// <summary>Volume of the soundboard sound (0 to 1).</summary>
    public double? Volume { get; set; }
    /// <summary>The id of the custom emoji for the soundboard sound.</summary>
    public ulong? EmojiId { get; set; }
    /// <summary>The unicode character of a standard emoji for the soundboard sound.</summary>
    public string? EmojiName { get; set; }
}

// Guild Onboarding Request Models
public class ModifyGuildOnboardingRequest
{
    /// <summary>Prompts shown during onboarding and in customize community.</summary>
    public List<OnboardingPromptRequest>? Prompts { get; set; }
    /// <summary>Channel IDs that members get opted into automatically.</summary>
    public List<ulong>? DefaultChannelIds { get; set; }
    /// <summary>Whether onboarding is enabled in the guild.</summary>
    public bool? Enabled { get; set; }
    /// <summary>Current mode of onboarding.</summary>
    public int? Mode { get; set; }
}

public class OnboardingPromptRequest
{
    public ulong? Id { get; set; }
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<OnboardingPromptOptionRequest> Options { get; set; } = new();
    public bool SingleSelect { get; set; }
    public bool Required { get; set; }
    public bool InOnboarding { get; set; }
}

public class OnboardingPromptOptionRequest
{
    public ulong? Id { get; set; }
    public List<ulong>? ChannelIds { get; set; }
    public List<ulong>? RoleIds { get; set; }
    public object? Emoji { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ── Internal response wrapper types ──────────────────────────────────────────

/// <summary>Wraps Discord's poll answer voters response { "users": [...] }.</summary>
public class PollVotersResponse
{
    [JsonPropertyName("users")]
    public List<User>? Users { get; set; }
}

/// <summary>Wraps Discord's guild soundboard sounds response { "items": [...] }.</summary>
public class GuildSoundboardSoundsResponse
{
    [JsonPropertyName("items")]
    public List<SoundboardSound>? Items { get; set; }
}

// ── Alpha13 request models ────────────────────────────────────────────────────

// Guild Template Request Models

public class CreateGuildTemplateRequest
{
    /// <summary>Name of the template (1-100 characters).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Description for the template (0-120 characters).</summary>
    public string? Description { get; set; }
}

public class ModifyGuildTemplateRequest
{
    /// <summary>New name for the template (1-100 characters).</summary>
    public string? Name { get; set; }
    /// <summary>New description for the template (0-120 characters).</summary>
    public string? Description { get; set; }
}

public class CreateGuildFromTemplateRequest
{
    /// <summary>Name of the guild (2-100 characters).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Base64-encoded 128x128 image for the guild icon.</summary>
    public string? Icon { get; set; }
}

// Guild Widget / Welcome Screen Request Models

public class ModifyGuildWidgetRequest
{
    /// <summary>Whether the widget is enabled.</summary>
    public bool? Enabled { get; set; }
    /// <summary>The widget channel ID, or null to remove.</summary>
    public ulong? ChannelId { get; set; }
}

public class ModifyGuildWelcomeScreenRequest
{
    /// <summary>Whether the welcome screen is enabled.</summary>
    public bool? Enabled { get; set; }
    /// <summary>Channels shown in the welcome screen (max 5).</summary>
    public List<WelcomeScreenChannelRequest>? WelcomeChannels { get; set; }
    /// <summary>The server description shown in the welcome screen.</summary>
    public string? Description { get; set; }
}

public class WelcomeScreenChannelRequest
{
    /// <summary>The channel ID to feature.</summary>
    public ulong ChannelId { get; set; }
    /// <summary>Description of the channel (max 42 characters).</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>ID of a custom emoji to display, or null for a unicode emoji.</summary>
    public ulong? EmojiId { get; set; }
    /// <summary>Unicode character of a standard emoji, or null for a custom emoji.</summary>
    public string? EmojiName { get; set; }
}

// Channel / Role Position Request Models

/// <summary>Used to reorder channels within a guild (PATCH /guilds/{id}/channels).</summary>
public class ModifyChannelPositionRequest
{
    /// <summary>Channel ID.</summary>
    public ulong Id { get; set; }
    /// <summary>New sort position (null to leave unchanged).</summary>
    public int? Position { get; set; }
    /// <summary>Syncs the permission overwrites with the new parent category. Only applicable to non-category channels.</summary>
    public bool? LockPermissions { get; set; }
    /// <summary>The new parent category for a channel.</summary>
    public ulong? ParentId { get; set; }
}

/// <summary>Used to reorder roles within a guild (PATCH /guilds/{id}/roles).</summary>
public class ModifyRolePositionRequest
{
    /// <summary>Role ID.</summary>
    public ulong Id { get; set; }
    /// <summary>New sort position.</summary>
    public int? Position { get; set; }
}

// ── Emoji Request / Response Models ──────────────────────────────────────────

/// <summary>POST /guilds/{guild.id}/emojis — create a guild emoji.</summary>
public class CreateGuildEmojiRequest
{
    /// <summary>Name for the emoji.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Base64 encoded image (data:image/png;base64,...). Max 256 KB.</summary>
    public string Image { get; set; } = string.Empty;
    /// <summary>Roles allowed to use this emoji. Empty means available to everyone.</summary>
    public List<ulong>? Roles { get; set; }
}

/// <summary>PATCH /guilds/{guild.id}/emojis/{emoji.id} — modify a guild emoji.</summary>
public class ModifyGuildEmojiRequest
{
    public string? Name { get; set; }
    public List<ulong>? Roles { get; set; }
}

/// <summary>POST /applications/{application.id}/emojis — create an application emoji.</summary>
public class CreateApplicationEmojiRequest
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Base64 encoded image (data:image/png;base64,...). Max 256 KB.</summary>
    public string Image { get; set; } = string.Empty;
}

/// <summary>PATCH /applications/{application.id}/emojis/{emoji.id} — modify an application emoji.</summary>
public class ModifyApplicationEmojiRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Response wrapper for GET /applications/{application.id}/emojis.</summary>
public class ApplicationEmojiListResponse
{
    [JsonPropertyName("items")]
    public List<Emoji>? Items { get; set; }
}

// ── Application Management ────────────────────────────────────────────────────

/// <summary>PATCH /applications/@me — edit the current application.</summary>
public class EditCurrentApplicationRequest
{
    public string? CustomInstallUrl { get; set; }
    public string? Description { get; set; }
    public string? RoleConnectionsVerificationUrl { get; set; }
    public string? InteractionsEndpointUrl { get; set; }
    public int? Flags { get; set; }
    /// <summary>Base64 encoded icon image data (data:image/png;base64,...).</summary>
    public string? Icon { get; set; }
    /// <summary>Base64 encoded cover image data.</summary>
    public string? CoverImage { get; set; }
    public List<string>? Tags { get; set; }
    public string? EventWebhooksUrl { get; set; }
    /// <summary>1 = disabled, 2 = enabled.</summary>
    public int? EventWebhooksStatus { get; set; }
    public List<string>? EventWebhooksTypes { get; set; }
}

// ── Guild Prune ───────────────────────────────────────────────────────────────

/// <summary>Returned by GET and POST /guilds/{id}/prune.</summary>
public class GuildPruneResult
{
    [JsonPropertyName("pruned")]
    public int? Pruned { get; set; }
}

/// <summary>POST /guilds/{id}/prune — begin guild prune.</summary>
public class BeginGuildPruneRequest
{
    public int? Days { get; set; }
    public bool? ComputePruneCount { get; set; }
    public List<ulong>? IncludeRoles { get; set; }
}

// ── Bulk Ban ──────────────────────────────────────────────────────────────────

/// <summary>POST /guilds/{id}/bulk-ban — ban up to 200 users.</summary>
public class BulkGuildBanRequest
{
    public List<ulong> UserIds { get; set; } = new();
    /// <summary>Number of seconds to delete messages for (0–604800). Default 0.</summary>
    public int? DeleteMessageSeconds { get; set; }
}

/// <summary>Response object from POST /guilds/{id}/bulk-ban.</summary>
public class BulkGuildBanResponse
{
    public List<ulong> BannedUsers { get; set; } = new();
    public List<ulong> FailedUsers { get; set; } = new();
}

// ── Guild Incident Actions ────────────────────────────────────────────────────

/// <summary>PUT /guilds/{id}/incident-actions — modify guild incident actions.</summary>
public class ModifyGuildIncidentActionsRequest
{
    public DateTimeOffset? InvitesDisabledUntil { get; set; }
    public DateTimeOffset? DmsDisabledUntil { get; set; }
}

// ── Guild Integration ─────────────────────────────────────────────────────────

/// <summary>Minimal representation of a guild integration returned by GET /guilds/{id}/integrations.</summary>
public class GuildIntegration
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("syncing")]
    public bool? Syncing { get; set; }

    [JsonPropertyName("role_id")]
    public ulong? RoleId { get; set; }

    [JsonPropertyName("expire_behavior")]
    public int? ExpireBehavior { get; set; }

    [JsonPropertyName("expire_grace_period")]
    public int? ExpireGracePeriod { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("application_id")]
    public ulong? ApplicationId { get; set; }
}
// ── Soundboard ───────────────────────────────────────────────────────────────

/// <summary>POST /channels/{channel.id}/send-soundboard-sound — play a soundboard sound.</summary>
public class SendSoundboardSoundRequest
{
    /// <summary>The id of the soundboard sound to play.</summary>
    [JsonPropertyName("sound_id")]
    public ulong SoundId { get; set; }

    /// <summary>The id of the guild the soundboard sound is from (required for non-default sounds).</summary>
    [JsonPropertyName("source_guild_id")]
    public ulong? SourceGuildId { get; set; }
}

// ── Voice State ───────────────────────────────────────────────────────────────

/// <summary>PATCH /guilds/{guild.id}/voice-states/@me — modify the current user's voice state.</summary>
public class ModifyCurrentUserVoiceStateRequest
{
    /// <summary>The id of the channel the user is currently in (null to disconnect).</summary>
    [JsonPropertyName("channel_id")]
    public ulong? ChannelId { get; set; }

    /// <summary>Toggles the user's suppress state.</summary>
    [JsonPropertyName("suppress")]
    public bool? Suppress { get; set; }

    /// <summary>Sets the user's request to speak (ISO8601 timestamp, null to cancel).</summary>
    [JsonPropertyName("request_to_speak_timestamp")]
    public DateTimeOffset? RequestToSpeakTimestamp { get; set; }
}

/// <summary>PATCH /guilds/{guild.id}/voice-states/{user.id} — modify another user's voice state.</summary>
public class ModifyUserVoiceStateRequest
{
    /// <summary>The id of the channel the user is currently in.</summary>
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }

    /// <summary>Toggles the user's suppress state.</summary>
    [JsonPropertyName("suppress")]
    public bool? Suppress { get; set; }
}

// ── User Application Role Connection ─────────────────────────────────────────

/// <summary>
/// A user's linked role connection metadata for an application.
/// Returned by GET /users/@me/applications/{application.id}/role-connection.
/// </summary>
public class ApplicationRoleConnection
{
    /// <summary>The vanity name of the platform a bot has connected (max 50 chars).</summary>
    [JsonPropertyName("platform_name")]
    public string? PlatformName { get; set; }

    /// <summary>The username on the platform a bot has connected (max 100 chars).</summary>
    [JsonPropertyName("platform_username")]
    public string? PlatformUsername { get; set; }

    /// <summary>Object mapping application role connection metadata keys to their string-ified value.</summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>PUT /users/@me/applications/{application.id}/role-connection — update user's role connection.</summary>
public class UpdateUserApplicationRoleConnectionRequest
{
    [JsonPropertyName("platform_name")]
    public string? PlatformName { get; set; }

    [JsonPropertyName("platform_username")]
    public string? PlatformUsername { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

// ── Archived Threads Response ─────────────────────────────────────────────────

/// <summary>
/// Response model for Discord's archived thread list endpoints.
/// Returned by GET /channels/{id}/threads/archived/public,
/// GET /channels/{id}/threads/archived/private, and
/// GET /channels/{id}/users/@me/threads/archived/private.
/// </summary>
public class ArchivedThreadsResponse
{
    /// <summary>The archived thread channels.</summary>
    [JsonPropertyName("threads")]
    public List<Channel> Threads { get; set; } = new();

    /// <summary>Thread member objects for the current user in each returned thread.</summary>
    [JsonPropertyName("members")]
    public List<ThreadMember> Members { get; set; } = new();

    /// <summary>Whether there are additional archived threads beyond this page.</summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}