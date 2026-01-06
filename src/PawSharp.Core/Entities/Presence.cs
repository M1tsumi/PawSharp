#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a user's presence.
/// </summary>
public class Presence
{
    /// <summary>
    /// The user presence is being updated for.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    /// <summary>
    /// The guild id the presence update is for, if any.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// Either "idle", "dnd", "online", or "offline".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// User's current activities.
    /// </summary>
    [JsonPropertyName("activities")]
    public List<Activity> Activities { get; set; } = new();
    
    /// <summary>
    /// User's platform-dependent status.
    /// </summary>
    [JsonPropertyName("client_status")]
    public ClientStatus ClientStatus { get; set; } = null!;
}

/// <summary>
/// Represents a user's activity.
/// </summary>
public class Activity
{
    /// <summary>
    /// The activity's name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Activity type.
    /// </summary>
    [JsonPropertyName("type")]
    public ActivityType Type { get; set; }
    
    /// <summary>
    /// Stream url, is validated when type is 1.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    /// <summary>
    /// When the activity was added to the user's session.
    /// </summary>
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
    
    /// <summary>
    /// Unix timestamps for start and/or end of the game.
    /// </summary>
    [JsonPropertyName("timestamps")]
    public ActivityTimestamps? Timestamps { get; set; }
    
    /// <summary>
    /// Application id for the game.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
    
    /// <summary>
    /// What the player is currently doing.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }
    
    /// <summary>
    /// The user's current party status.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }
    
    /// <summary>
    /// The emoji used for a custom status.
    /// </summary>
    [JsonPropertyName("emoji")]
    public ActivityEmoji? Emoji { get; set; }
    
    /// <summary>
    /// Information for the current party of the player.
    /// </summary>
    [JsonPropertyName("party")]
    public ActivityParty? Party { get; set; }
    
    /// <summary>
    /// Images for the presence and their hover texts.
    /// </summary>
    [JsonPropertyName("assets")]
    public ActivityAssets? Assets { get; set; }
    
    /// <summary>
    /// Secrets for Rich Presence joining and spectating.
    /// </summary>
    [JsonPropertyName("secrets")]
    public ActivitySecrets? Secrets { get; set; }
    
    /// <summary>
    /// Whether or not the activity is an instanced game session.
    /// </summary>
    [JsonPropertyName("instance")]
    public bool? Instance { get; set; }
    
    /// <summary>
    /// Activity flags ORd together, describes what the payload includes.
    /// </summary>
    [JsonPropertyName("flags")]
    public ActivityFlags? Flags { get; set; }
    
    /// <summary>
    /// The custom buttons shown in the Rich Presence (max 2).
    /// </summary>
    [JsonPropertyName("buttons")]
    public List<ActivityButton>? Buttons { get; set; }
}

/// <summary>
/// Represents activity timestamps.
/// </summary>
public class ActivityTimestamps
{
    /// <summary>
    /// Unix time (in milliseconds) of when the activity started.
    /// </summary>
    [JsonPropertyName("start")]
    public long? Start { get; set; }
    
    /// <summary>
    /// Unix time (in milliseconds) of when the activity ends.
    /// </summary>
    [JsonPropertyName("end")]
    public long? End { get; set; }
}

/// <summary>
/// Represents an activity emoji.
/// </summary>
public class ActivityEmoji
{
    /// <summary>
    /// The name of the emoji.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The id of the emoji.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? Id { get; set; }
    
    /// <summary>
    /// Whether this emoji is animated.
    /// </summary>
    [JsonPropertyName("animated")]
    public bool? Animated { get; set; }
}

/// <summary>
/// Represents activity party information.
/// </summary>
public class ActivityParty
{
    /// <summary>
    /// The id of the party.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    /// <summary>
    /// Used to show the party's current and maximum size.
    /// </summary>
    [JsonPropertyName("size")]
    public List<int>? Size { get; set; }
}

/// <summary>
/// Represents activity assets.
/// </summary>
public class ActivityAssets
{
    /// <summary>
    /// The id for a large asset of the activity, usually a snowflake.
    /// </summary>
    [JsonPropertyName("large_image")]
    public string? LargeImage { get; set; }
    
    /// <summary>
    /// Text displayed when hovering over the large image of the activity.
    /// </summary>
    [JsonPropertyName("large_text")]
    public string? LargeText { get; set; }
    
    /// <summary>
    /// The id for a small asset of the activity, usually a snowflake.
    /// </summary>
    [JsonPropertyName("small_image")]
    public string? SmallImage { get; set; }
    
    /// <summary>
    /// Text displayed when hovering over the small image of the activity.
    /// </summary>
    [JsonPropertyName("small_text")]
    public string? SmallText { get; set; }
}

/// <summary>
/// Represents activity secrets.
/// </summary>
public class ActivitySecrets
{
    /// <summary>
    /// The secret for joining a party.
    /// </summary>
    [JsonPropertyName("join")]
    public string? Join { get; set; }
    
    /// <summary>
    /// The secret for spectating a game.
    /// </summary>
    [JsonPropertyName("spectate")]
    public string? Spectate { get; set; }
    
    /// <summary>
    /// The secret for a specific instanced match.
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; set; }
}

/// <summary>
/// Represents an activity button.
/// </summary>
public class ActivityButton
{
    /// <summary>
    /// The text shown on the button (1-32 characters).
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// The url opened when clicking the button (1-512 characters).
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Represents client status.
/// </summary>
public class ClientStatus
{
    /// <summary>
    /// The user's status set for an active desktop (Windows, Linux, Mac) application session.
    /// </summary>
    [JsonPropertyName("desktop")]
    public string? Desktop { get; set; }
    
    /// <summary>
    /// The user's status set for an active mobile (iOS, Android) application session.
    /// </summary>
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    
    /// <summary>
    /// The user's status set for an active web (browser, bot account) application session.
    /// </summary>
    [JsonPropertyName("web")]
    public string? Web { get; set; }
}

/// <summary>
/// Activity type.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// Playing {name}.
    /// </summary>
    Game = 0,
    
    /// <summary>
    /// Streaming {details}.
    /// </summary>
    Streaming = 1,
    
    /// <summary>
    /// Listening to {name}.
    /// </summary>
    Listening = 2,
    
    /// <summary>
    /// Watching {name}.
    /// </summary>
    Watching = 3,
    
    /// <summary>
    /// {emoji} {name}.
    /// </summary>
    Custom = 4,
    
    /// <summary>
    /// Competing in {name}.
    /// </summary>
    Competing = 5
}

/// <summary>
/// Activity flags.
/// </summary>
[Flags]
public enum ActivityFlags
{
    /// <summary>
    /// Instance.
    /// </summary>
    Instance = 1 << 0,
    
    /// <summary>
    /// Join.
    /// </summary>
    Join = 1 << 1,
    
    /// <summary>
    /// Spectate.
    /// </summary>
    Spectate = 1 << 2,
    
    /// <summary>
    /// Join Request.
    /// </summary>
    JoinRequest = 1 << 3,
    
    /// <summary>
    /// Sync.
    /// </summary>
    Sync = 1 << 4,
    
    /// <summary>
    /// Play.
    /// </summary>
    Play = 1 << 5,
    
    /// <summary>
    /// Party Privacy Friends.
    /// </summary>
    PartyPrivacyFriends = 1 << 6,
    
    /// <summary>
    /// Party Privacy Voice Channel.
    /// </summary>
    PartyPrivacyVoiceChannel = 1 << 7,
    
    /// <summary>
    /// Embedded.
    /// </summary>
    Embedded = 1 << 8
}