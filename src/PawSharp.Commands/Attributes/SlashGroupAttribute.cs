#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a module as a slash command group, organizing related commands.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SlashGroupAttribute : Attribute
{
    /// <summary>Gets the group name (1–32 characters, lowercase).</summary>
    public string Name { get; }

    /// <summary>Gets the group description (1–100 characters).</summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashGroupAttribute"/> class.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="description">The group description.</param>
    public SlashGroupAttribute(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
