#nullable enable
using PawSharp.Client;
using PawSharp.Interactivity;

namespace PawSharp.Interactivity.Extensions;

/// <summary>
/// Extension methods for adding interactivity to Discord clients.
/// </summary>
public static class InteractivityExtensions
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<DiscordClient, InteractivityExtension> _extensions = new();

    /// <summary>
    /// Enables interactivity for the Discord client.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="config">The interactivity configuration.</param>
    /// <returns>The interactivity extension.</returns>
    public static InteractivityExtension UseInteractivity(
        this DiscordClient client,
        InteractivityConfiguration? config = null)
    {
        return _extensions.GetOrAdd(client, c => new InteractivityExtension(config));
    }
}