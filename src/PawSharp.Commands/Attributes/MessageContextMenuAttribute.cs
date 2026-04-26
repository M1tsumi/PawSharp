#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a method as a message context menu command (right-click on a message).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MessageContextMenuAttribute : Attribute
{
    /// <summary>Gets the context menu name (1–32 characters).</summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageContextMenuAttribute"/> class.
    /// </summary>
    /// <param name="name">The context menu name.</param>
    public MessageContextMenuAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
