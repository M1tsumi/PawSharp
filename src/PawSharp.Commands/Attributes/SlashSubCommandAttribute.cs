#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a method as a slash command subcommand within a group.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlashSubCommandAttribute : Attribute
{
    /// <summary>Gets the subcommand name (1–32 characters, lowercase).</summary>
    public string Name { get; }

    /// <summary>Gets the subcommand description (1–100 characters).</summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashSubCommandAttribute"/> class.
    /// </summary>
    /// <param name="name">The subcommand name.</param>
    /// <param name="description">The subcommand description.</param>
    public SlashSubCommandAttribute(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
