#nullable enable
using System;

namespace PawSharp.Commands.Attributes;

/// <summary>
/// Sets a localized description for a slash command or option.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class SlashLocalizedDescriptionAttribute : Attribute
{
    /// <summary>Gets the locale code (e.g., "en-US", "fr-FR").</summary>
    public string Locale { get; }

    /// <summary>Gets the localized description.</summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashLocalizedDescriptionAttribute"/> class.
    /// </summary>
    /// <param name="locale">The locale code.</param>
    /// <param name="description">The localized description.</param>
    public SlashLocalizedDescriptionAttribute(string locale, string description)
    {
        Locale = locale ?? throw new ArgumentNullException(nameof(locale));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
