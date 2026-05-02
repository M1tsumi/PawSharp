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
    /// Builds the Section.
    /// </summary>
    /// <returns>The Section component.</returns>
    public Section Build()
    {
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
    /// Builds the Separator.
    /// </summary>
    /// <returns>The Separator component.</returns>
    public Separator Build()
    {
        return new Separator
        {
            Spacing = _spacing
        };
    }
}

/// <summary>
/// Builder for Container components (Components v2).
/// </summary>
public class ContainerBuilder
{
    private List<MessageComponent>? _components;
    private int? _accentColor;
    
    /// <summary>
    /// Adds a component to the container.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public ContainerBuilder AddComponent(MessageComponent component)
    {
        _components ??= new List<MessageComponent>();
        _components.Add(component);
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
    /// Builds the Container.
    /// </summary>
    /// <returns>The Container component.</returns>
    public Container Build()
    {
        return new Container
        {
            Components = _components,
            AccentColor = _accentColor
        };
    }
}
