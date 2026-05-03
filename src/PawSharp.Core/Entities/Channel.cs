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
    /// Explicit permission overwrites for members and roles.
    /// </summary>
    [JsonPropertyName("permission_overwrites")]
    public List<Overwrite>? PermissionOverwrites { get; set; }
    
    /// <summary>
    /// The name of the channel (1-100 characters).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    private string? _validatedName;

    /// <summary>
    /// Gets or sets the validated channel name (1-100 characters).
    /// </summary>
    public string? ValidatedName
    {
        get => _validatedName;
        set
        {
            if (value != null && (value.Length < 1 || value.Length > 100))
                throw new ArgumentException("Channel name must be between 1 and 100 characters.", nameof(value));
            _validatedName = value;
        }
    }
    
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
    public VideoQualityMode? VideoQualityMode { get; set; }
    
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
    [JsonConverter(typeof(NullablePermissionsJsonConverter))]
    public Permissions? Permissions { get; set; }
    
    /// <summary>
    /// Channel flags combined as a bitfield.
    /// </summary>
    [JsonPropertyName("flags")]
    public int? Flags { get; set; }
    
    /// <summary>
    /// The set of tags that can be used in a GUILD_FORUM channel.
    /// </summary>
    [JsonPropertyName("available_tags")]
    public List<ForumTag>? AvailableTags { get; set; }
    
    /// <summary>
    /// The IDs of the set of tags that have been applied to a thread in a GUILD_FORUM channel.
    /// </summary>
    [JsonPropertyName("applied_tags")]
    public List<ulong>? AppliedTags { get; set; }
    
    /// <summary>
    /// The emoji to show in the add reaction button on a thread in a GUILD_FORUM channel.
    /// </summary>
    [JsonPropertyName("default_reaction_emoji")]
    public DefaultReaction? DefaultReactionEmoji { get; set; }
    
    /// <summary>
    /// The initial rate_limit_per_user to set on newly created threads in a channel.
    /// </summary>
    [JsonPropertyName("default_thread_rate_limit_per_user")]
    public int? DefaultThreadRateLimitPerUser { get; set; }
    
    /// <summary>
    /// The default sort order type used to order posts in GUILD_FORUM channels.
    /// </summary>
    [JsonPropertyName("default_sort_order")]
    public SortOrderType? DefaultSortOrder { get; set; }
    
    /// <summary>
    /// The default forum layout view used to display posts in GUILD_FORUM channels.
    /// </summary>
    [JsonPropertyName("default_forum_layout")]
    public ForumLayoutType? DefaultForumLayout { get; set; }

    /// <summary>
    /// The voice channel status text (set by members with permission). Null when cleared.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets whether this is a guild text channel.
    /// </summary>
    public bool IsText => Type == ChannelType.GuildText;

    /// <summary>
    /// Gets whether this is a voice channel.
    /// </summary>
    public bool IsVoice => Type == ChannelType.GuildVoice;

    /// <summary>
    /// Gets whether this is a category channel.
    /// </summary>
    public bool IsCategory => Type == ChannelType.GuildCategory;

    /// <summary>
    /// Gets whether this is a forum channel.
    /// </summary>
    public bool IsForum => Type == ChannelType.GuildForum;

    /// <summary>
    /// Gets whether this is a media channel.
    /// </summary>
    public bool IsMedia => Type == ChannelType.GuildMedia;

    /// <summary>
    /// Gets whether this is a thread channel.
    /// </summary>
    public bool IsThread => Type is ChannelType.PublicThread or ChannelType.PrivateThread or ChannelType.AnnouncementThread;

    /// <summary>
    /// Gets whether this is a direct message channel.
    /// </summary>
    public bool IsDm => Type is ChannelType.DM or ChannelType.GroupDM;

    /// <summary>
    /// Gets whether this is an announcement channel.
    /// </summary>
    public bool IsAnnouncement => Type == ChannelType.GuildAnnouncement;

    /// <summary>
    /// Gets whether this is a stage voice channel.
    /// </summary>
    public bool IsStage => Type == ChannelType.GuildStageVoice;

    /// <summary>
    /// Gets whether this is a guild channel (not a DM).
    /// </summary>
    public bool IsGuildChannel => Type is ChannelType.GuildText or ChannelType.GuildVoice or
        ChannelType.GuildCategory or ChannelType.GuildAnnouncement or
        ChannelType.GuildStageVoice or ChannelType.GuildForum or ChannelType.GuildMedia;

    /// <summary>
    /// Gets whether this channel is NSFW.
    /// </summary>
    public bool IsNsfw => Nsfw == true;
}
