namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for Discord-specific string formatting.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Formats the text as bold in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    /// <example>
    /// <code>
    /// string formatted = "Hello".ToBold(); // Returns "**Hello**"
    /// </code>
    /// </example>
    public static string ToBold(this string text)
    {
        return $"**{text}**";
    }

    /// <summary>
    /// Formats the text as italic in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string ToItalic(this string text)
    {
        return $"*{text}*";
    }

    /// <summary>
    /// Formats the text as underlined in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string ToUnderline(this string text)
    {
        return $"__{text}__";
    }

    /// <summary>
    /// Formats the text as strikethrough in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string ToStrikethrough(this string text)
    {
        return $"~~{text}~~";
    }

    /// <summary>
    /// Formats the text as a spoiler in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string ToSpoiler(this string text)
    {
        return $"||{text}||";
    }

    /// <summary>
    /// Formats the text as a code block in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="language">Optional language for syntax highlighting.</param>
    /// <returns>The formatted text.</returns>
    public static string ToCodeBlock(this string text, string? language = null)
    {
        return string.IsNullOrEmpty(language)
            ? $"```{text}```"
            : $"```{language}\n{text}```";
    }

    /// <summary>
    /// Formats the text as inline code in Discord markdown.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <returns>The formatted text.</returns>
    public static string ToInlineCode(this string text)
    {
        return $"`{text}`";
    }

    /// <summary>
    /// Formats a user ID as a user mention.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The mention string.</returns>
    /// <example>
    /// <code>
    /// ulong userId = 123456789012345678;
    /// string mention = userId.ToUserMention(); // Returns "&lt;@123456789012345678&gt;"
    /// </code>
    /// </example>
    public static string ToUserMention(this ulong userId)
    {
        return $"<@{userId}>";
    }

    /// <summary>
    /// Formats a role ID as a role mention.
    /// </summary>
    /// <param name="roleId">The role ID.</param>
    /// <returns>The mention string.</returns>
    public static string ToRoleMention(this ulong roleId)
    {
        return $"<@&{roleId}>";
    }

    /// <summary>
    /// Formats a channel ID as a channel mention.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The mention string.</returns>
    public static string ToChannelMention(this ulong channelId)
    {
        return $"<#{channelId}>";
    }

    /// <summary>
    /// Truncates text to a maximum length with an ellipsis.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="ellipsis">The ellipsis string to use (default: "...").</param>
    /// <returns>The truncated text.</returns>
    public static string Truncate(this string text, int maxLength, string ellipsis = "...")
    {
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - ellipsis.Length) + ellipsis;
    }
}
