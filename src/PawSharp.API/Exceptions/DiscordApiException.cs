#nullable enable
using System;
using System.Net;

namespace PawSharp.API.Exceptions;

/// <summary>
/// Represents an error that occurred while interacting with the Discord API.
/// </summary>
public sealed class DiscordApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code returned by Discord, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the Discord error code, if available.
    /// </summary>
    public int? DiscordErrorCode { get; }

    /// <summary>
    /// Gets the Discord error message, if available.
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
