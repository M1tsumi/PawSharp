#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PawSharp.Commands.Attributes;
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
    /// <param name="page">The page number (1-indexed).</param>
    /// <param name="pageSize">The number of commands per page.</param>
    /// <returns>A formatted help message with pagination info.</returns>
    public static string GenerateHelp(IReadOnlyList<CommandInfo> commands, string prefix = "!", int page = 1, int pageSize = 10)
    {
        var sb = new StringBuilder();
        
        var grouped = commands.GroupBy(c => c.Name.Split(' ')[0]).OrderBy(g => g.Key).ToList();
        var totalPages = (int)Math.Ceiling((double)grouped.Count / pageSize);
        
        if (page < 1 || page > totalPages)
        {
            page = 1;
        }
        
        var pageCommands = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        
        sb.AppendLine($"📚 **Available Commands** (Page {page}/{totalPages})\n");
        
        foreach (var group in pageCommands)
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
        
        if (totalPages > 1)
        {
            sb.AppendLine($"*Use `{prefix}help {page + 1}` for the next page or `{prefix}help {page - 1}` for the previous page.*");
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

        if (command.Parameters != null && command.Parameters.Any())
        {
            sb.AppendLine("**Parameters:**");
            foreach (var param in command.Parameters)
            {
                var required = param.IsRequired ? "(required)" : "(optional)";
                sb.AppendLine($"  • `{param.Name}` {required}: {param.Description ?? "No description"}");
            }
            sb.AppendLine();
        }

        if (command.Preconditions != null && command.Preconditions.Any())
        {
            sb.AppendLine("**Requirements:**");
            foreach (var precondition in command.Preconditions)
            {
                sb.AppendLine($"  • {precondition}");
            }
            sb.AppendLine();
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
    public async Task HelpAsync(CommandContext ctx, [Optional] string? commandName = null, [Optional] int? page = null)
    {
        var commands = _commandsExtension.GetRegisteredCommands();
        var stringComparison = _commandsExtension.CaseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;
        
        if (string.IsNullOrEmpty(commandName))
        {
            var helpMessage = HelpCommand.GenerateHelp(commands, ctx.Prefix, page ?? 1);
            await ctx.RespondAsync(helpMessage);
        }
        else
        {
            var command = commands.FirstOrDefault(c => 
                c.Name.Equals(commandName, stringComparison) ||
                c.Aliases.Any(a => a.Equals(commandName, stringComparison)));
            
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
