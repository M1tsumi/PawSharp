#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Marks a command parameter as optional, allowing it to be omitted or null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OptionalAttribute : Attribute
{
    /// <summary>
    /// Gets the default value to use if the argument is not provided.
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalAttribute"/> class.
    /// </summary>
    public OptionalAttribute()
    {
        DefaultValue = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalAttribute"/> class with a default value.
    /// </summary>
    /// <param name="defaultValue">The default value to use.</param>
    public OptionalAttribute(object? defaultValue)
    {
        DefaultValue = defaultValue;
    }
}
