#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

// ─── Guild Preview ────────────────────────────────────────────────────────────

/// <summary>
/// A preview of a guild that is publicly discoverable or can be previewed with the GUILD_PREVIEW feature.
/// Returned by GET /guilds/{guild.id}/preview.
/// </summary>
public class GuildPreview : DiscordEntity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("splash")]
    public string? Splash { get; set; }

    [JsonPropertyName("discovery_splash")]
    public string? DiscoverySplash { get; set; }

    [JsonPropertyName("emojis")]
    public List<Emoji> Emojis { get; set; } = new();

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("approximate_member_count")]
    public int ApproximateMemberCount { get; set; }

    [JsonPropertyName("approximate_presence_count")]
    public int ApproximatePresenceCount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("stickers")]
    public List<Sticker>? Stickers { get; set; }
}

// ─── Guild Widget ─────────────────────────────────────────────────────────────

/// <summary>Settings for the guild's embeddable widget.</summary>
public class GuildWidgetSettings
{
    /// <summary>Whether the widget is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>The channel ID for the widget's invite link, if set.</summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }
}

// ─── Welcome Screen ───────────────────────────────────────────────────────────

/// <summary>Represents the Welcome Screen shown to new guild members.</summary>
public class WelcomeScreen
{
    /// <summary>The server description shown in the welcome screen.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>The channels shown in the welcome screen (max 5).</summary>
    [JsonPropertyName("welcome_channels")]
    public List<WelcomeScreenChannel> WelcomeChannels { get; set; } = new();
}

/// <summary>A channel entry displayed on the guild welcome screen.</summary>
public class WelcomeScreenChannel
{
    /// <summary>The channel's ID.</summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    /// <summary>The description shown for the channel.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>The emoji ID, if a custom emoji is set.</summary>
    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }

    /// <summary>The emoji name (or unicode character) for the channel, if set.</summary>
    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }
}

// ─── Followed Channel ─────────────────────────────────────────────────────────

/// <summary>
/// Returned when following an Announcement channel.
/// Contains the source channel ID and the newly created webhook ID.
/// </summary>
public class FollowedChannel
{
    /// <summary>The source Announcement channel that was followed.</summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    /// <summary>The webhook created in the target channel to deliver the crossposted messages.</summary>
    [JsonPropertyName("webhook_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong WebhookId { get; set; }
}

// ─── Vanity URL ───────────────────────────────────────────────────────────────

/// <summary>A guild's vanity invite URL information.</summary>
public class VanityUrl
{
    /// <summary>The vanity invite code (or <c>null</c> if the guild has no vanity URL).</summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>Number of times the vanity invite has been used.</summary>
    [JsonPropertyName("uses")]
    public int Uses { get; set; }
}
