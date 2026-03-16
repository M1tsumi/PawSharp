#nullable enable
using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Models;
using PawSharp.Core.Metrics;
using PawSharp.Core.Serialization;
using PawSharp.Gateway.Connection;
using PawSharp.Gateway.Events;
using PawSharp.Gateway.Heartbeat;

namespace PawSharp.Gateway
{
    public class GatewayClient : IGatewayClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger _logger;
        private readonly IPerformanceMetrics? _metrics;
        private readonly WebSocketConnection _webSocket;
        private HeartbeatManager _heartbeatManager;
        private readonly EventDispatcher _eventDispatcher;
        private readonly ReconnectionManager _reconnectionManager;
        private CancellationTokenSource? _cts;
        private Task? _receiveTask;
        
        private GatewayState _currentState = GatewayState.Disconnected;
        /// <remarks>
        /// Discord session IDs are opaque hex-like strings (e.g. "abc123...").
        /// They must be stored as <see cref="string"/>, not a numeric snowflake.
        /// </remarks>
        private string? _resumeSessionId;
        private int? _resumeSequence;
        /// <summary>
        /// The résumé gateway URL sent by Discord in the READY payload.
        /// Must be used instead of the default gateway URL when reconnecting/resuming,
        /// per Discord API documentation.
        /// </summary>
        private string? _resumeGatewayUrl;
        private DateTimeOffset? _lastHeartbeatSent;
        private TimeSpan? _lastHeartbeatLatency;

        // Discord allows 120 gateway commands per 60-second sliding window (per-connection).
        // Heartbeat opcodes are exempt from this limit.
        // SemaphoreSlim token-bucket: each acquired token is returned after 60 s.
        private readonly SemaphoreSlim _wsRateLimiter = new(120, 120);

        // Options shared by any manual deserialisation that happens outside EventDispatcher
        // (e.g. VoiceStateUpdate / VoiceServerUpdate mirror-events).
        private static readonly JsonSerializerOptions _snowflakeOptions = new()
        {
            Converters = { new SnowflakeJsonConverter(), new NullableSnowflakeJsonConverter() }
        };

        /// <summary>
        /// Fired when the gateway state changes.
        /// </summary>
        public event Func<GatewayState, GatewayState, Task>? OnStateChanged;

        /// <summary>
        /// Fired when reconnection is about to be attempted.
        /// </summary>
        public event Func<int, Task>? OnReconnectionAttempt;

        /// <summary>
        /// Fired when reconnection has failed after all attempts.
        /// </summary>
        public event Func<Task>? OnReconnectionFailed;

        /// <summary>
        /// Fired when a voice state update is received.
        /// </summary>
        public event Func<VoiceStateUpdateEvent, Task>? VoiceStateUpdate;

        /// <summary>
        /// Fired when a voice server update is received.
        /// </summary>
        public event Func<VoiceServerUpdateEvent, Task>? VoiceServerUpdate;

        /// <summary>
        /// Fired when identify fails.
        /// </summary>
        public event Func<string, Task>? OnIdentifyFailed;

        /// <summary>
        /// Fired when resume fails.
        /// </summary>
        public event Func<string, Task>? OnResumeFailed;

        public GatewayClient(PawSharpOptions options, ILogger logger, IPerformanceMetrics? metrics = null)
        {
            _options = options;
            _logger = logger;
            _metrics = metrics;
            _webSocket = new WebSocketConnection(_options.EnableCompression);
            _heartbeatManager = new HeartbeatManager(41250, SendHeartbeatAsync, logger, _options.MaxMissedHeartbeatAcks);
            _eventDispatcher = new EventDispatcher(logger);
            _reconnectionManager = new ReconnectionManager(logger, metrics);
            
            _reconnectionManager.OnReconnectionAttempt += async (attempt) =>
            {
                OnReconnectionAttempt?.Invoke(attempt);
                await Task.CompletedTask;
            };
            _reconnectionManager.OnReconnectionFailed += async () =>
            {
                await SetStateAsync(GatewayState.Failed);
                OnReconnectionFailed?.Invoke();
            };

            _heartbeatManager.OnZombieConnection += async () =>
            {
                _logger.LogError("Zombie connection detected - reconnecting...");
                await ReconnectAsync();
            };
        }

        /// <summary>
        /// Access the event dispatcher to register event handlers.
        /// </summary>
        public EventDispatcher Events => _eventDispatcher;

        /// <summary>
        /// Get the current gateway connection state.
        /// </summary>
        public GatewayState CurrentState => _currentState;

        /// <inheritdoc/>
        public string? SessionId => _resumeSessionId;

        /// <inheritdoc/>
        public TimeSpan? LastHeartbeatLatency => _lastHeartbeatLatency;

        public async Task ConnectAsync()
        {
            if (_currentState != GatewayState.Disconnected)
            {
                _logger.LogWarning("Cannot connect - already in state {State}", _currentState);
                return;
            }

            await SetStateAsync(GatewayState.Connecting);
            _cts = new CancellationTokenSource();

            // Discord requires using resume_gateway_url (from the most recent READY) when
            // reconnecting to resume a session.  Fall back to the canonical gateway URL for
            // fresh connections.
            var gatewayHost = (_resumeSessionId is not null && _resumeGatewayUrl is not null)
                ? _resumeGatewayUrl
                : "wss://gateway.discord.gg";
            var uri = new Uri($"{gatewayHost}?v={_options.ApiVersion}&encoding=json");

            try
            {
                _logger.LogInformation("Connecting to Discord Gateway...");
                await _webSocket.ConnectAsync(uri, _cts.Token);
                await SetStateAsync(GatewayState.Connected);

                // Start receiving messages
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                // Try to resume if we have a session, otherwise identify
                if (_resumeSessionId is not null && _resumeSequence.HasValue)
                {
                    await SendResumeAsync();
                }
                else
                {
                    await SendIdentifyAsync();
                }

                _logger.LogInformation("Connected to Discord Gateway.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Gateway");
                await SetStateAsync(GatewayState.Disconnected);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_currentState == GatewayState.Disconnected)
            {
                return;
            }

            _logger.LogInformation("Disconnecting from Discord Gateway...");
            _heartbeatManager.Stop();
            _cts?.Cancel();
            await _webSocket.DisconnectAsync(_cts?.Token ?? CancellationToken.None);
            await SetStateAsync(GatewayState.Disconnected);
            _logger.LogInformation("Disconnected from Discord Gateway.");
        }

        /// <summary>
        /// Update client presence/status (Opcode 3).
        /// </summary>
        public async Task UpdatePresenceAsync(string status, string? game = null, string? streamUrl = null)
        {
            try
            {
                var presencePayload = new
                {
                    op = 3, // Status Update
                    d = new
                    {
                        since = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        activities = string.IsNullOrEmpty(game) ? Array.Empty<object>() : new object[]
                        {
                            new
                            {
                                name = game,
                                type = string.IsNullOrEmpty(streamUrl) ? 0 : 1,
                                url = streamUrl
                            }
                        },
                        status = status, // "online", "dnd", "idle", "invisible"
                        afk = false
                    }
                };

                var json = JsonSerializer.Serialize(presencePayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation("Updated presence to: {Status}", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating presence");
            }
        }

        /// <summary>
        /// Request guild members list (Opcode 8). Used for member chunking.
        /// Provide <paramref name="userIds"/> for targeted member fetches (mutually exclusive with <paramref name="query"/>).
        /// </summary>
        public async Task RequestGuildMembersAsync(ulong guildId, int limit = 0, string? query = null, bool? presences = null, ulong[]? userIds = null)
        {
            try
            {
                // Build d payload — user_ids and query are mutually exclusive per Discord docs.
                object d = userIds is { Length: > 0 }
                    ? new
                    {
                        guild_id = guildId.ToString(),
                        user_ids = Array.ConvertAll(userIds, id => id.ToString()),
                        limit,
                        presences
                    }
                    : (object)new
                    {
                        guild_id = guildId.ToString(),
                        query = query ?? "",
                        limit,
                        presences
                    };

                var requestPayload = new { op = 8, d };

                var json = JsonSerializer.Serialize(requestPayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation("Requested guild members for guild {GuildId}", guildId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting guild members");
            }
        }

        /// <summary>
        /// Requests soundboard sounds for one or more guilds (Opcode 31).
        /// Discord will respond with a GUILD_SOUNDBOARD_SOUNDS_UPDATE event for each requested guild.
        /// </summary>
        /// <param name="guildIds">The IDs of the guilds whose soundboard sounds to request.</param>
        public async Task RequestSoundboardSoundsAsync(params ulong[] guildIds)
        {
            try
            {
                var requestPayload = new
                {
                    op = 31, // Request Soundboard Sounds
                    d = new
                    {
                        guild_ids = System.Array.ConvertAll(guildIds, id => id.ToString())
                    }
                };

                var json = JsonSerializer.Serialize(requestPayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation("Requested soundboard sounds for {Count} guild(s)", guildIds.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting soundboard sounds");
            }
        }

        /// <summary>
        /// Gracefully reconnect with exponential backoff on transient errors.
        /// </summary>
        private async Task ReconnectAsync()
        {
            if (!_reconnectionManager.CanReconnect)
            {
                _logger.LogError("Cannot reconnect - maximum attempts exceeded");
                return;
            }

            await DisconnectAsync();

            if (!await _reconnectionManager.ReconnectAsync())
            {
                _logger.LogError("Reconnection failed - giving up");
                return;
            }

            try
            {
                await ConnectAsync();
                _reconnectionManager.Reset();
                _logger.LogInformation("Reconnected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnection attempt failed");
            }
        }

        private async Task SetStateAsync(GatewayState newState)
        {
            if (_currentState != newState)
            {
                var oldState = _currentState;
                _currentState = newState;
                _logger.LogInformation("Gateway state: {OldState} -> {NewState}", oldState, newState);
                if (OnStateChanged is { } handler) await handler(oldState, newState);
            }
            await Task.CompletedTask;
        }

        private async Task SendIdentifyAsync()
        {
            try
            {
                var identifyPayload = new
                {
                    op = 2, // Identify
                    d = new
                    {
                        token = _options.Token,
                        intents = (int)_options.Intents,
                        properties = new
                        {
                            os = "linux",
                            browser = "pawsharp",
                            device = "pawsharp"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(identifyPayload);
                // SECURITY: Do not log the 'json' variable — it contains the bot token in plaintext.
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation("Sent identify payload.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send identify payload");
                OnIdentifyFailed?.Invoke($"Failed to send identify: {ex.Message}");
                throw;
            }
        }

        private async Task SendResumeAsync()
        {
            if (_resumeSessionId is null || !_resumeSequence.HasValue)
            {
                _logger.LogWarning("Cannot resume - missing session or sequence");
                OnResumeFailed?.Invoke("Cannot resume - missing session or sequence");
                await SendIdentifyAsync();
                return;
            }

            try
            {
                var resumePayload = new
                {
                    op = 6, // Resume
                    d = new
                    {
                        token = _options.Token,
                        session_id = _resumeSessionId,   // must be the raw string value from READY
                        seq = _resumeSequence.Value
                    }
                };

                var json = JsonSerializer.Serialize(resumePayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation("Sent resume payload.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send resume payload");
                OnResumeFailed?.Invoke($"Failed to send resume: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket.IsConnected)
            {
                try
                {
                    var message = await _webSocket.ReceiveAsync(cancellationToken);
                    await HandleMessageAsync(message);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Receive loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving message from Gateway - attempting reconnection");
                    await ReconnectAsync();
                }
            }
        }

        private async Task HandleMessageAsync(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                int op = root.GetProperty("op").GetInt32();
                int? s = root.TryGetProperty("s", out var sProp) && sProp.ValueKind != JsonValueKind.Null 
                    ? sProp.GetInt32() 
                    : (int?)null;
                string? t = root.TryGetProperty("t", out var tProp) ? tProp.GetString() : null;
                var d = root.TryGetProperty("d", out var dProp) ? dProp : default;

                // Track sequence number for resumption
                if (s.HasValue)
                {
                    _resumeSequence = s.Value;
                }

                _logger.LogDebug("Received Gateway message: op={Op}, t={EventType}, seq={Seq}", op, t, s);

                switch (op)
                {
                    case 0: // Dispatch — Server event
                        if (!string.IsNullOrEmpty(t))
                        {
                            _logger.LogDebug("Dispatching event: {EventType}", t);
                            await HandleDispatchEventAsync(t, d.GetRawText());
                        }
                        break;
                    case 1: // Heartbeat — Server requesting heartbeat (server-initiated)
                        _logger.LogDebug("Server requested heartbeat");
                        await SendHeartbeatAsync();
                        break;
                    case 2: // Identify — Client authenticate (handled elsewhere, client-only)
                        _logger.LogDebug("Opcode 2 (Identify) should not be received from server");
                        break;
                    case 3: // Status Update — Client presence (handled elsewhere, client-only)
                        _logger.LogDebug("Opcode 3 (Status Update) should not be received from server");
                        break;
                    case 4: // Voice State Update — Client voice state (handled elsewhere, client-only)
                        _logger.LogDebug("Opcode 4 (Voice State Update) should not be received from server");
                        break;
                    case 5: // Reserved — No longer used in modern Discord Gateway
                        _logger.LogDebug("Received opcode 5 (reserved/unused in current Gateway spec)");
                        // Voice server updates are handled via VOICE_SERVER_UPDATE dispatch events (opcode 0)
                        // and are fully supported via PawSharp.Voice component
                        break;
                    case 6: // Resume — Client session resume (handled elsewhere, client-only)
                        _logger.LogDebug("Opcode 6 (Resume) should not be received from server");
                        break;
                    case 7: // Reconnect — Server forcing reconnection
                        _logger.LogWarning("Server requested reconnection");
                        await ReconnectAsync();
                        break;
                    case 8: // Request Guild Members — Client requesting members (handled elsewhere, client-only)
                        _logger.LogDebug("Opcode 8 (Request Guild Members) should not be received from server");
                        break;
                    case 9: // Invalid Session — Auth/session failed
                        // d is a boolean: true means the session is resumable, false means start fresh
                        bool resumable = d.ValueKind == JsonValueKind.True;
                        string errorMsg = resumable 
                            ? "Invalid session but resumable - will re-resume" 
                            : "Invalid session - clearing resume data and re-identifying";
                        _logger.LogError(errorMsg);
                        
                        if (!resumable)
                        {
                            _resumeSessionId = null;
                            _resumeSequence = null;
                            _resumeGatewayUrl = null;
                        }
                        
                        OnIdentifyFailed?.Invoke(errorMsg);
                        // Discord requires a small delay before re-identifying after invalid session
                        await Task.Delay(TimeSpan.FromSeconds(resumable ? 1 : 5));
                        // When the session is resumable Discord expects a RESUME, not a fresh IDENTIFY.
                        if (resumable)
                            await SendResumeAsync();
                        else
                            await SendIdentifyAsync();
                        break;
                    case 10: // Hello — Server handshake
                        await HandleHelloAsync(d);
                        break;
                    case 11: // Heartbeat ACK — Server heartbeat response
                        _logger.LogDebug("Heartbeat acknowledged");
                        if (_lastHeartbeatSent.HasValue)
                            _lastHeartbeatLatency = DateTimeOffset.UtcNow - _lastHeartbeatSent.Value;
                        await _heartbeatManager.ReceiveAckAsync();
                        break;
                    default:
                        _logger.LogDebug("Unhandled opcode: {Op}", op);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Gateway message.");
            }
        }

        private async Task HandleHelloAsync(JsonElement data)
        {
            try
            {
                if (data.TryGetProperty("heartbeat_interval", out var intervalProp))
                {
                    int interval = intervalProp.GetInt32();
                    _logger.LogInformation("Received heartbeat interval: {Interval}ms", interval);
                    
                    _heartbeatManager.Stop();
                    _heartbeatManager = new HeartbeatManager(interval, SendHeartbeatAsync, _logger, _options.MaxMissedHeartbeatAcks);
                    _heartbeatManager.OnZombieConnection += async () =>
                    {
                        _logger.LogError("Zombie connection detected - reconnecting...");
                        await ReconnectAsync();
                    };
                    _heartbeatManager.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling HELLO");
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends a serialised payload through the WebSocket, optionally subject to the
        /// 120-commands-per-60-second gateway rate limit.  Heartbeat opcodes (op 1) should
        /// pass <paramref name="isHeartbeat"/>&#xA0;=&#xA0;<see langword="true"/> to bypass
        /// the throttle — they must always be sent on time to keep the connection alive.
        /// </summary>
        private async Task GatewaySendAsync(string json, CancellationToken ct, bool isHeartbeat = false)
        {
            if (!isHeartbeat)
            {
                await _wsRateLimiter.WaitAsync(ct);
                // Return the token to the bucket after 60 s (sliding window).
                _ = Task.Delay(60_000, ct)
                    .ContinueWith(_ => _wsRateLimiter.Release(),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Default);
            }

            await _webSocket.SendAsync(json, ct);
        }

        private async Task SendHeartbeatAsync()
        {
            try
            {
                _lastHeartbeatSent = DateTimeOffset.UtcNow;
                var heartbeatPayload = new { op = 1, d = _resumeSequence ?? (object?)null };
                var json = JsonSerializer.Serialize(heartbeatPayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None, isHeartbeat: true);
                _logger.LogDebug("Sent heartbeat (seq={Seq})", _resumeSequence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }

        /// <summary>
        /// Sends a voice state update to the gateway.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="channelId">The channel ID to join (null to leave).</param>
        /// <param name="selfMute">Whether to mute self.</param>
        /// <param name="selfDeaf">Whether to deafen self.</param>
        public async Task SendVoiceStateUpdateAsync(ulong guildId, ulong? channelId, bool selfMute, bool selfDeaf)
        {
            try
            {
                var voiceStatePayload = new
                {
                    op = 4,
                    d = new
                    {
                        guild_id = guildId.ToString(),
                        channel_id = channelId?.ToString(),
                        self_mute = selfMute,
                        self_deaf = selfDeaf
                    }
                };
                var json = JsonSerializer.Serialize(voiceStatePayload);
                await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogDebug("Sent voice state update for guild {GuildId}, channel {ChannelId}", guildId, channelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice state update");
            }
        }

        private async Task HandleDispatchEventAsync(string eventType, string eventData)
        {
            try
            {
                switch (eventType)
                {
                    case "READY":
                        await HandleReadyEventAsync(eventData);
                        await _eventDispatcher.DispatchFromJsonAsync<ReadyEvent>(eventType, eventData);
                        break;
                    case "RESUMED":
                        _logger.LogInformation("Session resumed successfully");
                        await SetStateAsync(GatewayState.Ready);
                        break;
                    case "MESSAGE_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageCreateEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageUpdateEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildCreateEvent>(eventType, eventData);
                        break;
                    case "GUILD_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_EMOJIS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildEmojisUpdateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ChannelCreateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ChannelUpdateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<ChannelDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_ADD":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildMemberAddEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildMemberUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_REMOVE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildMemberRemoveEvent>(eventType, eventData);
                        break;
                    case "INTERACTION_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<InteractionCreateEvent>(eventType, eventData);
                        break;
                    case "TYPING_START":
                        await _eventDispatcher.DispatchFromJsonAsync<TypingStartEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_ADD":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageReactionAddEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_REMOVE":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_REMOVE_ALL":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveAllEvent>(eventType, eventData);
                        break;
                    case "PRESENCE_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<PresenceUpdateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_PINS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ChannelPinsUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_BAN_ADD":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildBanAddEvent>(eventType, eventData);
                        break;
                    case "GUILD_BAN_REMOVE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildBanRemoveEvent>(eventType, eventData);
                        break;
                    case "GUILD_ROLE_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildRoleCreateEvent>(eventType, eventData);
                        break;
                    case "GUILD_ROLE_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildRoleUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_ROLE_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildRoleDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBERS_CHUNK":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildMembersChunkEvent>(eventType, eventData);
                        break;
                    case "GUILD_STICKERS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildStickersUpdateEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_REMOVE_EMOJI":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEmojiEvent>(eventType, eventData);
                        break;
                    case "GUILD_INTEGRATIONS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildIntegrationsUpdateEvent>(eventType, eventData);
                        break;
                    case "USER_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<UserUpdateEvent>(eventType, eventData);
                        break;
                    case "VOICE_STATE_UPDATE":                        await _eventDispatcher.DispatchFromJsonAsync<VoiceStateUpdateEvent>(eventType, eventData);
                        if (VoiceStateUpdate != null)
                        {
                            var voiceStateEvent = JsonSerializer.Deserialize<VoiceStateUpdateEvent>(eventData, _snowflakeOptions);
                            if (voiceStateEvent != null)
                            {
                                await VoiceStateUpdate.Invoke(voiceStateEvent);
                            }
                        }
                        break;
                    case "VOICE_SERVER_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<VoiceServerUpdateEvent>(eventType, eventData);
                        if (VoiceServerUpdate != null)
                        {
                            var voiceServerEvent = JsonSerializer.Deserialize<VoiceServerUpdateEvent>(eventData, _snowflakeOptions);
                            if (voiceServerEvent != null)
                            {
                                await VoiceServerUpdate.Invoke(voiceServerEvent);
                            }
                        }
                        break;
                    case "THREAD_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadCreateEvent>(eventType, eventData);
                        break;
                    case "THREAD_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadUpdateEvent>(eventType, eventData);
                        break;
                    case "THREAD_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadDeleteEvent>(eventType, eventData);
                        break;
                    case "THREAD_LIST_SYNC":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadListSyncEvent>(eventType, eventData);
                        break;
                    case "THREAD_MEMBER_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadMemberUpdateEvent>(eventType, eventData);
                        break;
                    case "THREAD_MEMBERS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ThreadMembersUpdateEvent>(eventType, eventData);
                        break;
                    // alpha12 events ─────────────────────────────────────────
                    case "GUILD_SCHEDULED_EVENT_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventCreateEvent>(eventType, eventData);
                        break;
                    case "GUILD_SCHEDULED_EVENT_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_SCHEDULED_EVENT_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_SCHEDULED_EVENT_USER_ADD":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserAddEvent>(eventType, eventData);
                        break;
                    case "GUILD_SCHEDULED_EVENT_USER_REMOVE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserRemoveEvent>(eventType, eventData);
                        break;
                    case "AUTO_MODERATION_RULE_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleCreateEvent>(eventType, eventData);
                        break;
                    case "AUTO_MODERATION_RULE_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleUpdateEvent>(eventType, eventData);
                        break;
                    case "AUTO_MODERATION_RULE_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleDeleteEvent>(eventType, eventData);
                        break;
                    case "AUTO_MODERATION_ACTION_EXECUTION":
                        await _eventDispatcher.DispatchFromJsonAsync<AutoModerationActionExecutionEvent>(eventType, eventData);
                        break;
                    case "STAGE_INSTANCE_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<StageInstanceCreateEvent>(eventType, eventData);
                        break;
                    case "STAGE_INSTANCE_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<StageInstanceUpdateEvent>(eventType, eventData);
                        break;
                    case "STAGE_INSTANCE_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<StageInstanceDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_AUDIT_LOG_ENTRY_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildAuditLogEntryCreateEvent>(eventType, eventData);
                        break;
                    case "ENTITLEMENT_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<EntitlementCreateEvent>(eventType, eventData);
                        break;
                    case "ENTITLEMENT_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<EntitlementUpdateEvent>(eventType, eventData);
                        break;
                    case "ENTITLEMENT_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<EntitlementDeleteEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_POLL_VOTE_ADD":
                        await _eventDispatcher.DispatchFromJsonAsync<MessagePollVoteAddEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_POLL_VOTE_REMOVE":
                        await _eventDispatcher.DispatchFromJsonAsync<MessagePollVoteRemoveEvent>(eventType, eventData);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildSoundboardSoundCreateEvent>(eventType, eventData);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildSoundboardSoundUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_SOUNDBOARD_SOUND_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildSoundboardSoundDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_SOUNDBOARD_SOUNDS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<GuildSoundboardSoundsUpdateEvent>(eventType, eventData);
                        break;
                    case "VOICE_CHANNEL_EFFECT_SEND":
                        await _eventDispatcher.DispatchFromJsonAsync<VoiceChannelEffectSendEvent>(eventType, eventData);
                        break;
                    case "VOICE_CHANNEL_STATUS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<VoiceChannelStatusUpdateEvent>(eventType, eventData);
                        break;
                    case "SUBSCRIPTION_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<SubscriptionCreateEvent>(eventType, eventData);
                        break;
                    case "SUBSCRIPTION_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<SubscriptionUpdateEvent>(eventType, eventData);
                        break;
                    case "SUBSCRIPTION_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<SubscriptionDeleteEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_DELETE_BULK":
                        await _eventDispatcher.DispatchFromJsonAsync<MessageDeleteBulkEvent>(eventType, eventData);
                        break;
                    case "INVITE_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<InviteCreateEvent>(eventType, eventData);
                        break;
                    case "INVITE_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<InviteDeleteEvent>(eventType, eventData);
                        break;
                    case "WEBHOOKS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<WebhooksUpdateEvent>(eventType, eventData);
                        break;
                    case "APPLICATION_COMMAND_PERMISSIONS_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<ApplicationCommandPermissionsUpdateEvent>(eventType, eventData);
                        break;
                    case "INTEGRATION_CREATE":
                        await _eventDispatcher.DispatchFromJsonAsync<IntegrationCreateEvent>(eventType, eventData);
                        break;
                    case "INTEGRATION_UPDATE":
                        await _eventDispatcher.DispatchFromJsonAsync<IntegrationUpdateEvent>(eventType, eventData);
                        break;
                    case "INTEGRATION_DELETE":
                        await _eventDispatcher.DispatchFromJsonAsync<IntegrationDeleteEvent>(eventType, eventData);
                        break;
                    default:
                        _logger.LogDebug("Unhandled event type: {EventType}", eventType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching event {EventType}", eventType);
            }
            
            await Task.CompletedTask;
        }

        private async Task HandleReadyEventAsync(string eventData)
        {
            try
            {
                using var doc = JsonDocument.Parse(eventData);
                var root = doc.RootElement;

                // session_id is an opaque string (hex-like), NOT a numeric snowflake.
                if (root.TryGetProperty("session_id", out var sessionIdProp))
                {
                    var sessionIdStr = sessionIdProp.GetString();
                    if (!string.IsNullOrWhiteSpace(sessionIdStr))
                    {
                        _resumeSessionId = sessionIdStr;
                        _logger.LogInformation("Stored session ID for resumption: {SessionId}", sessionIdStr);
                    }
                }

                // Cache the resume URL per Discord docs – prefer this URL on reconnect
                if (root.TryGetProperty("resume_gateway_url", out var resumeUrlProp))
                {
                    var resumeUrl = resumeUrlProp.GetString();
                    if (!string.IsNullOrWhiteSpace(resumeUrl))
                    {
                        _resumeGatewayUrl = resumeUrl;
                        _logger.LogDebug("Resume gateway URL: {ResumeUrl}", resumeUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing READY event session ID");
            }

            await SetStateAsync(GatewayState.Ready);
        }
    }
}
