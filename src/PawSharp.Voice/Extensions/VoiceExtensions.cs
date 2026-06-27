#nullable enable
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Voice;

namespace PawSharp.Voice.Extensions;

/// <summary>
/// Extension methods for adding voice support to Discord clients.
/// </summary>
public static class VoiceExtensions
{
    private static readonly ConditionalWeakTable<IDiscordClient, VoiceClient> _instances = new();

    public static VoiceClient UseVoice(this IDiscordClient client, ILogger? logger = null)
        => _instances.GetValue(client, c => new VoiceClient((DiscordClient)c, logger));
}