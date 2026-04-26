#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a slash command as NSFW-only.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlashNsfwAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlashNsfwAttribute"/> class.
    /// </summary>
    public SlashNsfwAttribute() { }
}
