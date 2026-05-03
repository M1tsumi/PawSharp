#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Interactions.Models;
using PawSharp.Core.Entities;

namespace PawSharp.Interactions;

/// <summary>
/// Handles Discord interactions (slash commands, components, autocomplete, context menus).
/// </summary>
public class InteractionHandler
{
    private readonly IDiscordRestClient _restClient;
    private readonly ILogger<InteractionHandler>? _logger;
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _commandHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _componentHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _modalHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task<List<AutocompleteChoice>>>> _autocompleteHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _userContextMenuHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _messageContextMenuHandlers = new();
    private readonly ConcurrentDictionary<string, Func<InteractionCreateEvent, Task>> _entryPointHandlers = new();

    /// <summary>
    /// Optional warning callback invoked when a registration overwrites an existing handler.
    /// </summary>
    public Action<string>? RegistrationWarning { get; set; }

    /// <summary>
    /// When true, duplicate registrations throw instead of overwriting.
    /// Default is false to preserve backward-compatible behavior.
    /// </summary>
    public bool ThrowOnDuplicateRegistration { get; set; }

    public InteractionHandler(IDiscordRestClient restClient, ILogger<InteractionHandler>? logger = null)
    {
        _restClient = restClient;
        _logger = logger;
    }

    /// <summary>
    /// Registers a slash command handler.
    /// </summary>
    public void RegisterCommand(string name, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_commandHandlers, name, handler, "slash command");
    }

    /// <summary>
    /// Registers multiple slash command handlers at once.
    /// </summary>
    public void RegisterCommands(params (string name, Func<InteractionCreateEvent, Task> handler)[] commands)
    {
        foreach (var (name, handler) in commands)
            RegisterCommand(name, handler);
    }

    /// <summary>
    /// Checks if a slash command handler is registered for the given name.
    /// </summary>
    public bool HasCommandHandler(string name) => _commandHandlers.ContainsKey(name);

    /// <summary>
    /// Checks if a component handler is registered for the given custom ID.
    /// </summary>
    public bool HasComponentHandler(string customId) => _componentHandlers.ContainsKey(customId);

    /// <summary>
    /// Checks if a modal handler is registered for the given custom ID.
    /// </summary>
    public bool HasModalHandler(string customId) => _modalHandlers.ContainsKey(customId);

    /// <summary>
    /// Checks if an autocomplete handler is registered for the given command name.
    /// </summary>
    public bool HasAutocompleteHandler(string commandName) => _autocompleteHandlers.ContainsKey(commandName);

    /// <summary>
    /// Checks if a user context menu handler is registered for the given name.
    /// </summary>
    public bool HasUserContextMenuHandler(string name) => _userContextMenuHandlers.ContainsKey(name);

    /// <summary>
    /// Checks if a message context menu handler is registered for the given name.
    /// </summary>
    public bool HasMessageContextMenuHandler(string name) => _messageContextMenuHandlers.ContainsKey(name);

    /// <summary>
    /// Checks if an entry point handler is registered for the given name.
    /// </summary>
    public bool HasEntryPointHandler(string name) => _entryPointHandlers.ContainsKey(name);

    /// <summary>
    /// Unregisters a slash command handler by name.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterCommand(string name) => _commandHandlers.TryRemove(name, out _);

    /// <summary>
    /// Unregisters a component handler by custom ID.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterComponent(string customId) => _componentHandlers.TryRemove(customId, out _);

    /// <summary>
    /// Unregisters a modal handler by custom ID.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterModal(string customId) => _modalHandlers.TryRemove(customId, out _);

    /// <summary>
    /// Unregisters an autocomplete handler by command name.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterAutocomplete(string commandName) => _autocompleteHandlers.TryRemove(commandName, out _);

    /// <summary>
    /// Unregisters a user context menu handler by name.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterUserContextMenu(string name) => _userContextMenuHandlers.TryRemove(name, out _);

    /// <summary>
    /// Unregisters a message context menu handler by name.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterMessageContextMenu(string name) => _messageContextMenuHandlers.TryRemove(name, out _);

    /// <summary>
    /// Unregisters an entry point handler by name.
    /// </summary>
    /// <returns>True if the handler was found and removed.</returns>
    public bool UnregisterEntryPoint(string name) => _entryPointHandlers.TryRemove(name, out _);

    /// <summary>
    /// Clears all registered handlers.
    /// </summary>
    public void ClearAllHandlers()
    {
        _commandHandlers.Clear();
        _componentHandlers.Clear();
        _modalHandlers.Clear();
        _autocompleteHandlers.Clear();
        _userContextMenuHandlers.Clear();
        _messageContextMenuHandlers.Clear();
        _entryPointHandlers.Clear();
    }

    /// <summary>
    /// Registers a component handler.
    /// </summary>
    public void RegisterComponent(string customId, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_componentHandlers, customId, handler, "component");
    }

    /// <summary>
    /// Registers an autocomplete handler for a command. The handler returns a list of choices
    /// which will be sent back to Discord automatically.
    /// </summary>
    public void RegisterAutocomplete(string commandName, Func<InteractionCreateEvent, Task<List<AutocompleteChoice>>> handler)
    {
        RegisterWithDiagnostics(_autocompleteHandlers, commandName, handler, "autocomplete");
    }

    /// <summary>
    /// Registers a user context menu command handler (right-click on a user).
    /// </summary>
    public void RegisterUserContextMenu(string name, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_userContextMenuHandlers, name, handler, "user context menu");
    }

    /// <summary>
    /// Registers a message context menu command handler (right-click on a message).
    /// </summary>
    public void RegisterMessageContextMenu(string name, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_messageContextMenuHandlers, name, handler, "message context menu");
    }

    /// <summary>
    /// Registers a PRIMARY_ENTRY_POINT command handler (Activity entry point).
    /// These commands are used to launch embedded Activities associated with the app.
    /// </summary>
    public void RegisterEntryPoint(string name, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_entryPointHandlers, name, handler, "entry point");
    }

    /// <summary>
    /// Registers a modal submit handler by its <c>custom_id</c>.
    /// </summary>
    public void RegisterModal(string customId, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_modalHandlers, customId, handler, "modal");
    }

    private void RegisterWithDiagnostics<THandler>(
        ConcurrentDictionary<string, THandler> handlers,
        string key,
        THandler handler,
        string kind)
    {
        if (handlers.ContainsKey(key))
        {
            var message = $"A {kind} handler with key '{key}' is already registered and will be replaced.";
            if (ThrowOnDuplicateRegistration)
                throw new InvalidOperationException(message);

            RegistrationWarning?.Invoke(message);
            _logger?.LogWarning("{Message}", message);
        }

        handlers[key] = handler;
        _logger?.LogDebug("Registered {Kind} handler with key '{Key}'", kind, key);
    }

    /// <summary>
    /// Handles an interaction event by routing to the appropriate registered handler.
    /// </summary>
    public async Task HandleInteractionAsync(InteractionCreateEvent interaction)
    {
        _logger?.LogDebug("Handling interaction of type {Type}, ID: {Id}", interaction.Type, interaction.Id);

        try
        {
            switch ((InteractionType)interaction.Type)
            {
                case InteractionType.ApplicationCommand:
                    await HandleApplicationCommandAsync(interaction);
                    break;

                case InteractionType.MessageComponent:
                    if (interaction.Data?.CustomId != null &&
                        _componentHandlers.TryGetValue(interaction.Data.CustomId, out var componentHandler))
                    {
                        await InvokeHandlerSafelyAsync(componentHandler, interaction, "component", interaction.Data.CustomId);
                    }
                    else
                    {
                        _logger?.LogWarning("No component handler registered for custom_id: {CustomId}", interaction.Data.CustomId);
                    }
                    break;

                case InteractionType.ApplicationCommandAutocomplete:
                    if (interaction.Data?.Name != null &&
                        _autocompleteHandlers.TryGetValue(interaction.Data.Name, out var autocompleteHandler))
                    {
                        await HandleAutocompleteAsync(interaction, autocompleteHandler);
                    }
                    else
                    {
                        _logger?.LogWarning("No autocomplete handler registered for command: {CommandName}", interaction.Data.Name);
                    }
                    break;

                case InteractionType.ModalSubmit:
                    if (interaction.Data?.CustomId != null &&
                        _modalHandlers.TryGetValue(interaction.Data.CustomId, out var modalHandler))
                    {
                        await InvokeHandlerSafelyAsync(modalHandler, interaction, "modal", interaction.Data.CustomId);
                    }
                    else
                    {
                        _logger?.LogWarning("No modal handler registered for custom_id: {CustomId}", interaction.Data.CustomId);
                    }
                    break;

                default:
                    _logger?.LogWarning("Unhandled interaction type: {Type}", interaction.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unhandled exception in HandleInteractionAsync for interaction ID: {Id}", interaction.Id);
            throw;
        }
    }

    private async Task HandleApplicationCommandAsync(InteractionCreateEvent interaction)
    {
        if (interaction.Data?.Name == null)
        {
            _logger?.LogWarning("Application command interaction has no name");
            return;
        }

        // Route by application command type: CHAT_INPUT=1, USER=2, MESSAGE=3, PRIMARY_ENTRY_POINT=4
        switch (interaction.Data.Type)
        {
            case (int)PawSharp.Interactions.Models.ApplicationCommandType.User:
                if (_userContextMenuHandlers.TryGetValue(interaction.Data.Name, out var userHandler))
                {
                    await InvokeHandlerSafelyAsync(userHandler, interaction, "user context menu", interaction.Data.Name);
                }
                else
                {
                    _logger?.LogWarning("No user context menu handler registered for: {CommandName}", interaction.Data.Name);
                }
                break;

            case (int)PawSharp.Interactions.Models.ApplicationCommandType.Message:
                if (_messageContextMenuHandlers.TryGetValue(interaction.Data.Name, out var messageHandler))
                {
                    await InvokeHandlerSafelyAsync(messageHandler, interaction, "message context menu", interaction.Data.Name);
                }
                else
                {
                    _logger?.LogWarning("No message context menu handler registered for: {CommandName}", interaction.Data.Name);
                }
                break;

            case 4: // PRIMARY_ENTRY_POINT - Activity entry point
                if (_entryPointHandlers.TryGetValue(interaction.Data.Name, out var entryHandler))
                {
                    await InvokeHandlerSafelyAsync(entryHandler, interaction, "entry point", interaction.Data.Name);
                }
                else
                {
                    _logger?.LogWarning("No entry point handler registered for: {CommandName}", interaction.Data.Name);
                }
                break;

            default: // CHAT_INPUT (1) or unrecognised — fall through to slash command handlers
                if (_commandHandlers.TryGetValue(interaction.Data.Name, out var slashHandler))
                {
                    await InvokeHandlerSafelyAsync(slashHandler, interaction, "slash command", interaction.Data.Name);
                }
                else
                {
                    _logger?.LogWarning("No slash command handler registered for: {CommandName}", interaction.Data.Name);
                }
                break;
        }
    }

    private async Task HandleAutocompleteAsync(InteractionCreateEvent interaction, Func<InteractionCreateEvent, Task<List<AutocompleteChoice>>> handler)
    {
        try
        {
            var choices = await handler(interaction);
            var response = new InteractionResponse
            {
                Type = (int)InteractionResponseType.ApplicationCommandAutocompleteResult,
                Data = new InteractionCallbackData { Choices = choices }
            };
            await _restClient.CreateInteractionResponseAsync(interaction.Id, interaction.Token, response);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Autocomplete handler failed for command: {CommandName}", interaction.Data?.Name);
            // Send empty choices to prevent timeout
            var response = new InteractionResponse
            {
                Type = (int)InteractionResponseType.ApplicationCommandAutocompleteResult,
                Data = new InteractionCallbackData { Choices = new List<AutocompleteChoice>() }
            };
            await _restClient.CreateInteractionResponseAsync(interaction.Id, interaction.Token, response);
        }
    }

    private async Task InvokeHandlerSafelyAsync(Func<InteractionCreateEvent, Task> handler, InteractionCreateEvent interaction, string handlerType, string key)
    {
        if (handler == null)
        {
            _logger?.LogWarning("Handler is null for {HandlerType} with key '{Key}'", handlerType, key);
            return;
        }

        try
        {
            await handler(interaction);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "{HandlerType} handler failed for key '{Key}'", handlerType, key);
            // Optionally send error response to user
            try
            {
                await _restClient.CreateInteractionResponseAsync(interaction.Id, interaction.Token, new InteractionResponse
                {
                    Type = (int)InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData { Content = "An error occurred while processing this interaction.", Flags = 64 }
                });
            }
            catch (Exception responseEx)
            {
                _logger?.LogError(responseEx, "Failed to send error response for failed {HandlerType} handler", handlerType);
            }
        }
    }

    /// <summary>
    /// Responds to an interaction with a message.
    /// </summary>
    public async Task<bool> RespondAsync(ulong interactionId, string interactionToken, InteractionResponse response)
    {
        return await _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds to a slash command interaction with an ephemeral (only-visible-to-user) message.
    /// </summary>
    public Task<bool> RespondEphemeralAsync(ulong interactionId, string interactionToken, string content)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = content, Flags = 64 }
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds to a slash command interaction with an ephemeral message and embeds.
    /// </summary>
    public Task<bool> RespondEphemeralAsync(ulong interactionId, string interactionToken, string content, List<Embed> embeds)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = content, Embeds = embeds, Flags = 64 }
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds to an interaction with a message and embeds.
    /// </summary>
    public Task<bool> RespondWithEmbedsAsync(ulong interactionId, string interactionToken, string content, List<Embed> embeds, bool ephemeral = false)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = content, Embeds = embeds, Flags = ephemeral ? 64 : null }
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds to an interaction by updating the component's message.
    /// </summary>
    public Task<bool> RespondUpdateAsync(ulong interactionId, string interactionToken, string content)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.UpdateMessage,
            Data = new InteractionCallbackData { Content = content }
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds to an interaction by updating the component's message with embeds.
    /// </summary>
    public Task<bool> RespondUpdateAsync(ulong interactionId, string interactionToken, string content, List<Embed>? embeds, List<MessageComponent>? components = null)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.UpdateMessage,
            Data = new InteractionCallbackData { Content = content, Embeds = embeds, Components = components }
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Defers a slash command interaction, showing a "Bot is thinking…" state.
    /// Use <see cref="EditResponseAsync"/> or <see cref="CreateFollowupAsync"/> to follow up.
    /// </summary>
    /// <param name="interactionId">The interaction ID from the event.</param>
    /// <param name="interactionToken">The interaction token from the event.</param>
    /// <param name="ephemeral">When <c>true</c>, the follow-up response will only be visible to the invoking user.</param>
    public Task<bool> DeferAsync(ulong interactionId, string interactionToken, bool ephemeral = false)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.DeferredChannelMessageWithSource,
            Data = ephemeral ? new InteractionCallbackData { Flags = 64 } : null
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Defers an update for a component interaction (button / select menu).
    /// The original message is not modified; use <see cref="EditResponseAsync"/> to update it afterwards.
    /// </summary>
    public Task<bool> DeferComponentAsync(ulong interactionId, string interactionToken)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.DeferredUpdateMessage
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Edits the original interaction response.
    /// </summary>
    public async Task<bool> EditResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)
    {
        var response = await _restClient.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Gets the original interaction response.
    /// </summary>
    public async Task<Message?> GetOriginalResponseAsync(string applicationId, string interactionToken)
    {
        return await _restClient.GetOriginalInteractionResponseAsync(applicationId, interactionToken);
    }

    /// <summary>
    /// Deletes the original interaction response.
    /// </summary>
    public async Task<bool> DeleteOriginalResponseAsync(string applicationId, string interactionToken)
    {
        return await _restClient.DeleteOriginalInteractionResponseAsync(applicationId, interactionToken);
    }

    /// <summary>
    /// Follows up with an additional message. Returns the created Message.
    /// </summary>
    public async Task<Message?> CreateFollowupAsync(string applicationId, string interactionToken, CreateMessageRequest request)
    {
        return await _restClient.CreateFollowupMessageAsync(applicationId, interactionToken, request);
    }

    /// <summary>
    /// Gets a follow-up message.
    /// </summary>
    public async Task<Message?> GetFollowupAsync(string applicationId, string interactionToken, ulong messageId)
    {
        return await _restClient.GetFollowupMessageAsync(applicationId, interactionToken, messageId);
    }

    /// <summary>
    /// Edits a previously sent follow-up message.
    /// </summary>
    public async Task<Message?> EditFollowupAsync(string applicationId, string interactionToken, ulong messageId, EditMessageRequest request)
    {
        return await _restClient.EditFollowupMessageAsync(applicationId, interactionToken, messageId, request);
    }

    /// <summary>
    /// Deletes a follow-up message.
    /// </summary>
    public async Task<bool> DeleteFollowupAsync(string applicationId, string interactionToken, ulong messageId)
    {
        return await _restClient.DeleteFollowupMessageAsync(applicationId, interactionToken, messageId);
    }

    /// <summary>
    /// Responds by launching the Activity associated with the app.
    /// Only available for apps with Activities enabled.
    /// </summary>
    /// <param name="interactionId">The interaction ID from the event.</param>
    /// <param name="interactionToken">The interaction token from the event.</param>
    public Task<bool> RespondWithActivityAsync(ulong interactionId, string interactionToken)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.LaunchActivity
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Responds with a premium required response (deprecated).
    /// </summary>
    /// <param name="interactionId">The interaction ID from the event.</param>
    /// <param name="interactionToken">The interaction token from the event.</param>
    public Task<bool> RespondPremiumRequiredAsync(ulong interactionId, string interactionToken)
    {
        var response = new InteractionResponse
        {
            Type = (int)InteractionResponseType.PremiumRequired
        };
        return _restClient.CreateInteractionResponseAsync(interactionId, interactionToken, response);
    }

    /// <summary>
    /// Gets all application command permissions for a guild.
    /// </summary>
    public async Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId)
    {
        return await _restClient.GetGuildApplicationCommandPermissionsAsync(applicationId, guildId);
    }

    /// <summary>
    /// Gets permissions for a specific application command.
    /// </summary>
    public async Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId)
    {
        return await _restClient.GetApplicationCommandPermissionsAsync(applicationId, guildId, commandId);
    }

    /// <summary>
    /// Edits permissions for a specific application command.
    /// </summary>
    public async Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions)
    {
        return await _restClient.EditApplicationCommandPermissionsAsync(applicationId, guildId, commandId, permissions);
    }

    /// <summary>
    /// Batch edits permissions for multiple application commands.
    /// </summary>
    public async Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions)
    {
        return await _restClient.BatchEditApplicationCommandPermissionsAsync(applicationId, guildId, permissions);
    }
}

/// <summary>
/// Discord interaction types.
/// </summary>
public enum InteractionType
{
    Ping = 1,
    ApplicationCommand = 2,
    MessageComponent = 3,
    ApplicationCommandAutocomplete = 4,
    ModalSubmit = 5
}

/// <summary>
/// Interaction response types.
/// </summary>
public enum InteractionResponseType
{
    Pong = 1,
    ChannelMessageWithSource = 4,
    DeferredChannelMessageWithSource = 5,
    DeferredUpdateMessage = 6,
    UpdateMessage = 7,
    ApplicationCommandAutocompleteResult = 8,
    Modal = 9,
    /// <summary>Deprecated. Respond to an interaction with an upgrade button.</summary>
    PremiumRequired = 10,
    /// <summary>Launch the Activity associated with the app. Only for apps with Activities enabled.</summary>
    LaunchActivity = 12
}