#nullable enable
using System.Runtime.CompilerServices;
using PawSharp.Client;
using PawSharp.Interactivity;

namespace PawSharp.Interactivity.Extensions;

/// <summary>
/// Extension methods for adding interactivity to Discord clients.
/// </summary>
public static class InteractivityExtensions
{
    // ConditionalWeakTable allows the DiscordClient key to be GC'd when no longer referenced,
    // preventing the singleton-per-client pattern from accidentally extending client lifetime.
    private static readonly ConditionalWeakTable<DiscordClient, InteractivityExtension> _extensions = new();

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
        return _extensions.GetValue(client, c => new InteractivityExtension(config));
    }

    /// <summary>
    /// Returns the <see cref="InteractivityExtension"/> registered for <paramref name="client"/>,
    /// or <c>null</c> if <see cref="UseInteractivity"/> has not been called for this client.
    /// </summary>
    internal static InteractivityExtension? GetExtension(DiscordClient client)
        => _extensions.TryGetValue(client, out var ext) ? ext : null;
}