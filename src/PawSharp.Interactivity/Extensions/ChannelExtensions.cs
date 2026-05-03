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
using PawSharp.Interactions;

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
        return await GetNextMessageAsync(channel, client, predicate, timeout, CancellationToken.None);
    }

    /// <summary>
    /// Waits for the next message in the channel with cancellation support.
    /// </summary>
    /// <param name="channel">The channel to wait in.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="predicate">The predicate to match messages.</param>
    /// <param name="timeout">The timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The interactivity result.</returns>
    public static async Task<InteractivityResult<MessageCreateEvent>> GetNextMessageAsync(
        this Channel channel,
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
        catch (OperationCanceledException)
        {
            return new InteractivityResult<MessageCreateEvent> { TimedOut = true };
        }
        finally
        {
            subscription.Dispose();
        }
    }

    /// <summary>
    /// Sends a confirmation dialog with Yes/No buttons and waits for the user's choice.
    /// </summary>
    /// <param name="channel">The channel to send the confirmation to.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="question">The question or prompt to display.</param>
    /// <param name="user">The user who can respond.</param>
    /// <param name="yesLabel">Label for the Yes button (default: "Yes").</param>
    /// <param name="noLabel">Label for the No button (default: "No").</param>
    /// <param name="timeout">Maximum time to wait for a response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Yes was clicked, False if No was clicked, or TimedOut if deadline exceeded.</returns>
    public static async Task<InteractivityResult<bool>> ConfirmAsync(
        this Channel channel,
        DiscordClient client,
        string question,
        User user,
        string yesLabel = "Yes",
        string noLabel = "No",
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        // Build Yes/No buttons
        var yesButton = new Button
        {
            CustomId = "confirm_yes",
            Label = yesLabel,
            Style = ButtonStyle.Success
        };

        var noButton = new Button
        {
            CustomId = "confirm_no",
            Label = noLabel,
            Style = ButtonStyle.Danger
        };

        var actionRow = new ActionRow
        {
            Components = new List<MessageComponent> { yesButton, noButton }
        };

        // Send confirmation message
        var request = new CreateMessageRequest
        {
            Content = question,
            Components = new List<MessageComponent> { actionRow }
        };

        var message = await client.Rest.CreateMessageAsync(channel.Id, request);
        if (message == null)
            return new InteractivityResult<bool> { TimedOut = true };

        // Wait for button click
        var result = await message.WaitForButtonAsync(client, user, timeout: timeout, cancellationToken: cancellationToken);

        if (result.TimedOut || result.Result == null)
        {
            // Clean up the buttons
            try
            {
                await client.Rest.EditMessageAsync(channel.Id, message.Id, new EditMessageRequest
                {
                    Content = $"{question}\n\n*(timed out)*",
                    Components = new List<MessageComponent>()
                });
            }
            catch { /* Best effort cleanup */ }

            return new InteractivityResult<bool> { TimedOut = true };
        }

        var confirmed = result.Result.Data?.CustomId == "confirm_yes";

        // Acknowledge the interaction to remove loading state
        await client.Rest.CreateInteractionResponseAsync(
            result.Result.Id,
            result.Result.Token,
            new InteractionResponse { Type = (int)InteractionResponseType.DeferredUpdateMessage });

        // Update message to show result and remove buttons
        try
        {
            await client.Rest.EditMessageAsync(channel.Id, message.Id, new EditMessageRequest
            {
                Content = $"{question}\n\n**{(confirmed ? "✅ Yes" : "❌ No")}**",
                Components = new List<MessageComponent>()
            });
        }
        catch { /* Best effort */ }

        return new InteractivityResult<bool> { Result = confirmed };
    }

    /// <summary>
    /// Sends a paginated message using buttons instead of reactions (modern Discord UX).
    /// This provides a cleaner experience with disabled states for boundary pages.
    /// </summary>
    /// <param name="channel">The channel to send to.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user who can control the pagination.</param>
    /// <param name="pages">The pages to paginate.</param>
    /// <param name="timeout">The timeout for the pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SendButtonPaginatedMessageAsync(
        this Channel channel,
        DiscordClient client,
        User user,
        IEnumerable<Page> pages,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var pageList = pages.ToList();
        if (!pageList.Any())
            return;

        var interactivity = InteractivityExtensions.GetExtension(client) ?? new InteractivityExtension();
        timeout ??= interactivity.Timeout;

        var currentPage = 0;
        var totalPages = pageList.Count;

        // Send initial message with buttons
        var initialRequest = new CreateMessageRequest
        {
            Content = pageList[currentPage].Content,
            Embeds = pageList[currentPage].Embed != null
                ? new List<Embed> { pageList[currentPage].Embed! }
                : null,
            Components = BuildPaginationButtons(currentPage, totalPages)
        };

        var message = await client.Rest.CreateMessageAsync(channel.Id, initialRequest);
        if (message == null) return;

        // Pagination loop
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await message.WaitForButtonAsync(
                client,
                user,
                timeout: timeout,
                cancellationToken: cancellationToken);

            if (result.TimedOut || result.Result == null)
                break;

            var customId = result.Result.Data?.CustomId;
            if (customId == null) continue;

            // Handle button clicks
            switch (customId)
            {
                case "page_first":
                    currentPage = 0;
                    break;
                case "page_prev":
                    if (currentPage > 0) currentPage--;
                    break;
                case "page_next":
                    if (currentPage < totalPages - 1) currentPage++;
                    break;
                case "page_last":
                    currentPage = totalPages - 1;
                    break;
                case "page_stop":
                    // Acknowledge and stop
                    await client.Rest.CreateInteractionResponseAsync(
                        result.Result.Id,
                        result.Result.Token,
                        new InteractionResponse { Type = (int)InteractionResponseType.DeferredUpdateMessage });

                    // Remove buttons
                    await client.Rest.EditMessageAsync(channel.Id, message.Id, new EditMessageRequest
                    {
                        Content = pageList[currentPage].Content,
                        Embeds = pageList[currentPage].Embed != null
                            ? new List<Embed> { pageList[currentPage].Embed! }
                            : null,
                        Components = new List<MessageComponent>()
                    });
                    return;
            }

            // Update message with new page and button states
            var updateResponse = new InteractionResponse
            {
                Type = (int)InteractionResponseType.UpdateMessage,
                Data = new InteractionCallbackData
                {
                    Content = pageList[currentPage].Content,
                    Embeds = pageList[currentPage].Embed != null
                        ? new List<Embed> { pageList[currentPage].Embed! }
                        : null,
                    Components = BuildPaginationButtons(currentPage, totalPages)
                }
            };

            await client.Rest.CreateInteractionResponseAsync(
                result.Result.Id,
                result.Result.Token,
                updateResponse);
        }
    }

    /// <summary>
    /// Builds pagination buttons with appropriate disabled states.
    /// </summary>
    private static List<MessageComponent> BuildPaginationButtons(int currentPage, int totalPages)
    {
        var buttons = new List<Button>
        {
            new()
            {
                CustomId = "page_first",
                Label = "⏮ First",
                Style = ButtonStyle.Secondary,
                Disabled = currentPage == 0
            },
            new()
            {
                CustomId = "page_prev",
                Label = "◀ Previous",
                Style = ButtonStyle.Secondary,
                Disabled = currentPage == 0
            },
            new()
            {
                CustomId = "page_stop",
                Label = "⏹ Stop",
                Style = ButtonStyle.Danger
            },
            new()
            {
                CustomId = "page_next",
                Label = "▶ Next",
                Style = ButtonStyle.Secondary,
                Disabled = currentPage >= totalPages - 1
            },
            new()
            {
                CustomId = "page_last",
                Label = "⏭ Last",
                Style = ButtonStyle.Secondary,
                Disabled = currentPage >= totalPages - 1
            }
        };

        return new List<MessageComponent>
        {
            new ActionRow { Components = buttons.Cast<MessageComponent>().ToList() }
        };
    }
}