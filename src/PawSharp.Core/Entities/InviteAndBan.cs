#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a guild ban.
/// </summary>
public class Ban
{
    /// <summary>
    /// The reason for the ban.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// The banned user.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
}

/// <summary>
/// Represents a guild invite.
/// </summary>
public class Invite
{
    /// <summary>
    /// The invite code (unique ID).
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The guild this invite is for.
    /// </summary>
    [JsonPropertyName("guild")]
    public InviteGuild? Guild { get; set; }

    /// <summary>
    /// The channel this invite is for.
    /// </summary>
    [JsonPropertyName("channel")]
    public InviteChannel Channel { get; set; } = null!;

    /// <summary>
    /// The user who created the invite.
    /// </summary>
    [JsonPropertyName("inviter")]
    public User? Inviter { get; set; }

    /// <summary>
    /// The type of target for this voice channel invite.
    /// </summary>
    [JsonPropertyName("target_type")]
    public int? TargetType { get; set; }

    /// <summary>
    /// The user whose stream to display for this voice channel stream invite.
    /// </summary>
    [JsonPropertyName("target_user")]
    public User? TargetUser { get; set; }

    /// <summary>
    /// The embedded application to open for this voice channel embedded application invite.
    /// </summary>
    [JsonPropertyName("target_application")]
    public object? TargetApplication { get; set; }

    /// <summary>
    /// Approximate count of online members, returned from the GET /invites/<code> endpoint when with_counts is true.
    /// </summary>
    [JsonPropertyName("approximate_presence_count")]
    public int? ApproximatePresenceCount { get; set; }

    /// <summary>
    /// Approximate count of total members, returned from the GET /invites/<code> endpoint when with_counts is true.
    /// </summary>
    [JsonPropertyName("approximate_member_count")]
    public int? ApproximateMemberCount { get; set; }

    /// <summary>
    /// The expiration date of this invite, returned from the GET /invites/<code> endpoint when with_expiration is true.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Stage instance data if there is a public Stage instance in the Stage channel this invite is for.
    /// </summary>
    [JsonPropertyName("stage_instance")]
    public object? StageInstance { get; set; }

    /// <summary>
    /// Guild scheduled event data, only included if guild_scheduled_event_id contains a valid guild scheduled event id.
    /// </summary>
    [JsonPropertyName("guild_scheduled_event")]
    public object? GuildScheduledEvent { get; set; }
}

/// <summary>
/// Represents a guild in an invite.
/// </summary>
public class InviteGuild
{
    /// <summary>
    /// Guild ID.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>
    /// Guild name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Guild splash hash.
    /// </summary>
    [JsonPropertyName("splash")]
    public string? Splash { get; set; }

    /// <summary>
    /// Guild banner hash.
    /// </summary>
    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    /// <summary>
    /// Guild description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Guild icon hash.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Guild features.
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Guild verification level.
    /// </summary>
    [JsonPropertyName("verification_level")]
    public int VerificationLevel { get; set; }

    /// <summary>
    /// Guild vanity url code.
    /// </summary>
    [JsonPropertyName("vanity_url_code")]
    public string? VanityUrlCode { get; set; }

    /// <summary>
    /// Whether the guild is partnered.
    /// </summary>
    [JsonPropertyName("partnered")]
    public bool? Partnered { get; set; }

    /// <summary>
    /// Whether the guild has premium progress bar enabled.
    /// </summary>
    [JsonPropertyName("premium_progress_bar_enabled")]
    public bool? PremiumProgressBarEnabled { get; set; }
}

/// <summary>
/// Represents a channel in an invite.
/// </summary>
public class InviteChannel
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>
    /// Channel name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Channel type.
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
}