#nullable enable
using System;

namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for Discord snowflake IDs.
/// </summary>
public static class SnowflakeExtensions
{
    /// <summary>
    /// Gets the creation timestamp of a snowflake ID.
    /// Discord snowflaves contain the timestamp in the high bits.
    /// </summary>
    /// <param name="snowflake">The snowflake ID.</param>
    /// <returns>The DateTimeOffset when the snowflake was created.</returns>
    /// <example>
    /// <code>
    /// ulong userId = 123456789012345678;
    /// DateTimeOffset createdAt = userId.GetCreatedAt();
    /// Console.WriteLine($"User created at: {createdAt}");
    /// </code>
    /// </example>
    public static DateTimeOffset GetCreatedAt(this ulong snowflake)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds((long)((snowflake >> 22) + 1420070400000UL));
    }

    /// <summary>
    /// Converts a snowflake ID to its string representation.
    /// Discord sends IDs as strings to prevent precision loss in JavaScript.
    /// </summary>
    /// <param name="snowflake">The snowflake ID.</param>
    /// <returns>The string representation of the snowflake.</returns>
    public static string ToSnowflakeString(this ulong snowflake)
    {
        return snowflake.ToString();
    }

    /// <summary>
    /// Validates whether a value is a valid Discord snowflake ID.
    /// </summary>
    /// <param name="snowflake">The snowflake ID to validate.</param>
    /// <returns>True if the snowflake is valid.</returns>
    public static bool IsValidSnowflake(this ulong snowflake)
    {
        return snowflake > 0;
    }
}
