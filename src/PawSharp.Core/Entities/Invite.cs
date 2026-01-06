#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a guild invite.
/// </summary>
public class Invite : DiscordEntity
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
    public Guild? Guild { get; set; }
    
    /// <summary>
    /// The channel this invite is for.
    /// </summary>
    [JsonPropertyName("channel")]
    public Channel? Channel { get; set; }
    
    /// <summary>
    /// The user who created the invite.
    /// </summary>
    [JsonPropertyName("inviter")]
    public User? Inviter { get; set; }
    
    /// <summary>
    /// The type of target for this voice channel invite.
    /// </summary>
    [JsonPropertyName("target_type")]
    public InviteTargetType? TargetType { get; set; }
    
    /// <summary>
    /// The user whose stream to display for this voice channel stream invite.
    /// </summary>
    [JsonPropertyName("target_user")]
    public User? TargetUser { get; set; }
    
    /// <summary>
    /// The embedded application to open for this voice channel embedded application invite.
    /// </summary>
    [JsonPropertyName("target_application")]
    public Application? TargetApplication { get; set; }
    
    /// <summary>
    /// Approximate count of online members, returned from the GET /invites/&lt;code&gt; endpoint when with_counts is true.
    /// </summary>
    [JsonPropertyName("approximate_presence_count")]
    public int? ApproximatePresenceCount { get; set; }
    
    /// <summary>
    /// Approximate count of total members, returned from the GET /invites/&lt;code&gt; endpoint when with_counts is true.
    /// </summary>
    [JsonPropertyName("approximate_member_count")]
    public int? ApproximateMemberCount { get; set; }
    
    /// <summary>
    /// The expiration date of this invite, returned from the GET /invites/&lt;code&gt; endpoint when with_expiration is true.
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }
    
    /// <summary>
    /// Stage instance data if there is a public Stage instance in the Stage channel this invite is for.
    /// </summary>
    [JsonPropertyName("stage_instance")]
    public InviteStageInstance? StageInstance { get; set; }
    
    /// <summary>
    /// Guild scheduled event data, only included if guild_scheduled_event_id contains a valid guild scheduled event id.
    /// </summary>
    [JsonPropertyName("guild_scheduled_event")]
    public GuildScheduledEvent? GuildScheduledEvent { get; set; }
    
    /// <summary>
    /// Number of times this invite has been used.
    /// </summary>
    [JsonPropertyName("uses")]
    public int Uses { get; set; }
    
    /// <summary>
    /// Max number of times this invite can be used.
    /// </summary>
    [JsonPropertyName("max_uses")]
    public int MaxUses { get; set; }
    
    /// <summary>
    /// Duration (in seconds) after which the invite expires.
    /// </summary>
    [JsonPropertyName("max_age")]
    public int MaxAge { get; set; }
    
    /// <summary>
    /// Whether this invite only grants temporary membership.
    /// </summary>
    [JsonPropertyName("temporary")]
    public bool Temporary { get; set; }
    
    /// <summary>
    /// When this invite was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public new DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Represents the stage instance data for an invite.
/// </summary>
public class InviteStageInstance
{
    /// <summary>
    /// The members speaking in the Stage.
    /// </summary>
    [JsonPropertyName("members")]
    public List<GuildMember> Members { get; set; } = new();
    
    /// <summary>
    /// The number of users in the Stage.
    /// </summary>
    [JsonPropertyName("participant_count")]
    public int ParticipantCount { get; set; }
    
    /// <summary>
    /// The number of users speaking in the Stage.
    /// </summary>
    [JsonPropertyName("speaker_count")]
    public int SpeakerCount { get; set; }
    
    /// <summary>
    /// The topic of the Stage instance (1-120 characters).
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
}

/// <summary>
/// Invite target type.
/// </summary>
public enum InviteTargetType
{
    /// <summary>
    /// Stream.
    /// </summary>
    Stream = 1,
    
    /// <summary>
    /// Embedded Application.
    /// </summary>
    EmbeddedApplication = 2
}