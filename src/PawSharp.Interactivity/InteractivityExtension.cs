#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PawSharp.Client;
using PawSharp.Core.Entities;

namespace PawSharp.Interactivity;

/// <summary>
/// Configuration for interactivity.
/// </summary>
public class InteractivityConfiguration
{
    /// <summary>
    /// Gets or sets the default timeout for interactivity operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the pagination behavior for reactions.
    /// </summary>
    public PollBehaviour PollBehaviour { get; set; } = PollBehaviour.DeleteEmojis;

    /// <summary>
    /// Gets or sets the pagination emojis.
    /// </summary>
    public PaginationEmojis PaginationEmojis { get; set; } = new();
}

/// <summary>
/// Pagination emojis.
/// </summary>
public class PaginationEmojis
{
    /// <summary>
    /// Gets or sets the left arrow emoji.
    /// </summary>
    public string Left { get; set; } = "◀";

    /// <summary>
    /// Gets or sets the right arrow emoji.
    /// </summary>
    public string Right { get; set; } = "▶";

    /// <summary>
    /// Gets or sets the skip left emoji.
    /// </summary>
    public string SkipLeft { get; set; } = "⏮";

    /// <summary>
    /// Gets or sets the skip right emoji.
    /// </summary>
    public string SkipRight { get; set; } = "⏭";

    /// <summary>
    /// Gets or sets the stop emoji.
    /// </summary>
    public string Stop { get; set; } = "⏹";
}

/// <summary>
/// Poll behavior for reactions.
/// </summary>
public enum PollBehaviour
{
    /// <summary>
    /// Delete all reactions when done.
    /// </summary>
    DeleteEmojis,

    /// <summary>
    /// Keep all reactions when done.
    /// </summary>
    KeepEmojis,

    /// <summary>
    /// Delete only the bot's reactions when done.
    /// </summary>
    DeleteReactions
}

/// <summary>
/// Represents a page in a paginated message.
/// </summary>
public class Page
{
    /// <summary>
    /// Gets or sets the embed for this page.
    /// </summary>
    public Embed? Embed { get; set; }

    /// <summary>
    /// Gets or sets the content for this page.
    /// </summary>
    public string? Content { get; set; }
}

/// <summary>
/// Result of an interactivity operation.
/// </summary>
public class InteractivityResult<T>
{
    /// <summary>
    /// Gets whether the operation timed out.
    /// </summary>
    public bool TimedOut { get; set; }

    /// <summary>
    /// Gets the result value.
    /// </summary>
    public T? Result { get; set; }
}

/// <summary>
/// Main interactivity extension.
/// </summary>
public class InteractivityExtension
{
    private readonly InteractivityConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractivityExtension"/> class.
    /// </summary>
    /// <param name="config">The interactivity configuration.</param>
    public InteractivityExtension(InteractivityConfiguration? config = null)
    {
        _config = config ?? new InteractivityConfiguration();
    }

    /// <summary>
    /// Gets the timeout for operations.
    /// </summary>
    public TimeSpan Timeout => _config.Timeout;

    /// <summary>
    /// Gets the pagination emojis used by <c>SendPaginatedMessageAsync</c>.
    /// </summary>
    public PaginationEmojis PaginationEmojis => _config.PaginationEmojis;

    /// <summary>
    /// Gets the poll behaviour (reaction cleanup) used by <c>SendPaginatedMessageAsync</c>.
    /// </summary>
    public PollBehaviour PollBehaviour => _config.PollBehaviour;

    /// <summary>
    /// Generates pages from content.
    /// </summary>
    /// <param name="content">The content to paginate.</param>
    /// <param name="maxLength">The maximum length per page.</param>
    /// <returns>The generated pages.</returns>
    public IEnumerable<Page> GeneratePagesInContent(string content, int maxLength = 2000)
    {
        if (string.IsNullOrEmpty(content))
            yield break;

        for (int i = 0; i < content.Length; i += maxLength)
        {
            var length = Math.Min(maxLength, content.Length - i);
            yield return new Page { Content = content.Substring(i, length) };
        }
    }

    /// <summary>
    /// Generates pages from content in embeds.
    /// </summary>
    /// <param name="content">The content to paginate.</param>
    /// <param name="maxLength">The maximum length per page.</param>
    /// <returns>The generated pages.</returns>
    public IEnumerable<Page> GeneratePagesInEmbed(string content, int maxLength = 4000)
    {
        if (string.IsNullOrEmpty(content))
            yield break;

        for (int i = 0; i < content.Length; i += maxLength)
        {
            var length = Math.Min(maxLength, content.Length - i);
            var embed = new Embed
            {
                Description = content.Substring(i, length)
            };
            yield return new Page { Embed = embed };
        }
    }
}