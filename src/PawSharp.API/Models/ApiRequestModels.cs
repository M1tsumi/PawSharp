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
