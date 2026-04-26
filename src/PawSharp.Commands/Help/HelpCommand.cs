#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PawSharp.Commands.Preconditions;

namespace PawSharp.Commands.Help;

/// <summary>
/// Built-in help command generator.
/// </summary>
public static class HelpCommand
{
    /// <summary>
    /// Generates a help message for all registered commands.
    /// </summary>
    /// <param name="commands">The registered commands.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <returns>A formatted help message.</returns>
    public static string GenerateHelp(IReadOnlyList<CommandInfo> commands, string prefix = "!")
    {
        var sb = new StringBuilder();
        sb.AppendLine("📚 **Available Commands**\n");
        
        var grouped = commands.GroupBy(c => c.Name.Split(' ')[0]); // Group by base command name
        
        foreach (var group in grouped.OrderBy(g => g.Key))
        {
            var command = group.First();
            sb.AppendLine($"**{prefix}{command.Name}**");
            
            if (!string.IsNullOrEmpty(command.Description))
            {
                sb.AppendLine($"   {command.Description}");
            }
            
            if (command.Aliases.Any())
            {
                sb.AppendLine($"   Aliases: {string.Join(", ", command.Aliases.Select(a => $"{prefix}{a}"))}");
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates a help message for a specific command.
    /// </summary>
    /// <param name="command">The command to generate help for.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <returns>A formatted help message.</returns>
    public static string GenerateCommandHelp(CommandInfo command, string prefix = "!")
    {
        var sb = new StringBuilder();
        sb.AppendLine($"📖 **{prefix}{command.Name}**\n");
        
        if (!string.IsNullOrEmpty(command.Description))
        {
            sb.AppendLine($"*{command.Description}*\n");
        }
        
        if (command.Aliases.Any())
        {
            sb.AppendLine($"**Aliases:** {string.Join(", ", command.Aliases.Select(a => $"{prefix}{a}"))}\n");
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// Module for the built-in help command.
/// </summary>
public class HelpModule : BaseCommandModule
{
    private readonly CommandsExtension _commandsExtension;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpModule"/> class.
    /// </summary>
    /// <param name="commandsExtension">The commands extension.</param>
    public HelpModule(CommandsExtension commandsExtension)
    {
        _commandsExtension = commandsExtension ?? throw new ArgumentNullException(nameof(commandsExtension));
    }
    
    /// <summary>
    /// Shows help for all commands or a specific command.
    /// </summary>
    [Command("help")]
    [Description("Shows help for commands")]
    public async Task HelpAsync(CommandContext ctx, [Optional] string? commandName = null)
    {
        var commands = _commandsExtension.GetRegisteredCommands();
        
        if (string.IsNullOrEmpty(commandName))
        {
            var helpMessage = HelpCommand.GenerateHelp(commands, ctx.Prefix);
            await ctx.RespondAsync(helpMessage);
        }
        else
        {
            var command = commands.FirstOrDefault(c => 
                c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) ||
                c.Aliases.Any(a => a.Equals(commandName, StringComparison.OrdinalIgnoreCase)));
            
            if (command == null)
            {
                await ctx.RespondAsync($"Command '{commandName}' not found.");
            }
            else
            {
                var helpMessage = HelpCommand.GenerateCommandHelp(command, ctx.Prefix);
                await ctx.RespondAsync(helpMessage);
            }
        }
    }
}
