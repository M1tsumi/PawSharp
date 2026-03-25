#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a running Discord embedded-application (Activity) instance.
/// Returned by <c>GET /applications/{application.id}/activity-instances/{instance.id}</c>.
/// </summary>
public class ActivityInstance
{
    /// <summary>Application ID of the activity.</summary>
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("application_id")]
    public ulong ApplicationId { get; set; }

    /// <summary>Unique string identifier of the instance.</summary>
    [JsonPropertyName("instance_id")]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Snowflake of the launch event that created this instance.</summary>
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("launch_id")]
    public ulong LaunchId { get; set; }

    /// <summary>Location where this activity is running.</summary>
    [JsonPropertyName("location")]
    public ActivityLocation Location { get; set; } = new();

    /// <summary>IDs of users currently participating in this activity.</summary>
    [JsonPropertyName("users")]
    public List<ulong> Users { get; set; } = new();
}

/// <summary>
/// Describes the channel/guild context of an <see cref="ActivityInstance"/>.
/// </summary>
public class ActivityLocation
{
    /// <summary>
    /// Unique identifier for this location (not a Discord snowflake — it is a stable
    /// opaque string assigned by Discord).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Where the activity is running: <c>"gc"</c> (guild channel) or <c>"pc"</c>
    /// (private/DM channel).  See <see cref="ActivityLocationKind"/> for constants.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Channel ID the activity is running in.</summary>
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }

    /// <summary>Guild ID, if the activity is in a guild channel.</summary>
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("guild_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong? GuildId { get; set; }

    /// <summary>
    /// Message ID of the activity-launch message, if applicable (e.g. watch-together
    /// sessions in a voice channel that originated from a message).
    /// </summary>
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("message_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ulong? MessageId { get; set; }
}

/// <summary>
/// Well-known values for <see cref="ActivityLocation.Kind"/>.
/// </summary>
public static class ActivityLocationKind
{
    /// <summary>The activity is running inside a guild (server) voice/stage channel.</summary>
    public const string GuildChannel = "gc";

    /// <summary>The activity is running inside a private or group DM channel.</summary>
    public const string PrivateChannel = "pc";
}
