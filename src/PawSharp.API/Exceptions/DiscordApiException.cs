#nullable enable
using System;
using System.Net;
using PawSharp.Core.Exceptions;

namespace PawSharp.API.Exceptions;

/// <summary>
/// Represents an error that occurred while interacting with the Discord API.
/// <para>
/// This exception is thrown when Discord's REST API returns an error response. It includes detailed context
/// about the HTTP status code, Discord-specific error code, error message, and the request details that caused the error.
/// </para>
/// <para>
/// <example>
/// <code>
/// try
/// {
///     await client.Rest.CreateMessageAsync(channelId, request);
/// }
/// catch (DiscordApiException ex)
/// {
///     Console.WriteLine($"Status Code: {ex.StatusCode}");
///     Console.WriteLine($"Discord Error Code: {ex.DiscordErrorCode}");
///     Console.WriteLine($"Discord Error Message: {ex.DiscordErrorMessage}");
///     Console.WriteLine($"Request: {ex.RequestMethod} {ex.RequestEndpoint}");
/// }
/// </code>
/// </example>
/// </para>
/// <para>
/// <remarks>
/// Common Discord error codes:
/// <list type="table">
/// <listheader><term>Code</term><description>Description</description></listheader>
/// <item><term>50001</term><description>Missing Access</description></item>
/// <item><term>50013</term><description>Missing Permissions</description></item>
/// <item><term>10003</term><description>Unknown Channel</description></item>
/// <item><term>10004</term><description>Unknown Guild</description></item>
/// <item><term>10007</term><description>Unknown Member</description></item>
/// <item><term>10008</term><description>Unknown Message</description></item>
/// <item><term>10011</term><description>Unknown Role</description></item>
/// <item><term>20031</term><description>Rate Limited</description></item>
/// </list>
/// </remarks>
/// </para>
/// </summary>
public sealed class DiscordApiException : DiscordException
{
    /// <summary>
    /// Gets the HTTP status code returned by Discord, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the Discord error code, if available.
    /// <para>This is Discord's internal error code that provides more specific information about what went wrong.</para>
    /// </summary>
    public int? DiscordErrorCode { get; }

    /// <summary>
    /// Gets the Discord error message, if available.
    /// <para>This is the human-readable error message from Discord explaining the issue.</para>
    /// </summary>
    public string? DiscordErrorMessage { get; }

    /// <summary>
    /// Gets the request method that caused the error.
    /// </summary>
    public string RequestMethod { get; }

    /// <summary>
    /// Gets the request endpoint that caused the error.
    /// </summary>
    public string RequestEndpoint { get; }

    public DiscordApiException(
        string message,
        HttpStatusCode? statusCode = null,
        int? discordErrorCode = null,
        string? discordErrorMessage = null,
        string? requestMethod = null,
        string? requestEndpoint = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        DiscordErrorCode = discordErrorCode;
        DiscordErrorMessage = discordErrorMessage;
        RequestMethod = requestMethod ?? "UNKNOWN";
        RequestEndpoint = requestEndpoint ?? "UNKNOWN";
    }

    /// <summary>
    /// Creates a DiscordApiException from an HTTP response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="requestMethod">The HTTP request method (GET, POST, etc.).</param>
    /// <param name="requestEndpoint">The API endpoint that was requested.</param>
    /// <param name="discordErrorCode">The Discord error code from the response body, if available.</param>
    /// <param name="discordErrorMessage">The Discord error message from the response body, if available.</param>
    /// <returns>A new DiscordApiException instance.</returns>
    public static DiscordApiException FromResponse(
        HttpStatusCode statusCode,
        string requestMethod,
        string requestEndpoint,
        string? discordErrorCode = null,
        string? discordErrorMessage = null)
    {
        var message = $"Discord API request failed: {requestMethod} {requestEndpoint} returned {statusCode}";
        if (discordErrorMessage != null)
        {
            message += $" - {discordErrorMessage}";
        }

        return new DiscordApiException(
            message,
            statusCode,
            discordErrorCode != null ? int.Parse(discordErrorCode) : null,
            discordErrorMessage,
            requestMethod,
            requestEndpoint);
    }

    public override string ToString()
    {
        var baseStr = base.ToString();
        if (StatusCode.HasValue)
        {
            baseStr += $"\nStatus Code: {StatusCode.Value}";
        }
        if (DiscordErrorCode.HasValue)
        {
            baseStr += $"\nDiscord Error Code: {DiscordErrorCode.Value}";
        }
        if (DiscordErrorMessage != null)
        {
            baseStr += $"\nDiscord Error Message: {DiscordErrorMessage}";
        }
        baseStr += $"\nRequest: {RequestMethod} {RequestEndpoint}";
        return baseStr;
    }
}
