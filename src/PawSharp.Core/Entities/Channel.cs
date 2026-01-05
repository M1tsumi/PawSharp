#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Enums;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord channel.
/// </summary>
public class Channel : DiscordEntity
{
    /// <summary>
    /// The type of channel.
    /// </summary>
    [JsonPropertyName("type")]
    public ChannelType Type { get; set; }
    
    /// <summary>
    /// The id of the guild (may be missing for some channel objects received over gateway guild dispatches).
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// Sorting position of the channel.
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }
    
    /// <summary>
    /// The name of the channel (1-100 characters).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// The channel topic (0-1024 characters).
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }
    
    /// <summary>
    /// Whether the channel is nsfw.
    /// </summary>
    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }
    
    /// <summary>
    /// The id of the last message sent in this channel (may not point to an existing or valid message).
    /// </summary>
    [JsonPropertyName("last_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LastMessageId { get; set; }
    
    /// <summary>
    /// The bitrate (in bits) of the voice channel.
    /// </summary>
    [JsonPropertyName("bitrate")]
    public int? Bitrate { get; set; }
    
    /// <summary>
    /// The user limit of the voice channel.
    /// </summary>
    [JsonPropertyName("user_limit")]
    public int? UserLimit { get; set; }
    
    /// <summary>
    /// Amount of seconds a user has to wait before sending another message (0-21600).
    /// </summary>
    [JsonPropertyName("rate_limit_per_user")]
    public int? RateLimitPerUser { get; set; }
    
    /// <summary>
    /// The recipients of the DM.
    /// </summary>
    [JsonPropertyName("recipients")]
    public List<User>? Recipients { get; set; }
    
    /// <summary>
    /// Icon hash of the group DM.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Id of the creator of the group DM or thread.
    /// </summary>
    [JsonPropertyName("owner_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? OwnerId { get; set; }
    
    /// <summary>
    /// Application id of the group DM creator if it is bot-created.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
    
    /// <summary>
    /// For guild channels: id of the parent category for a channel.
    /// </summary>
    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ParentId { get; set; }
    
    /// <summary>
    /// When the last pinned message was pinned.
    /// </summary>
    [JsonPropertyName("last_pin_timestamp")]
    public DateTimeOffset? LastPinTimestamp { get; set; }
    
    /// <summary>
    /// Voice region id for the voice channel, automatic when set to null.
    /// </summary>
    [JsonPropertyName("rtc_region")]
    public string? RtcRegion { get; set; }
    
    /// <summary>
    /// The camera video quality mode of the voice channel.
    /// </summary>
    [JsonPropertyName("video_quality_mode")]
    public int? VideoQualityMode { get; set; }
    
    /// <summary>
    /// Number of messages (not including the initial message or deleted messages) in a thread.
    /// </summary>
    [JsonPropertyName("message_count")]
    public int? MessageCount { get; set; }
    
    /// <summary>
    /// An approximate count of users in a thread, stops counting at 50.
    /// </summary>
    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }
    
    /// <summary>
    /// Default duration that the clients use (not the API) for newly created threads.
    /// </summary>
    [JsonPropertyName("default_auto_archive_duration")]
    public int? DefaultAutoArchiveDuration { get; set; }
    
    /// <summary>
    /// Computed permissions for the invoking user in the channel.
    /// </summary>
    [JsonPropertyName("permissions")]
    public string? Permissions { get; set; }
    
    /// <summary>
    /// Channel flags combined as a bitfield.
    /// </summary>
    [JsonPropertyName("flags")]
    public int? Flags { get; set; }

}
