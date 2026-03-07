#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord emoji. Works as both a full guild emoji (with all metadata)
/// and as a partial emoji (id/name/animated) used in reactions, buttons, and select options.
/// </summary>
public class Emoji
{
    /// <summary>
    /// Snowflake ID of the emoji. Null for standard Unicode emojis.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? Id { get; set; }

    /// <summary>
    /// Emoji name. May be null in reaction remove events.
    /// For Unicode emojis this is the actual Unicode character(s).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Roles allowed to use this emoji (guild emojis only).</summary>
    [JsonPropertyName("roles")]
    public List<ulong>? Roles { get; set; }

    /// <summary>User that created this emoji (guild emojis only).</summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }

    /// <summary>Whether this emoji must be wrapped in colons.</summary>
    [JsonPropertyName("require_colons")]
    public bool? RequireColons { get; set; }

    /// <summary>Whether this emoji is managed by an integration.</summary>
    [JsonPropertyName("managed")]
    public bool? Managed { get; set; }

    /// <summary>Whether this emoji is animated.</summary>
    [JsonPropertyName("animated")]
    public bool? Animated { get; set; }

    /// <summary>Whether this emoji can be used; may be false due to loss of Server Boosts.</summary>
    [JsonPropertyName("available")]
    public bool? Available { get; set; }

    /// <summary>
    /// Returns the string usable in Discord message content or interaction payloads.
    /// </summary>
    public string ToDiscordString()
    {
        if (!Id.HasValue || Id.Value == 0)
            return Name ?? string.Empty;

        var prefix = Animated == true ? "a" : "";
        return $"<{prefix}:{Name}:{Id.Value}>";
    }
}
