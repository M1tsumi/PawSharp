#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a subscription.
/// </summary>
public class Subscription : DiscordEntity
{
    /// <summary>
    /// ID of the user who is subscribed.
    /// </summary>
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }
    
    /// <summary>
    /// List of SKUs subscribed to.
    /// </summary>
    [JsonPropertyName("sku_ids")]
    public List<ulong> SkuIds { get; set; } = new();
    
    /// <summary>
    /// List of entitlements granted for this subscription.
    /// </summary>
    [JsonPropertyName("entitlement_ids")]
    public List<ulong> EntitlementIds { get; set; } = new();
    
    /// <summary>
    /// List of SKUs that this user will be subscribed to at renewal.
    /// </summary>
    [JsonPropertyName("renewal_sku_ids")]
    public List<ulong>? RenewalSkuIds { get; set; }
    
    /// <summary>
    /// Start of the current subscription period.
    /// </summary>
    [JsonPropertyName("current_period_start")]
    public DateTimeOffset CurrentPeriodStart { get; set; }
    
    /// <summary>
    /// End of the current subscription period.
    /// </summary>
    [JsonPropertyName("current_period_end")]
    public DateTimeOffset CurrentPeriodEnd { get; set; }
    
    /// <summary>
    /// Current status of the subscription.
    /// </summary>
    [JsonPropertyName("status")]
    public SubscriptionStatus Status { get; set; }
    
    /// <summary>
    /// When the subscription was canceled.
    /// </summary>
    [JsonPropertyName("canceled_at")]
    public DateTimeOffset? CanceledAt { get; set; }
}

/// <summary>
/// Subscription status.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and scheduled to renew.
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Subscription is active but will not renew.
    /// </summary>
    Ending = 1,
    
    /// <summary>
    /// Subscription is inactive and not being charged.
    /// </summary>
    Inactive = 2
}