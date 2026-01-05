using System;

namespace PawSharp.Core.Interfaces;

/// <summary>
/// Represents a base Discord entity with a Snowflake ID.
/// </summary>
public interface IDiscordEntity
{
    /// <summary>
    /// The entity's unique Snowflake ID.
    /// </summary>
    ulong Id { get; }
    
    /// <summary>
    /// When the entity was created (derived from Snowflake).
    /// </summary>
    DateTimeOffset CreatedAt { get; }
}