#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Specifies the maximum value for a slash command numeric option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashMaxValueAttribute : Attribute
{
    /// <summary>Gets the maximum value.</summary>
    public double MaxValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashMaxValueAttribute"/> class.
    /// </summary>
    /// <param name="maxValue">The maximum value.</param>
    public SlashMaxValueAttribute(double maxValue)
    {
        MaxValue = maxValue;
    }
}
