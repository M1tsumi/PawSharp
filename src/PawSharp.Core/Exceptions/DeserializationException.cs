#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when JSON deserialization fails.
/// </summary>
public class DeserializationException : DiscordException
{
    /// <summary>
    /// Gets the raw JSON string that failed to deserialize.
    /// </summary>
    public string? RawJson { get; }

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