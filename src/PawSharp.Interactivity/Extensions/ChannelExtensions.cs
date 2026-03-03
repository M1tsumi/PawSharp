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

        var interactivity = new InteractivityExtension();
        timeout ??= interactivity.Timeout;

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

        // Add reaction controls
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, "◀");
        await client.Rest.CreateReactionAsync(channel.Id, message.Id, "▶");

        // Capture the delay value now — timeout is guaranteed non-null after the ??= above,
        // but the compiler cannot verify this through a lambda closure capture.
        var paginationDelay = timeout!.Value;

        // Handle reactions (simplified - would need event handling)
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(paginationDelay);

                // Clean up reactions
                try
                {
                    // await client.Rest.DeleteAllReactionsAsync(channel.Id, message.Id); // TODO: Implement when API supports it
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch
            {
                // Ignore timeout errors
            }
        });
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
        var interactivity = new InteractivityExtension();
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

        client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", OnMessageCreate);

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
            // Note: EventDispatcher doesn't have Remove method, handlers are persistent
            cts.Dispose();
        }
    }
}