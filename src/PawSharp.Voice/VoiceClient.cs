#nullable enable
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
    private readonly ConcurrentDictionary<ulong, VoiceConnection> _connections = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceClient"/> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public VoiceClient(DiscordClient discordClient)
    {
        _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));

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
        var connection = new VoiceConnection(_discordClient, channel);
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