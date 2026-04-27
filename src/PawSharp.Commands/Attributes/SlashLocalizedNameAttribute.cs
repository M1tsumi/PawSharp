#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets a localized name for a slash command or option.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SlashLocalizedNameAttribute : Attribute
{
    /// <summary>Gets the locale code (e.g., "en-US", "fr-FR").</summary>
    public string Locale { get; }

    /// <summary>Gets the localized name.</summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashLocalizedNameAttribute"/> class.
    /// </summary>
    /// <param name="locale">The locale code.</param>
    /// <param name="name">The localized name.</param>
    public SlashLocalizedNameAttribute(string locale, string name)
    {
        Locale = locale ?? throw new ArgumentNullException(nameof(locale));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
