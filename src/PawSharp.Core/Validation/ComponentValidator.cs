#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Validation;

/// <summary>
/// Validation methods for Discord message components.
/// </summary>
public static class ComponentValidator
{
    /// <summary>
    /// Validates a button component.
    /// </summary>
    /// <param name="button">The button to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateButton(Button button)
    {
        if (button.Label != null && button.Label.Length > DiscordLimits.MaxButtonLabelLength)
        {
            throw new ValidationException(
                nameof(button.Label),
                button.Label,
                $"Button label must not exceed {DiscordLimits.MaxButtonLabelLength} characters."
            );
        }

        if (button.CustomId != null && button.CustomId.Length > DiscordLimits.MaxButtonCustomIdLength)
        {
            throw new ValidationException(
                nameof(button.CustomId),
                button.CustomId,
                $"Button custom ID must not exceed {DiscordLimits.MaxButtonCustomIdLength} characters."
            );
        }

        // Link buttons must have URL, non-link buttons must have custom_id
        if (button.Style == ButtonStyle.Link)
        {
            if (string.IsNullOrEmpty(button.Url))
            {
                throw new ValidationException(
                    nameof(button.Url),
                    null,
                    "Link buttons must have a URL."
                );
            }
        }
        else if (button.Style != ButtonStyle.Premium)
        {
            if (string.IsNullOrEmpty(button.CustomId))
            {
                throw new ValidationException(
                    nameof(button.CustomId),
                    null,
                    "Non-link and non-premium buttons must have a custom ID."
                );
            }
        }
    }

    /// <summary>
    /// Validates a select menu component.
    /// </summary>
    /// <param name="selectMenu">The select menu to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateSelectMenu(SelectMenuBase selectMenu)
    {
        if (selectMenu.CustomId != null && selectMenu.CustomId.Length > DiscordLimits.MaxSelectMenuCustomIdLength)
        {
            throw new ValidationException(
                nameof(selectMenu.CustomId),
                selectMenu.CustomId,
                $"Select menu custom ID must not exceed {DiscordLimits.MaxSelectMenuCustomIdLength} characters."
            );
        }

        if (selectMenu.Placeholder != null && selectMenu.Placeholder.Length > DiscordLimits.MaxSelectMenuPlaceholderLength)
        {
            throw new ValidationException(
                nameof(selectMenu.Placeholder),
                selectMenu.Placeholder,
                $"Select menu placeholder must not exceed {DiscordLimits.MaxSelectMenuPlaceholderLength} characters."
            );
        }

        if (selectMenu.MinValues.HasValue && selectMenu.MinValues.Value > DiscordLimits.MaxSelectMenuMinValues)
        {
            throw new ValidationException(
                $"Select menu minimum values must not exceed {DiscordLimits.MaxSelectMenuMinValues}.",
                nameof(selectMenu.MinValues),
                selectMenu.MinValues.Value
            );
        }

        if (selectMenu.MaxValues.HasValue && selectMenu.MaxValues.Value > DiscordLimits.MaxSelectMenuMaxValues)
        {
            throw new ValidationException(
                $"Select menu maximum values must not exceed {DiscordLimits.MaxSelectMenuMaxValues}.",
                nameof(selectMenu.MaxValues),
                selectMenu.MaxValues.Value
            );
        }

        if (selectMenu is SelectMenu stringSelect)
        {
            ValidateStringSelectMenu(stringSelect);
        }
    }

    private static void ValidateStringSelectMenu(SelectMenu selectMenu)
    {
        if (selectMenu.Options.Count > DiscordLimits.MaxSelectMenuOptions)
        {
            throw new ValidationException(
                $"Select menu must not have more than {DiscordLimits.MaxSelectMenuOptions} options.",
                nameof(selectMenu.Options),
                selectMenu.Options.Count
            );
        }

        foreach (var option in selectMenu.Options)
        {
            ValidateSelectOption(option);
        }
    }

    /// <summary>
    /// Validates a select menu option.
    /// </summary>
    /// <param name="option">The option to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateSelectOption(SelectOption option)
    {
        if (option.Label.Length > DiscordLimits.MaxSelectMenuOptionLabelLength)
        {
            throw new ValidationException(
                nameof(option.Label),
                option.Label,
                $"Select option label must not exceed {DiscordLimits.MaxSelectMenuOptionLabelLength} characters."
            );
        }

        if (option.Value.Length > DiscordLimits.MaxSelectMenuOptionValueLength)
        {
            throw new ValidationException(
                nameof(option.Value),
                option.Value,
                $"Select option value must not exceed {DiscordLimits.MaxSelectMenuOptionValueLength} characters."
            );
        }

        if (option.Description != null && option.Description.Length > DiscordLimits.MaxSelectMenuOptionDescriptionLength)
        {
            throw new ValidationException(
                nameof(option.Description),
                option.Description,
                $"Select option description must not exceed {DiscordLimits.MaxSelectMenuOptionDescriptionLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a text input component.
    /// </summary>
    /// <param name="textInput">The text input to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateTextInput(TextInput textInput)
    {
        if (textInput.CustomId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                nameof(textInput.CustomId),
                textInput.CustomId,
                $"Text input custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters."
            );
        }

        if (textInput.Label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                nameof(textInput.Label),
                textInput.Label,
                $"Text input label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters."
            );
        }

        if (textInput.Placeholder != null && textInput.Placeholder.Length > DiscordLimits.MaxTextInputPlaceholderLength)
        {
            throw new ValidationException(
                nameof(textInput.Placeholder),
                textInput.Placeholder,
                $"Text input placeholder must not exceed {DiscordLimits.MaxTextInputPlaceholderLength} characters."
            );
        }

        if (textInput.Value != null && textInput.Value.Length > DiscordLimits.MaxTextInputValueLength)
        {
            throw new ValidationException(
                nameof(textInput.Value),
                textInput.Value,
                $"Text input value must not exceed {DiscordLimits.MaxTextInputValueLength} characters."
            );
        }

        if (textInput.MinLength.HasValue && textInput.MinLength.Value > DiscordLimits.MaxTextInputMinLength)
        {
            throw new ValidationException(
                $"Text input minimum length must not exceed {DiscordLimits.MaxTextInputMinLength}.",
                nameof(textInput.MinLength),
                textInput.MinLength.Value
            );
        }

        if (textInput.MaxLength.HasValue && textInput.MaxLength.Value > DiscordLimits.MaxTextInputMaxLength)
        {
            throw new ValidationException(
                $"Text input maximum length must not exceed {DiscordLimits.MaxTextInputMaxLength}.",
                nameof(textInput.MaxLength),
                textInput.MaxLength.Value
            );
        }
    }

    /// <summary>
    /// Validates a text display component.
    /// </summary>
    /// <param name="textDisplay">The text display to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateTextDisplay(TextDisplay textDisplay)
    {
        if (textDisplay.Content.Length > DiscordLimits.MaxTextDisplayContentLength)
        {
            throw new ValidationException(
                nameof(textDisplay.Content),
                textDisplay.Content,
                $"Text display content must not exceed {DiscordLimits.MaxTextDisplayContentLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a media gallery component.
    /// </summary>
    /// <param name="mediaGallery">The media gallery to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateMediaGallery(MediaGallery mediaGallery)
    {
        if (mediaGallery.Items.Count < DiscordLimits.MinMediaGalleryItems)
        {
            throw new ValidationException(
                $"Media gallery must have at least {DiscordLimits.MinMediaGalleryItems} item(s).",
                nameof(mediaGallery.Items),
                mediaGallery.Items.Count
            );
        }

        if (mediaGallery.Items.Count > DiscordLimits.MaxMediaGalleryItems)
        {
            throw new ValidationException(
                $"Media gallery must not have more than {DiscordLimits.MaxMediaGalleryItems} items.",
                nameof(mediaGallery.Items),
                mediaGallery.Items.Count
            );
        }
    }

    /// <summary>
    /// Validates an ActionRow component.
    /// </summary>
    /// <param name="actionRow">The ActionRow to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateActionRow(ActionRow actionRow)
    {
        if (actionRow.Components.Count > DiscordLimits.MaxComponentsPerActionRow)
        {
            throw new ValidationException(
                $"ActionRow must not contain more than {DiscordLimits.MaxComponentsPerActionRow} components.",
                nameof(actionRow.Components),
                actionRow.Components.Count
            );
        }

        // Validate each component in the ActionRow
        foreach (var component in actionRow.Components)
        {
            ValidateComponent(component);
        }
    }

    /// <summary>
    /// Validates a message component hierarchy.
    /// </summary>
    /// <param name="components">The root-level components to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateComponentHierarchy(List<MessageComponent> components)
    {
        if (components.Count > DiscordLimits.MaxActionRowsPerMessage)
        {
            throw new ValidationException(
                $"Message must not contain more than {DiscordLimits.MaxActionRowsPerMessage} ActionRows.",
                nameof(components),
                components.Count
            );
        }

        foreach (var component in components)
        {
            ValidateComponent(component);
        }
    }

    /// <summary>
    /// Validates a single message component.
    /// </summary>
    /// <param name="component">The component to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateComponent(MessageComponent component)
    {
        switch (component)
        {
            case Button button:
                ValidateButton(button);
                break;
            case SelectMenuBase selectMenu:
                ValidateSelectMenu(selectMenu);
                break;
            case TextInput textInput:
                ValidateTextInput(textInput);
                break;
            case TextDisplay textDisplay:
                ValidateTextDisplay(textDisplay);
                break;
            case MediaGallery mediaGallery:
                ValidateMediaGallery(mediaGallery);
                break;
            case ActionRow actionRow:
                ValidateActionRow(actionRow);
                break;
            case Container container:
                ValidateComponentHierarchy(container.Components);
                break;
            case Section section:
                ValidateSection(section);
                break;
            case UnknownComponent:
                // Unknown components are not validated
                break;
        }
    }

    private static void ValidateSection(Section section)
    {
        // Validate all TextDisplay children
        foreach (var component in section.Components)
        {
            if (component is TextDisplay textDisplay)
            {
                ValidateTextDisplay(textDisplay);
            }
        }

        // Validate accessory if present
        if (section.Accessory != null)
        {
            ValidateComponent(section.Accessory);
        }
    }
}
