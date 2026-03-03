#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Client;
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
    /// Initializes a new instance of the <see cref="CommandContext"/> class.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="message">The message.</param>
    /// <param name="prefix">The command prefix.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="arguments">The command arguments.</param>
    /// <param name="rawArguments">The raw arguments.</param>
    public CommandContext(
        DiscordClient client,
        Message message,
        string prefix,
        string commandName,
        string[] arguments,
        string rawArguments)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        RawArguments = rawArguments ?? throw new ArgumentNullException(nameof(rawArguments));
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
    /// Initializes a new instance of the <see cref="Command"/> class.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="aliases">The command aliases.</param>
    /// <param name="description">The command description.</param>
    /// <param name="method">The command method.</param>
    /// <param name="module">The command module.</param>
    public Command(string name, string[] aliases, string? description, MethodInfo method, BaseCommandModule module)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
        Description = description;
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Module = module ?? throw new ArgumentNullException(nameof(module));
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
    private DiscordClient? _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsExtension"/> class.
    /// </summary>
    /// <param name="prefix">The command prefix.</param>
    public CommandsExtension(string prefix = "!")
    {
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
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

        foreach (var method in methods)
        {
            var commandAttr = method.GetCustomAttribute<CommandAttribute>();
            if (commandAttr == null)
                continue;

            var aliasesAttr = method.GetCustomAttribute<AliasesAttribute>();
            var descriptionAttr = method.GetCustomAttribute<DescriptionAttribute>();

            var aliases = aliasesAttr?.Aliases ?? Array.Empty<string>();
            var description = descriptionAttr?.Description;

            var command = new Command(commandAttr.Name, aliases, description, method, module);

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

            var command = new Command(commandAttr.Name, aliases, description, method, module);

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

    private async void OnMessageCreate(MessageCreateEvent evt)
    {
        if (evt.Author?.Bot == true || string.IsNullOrEmpty(evt.Content))
            return;

        if (!evt.Content.StartsWith(_prefix))
            return;

        var content = evt.Content[_prefix.Length..];
        var parts = content.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return;

        var commandName = parts[0];
        var rawArgs = parts.Length > 1 ? parts[1] : string.Empty;
        var args = rawArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (_commands.TryGetValue(commandName, out var command))
        {
            // Create a Message object from the event
            var message = new Message
            {
                Id = evt.Id,
                ChannelId = evt.ChannelId,
                Author = evt.Author!, // author is always present on gateway MESSAGE_CREATE events
                Content = evt.Content,
                Timestamp = evt.Timestamp,
                EditedTimestamp = evt.EditedTimestamp,
                Tts = evt.Tts,
                MentionEveryone = evt.MentionEveryone,
                Mentions = evt.Mentions,
                // Add other properties as needed
            };

            // _client is guaranteed non-null here: OnMessageCreate is only registered after _client is set
            var ctx = new CommandContext(_client!, message, _prefix, commandName, args, rawArgs);

            try
            {
                await command.Module.BeforeExecutionAsync(ctx);

                var parameters = command.Method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(CommandContext))
                {
                    await (Task)command.Method.Invoke(command.Module, new object[] { ctx })!;
                }
                else
                {
                    // Handle parameter parsing here
                    await (Task)command.Method.Invoke(command.Module, new object[] { ctx })!;
                }

                await command.Module.AfterExecutionAsync(ctx);
            }
            catch (Exception ex)
            {
                // Handle command execution errors
                Console.WriteLine($"Command execution error: {ex.Message}");
            }
        }
    }
}