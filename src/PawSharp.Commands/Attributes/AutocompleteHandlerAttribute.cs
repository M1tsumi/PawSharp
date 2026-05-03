#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a method as an autocomplete handler for a slash command option.
/// The method will be automatically registered to handle autocomplete interactions
/// for the specified command and option.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class AutocompleteHandlerAttribute : Attribute
{
    /// <summary>Gets the name of the command this handler provides autocomplete for.</summary>
    public string CommandName { get; }

    /// <summary>Gets the name of the option this handler provides autocomplete for.</summary>
    public string OptionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AutocompleteHandlerAttribute"/> class.
    /// </summary>
    /// <param name="commandName">The name of the slash command.</param>
    /// <param name="optionName">The name of the option within the command.</param>
    public AutocompleteHandlerAttribute(string commandName, string optionName)
    {
        CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        OptionName = optionName ?? throw new ArgumentNullException(nameof(optionName));
    }
}
