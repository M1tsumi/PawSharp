#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a scheduled event in a guild.
/// </summary>
public class GuildScheduledEvent : DiscordEntity
{
    /// <summary>
    /// The channel id of the scheduled event.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }
    
    /// <summary>
    /// The creator of the scheduled event.
    /// </summary>
    [JsonPropertyName("creator")]
    public User? Creator { get; set; }
    
    /// <summary>
    /// The name of the scheduled event (1-100 characters).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The description of the scheduled event (1-1000 characters).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// The time the scheduled event will start.
    /// </summary>
    [JsonPropertyName("scheduled_start_time")]
    public DateTimeOffset ScheduledStartTime { get; set; }
    
    /// <summary>
    /// The time the scheduled event will end, if it does end.
    /// </summary>
    [JsonPropertyName("scheduled_end_time")]
    public DateTimeOffset? ScheduledEndTime { get; set; }
    
    /// <summary>
    /// The privacy level of the scheduled event.
    /// </summary>
    [JsonPropertyName("privacy_level")]
    public GuildScheduledEventPrivacyLevel PrivacyLevel { get; set; }
    
    /// <summary>
    /// The status of the scheduled event.
    /// </summary>
    [JsonPropertyName("status")]
    public GuildScheduledEventStatus Status { get; set; }
    
    /// <summary>
    /// The type of the scheduled event.
    /// </summary>
    [JsonPropertyName("entity_type")]
    public GuildScheduledEventEntityType EntityType { get; set; }
    
    /// <summary>
    /// The id of an entity associated with a guild scheduled event.
    /// </summary>
    [JsonPropertyName("entity_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EntityId { get; set; }
    
    /// <summary>
    /// Additional metadata for the guild scheduled event.
    /// </summary>
    [JsonPropertyName("entity_metadata")]
    public GuildScheduledEventEntityMetadata? EntityMetadata { get; set; }
    
    /// <summary>
    /// The user that created the scheduled event.
    /// </summary>
    [JsonPropertyName("creator_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? CreatorId { get; set; }
    
    /// <summary>
    /// The number of users subscribed to the scheduled event.
    /// </summary>
    [JsonPropertyName("user_count")]
    public int? UserCount { get; set; }
    
    /// <summary>
    /// The cover image hash of the scheduled event.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }
}

/// <summary>
/// Represents the entity metadata for a scheduled event.
/// </summary>
public class GuildScheduledEventEntityMetadata
{
    /// <summary>
    /// Location of the event (1-100 characters).
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

/// <summary>
/// Represents a user subscribed to a scheduled event.
/// </summary>
public class GuildScheduledEventUser
{
    /// <summary>
    /// The scheduled event id which the user subscribed to.
    /// </summary>
    [JsonPropertyName("guild_scheduled_event_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildScheduledEventId { get; set; }
    
    /// <summary>
    /// User which subscribed to an event.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Guild member data for this user for the guild which this event belongs to, if any.
    /// </summary>
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
}

/// <summary>
/// Privacy level of a scheduled event.
/// </summary>
public enum GuildScheduledEventPrivacyLevel
{
    /// <summary>
    /// The scheduled event is only accessible to guild members.
    /// </summary>
    GuildOnly = 2
}

/// <summary>
/// Status of a scheduled event.
/// </summary>
public enum GuildScheduledEventStatus
{
    /// <summary>
    /// The scheduled event has been scheduled.
    /// </summary>
    Scheduled = 1,
    
    /// <summary>
    /// The scheduled event is active.
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// The scheduled event has been completed.
    /// </summary>
    Completed = 3,
    
    /// <summary>
    /// The scheduled event has been canceled.
    /// </summary>
    Canceled = 4
}

/// <summary>
/// Entity type of a scheduled event.
/// </summary>
public enum GuildScheduledEventEntityType
{
    /// <summary>
    /// A stage channel event.
    /// </summary>
    StageInstance = 1,
    
    /// <summary>
    /// A voice channel event.
    /// </summary>
    Voice = 2,
    
    /// <summary>
    /// An external event.
    /// </summary>
    External = 3
}