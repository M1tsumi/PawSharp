#nullable enable
using System;
using System.Text.RegularExpressions;

namespace PawSharp.Commands.Utilities;

/// <summary>
/// Validator for Discord slash command naming conventions.
/// </summary>
public static class SlashCommandValidator
{
    private static readonly Regex ValidNameRegex = new(@"^[a-z0-9_-]{1,32}$", RegexOptions.Compiled);
    private static readonly Regex ValidDescriptionRegex = new(@"^.{1,100}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a slash command name according to Discord's naming rules.
    /// </summary>
    /// <param name="name">The command name to validate.</param>
    /// <returns>A validation result indicating success or failure with an error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Command name cannot be empty.");
        }

        if (name.Length > 32)
        {
            return (false, "Command name cannot exceed 32 characters.");
        }

        if (!ValidNameRegex.IsMatch(name))
        {
            return (false, "Command name must match the regex '^[a-z0-9_-]{1,32}$' (lowercase alphanumeric, underscores, and hyphens only).");
        }

        if (name.StartsWith("-") || name.StartsWith("_"))
        {
            return (false, "Command name cannot start with a hyphen or underscore.");
        }

        if (name.Contains(" ") || name.Contains(".."))
        {
            return (false, "Command name cannot contain spaces or consecutive periods.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a slash command description according to Discord's rules.
    /// </summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>A validation result indicating success or failure with an error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return (false, "Description cannot be empty.");
        }

        if (description.Length > 100)
        {
            return (false, "Description cannot exceed 100 characters.");
        }

        if (!ValidDescriptionRegex.IsMatch(description))
        {
            return (false, "Description must be between 1 and 100 characters.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a slash command option name according to Discord's rules.
    /// </summary>
    /// <param name="name">The option name to validate.</param>
    /// <returns>A validation result indicating success or failure with an error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateOptionName(string name)
    {
        // Option names follow the same rules as command names
        return ValidateName(name);
    }

    /// <summary>
    /// Validates a slash command option description according to Discord's rules.
    /// </summary>
    /// <param name="description">The option description to validate.</param>
    /// <returns>A validation result indicating success or failure with an error message.</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateOptionDescription(string description)
    {
        // Option descriptions follow the same rules as command descriptions
        return ValidateDescription(description);
    }
}
