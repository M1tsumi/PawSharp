#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Specifies the minimum length for a slash command string option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashMinLengthAttribute : Attribute
{
    /// <summary>Gets the minimum length.</summary>
    public int MinLength { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashMinLengthAttribute"/> class.
    /// </summary>
    /// <param name="minLength">The minimum length.</param>
    public SlashMinLengthAttribute(int minLength)
    {
        MinLength = minLength;
    }
}
