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
using PawSharp.Interactivity.Validation;

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
    /// Collects reactions on the message and returns a dictionary of emoji to reaction count.
    /// </summary>
    /// <param name="message">The message to collect reactions from.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="timeout">The timeout for collecting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A dictionary mapping emoji names to their reaction counts.</returns>
    public static async Task<Dictionary<string, int>> CollectReactionsAsync(
        this Message message,
        DiscordClient client,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var reactionCounts = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var cts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        linkedCts.Token.Register(() => tcs.TrySetResult(true));

        void OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId == message.Id)
            {
                var emojiName = evt.Emoji.Name ?? string.Empty;
                if (!string.IsNullOrEmpty(emojiName))
                {
                    reactionCounts.AddOrUpdate(emojiName, 1, (_, count) => count + 1);
                }
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
        }

        return new Dictionary<string, int>(reactionCounts);
    }

    /// <summary>
    /// Waits for any of the specified reactions on the message.
    /// </summary>
    /// <param name="message">The message to wait for reactions on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose reaction to wait for.</param>
    /// <param name="emojis">The list of emojis to wait for (any match will trigger).</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<Reaction>> WaitForAnyReactionAsync(
        this Message message,
        DiscordClient client,
        User user,
        IEnumerable<string> emojis,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        InteractivityValidation.RequireNotNull(message, nameof(message));
        InteractivityValidation.RequireNotNull(client, nameof(client));
        InteractivityValidation.RequireNotNull(user, nameof(user));
        InteractivityValidation.RequireNotEmpty(emojis, nameof(emojis));

        var emojiList = emojis.ToList();

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
                emojiList.Contains(evt.Emoji.Name ?? string.Empty))
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
    /// Waits for all specified users to react with a specific emoji.
    /// </summary>
    /// <param name="message">The message to wait for reactions on.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="users">The users who must react.</param>
    /// <param name="emoji">The emoji to wait for.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result containing the list of users who reacted.</returns>
    public static async Task<InteractivityResult<List<User>>> WaitForAllReactionsAsync(
        this Message message,
        DiscordClient client,
        IEnumerable<User> users,
        string emoji,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        InteractivityValidation.RequireNotNull(message, nameof(message));
        InteractivityValidation.RequireNotNull(client, nameof(client));
        InteractivityValidation.RequireNotNullOrEmpty(emoji, nameof(emoji));
        InteractivityValidation.RequireNotEmpty(users, nameof(users));

        var userList = users.ToList();

        var userIds = userList.Select(u => u.Id).ToHashSet();
        var reactedUsers = new System.Collections.Concurrent.ConcurrentBag<User>();
        var reactedUserIds = new System.Collections.Concurrent.ConcurrentDictionary<ulong, bool>();

        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<List<User>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId == message.Id &&
                userIds.Contains(evt.UserId) &&
                evt.Emoji.Name == emoji)
            {
                if (reactedUserIds.TryAdd(evt.UserId, true))
                {
                    var user = userList.FirstOrDefault(u => u.Id == evt.UserId);
                    if (user != null)
                    {
                        reactedUsers.Add(user);

                        if (reactedUsers.Count == userList.Count)
                        {
                            tcs.TrySetResult(reactedUsers.ToList());
                        }
                    }
                }
            }
        }

        var subscription = client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            var result = await tcs.Task;
            return new InteractivityResult<List<User>> { Result = result };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<List<User>> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
        }
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
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CreatePollAsync(
        this Message message,
        DiscordClient client,
        string question,
        IEnumerable<string> options,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        InteractivityValidation.RequireNotNull(message, nameof(message));
        InteractivityValidation.RequireNotNull(client, nameof(client));
        InteractivityValidation.RequireNotNullOrEmpty(question, nameof(question));
        InteractivityValidation.RequireCountBetween(options, 2, 10, nameof(options));

        var optionList = options.ToList();

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
            await client.Rest.CreateReactionAsync(message.ChannelId, message.Id, pollEmojis[i]).ConfigureAwait(false);
        }

        // Auto-cleanup after timeout
        if (timeout.HasValue)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeout.Value, cancellationToken).ConfigureAwait(false);
                    // Clean up all reactions from this message
                    await client.Rest.DeleteAllReactionsAsync(message.ChannelId, message.Id).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation is expected
                }
                catch (Exception ex)
                {
                    // Poll cleanup failed — log and ignore
                    System.Diagnostics.Debug.WriteLine($"[MessageExtensions] Poll cleanup failed: {ex.Message}");
                }
            }, CancellationToken.None);
        }
    }

    /// <summary>
    /// Gets the results of a custom reaction poll created by CreatePollAsync.
    /// </summary>
    /// <param name="message">The message containing the poll.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="options">The poll options (must match the order used in CreatePollAsync).</param>
    /// <returns>A dictionary mapping each option to its vote count.</returns>
    public static async Task<Dictionary<string, int>> GetPollResultsAsync(
        this Message message,
        DiscordClient client,
        IEnumerable<string> options)
    {
        InteractivityValidation.RequireNotNull(message, nameof(message));
        InteractivityValidation.RequireNotNull(client, nameof(client));
        InteractivityValidation.RequireNotNull(options, nameof(options));

        var optionList = options.ToList();
        var pollEmojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
        var results = new Dictionary<string, int>();

        try
        {
            // Get the message with reactions
            var updatedMessage = await client.Rest.GetMessageAsync(message.ChannelId, message.Id).ConfigureAwait(false);
            if (updatedMessage?.Reactions == null)
            {
                // Initialize all options with 0 votes if no reactions
                foreach (var option in optionList)
                {
                    results[option] = 0;
                }
                return results;
            }

            // Map emojis to vote counts
            for (int i = 0; i < optionList.Count && i < pollEmojis.Length; i++)
            {
                var emoji = pollEmojis[i];
                var reaction = updatedMessage.Reactions.FirstOrDefault(r => r.Emoji?.Name == emoji);
                results[optionList[i]] = reaction?.Count ?? 0;
            }
        }
        catch (Exception ex)
        {
            // Log and return empty results on error
            System.Diagnostics.Debug.WriteLine($"GetPollResultsAsync failed: {ex.Message}");
            foreach (var option in optionList)
            {
                results[option] = 0;
            }
        }

        return results;
    }

    /// <summary>
    /// Gets the results of a custom reaction poll with voter information.
    /// </summary>
    /// <param name="message">The message containing the poll.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="options">The poll options (must match the order used in CreatePollAsync).</param>
    /// <returns>A dictionary mapping each option to the list of users who voted for it.</returns>
    public static async Task<Dictionary<string, List<User>>> GetPollVotersAsync(
        this Message message,
        DiscordClient client,
        IEnumerable<string> options)
    {
        var optionList = options.ToList();
        var pollEmojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
        var results = new Dictionary<string, List<User>>();

        try
        {
            for (int i = 0; i < optionList.Count && i < pollEmojis.Length; i++)
            {
                var emoji = pollEmojis[i];
                var voters = new List<User>();

                try
                {
                    // Get users who reacted with this emoji
                    var reactionUsers = await client.Rest.GetReactionsAsync(message.ChannelId, message.Id, emoji).ConfigureAwait(false);
                    if (reactionUsers != null)
                    {
                        voters.AddRange(reactionUsers);
                    }
                }
                catch (Exception)
                {
                    // Failed to get voters for this option — safe to ignore
                }

                results[optionList[i]] = voters;
            }
        }
        catch (Exception)
        {
            // Return empty results on error
            foreach (var option in optionList)
            {
                results[option] = new List<User>();
            }
        }

        return results;
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

        return await client.Rest.EndPollAsync(message.ChannelId, message.Id).ConfigureAwait(false);
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

    /// <summary>
    /// Waits for the next message in the same channel as this message.
    /// </summary>
    /// <param name="message">The message to get the channel from.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="predicate">The predicate to match messages.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<MessageCreateEvent>> WaitForMessageAsync(
        this Message message,
        DiscordClient client,
        Func<MessageCreateEvent, bool>? predicate = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<MessageCreateEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        void OnMessageCreate(MessageCreateEvent evt)
        {
            if (evt.ChannelId == message.ChannelId && (predicate == null || predicate(evt)))
            {
                tcs.TrySetResult(evt);
            }
        }

        var subscription = client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", OnMessageCreate);

        try
        {
            var msg = await tcs.Task;
            return new InteractivityResult<MessageCreateEvent> { Result = msg };
        }
        catch (OperationCanceledException)
        {
            return new InteractivityResult<MessageCreateEvent> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
        }
    }

    // Resolves the user ID from an interaction: guild interactions carry the user
    // inside the member object; DM interactions have the user directly.
    private static ulong GetUserId(InteractionCreateEvent evt)
        => evt.User?.Id ?? evt.Member?.User?.Id ?? 0;

    // ── Components V2 Modal Component Waiters ─────────────────────────────────

    /// <summary>
    /// Waits for a RadioGroup component submission from a modal.
    /// Note: This is handled by WaitForModalAsync, but this method provides a more ergonomic interface.
    /// </summary>
    /// <param name="message">The message that triggered the modal (optional, for context).</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose submission to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific RadioGroup to wait for, or null to accept any RadioGroup.</param>
    /// <param name="timeout">The maximum time to wait. Falls back to InteractivityExtension.Timeout if not specified.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An InteractivityResult wrapping the selected RadioGroup value, or a timed-out result after the deadline.</returns>
    public static async Task<InteractivityResult<string?>> WaitForRadioGroupAsync(
        this Message? message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // RadioGroup is a modal component, so we use WaitForModalAsync and extract the value
        var result = await WaitForModalAsync(message, client, user, customId, timeout, cancellationToken).ConfigureAwait(false);

        if (result.TimedOut || result.Result == null)
            return new InteractivityResult<string?> { TimedOut = true };

        // Extract RadioGroup value from modal submission
        // Component type 21 = RadioGroup
        var value = ExtractComponentValue(result.Result.Data?.Components?.Cast<object>().ToList(), 21, customId);
        return new InteractivityResult<string?> { Result = value };
    }

    /// <summary>
    /// Waits for a CheckboxGroup component submission from a modal.
    /// Note: This is handled by WaitForModalAsync, but this method provides a more ergonomic interface.
    /// </summary>
    /// <param name="message">The message that triggered the modal (optional, for context).</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose submission to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific CheckboxGroup to wait for, or null to accept any CheckboxGroup.</param>
    /// <param name="timeout">The maximum time to wait. Falls back to InteractivityExtension.Timeout if not specified.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An InteractivityResult wrapping the list of selected CheckboxGroup values, or a timed-out result after the deadline.</returns>
    public static async Task<InteractivityResult<List<string>>> WaitForCheckboxGroupAsync(
        this Message? message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // CheckboxGroup is a modal component, so we use WaitForModalAsync and extract the values
        var result = await WaitForModalAsync(message, client, user, customId, timeout, cancellationToken).ConfigureAwait(false);

        if (result.TimedOut || result.Result == null)
            return new InteractivityResult<List<string>> { TimedOut = true };

        // Extract CheckboxGroup values from modal submission
        // Component type 22 = CheckboxGroup
        var values = ExtractComponentValues(result.Result.Data?.Components?.Cast<object>().ToList(), 22, customId) ?? new List<string>();
        return new InteractivityResult<List<string>> { Result = values };
    }

    /// <summary>
    /// Waits for a Checkbox component submission from a modal.
    /// Note: This is handled by WaitForModalAsync, but this method provides a more ergonomic interface.
    /// </summary>
    /// <param name="message">The message that triggered the modal (optional, for context).</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user whose submission to accept, or null to accept any user.</param>
    /// <param name="customId">The custom_id of the specific Checkbox to wait for, or null to accept any Checkbox.</param>
    /// <param name="timeout">The maximum time to wait. Falls back to InteractivityExtension.Timeout if not specified.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An InteractivityResult wrapping the Checkbox value, or a timed-out result after the deadline.</returns>
    public static async Task<InteractivityResult<bool?>> WaitForCheckboxAsync(
        this Message? message,
        DiscordClient client,
        User? user = null,
        string? customId = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        // Checkbox is a modal component, so we use WaitForModalAsync and extract the value
        var result = await WaitForModalAsync(message, client, user, customId, timeout, cancellationToken).ConfigureAwait(false);

        if (result.TimedOut || result.Result == null)
            return new InteractivityResult<bool?> { TimedOut = true };

        // Extract Checkbox value from modal submission
        // Component type 23 = Checkbox
        var value = ExtractCheckboxValue(result.Result.Data?.Components?.Cast<object>().ToList(), 23, customId);
        return new InteractivityResult<bool?> { Result = value };
    }

    // Helper methods for extracting component values from modal submissions
    private static string? ExtractComponentValue(List<object>? components, int componentType, string? customId)
    {
        if (components == null) return null;

        foreach (var component in components)
        {
            if (component is System.Text.Json.JsonElement jsonElement)
            {
                // Navigate through Label wrapper (type 18) to find the actual component
                var actualComponent = jsonElement;
                if (jsonElement.TryGetProperty("type", out var type) && type.GetInt32() == 18)
                {
                    // This is a Label, get the inner component
                    if (jsonElement.TryGetProperty("component", out var inner))
                    {
                        actualComponent = inner;
                    }
                }

                if (actualComponent.TryGetProperty("type", out var compType) && compType.GetInt32() == componentType)
                {
                    if (customId == null || (actualComponent.TryGetProperty("custom_id", out var compCustomId) && compCustomId.GetString() == customId))
                    {
                        if (actualComponent.TryGetProperty("value", out var value))
                        {
                            return value.GetString();
                        }
                    }
                }
            }
        }
        return null;
    }

    private static List<string>? ExtractComponentValues(List<object>? components, int componentType, string? customId)
    {
        if (components == null) return null;

        foreach (var component in components)
        {
            if (component is System.Text.Json.JsonElement jsonElement)
            {
                // Navigate through Label wrapper (type 18) to find the actual component
                var actualComponent = jsonElement;
                if (jsonElement.TryGetProperty("type", out var type) && type.GetInt32() == 18)
                {
                    // This is a Label, get the inner component
                    if (jsonElement.TryGetProperty("component", out var inner))
                    {
                        actualComponent = inner;
                    }
                }

                if (actualComponent.TryGetProperty("type", out var compType) && compType.GetInt32() == componentType)
                {
                    if (customId == null || (actualComponent.TryGetProperty("custom_id", out var compCustomId) && compCustomId.GetString() == customId))
                    {
                        if (actualComponent.TryGetProperty("values", out var values))
                        {
                            var result = new List<string>();
                            foreach (var value in values.EnumerateArray())
                            {
                                result.Add(value.GetString()!);
                            }
                            return result;
                        }
                    }
                }
            }
        }
        return null;
    }

    private static bool? ExtractCheckboxValue(List<object>? components, int componentType, string? customId)
    {
        if (components == null) return null;

        foreach (var component in components)
        {
            if (component is System.Text.Json.JsonElement jsonElement)
            {
                // Navigate through Label wrapper (type 18) to find the actual component
                var actualComponent = jsonElement;
                if (jsonElement.TryGetProperty("type", out var type) && type.GetInt32() == 18)
                {
                    // This is a Label, get the inner component
                    if (jsonElement.TryGetProperty("component", out var inner))
                    {
                        actualComponent = inner;
                    }
                }

                if (actualComponent.TryGetProperty("type", out var compType) && compType.GetInt32() == componentType)
                {
                    if (customId == null || (actualComponent.TryGetProperty("custom_id", out var compCustomId) && compCustomId.GetString() == customId))
                    {
                        if (actualComponent.TryGetProperty("value", out var value))
                        {
                            return value.GetBoolean();
                        }
                    }
                }
            }
        }
        return null;
    }
}