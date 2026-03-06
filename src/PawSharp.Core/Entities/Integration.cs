#nullable enable
using System;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord guild integration (e.g. Twitch, YouTube, or a bot application).
/// </summary>
public class Integration : DiscordEntity
{
    /// <summary>Integration name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Integration type ("twitch", "youtube", "discord", "guild_subscription").</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Whether this integration is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Whether this integration is syncing (Twitch/YouTube only).</summary>
    [JsonPropertyName("syncing")]
    public bool? Syncing { get; set; }

    /// <summary>Id that this integration uses for "subscribers" (Twitch/YouTube only).</summary>
    [JsonPropertyName("role_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? RoleId { get; set; }

    /// <summary>Whether emoticons should be synced for this integration (Twitch only).</summary>
    [JsonPropertyName("enable_emoticons")]
    public bool? EnableEmoticons { get; set; }

    /// <summary>
    /// The behavior of expiring subscribers.
    /// 0 = Remove role, 1 = Kick.
    /// </summary>
    [JsonPropertyName("expire_behavior")]
    public int? ExpireBehavior { get; set; }

    /// <summary>Grace period (days) before expiring subscribers.</summary>
    [JsonPropertyName("expire_grace_period")]
    public int? ExpireGracePeriod { get; set; }

    /// <summary>User for this integration.</summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }

    /// <summary>Integration account information.</summary>
    [JsonPropertyName("account")]
    public IntegrationAccount? Account { get; set; }

    /// <summary>When this integration was last synced.</summary>
    [JsonPropertyName("synced_at")]
    public DateTimeOffset? SyncedAt { get; set; }

    /// <summary>How many subscribers this integration has.</summary>
    [JsonPropertyName("subscriber_count")]
    public int? SubscriberCount { get; set; }

    /// <summary>Has this integration been revoked.</summary>
    [JsonPropertyName("revoked")]
    public bool? Revoked { get; set; }

    /// <summary>The bot/OAuth2 application for Discord integrations.</summary>
    [JsonPropertyName("application")]
    public IntegrationApplication? Application { get; set; }

    /// <summary>Scopes the application was authorized for.</summary>
    [JsonPropertyName("scopes")]
    public System.Collections.Generic.List<string>? Scopes { get; set; }

    /// <summary>ID of the guild the integration belongs to.</summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
}

/// <summary>Integration account information.</summary>
public class IntegrationAccount
{
    /// <summary>Id of the account.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Name of the account.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>The bot/OAuth2 application attached to a Discord integration.</summary>
public class IntegrationApplication
{
    /// <summary>The id of the app.</summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>The name of the app.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>The icon hash of the app.</summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>The description of the app.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>The bot associated with this application.</summary>
    [JsonPropertyName("bot")]
    public User? Bot { get; set; }
}
