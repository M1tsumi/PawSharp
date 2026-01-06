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
    public List<object>? Components { get; set; }
    public bool? Tts { get; set; }
    public object? AllowedMentions { get; set; }
    public object? MessageReference { get; set; }
}

public class EditMessageRequest
{
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public List<object>? Components { get; set; }
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
    public List<object>? Components { get; set; }
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
