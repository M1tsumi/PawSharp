#nullable enable
using System.Reflection;
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
    private static readonly ConditionalWeakTable<IDiscordClient, CommandsExtension> _instances = new();

    /// <summary>
    /// Enables prefix-based commands for the Discord client and returns the singleton
    /// <see cref="CommandsExtension"/> for this client.  Idempotent — subsequent calls
    /// with the same <paramref name="client"/> return the existing instance.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="prefix">The command prefix (default: <c>!</c>).</param>
    /// <returns>The <see cref="CommandsExtension"/> bound to this client.</returns>
    public static CommandsExtension UseCommands(this IDiscordClient client, string prefix = "!")
        => _instances.GetValue(client, c => new CommandsExtension(prefix));

    /// <summary>
    /// Registers all command modules found in the calling assembly with auto-discovery.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="prefix">The command prefix (default: <c>!</c>).</param>
    /// <returns>The <see cref="CommandsExtension"/> bound to this client.</returns>
    public static CommandsExtension UseCommandsWithAutoDiscovery(this IDiscordClient client, string prefix = "!")
    {
        var extension = UseCommands(client, prefix);
        extension.RegisterModulesInAssembly(client);
        return extension;
    }

    /// <summary>
    /// Registers all command modules found in the specified assembly with auto-discovery.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="prefix">The command prefix (default: <c>!</c>).</param>
    /// <returns>The <see cref="CommandsExtension"/> bound to this client.</returns>
    public static CommandsExtension UseCommandsWithAutoDiscovery(this IDiscordClient client, Assembly assembly, string prefix = "!")
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var extension = UseCommands(client, prefix);
        extension.RegisterModulesInAssembly(client, assembly);
        return extension;
    }
}