#nullable enable
using System;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents an entitlement.
/// </summary>
public class Entitlement : DiscordEntity
{
    /// <summary>
    /// ID of the SKU.
    /// </summary>
    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }
    
    /// <summary>
    /// ID of the parent application.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }
    
    /// <summary>
    /// ID of the user that is granted access to the entitlement's sku.
    /// </summary>
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? UserId { get; set; }
    
    /// <summary>
    /// Type of entitlement.
    /// </summary>
    [JsonPropertyName("type")]
    public EntitlementType Type { get; set; }
    
    /// <summary>
    /// Entitlement was deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
    
    /// <summary>
    /// Start date at which the entitlement is valid. Not present when using test entitlements.
    /// </summary>
    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }
    
    /// <summary>
    /// Date at which the entitlement is no longer valid. Not present when using test entitlements.
    /// </summary>
    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }
    
    /// <summary>
    /// ID of the guild that is granted access to the entitlement's sku.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// For consumable items, whether or not the entitlement has been consumed.
    /// </summary>
    [JsonPropertyName("consumed")]
    public bool? Consumed { get; set; }
}

/// <summary>
/// Entitlement type.
/// </summary>
public enum EntitlementType
{
    /// <summary>
    /// Entitlement was purchased by user.
    /// </summary>
    Purchase = 1,
    
    /// <summary>
    /// Entitlement for Discord Nitro subscription.
    /// </summary>
    PremiumSubscription = 2,
    
    /// <summary>
    /// Entitlement was gifted by developer.
    /// </summary>
    DeveloperGift = 3,
    
    /// <summary>
    /// Entitlement was purchased by a dev in application test mode.
    /// </summary>
    TestModePurchase = 4,
    
    /// <summary>
    /// Entitlement was granted when the SKU was free.
    /// </summary>
    FreePurchase = 5,
    
    /// <summary>
    /// Entitlement was gifted by another user.
    /// </summary>
    UserGift = 6,
    
    /// <summary>
    /// Entitlement was claimed by user for free as a Nitro subscriber.
    /// </summary>
    PremiumPurchase = 7,
    
    /// <summary>
    /// Entitlement was purchased as an app subscription.
    /// </summary>
    ApplicationSubscription = 8
}