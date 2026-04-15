#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _commandHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _componentHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _modalHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task<List<AutocompleteChoice>>>> _autocompleteHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _userContextMenuHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _messageContextMenuHandlers = new();

    /// <summary>
    /// Optional warning callback invoked when a registration overwrites an existing handler.
    /// </summary>
    public Action<string>? RegistrationWarning { get; set; }

    /// <summary>
    /// When true, duplicate registrations throw instead of overwriting.
    /// Default is false to preserve backward-compatible behavior.
    /// </summary>
    public bool ThrowOnDuplicateRegistration { get; set; }

    public InteractionHandler(IDiscordRestClient restClient)
    {
        _restClient = restClient;
    }

    /// <summary>
    /// Registers a slash command handler.
    /// </summary>
    public void RegisterCommand(string name, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_commandHandlers, name, handler, "slash command");
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
    /// Registers a modal submit handler by its <c>custom_id</c>.
    /// </summary>
    public void RegisterModal(string customId, Func<InteractionCreateEvent, Task> handler)
    {
        RegisterWithDiagnostics(_modalHandlers, customId, handler, "modal");
    }

    private void RegisterWithDiagnostics<THandler>(
        Dictionary<string, THandler> handlers,
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
        }

        handlers[key] = handler;
    }

    /// <summary>
    /// Handles an interaction event by routing to the appropriate registered handler.
    /// </summary>
    public async Task HandleInteractionAsync(InteractionCreateEvent interaction)
    {
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
                        await componentHandler(interaction);
                    }
                    break;

                case InteractionType.ApplicationCommandAutocomplete:
                    if (interaction.Data?.Name != null &&
                        _autocompleteHandlers.TryGetValue(interaction.Data.Name, out var autocompleteHandler))
                    {
                        var choices = await autocompleteHandler(interaction);
                        var response = new InteractionResponse
                        {
                            Type = (int)InteractionResponseType.ApplicationCommandAutocompleteResult,
                            Data = new InteractionCallbackData { Choices = choices }
                        };
                        await _restClient.CreateInteractionResponseAsync(interaction.Id, interaction.Token, response);
                    }
                    break;

                case InteractionType.ModalSubmit:
                    if (interaction.Data?.CustomId != null &&
                        _modalHandlers.TryGetValue(interaction.Data.CustomId, out var modalHandler))
                    {
                        await modalHandler(interaction);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the bot
            // User handlers should handle their own errors, but we catch here as a safety net
            // Consider adding ILogger dependency in future for proper logging
            Console.Error.WriteLine($"Unhandled exception in interaction handler for interaction {interaction.Id}: {ex}");
        }
    }

    private async Task HandleApplicationCommandAsync(InteractionCreateEvent interaction)
    {
        if (interaction.Data?.Name == null) return;

        // Route by application command type: CHAT_INPUT=1, USER=2, MESSAGE=3
        switch (interaction.Data.Type)
        {
            case (int)PawSharp.Interactions.Models.ApplicationCommandType.User:
                if (_userContextMenuHandlers.TryGetValue(interaction.Data.Name, out var userHandler))
                    await userHandler(interaction);
                break;

            case (int)PawSharp.Interactions.Models.ApplicationCommandType.Message:
                if (_messageContextMenuHandlers.TryGetValue(interaction.Data.Name, out var messageHandler))
                    await messageHandler(interaction);
                break;

            default: // CHAT_INPUT (1) or unrecognised — fall through to slash command handlers
                if (_commandHandlers.TryGetValue(interaction.Data.Name, out var slashHandler))
                    await slashHandler(interaction);
                break;
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
    public async Task<HttpResponseMessage> EditResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)
    {
        return await _restClient.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request);
    }

    /// <summary>
    /// Follows up with an additional message. Returns the created Message.
    /// </summary>
    public async Task<Message?> CreateFollowupAsync(string applicationId, string interactionToken, CreateMessageRequest request)
    {
        return await _restClient.CreateFollowupMessageAsync(applicationId, interactionToken, request);
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
    /// Follows up with an additional message (legacy overload using InteractionCallbackData).
    /// </summary>
    [Obsolete("Use CreateFollowupAsync(applicationId, interactionToken, CreateMessageRequest) instead.")]
    public async Task<HttpResponseMessage> FollowupAsync(string applicationId, string interactionToken, InteractionCallbackData data)
    {
        // InteractionCallbackData has [JsonPropertyName] attributes ensuring correct snake_case output.
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await _restClient.PostAsync($"webhooks/{applicationId}/{interactionToken}", content);
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
    Modal = 9
}