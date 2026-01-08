#nullable enable
using System;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Utility class for validating Snowflake IDs.
/// </summary>
public static class SnowflakeValidator
{
    /// <summary>
    /// Validates that a Snowflake ID is valid.
    /// </summary>
    /// <param name="snowflake">The Snowflake ID to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the Snowflake ID is invalid.</exception>
    public static void ValidateSnowflake(ulong snowflake, string parameterName = "snowflake")
    {
        if (snowflake == 0)
        {
            throw new ValidationException($"Snowflake ID cannot be zero.", parameterName, snowflake);
        }

        // Discord Snowflake IDs are 64-bit integers, so any non-zero value is technically valid
        // However, we can add additional validation for reasonable ranges if needed
        // Discord Snowflake IDs started around 2015, so they should be > 1000000000000000000
        if (snowflake < 1000000000000000000UL)
        {
            throw new ValidationException($"Snowflake ID appears to be invalid (too small).", parameterName, snowflake);
        }
    }

    /// <summary>
    /// Validates that a nullable Snowflake ID is valid if provided.
    /// </summary>
    /// <param name="snowflake">The Snowflake ID to validate (can be null).</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the Snowflake ID is invalid.</exception>
    public static void ValidateSnowflake(ulong? snowflake, string parameterName = "snowflake")
    {
        if (snowflake.HasValue)
        {
            ValidateSnowflake(snowflake.Value, parameterName);
        }
    }
}