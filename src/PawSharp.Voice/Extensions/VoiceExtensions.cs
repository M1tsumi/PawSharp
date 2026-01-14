#nullable enable
using PawSharp.Client;
using PawSharp.Voice;

namespace PawSharp.Voice.Extensions;

/// <summary>
/// Extension methods for adding voice support to Discord clients.
/// </summary>
public static class VoiceExtensions
{
    /// <summary>
    /// Enables voice support for the Discord client.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <returns>The voice client.</returns>
    public static VoiceClient UseVoice(this DiscordClient client)
    {
        return new VoiceClient(client);
    }
}