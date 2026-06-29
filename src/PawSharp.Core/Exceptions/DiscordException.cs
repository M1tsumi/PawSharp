#nullable enable
using System;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Base exception for all PawSharp-related errors.
/// <para>
/// This is the root exception type for all custom exceptions thrown by the PawSharp library.
/// Catching this exception allows handling any PawSharp-specific error in a single catch block.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     await client.Rest.CreateMessageAsync(channelId, request);
/// }
/// catch (DiscordException ex)
/// {
///     // Handle any PawSharp-specific error
///     Console.WriteLine($"PawSharp error: {ex.Message}");
/// }
/// </code>
/// </example>
/// </para>
/// <para>
/// <remarks>
/// For more specific error handling, catch the derived exception types:
/// <list type="bullet">
/// <item><description><c>DiscordApiException</c> - REST API errors</description></item>
/// <item><description><see cref="GatewayException"/> - WebSocket connection errors</description></item>
/// <item><description><see cref="ValidationException"/> - Input validation errors</description></item>
/// <item><description><see cref="RateLimitException"/> - Rate limiting errors</description></item>
/// <item><description><see cref="DeserializationException"/> - JSON parsing errors</description></item>
/// </list>
/// </remarks>
/// </para>
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