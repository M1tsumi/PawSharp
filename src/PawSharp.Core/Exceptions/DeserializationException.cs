#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when JSON deserialization fails.
/// <para>
/// This exception is thrown when the library fails to deserialize JSON responses from Discord's API.
/// It includes the raw JSON content that failed to parse and the target type that was being deserialized to.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     var guild = await client.Rest.GetGuildAsync(guildId);
/// }
/// catch (DeserializationException ex)
/// {
///     Console.WriteLine($"Target Type: {ex.TargetType}");
///     Console.WriteLine($"Raw JSON: {ex.RawJson}");
///     Console.WriteLine($"Error: {ex.Message}");
///     
///     // This helps diagnose API changes or malformed responses
/// }
/// </code>
/// </example>
/// </para>
/// <para>
/// <remarks>
/// This exception typically indicates a mismatch between the library's data models and Discord's API response format.
/// It may occur when Discord introduces breaking changes to their API.
/// </remarks>
/// </para>
/// </summary>
public class DeserializationException : DiscordException
{
    /// <summary>
    /// Gets the raw JSON string that failed to deserialize.
    /// </summary>
    public string? RawJson { get; }

    /// <summary>
    /// Gets the raw JSON string that failed to deserialize. Alias for <see cref="RawJson"/>.
    /// </summary>
    public string? JsonContent => RawJson;

    /// <summary>
    /// Gets the target type that was being deserialized to.
    /// </summary>
    public Type? TargetType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DeserializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeserializationException"/> class with the raw JSON that failed.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="jsonContent">The raw JSON string that failed to deserialize.</param>
    public DeserializationException(string message, string jsonContent)
        : base(message)
    {
        RawJson = jsonContent;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeserializationException"/> class with deserialization details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="rawJson">The raw JSON string that failed to deserialize.</param>
    /// <param name="targetType">The target type that was being deserialized to.</param>
    /// <param name="innerException">The inner exception that caused the deserialization failure.</param>
    public DeserializationException(string message, string rawJson, Type targetType, Exception innerException)
        : base(message, innerException)
    {
        RawJson = rawJson;
        TargetType = targetType;
    }
}