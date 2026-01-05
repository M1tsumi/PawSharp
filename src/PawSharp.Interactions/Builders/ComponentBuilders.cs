using System.Collections.Generic;
using PawSharp.Interactions.Models;

namespace PawSharp.Interactions.Builders;

/// <summary>
/// Builder for creating buttons.
/// </summary>
public class ButtonBuilder
{
    private readonly MessageComponent _button = new() { Type = ComponentType.Button };

    public ButtonBuilder(string customId, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        _button.CustomId = customId;
        _button.Label = label;
        _button.Style = (int)style;
    }

    public ButtonBuilder SetDisabled(bool disabled)
    {
        _button.Disabled = disabled;
        return this;
    }

    public ButtonBuilder SetEmoji(string emoji)
    {
        _button.Emoji = new { name = emoji };
        return this;
    }

    public ButtonBuilder SetUrl(string url)
    {
        _button.Url = url;
        _button.Style = (int)ButtonStyle.Link;
        _button.CustomId = null; // Link buttons don't have custom IDs
        return this;
    }

    public MessageComponent Build()
    {
        return _button;
    }
}

/// <summary>
/// Builder for creating select menus.
/// </summary>
public class SelectMenuBuilder
{
    private readonly MessageComponent _selectMenu = new() 
    { 
        Type = ComponentType.StringSelect,
        Options = new List<SelectMenuOption>()
    };

    public SelectMenuBuilder(string customId, string placeholder = "Select an option")
    {
        _selectMenu.CustomId = customId;
        _selectMenu.Placeholder = placeholder;
    }

    public SelectMenuBuilder AddOption(string label, string value, string? description = null, bool isDefault = false)
    {
        _selectMenu.Options!.Add(new SelectMenuOption
        {
            Label = label,
            Value = value,
            Description = description,
            Default = isDefault
        });
        return this;
    }

    public SelectMenuBuilder SetMinValues(int min)
    {
        _selectMenu.MinValues = min;
        return this;
    }

    public SelectMenuBuilder SetMaxValues(int max)
    {
        _selectMenu.MaxValues = max;
        return this;
    }

    public SelectMenuBuilder SetDisabled(bool disabled)
    {
        _selectMenu.Disabled = disabled;
        return this;
    }

    public MessageComponent Build()
    {
        return _selectMenu;
    }
}

/// <summary>
/// Builder for creating action rows (component containers).
/// </summary>
public class ActionRowBuilder
{
    private readonly MessageComponent _actionRow = new()
    {
        Type = ComponentType.ActionRow,
        Components = new List<MessageComponent>()
    };

    public ActionRowBuilder AddComponent(MessageComponent component)
    {
        _actionRow.Components!.Add(component);
        return this;
    }

    public ActionRowBuilder AddButton(ButtonBuilder button)
    {
        return AddComponent(button.Build());
    }

    public ActionRowBuilder AddSelectMenu(SelectMenuBuilder selectMenu)
    {
        return AddComponent(selectMenu.Build());
    }

    public MessageComponent Build()
    {
        return _actionRow;
    }
}
