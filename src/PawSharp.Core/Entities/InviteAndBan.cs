#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a guild ban.
/// </summary>
public class Ban
{
    /// <summary>
    /// The reason for the ban.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// The banned user.
    /// </summary>
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
}