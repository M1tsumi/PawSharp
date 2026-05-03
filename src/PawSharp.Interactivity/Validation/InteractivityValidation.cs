#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PawSharp.Interactivity.Validation;

/// <summary>
/// Validation helpers for interactivity operations.
/// </summary>
public static class InteractivityValidation
{
    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void RequireNotNullOrEmpty(string value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException(
                $"{paramName} cannot be null or empty. " +
                $"Ensure the value is provided and contains at least one character.",
                paramName);
    }

    /// <summary>
    /// Validates that a collection is not empty.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void RequireNotEmpty<T>(IEnumerable<T> collection, string paramName)
    {
        if (!collection.Any())
            throw new ArgumentException(
                $"{paramName} cannot be empty. " +
                $"The collection must contain at least one element.",
                paramName);
    }

    /// <summary>
    /// Validates that a collection has a minimum and maximum count.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="min">Minimum count.</param>
    /// <param name="max">Maximum count.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void RequireCountBetween<T>(IEnumerable<T> collection, int min, int max, string paramName)
    {
        var count = collection.Count();
        if (count < min || count > max)
            throw new ArgumentException(
                $"{paramName} must have between {min} and {max} items. Provided: {count}. " +
                $"Adjust the collection size to fall within the allowed range.",
                paramName);
    }

    /// <summary>
    /// Validates that a value is positive.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void RequirePositive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException(
                $"{paramName} must be positive. Provided: {value}. " +
                $"Ensure the value is greater than zero.",
                paramName);
    }

    /// <summary>
    /// Validates that a value is positive.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public static void RequirePositive(TimeSpan value, string paramName)
    {
        if (value <= TimeSpan.Zero)
            throw new ArgumentException(
                $"{paramName} must be positive. Provided: {value}. " +
                $"Ensure the duration is greater than zero.",
                paramName);
    }

    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <exception cref="ArgumentNullException">Thrown when validation fails.</exception>
    public static void RequireNotNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(
                paramName,
                $"{paramName} cannot be null. " +
                $"Ensure a valid object instance is provided.");
    }
}
