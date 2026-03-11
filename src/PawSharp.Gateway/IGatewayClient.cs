#nullable enable
using System;
using System.Threading.Tasks;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway;

/// <summary>
/// Abstraction over the Discord gateway WebSocket connection.
/// Enables dependency injection and unit testing without a live WebSocket.
/// </summary>
public interface IGatewayClient
{
    /// <summary>Access the event dispatcher to subscribe to typed gateway events.</summary>
    EventDispatcher Events { get; }

    /// <summary>The current gateway connection state.</summary>
    GatewayState CurrentState { get; }

    /// <summary>The session ID from the most recent READY event. Opaque string — not a numeric snowflake.</summary>
    string? SessionId { get; }

    /// <summary>Round-trip latency from the last heartbeat–ACK pair. Null until the first ACK is received.</summary>
    TimeSpan? LastHeartbeatLatency { get; }

    /// <summary>Opens the WebSocket connection to Discord's gateway.</summary>
    Task ConnectAsync();

    /// <summary>Closes the WebSocket connection gracefully.</summary>
    Task DisconnectAsync();

    /// <summary>
    /// Updates the bot's presence/status (Opcode 3).
    /// </summary>
    /// <param name="status">Status string: "online", "idle", "dnd", or "invisible".</param>
    /// <param name="game">Optional activity/game name to display.</param>
    /// <param name="streamUrl">Optional Twitch stream URL (sets status to streaming).</param>
    Task UpdatePresenceAsync(string status, string? game = null, string? streamUrl = null);

    /// <summary>
    /// Requests guild member chunks (Opcode 8).
    /// Responses are delivered via <see cref="GuildMembersChunkEvent"/>.
    /// </summary>
    Task RequestGuildMembersAsync(ulong guildId, int limit = 0, string? query = null, bool? presences = null, ulong[]? userIds = null);

    /// <summary>
    /// Requests soundboard sounds for one or more guilds (Opcode 31).
    /// Discord responds with a <see cref="GuildSoundboardSoundsUpdateEvent"/> for each requested guild.
    /// </summary>
    Task RequestSoundboardSoundsAsync(params ulong[] guildIds);

    /// <summary>Fires when a voice state update is received from Discord.</summary>
    event Func<VoiceStateUpdateEvent, Task>? VoiceStateUpdate;

    /// <summary>Fires when a voice server update is received from Discord.</summary>
    event Func<VoiceServerUpdateEvent, Task>? VoiceServerUpdate;

    /// <summary>
    /// Sends a Voice State Update payload (Opcode 4) to join/leave/move voice channels.
    /// </summary>
    Task SendVoiceStateUpdateAsync(ulong guildId, ulong? channelId, bool selfMute, bool selfDeaf);
}
