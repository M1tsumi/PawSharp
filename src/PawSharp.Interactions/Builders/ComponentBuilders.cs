#nullable enable
using System.Collections.Generic;
using PawSharp.Interactions.Models;
using CoreComponents = PawSharp.Core.Entities;

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

// ── Components v2 builders ────────────────────────────────────────────────────

/// <summary>
/// Builder for <see cref="CoreComponents.TextDisplay"/> (component type 10).
/// Renders a block of markdown-formatted text in a Components v2 layout.
/// </summary>
public class TextDisplayBuilder
{
    private readonly CoreComponents.TextDisplay _component = new();

    public TextDisplayBuilder(string content)
    {
        _component.Content = content;
    }

    public TextDisplayBuilder SetContent(string content)
    {
        _component.Content = content;
        return this;
    }

    public CoreComponents.TextDisplay Build() => _component;
}

/// <summary>
/// Builder for <see cref="CoreComponents.Separator"/> (component type 14).
/// Renders a visual divider between other Components v2 elements.
/// </summary>
public class SeparatorBuilder
{
    private readonly CoreComponents.Separator _component = new();

    public SeparatorBuilder SetDivider(bool divider)
    {
        _component.Divider = divider;
        return this;
    }

    public SeparatorBuilder SetSpacing(CoreComponents.SeparatorSpacing spacing)
    {
        _component.Spacing = spacing;
        return this;
    }

    public CoreComponents.Separator Build() => _component;
}

/// <summary>
/// Builder for <see cref="CoreComponents.MediaGallery"/> (component type 12).
/// </summary>
public class MediaGalleryBuilder
{
    private readonly CoreComponents.MediaGallery _component = new()
    {
        Items = new List<CoreComponents.MediaGalleryItem>()
    };

    /// <summary>Adds a media item to the gallery.</summary>
    /// <param name="url">URL of the image or video.</param>
    /// <param name="description">Alt text / description.</param>
    /// <param name="spoiler">Whether to hide the media behind a spoiler overlay.</param>
    public MediaGalleryBuilder AddItem(string url, string? description = null, bool spoiler = false)
    {
        _component.Items!.Add(new CoreComponents.MediaGalleryItem
        {
            Media = new CoreComponents.UnfurledMediaItem { Url = url },
            Description = description,
            Spoiler = spoiler,
        });
        return this;
    }

    public CoreComponents.MediaGallery Build() => _component;
}

/// <summary>
/// Builder for <see cref="CoreComponents.Section"/> (component type 9).
/// A horizontal row that groups text displays with an optional accessory (button or thumbnail).
/// </summary>
public class SectionBuilder
{
    private readonly CoreComponents.Section _component = new()
    {
        Components = new List<CoreComponents.MessageComponent>()
    };

    public SectionBuilder AddText(string content)
    {
        _component.Components!.Add(new CoreComponents.TextDisplay { Content = content });
        return this;
    }

    public SectionBuilder SetAccessory(CoreComponents.MessageComponent accessory)
    {
        _component.Accessory = accessory;
        return this;
    }

    public CoreComponents.Section Build() => _component;
}

/// <summary>
/// Builder for <see cref="CoreComponents.Container"/> (component type 17).
/// A full-width layout container that can hold any mix of Components v2 elements.
/// </summary>
public class ContainerBuilder
{
    private readonly CoreComponents.Container _component = new()
    {
        Components = new List<CoreComponents.MessageComponent>()
    };

    public ContainerBuilder AddComponent(CoreComponents.MessageComponent component)
    {
        _component.Components!.Add(component);
        return this;
    }

    public ContainerBuilder AddTextDisplay(string content)
        => AddComponent(new CoreComponents.TextDisplay { Content = content });

    public ContainerBuilder AddSeparator(bool divider = true, CoreComponents.SeparatorSpacing spacing = CoreComponents.SeparatorSpacing.Small)
        => AddComponent(new CoreComponents.Separator { Divider = divider, Spacing = spacing });

    public ContainerBuilder AddMediaGallery(CoreComponents.MediaGallery gallery)
        => AddComponent(gallery);

    public ContainerBuilder AddSection(CoreComponents.Section section)
        => AddComponent(section);

    public ContainerBuilder SetAccentColor(int color)
    {
        _component.AccentColor = color;
        return this;
    }

    public ContainerBuilder SetSpoiler(bool spoiler)
    {
        _component.Spoiler = spoiler;
        return this;
    }

    public CoreComponents.Container Build() => _component;
}
