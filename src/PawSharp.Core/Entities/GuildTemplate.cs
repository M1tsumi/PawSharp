#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a guild template.
/// </summary>
public class GuildTemplate
{
    /// <summary>
    /// The template code (unique ID).
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Template name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The description for the template.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Number of times this template has been used.
    /// </summary>
    [JsonPropertyName("usage_count")]
    public int UsageCount { get; set; }
    
    /// <summary>
    /// The ID of the user who created the template.
    /// </summary>
    [JsonPropertyName("creator_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong CreatorId { get; set; }
    
    /// <summary>
    /// The user who created the template.
    /// </summary>
    [JsonPropertyName("creator")]
    public User Creator { get; set; } = null!;
    
    /// <summary>
    /// When this template was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When this template was last synced to the source guild.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
    
    /// <summary>
    /// The ID of the guild this template is based on.
    /// </summary>
    [JsonPropertyName("source_guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SourceGuildId { get; set; }
    
    /// <summary>
    /// The guild snapshot this template contains.
    /// </summary>
    [JsonPropertyName("serialized_source_guild")]
    public GuildSnapshot SerializedSourceGuild { get; set; } = null!;
    
    /// <summary>
    /// Whether the template has unsynced changes.
    /// </summary>
    [JsonPropertyName("is_dirty")]
    public bool? IsDirty { get; set; }
}

/// <summary>
/// Represents a guild snapshot.
/// </summary>
public class GuildSnapshot
{
    /// <summary>
    /// Guild name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Guild description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Guild region.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    
    /// <summary>
    /// Guild verification level.
    /// </summary>
    [JsonPropertyName("verification_level")]
    public VerificationLevel VerificationLevel { get; set; }
    
    /// <summary>
    /// Guild default message notifications.
    /// </summary>
    [JsonPropertyName("default_message_notifications")]
    public DefaultMessageNotificationLevel DefaultMessageNotifications { get; set; }
    
    /// <summary>
    /// Guild explicit content filter.
    /// </summary>
    [JsonPropertyName("explicit_content_filter")]
    public ExplicitContentFilterLevel ExplicitContentFilter { get; set; }
    
    /// <summary>
    /// Guild preferred locale.
    /// </summary>
    [JsonPropertyName("preferred_locale")]
    public string PreferredLocale { get; set; } = string.Empty;
    
    /// <summary>
    /// Guild afk timeout.
    /// </summary>
    [JsonPropertyName("afk_timeout")]
    public int AfkTimeout { get; set; }
    
    /// <summary>
    /// Roles in the guild.
    /// </summary>
    [JsonPropertyName("roles")]
    public List<Role> Roles { get; set; } = new();
    
    /// <summary>
    /// Channels in the guild.
    /// </summary>
    [JsonPropertyName("channels")]
    public List<Channel> Channels { get; set; } = new();
    
    /// <summary>
    /// Guild afk channel id.
    /// </summary>
    [JsonPropertyName("afk_channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? AfkChannelId { get; set; }
    
    /// <summary>
    /// Guild system channel id.
    /// </summary>
    [JsonPropertyName("system_channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? SystemChannelId { get; set; }
    
    /// <summary>
    /// Guild system channel flags.
    /// </summary>
    [JsonPropertyName("system_channel_flags")]
    public SystemChannelFlags SystemChannelFlags { get; set; }
    
    /// <summary>
    /// Guild rules channel id.
    /// </summary>
    [JsonPropertyName("rules_channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? RulesChannelId { get; set; }
    
    /// <summary>
    /// Guild public updates channel id.
    /// </summary>
    [JsonPropertyName("public_updates_channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? PublicUpdatesChannelId { get; set; }
}