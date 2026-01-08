#nullable enable
using System;
using System.Text.RegularExpressions;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Utility class for validating URLs.
/// </summary>
public static class UrlValidator
{
    /// <summary>
    /// Regular expression for validating URLs.
    /// </summary>
    private static readonly Regex UrlRegex = new Regex(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates that a URL is well-formed and uses HTTP/HTTPS.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the URL is invalid.</exception>
    public static void ValidateUrl(string? url, string parameterName = "url")
    {
        if (string.IsNullOrEmpty(url))
        {
            return; // Empty URLs are allowed in some contexts
        }

        if (!UrlRegex.IsMatch(url))
        {
            throw new ValidationException(
                "URL must be a valid HTTP or HTTPS URL.",
                parameterName,
                url);
        }

        // Additional validation for Discord-specific limits
        if (url.Length > 2048)
        {
            throw new ValidationException(
                "URL exceeds maximum length of 2048 characters.",
                parameterName,
                url.Length);
        }
    }

    /// <summary>
    /// Validates that an image URL is valid and points to a supported image format.
    /// </summary>
    /// <param name="imageUrl">The image URL to validate.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <exception cref="ValidationException">Thrown when the image URL is invalid.</exception>
    public static void ValidateImageUrl(string? imageUrl, string parameterName = "imageUrl")
    {
        ValidateUrl(imageUrl, parameterName);

        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }

        // Check for common image extensions
        string lowerUrl = imageUrl.ToLowerInvariant();
        bool isImageUrl = lowerUrl.Contains(".png") ||
                         lowerUrl.Contains(".jpg") ||
                         lowerUrl.Contains(".jpeg") ||
                         lowerUrl.Contains(".gif") ||
                         lowerUrl.Contains(".webp");

        if (!isImageUrl)
        {
            throw new ValidationException(
                "Image URL must point to a supported image format (PNG, JPG, GIF, WebP).",
                parameterName,
                imageUrl);
        }
    }
}