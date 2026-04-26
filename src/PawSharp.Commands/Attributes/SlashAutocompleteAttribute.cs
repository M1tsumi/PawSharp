#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a slash command option as having autocomplete support.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashAutocompleteAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlashAutocompleteAttribute"/> class.
    /// </summary>
    public SlashAutocompleteAttribute() { }
}
