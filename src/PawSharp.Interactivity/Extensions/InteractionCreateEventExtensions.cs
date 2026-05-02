#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;
using PawSharp.Interactions;

namespace PawSharp.Interactivity.Extensions;

/// <summary>
/// Extension methods for InteractionCreateEvent that bridge Interactions and Interactivity.
/// Enables seamless workflows where you respond to an interaction and then wait for follow-up input.
/// </summary>
public static class InteractionCreateEventExtensions
{
    /// <summary>
    /// Responds to the interaction with a message containing components, then waits for a button click.
    /// </summary>
    /// <param name="interaction">The interaction to respond to.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="initialResponse">The initial response containing the message with buttons.</param>
    /// <param name="targetCustomId">Optional specific button custom_id to wait for. If null, accepts any button.</param>
    /// <param name="timeout">Maximum time to wait. Falls back to InteractivityExtension.Timeout if not specified.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The button click interaction, or TimedOut=true if deadline exceeded.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> RespondAndWaitForButtonAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        InteractionResponse initialResponse,
        string? targetCustomId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // Send the initial response
        var responded = await client.Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, initialResponse);
        if (!responded)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Get the application ID
        var appId = client.CurrentUser?.Id.ToString()
            ?? throw new InvalidOperationException("CurrentUser not available. Ensure client is connected.");

        // Get the message that was created
        var message = await client.Rest.GetOriginalInteractionResponseAsync(appId, interaction.Token);
        if (message == null)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Wait for button click on that message
        return await WaitForButtonOnMessageAsync(message, client, GetUserFromInteraction(interaction), targetCustomId, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a follow-up message with components and waits for any component interaction.
    /// </summary>
    /// <param name="interaction">The original interaction.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="followUpRequest">The follow-up message request.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The component interaction, or TimedOut=true if deadline exceeded.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> FollowUpAndWaitAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        CreateMessageRequest followUpRequest,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var appId = client.CurrentUser?.Id.ToString()
            ?? throw new InvalidOperationException("CurrentUser not available");

        // Send follow-up message
        var message = await client.Rest.CreateFollowupMessageAsync(appId, interaction.Token, followUpRequest);
        if (message == null)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Wait for any component interaction on this message from the same user
        return await WaitForAnyComponentAsync(message, client, GetUserFromInteraction(interaction), targetCustomId: null, timeout, cancellationToken);
    }

    /// <summary>
    /// Sends a modal response and waits for the modal submission.
    /// </summary>
    /// <param name="interaction">The interaction to respond to with a modal.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="modalData">The modal configuration (custom_id, title, components).</param>
    /// <param name="timeout">Maximum time to wait for submission.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The modal submission interaction, or TimedOut=true if deadline exceeded.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> ShowModalAndWaitAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        InteractionCallbackData modalData,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // Send modal response (type 9)
        var response = new InteractionResponse
        {
            Type = 9, // Modal
            Data = modalData
        };

        var responded = await client.Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, response);
        if (!responded)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Wait for modal submission from the same user with matching custom_id
        return await WaitForModalSubmissionAsync(
            GetUserFromInteraction(interaction),
            modalData.CustomId,
            client,
            timeout,
            cancellationToken);
    }

    /// <summary>
    /// Defers the interaction (shows "Bot is thinking...") and then waits for the next message from the user.
    /// Useful for long-running operations followed by text input collection.
    /// </summary>
    /// <param name="interaction">The interaction to defer.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="channel">The channel to wait for messages in.</param>
    /// <param name="timeout">Maximum time to wait for a message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next message from the user, or TimedOut=true.</returns>
    public static async Task<InteractivityResult<MessageCreateEvent>> DeferAndWaitForMessageAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        Channel channel,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // Send deferred response
        var deferResponse = new InteractionResponse
        {
            Type = (int)InteractionResponseType.DeferredChannelMessageWithSource
        };

        await client.Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, deferResponse);

        // Wait for next message from the interacting user
        var user = GetUserFromInteraction(interaction);
        return await channel.GetNextMessageAsync(
            client,
            msg => msg.Author.Id == user?.Id,
            timeout,
            cancellationToken);
    }

    /// <summary>
    /// Updates the original response message and waits for a component interaction on it.
    /// </summary>
    /// <param name="interaction">The original interaction.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="updateRequest">The message update content.</param>
    /// <param name="targetCustomId">Optional specific component to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The component interaction, or TimedOut=true.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> UpdateAndWaitAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        EditMessageRequest updateRequest,
        string? targetCustomId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var appId = client.CurrentUser?.Id.ToString()
            ?? throw new InvalidOperationException("CurrentUser not available");

        // Update the original message
        var response = await client.Rest.EditOriginalInteractionResponseAsync(appId, interaction.Token, updateRequest);
        if (!response.IsSuccessStatusCode)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Get the updated message
        var message = await client.Rest.GetOriginalInteractionResponseAsync(appId, interaction.Token);
        if (message == null)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Wait for component interaction
        return await WaitForAnyComponentAsync(
            message,
            client,
            GetUserFromInteraction(interaction),
            targetCustomId,
            timeout,
            cancellationToken);
    }

    /// <summary>
    /// Acknowledges a component interaction and sends a follow-up that waits for response.
    /// </summary>
    /// <param name="interaction">The component interaction to acknowledge.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="acknowledgeWithUpdate">If true, updates the message; if false, defers.</param>
    /// <param name="followUpRequest">Optional follow-up message to send after acknowledging.</param>
    /// <param name="timeout">Timeout for waiting on the follow-up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The interaction result, or TimedOut=true.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> AcknowledgeAndFollowUpAsync(
        this InteractionCreateEvent interaction,
        DiscordClient client,
        bool acknowledgeWithUpdate = false,
        CreateMessageRequest? followUpRequest = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // Acknowledge the interaction
        var ackResponse = new InteractionResponse
        {
            Type = acknowledgeWithUpdate
                ? (int)InteractionResponseType.UpdateMessage
                : (int)InteractionResponseType.DeferredUpdateMessage
        };

        await client.Rest.CreateInteractionResponseAsync(interaction.Id, interaction.Token, ackResponse);

        // If no follow-up requested, return success
        if (followUpRequest == null)
            return new InteractivityResult<InteractionCreateEvent> { Result = interaction };

        // Send follow-up
        var appId = client.CurrentUser?.Id.ToString()
            ?? throw new InvalidOperationException("CurrentUser not available");

        var message = await client.Rest.CreateFollowupMessageAsync(appId, interaction.Token, followUpRequest);
        if (message == null)
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };

        // Wait for response on follow-up
        return await WaitForAnyComponentAsync(
            message,
            client,
            GetUserFromInteraction(interaction),
            targetCustomId: null,
            timeout: timeout,
            cancellationToken: cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helper methods
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<InteractivityResult<InteractionCreateEvent>> WaitForButtonOnMessageAsync(
        Message message,
        DiscordClient client,
        User? user,
        string? targetCustomId,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != 3) return; // MESSAGE_COMPONENT
            if (evt.Data?.ComponentType != 2) return; // Button
            if (evt.Message?.Id != message.Id) return;
            if (user is not null && GetUserId(evt) != user.Id) return;
            if (targetCustomId is not null && evt.Data?.CustomId != targetCustomId) return;

            tcs.TrySetResult(evt);
        }

        using var subscription = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

        try
        {
            var evt = await tcs.Task;
            return new InteractivityResult<InteractionCreateEvent> { Result = evt };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };
        }
    }

    private static async Task<InteractivityResult<InteractionCreateEvent>> WaitForAnyComponentAsync(
        Message message,
        DiscordClient client,
        User? user,
        string? targetCustomId,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != 3) return; // MESSAGE_COMPONENT
            if (evt.Message?.Id != message.Id) return;
            if (user is not null && GetUserId(evt) != user.Id) return;
            if (targetCustomId is not null && evt.Data?.CustomId != targetCustomId) return;

            tcs.TrySetResult(evt);
        }

        using var subscription = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

        try
        {
            var evt = await tcs.Task;
            return new InteractivityResult<InteractionCreateEvent> { Result = evt };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };
        }
    }

    private static async Task<InteractivityResult<InteractionCreateEvent>> WaitForModalSubmissionAsync(
        User? user,
        string? customId,
        DiscordClient client,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != 5) return; // MODAL_SUBMIT
            if (user is not null && GetUserId(evt) != user.Id) return;
            if (customId is not null && evt.Data?.CustomId != customId) return;

            tcs.TrySetResult(evt);
        }

        using var subscription = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

        try
        {
            var evt = await tcs.Task;
            return new InteractivityResult<InteractionCreateEvent> { Result = evt };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<InteractionCreateEvent> { TimedOut = true };
        }
    }

    private static User? GetUserFromInteraction(InteractionCreateEvent interaction)
        => interaction.User ?? interaction.Member?.User;

    private static ulong GetUserId(InteractionCreateEvent evt)
        => evt.User?.Id ?? evt.Member?.User?.Id ?? 0;
}
