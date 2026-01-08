#nullable enable
using System;
using System.Net;

namespace PawSharp.Core.Exceptions;

/// <summary>
/// Exception thrown when the Discord API returns an error response.
/// </summary>
public class DiscordApiException : DiscordException
{
    /// <summary>
    /// Gets the HTTP status code returned by the Discord API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the error code from Discord's API response, if available.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// Gets the error message from Discord's API response, if available.
    /// </summary>
    public string? ApiErrorMessage { get; }

    /// <summary>
    /// Gets the retry-after value in seconds, if provided by Discord.
    /// </summary>
    public int? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordApiException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by Discord.</param>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The Discord API error code, if available.</param>
    /// <param name="apiErrorMessage">The error message from Discord's API response.</param>
    /// <param name="retryAfter">The retry-after value in seconds.</param>
    public DiscordApiException(HttpStatusCode statusCode, string message, int? errorCode = null, string? apiErrorMessage = null, int? retryAfter = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ApiErrorMessage = apiErrorMessage;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordApiException"/> class with an inner exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code returned by Discord.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DiscordApiException(HttpStatusCode statusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}