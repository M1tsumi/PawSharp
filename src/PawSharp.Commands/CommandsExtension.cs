#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PawSharp.API.Models;
using PawSharp.Client;
using PawSharp.Commands.Attributes;
using PawSharp.Commands.Conversion;
using PawSharp.Commands.Execution;
using PawSharp.Commands.Middleware;
using PawSharp.Commands.Permissions;
using PawSharp.Commands.Preconditions;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;

namespace PawSharp.Commands;

/// <summary>
/// Represents a command context.
/// </summary>
public class CommandContext
{
    /// <summary>
    /// Gets the Discord client.
    /// </summary>
    public DiscordClient Client { get; }

    /// <summary>
    /// Gets the message that triggered the command.
    /// </summary>
    public Message Message { get; }

    /// <summary>
    /// Gets the channel where the command was executed.
    /// </summary>
    public ulong ChannelId => Message.ChannelId;

    /// <summary>
    /// Gets the user who executed the command.
    /// </summary>
    public User User => Message.Author!;

    /// <summary>
    /// Gets the guild where the command was executed, if applicable.
    /// </summary>
    public ulong? GuildId => Message.GuildId;

    /// <summary>
    /// Gets the command prefix used.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Gets the command name.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Gets the command arguments.
    /// </summary>
    public string[] Arguments { get; }

    /// <summary>
    /// Gets the raw argument string.
    /// </summary>
    public string RawArguments { get; }

    /// <summary>
    /// Gets the guild member who invoked the command, or <see langword="null"/> for DM invocations.
    /// Populated from the <c>member</c> field of the gateway <c>MESSAGE_CREATE</c> event.
    /// Use <see cref="Member"/> in <see cref="IPrecondition"/> checks to access computed permissions.
    /// </summary>
    public GuildMember? Member { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="message">The message.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="arguments">The command arguments.</param>
    /// <param name="rawArguments">The raw arguments.</param>
    /// <param name="member">The guild member who triggered the command, if in a guild.</param>
    public CommandContext(
        DiscordClient client,
        Message message,
        string prefix,
        string commandName,
        string[] arguments,
        string rawArguments,
        GuildMember? member = null)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        RawArguments = rawArguments ?? throw new ArgumentNullException(nameof(rawArguments));
        Member = member;
    }

    /// <summary>
    /// Responds to the command.
    /// </summary>
    /// <param name="content">The response content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RespondAsync(string content)
    {
        await Client.Rest.CreateMessageAsync(ChannelId, new CreateMessageRequest { Content = content });
    }

    /// <summary>
    /// Responds to the command with an embed.
    /// </summary>
    /// <param name="embed">The embed to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RespondAsync(Embed embed)
    {
        await Client.Rest.CreateMessageAsync(ChannelId, new CreateMessageRequest { Embeds = new List<Embed> { embed } });
    }

    /// <summary>
    /// Replies to the triggering message (creates a Discord message reply thread).
    /// </summary>
    /// <param name="content">The reply content.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReplyAsync(string content)
    {
        await Client.Rest.CreateMessageAsync(ChannelId, new CreateMessageRequest
        {
            Content          = content,
            MessageReference = new MessageReference { MessageId = Message.Id, ChannelId = ChannelId }
        });
    }

    /// <summary>
    /// Replies to the triggering message with an embed.
    /// </summary>
    /// <param name="embed">The embed to include in the reply.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReplyAsync(Embed embed)
    {
        await Client.Rest.CreateMessageAsync(ChannelId, new CreateMessageRequest
        {
            Embeds           = new List<Embed> { embed },
            MessageReference = new MessageReference { MessageId = Message.Id, ChannelId = ChannelId }
        });
    }
}

/// <summary>
/// Base class for command modules.
/// </summary>
public abstract class BaseCommandModule
{
    /// <summary>
    /// Called during module registration for async initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called before command execution.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task BeforeExecutionAsync(CommandContext ctx)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after command execution.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task AfterExecutionAsync(CommandContext ctx)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Attribute for command methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    /// <summary>
    /// Gets the command name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAttribute"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    public CommandAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

/// <summary>
/// Attribute for command aliases.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AliasesAttribute : Attribute
{
    /// <summary>
    /// Gets the command aliases.
    /// </summary>
    public string[] Aliases { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasesAttribute"/> class.
    /// </summary>
    /// <param name="aliases">The command aliases.</param>
    public AliasesAttribute(params string[] aliases)
    {
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
    }
}

/// <summary>
/// Attribute for command descriptions.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DescriptionAttribute : Attribute
{
    /// <summary>
    /// Gets the command description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionAttribute"/> class.
    /// </summary>
    /// <param name="description">The command description.</param>
    public DescriptionAttribute(string description)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Represents a command.
/// </summary>
public class Command
{
    /// <summary>
    /// Gets the command name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the command aliases.
    /// </summary>
    public string[] Aliases { get; }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the command method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets the command module.
    /// </summary>
    public BaseCommandModule Module { get; }

    /// <summary>
    /// Gets the precondition checks for this command, collected once at registration.
    /// Stored here so attribute instances (and their state, e.g. <see cref="CooldownAttribute._buckets"/>)
    /// are reused across invocations rather than being reconstructed by each
    /// <c>GetCustomAttributes</c> call.
    /// </summary>
    public IReadOnlyList<IPrecondition> Preconditions { get; }

    /// <summary>
    /// Gets the compiled delegate for this command method.
    /// </summary>
    public Func<BaseCommandModule, object?[], Task>? Delegate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="aliases">The command aliases.</param>
    /// <param name="description">The command description.</param>
    /// <param name="method">The command method.</param>
    /// <param name="module">The command module.</param>
    /// <param name="preconditions">Pre-collected precondition instances for this command.</param>
    /// <param name="useCompiledDelegates">Whether to use compiled delegates instead of reflection.</param>
    public Command(string name, string[] aliases, string? description, MethodInfo method, BaseCommandModule module, IReadOnlyList<IPrecondition> preconditions, bool useCompiledDelegates = true)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        Description = description;
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Module = module ?? throw new ArgumentNullException(nameof(module));
        Preconditions = preconditions ?? throw new ArgumentNullException(nameof(preconditions));
        
        if (useCompiledDelegates)
        {
            Delegate = CommandDelegateFactory.CreateDelegate(method);
        }
    }
}

/// <summary>
/// Represents information about a registered command.
/// </summary>
public class CommandInfo
{
    /// <summary>
    /// Gets the command name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the command aliases.
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandInfo"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="aliases">The command aliases.</param>
    /// <param name="description">The command description.</param>
    public CommandInfo(string name, string[] aliases, string? description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Aliases = aliases ?? Array.Empty<string>();
        Description = description;
    }
}

/// <summary>
/// Main commands extension.
/// </summary>
public class CommandsExtension
{
    private readonly string _prefix;
    private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<CommandsExtension> _logger;
    private readonly TypeConverterService _typeConverterService;
    private readonly MiddlewarePipeline _middlewarePipeline;
    private readonly IServiceProvider? _serviceProvider;
    private readonly bool _caseSensitive;
    private DiscordClient? _client;

    /// <summary>
    /// Invoked when a command throws an unhandled exception.
    /// Assign a handler here to customise error reporting (e.g. send a user-facing error message).
    /// If no handler is assigned, the exception is logged at <c>Error</c> level and swallowed.
    /// </summary>
    public Func<CommandErrorEventArgs, Task>? CommandErrored { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsExtension"/> class.
    /// </summary>
    /// <param name="prefix">The command prefix.</param>
    /// <param name="logger">Optional logger; defaults to no-op.</param>
    /// <param name="typeConverterService">Optional type converter service.</param>
    /// <param name="middlewarePipeline">Optional middleware pipeline.</param>
    /// <param name="serviceProvider">Optional service provider for dependency injection.</param>
    /// <param name="caseSensitive">Whether commands are case-sensitive.</param>
    public CommandsExtension(
        string prefix = "!",
        ILogger<CommandsExtension>? logger = null,
        TypeConverterService? typeConverterService = null,
        MiddlewarePipeline? middlewarePipeline = null,
        IServiceProvider? serviceProvider = null,
        bool caseSensitive = false)
    {
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        _logger = logger ?? NullLogger<CommandsExtension>.Instance;
        _typeConverterService = typeConverterService ?? new TypeConverterService(null);
        _middlewarePipeline = middlewarePipeline ?? new MiddlewarePipeline();
        _serviceProvider = serviceProvider;
        _caseSensitive = caseSensitive;
        
        if (_caseSensitive)
        {
            _commands = new Dictionary<string, Command>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Registers a command module.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="module">The command module to register.</param>
    public void RegisterModule(DiscordClient client, BaseCommandModule module)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        _client = client;

        if (_client != null && !_commands.Any())
        {
            _client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", OnMessageCreate);
        }

        var type = module.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Use DI to create module if service provider is available
        if (_serviceProvider != null)
        {
            module = (BaseCommandModule)_serviceProvider.GetRequiredService(type);
        }

        foreach (var method in methods)
        {
            var commandAttr = method.GetCustomAttribute<CommandAttribute>();
            if (commandAttr == null)
                continue;

            var aliasesAttr = method.GetCustomAttribute<AliasesAttribute>();
            var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();

            var aliases = aliasesAttr?.Aliases ?? Array.Empty<string>();
            var description = descriptionAttr?.Description;

            // Collect preconditions once at registration so that stateful instances
            // (e.g. CooldownAttribute with its _buckets dictionary) are reused across
            // invocations.  GetCustomAttributes creates NEW attribute objects on every
            // call, so we must NOT call it at execution time for stateful preconditions.
            var preconditions = method.GetCustomAttributes(typeof(IPrecondition), inherit: true)
                .Concat(type.GetCustomAttributes(typeof(IPrecondition), inherit: true))
                .Cast<IPrecondition>()
                .ToList()
                .AsReadOnly();

            var command = new Command(commandAttr.Name, aliases, description, method, module, preconditions);

            _commands[commandAttr.Name] = command;
            foreach (var alias in aliases)
            {
                _commands[alias] = command;
            }
        }
    }

    /// <summary>
    /// Unregisters a command module.
    /// </summary>
    /// <param name="module">The command module to unregister.</param>
    public void UnregisterModule(BaseCommandModule module)
    {
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        var commandsToRemove = _commands.Where(kvp => kvp.Value.Module == module).ToList();
        foreach (var kvp in commandsToRemove)
        {
            _commands.Remove(kvp.Key);
        }
    }

    /// <summary>
    /// Registers a command module asynchronously, allowing for async initialization.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="module">The command module to register.</param>
    public async Task RegisterModuleAsync(DiscordClient client, BaseCommandModule module)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        _client = client;

        if (_client != null && !_commands.Any())
        {
            _client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", OnMessageCreate);
        }

        // Allow async initialization
        await module.InitializeAsync();

        var type = module.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var commandAttr = method.GetCustomAttribute<CommandAttribute>();
            if (commandAttr == null)
                continue;

            var aliasesAttr = method.GetCustomAttribute<AliasesAttribute>();
            var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();

            var aliases = aliasesAttr?.Aliases ?? Array.Empty<string>();
            var description = descriptionAttr?.Description;

            var preconditions = method.GetCustomAttributes(typeof(IPrecondition), inherit: true)
                .Concat(type.GetCustomAttributes(typeof(IPrecondition), inherit: true))
                .Cast<IPrecondition>()
                .ToList()
                .AsReadOnly();

            var command = new Command(commandAttr.Name, aliases, description, method, module, preconditions);

            _commands[commandAttr.Name] = command;
            foreach (var alias in aliases)
            {
                _commands[alias] = command;
            }
        }
    }

    /// <summary>
    /// Gets a list of all registered commands.
    /// </summary>
    /// <returns>A list of registered command information.</returns>
    public IReadOnlyList<CommandInfo> GetRegisteredCommands()
    {
        return _commands.Values
            .Distinct()
            .Select(c => new CommandInfo(c.Name, c.Aliases, c.Description))
            .ToList()
            .AsReadOnly();
    }

    private async Task OnMessageCreate(MessageCreateEvent evt)
    {
        if (evt.Author?.Bot == true || string.IsNullOrEmpty(evt.Content))
            return;

        // Use ArgumentParser for advanced parsing with quote and escape support
        var (commandName, rawArgs) = ArgumentParser.ExtractCommand(evt.Content, _prefix);
        if (string.IsNullOrEmpty(commandName))
            return;

        var args = ArgumentParser.ParseArguments(rawArgs).ToArray();

        if (!_commands.TryGetValue(commandName, out var command))
            return;

        // Use ToMessage() so all fields (including GuildId) are correctly propagated.
        var message = evt.ToMessage();

        // _client is guaranteed non-null here: OnMessageCreate is only registered after _client is set
        var ctx = new CommandContext(_client!, message, _prefix, commandName, args, rawArgs, evt.Member);

        // ── Middleware Pipeline ─────────────────────────────────────────────────
        try
        {
            await _middlewarePipeline.ExecuteAsync(ctx, async () =>
            {
                // ── Precondition checks ─────────────────────────────────────────────────
                foreach (var check in command.Preconditions)
                {
                    var result = await check.CheckAsync(ctx);
                    if (!result.IsSuccess)
                    {
                        _logger.LogDebug(
                            "Precondition {Check} blocked command {Command} for user {UserId}: {Reason}",
                            check.GetType().Name, commandName, evt.Author?.Id, result.ErrorMessage);

                        // Surface the failure through CommandErrored so callers can respond to the user
                        if (CommandErrored != null)
                        {
                            try
                            {
                                await CommandErrored(new CommandErrorEventArgs(
                                    ctx, new PreconditionFailedException(result.ErrorMessage ?? string.Empty)));
                            }
                            catch (Exception handlerEx)
                            {
                                _logger.LogError(handlerEx,
                                    "CommandErrored handler threw while reporting precondition failure for {Command}",
                                    commandName);
                            }
                        }
                        return;
                    }
                }

                // ── Command Execution with Type Conversion ───────────────────────────────
                await command.Module.BeforeExecutionAsync(ctx);
                
                var parameters = command.Method.GetParameters();
                var argsArray = new object?[parameters.Length];
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param.ParameterType == typeof(CommandContext))
                    {
                        argsArray[i] = ctx;
                        continue;
                    }

                    // Handle [Remaining] attribute
                    var remainingAttr = param.GetCustomAttribute<RemainingAttribute>();
                    if (remainingAttr != null)
                    {
                        // Capture all remaining arguments as a string
                        var remainingArgs = string.Join(" ", args.Skip(i));
                        argsArray[i] = remainingArgs;
                        continue;
                    }

                    // Handle [Optional] attribute
                    var optionalAttr = param.GetCustomAttribute<OptionalAttribute>();
                    if (optionalAttr != null && i >= args.Length)
                    {
                        argsArray[i] = optionalAttr.DefaultValue ?? GetDefault(param.ParameterType);
                        continue;
                    }

                    // Type conversion for regular parameters
                    if (i < args.Length)
                    {
                        var argValue = args[i];
                        var paramType = param.ParameterType;
                        
                        if (paramType == typeof(string))
                        {
                            argsArray[i] = argValue;
                        }
                        else
                        {
                            // Use type converter service
                            var conversionResult = await _typeConverterService.ConvertAsync(paramType, argValue, ctx);
                            if (conversionResult != null)
                            {
                                argsArray[i] = conversionResult;
                            }
                            else
                            {
                                // Type conversion failed
                                if (CommandErrored != null)
                                {
                                    await CommandErrored(new CommandErrorEventArgs(
                                        ctx, new ArgumentException("Type conversion failed")));
                                }
                                return;
                            }
                        }
                    }
                    else
                    {
                        argsArray[i] = GetDefault(param.ParameterType);
                    }
                }

                // Use compiled delegate if available, otherwise fall back to reflection
                if (command.Delegate != null)
                {
                    await command.Delegate(command.Module, argsArray);
                }
                else
                {
                    await (Task)command.Method.Invoke(command.Module, argsArray)!;
                }
                await command.Module.AfterExecutionAsync(ctx);
            });
        }
        catch (Exception ex)
        {
            // Unwrap TargetInvocationException so callers see the real exception
            var unwrapped = ex is TargetInvocationException tie && tie.InnerException != null
                ? tie.InnerException
                : ex;

            _logger.LogError(unwrapped, "Error executing command {Command} for user {UserId}",
                commandName, evt.Author?.Id);

            if (CommandErrored != null)
            {
                try { await CommandErrored(new CommandErrorEventArgs(ctx, unwrapped)); }
                catch (Exception handlerEx)
                {
                    _logger.LogError(handlerEx, "CommandErrored handler itself threw for command {Command}", commandName);
                }
            }
        }
    }

    private static object? GetDefault([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;

    // ── Slash command auto-registration ──────────────────────────────────────

    /// <summary>
    /// Scans a module for <see cref="SlashCommandAttribute"/>-decorated methods, registers each
    /// discovered command with the Discord API, and wires the interaction handlers automatically.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="module">The command module to scan.</param>
    /// <param name="applicationId">The bot application ID used to register commands with Discord.</param>
    /// <param name="guildId">
    /// When non-null, commands are registered as guild-scoped (instant propagation, ideal during
    /// development). Pass <see langword="null"/> to register as global commands (may take up to
    /// one hour to propagate to all clients).
    /// </param>
    public async Task RegisterSlashModuleAsync(
        DiscordClient client,
        BaseCommandModule module,
        ulong applicationId,
        ulong? guildId = null)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (module == null) throw new ArgumentNullException(nameof(module));

        var type = module.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var slashAttr = method.GetCustomAttribute<SlashCommandAttribute>();
            if (slashAttr == null) continue;

            // Build the option list from every parameter that is NOT InteractionCreateEvent
            var parameters = method.GetParameters();
            var options = new List<ApplicationCommandOption>();

            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(InteractionCreateEvent)) continue;

                var optAttr = param.GetCustomAttribute<SlashOptionAttribute>();
                var optName = optAttr?.Name ?? param.Name ?? "option";
                var optDesc = optAttr?.Description ?? "No description provided.";
                var required = optAttr?.Required ?? !IsOptionalType(param.ParameterType);

                var option = new ApplicationCommandOption
                {
                    Name = optName,
                    Description = optDesc,
                    Required = required,
                    Type = MapTypeToOptionType(param.ParameterType),
                };

                // Add choices if present
                var choiceAttrs = param.GetCustomAttributes<SlashChoiceAttribute>();
                if (choiceAttrs.Any())
                {
                    option.Choices = choiceAttrs.Select(c => new ApplicationCommandOptionChoice
                    {
                        Name = c.Name,
                        Value = c.Value
                    }).ToList();
                }

                // Add min/max value for numeric types
                var minValAttr = param.GetCustomAttribute<SlashMinValueAttribute>();
                if (minValAttr != null)
                {
                    option.MinValue = minValAttr.MinValue;
                }

                var maxValAttr = param.GetCustomAttribute<SlashMaxValueAttribute>();
                if (maxValAttr != null)
                {
                    option.MaxValue = maxValAttr.MaxValue;
                }

                // Add min/max length for string types
                var minLenAttr = param.GetCustomAttribute<SlashMinLengthAttribute>();
                if (minLenAttr != null)
                {
                    option.MinLength = minLenAttr.MinLength;
                }

                var maxLenAttr = param.GetCustomAttribute<SlashMaxLengthAttribute>();
                if (maxLenAttr != null)
                {
                    option.MaxLength = maxLenAttr.MaxLength;
                }

                // Add channel types for channel options
                var channelTypesAttr = param.GetCustomAttribute<SlashChannelTypesAttribute>();
                if (channelTypesAttr != null)
                {
                    option.ChannelTypes = channelTypesAttr.ChannelTypes.ToList();
                }

                // Add autocomplete flag
                var autocompleteAttr = param.GetCustomAttribute<SlashAutocompleteAttribute>();
                if (autocompleteAttr != null)
                {
                    option.Autocomplete = true;
                }

                options.Add(option);
            }

            var request = new CreateApplicationCommandRequest
            {
                Name        = slashAttr.Name,
                Description = slashAttr.Description,
                Type        = 1, // CHAT_INPUT
                Options     = options.Count > 0 ? options : null,
            };

            // Add NSFW flag
            var nsfwAttr = method.GetCustomAttribute<SlashNsfwAttribute>();
            if (nsfwAttr != null)
            {
                request.Nsfw = true;
            }

            // Add DM permission
            var dmPermAttr = method.GetCustomAttribute<SlashDmPermissionAttribute>();
            if (dmPermAttr != null)
            {
                request.DmPermission = dmPermAttr.AllowDm;
            }

            // Add default member permissions
            var defaultPermAttr = method.GetCustomAttribute<SlashDefaultPermissionAttribute>();
            if (defaultPermAttr != null)
            {
                request.DefaultPermission = defaultPermAttr.Permission;
            }

            // Add localized names
            var nameLocalizations = method.GetCustomAttributes<SlashLocalizedNameAttribute>();
            if (nameLocalizations.Any())
            {
                request.NameLocalizations = nameLocalizations.ToDictionary(l => l.Locale, l => l.Name);
            }

            // Add localized descriptions
            var descLocalizations = method.GetCustomAttributes<SlashLocalizedDescriptionAttribute>();
            if (descLocalizations.Any())
            {
                request.DescriptionLocalizations = descLocalizations.ToDictionary(l => l.Locale, l => l.Description);
            }

            // Register with Discord REST API
            try
            {
                if (guildId.HasValue)
                    await client.Rest.CreateGuildApplicationCommandAsync(applicationId, guildId.Value, request);
                else
                    await client.Rest.CreateGlobalApplicationCommandAsync(applicationId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register slash command /{Name} with Discord", slashAttr.Name);
                continue;
            }

            // Capture loop variables for the async closure
            var capturedMethod    = method;
            var capturedModule    = module;
            var capturedParams    = parameters;
            var capturedSlashAttr = slashAttr;

            // Wire the interaction handler
            client.Interactions.RegisterCommand(slashAttr.Name, async interaction =>
            {
                var args = new object?[capturedParams.Length];
                for (var i = 0; i < capturedParams.Length; i++)
                {
                    var param = capturedParams[i];
                    if (param.ParameterType == typeof(InteractionCreateEvent))
                    {
                        args[i] = interaction;
                        continue;
                    }

                    var optAttr = param.GetCustomAttribute<SlashOptionAttribute>();
                    var optName = optAttr?.Name ?? param.Name ?? "option";
                    args[i] = GetOptionValueForType(interaction, optName, param.ParameterType);
                }

                try
                {
                    var result = capturedMethod.Invoke(capturedModule, args);
                    if (result is Task task) await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing slash command /{Name}", capturedSlashAttr.Name);

                    if (CommandErrored != null)
                    {
                        // Build a minimal CommandContext for the error event (no message — slash context)
                        try
                        {
                            await CommandErrored(new CommandErrorEventArgs(
                                new SlashCommandContext(client, interaction, capturedSlashAttr.Name), ex));
                        }
                        catch (Exception handlerEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in slash command error handler: {handlerEx.Message}");
                        }
                    }
                }
            });

            _logger.LogDebug("Registered slash command /{Name} for application {AppId}", slashAttr.Name, applicationId);
        }
    }

    /// <summary>
    /// Scans every module in <paramref name="modules"/> for <see cref="SlashCommandAttribute"/>-decorated
    /// methods, registers all discovered commands with Discord in a single bulk-overwrite call, then wires
    /// all interaction handlers.  Prefer this over calling <see cref="RegisterSlashModuleAsync"/> per
    /// module on startup — it uses one REST round-trip instead of <em>N</em>, avoiding rate limits.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="modules">The command modules to scan.</param>
    /// <param name="applicationId">The bot application ID.</param>
    /// <param name="guildId">
    /// When non-null, registers commands as guild-scoped (instant propagation, ideal for development).
    /// Pass <see langword="null"/> to register as global commands (up to one hour propagation).
    /// </param>
    public async Task BulkRegisterSlashModulesAsync(
        DiscordClient client,
        IEnumerable<BaseCommandModule> modules,
        ulong applicationId,
        ulong? guildId = null)
    {
        if (client == null)  throw new ArgumentNullException(nameof(client));
        if (modules == null) throw new ArgumentNullException(nameof(modules));

        var requests        = new List<CreateApplicationCommandRequest>();
        var handlerBuilders = new List<(string Name, Func<InteractionCreateEvent, Task> Handler)>();

        foreach (var module in modules)
        {
            if (module == null) continue;
            var type = module.GetType();

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var slashAttr = method.GetCustomAttribute<SlashCommandAttribute>();
                if (slashAttr == null) continue;

                var parameters = method.GetParameters();
                var options    = new List<ApplicationCommandOption>();

                foreach (var param in parameters)
                {
                    if (param.ParameterType == typeof(InteractionCreateEvent)) continue;

                    var optAttr  = param.GetCustomAttribute<SlashOptionAttribute>();
                    var optName  = optAttr?.Name ?? param.Name ?? "option";
                    var optDesc  = optAttr?.Description ?? "No description provided.";
                    var required = optAttr?.Required ?? !IsOptionalType(param.ParameterType);

                    options.Add(new ApplicationCommandOption
                    {
                        Name        = optName,
                        Description = optDesc,
                        Required    = required,
                        Type        = MapTypeToOptionType(param.ParameterType),
                    });
                }

                requests.Add(new CreateApplicationCommandRequest
                {
                    Name        = slashAttr.Name,
                    Description = slashAttr.Description,
                    Type        = 1, // CHAT_INPUT
                    Options     = options.Count > 0 ? options : null,
                });

                // Capture loop variables for the async closure.
                var capturedMethod    = method;
                var capturedModule    = module;
                var capturedParams    = parameters;
                var capturedSlashAttr = slashAttr;

                handlerBuilders.Add((slashAttr.Name, async interaction =>
                {
                    var args = new object?[capturedParams.Length];
                    for (var i = 0; i < capturedParams.Length; i++)
                    {
                        var param = capturedParams[i];
                        if (param.ParameterType == typeof(InteractionCreateEvent))
                        {
                            args[i] = interaction;
                            continue;
                        }
                        var optAttr = param.GetCustomAttribute<SlashOptionAttribute>();
                        var optName = optAttr?.Name ?? param.Name ?? "option";
                        args[i] = GetOptionValueForType(interaction, optName, param.ParameterType);
                    }

                    try
                    {
                        var result = capturedMethod.Invoke(capturedModule, args);
                        if (result is Task task) await task;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing slash command /{Name}", capturedSlashAttr.Name);
                        if (CommandErrored != null)
                        {
                            try
                            {
                                await CommandErrored(new CommandErrorEventArgs(
                                    new SlashCommandContext(client, interaction, capturedSlashAttr.Name), ex));
                            }
                            catch (Exception handlerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in slash command error handler: {handlerEx.Message}");
                            }
                        }
                    }
                }));
            }
        }

        if (requests.Count == 0)
            return;

        // Single bulk-overwrite call — one round-trip regardless of command count.
        try
        {
            if (guildId.HasValue)
                await client.Rest.BulkOverwriteGuildApplicationCommandsAsync(applicationId, guildId.Value, requests);
            else
                await client.Rest.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk slash command registration failed for application {AppId}", applicationId);
            return;
        }

        foreach (var (name, handler) in handlerBuilders)
        {
            client.Interactions.RegisterCommand(name, handler);
            _logger.LogDebug("Wired slash command handler /{Name} for application {AppId}", name, applicationId);
        }

        _logger.LogInformation("Bulk-registered {Count} slash command(s) for application {AppId}",
            requests.Count, applicationId);
    }

    // Maps a C# parameter type to the corresponding Discord ApplicationCommandOptionType.
    private static ApplicationCommandOptionType MapTypeToOptionType(Type type)
    {
        // Unwrap Nullable<T> — Discord doesn't have a nullable concept; Required=false handles optionality.
        var inner = Nullable.GetUnderlyingType(type) ?? type;

        if (inner == typeof(string)) return ApplicationCommandOptionType.String;
        if (inner == typeof(int) || inner == typeof(long)) return ApplicationCommandOptionType.Integer;
        if (inner == typeof(bool)) return ApplicationCommandOptionType.Boolean;
        if (inner == typeof(double) || inner == typeof(float)) return ApplicationCommandOptionType.Number;
        if (inner == typeof(PawSharp.Core.Entities.User)) return ApplicationCommandOptionType.User;
        if (inner == typeof(PawSharp.Core.Entities.Channel)) return ApplicationCommandOptionType.Channel;
        if (inner == typeof(PawSharp.Core.Entities.Role)) return ApplicationCommandOptionType.Role;
        if (inner == typeof(PawSharp.Core.Entities.GuildMember)) return ApplicationCommandOptionType.Mentionable;
        if (inner == typeof(PawSharp.Core.Entities.Attachment)) return ApplicationCommandOptionType.Attachment;

        // Default to string for any type we don't recognise
        return ApplicationCommandOptionType.String;
    }

    // Returns true for types that are inherently "optional" (nullable/reference), used to
    // infer Required when no [SlashOption] attribute specifies it explicitly.
    private static bool IsOptionalType(Type type)
        => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    // Extracts and converts one named option value from the interaction for the given target type.
    private static object? GetOptionValueForType(InteractionCreateEvent interaction, string name, Type targetType)
    {
        var options = interaction.Data?.Options;
        if (options == null) return GetDefault(targetType);

        var option = options.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
        if (option?.Value == null) return GetDefault(targetType);

        var inner = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (option.Value is JsonElement element)
        {
            if (inner == typeof(string))  return element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
            if (inner == typeof(int))     return element.GetInt32();
            if (inner == typeof(long))    return element.GetInt64();
            if (inner == typeof(bool))    return element.GetBoolean();
            if (inner == typeof(double))  return element.GetDouble();
            if (inner == typeof(float))   return (float)element.GetDouble();
        }

        try { return Convert.ChangeType(option.Value, inner, CultureInfo.InvariantCulture); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Type conversion failed for {targetType.Name}: {ex.Message}");
            return GetDefault(targetType);
        }
    }

    // ── Context menu command auto-registration ─────────────────────────────────

    /// <summary>
    /// Scans a module for context menu command attributes and registers them with Discord.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="module">The command module to scan.</param>
    /// <param name="applicationId">The bot application ID.</param>
    /// <param name="guildId">Optional guild ID for guild-scoped registration.</param>
    public async Task RegisterContextMenuModuleAsync(
        DiscordClient client,
        BaseCommandModule module,
        ulong applicationId,
        ulong? guildId = null)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (module == null) throw new ArgumentNullException(nameof(module));

        var type = module.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var userMenuAttr = method.GetCustomAttribute<UserContextMenuAttribute>();
            var messageMenuAttr = method.GetCustomAttribute<MessageContextMenuAttribute>();

            if (userMenuAttr == null && messageMenuAttr == null) continue;

            var isUserMenu = userMenuAttr != null;
            var name = isUserMenu ? userMenuAttr.Name : messageMenuAttr!.Name;
            var typeValue = isUserMenu ? 2 : 3; // 2 = USER, 3 = MESSAGE

            var request = new CreateApplicationCommandRequest
            {
                Name = name,
                Type = typeValue,
            };

            try
            {
                if (guildId.HasValue)
                    await client.Rest.CreateGuildApplicationCommandAsync(applicationId, guildId.Value, request);
                else
                    await client.Rest.CreateGlobalApplicationCommandAsync(applicationId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register context menu command {Name} with Discord", name);
                continue;
            }

            // Wire the interaction handler
            var capturedMethod = method;
            var capturedModule = module;
            var capturedName = name;

            if (isUserMenu)
            {
                client.Interactions.RegisterUserContextMenu(name, async interaction =>
                {
                    try
                    {
                        var result = capturedMethod.Invoke(capturedModule, new object[] { interaction });
                        if (result is Task task) await task;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing context menu command {Name}", capturedName);
                        if (CommandErrored != null)
                        {
                            try
                            {
                                await CommandErrored(new CommandErrorEventArgs(
                                    new SlashCommandContext(client, interaction, capturedName), ex));
                            }
                            catch (Exception handlerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in context menu error handler: {handlerEx.Message}");
                            }
                        }
                    }
                });
            }
            else
            {
                client.Interactions.RegisterMessageContextMenu(name, async interaction =>
                {
                    try
                    {
                        var result = capturedMethod.Invoke(capturedModule, new object[] { interaction });
                        if (result is Task task) await task;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing context menu command {Name}", capturedName);
                        if (CommandErrored != null)
                        {
                            try
                            {
                                await CommandErrored(new CommandErrorEventArgs(
                                    new SlashCommandContext(client, interaction, capturedName), ex));
                            }
                            catch (Exception handlerEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in context menu error handler: {handlerEx.Message}");
                            }
                        }
                    }
                });
            }

            _logger.LogDebug("Registered context menu command {Name} for application {AppId}", name, applicationId);
        }
    }

    /// <summary>
    /// Bulk registers context menu commands from multiple modules in a single API call.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="modules">The command modules to scan.</param>
    /// <param name="applicationId">The bot application ID.</param>
    /// <param name="guildId">Optional guild ID for guild-scoped registration.</param>
    public async Task BulkRegisterContextMenuModulesAsync(
        DiscordClient client,
        IEnumerable<BaseCommandModule> modules,
        ulong applicationId,
        ulong? guildId = null)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (modules == null) throw new ArgumentNullException(nameof(modules));

        var requests = new List<CreateApplicationCommandRequest>();
        var handlerBuilders = new List<(string Name, int Type, Func<InteractionCreateEvent, Task> Handler)>();

        foreach (var module in modules)
        {
            if (module == null) continue;
            var type = module.GetType();

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var userMenuAttr = method.GetCustomAttribute<UserContextMenuAttribute>();
                var messageMenuAttr = method.GetCustomAttribute<MessageContextMenuAttribute>();

                if (userMenuAttr == null && messageMenuAttr == null) continue;

                var isUserMenu = userMenuAttr != null;
                var name = isUserMenu ? userMenuAttr.Name : messageMenuAttr!.Name;
                var typeValue = isUserMenu ? 2 : 3;

                requests.Add(new CreateApplicationCommandRequest
                {
                    Name = name,
                    Type = typeValue,
                });

                var capturedMethod = method;
                var capturedModule = module;
                var capturedName = name;

                if (isUserMenu)
                {
                    handlerBuilders.Add((name, typeValue, async interaction =>
                    {
                        try
                        {
                            var result = capturedMethod.Invoke(capturedModule, new object[] { interaction });
                            if (result is Task task) await task;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing context menu command {Name}", capturedName);
                            if (CommandErrored != null)
                            {
                                try
                                {
                                    await CommandErrored(new CommandErrorEventArgs(
                                        new SlashCommandContext(client, interaction, capturedName), ex));
                                }
                                catch (Exception handlerEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error in context menu error handler: {handlerEx.Message}");
                                }
                            }
                        }
                    }));
                }
                else
                {
                    handlerBuilders.Add((name, typeValue, async interaction =>
                    {
                        try
                        {
                            var result = capturedMethod.Invoke(capturedModule, new object[] { interaction });
                            if (result is Task task) await task;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing context menu command {Name}", capturedName);
                            if (CommandErrored != null)
                            {
                                try
                                {
                                    await CommandErrored(new CommandErrorEventArgs(
                                        new SlashCommandContext(client, interaction, capturedName), ex));
                                }
                                catch (Exception handlerEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error in context menu error handler: {handlerEx.Message}");
                                }
                            }
                        }
                    }));
                }
            }
        }

        if (requests.Count == 0)
            return;

        try
        {
            if (guildId.HasValue)
                await client.Rest.BulkOverwriteGuildApplicationCommandsAsync(applicationId, guildId.Value, requests);
            else
                await client.Rest.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk register context menu commands with Discord");
            return;
        }

        // Wire all handlers
        foreach (var (name, typeValue, handler) in handlerBuilders)
        {
            if (typeValue == 2)
                client.Interactions.RegisterUserContextMenu(name, handler);
            else
                client.Interactions.RegisterMessageContextMenu(name, handler);
        }

        _logger.LogInformation("Bulk-registered {Count} context menu command(s) for application {AppId}",
            requests.Count, applicationId);
    }
}

/// <summary>
/// Event arguments passed to <see cref="CommandsExtension.CommandErrored"/> when a command throws.
/// </summary>
public sealed class CommandErrorEventArgs
{
    /// <summary>The context under which the failing command was invoked.</summary>
    public CommandContext Context { get; }

    /// <summary>The exception that was thrown by the command method.</summary>
    public Exception Exception { get; }

    internal CommandErrorEventArgs(CommandContext context, Exception exception)
    {
        Context   = context   ?? throw new ArgumentNullException(nameof(context));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}

/// <summary>
/// A lightweight <see cref="CommandContext"/> surrogate used for slash command error reporting.
/// Unlike a normal <see cref="CommandContext"/>, there is no backing <see cref="Message"/>; the
/// context wraps the raw <see cref="InteractionCreateEvent"/> instead.
/// </summary>
public sealed class SlashCommandContext : CommandContext
{
    /// <summary>The interaction that triggered this slash command invocation.</summary>
    public InteractionCreateEvent Interaction { get; }

    internal SlashCommandContext(DiscordClient client, InteractionCreateEvent interaction, string commandName)
        : base(
            client,
            new PawSharp.Core.Entities.Message
            {
                Id        = interaction.Id,
                ChannelId = interaction.ChannelId,
                GuildId   = interaction.GuildId,
                Author    = interaction.Member?.User
                            ?? interaction.User
                            ?? new PawSharp.Core.Entities.User
                            {
                                Id = 0,
                                Username = "unknown",
                                Discriminator = "0"
                            },
            },
            prefix: "/",
            commandName: commandName,
            arguments: Array.Empty<string>(),
            rawArguments: string.Empty,
            member: interaction.Member)
    {
        Interaction = interaction;
    }
}

/// <summary>
/// Marks a method in a <see cref="BaseCommandModule"/> as a Discord application (slash) command.
/// Use <see cref="CommandsExtension.RegisterSlashModuleAsync"/> to auto-register all methods
/// decorated with this attribute in a module.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlashCommandAttribute : Attribute
{
    /// <summary>Gets the slash command name (1–32 characters, lowercase, no spaces).</summary>
    public string Name { get; }

    /// <summary>Gets the slash command description shown in the Discord UI (1–100 characters).</summary>
    public string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashCommandAttribute"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="description">The command description.</param>
    public SlashCommandAttribute(string name, string description)
    {
        Name        = name        ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Marks a method parameter as a Discord slash command option.
/// Applied to parameters of methods decorated with <see cref="SlashCommandAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SlashOptionAttribute : Attribute
{
    /// <summary>Gets the option name shown in the Discord UI (1–32 characters).</summary>
    public string Name { get; }

    /// <summary>Gets the option description shown in the Discord UI (1–100 characters).</summary>
    public string Description { get; }

    /// <summary>Gets or sets whether this option is required. Defaults to <see langword="true"/>.</summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlashOptionAttribute"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">The option description.</param>
    public SlashOptionAttribute(string name, string description)
    {
        Name        = name        ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}