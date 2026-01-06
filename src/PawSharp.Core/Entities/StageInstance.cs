#nullable enable
using System;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a stage instance.
/// </summary>
public class StageInstance : DiscordEntity
{
    /// <summary>
    /// The guild id of the associated Stage channel.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    /// <summary>
    /// The id of the associated Stage channel.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }
    
    /// <summary>
    /// The topic of the Stage instance (1-120 characters).
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
    
    /// <summary>
    /// The privacy level of the Stage instance.
    /// </summary>
    [JsonPropertyName("privacy_level")]
    public StageInstancePrivacyLevel PrivacyLevel { get; set; }
    
    /// <summary>
    /// Whether or not Stage discovery is disabled.
    /// </summary>
    [JsonPropertyName("discoverable_disabled")]
    public bool DiscoverableDisabled { get; set; }
    
    /// <summary>
    /// The id of the scheduled event for this Stage instance.
    /// </summary>
    [JsonPropertyName("guild_scheduled_event_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildScheduledEventId { get; set; }
}

/// <summary>
/// Stage instance privacy level.
/// </summary>
public enum StageInstancePrivacyLevel
{
    /// <summary>
    /// The Stage instance is visible publicly. (deprecated)
    /// </summary>
    Public = 1,
    
    /// <summary>
    /// The Stage instance is visible to only guild members.
    /// </summary>
    GuildOnly = 2
}