#nullable enable
using System;

namespace PawSharp.Core.Builders;

/// <summary>
/// Preconfigured embed templates for common bot responses.
/// </summary>
public static class EmbedTemplates
{
    private const int SuccessColor = 0x57F287;
    private const int ErrorColor = 0xED4245;
    private const int InfoColor = 0x5865F2;
    private const int WarningColor = 0xFEE75C;

    /// <summary>
    /// Creates a success-styled embed template.
    /// </summary>
    public static EmbedBuilder Success(string title, string description)
        => BuildTemplate(title, description, SuccessColor);

    /// <summary>
    /// Creates an error-styled embed template.
    /// </summary>
    public static EmbedBuilder Error(string title, string description)
        => BuildTemplate(title, description, ErrorColor);

    /// <summary>
    /// Creates an informational embed template.
    /// </summary>
    public static EmbedBuilder Info(string title, string description)
        => BuildTemplate(title, description, InfoColor);

    /// <summary>
    /// Creates a warning-styled embed template.
    /// </summary>
    public static EmbedBuilder Warning(string title, string description)
        => BuildTemplate(title, description, WarningColor);

    private static EmbedBuilder BuildTemplate(string title, string description, int color)
        => new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.UtcNow);
}
