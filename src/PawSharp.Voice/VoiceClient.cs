#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;
using PawSharp.Gateway.Events;

namespace PawSharp.Voice;

/// <summary>
/// Main voice client for handling Discord voice connections.
/// </summary>
public class VoiceClient : IDisposable
{
    private readonly DiscordClient _discordClient;
    private readonly ILogger _logger;
    private bool _disposed;
    private readonly ConcurrentDictionary<ulong, VoiceConnection> _connections = new();
    private readonly ConcurrentDictionary<ulong, ReconnectionState> _reconnectionStates = new();

    // guildId → sessionId, populated from VOICE_STATE_UPDATE before VOICE_SERVER_UPDATE arrives
    private readonly ConcurrentDictionary<ulong, string> _pendingSessions = new();

    private const int MaxReconnectionAttempts = 5;
    private const int InitialBackoffMs = 1000;
    private const int MaxBackoffMs = 30000;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceClient"/> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="logger">The logger.</param>
    public VoiceClient(DiscordClient discordClient, ILogger? logger = null)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        // Subscribe to voice events
        _discordClient.Gateway.VoiceStateUpdate += OnVoiceStateUpdate;
        _discordClient.Gateway.VoiceServerUpdate += OnVoiceServerUpdate;
    }

    /// <summary>
    /// Connects to a voice channel. The actual WebSocket connection is completed
    /// asynchronously when Discord sends VOICE_SERVER_UPDATE.
    /// </summary>
    /// <param name="channel">The voice channel to connect to.</param>
    /// <param name="options">Configuration options for the voice connection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<VoiceConnection> ConnectAsync(Channel channel, VoiceConnectionOptions? options = null)
    {
        if (channel.Type != ChannelType.GuildVoice && channel.Type != ChannelType.GuildStageVoice)
            throw new ArgumentException("Channel must be a voice channel.", nameof(channel));

        var guildId = channel.GuildId ?? throw new ArgumentException("Channel must be in a guild.", nameof(channel));

        // Create and register the connection object — actual WebSocket connect
        // happens in OnVoiceServerUpdate when Discord provides the server endpoint.
        var connection = new VoiceConnection(
            _discordClient,
            channel,
            channelId => _ = HandleConnectionFailureAsync(channelId),
            _logger,
            options);
        _connections[channel.Id] = connection;

        // Send op4 — Discord will reply with VOICE_STATE_UPDATE then VOICE_SERVER_UPDATE
        await _discordClient.Gateway.SendVoiceStateUpdateAsync(guildId, channel.Id, false, false);

        return connection;
    }

    /// <summary>
    /// Disconnects from a voice channel.
    /// </summary>
    /// <param name="channel">The voice channel to disconnect from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync(Channel channel)
    {
        var guildId = channel.GuildId;
        if (_connections.TryRemove(channel.Id, out var connection))
        {
            await connection.DisconnectAsync();
            _reconnectionStates.TryRemove(channel.Id, out _);
        }

        // Send op4 with channel_id=null so Discord removes the bot from the voice channel
        if (guildId.HasValue)
            await _discordClient.Gateway.SendVoiceStateUpdateAsync(guildId.Value, null, false, false);
    }

    /// <summary>
    /// Handles voice connection failure and attempts reconnection.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task HandleConnectionFailureAsync(ulong channelId)
    {
        if (!_connections.TryGetValue(channelId, out var connection))
            return;

        var state = _reconnectionStates.GetOrAdd(channelId, _ => new ReconnectionState());

        bool shouldProceed;
        lock (state)
        {
            if (state.IsReconnecting || state.Attempts >= MaxReconnectionAttempts)
            {
                shouldProceed = false;
            }
            else
            {
                state.IsReconnecting = true;
                state.Attempts++;
                state.CurrentBackoffMs = Math.Min(state.CurrentBackoffMs == 0 ? InitialBackoffMs : state.CurrentBackoffMs * 2, MaxBackoffMs);
                state.LastAttempt = DateTime.UtcNow;
                shouldProceed = true;
            }
        }

        if (!shouldProceed)
        {
            _logger.LogWarning("Voice reconnection failed for channel {ChannelId} after {Attempts} attempts", channelId, state.Attempts);
            await DisconnectAsync(connection.Channel);
            return;
        }

        _logger.LogInformation("Attempting voice reconnection for channel {ChannelId}, attempt {Attempt}/{MaxAttempts}, backoff {Backoff}ms",
            channelId, state.Attempts, MaxReconnectionAttempts, state.CurrentBackoffMs);

        await Task.Delay(state.CurrentBackoffMs);

        try
        {
            await connection.ReconnectAsync();
            state = new ReconnectionState();
            _reconnectionStates[channelId] = state;
            _logger.LogInformation("Voice reconnection successful for channel {ChannelId}", channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice reconnection failed for channel {ChannelId}", channelId);
            state.IsReconnecting = false;
        }
    }

    /// <summary>
    /// Gets all currently active voice connections keyed by channel ID.
    /// </summary>
    public IReadOnlyDictionary<ulong, VoiceConnection> ActiveConnections => _connections;

    /// <summary>
    /// Gets the voice connection for a channel.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The voice connection, or null if not connected.</returns>
    public VoiceConnection? GetConnection(ulong channelId)
    {
        _connections.TryGetValue(channelId, out var connection);
        return connection;
    }

    /// <summary>
    /// Unsubscribes gateway event handlers to prevent delegate leaks.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _discordClient.Gateway.VoiceStateUpdate -= OnVoiceStateUpdate;
        _discordClient.Gateway.VoiceServerUpdate -= OnVoiceServerUpdate;
    }

    private async Task OnVoiceStateUpdate(VoiceStateUpdateEvent evt)
    {
        // Capture the bot's own session_id from its VSU events.
        var botUserId = _discordClient.CurrentUser?.Id;
        if (evt.GuildId.HasValue && botUserId.HasValue && evt.UserId == botUserId.Value)
        {
            _pendingSessions[evt.GuildId.Value] = evt.SessionId;
            _logger.LogDebug("Captured voice session_id for guild {GuildId}", evt.GuildId.Value);
        }

        if (evt.ChannelId.HasValue && _connections.TryGetValue(evt.ChannelId.Value, out var connection))
        {
            connection.UpdateVoiceState(evt);
        }
        await Task.CompletedTask;
    }

    private async Task OnVoiceServerUpdate(VoiceServerUpdateEvent evt)
    {
        // Find the connection for this guild
        VoiceConnection? target = null;
        foreach (var conn in _connections.Values)
        {
            if (conn.GuildId == evt.GuildId)
            {
                target = conn;
                break;
            }
        }

        if (target is null)
        {
            _logger.LogDebug("Received VOICE_SERVER_UPDATE for guild {GuildId} but no pending connection found", evt.GuildId);
            await Task.CompletedTask;
            return;
        }

        if (!_pendingSessions.TryGetValue(evt.GuildId, out var sessionId))
        {
            // VOICE_SERVER_UPDATE may arrive before VOICE_STATE_UPDATE has stored the
            // session_id. Retry a few times with a short delay before giving up.
            const int retries = 5;
            const int retryDelayMs = 200;
            for (int i = 0; i < retries && !_pendingSessions.TryGetValue(evt.GuildId, out sessionId); i++)
                await Task.Delay(retryDelayMs);

            if (sessionId is null)
            {
                _logger.LogWarning("Received VOICE_SERVER_UPDATE for guild {GuildId} but no session_id is available yet", evt.GuildId);
                await Task.CompletedTask;
                return;
            }
        }

        var botUserId = _discordClient.CurrentUser?.Id ?? 0UL;

        _logger.LogInformation("Initiating voice WebSocket connection for guild {GuildId}", evt.GuildId);

        try
        {
            await target.ConnectAsync(evt.Endpoint, evt.GuildId, botUserId, sessionId, evt.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect voice WebSocket for guild {GuildId}", evt.GuildId);
        }
    }
}

internal class ReconnectionState
{
    public int Attempts { get; set; }
    public int CurrentBackoffMs { get; set; }
    public bool IsReconnecting { get; set; }
    public DateTime? LastAttempt { get; set; }
}