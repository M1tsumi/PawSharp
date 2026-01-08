#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when input validation fails.
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