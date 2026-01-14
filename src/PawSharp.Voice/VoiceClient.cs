#nullable enable
using System;
using System.Collections.Concurrent;
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
public class VoiceClient
{
    private readonly DiscordClient _discordClient;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<ulong, VoiceConnection> _connections = new();
    private readonly ConcurrentDictionary<ulong, ReconnectionState> _reconnectionStates = new();

    private const int MaxReconnectionAttempts = 5;
    private const int InitialBackoffMs = 1000;
    private const int MaxBackoffMs = 30000;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceClient"/> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="logger">The logger.</param>
    public VoiceClient(DiscordClient discordClient, ILogger logger = null)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        // Subscribe to voice events
        _discordClient.Gateway.VoiceStateUpdate += OnVoiceStateUpdate;
        _discordClient.Gateway.VoiceServerUpdate += OnVoiceServerUpdate;
    }

    /// <summary>
    /// Connects to a voice channel.
    /// </summary>
    /// <param name="channel">The voice channel to connect to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task<VoiceConnection> ConnectAsync(Channel channel)
    {
        if (channel.Type != ChannelType.GuildVoice && channel.Type != ChannelType.GuildStageVoice)
            throw new ArgumentException("Channel must be a voice channel.", nameof(channel));

        var guildId = channel.GuildId ?? throw new ArgumentException("Channel must be in a guild.", nameof(channel));

        // Send voice state update to join channel
        await _discordClient.Gateway.SendVoiceStateUpdateAsync(guildId, channel.Id, false, false);

        // Create connection
        var connection = new VoiceConnection(_discordClient, channel, channelId => _ = HandleConnectionFailureAsync(channelId));
        _connections[channel.Id] = connection;

        // Connect to voice
        await connection.ConnectAsync();

        return connection;
    }

    /// <summary>
    /// Disconnects from a voice channel.
    /// </summary>
    /// <param name="channel">The voice channel to disconnect from.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync(Channel channel)
    {
        if (_connections.TryRemove(channel.Id, out var connection))
        {
            await connection.DisconnectAsync();
            _reconnectionStates.TryRemove(channel.Id, out _);
        }
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

        if (state.IsReconnecting || state.Attempts >= MaxReconnectionAttempts)
        {
            _logger.LogWarning("Voice reconnection failed for channel {ChannelId} after {Attempts} attempts", channelId, state.Attempts);
            await DisconnectAsync(connection.Channel);
            return;
        }

        state.IsReconnecting = true;
        state.Attempts++;
        state.CurrentBackoffMs = Math.Min(state.CurrentBackoffMs == 0 ? InitialBackoffMs : state.CurrentBackoffMs * 2, MaxBackoffMs);
        state.LastAttempt = DateTime.UtcNow;

        _logger.LogInformation("Attempting voice reconnection for channel {ChannelId}, attempt {Attempt}/{MaxAttempts}, backoff {Backoff}ms",
            channelId, state.Attempts, MaxReconnectionAttempts, state.CurrentBackoffMs);

        await Task.Delay(state.CurrentBackoffMs);

        try
        {
            // Attempt to reconnect
            await connection.ConnectAsync();
            state = new ReconnectionState(); // Reset on success
            _reconnectionStates[channelId] = state;
            _logger.LogInformation("Voice reconnection successful for channel {ChannelId}", channelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice reconnection failed for channel {ChannelId}", channelId);
            state.IsReconnecting = false;
            await HandleConnectionFailureAsync(channelId); // Recursive retry
        }
    }

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

    private async Task OnVoiceStateUpdate(VoiceStateUpdateEvent evt)
    {
        // Handle voice state updates
        if (evt.ChannelId.HasValue && _connections.TryGetValue(evt.ChannelId.Value, out var connection))
        {
            connection.UpdateVoiceState(evt);
        }
        await Task.CompletedTask;
    }

    private async Task OnVoiceServerUpdate(VoiceServerUpdateEvent evt)
    {
        // Handle voice server updates
        foreach (var connection in _connections.Values)
        {
            if (connection.GuildId == evt.GuildId)
            {
                connection.UpdateVoiceServer(evt);
                break;
            }
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