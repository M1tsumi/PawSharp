#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets installation contexts where a global slash command is available.
/// Discord values: 0 = GUILD_INSTALL, 1 = USER_INSTALL.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SlashIntegrationTypesAttribute : Attribute
{
    /// <summary>Gets the allowed installation contexts.</summary>
    public IReadOnlyList<int> IntegrationTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashIntegrationTypesAttribute"/> class.
    /// </summary>
    /// <param name="integrationTypes">One or more Discord integration type values.</param>
    public SlashIntegrationTypesAttribute(params int[] integrationTypes)
    {
        IntegrationTypes = (integrationTypes ?? Array.Empty<int>())
            .Distinct()
            .ToList()
            .AsReadOnly();
    }
}
