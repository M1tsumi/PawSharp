#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets whether the slash command can be used in DMs.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlashDmPermissionAttribute : Attribute
{
    /// <summary>Gets whether the command can be used in DMs.</summary>
    public bool AllowDm { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashDmPermissionAttribute"/> class.
    /// </summary>
    /// <param name="allowDm">Whether to allow DM usage.</param>
    public SlashDmPermissionAttribute(bool allowDm = true)
    {
        AllowDm = allowDm;
    }
}
