#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets the default member permissions for a slash command.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlashDefaultPermissionAttribute : Attribute
{
    /// <summary>Gets the default permission value.</summary>
    public bool Permission { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashDefaultPermissionAttribute"/> class.
    /// </summary>
    /// <param name="permission">The default permission (true = enabled by default, false = disabled by default).</param>
    public SlashDefaultPermissionAttribute(bool permission = true)
    {
        Permission = permission;
    }
}
