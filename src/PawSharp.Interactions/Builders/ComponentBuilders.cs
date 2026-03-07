#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;
using CoreComponents = PawSharp.Core.Entities;

namespace PawSharp.Interactions.Builders;

/// <summary>
/// Builder for creating buttons.
/// </summary>
public class ButtonBuilder
{
    private readonly Button _button = new();

    public ButtonBuilder(string customId, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        _button.CustomId = customId;
        _button.Label = label;
        _button.Style = style;
    }

    public ButtonBuilder SetDisabled(bool disabled)
    {
        _button.Disabled = disabled;
        return this;
    }

    /// <summary>Sets a Unicode emoji on the button (e.g. <c>"🔥"</c>).</summary>
    public ButtonBuilder SetEmoji(string unicodeEmoji)
    {
        _button.Emoji = new Emoji { Name = unicodeEmoji };
        return this;
    }

    /// <summary>Sets a custom guild emoji on the button.</summary>
    /// <param name="name">Emoji name (without colons).</param>
    /// <param name="id">The guild emoji snowflake ID.</param>
    /// <param name="animated">Whether the emoji is animated.</param>
    public ButtonBuilder SetCustomEmoji(string name, ulong id, bool animated = false)
    {
        _button.Emoji = new Emoji { Name = name, Id = id, Animated = animated };
        return this;
    }

    /// <summary>Sets the SKU ID for a <see cref="ButtonStyle.Premium"/> button.</summary>
    public ButtonBuilder SetSkuId(ulong skuId)
    {
        _button.SkuId = skuId;
        _button.Style = ButtonStyle.Premium;
        _button.CustomId = null;
        _button.Label = null;
        _button.Emoji = null;
        return this;
    }

    public ButtonBuilder SetUrl(string url)
    {
        _button.Url = url;
        _button.Style = ButtonStyle.Link;
        _button.CustomId = null; // Link buttons don't have custom IDs
        return this;
    }

    public Button Build() => _button;
}

/// <summary>
/// Builder for creating select menus.
/// </summary>
public class SelectMenuBuilder
{
    private readonly SelectMenu _selectMenu = new();

    public SelectMenuBuilder(string customId, string placeholder = "Select an option")
    {
        _selectMenu.CustomId = customId;
        _selectMenu.Placeholder = placeholder;
    }

    public SelectMenuBuilder AddOption(string label, string value, string? description = null, bool isDefault = false)
    {
        _selectMenu.Options.Add(new SelectOption
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

    public SelectMenu Build() => _selectMenu;
}

/// <summary>
/// Builder for a User select menu (component type 5) — lets users choose one or more guild members.
/// </summary>
public class UserSelectMenuBuilder
{
    private readonly UserSelectMenu _menu = new();

    public UserSelectMenuBuilder(string customId, string placeholder = "Select a user")
    {
        _menu.CustomId = customId;
        _menu.Placeholder = placeholder;
    }

    public UserSelectMenuBuilder SetMinValues(int min) { _menu.MinValues = min; return this; }
    public UserSelectMenuBuilder SetMaxValues(int max) { _menu.MaxValues = max; return this; }
    public UserSelectMenuBuilder SetDisabled(bool disabled) { _menu.Disabled = disabled; return this; }
    public UserSelectMenu Build() => _menu;
}

/// <summary>
/// Builder for a Role select menu (component type 6) — lets users choose one or more guild roles.
/// </summary>
public class RoleSelectMenuBuilder
{
    private readonly RoleSelectMenu _menu = new();

    public RoleSelectMenuBuilder(string customId, string placeholder = "Select a role")
    {
        _menu.CustomId = customId;
        _menu.Placeholder = placeholder;
    }

    public RoleSelectMenuBuilder SetMinValues(int min) { _menu.MinValues = min; return this; }
    public RoleSelectMenuBuilder SetMaxValues(int max) { _menu.MaxValues = max; return this; }
    public RoleSelectMenuBuilder SetDisabled(bool disabled) { _menu.Disabled = disabled; return this; }
    public RoleSelectMenu Build() => _menu;
}

/// <summary>
/// Builder for a Mentionable select menu (component type 7) — lets users choose users or roles.
/// </summary>
public class MentionableSelectMenuBuilder
{
    private readonly MentionableSelectMenu _menu = new();

    public MentionableSelectMenuBuilder(string customId, string placeholder = "Select a user or role")
    {
        _menu.CustomId = customId;
        _menu.Placeholder = placeholder;
    }

    public MentionableSelectMenuBuilder SetMinValues(int min) { _menu.MinValues = min; return this; }
    public MentionableSelectMenuBuilder SetMaxValues(int max) { _menu.MaxValues = max; return this; }
    public MentionableSelectMenuBuilder SetDisabled(bool disabled) { _menu.Disabled = disabled; return this; }
    public MentionableSelectMenu Build() => _menu;
}

/// <summary>
/// Builder for a Channel select menu (component type 8) — lets users choose one or more channels.
/// </summary>
public class ChannelSelectMenuBuilder
{
    private readonly ChannelSelectMenu _menu = new();

    public ChannelSelectMenuBuilder(string customId, string placeholder = "Select a channel")
    {
        _menu.CustomId = customId;
        _menu.Placeholder = placeholder;
    }

    /// <summary>Restricts channels shown to the specified types (<see cref="PawSharp.Core.Enums.ChannelType"/> int values).</summary>
    public ChannelSelectMenuBuilder SetChannelTypes(params int[] channelTypes)
    {
        _menu.ChannelTypes = new List<int>(channelTypes);
        return this;
    }

    public ChannelSelectMenuBuilder SetMinValues(int min) { _menu.MinValues = min; return this; }
    public ChannelSelectMenuBuilder SetMaxValues(int max) { _menu.MaxValues = max; return this; }
    public ChannelSelectMenuBuilder SetDisabled(bool disabled) { _menu.Disabled = disabled; return this; }
    public ChannelSelectMenu Build() => _menu;
}

/// <summary>
/// Builder for creating action rows (component containers).
/// </summary>
public class ActionRowBuilder
{
    private readonly ActionRow _actionRow = new();

    public ActionRowBuilder AddComponent(MessageComponent component)
    {
        if (_actionRow.Components.Count >= 5)
            throw new InvalidOperationException("An ActionRow cannot contain more than 5 components.");
        _actionRow.Components.Add(component);
        return this;
    }

    public ActionRowBuilder AddButton(ButtonBuilder button) => AddComponent(button.Build());

    public ActionRowBuilder AddSelectMenu(SelectMenuBuilder selectMenu) => AddComponent(selectMenu.Build());

    public ActionRow Build() => _actionRow;
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
