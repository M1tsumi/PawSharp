#nullable enable
using System;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a soundboard sound.
/// </summary>
public class SoundboardSound : DiscordEntity
{
    /// <summary>
    /// The name of this sound.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The sound's description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// The volume of this sound, from 0 to 1.
    /// </summary>
    [JsonPropertyName("volume")]
    public double Volume { get; set; }
    
    /// <summary>
    /// The id of the emoji that represents this sound.
    /// </summary>
    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }
    
    /// <summary>
    /// The unicode character of the emoji that represents this sound.
    /// </summary>
    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }
    
    /// <summary>
    /// The guild id this soundboard sound is in.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// Whether this sound can be used, may be false due to loss of Server Boosts.
    /// </summary>
    [JsonPropertyName("available")]
    public bool Available { get; set; }
    
    /// <summary>
    /// The user who created this sound.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
}