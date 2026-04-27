#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a command parameter to capture all remaining arguments as a single string or array.
/// This attribute should only be used on the last parameter of a command method.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RemainingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemainingAttribute"/> class.
    /// </summary>
    public RemainingAttribute() { }
}
