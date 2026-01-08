#nullable enable
using System;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Utility class for validating embed data.
/// </summary>
public static class EmbedValidator
{
    /// <summary>
    /// Maximum number of fields allowed in an embed (25).
    /// </summary>
    public const int MaxEmbedFields = 25;

    /// <summary>
    /// Maximum total embed length (6000 characters).
    /// </summary>
    public const int MaxEmbedTotalLength = 6000;

    /// <summary>
    /// Validates the number of embed fields.
    /// </summary>
    /// <param name="fieldCount">The number of fields in the embed.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when there are too many fields.</exception>
    public static void ValidateEmbedFieldCount(int fieldCount, string parameterName = "fields")
    {
        if (fieldCount > MaxEmbedFields)
        {
            throw new ValidationException(
                $"Embed cannot have more than {MaxEmbedFields} fields (current: {fieldCount}).",
                parameterName,
                fieldCount);
        }
    }

    /// <summary>
    /// Validates the total length of an embed.
    /// </summary>
    /// <param name="totalLength">The total character count of the embed.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the embed is too long.</exception>
    public static void ValidateEmbedTotalLength(int totalLength, string parameterName = "embed")
    {
        if (totalLength > MaxEmbedTotalLength)
        {
            throw new ValidationException(
                $"Embed total length exceeds maximum of {MaxEmbedTotalLength} characters (current: {totalLength}).",
                parameterName,
                totalLength);
        }
    }

    /// <summary>
    /// Validates that an embed has at least one of the required properties.
    /// </summary>
    /// <param name="title">The embed title.</param>
    /// <param name="description">The embed description.</param>
    /// <param name="fields">The embed fields.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the embed has no content.</exception>
    public static void ValidateEmbedHasContent(string? title, string? description, object? fields, string parameterName = "embed")
    {
        bool hasTitle = !string.IsNullOrEmpty(title);
        bool hasDescription = !string.IsNullOrEmpty(description);
        bool hasFields = fields != null; // This is a simplified check

        if (!hasTitle && !hasDescription && !hasFields)
        {
            throw new ValidationException(
                "Embed must have at least one of: title, description, or fields.",
                parameterName);
        }
    }
}