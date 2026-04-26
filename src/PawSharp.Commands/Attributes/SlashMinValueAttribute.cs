#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Specifies the minimum value for a slash command numeric option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashMinValueAttribute : Attribute
{
    /// <summary>Gets the minimum value.</summary>
    public double MinValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashMinValueAttribute"/> class.
    /// </summary>
    /// <param name="minValue">The minimum value.</param>
    public SlashMinValueAttribute(double minValue)
    {
        MinValue = minValue;
    }
}
