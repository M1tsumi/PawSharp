#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a sticker.
/// </summary>
public class Sticker : DiscordEntity
{
    /// <summary>
    /// For standard stickers, id of the pack the sticker is from.
    /// </summary>
    [JsonPropertyName("pack_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? PackId { get; set; }
    
    /// <summary>
    /// The name of the sticker.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the sticker.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// For guild stickers, the Discord name of a unicode emoji representing the sticker's expression. for standard stickers, a comma-separated list of related expressions.
    /// </summary>
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonPropertyName("format_type")]
    public StickerFormatType FormatType { get; set; }
    
    /// <summary>
    /// Whether this guild sticker can be used, may be false due to loss of Server Boosts.
    /// </summary>
    [JsonPropertyName("available")]
    public bool? Available { get; set; }
    
    /// <summary>
    /// The guild that owns this sticker.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// The user that uploaded the guild sticker.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    /// <summary>
    /// The standard sticker's sort order within its pack.
    /// </summary>
    [JsonPropertyName("sort_value")]
    public int? SortValue { get; set; }
}

/// <summary>
/// Represents a sticker pack.
/// </summary>
public class StickerPack
{
    /// <summary>
    /// Id of the sticker pack.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    /// <summary>
    /// The stickers in the pack.
    /// </summary>
    [JsonPropertyName("stickers")]
    public List<Sticker> Stickers { get; set; } = new();
    
    /// <summary>
    /// Name of the sticker pack.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Id of the pack's SKU.
    /// </summary>
    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }
    
    /// <summary>
    /// Id of a sticker in the pack which is shown as the pack's icon.
    /// </summary>
    [JsonPropertyName("cover_sticker_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? CoverStickerId { get; set; }
    
    /// <summary>
    /// Description of the sticker pack.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Id of the sticker pack's banner image.
    /// </summary>
    [JsonPropertyName("banner_asset_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? BannerAssetId { get; set; }
}

/// <summary>
/// Represents a sticker item.
/// </summary>
public class StickerItem
{
    /// <summary>
    /// Id of the sticker.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    /// <summary>
    /// Name of the sticker.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of sticker format.
    /// </summary>
    [JsonPropertyName("format_type")]
    public StickerFormatType FormatType { get; set; }
}

/// <summary>
/// Sticker format type.
/// </summary>
public enum StickerFormatType
{
    /// <summary>
    /// PNG.
    /// </summary>
    Png = 1,
    
    /// <summary>
    /// APNG.
    /// </summary>
    Apng = 2,
    
    /// <summary>
    /// Lottie.
    /// </summary>
    Lottie = 3,
    
    /// <summary>
    /// GIF.
    /// </summary>
    Gif = 4
}