#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when input validation fails.
/// <para>
/// This exception is thrown when user input or parameters fail validation before being sent to Discord's API.
/// It includes detailed information about which parameter failed validation and what value was provided.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     await client.Rest.GetChannelMessagesAsync(channelId, limit: 500); // Max is 100
/// }
/// catch (ValidationException ex)
/// {
///     Console.WriteLine($"Parameter: {ex.ParameterName}");
///     Console.WriteLine($"Invalid Value: {ex.InvalidValue}");
///     Console.WriteLine($"Error: {ex.Message}");
/// }
/// </code>
/// </example>
/// </para>
/// </summary>
public class ValidationException : DiscordException
{
    /// <summary>
    /// Gets the name of the parameter that failed validation.
    /// </summary>
    public string? ParameterName { get; }

    /// <summary>
    /// Gets the invalid value that was provided.
    /// </summary>
    public object? InvalidValue { get; }

    /// <summary>
    /// Gets the invalid value that was provided. Alias for <see cref="InvalidValue"/>.
    /// </summary>
    public object? Value => InvalidValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with parameter information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="parameterName">The name of the parameter that failed validation.</param>
    /// <param name="invalidValue">The invalid value that was provided.</param>
    public ValidationException(string message, string parameterName, object? invalidValue = null)
        : base(message)
    {
        ParameterName = parameterName;
        InvalidValue = invalidValue;
    }
}