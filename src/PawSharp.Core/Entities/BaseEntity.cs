using System;
using System.Text.Json.Serialization;
using PawSharp.Core.Interfaces;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Base implementation of IDiscordEntity.
/// </summary>
public abstract class DiscordEntity : IDiscordEntity
{
    /// <inheritdoc />
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    [JsonPropertyName("id")]
    public ulong Id { get; set; }
    
    /// <inheritdoc />
    public DateTimeOffset CreatedAt => DateTimeOffset.FromUnixTimeMilliseconds((long)((Id >> 22) + 1420070400000UL));
}