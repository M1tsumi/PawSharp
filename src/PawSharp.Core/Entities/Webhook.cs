#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord webhook.
/// </summary>
public class Webhook : DiscordEntity
{
    /// <summary>
    /// The type of the webhook.
    /// </summary>
    [JsonPropertyName("type")]
    public WebhookType Type { get; set; }
    
    /// <summary>
    /// The guild id this webhook is for, if any.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// The channel id this webhook is for, if any.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }
    
    /// <summary>
    /// The user this webhook was created by (not returned when getting a webhook with its token).
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    /// <summary>
    /// The default name of the webhook.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// The default user avatar hash of the webhook.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// The secure token of the webhook (returned for Incoming Webhooks).
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    
    /// <summary>
    /// The bot/OAuth2 application that created this webhook.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
    
    /// <summary>
    /// The guild of the channel that this webhook is following (returned for Channel Follower Webhooks).
    /// </summary>
    [JsonPropertyName("source_guild")]
    public Guild? SourceGuild { get; set; }
    
    /// <summary>
    /// The channel that this webhook is following (returned for Channel Follower Webhooks).
    /// </summary>
    [JsonPropertyName("source_channel")]
    public Channel? SourceChannel { get; set; }
    
    /// <summary>
    /// The url used for executing the webhook (returned by the webhooks OAuth2 flow).
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Webhook type.
/// </summary>
public enum WebhookType
{
    /// <summary>
    /// Incoming Webhooks can post messages to channels with a generated token.
    /// </summary>
    Incoming = 1,
    
    /// <summary>
    /// Channel Follower Webhooks are internal webhooks used with Channel Following to post new messages into channels.
    /// </summary>
    ChannelFollower = 2,
    
    /// <summary>
    /// Application webhooks are webhooks used with Interactions.
    /// </summary>
    Application = 3
}