#nullable enable
using System.Text.RegularExpressions;

namespace PawSharp.API.Security;

/// <summary>Provides redaction helpers for safe logging of endpoints and HTTP error payloads.</summary>
public static class LogSanitizer
{
    private static readonly Regex WebhookTokenRegex = new(@"(?<=webhooks/[^/]+/)[^/?]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InteractionTokenRegex = new(@"(?<=interactions/[^/]+/)[^/?]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SensitiveJsonRegex = new("\"(token|access_token|refresh_token|client_secret|authorization)\"\\s*:\\s*\"[^\"]*\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex BearerTokenRegex = new(@"Bearer\s+[A-Za-z0-9._\-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Replaces sensitive token-like path segments with REDACTED for safe log output.</summary>
    public static string RedactSensitiveEndpoint(string endpoint)
    {
        var redacted = WebhookTokenRegex.Replace(endpoint, "REDACTED");
        redacted = InteractionTokenRegex.Replace(redacted, "REDACTED");
        return redacted;
    }

    /// <summary>Sanitizes HTTP response payloads before logging and truncates oversized bodies.</summary>
    public static string SanitizeHttpErrorBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "<empty>";

        var redacted = body;

        redacted = SensitiveJsonRegex.Replace(redacted, m =>
        {
            var key = m.Value[..m.Value.IndexOf(':')];
            return $"{key}:\"REDACTED\"";
        });

        redacted = BearerTokenRegex.Replace(redacted, "Bearer REDACTED");

        const int maxLoggedChars = 512;
        return redacted.Length <= maxLoggedChars
            ? redacted
            : redacted[..maxLoggedChars] + "... [truncated]";
    }
}
