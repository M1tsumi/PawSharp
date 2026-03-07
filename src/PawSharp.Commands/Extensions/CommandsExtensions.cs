#nullable enable
using System.Runtime.CompilerServices;
using PawSharp.Client;
using PawSharp.Commands;

namespace PawSharp.Commands.Extensions;

/// <summary>
/// Extension methods for adding commands to Discord clients.
/// </summary>
public static class CommandsExtensions
{
    // ConditionalWeakTable allows the DiscordClient key to be GC'd when no longer referenced,
    // preventing the singleton-per-client pattern from accidentally extending client lifetime.
    private static readonly ConditionalWeakTable<DiscordClient, CommandsExtension> _instances = new();

    /// <summary>
    /// Enables prefix-based commands for the Discord client and returns the singleton
    /// <see cref="CommandsExtension"/> for this client.  Idempotent — subsequent calls
    /// with the same <paramref name="client"/> return the existing instance.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="prefix">The command prefix (default: <c>!</c>).</param>
    /// <returns>The <see cref="CommandsExtension"/> bound to this client.</returns>
    public static CommandsExtension UseCommands(this DiscordClient client, string prefix = "!")
        => _instances.GetValue(client, c => new CommandsExtension(prefix));
}