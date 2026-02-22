#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Entities;

namespace PawSharp.API.Models;

// Message Request Models
public class CreateMessageRequest
{
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public List<MessageComponent>? Components { get; set; }
    public bool? Tts { get; set; }
    public object? AllowedMentions { get; set; }
    public object? MessageReference { get; set; }
    /// <summary>A poll to include with this message.</summary>
    public CreatePollRequest? Poll { get; set; }
}

public class EditMessageRequest
{
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public List<MessageComponent>? Components { get; set; }
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
    public int Type { get; set; }
    public InteractionCallbackData? Data { get; set; }
}

public class InteractionCallbackData
{
    public bool? Tts { get; set; }
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public object? AllowedMentions { get; set; }
    public int? Flags { get; set; }
    public List<MessageComponent>? Components { get; set; }
    /// <summary>
    /// Autocomplete result choices. Only used with ApplicationCommandAutocompleteResult responses.
    /// </summary>
    public List<AutocompleteChoice>? Choices { get; set; }
    /// <summary>
    /// Title of the modal. Only used with Modal responses.
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// Custom ID of the modal. Only used with Modal responses.
    /// </summary>
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
    public bool? DefaultPermission { get; set; }
    public int? Type { get; set; }
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
