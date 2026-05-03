#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;
using PawSharp.Core.Validation;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Builders;

/// <summary>
/// Fluent builder for constructing Discord application commands with validation.
/// </summary>
/// <example>
/// <code>
/// var command = new CommandBuilder()
///     .WithType(ApplicationCommandType.ChatInput)
///     .WithName("greet")
///     .WithDescription("Greets a user")
///     .AddOption(opt => opt
///         .WithType(ApplicationCommandOptionType.String)
///         .WithName("message")
///         .WithDescription("The message to send")
///         .WithRequired(true))
///     .Build();
/// </code>
/// </example>
public class CommandBuilder
{
    private ApplicationCommandType _type = ApplicationCommandType.ChatInput;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Permissions? _defaultMemberPermissions;
    private bool? _dmPermission;
    private bool? _nsfw;
    private List<int>? _integrationTypes;
    private List<int>? _contexts;
    private Dictionary<string, string>? _nameLocalizations;
    private Dictionary<string, string>? _descriptionLocalizations;
    private readonly List<ApplicationCommandOption> _options = new();
    
    /// <summary>
    /// Creates a new CommandBuilder.
    /// </summary>
    public CommandBuilder() { }
    
    /// <summary>
    /// Sets the command type.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithType(ApplicationCommandType type)
    {
        _type = type;
        return this;
    }
    
    /// <summary>
    /// Sets the command name.
    /// </summary>
    /// <param name="name">The name (1-32 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    /// <summary>
    /// Sets the command description.
    /// </summary>
    /// <param name="description">The description (1-100 characters for ChatInput, empty for User/Message).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    /// <summary>
    /// Sets the default member permissions.
    /// </summary>
    /// <param name="permissions">The permissions bitfield.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithDefaultMemberPermissions(Permissions permissions)
    {
        _defaultMemberPermissions = permissions;
        return this;
    }
    
    /// <summary>
    /// Sets whether the command is available in DMs.
    /// </summary>
    /// <param name="dmPermission">Whether available in DMs.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithDmPermission(bool dmPermission)
    {
        _dmPermission = dmPermission;
        return this;
    }
    
    /// <summary>
    /// Sets whether the command is age-restricted.
    /// </summary>
    /// <param name="nsfw">Whether age-restricted.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithNsfw(bool nsfw)
    {
        _nsfw = nsfw;
        return this;
    }
    
    /// <summary>
    /// Sets the installation context types.
    /// </summary>
    /// <param name="integrationTypes">List of integration type integers.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithIntegrationTypes(List<int> integrationTypes)
    {
        _integrationTypes = integrationTypes;
        return this;
    }
    
    /// <summary>
    /// Sets the interaction context types.
    /// </summary>
    /// <param name="contexts">List of context type integers.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithContexts(List<int> contexts)
    {
        _contexts = contexts;
        return this;
    }
    
    /// <summary>
    /// Sets the name localizations.
    /// </summary>
    /// <param name="localizations">Dictionary of locale to localized name.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithNameLocalizations(Dictionary<string, string> localizations)
    {
        _nameLocalizations = localizations;
        return this;
    }
    
    /// <summary>
    /// Sets the description localizations.
    /// </summary>
    /// <param name="localizations">Dictionary of locale to localized description.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder WithDescriptionLocalizations(Dictionary<string, string> localizations)
    {
        _descriptionLocalizations = localizations;
        return this;
    }
    
    /// <summary>
    /// Adds an option to the command.
    /// </summary>
    /// <param name="configure">Action to configure the option.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandBuilder AddOption(Action<CommandOptionBuilder> configure)
    {
        var builder = new CommandOptionBuilder();
        configure(builder);
        _options.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Builds the ApplicationCommand.
    /// </summary>
    /// <returns>The ApplicationCommand.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public ApplicationCommand Build()
    {
        if (_name.Length < DiscordLimits.MinCommandNameLength || _name.Length > DiscordLimits.MaxCommandNameLength)
        {
            throw new ValidationException(
                $"Command name must be between {DiscordLimits.MinCommandNameLength} and {DiscordLimits.MaxCommandNameLength} characters.",
                nameof(_name),
                _name.Length
            );
        }
        
        // Description is required for ChatInput commands
        if (_type == ApplicationCommandType.ChatInput)
        {
            if (string.IsNullOrEmpty(_description) || _description.Length > DiscordLimits.MaxCommandDescriptionLength)
            {
                throw new ValidationException(
                    $"ChatInput command description must be between 1 and {DiscordLimits.MaxCommandDescriptionLength} characters.",
                nameof(_description),
                _description?.Length ?? 0
                );
            }
        }
        
        if (_options.Count > DiscordLimits.MaxCommandOptions)
        {
            throw new ValidationException(
                $"Command must not have more than {DiscordLimits.MaxCommandOptions} options.",
                nameof(_options),
                _options.Count
            );
        }
        
        return new ApplicationCommand
        {
            Type = _type,
            Name = _name,
            Description = _description,
            DefaultMemberPermissions = _defaultMemberPermissions,
            DmPermission = _dmPermission,
            Nsfw = _nsfw,
            IntegrationTypes = _integrationTypes,
            Contexts = _contexts,
            NameLocalizations = _nameLocalizations,
            DescriptionLocalizations = _descriptionLocalizations,
            Options = _options.Count > 0 ? new List<ApplicationCommandOption>(_options) : null
        };
    }
}

/// <summary>
/// Builder for ApplicationCommandOption.
/// </summary>
public class CommandOptionBuilder
{
    private ApplicationCommandOptionType _type;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private bool? _required;
    private List<int>? _channelTypes;
    private object? _minValue;
    private object? _maxValue;
    private int? _minLength;
    private int? _maxLength;
    private bool? _autocomplete;
    private Dictionary<string, string>? _nameLocalizations;
    private Dictionary<string, string>? _descriptionLocalizations;
    private readonly List<ApplicationCommandOptionChoice> _choices = new();
    private readonly List<ApplicationCommandOption> _nestedOptions = new();
    
    /// <summary>
    /// Sets the option type.
    /// </summary>
    /// <param name="type">The option type.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithType(ApplicationCommandOptionType type)
    {
        _type = type;
        return this;
    }
    
    /// <summary>
    /// Sets the option name.
    /// </summary>
    /// <param name="name">The name (1-32 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    /// <summary>
    /// Sets the option description.
    /// </summary>
    /// <param name="description">The description (1-100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    /// <summary>
    /// Sets whether the option is required.
    /// </summary>
    /// <param name="required">Whether required.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithRequired(bool required = true)
    {
        _required = required;
        return this;
    }
    
    /// <summary>
    /// Sets the channel types for channel options.
    /// </summary>
    /// <param name="channelTypes">List of channel type integers.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithChannelTypes(List<int> channelTypes)
    {
        _channelTypes = channelTypes;
        return this;
    }
    
    /// <summary>
    /// Sets the minimum value for numeric options.
    /// </summary>
    /// <param name="minValue">The minimum value.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithMinValue(object minValue)
    {
        _minValue = minValue;
        return this;
    }
    
    /// <summary>
    /// Sets the maximum value for numeric options.
    /// </summary>
    /// <param name="maxValue">The maximum value.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithMaxValue(object maxValue)
    {
        _maxValue = maxValue;
        return this;
    }
    
    /// <summary>
    /// Sets the minimum length for string options.
    /// </summary>
    /// <param name="minLength">The minimum length (0-6000).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithMinLength(int minLength)
    {
        _minLength = minLength;
        return this;
    }
    
    /// <summary>
    /// Sets the maximum length for string options.
    /// </summary>
    /// <param name="maxLength">The maximum length (1-6000).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }
    
    /// <summary>
    /// Sets whether autocomplete is enabled.
    /// </summary>
    /// <param name="autocomplete">Whether autocomplete is enabled.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithAutocomplete(bool autocomplete = true)
    {
        _autocomplete = autocomplete;
        return this;
    }
    
    /// <summary>
    /// Sets the name localizations.
    /// </summary>
    /// <param name="localizations">Dictionary of locale to localized name.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithNameLocalizations(Dictionary<string, string> localizations)
    {
        _nameLocalizations = localizations;
        return this;
    }
    
    /// <summary>
    /// Sets the description localizations.
    /// </summary>
    /// <param name="localizations">Dictionary of locale to localized description.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder WithDescriptionLocalizations(Dictionary<string, string> localizations)
    {
        _descriptionLocalizations = localizations;
        return this;
    }
    
    /// <summary>
    /// Adds a choice to the option.
    /// </summary>
    /// <param name="configure">Action to configure the choice.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder AddChoice(Action<CommandOptionChoiceBuilder> configure)
    {
        var builder = new CommandOptionChoiceBuilder();
        configure(builder);
        _choices.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Adds a nested option (for subcommands/subcommand groups).
    /// </summary>
    /// <param name="configure">Action to configure the nested option.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionBuilder AddOption(Action<CommandOptionBuilder> configure)
    {
        var builder = new CommandOptionBuilder();
        configure(builder);
        _nestedOptions.Add(builder.Build());
        return this;
    }
    
    /// <summary>
    /// Builds the ApplicationCommandOption.
    /// </summary>
    /// <returns>The ApplicationCommandOption.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public ApplicationCommandOption Build()
    {
        if (_name.Length < DiscordLimits.MinCommandNameLength || _name.Length > DiscordLimits.MaxCommandOptionNameLength)
        {
            throw new ValidationException(
                $"Option name must be between {DiscordLimits.MinCommandNameLength} and {DiscordLimits.MaxCommandOptionNameLength} characters.",
                nameof(_name),
                _name.Length
            );
        }
        
        if (_description.Length > DiscordLimits.MaxCommandOptionDescriptionLength)
        {
            throw new ValidationException(
                $"Option description must not exceed {DiscordLimits.MaxCommandOptionDescriptionLength} characters.",
                nameof(_description),
                _description.Length
            );
        }
        
        if (_choices.Count > DiscordLimits.MaxCommandOptionChoices)
        {
            throw new ValidationException(
                $"Option must not have more than {DiscordLimits.MaxCommandOptionChoices} choices.",
                nameof(_choices),
                _choices.Count
            );
        }
        
        if (_minLength.HasValue && _minLength.Value > DiscordLimits.MaxCommandOptionMinStringLength)
        {
            throw new ValidationException(
                $"Option minimum length must not exceed {DiscordLimits.MaxCommandOptionMinStringLength}.",
                nameof(_minLength),
                _minLength.Value
            );
        }
        
        if (_maxLength.HasValue && _maxLength.Value > DiscordLimits.MaxCommandOptionMaxStringLength)
        {
            throw new ValidationException(
                $"Option maximum length must not exceed {DiscordLimits.MaxCommandOptionMaxStringLength}.",
                nameof(_maxLength),
                _maxLength.Value
            );
        }
        
        return new ApplicationCommandOption
        {
            Type = _type,
            Name = _name,
            Description = _description,
            Required = _required,
            ChannelTypes = _channelTypes,
            MinValue = _minValue,
            MaxValue = _maxValue,
            MinLength = _minLength,
            MaxLength = _maxLength,
            Autocomplete = _autocomplete,
            NameLocalizations = _nameLocalizations,
            DescriptionLocalizations = _descriptionLocalizations,
            Choices = _choices.Count > 0 ? new List<ApplicationCommandOptionChoice>(_choices) : null,
            Options = _nestedOptions.Count > 0 ? new List<ApplicationCommandOption>(_nestedOptions) : null
        };
    }
}

/// <summary>
/// Builder for ApplicationCommandOptionChoice.
/// </summary>
public class CommandOptionChoiceBuilder
{
    private string _name = string.Empty;
    private object _value = string.Empty;
    private Dictionary<string, string>? _nameLocalizations;
    
    /// <summary>
    /// Sets the choice name.
    /// </summary>
    /// <param name="name">The name (max 100 characters).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionChoiceBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    /// <summary>
    /// Sets the choice value.
    /// </summary>
    /// <param name="value">The value (string, int, or double).</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionChoiceBuilder WithValue(object value)
    {
        _value = value;
        return this;
    }
    
    /// <summary>
    /// Sets the name localizations.
    /// </summary>
    /// <param name="localizations">Dictionary of locale to localized name.</param>
    /// <returns>The builder for method chaining.</returns>
    public CommandOptionChoiceBuilder WithNameLocalizations(Dictionary<string, string> localizations)
    {
        _nameLocalizations = localizations;
        return this;
    }
    
    /// <summary>
    /// Builds the ApplicationCommandOptionChoice.
    /// </summary>
    /// <returns>The ApplicationCommandOptionChoice.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public ApplicationCommandOptionChoice Build()
    {
        if (_name.Length > DiscordLimits.MaxCommandOptionChoiceNameLength)
        {
            throw new ValidationException(
                $"Choice name must not exceed {DiscordLimits.MaxCommandOptionChoiceNameLength} characters.",
                nameof(_name),
                _name.Length
            );
        }
        
        if (_value is string stringValue && stringValue.Length > DiscordLimits.MaxCommandOptionChoiceValueLength)
        {
            throw new ValidationException(
                $"Choice value must not exceed {DiscordLimits.MaxCommandOptionChoiceValueLength} characters.",
                nameof(_value),
                stringValue.Length
            );
        }
        
        return new ApplicationCommandOptionChoice
        {
            Name = _name,
            Value = _value,
            NameLocalizations = _nameLocalizations
        };
    }
}
