#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when gateway connection issues occur.
/// </summary>
public class GatewayException : DiscordException
{
    /// <summary>
    /// Gets the gateway opcode that caused the error, if applicable.
    /// </summary>
    public int? Opcode { get; }

    /// <summary>
    /// Gets the event type that caused the error, if applicable.
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    /// Gets whether this error is recoverable.
    /// </summary>
    public bool IsRecoverable { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="isRecoverable">Whether this error is recoverable.</param>
    public GatewayException(string message, bool isRecoverable = true)
        : base(message)
    {
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayException"/> class with opcode information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="opcode">The gateway opcode that caused the error.</param>
    /// <param name="eventType">The event type that caused the error.</param>
    /// <param name="isRecoverable">Whether this error is recoverable.</param>
    public GatewayException(string message, int opcode, string? eventType = null, bool isRecoverable = true)
        : base(message)
    {
        Opcode = opcode;
        EventType = eventType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="isRecoverable">Whether this error is recoverable.</param>
    public GatewayException(string message, Exception innerException, bool isRecoverable = true)
        : base(message, innerException)
    {
        IsRecoverable = isRecoverable;
    }
}