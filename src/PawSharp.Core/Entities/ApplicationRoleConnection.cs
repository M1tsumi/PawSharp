#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a role connection metadata record for an application.
/// These records define the metadata fields that can be used to link users' roles to external service data.
/// </summary>
public class ApplicationRoleConnectionMetadata
{
    /// <summary>
    /// Type of metadata value.
    /// </summary>
    [JsonPropertyName("type")]
    public ApplicationRoleConnectionMetadataType Type { get; set; }

    /// <summary>
    /// Dictionary key for the metadata field (must be [a-z0-9_], max 50 characters).
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Name of this metadata field (max 100 characters).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Translations of the name (ISO 3166-1 alpha-2 locale -> name mapping).
    /// </summary>
    [JsonPropertyName("name_localizations")]
    public Dictionary<string, string>? NameLocalizations { get; set; }

    /// <summary>
    /// Description of this metadata field (max 200 characters).
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Translations of the description.
    /// </summary>
    [JsonPropertyName("description_localizations")]
    public Dictionary<string, string>? DescriptionLocalizations { get; set; }
}

/// <summary>
/// Type of application role connection metadata.
/// </summary>
public enum ApplicationRoleConnectionMetadataType
{
    /// <summary>the metadata value (integer) is less than or equal to the guild's configured value.</summary>
    IntegerLessThanOrEqual = 1,
    /// <summary>the metadata value (integer) is greater than or equal to the guild's configured value.</summary>
    IntegerGreaterThanOrEqual = 2,
    /// <summary>the metadata value (integer) is equal to the guild's configured value.</summary>
    IntegerEqual = 3,
    /// <summary>the metadata value (integer) is not equal to the guild's configured value.</summary>
    IntegerNotEqual = 4,
    /// <summary>the metadata value (ISO8601 string) is less than or equal to the guild's configured value (ISO8601 string).</summary>
    DatetimeLessThanOrEqual = 5,
    /// <summary>the metadata value (ISO8601 string) is greater than or equal to the guild's configured value (ISO8601 string).</summary>
    DatetimeGreaterThanOrEqual = 6,
    /// <summary>the metadata value (integer, 0 or 1) is equal to the guild's configured value (integer, 0 or 1).</summary>
    BooleanEqual = 7,
    /// <summary>the metadata value (integer, 0 or 1) is not equal to the guild's configured value (integer, 0 or 1).</summary>
    BooleanNotEqual = 8
}
