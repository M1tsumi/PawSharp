#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Enums;
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

    /// <summary>Enhanced role style colors (gradient/holographic).</summary>
    [JsonPropertyName("colors")]
    public RoleColors? Colors { get; set; }

    /// <summary>
    /// Gets whether this role is managed by an integration.
    /// </summary>
    public bool IsManaged => Managed;

    /// <summary>
    /// Gets whether this role is mentionable.
    /// </summary>
    public bool IsMentionable => Mentionable;

    /// <summary>
    /// Gets whether this role is hoisted (displayed separately in member list).
    /// </summary>
    public bool IsHoisted => Hoist;

    /// <summary>
    /// Checks if this role has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the role has the permission.</returns>
    public bool HasPermission(Permissions permission)
    {
        var rolePermissions = (Permissions)ulong.Parse(Permissions);
        return (rolePermissions & permission) == permission;
    }

    /// <summary>
    /// Gets the role's color as a hexadecimal string.
    /// </summary>
    public string GetColorHex()
    {
        return Color.ToString("X6").PadLeft(6, '0');
    }

    /// <summary>
    /// Gets the role's color as RGB components.
    /// </summary>
    /// <returns>A tuple containing R, G, and B components (0-255 each).</returns>
    public (byte R, byte G, byte B) GetColorRgb()
    {
        return ((byte)((Color >> 16) & 0xFF),
                (byte)((Color >> 8) & 0xFF),
                (byte)(Color & 0xFF));
    }

    /// <summary>
    /// Gets whether this is the guild's premium subscriber (booster) role.
    /// </summary>
    public bool IsPremiumSubscriber => Tags?.PremiumSubscriber != null;

    /// <summary>
    /// Gets whether this role is available for purchase.
    /// </summary>
    public bool IsAvailableForPurchase => Tags?.AvailableForPurchase != null;

    /// <summary>
    /// Gets whether this is a guild's linked role.
    /// </summary>
    public bool IsLinkedRole => Tags?.GuildConnections != null;
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

/// <summary>
/// Enhanced role style colors for gradient and holographic effects.
/// </summary>
public class RoleColors
{
    /// <summary>The primary color of the role (hex color code as integer).</summary>
    [JsonPropertyName("primaryColor")]
    public int PrimaryColor { get; set; }

    /// <summary>
    /// The secondary color of the role (hex color code as integer).
    /// When set, this creates a gradient between primary and secondary colors.
    /// </summary>
    [JsonPropertyName("secondaryColor")]
    public int? SecondaryColor { get; set; }

    /// <summary>
    /// The tertiary color of the role (hex color code as integer).
    /// When set, this creates a holographic style with specific enforced values:
    /// primaryColor = 11127295, secondaryColor = 16759788, tertiaryColor = 16761760.
    /// </summary>
    [JsonPropertyName("tertiaryColor")]
    public int? TertiaryColor { get; set; }

    /// <summary>
    /// Gets whether this role has a gradient color style.
    /// </summary>
    public bool IsGradient => SecondaryColor.HasValue;

    /// <summary>
    /// Gets whether this role has a holographic color style.
    /// </summary>
    public bool IsHolographic => TertiaryColor.HasValue;

    /// <summary>
    /// Gets the primary color as a hexadecimal string.
    /// </summary>
    public string GetPrimaryColorHex()
    {
        return PrimaryColor.ToString("X6").PadLeft(6, '0');
    }

    /// <summary>
    /// Gets the secondary color as a hexadecimal string, if set.
    /// </summary>
    public string? GetSecondaryColorHex()
    {
        return SecondaryColor?.ToString("X6").PadLeft(6, '0');
    }

    /// <summary>
    /// Gets the tertiary color as a hexadecimal string, if set.
    /// </summary>
    public string? GetTertiaryColorHex()
    {
        return TertiaryColor?.ToString("X6").PadLeft(6, '0');
    }
}
