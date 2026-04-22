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
/// Extension methods for Discord channels.
/// </summary>
public static class ChannelExtensions
{
    /// <summary>
    /// Sends a paginated message.
    /// </summary>
    /// <param name="channel">The channel to send to.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user who can control the pagination.</param>
    /// <param name="pages">The pages to paginate.</param>
    /// <param name="timeout">The timeout for the pagination.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SendPaginatedMessageAsync(
        this Channel channel,
        DiscordClient client,
        User user,
        IEnumerable<Page> pages,
        TimeSpan? timeout = null)
    {
        var pageList = pages.ToList();
        if (!pageList.Any())
            return;

        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var emojis = interactivity.PaginationEmojis;
        var behaviour = interactivity.PollBehaviour;

        var currentPage = 0;
        var message = await client.Rest.CreateMessageAsync(channel.Id, new CreateMessageRequest
        {
            Content = pageList[currentPage].Content,
            Embeds = pageList[currentPage].Embed != null ? new List<Embed> { pageList[currentPage].Embed! } : null
        });

        // Guard: if the message could not be sent (e.g. missing permissions), bail out gracefully
        if (message is null) return;

        if (pageList.Count == 1)
            return; // No need for pagination controls

        // Add all navigation reaction controls
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, emojis.SkipLeft);
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, emojis.Left);
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, emojis.Stop);
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, emojis.Right);
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, emojis.SkipRight);

        var tcs = new TaskCompletionSource<bool>();
        using var cts = new CancellationTokenSource(timeout!.Value);
        cts.Token.Register(() => tcs.TrySetResult(false));

        async Task OnReactionAdd(MessageReactionAddEvent evt)
        {
            if (evt.MessageId != message.Id || evt.UserId != user.Id)
                return;

            // Remove the user's reaction so they can click the same arrow again
            var emojiName = evt.Emoji.Name ?? string.Empty;
            try
            {
                await client.Rest.DeleteUserReactionAsync(channel.Id, message.Id, emojiName, user.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reaction cleanup failed: {ex.Message}");
            }

            var previousPage = currentPage;

            if (emojiName == emojis.Left       && currentPage > 0)                        currentPage--;
            else if (emojiName == emojis.Right  && currentPage < pageList.Count - 1)      currentPage++;
            else if (emojiName == emojis.SkipLeft  && currentPage != 0)                   currentPage = 0;
            else if (emojiName == emojis.SkipRight && currentPage != pageList.Count - 1)  currentPage = pageList.Count - 1;
            else if (emojiName == emojis.Stop)  { tcs.TrySetResult(true); return; }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unrecognised emoji in pagination: {emojiName}");
                return; // unrecognised emoji — ignore
            }

            if (currentPage == previousPage) return; // no-op (already at boundary)

            try
            {
                await client.Rest.EditMessageAsync(channel.Id, message.Id, new EditMessageRequest
                {
                    Content = pageList[currentPage].Content,
                    Embeds = pageList[currentPage].Embed != null
                        ? new List<Embed> { pageList[currentPage].Embed! }
                        : new List<Embed>()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Message edit failed: {ex.Message}");
            }
        }

        var subscription = client.Gateway.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", OnReactionAdd);

        try
        {
            await tcs.Task;
        }
        finally
        {
            subscription.Dispose();

            // Clean up navigation reactions according to the configured behaviour
            if (behaviour == PollBehaviour.DeleteEmojis)
            {
                try { await client.Rest.DeleteAllReactionsAsync(channel.Id, message.Id); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Pagination cleanup failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Waits for the next message in the channel.
    /// </summary>
    /// <param name="channel">The channel to wait in.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="predicate">The predicate to match messages.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<MessageCreateEvent>> GetNextMessageAsync(
        this Channel channel,
        DiscordClient client,
        Func<MessageCreateEvent, bool>? predicate = null,
        TimeSpan? timeout = null)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var tcs = new TaskCompletionSource<MessageCreateEvent>();
        var cts = new CancellationTokenSource(timeout.Value);

        cts.Token.Register(() => tcs.TrySetCanceled());

        void OnMessageCreate(MessageCreateEvent evt)
        {
            if (evt.ChannelId == channel.Id && (predicate == null || predicate(evt)))
            {
                tcs.TrySetResult(evt);
            }
        }

        var subscription = client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", OnMessageCreate);

        try
        {
            var message = await tcs.Task;
            return new InteractivityResult<MessageCreateEvent> { Result = message };
        }
        catch (TaskCanceledException)
        {
            return new InteractivityResult<MessageCreateEvent> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
            cts.Dispose();
        }
    }
}