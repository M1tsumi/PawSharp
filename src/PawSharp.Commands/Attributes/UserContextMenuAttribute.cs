#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a method as a user context menu command (right-click on a user).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UserContextMenuAttribute : Attribute
{
    /// <summary>Gets the context menu name (1–32 characters).</summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserContextMenuAttribute"/> class.
    /// </summary>
    /// <param name="name">The context menu name.</param>
    public UserContextMenuAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
