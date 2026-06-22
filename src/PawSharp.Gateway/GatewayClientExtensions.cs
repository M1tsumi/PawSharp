#nullable enable
using System;
using System.Threading.Tasks;
using PawSharp.Core.Models;

namespace PawSharp.Gateway;

/// <summary>
/// Extension methods for GatewayClient to provide convenience methods for common operations.
/// </summary>
public static class GatewayClientExtensions
{
    // Presence convenience methods

    /// <summary>
    /// Sets the bot's presence to online.
    /// </summary>
    public static Task SetOnlineAsync(this IGatewayClient client, string? activityName = null)
    {
        return client.UpdatePresenceAsync("online", activityName);
    }

    /// <summary>
    /// Sets the bot's presence to idle.
    /// </summary>
    public static Task SetIdleAsync(this IGatewayClient client, string? activityName = null)
    {
        return client.UpdatePresenceAsync("idle", activityName);
    }

    /// <summary>
    /// Sets the bot's presence to Do Not Disturb.
    /// </summary>
    public static Task SetDndAsync(this IGatewayClient client, string? activityName = null)
    {
        return client.UpdatePresenceAsync("dnd", activityName);
    }

    /// <summary>
    /// Sets the bot's presence to invisible.
    /// </summary>
    public static Task SetInvisibleAsync(this IGatewayClient client)
    {
        return client.UpdatePresenceAsync("invisible");
    }

    /// <summary>
    /// Sets the bot's presence to "Playing" activity.
    /// </summary>
    public static Task SetPlayingAsync(this IGatewayClient client, string game)
    {
        return client.UpdatePresenceAsync("online", game);
    }

    /// <summary>
    /// Sets the bot's presence to "Watching" activity.
    /// </summary>
    public static Task SetWatchingAsync(this IGatewayClient client, string activity)
    {
        return client.UpdatePresenceAsync("online", activity);
    }

    /// <summary>
    /// Sets the bot's presence to "Listening to" activity.
    /// </summary>
    public static Task SetListeningAsync(this IGatewayClient client, string activity)
    {
        return client.UpdatePresenceAsync("online", activity);
    }

    /// <summary>
    /// Sets the bot's presence to "Streaming" activity.
    /// </summary>
    public static Task SetStreamingAsync(this IGatewayClient client, string game, string streamUrl)
    {
        return client.UpdatePresenceAsync("online", game, streamUrl);
    }

    /// <summary>
    /// Sets the bot's presence to "Competing in" activity.
    /// </summary>
    public static Task SetCompetingAsync(this IGatewayClient client, string activity)
    {
        return client.UpdatePresenceAsync("online", activity);
    }

    // Connection state convenience methods

    /// <summary>
    /// Checks if the gateway is currently connected.
    /// </summary>
    public static bool IsConnected(this IGatewayClient client)
    {
        return client.CurrentState == GatewayState.Connected || client.CurrentState == GatewayState.Ready;
    }

    /// <summary>
    /// Checks if the gateway is currently ready to receive events.
    /// </summary>
    public static bool IsReady(this IGatewayClient client)
    {
        return client.CurrentState == GatewayState.Ready;
    }

    /// <summary>
    /// Checks if the gateway is currently disconnected.
    /// </summary>
    public static bool IsDisconnected(this IGatewayClient client)
    {
        return client.CurrentState == GatewayState.Disconnected;
    }

    /// <summary>
    /// Checks if the gateway is currently connecting.
    /// </summary>
    public static bool IsConnecting(this IGatewayClient client)
    {
        return client.CurrentState == GatewayState.Connecting;
    }

    /// <summary>
    /// Checks if the gateway is currently in a failed state.
    /// </summary>
    public static bool IsFailed(this IGatewayClient client)
    {
        return client.CurrentState == GatewayState.Failed;
    }

    /// <summary>
    /// Waits for the gateway to reach the Ready state.
    /// </summary>
    /// <param name="client">The gateway client</param>
    /// <param name="timeout">Maximum time to wait (default: 30 seconds)</param>
    /// <returns>True if the gateway became ready, false if timeout occurred</returns>
    public static async Task<bool> WaitForReadyAsync(this IGatewayClient client, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (client.CurrentState != GatewayState.Ready)
        {
            if (DateTime.UtcNow - startTime > effectiveTimeout)
            {
                return false;
            }

            if (client.CurrentState == GatewayState.Failed)
            {
                return false;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        return true;
    }

    // Guild member request convenience methods

    /// <summary>
    /// Requests all members of a guild.
    /// </summary>
    public static Task RequestAllGuildMembersAsync(this IGatewayClient client, ulong guildId, bool presences = false)
    {
        return client.RequestGuildMembersAsync(guildId, 0, "", presences);
    }

    /// <summary>
    /// Requests specific members of a guild by user IDs.
    /// </summary>
    public static Task RequestGuildMembersAsync(this IGatewayClient client, ulong guildId, params ulong[] userIds)
    {
        return client.RequestGuildMembersAsync(guildId, userIds.Length, userIds: userIds);
    }

    /// <summary>
    /// Requests guild members matching a query.
    /// </summary>
    public static Task RequestGuildMembersAsync(this IGatewayClient client, ulong guildId, string query, int limit = 100, bool presences = false)
    {
        return client.RequestGuildMembersAsync(guildId, limit, query, presences);
    }

    // Voice state convenience methods

    /// <summary>
    /// Joins a voice channel.
    /// </summary>
    public static Task JoinVoiceChannelAsync(this IGatewayClient client, ulong guildId, ulong channelId)
    {
        return client.SendVoiceStateUpdateAsync(guildId, channelId, false, false);
    }

    /// <summary>
    /// Joins a voice channel muted.
    /// </summary>
    public static Task JoinVoiceChannelMutedAsync(this IGatewayClient client, ulong guildId, ulong channelId)
    {
        return client.SendVoiceStateUpdateAsync(guildId, channelId, true, false);
    }

    /// <summary>
    /// Joins a voice channel deafened.
    /// </summary>
    public static Task JoinVoiceChannelDeafenedAsync(this IGatewayClient client, ulong guildId, ulong channelId)
    {
        return client.SendVoiceStateUpdateAsync(guildId, channelId, false, true);
    }

    /// <summary>
    /// Leaves a voice channel.
    /// </summary>
    public static Task LeaveVoiceChannelAsync(this IGatewayClient client, ulong guildId)
    {
        return client.SendVoiceStateUpdateAsync(guildId, null, false, false);
    }

    /// <summary>
    /// Moves to a different voice channel.
    /// </summary>
    public static Task MoveVoiceChannelAsync(this IGatewayClient client, ulong guildId, ulong channelId)
    {
        return client.SendVoiceStateUpdateAsync(guildId, channelId, false, false);
    }
}
