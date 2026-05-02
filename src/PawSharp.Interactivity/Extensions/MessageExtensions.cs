#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;

namespace PawSharp.Interactivity.Extensions;

/// <summary>
/// Extension methods for Discord messages.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Waits for a reaction on the message.
    /// </summary>
    /// <param name="message">The message to wait for reactions on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose reaction to wait for.</param>
    /// <param name="emoji">The specific emoji to wait for, or null for any emoji.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<Reaction>> WaitForReactionAsync(
        this Message message,
        DiscordClient client,
        User user,
        string? emoji = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<Reaction>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId == message.Id &&
                evt.UserId == user.Id &&
                (emoji == null || evt.Emoji.Name == emoji))
            {
                var reaction = new Reaction
                {
                    Count = 1,
                    Me = evt.UserId == client.CurrentUser?.Id,
                    Emoji = evt.Emoji
                };
                tcs.TrySetResult(reaction);
            }
        }

        var subscription = client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            var reaction = await tcs.Task;
            return new InteractivityResult<Reaction> { Result = reaction };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<Reaction> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Collects reactions on the message.
    /// </summary>
    /// <param name="message">The message to collect reactions from.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="timeout">The timeout for collecting.</param>
    /// <returns>The collected reactions.</returns>
    public static async Task<IEnumerable<Reaction>> CollectReactionsAsync(
        this Message message,
        DiscordClient client,
        TimeSpan? timeout = null)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var reactions = new List<Reaction>();

        var tcs = new TaskCompletionSource<bool>();
        var cts = new CancellationTokenSource(timeout.Value);

        cts.Token.Register(() => tcs.TrySetResult(true));

        void OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId == message.Id)
            {
                var reaction = new Reaction
                {
                    Count = 1,
                    Me = evt.UserId == client.CurrentUser?.Id,
                    Emoji = evt.Emoji
                };
                reactions.Add(reaction);
            }
        }

        var subscription = client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            await tcs.Task;
        }
        finally
        {
            subscription.Dispose(); // Unregister handler to prevent unbounded list growth
            cts.Dispose();
        }

        return reactions;
    }

    /// <summary>
    /// Waits for a reaction to be removed from the message.
    /// </summary>
    /// <param name="message">The message to wait for reaction removal on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose reaction removal to wait for.</param>
    /// <param name="emoji">The specific emoji to wait for removal of, or null for any emoji.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<Reaction>> WaitForReactionRemoveAsync(
        this Message message,
        DiscordClient client,
        User user,
        string? emoji = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<Reaction>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnReactionRemove(MessageReactionRemoveEvent evt)
        {
            if (evt.MessageId == message.Id &&
                evt.UserId == user.Id &&
                (emoji == null || evt.Emoji.Name == emoji))
            {
                var reaction = new Reaction
                {
                    Count = 0, // Reaction was removed
                    Me = evt.UserId == client.CurrentUser?.Id,
                    Emoji = evt.Emoji
                };
                tcs.TrySetResult(reaction);
            }
        }

        var subscription = client.Gateway.Events.On<MessageReactionRemoveEvent>("MESSAGE_REACTION_REMOVE", OnReactionRemove);

        try
        {
            var reaction = await tcs.Task;
            return new InteractivityResult<Reaction> { Result = reaction };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<Reaction> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Creates a poll on the message.
    /// </summary>
    /// <param name="message">The message to create a poll on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="question">The poll question.</param>
    /// <param name="options">The poll options.</param>
    /// <param name="timeout">The timeout for the poll.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CreatePollAsync(
        this Message message,
        DiscordClient client,
        string question,
        IEnumerable<string> options,
        TimeSpan? timeout = null)
    {
        var optionList = options.ToList();
        if (optionList.Count < 2 || optionList.Count > 10)
            throw new ArgumentException("Poll must have between 2 and 10 options.", nameof(options));

        var pollEmojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };

        var embed = new Embed
        {
            Title = question,
            Description = string.Join("\n", optionList.Select((opt, i) => $"{pollEmojis[i]} {opt}"))
        };

        // Update the message with poll content
        await client.Rest.EditMessageAsync(message.ChannelId, message.Id, new EditMessageRequest
        {
            Embeds = new List<Embed> { embed }
        });

        // Add reactions for voting
        for (int i = 0; i < optionList.Count; i++)
        {
            await client.Rest.CreateReactionAsync(message.ChannelId, message.Id, pollEmojis[i]);
        }

        // Auto-cleanup after timeout
        if (timeout.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout.Value);
                    // Clean up all reactions from this message
                    await client.Rest.DeleteAllReactionsAsync(message.ChannelId, message.Id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Poll cleanup failed: {ex.Message}");
                }
            });
        }
    }

    /// <summary>
    /// Gets the voters for a specific answer in a Discord native poll.
    /// </summary>
    /// <param name="message">The message containing the poll.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="answerId">The ID of the poll answer to get voters for.</param>
    /// <param name="limit">Maximum number of voters to return (default 25, max 100).</param>
    /// <param name="after">Get voters after this user ID for pagination.</param>
    /// <returns>A list of users who voted for the specified answer.</returns>
    public static async Task<List<User>?> GetPollAnswerVotersAsync(
        this Message message,
        DiscordClient client,
        int answerId,
        int? limit = null,
        ulong? after = null)
    {
        if (message.Poll == null)
            throw new InvalidOperationException("Message does not contain a poll.");

        return await client.Rest.GetAnswerVotersAsync(
            message.ChannelId,
            message.Id,
            answerId,
            limit,
            after);
    }

    /// <summary>
    /// Ends a Discord native poll early, finalizing the results.
    /// </summary>
    /// <param name="message">The message containing the poll.</param>
    /// <param name="client">The Discord client.</param>
    /// <returns>The updated message with finalized poll results.</returns>
    public static async Task<Message?> EndPollAsync(
        this Message message,
        DiscordClient client)
    {
        if (message.Poll == null)
            throw new InvalidOperationException("Message does not contain a poll.");

        return await client.Rest.EndPollAsync(message.ChannelId, message.Id);
    }

    // ── Component interaction waiting ─────────────────────────────────────────

    /// <summary>
    /// Waits for a button click on this message and returns the resulting interaction.
    /// </summary>
    /// <param name="message">The message whose buttons to listen on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose interaction to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific button to wait for, or null for any button.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> WaitForButtonAsync(
        this Message message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        // Interaction type 3 = MessageComponent; component_type 2 = Button
        const int messageComponentType = 3;
        const int buttonComponentType  = 2;

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != messageComponentType)                               return;
            if (evt.Data?.ComponentType != buttonComponentType)                 return;
            if (evt.Message?.Id != message.Id)                                  return;
            if (user is not null && GetUserId(evt) != user.Id)                  return;
            if (customId is not null && evt.Data.CustomId != customId)          return;

            tcs.TrySetResult(evt);
        }

        using var sub = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

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

    /// <summary>
    /// Waits for a select menu interaction on this message.
    /// </summary>
    /// <param name="message">The message whose select menus to listen on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose interaction to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific select menu to wait for, or null for any.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> WaitForSelectAsync(
        this Message message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        // Interaction type 3 = MessageComponent
        // component_type: 3 = StringSelect, 5 = UserSelect, 6 = RoleSelect,
        //                 7 = MentionableSelect, 8 = ChannelSelect
        const int messageComponentType = 3;

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != messageComponentType)                       return;
            if (evt.Message?.Id != message.Id)                          return;
            // Exclude buttons (component_type 2); accept all select menu types
            var ct = evt.Data?.ComponentType;
            if (ct is null or 2)                                        return;
            if (user is not null && GetUserId(evt) != user.Id)          return;
            if (customId is not null && evt.Data!.CustomId != customId) return;

            tcs.TrySetResult(evt);
        }

        using var sub = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

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

    /// <summary>
    /// Waits for a modal submission interaction.
    /// </summary>
    /// <param name="message">The message that triggered the modal (optional, for context).</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose submission to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific modal to wait for, or null for any.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<InteractionCreateEvent>> WaitForModalAsync(
        this Message? message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<InteractionCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        // Interaction type 5 = ModalSubmit
        const int modalSubmitType = 5;

        void OnInteraction(InteractionCreateEvent evt)
        {
            if (evt.Type != modalSubmitType)                                  return;
            if (user is not null && GetUserId(evt) != user.Id)                return;
            if (customId is not null && evt.Data?.CustomId != customId)        return;

            tcs.TrySetResult(evt);
        }

        using var sub = client.Gateway.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", OnInteraction);

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

    // Resolves the user ID from an interaction: guild interactions carry the user
    // inside the member object; DM interactions have the user directly.
    private static ulong GetUserId(InteractionCreateEvent evt)
        => evt.User?.Id ?? evt.Member?.User?.Id ?? 0;
}