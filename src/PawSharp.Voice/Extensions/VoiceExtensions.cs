#nullable enable
using System.Runtime.CompilerServices;
using PawSharp.Client;
using PawSharp.Voice;

namespace PawSharp.Voice.Extensions;

/// <summary>
/// Extension methods for adding voice support to Discord clients.
/// </summary>
public static class VoiceExtensions
{
    // One VoiceClient per DiscordClient instance — prevents duplicate gateway event subscriptions.
    private static readonly ConditionalWeakTable<DiscordClient, VoiceClient> _instances = new();

    /// <summary>
    /// Enables voice support for the Discord client.
    /// Returns the same <see cref="VoiceClient"/> on repeated calls for the same client instance.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <returns>The voice client.</returns>
    public static VoiceClient UseVoice(this DiscordClient client)
        => _instances.GetValue(client, c => new VoiceClient(c));
}