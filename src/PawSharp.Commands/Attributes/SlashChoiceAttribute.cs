#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Defines a choice for a slash command option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SlashChoiceAttribute : Attribute
{
    /// <summary>Gets the choice name (1–100 characters).</summary>
    public string Name { get; }

    /// <summary>Gets the choice value.</summary>
    public object Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashChoiceAttribute"/> class.
    /// </summary>
    /// <param name="name">The choice name.</param>
    /// <param name="value">The choice value.</param>
    public SlashChoiceAttribute(string name, object value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
