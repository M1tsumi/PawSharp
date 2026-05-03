#nullable enable
using System.Collections.Generic;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using static PawSharp.Interactions.InteractionResponseType;

namespace PawSharp.Interactions.Builders;

/// <summary>
/// Fluent builder for Discord modal dialogs.
/// </summary>
public class ModalBuilder
{
    private string _customId = string.Empty;
    private string _title = string.Empty;
    private readonly List<MessageComponent> _components = new();

    /// <summary>
    /// Sets the custom ID that will be sent back in the MODAL_SUBMIT interaction.
    /// </summary>
    public ModalBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }

    /// <summary>
    /// Sets the title shown at the top of the modal.
    /// </summary>
    public ModalBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Adds a text input field to the modal.
    /// Each input is automatically wrapped in an ActionRow as Discord requires.
    /// </summary>
    /// <param name="label">Label shown above the input field.</param>
    /// <param name="customId">Unique ID used to retrieve submitted value.</param>
    /// <param name="style">1 = SHORT (single-line), 2 = PARAGRAPH (multi-line).</param>
    /// <param name="required">Whether the field must be filled before submission.</param>
    /// <param name="placeholder">Placeholder text shown when the field is empty.</param>
    /// <param name="minLength">Minimum character count.</param>
    /// <param name="maxLength">Maximum character count (max 4000).</param>
    public ModalBuilder AddTextInput(
        string label,
        string customId,
        TextInputStyle style = TextInputStyle.Short,
        bool required = true,
        string? placeholder = null,
        int? minLength = null,
        int? maxLength = null)
    {
        var input = new TextInput
        {
            Label     = label,
            CustomId  = customId,
            Style     = style,
            Required  = required,
            Placeholder = placeholder,
            MinLength = minLength,
            MaxLength = maxLength
        };

        // Discord requires each component to be inside an ActionRow
        _components.Add(new ActionRow { Components = new List<MessageComponent> { input } });
        return this;
    }

    /// <summary>
    /// Builds the <see cref="InteractionCallbackData"/> payload ready to be sent as a Modal response.
    /// </summary>
    public InteractionCallbackData Build()
    {
        return new InteractionCallbackData
        {
            CustomId = _customId,
            Title = _title,
            Components = _components
        };
    }

    /// <summary>
    /// Builds a complete <see cref="InteractionResponse"/> wrapping the modal payload.
    /// </summary>
    public InteractionResponse BuildResponse()
    {
        return new InteractionResponse
        {
            Type = (int)Modal,
            Data = Build()
        };
    }
}
