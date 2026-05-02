#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets the interaction contexts where a slash command can be used.
/// Discord values: 0 = GUILD, 1 = BOT_DM, 2 = PRIVATE_CHANNEL.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SlashContextsAttribute : Attribute
{
    /// <summary>Gets the allowed interaction contexts.</summary>
    public IReadOnlyList<int> Contexts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashContextsAttribute"/> class.
    /// </summary>
    /// <param name="contexts">One or more Discord interaction context values.</param>
    public SlashContextsAttribute(params int[] contexts)
    {
        Contexts = (contexts ?? Array.Empty<int>())
            .Distinct()
            .ToList()
            .AsReadOnly();
    }
}
