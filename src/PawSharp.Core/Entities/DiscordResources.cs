#nullable enable
using System.Text.Json.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents gateway connection information returned by GET /gateway/bot.
/// </summary>
public class GatewayBotInfo
{
    /// <summary>The WSS URL to connect to.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Discord's recommended shard count for this bot.</summary>
    [JsonPropertyName("shards")]
    public int Shards { get; set; }

    /// <summary>Information about the current session start limit.</summary>
    [JsonPropertyName("session_start_limit")]
    public SessionStartLimit SessionStartLimit { get; set; } = null!;
}

/// <summary>
/// Session start limit information returned alongside gateway bot info.
/// </summary>
public class SessionStartLimit
{
    /// <summary>Total number of session starts the current user is allowed.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Remaining number of session starts the current user is allowed.</summary>
    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }

    /// <summary>Milliseconds after which the limit resets.</summary>
    [JsonPropertyName("reset_after")]
    public int ResetAfter { get; set; }

    /// <summary>Number of identify requests allowed per 5 seconds.</summary>
    [JsonPropertyName("max_concurrency")]
    public int MaxConcurrency { get; set; }
}

/// <summary>
/// Represents a Discord voice region.
/// </summary>
public class VoiceRegion
{
    /// <summary>Unique ID for the region.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Name of the region.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this is the closest region to the current user's client.</summary>
    [JsonPropertyName("optimal")]
    public bool Optimal { get; set; }

    /// <summary>Whether this is a deprecated voice region.</summary>
    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; }

    /// <summary>Whether this is a custom voice region (used for events/etc).</summary>
    [JsonPropertyName("custom")]
    public bool Custom { get; set; }
}

/// <summary>
/// Represents a user's connection to a third-party service (e.g. Twitch, GitHub).
/// </summary>
public class UserConnection
{
    /// <summary>ID of the connection account.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>The username of the connection account.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The service this connection is with (e.g. "twitch", "github").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Whether the connection is revoked.</summary>
    [JsonPropertyName("revoked")]
    public bool? Revoked { get; set; }

    /// <summary>Whether the connection is verified.</summary>
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    /// <summary>Whether friend sync is enabled for this connection.</summary>
    [JsonPropertyName("friend_sync")]
    public bool FriendSync { get; set; }

    /// <summary>Whether activities related to this connection will be shown in presence updates.</summary>
    [JsonPropertyName("show_activity")]
    public bool ShowActivity { get; set; }

    /// <summary>Whether this connection has a corresponding third party OAuth2 token.</summary>
    [JsonPropertyName("two_way_link")]
    public bool TwoWayLink { get; set; }

    /// <summary>Visibility of this connection (0 = None, 1 = Everyone).</summary>
    [JsonPropertyName("visibility")]
    public int Visibility { get; set; }
}

// Represents a Nitro sticker pack. Alias kept for documentation — see StickerPack in Sticker.cs.
// NOTE: StickerPack is already defined in Sticker.cs — this marker comment keeps the file non-empty.
