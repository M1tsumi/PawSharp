#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when gateway connection issues occur.
/// <para>
/// This exception is thrown when WebSocket connection to Discord's gateway fails or encounters issues.
/// It includes information about the gateway opcode and event type that caused the error, as well as
/// whether the error is recoverable (can be retried automatically) or requires manual intervention.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     await client.ConnectAsync();
/// }
/// catch (GatewayException ex)
/// {
///     Console.WriteLine($"Gateway error: {ex.Message}");
///     Console.WriteLine($"Opcode: {ex.Opcode}");
///     Console.WriteLine($"Event Type: {ex.EventType}");
///     Console.WriteLine($"Is Recoverable: {ex.IsRecoverable}");
///     
///     if (ex.IsRecoverable)
///     {
///         // Attempt reconnection
///         await Task.Delay(TimeSpan.FromSeconds(5));
///         await client.ConnectAsync();
///     }
///     else
/// {
///         // Fatal error - manual intervention required
///         throw;
///     }
/// }
/// </code>
/// </example>
/// </para>
/// </summary>
public class GatewayException : DiscordException
{
    /// <summary>
    /// Gets the gateway opcode that caused the error, if applicable.
    /// <para>Discord gateway opcodes indicate the type of message being sent or received.</para>
    /// </summary>
    public int? Opcode { get; }

    /// <summary>
    /// Gets the event type that caused the error, if applicable.
    /// </summary>
    public string? EventType { get; }

    /// <summary>
    /// Gets whether this error is recoverable.
    /// <para>Recoverable errors can be automatically retried by the library. Non-recoverable errors require manual intervention.</para>
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