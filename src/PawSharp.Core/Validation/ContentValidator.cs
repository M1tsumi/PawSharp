#nullable enable
using System;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Utility class for validating content and message data.
/// </summary>
public static class ContentValidator
{
    /// <summary>
    /// Maximum allowed message content length (2000 characters).
    /// </summary>
    public const int MaxMessageLength = 2000;

    /// <summary>
    /// Maximum allowed embed title length (256 characters).
    /// </summary>
    public const int MaxEmbedTitleLength = 256;

    /// <summary>
    /// Maximum allowed embed description length (4096 characters).
    /// </summary>
    public const int MaxEmbedDescriptionLength = 4096;

    /// <summary>
    /// Maximum allowed embed field name length (256 characters).
    /// </summary>
    public const int MaxEmbedFieldNameLength = 256;

    /// <summary>
    /// Maximum allowed embed field value length (1024 characters).
    /// </summary>
    public const int MaxEmbedFieldValueLength = 1024;

    /// <summary>
    /// Validates message content length.
    /// </summary>
    /// <param name="content">The message content to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the content is too long.</exception>
    public static void ValidateMessageContent(string? content, string parameterName = "content")
    {
        if (string.IsNullOrEmpty(content))
        {
            return; // Empty content is allowed
        }

        if (content.Length > MaxMessageLength)
        {
            throw new ValidationException(
                $"Message content exceeds maximum length of {MaxMessageLength} characters (current: {content.Length}).",
                parameterName,
                content.Length);
        }
    }

    /// <summary>
    /// Validates embed title length.
    /// </summary>
    /// <param name="title">The embed title to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the title is too long.</exception>
    public static void ValidateEmbedTitle(string? title, string parameterName = "title")
    {
        if (string.IsNullOrEmpty(title))
        {
            return; // Empty title is allowed
        }

        if (title.Length > MaxEmbedTitleLength)
        {
            throw new ValidationException(
                $"Embed title exceeds maximum length of {MaxEmbedTitleLength} characters (current: {title.Length}).",
                parameterName,
                title.Length);
        }
    }

    /// <summary>
    /// Validates embed description length.
    /// </summary>
    /// <param name="description">The embed description to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the description is too long.</exception>
    public static void ValidateEmbedDescription(string? description, string parameterName = "description")
    {
        if (string.IsNullOrEmpty(description))
        {
            return; // Empty description is allowed
        }

        if (description.Length > MaxEmbedDescriptionLength)
        {
            throw new ValidationException(
                $"Embed description exceeds maximum length of {MaxEmbedDescriptionLength} characters (current: {description.Length}).",
                parameterName,
                description.Length);
        }
    }

    /// <summary>
    /// Validates embed field name length.
    /// </summary>
    /// <param name="name">The embed field name to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the field name is too long.</exception>
    public static void ValidateEmbedFieldName(string? name, string parameterName = "fieldName")
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ValidationException("Embed field name cannot be empty.", parameterName, name);
        }

        if (name.Length > MaxEmbedFieldNameLength)
        {
            throw new ValidationException(
                $"Embed field name exceeds maximum length of {MaxEmbedFieldNameLength} characters (current: {name.Length}).",
                parameterName,
                name.Length);
        }
    }

    /// <summary>
    /// Validates embed field value length.
    /// </summary>
    /// <param name="value">The embed field value to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the field value is too long.</exception>
    public static void ValidateEmbedFieldValue(string? value, string parameterName = "fieldValue")
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ValidationException("Embed field value cannot be empty.", parameterName, value);
        }

        if (value.Length > MaxEmbedFieldValueLength)
        {
            throw new ValidationException(
                $"Embed field value exceeds maximum length of {MaxEmbedFieldValueLength} characters (current: {value.Length}).",
                parameterName,
                value.Length);
        }
    }
}