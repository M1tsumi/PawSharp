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
                ValidateContainer(container);
                break;
            case Section section:
                ValidateSection(section);
                break;
            case Separator separator:
                // Separator has no specific validation beyond structure
                break;
            case Label label:
                ValidateLabel(label);
                break;
            case FileUpload fileUpload:
                ValidateFileUpload(fileUpload);
                break;
            case RadioGroup radioGroup:
                ValidateRadioGroup(radioGroup);
                break;
            case CheckboxGroup checkboxGroup:
                ValidateCheckboxGroup(checkboxGroup);
                break;
            case Checkbox checkbox:
                ValidateCheckbox(checkbox);
                break;
            case ThumbnailComponent thumbnail:
                ValidateThumbnail(thumbnail);
                break;
            case FileComponent file:
                ValidateFile(file);
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

    /// <summary>
    /// Validates a Label component.
    /// </summary>
    /// <param name="label">The label to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateLabel(Label label)
    {
        if (string.IsNullOrEmpty(label.Text))
        {
            throw new ValidationException(
                "Label text is required.",
                nameof(label.Text),
                null
            );
        }

        if (label.Text.Length > DiscordLimits.MaxLabelTextLength)
        {
            throw new ValidationException(
                nameof(label.Text),
                label.Text,
                $"Label text must not exceed {DiscordLimits.MaxLabelTextLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a FileUpload component.
    /// </summary>
    /// <param name="fileUpload">The file upload to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateFileUpload(FileUpload fileUpload)
    {
        if (string.IsNullOrEmpty(fileUpload.CustomId))
        {
            throw new ValidationException(
                "FileUpload custom ID is required.",
                nameof(fileUpload.CustomId),
                null
            );
        }

        if (string.IsNullOrEmpty(fileUpload.Label))
        {
            throw new ValidationException(
                "FileUpload label is required.",
                nameof(fileUpload.Label),
                null
            );
        }

        if (fileUpload.CustomId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                nameof(fileUpload.CustomId),
                fileUpload.CustomId,
                $"FileUpload custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters."
            );
        }

        if (fileUpload.Label.Length > DiscordLimits.MaxFileUploadLabelLength)
        {
            throw new ValidationException(
                nameof(fileUpload.Label),
                fileUpload.Label,
                $"FileUpload label must not exceed {DiscordLimits.MaxFileUploadLabelLength} characters."
            );
        }

        if (fileUpload.Placeholder != null && fileUpload.Placeholder.Length > DiscordLimits.MaxFileUploadPlaceholderLength)
        {
            throw new ValidationException(
                nameof(fileUpload.Placeholder),
                fileUpload.Placeholder,
                $"FileUpload placeholder must not exceed {DiscordLimits.MaxFileUploadPlaceholderLength} characters."
            );
        }

        if (fileUpload.MinLength.HasValue && fileUpload.MinLength.Value > DiscordLimits.MaxFileUploadMinLength)
        {
            throw new ValidationException(
                $"FileUpload minimum length must not exceed {DiscordLimits.MaxFileUploadMinLength}.",
                nameof(fileUpload.MinLength),
                fileUpload.MinLength.Value
            );
        }

        if (fileUpload.MaxLength.HasValue && fileUpload.MaxLength.Value > DiscordLimits.MaxFileUploadMaxLength)
        {
            throw new ValidationException(
                $"FileUpload maximum length must not exceed {DiscordLimits.MaxFileUploadMaxLength}.",
                nameof(fileUpload.MaxLength),
                fileUpload.MaxLength.Value
            );
        }

        if (fileUpload.MinLength.HasValue && fileUpload.MaxLength.HasValue && fileUpload.MinLength.Value > fileUpload.MaxLength.Value)
        {
            throw new ValidationException(
                "FileUpload minimum length cannot be greater than maximum length.",
                nameof(fileUpload.MinLength),
                fileUpload.MinLength.Value
            );
        }
    }

    /// <summary>
    /// Validates a RadioGroup component.
    /// </summary>
    /// <param name="radioGroup">The radio group to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateRadioGroup(RadioGroup radioGroup)
    {
        if (string.IsNullOrEmpty(radioGroup.CustomId))
        {
            throw new ValidationException(
                "RadioGroup custom ID is required.",
                nameof(radioGroup.CustomId),
                null
            );
        }

        if (string.IsNullOrEmpty(radioGroup.Label))
        {
            throw new ValidationException(
                "RadioGroup label is required.",
                nameof(radioGroup.Label),
                null
            );
        }

        if (radioGroup.CustomId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                nameof(radioGroup.CustomId),
                radioGroup.CustomId,
                $"RadioGroup custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters."
            );
        }

        if (radioGroup.Label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                nameof(radioGroup.Label),
                radioGroup.Label,
                $"RadioGroup label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters."
            );
        }

        if (radioGroup.Options.Count > DiscordLimits.MaxRadioGroupOptions)
        {
            throw new ValidationException(
                $"RadioGroup must not have more than {DiscordLimits.MaxRadioGroupOptions} options.",
                nameof(radioGroup.Options),
                radioGroup.Options.Count
            );
        }

        if (radioGroup.Options.Count == 0)
        {
            throw new ValidationException(
                "RadioGroup must have at least one option.",
                nameof(radioGroup.Options),
                null
            );
        }

        foreach (var option in radioGroup.Options)
        {
            ValidateRadioOption(option);
        }
    }

    /// <summary>
    /// Validates a RadioOption.
    /// </summary>
    /// <param name="option">The option to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateRadioOption(RadioOption option)
    {
        if (string.IsNullOrEmpty(option.Label))
        {
            throw new ValidationException(
                "RadioOption label is required.",
                nameof(option.Label),
                null
            );
        }

        if (string.IsNullOrEmpty(option.Value))
        {
            throw new ValidationException(
                "RadioOption value is required.",
                nameof(option.Value),
                null
            );
        }

        if (option.Label.Length > DiscordLimits.MaxRadioGroupOptionLabelLength)
        {
            throw new ValidationException(
                nameof(option.Label),
                option.Label,
                $"RadioOption label must not exceed {DiscordLimits.MaxRadioGroupOptionLabelLength} characters."
            );
        }

        if (option.Value.Length > DiscordLimits.MaxRadioGroupOptionValueLength)
        {
            throw new ValidationException(
                nameof(option.Value),
                option.Value,
                $"RadioOption value must not exceed {DiscordLimits.MaxRadioGroupOptionValueLength} characters."
            );
        }

        if (option.Description != null && option.Description.Length > DiscordLimits.MaxRadioGroupOptionDescriptionLength)
        {
            throw new ValidationException(
                nameof(option.Description),
                option.Description,
                $"RadioOption description must not exceed {DiscordLimits.MaxRadioGroupOptionDescriptionLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a CheckboxGroup component.
    /// </summary>
    /// <param name="checkboxGroup">The checkbox group to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCheckboxGroup(CheckboxGroup checkboxGroup)
    {
        if (string.IsNullOrEmpty(checkboxGroup.CustomId))
        {
            throw new ValidationException(
                "CheckboxGroup custom ID is required.",
                nameof(checkboxGroup.CustomId),
                null
            );
        }

        if (string.IsNullOrEmpty(checkboxGroup.Label))
        {
            throw new ValidationException(
                "CheckboxGroup label is required.",
                nameof(checkboxGroup.Label),
                null
            );
        }

        if (checkboxGroup.CustomId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                nameof(checkboxGroup.CustomId),
                checkboxGroup.CustomId,
                $"CheckboxGroup custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters."
            );
        }

        if (checkboxGroup.Label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                nameof(checkboxGroup.Label),
                checkboxGroup.Label,
                $"CheckboxGroup label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters."
            );
        }

        if (checkboxGroup.Options.Count > DiscordLimits.MaxCheckboxGroupOptions)
        {
            throw new ValidationException(
                $"CheckboxGroup must not have more than {DiscordLimits.MaxCheckboxGroupOptions} options.",
                nameof(checkboxGroup.Options),
                checkboxGroup.Options.Count
            );
        }

        if (checkboxGroup.Options.Count == 0)
        {
            throw new ValidationException(
                "CheckboxGroup must have at least one option.",
                nameof(checkboxGroup.Options),
                null
            );
        }

        if (checkboxGroup.MinValues.HasValue && checkboxGroup.MaxValues.HasValue && checkboxGroup.MinValues.Value > checkboxGroup.MaxValues.Value)
        {
            throw new ValidationException(
                "CheckboxGroup minimum values cannot be greater than maximum values.",
                nameof(checkboxGroup.MinValues),
                checkboxGroup.MinValues.Value
            );
        }

        foreach (var option in checkboxGroup.Options)
        {
            ValidateCheckboxOption(option);
        }
    }

    /// <summary>
    /// Validates a CheckboxOption.
    /// </summary>
    /// <param name="option">The option to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCheckboxOption(CheckboxOption option)
    {
        if (string.IsNullOrEmpty(option.Label))
        {
            throw new ValidationException(
                "CheckboxOption label is required.",
                nameof(option.Label),
                null
            );
        }

        if (string.IsNullOrEmpty(option.Value))
        {
            throw new ValidationException(
                "CheckboxOption value is required.",
                nameof(option.Value),
                null
            );
        }

        if (option.Label.Length > DiscordLimits.MaxCheckboxGroupOptionLabelLength)
        {
            throw new ValidationException(
                nameof(option.Label),
                option.Label,
                $"CheckboxOption label must not exceed {DiscordLimits.MaxCheckboxGroupOptionLabelLength} characters."
            );
        }

        if (option.Value.Length > DiscordLimits.MaxCheckboxGroupOptionValueLength)
        {
            throw new ValidationException(
                nameof(option.Value),
                option.Value,
                $"CheckboxOption value must not exceed {DiscordLimits.MaxCheckboxGroupOptionValueLength} characters."
            );
        }

        if (option.Description != null && option.Description.Length > DiscordLimits.MaxCheckboxGroupOptionDescriptionLength)
        {
            throw new ValidationException(
                nameof(option.Description),
                option.Description,
                $"CheckboxOption description must not exceed {DiscordLimits.MaxCheckboxGroupOptionDescriptionLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a Checkbox component.
    /// </summary>
    /// <param name="checkbox">The checkbox to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateCheckbox(Checkbox checkbox)
    {
        if (string.IsNullOrEmpty(checkbox.CustomId))
        {
            throw new ValidationException(
                "Checkbox custom ID is required.",
                nameof(checkbox.CustomId),
                null
            );
        }

        if (string.IsNullOrEmpty(checkbox.Label))
        {
            throw new ValidationException(
                "Checkbox label is required.",
                nameof(checkbox.Label),
                null
            );
        }

        if (checkbox.CustomId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                nameof(checkbox.CustomId),
                checkbox.CustomId,
                $"Checkbox custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters."
            );
        }

        if (checkbox.Label.Length > DiscordLimits.MaxCheckboxLabelLength)
        {
            throw new ValidationException(
                nameof(checkbox.Label),
                checkbox.Label,
                $"Checkbox label must not exceed {DiscordLimits.MaxCheckboxLabelLength} characters."
            );
        }
    }

    /// <summary>
    /// Validates a Container component.
    /// </summary>
    /// <param name="container">The container to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateContainer(Container container)
    {
        if (container.Components.Count > DiscordLimits.MaxComponentsPerContainer)
        {
            throw new ValidationException(
                $"Container must not contain more than {DiscordLimits.MaxComponentsPerContainer} components.",
                nameof(container.Components),
                container.Components.Count
            );
        }

        // Validate each component in the container
        foreach (var component in container.Components)
        {
            ValidateComponent(component);
        }
    }

    /// <summary>
    /// Validates a Thumbnail component.
    /// </summary>
    /// <param name="thumbnail">The thumbnail to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateThumbnail(ThumbnailComponent thumbnail)
    {
        if (string.IsNullOrEmpty(thumbnail.Media.Url))
        {
            throw new ValidationException(
                "Thumbnail must have a URL.",
                nameof(thumbnail.Media.Url),
                null
            );
        }
    }

    /// <summary>
    /// Validates a File component.
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateFile(FileComponent file)
    {
        if (string.IsNullOrEmpty(file.File.Url))
        {
            throw new ValidationException(
                "File must have a URL.",
                nameof(file.File.Url),
                null
            );
        }
    }
}
