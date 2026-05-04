#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;
using PawSharp.Core.Validation;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Builders;

/// <summary>
/// Fluent builder for constructing Discord message components with validation.
/// </summary>
public class ComponentBuilder
{
    private readonly List<MessageComponent> _components = new();
    
    /// <summary>
    /// Creates a new ComponentBuilder.
    /// </summary>
    public ComponentBuilder() { }
    
    /// <summary>
    /// Adds an ActionRow to the components.
    /// </summary>
    /// <param name="configure">Action to configure the ActionRow.</param>
    /// <returns>The builder for method chaining.</returns>
    public ComponentBuilder AddActionRow(Action<ActionRowBuilder> configure)
    {
        var builder = new ActionRowBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a Section component (Components v2).
    /// </summary>
    /// <param name="configure">Action to configure the Section.</param>
    /// <returns>The builder for method chaining.</returns>
    public ComponentBuilder AddSection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a Separator component (Components v2).
    /// </summary>
    /// <param name="configure">Action to configure the Separator.</param>
    /// <returns>The builder for method chaining.</returns>
    public ComponentBuilder AddSeparator(Action<SeparatorBuilder> configure)
    {
        var builder = new SeparatorBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a Container component (Components v2).
    /// </summary>
    /// <param name="configure">Action to configure the Container.</param>
    /// <returns>The builder for method chaining.</returns>
    public ComponentBuilder AddContainer(Action<ContainerBuilder> configure)
    {
        var builder = new ContainerBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Builds the component list.
    /// </summary>
    /// <returns>List of message components.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public List<MessageComponent> Build()
    {
        ComponentValidator.ValidateComponentHierarchy(_components);
        return new List<MessageComponent>(_components);
    }
}

/// <summary>
/// Builder for ActionRow components.
/// </summary>
public class ActionRowBuilder
{
    private readonly List<MessageComponent> _components = new();
    
    /// <summary>
    /// Adds a button to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the button.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddButton(Action<ButtonBuilder> configure)
    {
        var builder = new ButtonBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }

    // ── Quick button methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds a primary (blurple) button to the ActionRow.
    /// </summary>
    /// <param name="label">The button label.</param>
    /// <param name="customId">The button's custom ID.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddPrimaryButton(string label, string customId)
    {
        return AddButton(b => b.WithStyle(ButtonStyle.Primary).WithLabel(label).WithCustomId(customId));
    }

    /// <summary>
    /// Adds a secondary (gray) button to the ActionRow.
    /// </summary>
    /// <param name="label">The button label.</param>
    /// <param name="customId">The button's custom ID.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddSecondaryButton(string label, string customId)
    {
        return AddButton(b => b.WithStyle(ButtonStyle.Secondary).WithLabel(label).WithCustomId(customId));
    }

    /// <summary>
    /// Adds a success (green) button to the ActionRow.
    /// </summary>
    /// <param name="label">The button label.</param>
    /// <param name="customId">The button's custom ID.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddSuccessButton(string label, string customId)
    {
        return AddButton(b => b.WithStyle(ButtonStyle.Success).WithLabel(label).WithCustomId(customId));
    }

    /// <summary>
    /// Adds a danger (red) button to the ActionRow.
    /// </summary>
    /// <param name="label">The button label.</param>
    /// <param name="customId">The button's custom ID.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddDangerButton(string label, string customId)
    {
        return AddButton(b => b.WithStyle(ButtonStyle.Danger).WithLabel(label).WithCustomId(customId));
    }

    /// <summary>
    /// Adds a link button to the ActionRow.
    /// </summary>
    /// <param name="label">The button label.</param>
    /// <param name="url">The URL the button links to.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddLinkButton(string label, string url)
    {
        return AddButton(b => b.WithStyle(ButtonStyle.Link).WithLabel(label).WithUrl(url));
    }
    
    /// <summary>
    /// Adds a string select menu to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the select menu.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddStringSelect(Action<StringSelectMenuBuilder> configure)
    {
        var builder = new StringSelectMenuBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a user select menu to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the select menu.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddUserSelect(Action<UserSelectMenuBuilder> configure)
    {
        var builder = new UserSelectMenuBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a role select menu to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the select menu.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddRoleSelect(Action<RoleSelectMenuBuilder> configure)
    {
        var builder = new RoleSelectMenuBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a mentionable select menu to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the select menu.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddMentionableSelect(Action<MentionableSelectMenuBuilder> configure)
    {
        var builder = new MentionableSelectMenuBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a channel select menu to the ActionRow.
    /// </summary>
    /// <param name="configure">Action to configure the select menu.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddChannelSelect(Action<ChannelSelectMenuBuilder> configure)
    {
        var builder = new ChannelSelectMenuBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a text input to the ActionRow (for modals).
    /// </summary>
    /// <param name="configure">Action to configure the text input.</param>
    /// <returns>The builder for method chaining.</returns>
    public ActionRowBuilder AddTextInput(Action<TextInputBuilder> configure)
    {
        var builder = new TextInputBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Builds the ActionRow.
    /// </summary>
    /// <returns>The ActionRow component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public ActionRow Build()
    {
        if (_components.Count > DiscordLimits.MaxComponentsPerActionRow)
        {
            throw new ValidationException(
                $"ActionRow can contain at most {DiscordLimits.MaxComponentsPerActionRow} components.",
                nameof(_components),
                _components.Count
            );
        }
        
        return new ActionRow
        {
            Components = new List<MessageComponent>(_components)
        };
    }
}

/// <summary>
/// Builder for Button components.
/// </summary>
public class ButtonBuilder
{
    private ButtonStyle _style = ButtonStyle.Primary;
    private string? _label;
    private Entities.Emoji? _emoji;
    private string? _customId;
    private string? _url;
    private ulong? _skuId;
    private bool _disabled = false;
    
    /// <summary>
    /// Sets the button style.
    /// </summary>
    /// <param name="style">The button style.</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithStyle(ButtonStyle style)
    {
        _style = style;
        return this;
    }
    
    /// <summary>
    /// Sets the button label.
    /// </summary>
    /// <param name="label">The label text (max 80 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }
    
    /// <summary>
    /// Sets the button emoji.
    /// </summary>
    /// <param name="emoji">The emoji to display.</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithEmoji(Entities.Emoji emoji)
    {
        _emoji = emoji;
        return this;
    }
    
    /// <summary>
    /// Sets the button custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }
    
    /// <summary>
    /// Sets the button URL (for Link buttons).
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }
    
    /// <summary>
    /// Sets the SKU ID (for Premium buttons).
    /// </summary>
    /// <param name="skuId">The SKU ID.</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithSkuId(ulong skuId)
    {
        _skuId = skuId;
        return this;
    }
    
    /// <summary>
    /// Sets whether the button is disabled.
    /// </summary>
    /// <param name="disabled">Whether disabled.</param>
    /// <returns>The builder for method chaining.</returns>
    public ButtonBuilder WithDisabled(bool disabled = true)
    {
        _disabled = disabled;
        return this;
    }
    
    /// <summary>
    /// Builds the Button.
    /// </summary>
    /// <returns>The Button component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public Button Build()
    {
        if (_label != null && _label.Length > DiscordLimits.MaxButtonLabelLength)
        {
            throw new ValidationException(
                $"Button label must not exceed {DiscordLimits.MaxButtonLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }
        
        if (_customId != null && _customId.Length > DiscordLimits.MaxButtonCustomIdLength)
        {
            throw new ValidationException(
                $"Button custom ID must not exceed {DiscordLimits.MaxButtonCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }
        
        // Link buttons require URL, non-link buttons require custom_id
        if (_style == ButtonStyle.Link && string.IsNullOrEmpty(_url))
        {
            throw new ValidationException(
                "Link buttons must have a URL.",
                nameof(_url),
                null
            );
        }
        
        if (_style != ButtonStyle.Link && _style != ButtonStyle.Premium && string.IsNullOrEmpty(_customId))
        {
            throw new ValidationException(
                "Non-link and non-premium buttons must have a custom ID.",
                nameof(_customId),
                null
            );
        }
        
        return new Button
        {
            Style = _style,
            Label = _label,
            Emoji = _emoji,
            CustomId = _customId,
            Url = _url,
            SkuId = _skuId,
            Disabled = _disabled
        };
    }
}

/// <summary>
/// Base builder for select menu components.
/// </summary>
public abstract class SelectMenuBuilderBase
{
    protected string _customId = string.Empty;
    protected string? _placeholder;
    protected int? _minValues;
    protected int? _maxValues;
    protected bool _disabled = false;
    
    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectMenuBuilderBase WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }
    
    /// <summary>
    /// Sets the placeholder text.
    /// </summary>
    /// <param name="placeholder">The placeholder (max 150 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectMenuBuilderBase WithPlaceholder(string placeholder)
    {
        _placeholder = placeholder;
        return this;
    }
    
    /// <summary>
    /// Sets the minimum number of values.
    /// </summary>
    /// <param name="minValues">Minimum values (0-25).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectMenuBuilderBase WithMinValues(int minValues)
    {
        _minValues = minValues;
        return this;
    }
    
    /// <summary>
    /// Sets the maximum number of values.
    /// </summary>
    /// <param name="maxValues">Maximum values (1-25).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectMenuBuilderBase WithMaxValues(int maxValues)
    {
        _maxValues = maxValues;
        return this;
    }
    
    /// <summary>
    /// Sets whether the select menu is disabled.
    /// </summary>
    /// <param name="disabled">Whether disabled.</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectMenuBuilderBase WithDisabled(bool disabled = true)
    {
        _disabled = disabled;
        return this;
    }
    
    /// <summary>
    /// Validates common select menu properties.
    /// </summary>
    protected void ValidateCommon()
    {
        if (_customId.Length > DiscordLimits.MaxSelectMenuCustomIdLength)
        {
            throw new ValidationException(
                $"Select menu custom ID must not exceed {DiscordLimits.MaxSelectMenuCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }
        
        if (_placeholder != null && _placeholder.Length > DiscordLimits.MaxSelectMenuPlaceholderLength)
        {
            throw new ValidationException(
                $"Select menu placeholder must not exceed {DiscordLimits.MaxSelectMenuPlaceholderLength} characters.",
                nameof(_placeholder),
                _placeholder.Length
            );
        }
        
        if (_minValues.HasValue && _minValues.Value > DiscordLimits.MaxSelectMenuMinValues)
        {
            throw new ValidationException(
                $"Select menu minimum values must not exceed {DiscordLimits.MaxSelectMenuMinValues}.",
                nameof(_minValues),
                _minValues.Value
            );
        }
        
        if (_maxValues.HasValue && _maxValues.Value > DiscordLimits.MaxSelectMenuMaxValues)
        {
            throw new ValidationException(
                $"Select menu maximum values must not exceed {DiscordLimits.MaxSelectMenuMaxValues}.",
                nameof(_maxValues),
                _maxValues.Value
            );
        }
    }
}

/// <summary>
/// Builder for StringSelectMenu components.
/// </summary>
public class StringSelectMenuBuilder : SelectMenuBuilderBase
{
    private readonly List<SelectOption> _options = new();
    
    /// <summary>
    /// Adds an option to the select menu.
    /// </summary>
    /// <param name="configure">Action to configure the option.</param>
    /// <returns>The builder for method chaining.</returns>
    public StringSelectMenuBuilder AddOption(Action<SelectOptionBuilder> configure)
    {
        var builder = new SelectOptionBuilder();
        configure(builder);
        _options.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Builds the StringSelectMenu.
    /// </summary>
    /// <returns>The StringSelectMenu component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public StringSelectMenu Build()
    {
        ValidateCommon();
        
        if (_options.Count > DiscordLimits.MaxSelectMenuOptions)
        {
            throw new ValidationException(
                $"Select menu must not have more than {DiscordLimits.MaxSelectMenuOptions} options.",
                nameof(_options),
                _options.Count
            );
        }
        
        return new StringSelectMenu
        {
            CustomId = _customId,
            Placeholder = _placeholder,
            MinValues = _minValues,
            MaxValues = _maxValues,
            Disabled = _disabled,
            Options = new List<SelectOption>(_options)
        };
    }
}

/// <summary>
/// Builder for SelectOption.
/// </summary>
public class SelectOptionBuilder
{
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string? _description;
    private Entities.Emoji? _emoji;
    private bool _default = false;
    
    /// <summary>
    /// Sets the option label.
    /// </summary>
    /// <param name="label">The label (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectOptionBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }
    
    /// <summary>
    /// Sets the option value.
    /// </summary>
    /// <param name="value">The value (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectOptionBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }
    
    /// <summary>
    /// Sets the option description.
    /// </summary>
    /// <param name="description">The description (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectOptionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    /// <summary>
    /// Sets the option emoji.
    /// </summary>
    /// <param name="emoji">The emoji.</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectOptionBuilder WithEmoji(Entities.Emoji emoji)
    {
        _emoji = emoji;
        return this;
    }
    
    /// <summary>
    /// Sets whether the option is selected by default.
    /// </summary>
    /// <param name="isDefault">Whether default.</param>
    /// <returns>The builder for method chaining.</returns>
    public SelectOptionBuilder WithDefault(bool isDefault = true)
    {
        _default = isDefault;
        return this;
    }
    
    /// <summary>
    /// Builds the SelectOption.
    /// </summary>
    /// <returns>The SelectOption.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public SelectOption Build()
    {
        if (_label.Length > DiscordLimits.MaxSelectMenuOptionLabelLength)
        {
            throw new ValidationException(
                $"Select option label must not exceed {DiscordLimits.MaxSelectMenuOptionLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }
        
        if (_value.Length > DiscordLimits.MaxSelectMenuOptionValueLength)
        {
            throw new ValidationException(
                $"Select option value must not exceed {DiscordLimits.MaxSelectMenuOptionValueLength} characters.",
                nameof(_value),
                _value.Length
            );
        }
        
        if (_description != null && _description.Length > DiscordLimits.MaxSelectMenuOptionDescriptionLength)
        {
            throw new ValidationException(
                $"Select option description must not exceed {DiscordLimits.MaxSelectMenuOptionDescriptionLength} characters.",
                nameof(_description),
                _description.Length
            );
        }
        
        return new SelectOption
        {
            Label = _label,
            Value = _value,
            Description = _description,
            Emoji = _emoji,
            Default = _default
        };
    }
}

/// <summary>
/// Builder for UserSelectMenu components.
/// </summary>
public class UserSelectMenuBuilder : SelectMenuBuilderBase
{
    public UserSelectMenu Build()
    {
        ValidateCommon();
        return new UserSelectMenu
        {
            CustomId = _customId,
            Placeholder = _placeholder,
            MinValues = _minValues,
            MaxValues = _maxValues,
            Disabled = _disabled
        };
    }
}

/// <summary>
/// Builder for RoleSelectMenu components.
/// </summary>
public class RoleSelectMenuBuilder : SelectMenuBuilderBase
{
    public RoleSelectMenu Build()
    {
        ValidateCommon();
        return new RoleSelectMenu
        {
            CustomId = _customId,
            Placeholder = _placeholder,
            MinValues = _minValues,
            MaxValues = _maxValues,
            Disabled = _disabled
        };
    }
}

/// <summary>
/// Builder for MentionableSelectMenu components.
/// </summary>
public class MentionableSelectMenuBuilder : SelectMenuBuilderBase
{
    public MentionableSelectMenu Build()
    {
        ValidateCommon();
        return new MentionableSelectMenu
        {
            CustomId = _customId,
            Placeholder = _placeholder,
            MinValues = _minValues,
            MaxValues = _maxValues,
            Disabled = _disabled
        };
    }
}

/// <summary>
/// Builder for ChannelSelectMenu components.
/// </summary>
public class ChannelSelectMenuBuilder : SelectMenuBuilderBase
{
    private List<int>? _channelTypes;
    
    /// <summary>
    /// Sets the channel types to filter.
    /// </summary>
    /// <param name="channelTypes">List of channel type integers.</param>
    /// <returns>The builder for method chaining.</returns>
    public ChannelSelectMenuBuilder WithChannelTypes(List<int> channelTypes)
    {
        _channelTypes = channelTypes;
        return this;
    }
    
    public ChannelSelectMenu Build()
    {
        ValidateCommon();
        return new ChannelSelectMenu
        {
            CustomId = _customId,
            Placeholder = _placeholder,
            MinValues = _minValues,
            MaxValues = _maxValues,
            Disabled = _disabled,
            ChannelTypes = _channelTypes
        };
    }
}

/// <summary>
/// Builder for TextInput components.
/// </summary>
public class TextInputBuilder
{
    private TextInputStyle _style = TextInputStyle.Short;
    private string _customId = string.Empty;
    private string _label = string.Empty;
    private string? _placeholder;
    private int? _minLength;
    private int? _maxLength;
    private string? _value;
    private bool _required = true;
    
    /// <summary>
    /// Sets the text input style.
    /// </summary>
    /// <param name="style">The style.</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithStyle(TextInputStyle style)
    {
        _style = style;
        return this;
    }
    
    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }
    
    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">The label (max 45 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }
    
    /// <summary>
    /// Sets the placeholder.
    /// </summary>
    /// <param name="placeholder">The placeholder (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithPlaceholder(string placeholder)
    {
        _placeholder = placeholder;
        return this;
    }
    
    /// <summary>
    /// Sets the minimum length.
    /// </summary>
    /// <param name="minLength">Minimum length (0-4000).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithMinLength(int minLength)
    {
        _minLength = minLength;
        return this;
    }
    
    /// <summary>
    /// Sets the maximum length.
    /// </summary>
    /// <param name="maxLength">Maximum length (1-4000).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }
    
    /// <summary>
    /// Sets the pre-filled value.
    /// </summary>
    /// <param name="value">The value (max 4000 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }
    
    /// <summary>
    /// Sets whether the input is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public TextInputBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }
    
    /// <summary>
    /// Builds the TextInput.
    /// </summary>
    /// <returns>The TextInput component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public TextInput Build()
    {
        if (_customId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                $"Text input custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }
        
        if (_label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                $"Text input label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }
        
        if (_placeholder != null && _placeholder.Length > DiscordLimits.MaxTextInputPlaceholderLength)
        {
            throw new ValidationException(
                $"Text input placeholder must not exceed {DiscordLimits.MaxTextInputPlaceholderLength} characters.",
                nameof(_placeholder),
                _placeholder.Length
            );
        }
        
        if (_minLength.HasValue && _minLength.Value > DiscordLimits.MaxTextInputMinLength)
        {
            throw new ValidationException(
                $"Text input minimum length must not exceed {DiscordLimits.MaxTextInputMinLength}.",
                nameof(_minLength),
                _minLength.Value
            );
        }
        
        if (_maxLength.HasValue && _maxLength.Value > DiscordLimits.MaxTextInputMaxLength)
        {
            throw new ValidationException(
                $"Text input maximum length must not exceed {DiscordLimits.MaxTextInputMaxLength}.",
                nameof(_maxLength),
                _maxLength.Value
            );
        }
        
        if (_value != null && _value.Length > DiscordLimits.MaxTextInputValueLength)
        {
            throw new ValidationException(
                $"Text input value must not exceed {DiscordLimits.MaxTextInputValueLength} characters.",
                nameof(_value),
                _value.Length
            );
        }
        
        return new TextInput
        {
            Style = _style,
            CustomId = _customId,
            Label = _label,
            Placeholder = _placeholder,
            MinLength = _minLength,
            MaxLength = _maxLength,
            Value = _value,
            Required = _required
        };
    }
}

// ── Components v2 Builders ─────────────────────────────────────────────────────

/// <summary>
/// Builder for Section components (Components v2).
/// </summary>
public class SectionBuilder
{
    private readonly List<MessageComponent> _components = new();
    private MessageComponent? _accessory;

    /// <summary>
    /// Adds a TextDisplay to the section.
    /// </summary>
    /// <param name="configure">Action to configure the TextDisplay.</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder AddTextDisplay(Action<TextDisplayBuilder> configure)
    {
        var builder = new TextDisplayBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a TextDisplay with content directly.
    /// </summary>
    /// <param name="content">The text content (max 4000 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder AddText(string content)
    {
        _components.Add(new TextDisplay { Content = content });
        return this;
    }

    /// <summary>
    /// Sets the accessory (button, select menu, or thumbnail).
    /// </summary>
    /// <param name="accessory">The accessory component.</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder WithAccessory(MessageComponent accessory)
    {
        _accessory = accessory;
        return this;
    }

    /// <summary>
    /// Sets a button as the accessory using a ButtonBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the button.</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder WithButtonAccessory(Action<ButtonBuilder> configure)
    {
        var builder = new ButtonBuilder();
        configure(builder);
        _accessory = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets a thumbnail as the accessory using a ThumbnailBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the thumbnail.</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder WithThumbnailAccessory(Action<ThumbnailBuilder> configure)
    {
        var builder = new ThumbnailBuilder("");
        configure(builder);
        _accessory = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets a thumbnail as the accessory directly from a URL.
    /// </summary>
    /// <param name="url">The thumbnail URL.</param>
    /// <param name="description">Optional alt text.</param>
    /// <param name="spoiler">Whether the thumbnail is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public SectionBuilder WithThumbnailAccessory(string url, string? description = null, bool spoiler = false)
    {
        _accessory = new ThumbnailBuilder(url)
            .WithDescription(description)
            .WithSpoiler(spoiler)
            .Build();
        return this;
    }

    /// <summary>
    /// Builds the Section.
    /// </summary>
    /// <returns>The Section component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public Section Build()
    {
        // Validate that all components are TextDisplay
        foreach (var component in _components)
        {
            if (component is not TextDisplay)
            {
                throw new ValidationException(
                    "Section can only contain TextDisplay components.",
                    nameof(_components),
                    null
                );
            }
        }

        // Validate accessory if present
        if (_accessory != null && _accessory is not Button && _accessory is not ThumbnailComponent)
        {
            throw new ValidationException(
                "Section accessory must be a Button or Thumbnail component.",
                nameof(_accessory),
                null
            );
        }

        return new Section
        {
            Components = new List<MessageComponent>(_components),
            Accessory = _accessory
        };
    }
}

/// <summary>
/// Builder for TextDisplay components (Components v2).
/// </summary>
public class TextDisplayBuilder
{
    private string _content = string.Empty;
    
    /// <summary>
    /// Sets the text content.
    /// </summary>
    /// <param name="content">The content (max 4000 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public TextDisplayBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }
    
    /// <summary>
    /// Builds the TextDisplay.
    /// </summary>
    /// <returns>The TextDisplay component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public TextDisplay Build()
    {
        if (_content.Length > DiscordLimits.MaxTextDisplayContentLength)
        {
            throw new ValidationException(
                $"Text display content must not exceed {DiscordLimits.MaxTextDisplayContentLength} characters.",
                nameof(_content),
                _content.Length
            );
        }
        
        return new TextDisplay
        {
            Content = _content
        };
    }
}

/// <summary>
/// Builder for Separator components (Components v2).
/// </summary>
public class SeparatorBuilder
{
    private SeparatorSpacing _spacing = SeparatorSpacing.Small;
    private bool _divider = true;

    /// <summary>
    /// Sets the spacing size.
    /// </summary>
    /// <param name="spacing">The spacing.</param>
    /// <returns>The builder for method chaining.</returns>
    public SeparatorBuilder WithSpacing(SeparatorSpacing spacing)
    {
        _spacing = spacing;
        return this;
    }

    /// <summary>
    /// Sets whether to show a visible dividing line.
    /// </summary>
    /// <param name="divider">Whether to show the divider.</param>
    /// <returns>The builder for method chaining.</returns>
    public SeparatorBuilder WithDivider(bool divider = true)
    {
        _divider = divider;
        return this;
    }

    /// <summary>
    /// Builds the Separator.
    /// </summary>
    /// <returns>The Separator component.</returns>
    public Separator Build()
    {
        return new Separator
        {
            Spacing = _spacing,
            Divider = _divider
        };
    }
}

/// <summary>
/// Builder for Container components (Components v2).
/// </summary>
public class ContainerBuilder
{
    private readonly List<MessageComponent> _components = new();
    private int? _accentColor;
    private bool? _spoiler;

    /// <summary>
    /// Adds a component to the container.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddComponent(MessageComponent component)
    {
        _components.Add(component);
        return this;
    }

    /// <summary>
    /// Adds a TextDisplay with content directly.
    /// </summary>
    /// <param name="content">The text content (max 4000 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddText(string content)
    {
        _components.Add(new TextDisplay { Content = content });
        return this;
    }

    /// <summary>
    /// Adds a Section using a SectionBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the section.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddSection(Action<SectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a Separator using a SeparatorBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the separator.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddSeparator(Action<SeparatorBuilder> configure)
    {
        var builder = new SeparatorBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a Separator with default settings.
    /// </summary>
    /// <param name="spacing">The spacing size.</param>
    /// <param name="divider">Whether to show the divider.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddSeparator(SeparatorSpacing spacing = SeparatorSpacing.Small, bool divider = true)
    {
        _components.Add(new Separator { Spacing = spacing, Divider = divider });
        return this;
    }

    /// <summary>
    /// Adds a MediaGallery using a MediaGalleryBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the media gallery.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddMediaGallery(Action<MediaGalleryBuilder> configure)
    {
        var builder = new MediaGalleryBuilder();
        configure(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a File using a FileBuilder.
    /// </summary>
    /// <param name="fileUrl">The file URL.</param>
    /// <param name="configure">Action to configure the file.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddFile(string fileUrl, Action<FileBuilder>? configure = null)
    {
        var builder = new FileBuilder(fileUrl);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a FileUpload using a FileUploadBuilder.
    /// </summary>
    /// <param name="customId">The custom ID.</param>
    /// <param name="label">The label.</param>
    /// <param name="configure">Action to configure the file upload.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddFileUpload(string customId, string label, Action<FileUploadBuilder>? configure = null)
    {
        var builder = new FileUploadBuilder(customId, label);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a pre-built FileUpload component.
    /// </summary>
    /// <param name="fileUpload">The FileUpload component.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddFileUpload(FileUpload fileUpload)
    {
        _components.Add(fileUpload);
        return this;
    }

    /// <summary>
    /// Adds a Label using a LabelBuilder.
    /// </summary>
    /// <param name="text">The label text.</param>
    /// <param name="configure">Action to configure the label.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddLabel(string text, Action<LabelBuilder>? configure = null)
    {
        var builder = new LabelBuilder(text);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a RadioGroup using a RadioGroupBuilder.
    /// </summary>
    /// <param name="customId">The custom ID.</param>
    /// <param name="label">The label.</param>
    /// <param name="configure">Action to configure the radio group.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddRadioGroup(string customId, string label, Action<RadioGroupBuilder>? configure = null)
    {
        var builder = new RadioGroupBuilder(customId, label);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a CheckboxGroup using a CheckboxGroupBuilder.
    /// </summary>
    /// <param name="customId">The custom ID.</param>
    /// <param name="label">The label.</param>
    /// <param name="configure">Action to configure the checkbox group.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddCheckboxGroup(string customId, string label, Action<CheckboxGroupBuilder>? configure = null)
    {
        var builder = new CheckboxGroupBuilder(customId, label);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds a Checkbox using a CheckboxBuilder.
    /// </summary>
    /// <param name="customId">The custom ID.</param>
    /// <param name="label">The label.</param>
    /// <param name="configure">Action to configure the checkbox.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddCheckbox(string customId, string label, Action<CheckboxBuilder>? configure = null)
    {
        var builder = new CheckboxBuilder(customId, label);
        configure?.Invoke(builder);
        _components.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets the accent color.
    /// </summary>
    /// <param name="accentColor">The accent color (integer, same format as role/embed colours).</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder WithAccentColor(int accentColor)
    {
        _accentColor = accentColor;
        return this;
    }

    /// <summary>
    /// Sets whether the entire container is a spoiler.
    /// </summary>
    /// <param name="spoiler">Whether the container is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder WithSpoiler(bool spoiler = true)
    {
        _spoiler = spoiler;
        return this;
    }

    /// <summary>
    /// Builds the Container.
    /// </summary>
    /// <returns>The Container component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public Container Build()
    {
        if (_components.Count > DiscordLimits.MaxComponentsPerContainer)
        {
            throw new ValidationException(
                $"Container can contain at most {DiscordLimits.MaxComponentsPerContainer} components.",
                nameof(_components),
                _components.Count
            );
        }

        return new Container
        {
            Components = new List<MessageComponent>(_components),
            AccentColor = _accentColor,
            Spoiler = _spoiler
        };
    }
}

/// <summary>
/// Builder for Thumbnail components (Components v2).
/// </summary>
public class ThumbnailBuilder
{
    private readonly UnfurledMediaItem _media = new();
    private string? _description;
    private bool? _spoiler;

    /// <summary>
    /// Creates a new ThumbnailBuilder with a URL.
    /// </summary>
    /// <param name="url">The media URL.</param>
    public ThumbnailBuilder(string url)
    {
        _media.Url = url;
    }

    /// <summary>
    /// Sets the media URL.
    /// </summary>
    /// <param name="url">The media URL.</param>
    /// <returns>The builder for method chaining.</returns>
    public ThumbnailBuilder WithUrl(string url)
    {
        _media.Url = url;
        return this;
    }

    /// <summary>
    /// Sets the optional description/alt text.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>The builder for method chaining.</returns>
    public ThumbnailBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets whether the thumbnail is a spoiler.
    /// </summary>
    /// <param name="spoiler">Whether the thumbnail is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public ThumbnailBuilder WithSpoiler(bool spoiler = true)
    {
        _spoiler = spoiler;
        return this;
    }

    /// <summary>
    /// Builds the Thumbnail component.
    /// </summary>
    /// <returns>The Thumbnail component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public ThumbnailComponent Build()
    {
        if (string.IsNullOrEmpty(_media.Url))
        {
            throw new ValidationException(
                "Thumbnail must have a URL.",
                nameof(_media.Url),
                null
            );
        }

        return new ThumbnailComponent
        {
            Media = _media,
            Description = _description,
            Spoiler = _spoiler
        };
    }
}

/// <summary>
/// Builder for File components (Components v2).
/// </summary>
public class FileBuilder
{
    private readonly UnfurledMediaItem _file = new();
    private bool? _spoiler;

    /// <summary>
    /// Creates a new FileBuilder with a file reference URL.
    /// </summary>
    /// <param name="fileUrl">The file URL (typically attachment://filename).</param>
    public FileBuilder(string fileUrl)
    {
        _file.Url = fileUrl;
    }

    /// <summary>
    /// Sets the file reference URL.
    /// </summary>
    /// <param name="fileUrl">The file URL (typically attachment://filename).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileBuilder WithFile(string fileUrl)
    {
        _file.Url = fileUrl;
        return this;
    }

    /// <summary>
    /// Sets whether the file is a spoiler.
    /// </summary>
    /// <param name="spoiler">Whether the file is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public FileBuilder WithSpoiler(bool spoiler = true)
    {
        _spoiler = spoiler;
        return this;
    }

    /// <summary>
    /// Builds the File component.
    /// </summary>
    /// <returns>The File component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public FileComponent Build()
    {
        if (string.IsNullOrEmpty(_file.Url))
        {
            throw new ValidationException(
                "File must have a URL.",
                nameof(_file.Url),
                null
            );
        }

        return new FileComponent
        {
            File = _file,
            Spoiler = _spoiler
        };
    }
}

/// <summary>
/// Builder for MediaGallery components (Components v2).
/// </summary>
public class MediaGalleryBuilder
{
    private readonly List<MediaGalleryItem> _items = new();

    /// <summary>
    /// Adds a media item to the gallery.
    /// </summary>
    /// <param name="url">The media URL.</param>
    /// <param name="description">Optional description/alt text.</param>
    /// <param name="spoiler">Whether the item is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public MediaGalleryBuilder AddItem(string url, string? description = null, bool spoiler = false)
    {
        _items.Add(new MediaGalleryItem
        {
            Media = new UnfurledMediaItem { Url = url },
            Description = description,
            Spoiler = spoiler
        });
        return this;
    }

    /// <summary>
    /// Adds a media item using a MediaItemBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the media item.</param>
    /// <returns>The builder for method chaining.</returns>
    public MediaGalleryBuilder AddItem(Action<MediaItemBuilder> configure)
    {
        var builder = new MediaItemBuilder();
        configure(builder);
        _items.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Builds the MediaGallery component.
    /// </summary>
    /// <returns>The MediaGallery component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public MediaGallery Build()
    {
        if (_items.Count < DiscordLimits.MinMediaGalleryItems)
        {
            throw new ValidationException(
                $"MediaGallery must have at least {DiscordLimits.MinMediaGalleryItems} item(s).",
                nameof(_items),
                _items.Count
            );
        }

        if (_items.Count > DiscordLimits.MaxMediaGalleryItems)
        {
            throw new ValidationException(
                $"MediaGallery can have at most {DiscordLimits.MaxMediaGalleryItems} items.",
                nameof(_items),
                _items.Count
            );
        }

        return new MediaGallery
        {
            Items = new List<MediaGalleryItem>(_items)
        };
    }
}

/// <summary>
/// Builder for MediaGalleryItem.
/// </summary>
public class MediaItemBuilder
{
    private readonly UnfurledMediaItem _media = new();
    private string? _description;
    private bool? _spoiler;

    /// <summary>
    /// Sets the media URL.
    /// </summary>
    /// <param name="url">The media URL.</param>
    /// <returns>The builder for method chaining.</returns>
    public MediaItemBuilder WithUrl(string url)
    {
        _media.Url = url;
        return this;
    }

    /// <summary>
    /// Sets the optional description/alt text.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns>The builder for method chaining.</returns>
    public MediaItemBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets whether the item is a spoiler.
    /// </summary>
    /// <param name="spoiler">Whether the item is a spoiler.</param>
    /// <returns>The builder for method chaining.</returns>
    public MediaItemBuilder WithSpoiler(bool spoiler = true)
    {
        _spoiler = spoiler;
        return this;
    }

    /// <summary>
    /// Builds the MediaGalleryItem.
    /// </summary>
    /// <returns>The MediaGalleryItem.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public MediaGalleryItem Build()
    {
        if (string.IsNullOrEmpty(_media.Url))
        {
            throw new ValidationException(
                "Media item must have a URL.",
                nameof(_media.Url),
                null
            );
        }

        return new MediaGalleryItem
        {
            Media = _media,
            Description = _description,
            Spoiler = _spoiler
        };
    }
}

/// <summary>
/// Builder for Label components (Components v2).
/// </summary>
public class LabelBuilder
{
    private string _text = string.Empty;
    private string? _description;
    private Emoji? _emoji;

    /// <summary>
    /// Creates a new LabelBuilder with text.
    /// </summary>
    /// <param name="text">The label text (max 80 characters).</param>
    public LabelBuilder(string text)
    {
        _text = text;
    }

    /// <summary>
    /// Sets the label text.
    /// </summary>
    /// <param name="text">The label text (max 80 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public LabelBuilder WithText(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    /// Sets the optional description.
    /// </summary>
    /// <param name="description">The description (max 200 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public LabelBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the emoji.
    /// </summary>
    /// <param name="emoji">The emoji.</param>
    /// <returns>The builder for method chaining.</returns>
    public LabelBuilder WithEmoji(Emoji emoji)
    {
        _emoji = emoji;
        return this;
    }

    /// <summary>
    /// Sets a Unicode emoji.
    /// </summary>
    /// <param name="unicodeEmoji">The Unicode emoji (e.g., "🔥").</param>
    /// <returns>The builder for method chaining.</returns>
    public LabelBuilder WithEmoji(string unicodeEmoji)
    {
        _emoji = new Emoji { Name = unicodeEmoji };
        return this;
    }

    /// <summary>
    /// Sets a custom guild emoji.
    /// </summary>
    /// <param name="name">The emoji name.</param>
    /// <param name="id">The emoji ID.</param>
    /// <param name="animated">Whether the emoji is animated.</param>
    /// <returns>The builder for method chaining.</returns>
    public LabelBuilder WithCustomEmoji(string name, ulong id, bool animated = false)
    {
        _emoji = new Emoji { Name = name, Id = id, Animated = animated };
        return this;
    }

    /// <summary>
    /// Builds the Label.
    /// </summary>
    /// <returns>The Label component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public Label Build()
    {
        if (string.IsNullOrEmpty(_text))
        {
            throw new ValidationException(
                "Label text is required.",
                nameof(_text),
                null
            );
        }

        if (_text.Length > DiscordLimits.MaxLabelTextLength)
        {
            throw new ValidationException(
                $"Label text must not exceed {DiscordLimits.MaxLabelTextLength} characters.",
                nameof(_text),
                _text.Length
            );
        }

        if (_description != null && _description.Length > DiscordLimits.MaxLabelDescriptionLength)
        {
            throw new ValidationException(
                $"Label description must not exceed {DiscordLimits.MaxLabelDescriptionLength} characters.",
                nameof(_description),
                _description.Length
            );
        }

        return new Label
        {
            Text = _text,
            Emoji = _emoji
        };
    }
}

/// <summary>
/// Builder for FileUpload components (Components v2).
/// </summary>
public class FileUploadBuilder
{
    private string _customId = string.Empty;
    private string _label = string.Empty;
    private bool? _required = true;
    private string? _placeholder;
    private int? _minLength;
    private int? _maxLength;
    private List<string>? _fileTypes;

    /// <summary>
    /// Creates a new FileUploadBuilder.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <param name="label">The label (max 45 characters).</param>
    public FileUploadBuilder(string customId, string label)
    {
        _customId = customId;
        _label = label;
    }

    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }

    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">The label (max 45 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Sets whether the file upload is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }

    /// <summary>
    /// Sets the placeholder text.
    /// </summary>
    /// <param name="placeholder">The placeholder (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithPlaceholder(string? placeholder)
    {
        _placeholder = placeholder;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of files.
    /// </summary>
    /// <param name="minLength">Minimum files (0-10).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithMinLength(int minLength)
    {
        _minLength = minLength;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of files.
    /// </summary>
    /// <param name="maxLength">Maximum files (1-10).</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>
    /// Sets the accepted file types (MIME types).
    /// </summary>
    /// <param name="fileTypes">The file types (e.g., "image/*", "application/pdf").</param>
    /// <returns>The builder for method chaining.</returns>
    public FileUploadBuilder WithFileTypes(params string[] fileTypes)
    {
        _fileTypes = new List<string>(fileTypes);
        return this;
    }

    /// <summary>
    /// Builds the FileUpload.
    /// </summary>
    /// <returns>The FileUpload component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public FileUpload Build()
    {
        if (string.IsNullOrEmpty(_customId))
        {
            throw new ValidationException(
                "FileUpload custom ID is required.",
                nameof(_customId),
                null
            );
        }

        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "FileUpload label is required.",
                nameof(_label),
                null
            );
        }

        if (_customId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                $"FileUpload custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }

        if (_label.Length > DiscordLimits.MaxFileUploadLabelLength)
        {
            throw new ValidationException(
                $"FileUpload label must not exceed {DiscordLimits.MaxFileUploadLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        if (_placeholder != null && _placeholder.Length > DiscordLimits.MaxFileUploadPlaceholderLength)
        {
            throw new ValidationException(
                $"FileUpload placeholder must not exceed {DiscordLimits.MaxFileUploadPlaceholderLength} characters.",
                nameof(_placeholder),
                _placeholder.Length
            );
        }

        if (_minLength.HasValue && _minLength.Value > DiscordLimits.MaxFileUploadMinLength)
        {
            throw new ValidationException(
                $"FileUpload minimum length must not exceed {DiscordLimits.MaxFileUploadMinLength}.",
                nameof(_minLength),
                _minLength.Value
            );
        }

        if (_maxLength.HasValue && _maxLength.Value > DiscordLimits.MaxFileUploadMaxLength)
        {
            throw new ValidationException(
                $"FileUpload maximum length must not exceed {DiscordLimits.MaxFileUploadMaxLength}.",
                nameof(_maxLength),
                _maxLength.Value
            );
        }

        if (_minLength.HasValue && _maxLength.HasValue && _minLength.Value > _maxLength.Value)
        {
            throw new ValidationException(
                "FileUpload minimum length cannot be greater than maximum length.",
                nameof(_minLength),
                _minLength.Value
            );
        }

        return new FileUpload
        {
            CustomId = _customId,
            Label = _label,
            Required = _required,
            Placeholder = _placeholder,
            MinLength = _minLength,
            MaxLength = _maxLength,
            FileTypes = _fileTypes
        };
    }
}

/// <summary>
/// Builder for RadioGroup components (Components v2).
/// </summary>
public class RadioGroupBuilder
{
    private string _customId = string.Empty;
    private string _label = string.Empty;
    private readonly List<RadioOption> _options = new();
    private bool? _required = true;
    private int? _defaultValue;

    /// <summary>
    /// Creates a new RadioGroupBuilder.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <param name="label">The label (max 45 characters).</param>
    public RadioGroupBuilder(string customId, string label)
    {
        _customId = customId;
        _label = label;
    }

    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }

    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">The label (max 45 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Adds an option to the radio group.
    /// </summary>
    /// <param name="label">The option label (max 100 characters).</param>
    /// <param name="value">The option value (max 100 characters).</param>
    /// <param name="description">Optional description (max 100 characters).</param>
    /// <param name="isDefault">Whether this option is selected by default.</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder AddOption(string label, string value, string? description = null, bool isDefault = false)
    {
        _options.Add(new RadioOption
        {
            Label = label,
            Value = value,
            Description = description,
            Default = isDefault
        });
        return this;
    }

    /// <summary>
    /// Adds an option using a RadioOptionBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the option.</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder AddOption(Action<RadioOptionBuilder> configure)
    {
        var builder = new RadioOptionBuilder();
        configure(builder);
        _options.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets whether the radio group is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }

    /// <summary>
    /// Sets the default selected option index.
    /// </summary>
    /// <param name="index">The index of the default option (0-based).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioGroupBuilder WithDefaultValue(int index)
    {
        _defaultValue = index;
        return this;
    }

    /// <summary>
    /// Builds the RadioGroup component.
    /// </summary>
    /// <returns>The RadioGroup component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public RadioGroup Build()
    {
        if (string.IsNullOrEmpty(_customId))
        {
            throw new ValidationException(
                "RadioGroup custom ID is required.",
                nameof(_customId),
                null
            );
        }

        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "RadioGroup label is required.",
                nameof(_label),
                null
            );
        }

        if (_customId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                $"RadioGroup custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }

        if (_label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                $"RadioGroup label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        if (_options.Count > DiscordLimits.MaxRadioGroupOptions)
        {
            throw new ValidationException(
                $"RadioGroup can have at most {DiscordLimits.MaxRadioGroupOptions} options.",
                nameof(_options),
                _options.Count
            );
        }

        if (_options.Count == 0)
        {
            throw new ValidationException(
                "RadioGroup must have at least one option.",
                nameof(_options),
                null
            );
        }

        return new RadioGroup
        {
            CustomId = _customId,
            Options = new List<RadioOption>(_options),
            Label = _label,
            Required = _required,
            DefaultValue = _defaultValue
        };
    }
}

/// <summary>
/// Builder for RadioOption.
/// </summary>
public class RadioOptionBuilder
{
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string? _description;
    private Emoji? _emoji;
    private bool _default = false;

    /// <summary>
    /// Sets the option label.
    /// </summary>
    /// <param name="label">The label (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioOptionBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Sets the option value.
    /// </summary>
    /// <param name="value">The value (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioOptionBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Sets the optional description.
    /// </summary>
    /// <param name="description">The description (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioOptionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the emoji.
    /// </summary>
    /// <param name="emoji">The emoji.</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioOptionBuilder WithEmoji(Emoji emoji)
    {
        _emoji = emoji;
        return this;
    }

    /// <summary>
    /// Sets whether this option is selected by default.
    /// </summary>
    /// <param name="isDefault">Whether default.</param>
    /// <returns>The builder for method chaining.</returns>
    public RadioOptionBuilder WithDefault(bool isDefault = true)
    {
        _default = isDefault;
        return this;
    }

    /// <summary>
    /// Builds the RadioOption.
    /// </summary>
    /// <returns>The RadioOption.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public RadioOption Build()
    {
        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "RadioOption label is required.",
                nameof(_label),
                null
            );
        }

        if (string.IsNullOrEmpty(_value))
        {
            throw new ValidationException(
                "RadioOption value is required.",
                nameof(_value),
                null
            );
        }

        if (_label.Length > DiscordLimits.MaxRadioGroupOptionLabelLength)
        {
            throw new ValidationException(
                $"RadioOption label must not exceed {DiscordLimits.MaxRadioGroupOptionLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        if (_value.Length > DiscordLimits.MaxRadioGroupOptionValueLength)
        {
            throw new ValidationException(
                $"RadioOption value must not exceed {DiscordLimits.MaxRadioGroupOptionValueLength} characters.",
                nameof(_value),
                _value.Length
            );
        }

        if (_description != null && _description.Length > DiscordLimits.MaxRadioGroupOptionDescriptionLength)
        {
            throw new ValidationException(
                $"RadioOption description must not exceed {DiscordLimits.MaxRadioGroupOptionDescriptionLength} characters.",
                nameof(_description),
                _description.Length
            );
        }

        return new RadioOption
        {
            Label = _label,
            Value = _value,
            Description = _description,
            Emoji = _emoji,
            Default = _default
        };
    }
}

/// <summary>
/// Builder for CheckboxGroup components (Components v2).
/// </summary>
public class CheckboxGroupBuilder
{
    private string _customId = string.Empty;
    private string _label = string.Empty;
    private readonly List<CheckboxOption> _options = new();
    private bool? _required = true;
    private int? _minValues;
    private int? _maxValues;

    /// <summary>
    /// Creates a new CheckboxGroupBuilder.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <param name="label">The label (max 45 characters).</param>
    public CheckboxGroupBuilder(string customId, string label)
    {
        _customId = customId;
        _label = label;
    }

    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }

    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">The label (max 45 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Adds an option to the checkbox group.
    /// </summary>
    /// <param name="label">The option label (max 100 characters).</param>
    /// <param name="value">The option value (max 100 characters).</param>
    /// <param name="description">Optional description (max 100 characters).</param>
    /// <param name="isDefault">Whether this option is checked by default.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder AddOption(string label, string value, string? description = null, bool isDefault = false)
    {
        _options.Add(new CheckboxOption
        {
            Label = label,
            Value = value,
            Description = description,
            Default = isDefault
        });
        return this;
    }

    /// <summary>
    /// Adds an option using a CheckboxOptionBuilder.
    /// </summary>
    /// <param name="configure">Action to configure the option.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder AddOption(Action<CheckboxOptionBuilder> configure)
    {
        var builder = new CheckboxOptionBuilder();
        configure(builder);
        _options.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Sets whether the checkbox group is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }

    /// <summary>
    /// Sets the minimum number of items that must be selected.
    /// </summary>
    /// <param name="minValues">Minimum values (0-25).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder WithMinValues(int minValues)
    {
        _minValues = minValues;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of items that can be selected.
    /// </summary>
    /// <param name="maxValues">Maximum values (1-25).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxGroupBuilder WithMaxValues(int maxValues)
    {
        _maxValues = maxValues;
        return this;
    }

    /// <summary>
    /// Builds the CheckboxGroup component.
    /// </summary>
    /// <returns>The CheckboxGroup component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public CheckboxGroup Build()
    {
        if (string.IsNullOrEmpty(_customId))
        {
            throw new ValidationException(
                "CheckboxGroup custom ID is required.",
                nameof(_customId),
                null
            );
        }

        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "CheckboxGroup label is required.",
                nameof(_label),
                null
            );
        }

        if (_customId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                $"CheckboxGroup custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }

        if (_label.Length > DiscordLimits.MaxTextInputLabelLength)
        {
            throw new ValidationException(
                $"CheckboxGroup label must not exceed {DiscordLimits.MaxTextInputLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        if (_options.Count > DiscordLimits.MaxCheckboxGroupOptions)
        {
            throw new ValidationException(
                $"CheckboxGroup can have at most {DiscordLimits.MaxCheckboxGroupOptions} options.",
                nameof(_options),
                _options.Count
            );
        }

        if (_options.Count == 0)
        {
            throw new ValidationException(
                "CheckboxGroup must have at least one option.",
                nameof(_options),
                null
            );
        }

        if (_minValues.HasValue && _maxValues.HasValue && _minValues.Value > _maxValues.Value)
        {
            throw new ValidationException(
                "CheckboxGroup minimum values cannot be greater than maximum values.",
                nameof(_minValues),
                _minValues.Value
            );
        }

        return new CheckboxGroup
        {
            CustomId = _customId,
            Options = new List<CheckboxOption>(_options),
            Label = _label,
            Required = _required,
            MinValues = _minValues,
            MaxValues = _maxValues
        };
    }
}

/// <summary>
/// Builder for CheckboxOption.
/// </summary>
public class CheckboxOptionBuilder
{
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string? _description;
    private Emoji? _emoji;
    private bool _default = false;

    /// <summary>
    /// Sets the option label.
    /// </summary>
    /// <param name="label">The label (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxOptionBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Sets the option value.
    /// </summary>
    /// <param name="value">The value (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxOptionBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Sets the optional description.
    /// </summary>
    /// <param name="description">The description (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxOptionBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the emoji.
    /// </summary>
    /// <param name="emoji">The emoji.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxOptionBuilder WithEmoji(Emoji emoji)
    {
        _emoji = emoji;
        return this;
    }

    /// <summary>
    /// Sets whether this option is checked by default.
    /// </summary>
    /// <param name="isDefault">Whether default.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxOptionBuilder WithDefault(bool isDefault = true)
    {
        _default = isDefault;
        return this;
    }

    /// <summary>
    /// Builds the CheckboxOption.
    /// </summary>
    /// <returns>The CheckboxOption.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public CheckboxOption Build()
    {
        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "CheckboxOption label is required.",
                nameof(_label),
                null
            );
        }

        if (string.IsNullOrEmpty(_value))
        {
            throw new ValidationException(
                "CheckboxOption value is required.",
                nameof(_value),
                null
            );
        }

        if (_label.Length > DiscordLimits.MaxCheckboxGroupOptionLabelLength)
        {
            throw new ValidationException(
                $"CheckboxOption label must not exceed {DiscordLimits.MaxCheckboxGroupOptionLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        if (_value.Length > DiscordLimits.MaxCheckboxGroupOptionValueLength)
        {
            throw new ValidationException(
                $"CheckboxOption value must not exceed {DiscordLimits.MaxCheckboxGroupOptionValueLength} characters.",
                nameof(_value),
                _value.Length
            );
        }

        if (_description != null && _description.Length > DiscordLimits.MaxCheckboxGroupOptionDescriptionLength)
        {
            throw new ValidationException(
                $"CheckboxOption description must not exceed {DiscordLimits.MaxCheckboxGroupOptionDescriptionLength} characters.",
                nameof(_description),
                _description.Length
            );
        }

        return new CheckboxOption
        {
            Label = _label,
            Value = _value,
            Description = _description,
            Emoji = _emoji,
            Default = _default
        };
    }
}

/// <summary>
/// Builder for Checkbox components (Components v2).
/// </summary>
public class CheckboxBuilder
{
    private string _customId = string.Empty;
    private string _label = string.Empty;
    private bool? _defaultValue = false;
    private bool? _required = false;

    /// <summary>
    /// Creates a new CheckboxBuilder.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <param name="label">The label (max 80 characters).</param>
    public CheckboxBuilder(string customId, string label)
    {
        _customId = customId;
        _label = label;
    }

    /// <summary>
    /// Sets the custom ID.
    /// </summary>
    /// <param name="customId">The custom ID (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxBuilder WithCustomId(string customId)
    {
        _customId = customId;
        return this;
    }

    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">The label (max 80 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxBuilder WithLabel(string label)
    {
        _label = label;
        return this;
    }

    /// <summary>
    /// Sets whether the checkbox is checked by default.
    /// </summary>
    /// <param name="defaultValue">Whether checked by default.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxBuilder WithDefaultValue(bool defaultValue = true)
    {
        _defaultValue = defaultValue;
        return this;
    }

    /// <summary>
    /// Sets whether the checkbox is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public CheckboxBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }

    /// <summary>
    /// Builds the Checkbox.
    /// </summary>
    /// <returns>The Checkbox component.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public Checkbox Build()
    {
        if (string.IsNullOrEmpty(_customId))
        {
            throw new ValidationException(
                "Checkbox custom ID is required.",
                nameof(_customId),
                null
            );
        }

        if (string.IsNullOrEmpty(_label))
        {
            throw new ValidationException(
                "Checkbox label is required.",
                nameof(_label),
                null
            );
        }

        if (_customId.Length > DiscordLimits.MaxTextInputCustomIdLength)
        {
            throw new ValidationException(
                $"Checkbox custom ID must not exceed {DiscordLimits.MaxTextInputCustomIdLength} characters.",
                nameof(_customId),
                _customId.Length
            );
        }

        if (_label.Length > DiscordLimits.MaxCheckboxLabelLength)
        {
            throw new ValidationException(
                $"Checkbox label must not exceed {DiscordLimits.MaxCheckboxLabelLength} characters.",
                nameof(_label),
                _label.Length
            );
        }

        return new Checkbox
        {
            CustomId = _customId,
            Label = _label,
            DefaultValue = _defaultValue,
            Required = _required
        };
    }
}
