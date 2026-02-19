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
            throw new ValidationException($"Snowflake ID must be a valid Snowflake (non-zero).", parameterName, snowflake);
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