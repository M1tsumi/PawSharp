#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets default member permissions required to use a slash command.
/// Serialized to Discord's <c>default_member_permissions</c> bitset string.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SlashDefaultMemberPermissionsAttribute : Attribute
{
    /// <summary>Gets the required permission bitset.</summary>
    public ulong Permissions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashDefaultMemberPermissionsAttribute"/> class.
    /// </summary>
    /// <param name="permissions">Bitwise OR of required Discord permission flags.</param>
    public SlashDefaultMemberPermissionsAttribute(ulong permissions)
    {
        Permissions = permissions;
    }
}
