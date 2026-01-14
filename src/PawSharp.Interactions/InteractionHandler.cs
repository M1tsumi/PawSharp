using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PawSharp.API;
using PawSharp.API.Clients;
using PawSharp.API.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Interactions.Models;
using PawSharp.Core.Entities;

namespace PawSharp.Interactions;

/// <summary>
/// Handles Discord interactions (slash commands, components).
/// </summary>
public class InteractionHandler
{
    private readonly DiscordRestClient _restClient;
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _commandHandlers = new();
    private readonly Dictionary<string, Func<InteractionCreateEvent, Task>> _componentHandlers = new();

    public InteractionHandler(DiscordRestClient restClient)
    {
        _restClient = restClient;
    }

    /// <summary>
    /// Registers a slash command handler.
    /// </summary>
    public void RegisterCommand(string name, Func<InteractionCreateEvent, Task> handler)
    {
        _commandHandlers[name] = handler;
    }

    /// <summary>
    /// Registers a component handler.
    /// </summary>
    public void RegisterComponent(string customId, Func<InteractionCreateEvent, Task> handler)
    {
        _componentHandlers[customId] = handler;
    }

    /// <summary>
    /// Handles an interaction event.
    /// </summary>
    public async Task HandleInteractionAsync(InteractionCreateEvent interaction)
    {
        if (interaction.Type == 2) // APPLICATION_COMMAND
        {
            if (interaction.Data?.Name != null && _commandHandlers.TryGetValue(interaction.Data.Name, out var handler))
            {
                await handler(interaction);
            }
        }
        else if (interaction.Type == 3) // MESSAGE_COMPONENT
        {
            if (interaction.Data?.CustomId != null && _componentHandlers.TryGetValue(interaction.Data.CustomId, out var handler))
            {
                await handler(interaction);
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
    /// Edits the original interaction response.
    /// </summary>
    public async Task<HttpResponseMessage> EditResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)
    {
        return await _restClient.EditOriginalInteractionResponseAsync(applicationId, interactionToken, request);
    }

    /// <summary>
    /// Follows up with an additional message.
    /// </summary>
    public async Task<HttpResponseMessage> FollowupAsync(string applicationId, string interactionToken, InteractionCallbackData data)
    {
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await _restClient.PostAsync($"webhooks/{applicationId}/{interactionToken}", content);
    }
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