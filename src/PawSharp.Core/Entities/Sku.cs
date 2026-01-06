#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a SKU.
/// </summary>
public class Sku : DiscordEntity
{
    /// <summary>
    /// Type of SKU.
    /// </summary>
    [JsonPropertyName("type")]
    public SkuType Type { get; set; }
    
    /// <summary>
    /// ID of the parent application.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }
    
    /// <summary>
    /// Customer-facing name of your premium offering.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// System-generated URL slug based on the SKU name.
    /// </summary>
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
    
    /// <summary>
    /// SKU flags.
    /// </summary>
    [JsonPropertyName("flags")]
    public SkuFlags Flags { get; set; }
}

/// <summary>
/// SKU type.
/// </summary>
public enum SkuType
{
    /// <summary>
    /// Represents a recurring subscription.
    /// </summary>
    Subscription = 2,
    
    /// <summary>
    /// System-generated group for each SUBSCRIPTION SKU created.
    /// </summary>
    SubscriptionGroup = 3,
    
    /// <summary>
    /// Represents a one-time purchase.
    /// </summary>
    Durable = 5,
    
    /// <summary>
    /// Represents a consumable one-time purchase.
    /// </summary>
    Consumable = 6
}

/// <summary>
/// SKU flags.
/// </summary>
[Flags]
public enum SkuFlags
{
    /// <summary>
    /// SKU is available for purchase.
    /// </summary>
    Available = 1 << 2,
    
    /// <summary>
    /// Recurring SKU that can be purchased by a user and applied to a single server. Grants access to every user in that server.
    /// </summary>
    GuildSubscription = 1 << 7,
    
    /// <summary>
    /// Recurring SKU purchased by a user for themselves. Grants access to the purchasing user in every server.
    /// </summary>
    UserSubscription = 1 << 8
}