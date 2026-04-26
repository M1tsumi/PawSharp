#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Validation methods for Discord application commands.
/// </summary>
public static class CommandValidator
{
    /// <summary>
    /// Validates an application command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCommand(ApplicationCommand command)
    {
        if (command.Name.Length < DiscordLimits.MinCommandNameLength || command.Name.Length > DiscordLimits.MaxCommandNameLength)
        {
            throw new ValidationException(
                $"Command name must be between {DiscordLimits.MinCommandNameLength} and {DiscordLimits.MaxCommandNameLength} characters.",
                nameof(command.Name),
                command.Name
            );
        }

        // Description is required for ChatInput commands, empty for User/Message commands
        if (command.Type == ApplicationCommandType.ChatInput)
        {
            if (string.IsNullOrEmpty(command.Description) || command.Description.Length > DiscordLimits.MaxCommandDescriptionLength)
            {
                throw new ValidationException(
                    $"ChatInput command description must be between 1 and {DiscordLimits.MaxCommandDescriptionLength} characters.",
                    nameof(command.Description),
                    command.Description
                );
            }
        }

        if (command.Options != null)
        {
            if (command.Options.Count > DiscordLimits.MaxCommandOptions)
            {
                throw new ValidationException(
                    $"Command must not have more than {DiscordLimits.MaxCommandOptions} options.",
                    nameof(command.Options),
                    command.Options.Count
                );
            }

            foreach (var option in command.Options)
            {
                ValidateCommandOption(option);
            }
        }
    }

    /// <summary>
    /// Validates an application command option.
    /// </summary>
    /// <param name="option">The option to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCommandOption(ApplicationCommandOption option)
    {
        if (option.Name.Length < DiscordLimits.MinCommandNameLength || option.Name.Length > DiscordLimits.MaxCommandOptionNameLength)
        {
            throw new ValidationException(
                $"Option name must be between {DiscordLimits.MinCommandNameLength} and {DiscordLimits.MaxCommandOptionNameLength} characters.",
                nameof(option.Name),
                option.Name
            );
        }

        if (option.Description.Length > DiscordLimits.MaxCommandOptionDescriptionLength)
        {
            throw new ValidationException(
                $"Option description must not exceed {DiscordLimits.MaxCommandOptionDescriptionLength} characters.",
                nameof(option.Description),
                option.Description
            );
        }

        if (option.Choices != null)
        {
            if (option.Choices.Count > DiscordLimits.MaxCommandOptionChoices)
            {
                throw new ValidationException(
                    $"Option must not have more than {DiscordLimits.MaxCommandOptionChoices} choices.",
                    nameof(option.Choices),
                    option.Choices.Count
                );
            }

            foreach (var choice in option.Choices)
            {
                ValidateCommandOptionChoice(choice);
            }
        }

        if (option.MinLength.HasValue && option.MinLength.Value > DiscordLimits.MaxCommandOptionMinStringLength)
        {
            throw new ValidationException(
                $"Option minimum length must not exceed {DiscordLimits.MaxCommandOptionMinStringLength}.",
                nameof(option.MinLength),
                option.MinLength.Value
            );
        }

        if (option.MaxLength.HasValue && option.MaxLength.Value > DiscordLimits.MaxCommandOptionMaxStringLength)
        {
            throw new ValidationException(
                $"Option maximum length must not exceed {DiscordLimits.MaxCommandOptionMaxStringLength}.",
                nameof(option.MaxLength),
                option.MaxLength.Value
            );
        }

        if (option.Options != null)
        {
            foreach (var nestedOption in option.Options)
            {
                ValidateCommandOption(nestedOption);
            }
        }
    }

    /// <summary>
    /// Validates an application command option choice.
    /// </summary>
    /// <param name="choice">The choice to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCommandOptionChoice(ApplicationCommandOptionChoice choice)
    {
        if (choice.Name.Length > DiscordLimits.MaxCommandOptionChoiceNameLength)
        {
            throw new ValidationException(
                $"Choice name must not exceed {DiscordLimits.MaxCommandOptionChoiceNameLength} characters.",
                nameof(choice.Name),
                choice.Name
            );
        }

        if (choice.Value is string stringValue && stringValue.Length > DiscordLimits.MaxCommandOptionChoiceValueLength)
        {
            throw new ValidationException(
                $"Choice value must not exceed {DiscordLimits.MaxCommandOptionChoiceValueLength} characters.",
                nameof(choice.Value),
                choice.Value
            );
        }
    }
}
