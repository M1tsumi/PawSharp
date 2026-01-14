#nullable enable
using PawSharp.Client;
using PawSharp.Commands;

namespace PawSharp.Commands.Extensions;

/// <summary>
/// Extension methods for adding commands to Discord clients.
/// </summary>
public static class CommandsExtensions
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<DiscordClient, CommandsExtension> _extensions = new();

    /// <summary>
    /// Enables commands for the Discord client.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <returns>The commands extension.</returns>
    public static CommandsExtension UseCommands(this DiscordClient client, string prefix = "!")
    {
        return _extensions.GetOrAdd(client, c => new CommandsExtension(prefix));
    }
}