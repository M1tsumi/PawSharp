#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord role within a guild.
/// </summary>
public class Role : DiscordEntity
{
    /// <summary>Role name (1–100 characters).</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Integer representation of a hexadecimal color code.</summary>
    [JsonPropertyName("color")]
    public int Color { get; set; }

    /// <summary>Whether this role is pinned in the user listing.</summary>
    [JsonPropertyName("hoist")]
    public bool Hoist { get; set; }

    /// <summary>Role icon hash.</summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>Unicode emoji for this role (used instead of a custom icon).</summary>
    [JsonPropertyName("unicode_emoji")]
    public string? UnicodeEmoji { get; set; }

    /// <summary>Position of this role (roles with the same position are sorted by id).</summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>Permission bitfield string.</summary>
    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = "0";

    /// <summary>Whether this role is managed by an integration.</summary>
    [JsonPropertyName("managed")]
    public bool Managed { get; set; }

    /// <summary>Whether this role is mentionable.</summary>
    [JsonPropertyName("mentionable")]
    public bool Mentionable { get; set; }

    /// <summary>The tags this role has.</summary>
    [JsonPropertyName("tags")]
    public RoleTags? Tags { get; set; }

    /// <summary>Role flags bitfield.</summary>
    [JsonPropertyName("flags")]
    public RoleFlags Flags { get; set; }
}

/// <summary>
/// Descriptor tags attached to a role (integration-managed, premium subscriber, etc.).
/// </summary>
public class RoleTags
{
    /// <summary>The id of the bot this role belongs to.</summary>
    [JsonPropertyName("bot_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? BotId { get; set; }

    /// <summary>The id of the integration this role belongs to.</summary>
    [JsonPropertyName("integration_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? IntegrationId { get; set; }

    /// <summary>
    /// The id of this role's subscription SKU and listing.
    /// </summary>
    [JsonPropertyName("subscription_listing_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? SubscriptionListingId { get; set; }

    /// <summary>
    /// Whether this is the guild's Booster role.
    /// Discord sends a JSON null for this field when the tag is present;
    /// the presence of the property (even as null) indicates the tag is set.
    /// </summary>
    [JsonPropertyName("premium_subscriber")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object? PremiumSubscriber { get; set; }

    /// <summary>Whether this role is available for purchase.</summary>
    [JsonPropertyName("available_for_purchase")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object? AvailableForPurchase { get; set; }

    /// <summary>Whether this role is a guild's linked role.</summary>
    [JsonPropertyName("guild_connections")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public object? GuildConnections { get; set; }
}

/// <summary>Bitfield flags for a Discord role.</summary>
[System.Flags]
public enum RoleFlags
{
    None     = 0,
    /// <summary>Role can be selected by members in an onboarding prompt.</summary>
    InPrompt = 1 << 0,
}
