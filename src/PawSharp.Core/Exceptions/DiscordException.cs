#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Base exception for all PawSharp-related errors.
/// </summary>
public class DiscordException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordException"/> class.
    /// </summary>
    public DiscordException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DiscordException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DiscordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}