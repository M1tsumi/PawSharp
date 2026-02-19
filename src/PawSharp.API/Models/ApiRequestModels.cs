#nullable enable
using System;
using System.Collections.Generic;
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

/// <summary>
/// Represents a text input component within a modal.
/// </summary>
public class TextInput : MessageComponent
{
    public TextInput() { Type = 4; }

    public string CustomId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>1 = SHORT, 2 = PARAGRAPH</summary>
    public int Style { get; set; } = 1;
    public bool Required { get; set; } = true;
    public string? Placeholder { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Value { get; set; }
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

// Message Component Models
public abstract class MessageComponent
{
    public int Type { get; set; }
}

public class ActionRow : MessageComponent
{
    public ActionRow()
    {
        Type = 1; // ACTION_ROW
    }
    
    public List<MessageComponent> Components { get; set; } = new();
}

public class Button : MessageComponent
{
    public Button()
    {
        Type = 2; // BUTTON
    }
    
    public int Style { get; set; } // 1-5 (PRIMARY, SECONDARY, SUCCESS, DANGER, LINK)
    public string? Label { get; set; }
    public Emoji? Emoji { get; set; }
    public string? CustomId { get; set; } // For non-link buttons
    public string? Url { get; set; } // For link buttons
    public bool? Disabled { get; set; }
}

public class SelectMenu : MessageComponent
{
    public SelectMenu()
    {
        Type = 3; // SELECT_MENU
    }
    
    public string CustomId { get; set; } = string.Empty;
    public List<SelectOption> Options { get; set; } = new();
    public string? Placeholder { get; set; }
    public int? MinValues { get; set; }
    public int? MaxValues { get; set; }
    public bool? Disabled { get; set; }
}

public class SelectOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Emoji? Emoji { get; set; }
    public bool? Default { get; set; }
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
