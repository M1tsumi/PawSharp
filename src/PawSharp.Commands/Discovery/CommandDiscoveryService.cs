#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PawSharp.Commands.Attributes;

namespace PawSharp.Commands.Discovery;

/// <summary>
/// Service for discovering and querying commands.
/// </summary>
public class CommandDiscoveryService
{
    private readonly CommandsExtension _commandsExtension;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDiscoveryService"/> class.
    /// </summary>
    /// <param name="commandsExtension">The commands extension.</param>
    public CommandDiscoveryService(CommandsExtension commandsExtension)
    {
        _commandsExtension = commandsExtension ?? throw new ArgumentNullException(nameof(commandsExtension));
    }
    
    /// <summary>
    /// Gets all registered commands.
    /// </summary>
    /// <returns>A list of all registered commands.</returns>
    public IReadOnlyList<CommandInfo> GetAllCommands()
        => _commandsExtension.GetRegisteredCommands();
    
    /// <summary>
    /// Gets a command by name or alias.
    /// </summary>
    /// <param name="name">The command name or alias.</param>
    /// <returns>The command info, or null if not found.</returns>
    public CommandInfo? GetCommand(string name)
    {
        var commands = _commandsExtension.GetRegisteredCommands();
        return commands.FirstOrDefault(c => 
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            c.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase)));
    }
    
    /// <summary>
    /// Searches for commands by name or description.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A list of matching commands.</returns>
    public IReadOnlyList<CommandInfo> SearchCommands(string query)
    {
        if (string.IsNullOrEmpty(query))
            return Array.Empty<CommandInfo>();
        
        var commands = _commandsExtension.GetRegisteredCommands();
        return commands.Where(c => 
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            c.Aliases.Any(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly();
    }
    
    /// <summary>
    /// Gets commands that have a specific precondition.
    /// </summary>
    /// <typeparam name="T">The precondition type.</typeparam>
    /// <returns>A list of commands with the precondition.</returns>
    public IReadOnlyList<CommandInfo> GetCommandsWithPrecondition<T>() where T : IPrecondition
    {
        // This would require access to the internal Command objects with preconditions
        // For now, return empty as preconditions are stored in Command, not CommandInfo
        return Array.Empty<CommandInfo>();
    }
    
    /// <summary>
    /// Discovers commands from an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A list of discovered command types.</returns>
    public static IReadOnlyList<Type> DiscoverCommandModules(Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));
        
        return assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseCommandModule).IsAssignableFrom(t))
            .ToList()
            .AsReadOnly();
    }
    
    /// <summary>
    /// Gets command methods from a module type.
    /// </summary>
    /// <param name="moduleType">The module type.</param>
    /// <returns>A list of command methods.</returns>
    public static IReadOnlyList<MethodInfo> GetCommandMethods(Type moduleType)
    {
        if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
        
        return moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<CommandAttribute>() != null)
            .ToList()
            .AsReadOnly();
    }
    
    /// <summary>
    /// Gets slash command methods from a module type.
    /// </summary>
    /// <param name="moduleType">The module type.</param>
    /// <returns>A list of slash command methods.</returns>
    public static IReadOnlyList<MethodInfo> GetSlashCommandMethods(Type moduleType)
    {
        if (moduleType == null) throw new ArgumentNullException(nameof(moduleType));
        
        return moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<SlashCommandAttribute>() != null)
            .ToList()
            .AsReadOnly();
    }
}
