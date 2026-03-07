#nullable enable
using System.Text.Json.Serialization;
using PawSharp.Core.Enums;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a channel permission overwrite for a role or guild member.
/// </summary>
public class Overwrite
{
    /// <summary>Role or user id this overwrite targets.</summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>Whether this overwrite targets a role (0) or member (1).</summary>
    [JsonPropertyName("type")]
    public OverwriteType Type { get; set; }

    /// <summary>Permission bitfield string for allowed permissions.</summary>
    [JsonPropertyName("allow")]
    public string Allow { get; set; } = "0";

    /// <summary>Permission bitfield string for denied permissions.</summary>
    [JsonPropertyName("deny")]
    public string Deny { get; set; } = "0";
}
