#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord thread.
/// </summary>
public class Thread : Channel
{
    /// <summary>
    /// ID of the user that created this thread.
    /// </summary>
    [JsonPropertyName("owner_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public new ulong OwnerId { get; set; }
    
    /// <summary>
    /// Timestamp when the thread was created.
    /// </summary>
    [JsonPropertyName("thread_metadata")]
    public ThreadMetadata? Metadata { get; set; }
    
    /// <summary>
    /// Thread member object for the current user, if they have joined the thread.
    /// </summary>
    [JsonPropertyName("member")]
    public ThreadMember? Member { get; set; }
    
    /// <summary>
    /// Thread-specific fields not needed by other channel types.
    /// </summary>
    [JsonPropertyName("total_message_sent")]
    public int? TotalMessageSent { get; set; }
    
    /// <summary>
    /// Number of messages ever sent in a thread, it's similar to message_count on message creation, but will not decrement the number when a message is deleted.
    /// </summary>
    [JsonPropertyName("message_count")]
    public new int MessageCount { get; set; }
    
    /// <summary>
    /// An approximate count of users in a thread, stops counting at 50.
    /// </summary>
    [JsonPropertyName("member_count")]
    public new int MemberCount { get; set; }
}

/// <summary>
/// Thread metadata object.
/// </summary>
public class ThreadMetadata
{
    /// <summary>
    /// Whether the thread is archived.
    /// </summary>
    [JsonPropertyName("archived")]
    public bool Archived { get; set; }
    
    /// <summary>
    /// Duration in minutes to automatically archive the thread after recent activity.
    /// </summary>
    [JsonPropertyName("auto_archive_duration")]
    public int AutoArchiveDuration { get; set; }
    
    /// <summary>
    /// Timestamp when the thread's archive status was last changed.
    /// </summary>
    [JsonPropertyName("archive_timestamp")]
    public DateTimeOffset ArchiveTimestamp { get; set; }
    
    /// <summary>
    /// Whether the thread is locked.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool? Locked { get; set; }
    
    /// <summary>
    /// Whether non-moderators can add other non-moderators to a thread.
    /// </summary>
    [JsonPropertyName("invitable")]
    public bool? Invitable { get; set; }
    
    /// <summary>
    /// Timestamp when the thread was created.
    /// </summary>
    [JsonPropertyName("create_timestamp")]
    public DateTimeOffset? CreateTimestamp { get; set; }
}

/// <summary>
/// Thread member object.
/// </summary>
public class ThreadMember
{
    /// <summary>
    /// ID of the thread.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    /// <summary>
    /// ID of the user.
    /// </summary>
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }
    
    /// <summary>
    /// Time the current user last joined the thread.
    /// </summary>
    [JsonPropertyName("join_timestamp")]
    public DateTimeOffset JoinTimestamp { get; set; }
    
    /// <summary>
    /// Any user-thread settings, currently only used for notifications.
    /// </summary>
    [JsonPropertyName("flags")]
    public int Flags { get; set; }
}

/// <summary>
/// Forum tag object.
/// </summary>
public class ForumTag
{
    /// <summary>
    /// The id of the tag.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get { return _id; } set { _id = value; } }
    private ulong _id;
    
    /// <summary>
    /// The name of the tag (0-20 characters).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this tag can only be added to or removed from threads by a member with the MANAGE_THREADS permission.
    /// </summary>
    [JsonPropertyName("moderated")]
    public bool Moderated { get; set; }
    
    /// <summary>
    /// The id of a guild's custom emoji.
    /// </summary>
    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }
    
    /// <summary>
    /// The unicode character of the emoji.
    /// </summary>
    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }
}

/// <summary>
/// Default reaction object.
/// </summary>
public class DefaultReaction
{
    /// <summary>
    /// The id of a guild's custom emoji.
    /// </summary>
    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }
    
    /// <summary>
    /// The unicode character of the emoji.
    /// </summary>
    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }
}