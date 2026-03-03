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
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<Reaction>> WaitForReactionAsync(
        this Message message,
        DiscordClient client,
        User user,
        string? emoji = null,
        TimeSpan? timeout = null)
    {
        var interactivity = new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<Reaction>();
        var cts = new CancellationTokenSource(timeout.Value);

        cts.Token.Register(() => tcs.TrySetCanceled());

        void OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId == message.Id &&
                evt.UserId == user.Id &&
                (emoji == null || evt.Emoji.Name == emoji))
            {
                var reaction = new Reaction
                {
                    Count = 1,
                    Me = false, // TODO: Check if current user reacted
                    Emoji = evt.Emoji
                };
                tcs.TrySetResult(reaction);
            }
        }

        client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            var reaction = await tcs.Task;
            return new InteractivityResult<Reaction> { Result = reaction };
        }
        catch (TaskCanceledException)
        {
            return new InteractivityResult<Reaction> { TimedOut = true };
        }
        finally
        {
            // Subscription is automatically cleaned up when the CancellationTokenSource fires
            cts.Dispose();
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
        var interactivity = new InteractivityExtension();
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
                    Me = false,
                    Emoji = evt.Emoji
                };
                reactions.Add(reaction);
            }
        }

        client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            await tcs.Task;
        }
        finally
        {
            cts.Dispose();
        }

        return reactions;
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
                await Task.Delay(timeout.Value);
                // Clean up all reactions from this message
                await client.Rest.DeleteAllReactionsAsync(message.ChannelId, message.Id);
            });
        }
    }
}