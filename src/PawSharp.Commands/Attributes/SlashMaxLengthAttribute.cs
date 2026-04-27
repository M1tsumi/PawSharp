#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Specifies the maximum length for a slash command string option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashMaxLengthAttribute : Attribute
{
    /// <summary>Gets the maximum length.</summary>
    public int MaxLength { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashMaxLengthAttribute"/> class.
    /// </summary>
    /// <param name="maxLength">The maximum length.</param>
    public SlashMaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }
}
