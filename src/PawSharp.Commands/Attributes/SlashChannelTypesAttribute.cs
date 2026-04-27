#nullable enable
using System;
using System.Collections.Generic;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Specifies the allowed channel types for a slash command channel option.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashChannelTypesAttribute : Attribute
{
    /// <summary>Gets the allowed channel types.</summary>
    public IReadOnlyList<int> ChannelTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashChannelTypesAttribute"/> class.
    /// </summary>
    /// <param name="channelTypes">The allowed channel types.</param>
    public SlashChannelTypesAttribute(params int[] channelTypes)
    {
        ChannelTypes = channelTypes ?? throw new ArgumentNullException(nameof(channelTypes));
    }
}
