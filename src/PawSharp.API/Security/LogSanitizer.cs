#nullable enable
using System.Text.RegularExpressions;

namespace PawSharp.API.Security;

/// <summary>Provides redaction helpers for safe logging of endpoints and HTTP error payloads.</summary>
public static class LogSanitizer
{
    /// <summary>Replaces sensitive token-like path segments with REDACTED for safe log output.</summary>
    public static string RedactSensitiveEndpoint(string endpoint)
    {
        var redacted = Regex.Replace(
            endpoint,
            @"(?<=webhooks/[^/]+/)[^/?]+",
            "REDACTED",
            RegexOptions.IgnoreCase);

        redacted = Regex.Replace(
            redacted,
            @"(?<=interactions/[^/]+/)[^/?]+",
            "REDACTED",
            RegexOptions.IgnoreCase);

        return redacted;
    }

    /// <summary>Sanitizes HTTP response payloads before logging and truncates oversized bodies.</summary>
    public static string SanitizeHttpErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "<empty>";

        var redacted = body;

        redacted = Regex.Replace(
            redacted,
            "\"(token|access_token|refresh_token|client_secret|authorization)\"\\s*:\\s*\"[^\"]*\"",
            m =>
            {
                var key = m.Value[..m.Value.IndexOf(':')];
                return $"{key}:\"REDACTED\"";
            },
            RegexOptions.IgnoreCase);

        redacted = Regex.Replace(
            redacted,
            @"Bearer\s+[A-Za-z0-9._\-]+",
            "Bearer REDACTED",
            RegexOptions.IgnoreCase);

        const int maxLoggedChars = 512;
        return redacted.Length <= maxLoggedChars
            ? redacted
            : redacted[..maxLoggedChars] + "... [truncated]";
    }
}
