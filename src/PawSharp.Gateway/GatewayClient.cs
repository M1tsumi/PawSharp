using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Models;
using PawSharp.Gateway.Connection;
using PawSharp.Gateway.Events;
using PawSharp.Gateway.Heartbeat;

namespace PawSharp.Gateway
{
    public class GatewayClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger _logger;
        private readonly WebSocketConnection _webSocket;
        private HeartbeatManager _heartbeatManager;
        private readonly EventDispatcher _eventDispatcher;
        private readonly ReconnectionManager _reconnectionManager;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        
        private GatewayState _currentState = GatewayState.Disconnected;
        private ulong? _resumeSessionId;
        private int? _resumeSequence;

        /// <summary>
        /// Fired when the gateway state changes.
        /// </summary>
        public event Func<GatewayState, GatewayState, Task> OnStateChanged;

        /// <summary>
        /// Fired when reconnection is about to be attempted.
        /// </summary>
        public event Func<int, Task> OnReconnectionAttempt;

        /// <summary>
        /// Fired when reconnection has failed after all attempts.
        /// </summary>
        public event Func<Task> OnReconnectionFailed;

        public GatewayClient(PawSharpOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
            _webSocket = new WebSocketConnection();
            _heartbeatManager = new HeartbeatManager(41250, SendHeartbeatAsync, logger);
            _eventDispatcher = new EventDispatcher(logger);
            _reconnectionManager = new ReconnectionManager(logger);
            
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

        public async Task ConnectAsync()
        {
            if (_currentState != GatewayState.Disconnected)
            {
                _logger.LogWarning($"Cannot connect - already in state {_currentState}");
                return;
            }

            await SetStateAsync(GatewayState.Connecting);
            _cts = new CancellationTokenSource();
            var uri = new Uri($"wss://gateway.discord.gg/?v={_options.ApiVersion}&encoding=json");

            try
            {
                _logger.LogInformation("Connecting to Discord Gateway...");
                await _webSocket.ConnectAsync(uri, _cts.Token);
                await SetStateAsync(GatewayState.Connected);

                // Start receiving messages
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                // Try to resume if we have a session, otherwise identify
                if (_resumeSessionId.HasValue && _resumeSequence.HasValue)
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
                        activities = string.IsNullOrEmpty(game) ? new object[0] : new object[]
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
                await _webSocket.SendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation($"Updated presence to: {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating presence");
            }
        }

        /// <summary>
        /// Request guild members list (Opcode 8). Used for member chunking.
        /// </summary>
        public async Task RequestGuildMembersAsync(ulong guildId, int limit = 0, string? query = null)
        {
            try
            {
                var requestPayload = new
                {
                    op = 8, // Request Guild Members
                    d = new
                    {
                        guild_id = guildId.ToString(),
                        query = query ?? "",
                        limit = limit
                    }
                };

                var json = JsonSerializer.Serialize(requestPayload);
                await _webSocket.SendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogInformation($"Requested guild members for guild {guildId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting guild members");
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
                _logger.LogInformation($"Gateway state: {oldState} -> {newState}");
                OnStateChanged?.Invoke(oldState, newState);
            }
            await Task.CompletedTask;
        }

        private async Task SendIdentifyAsync()
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
            await _webSocket.SendAsync(json, _cts.Token);
            _logger.LogInformation("Sent identify payload.");
        }

        private async Task SendResumeAsync()
        {
            if (!_resumeSessionId.HasValue || !_resumeSequence.HasValue)
            {
                _logger.LogWarning("Cannot resume - missing session or sequence");
                await SendIdentifyAsync();
                return;
            }

            var resumePayload = new
            {
                op = 6, // Resume
                d = new
                {
                    token = _options.Token,
                    session_id = _resumeSessionId.Value.ToString(),
                    seq = _resumeSequence.Value
                }
            };

            var json = JsonSerializer.Serialize(resumePayload);
            await _webSocket.SendAsync(json, _cts.Token);
            _logger.LogInformation("Sent resume payload.");
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
                string t = root.TryGetProperty("t", out var tProp) ? tProp.GetString() : null;
                var d = root.TryGetProperty("d", out var dProp) ? dProp : default;

                // Track sequence number for resumption
                if (s.HasValue)
                {
                    _resumeSequence = s.Value;
                }

                _logger.LogDebug($"Received Gateway message: op={op}, t={t}, seq={s}");

                switch (op)
                {
                    case 0: // Dispatch — Server event
                        if (!string.IsNullOrEmpty(t))
                        {
                            _logger.LogDebug($"Dispatching event: {t}");
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
                    case 5: // Voice Server Ping — Server voice ping
                        _logger.LogDebug("Received voice server ping (voice support not yet implemented)");
                        // Voice support planned for future phase
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
                        _logger.LogError("Invalid session - clearing resume data and re-identifying");
                        _resumeSessionId = null;
                        _resumeSequence = null;
                        await SendIdentifyAsync();
                        break;
                    case 10: // Hello — Server handshake
                        await HandleHelloAsync(d);
                        break;
                    case 11: // Heartbeat ACK — Server heartbeat response
                        _logger.LogDebug("Heartbeat acknowledged");
                        await _heartbeatManager.ReceiveAckAsync();
                        break;
                    default:
                        _logger.LogDebug($"Unhandled opcode: {op}");
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
                    _logger.LogInformation($"Received heartbeat interval: {interval}ms");
                    
                    _heartbeatManager.Stop();
                    _heartbeatManager = new HeartbeatManager(interval, SendHeartbeatAsync, _logger);
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

        private async Task SendHeartbeatAsync()
        {
            try
            {
                var heartbeatPayload = new { op = 1, d = _resumeSequence ?? (object)null };
                var json = JsonSerializer.Serialize(heartbeatPayload);
                await _webSocket.SendAsync(json, _cts?.Token ?? CancellationToken.None);
                _logger.LogDebug($"Sent heartbeat (seq={_resumeSequence})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
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
                        _eventDispatcher.DispatchFromJson<ReadyEvent>(eventType, eventData);
                        break;
                    case "RESUMED":
                        _logger.LogInformation("Session resumed successfully");
                        await SetStateAsync(GatewayState.Ready);
                        break;
                    case "MESSAGE_CREATE":
                        _eventDispatcher.DispatchFromJson<MessageCreateEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_UPDATE":
                        _eventDispatcher.DispatchFromJson<MessageUpdateEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_DELETE":
                        _eventDispatcher.DispatchFromJson<MessageDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_CREATE":
                        _eventDispatcher.DispatchFromJson<GuildCreateEvent>(eventType, eventData);
                        break;
                    case "GUILD_UPDATE":
                        _eventDispatcher.DispatchFromJson<GuildUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_DELETE":
                        _eventDispatcher.DispatchFromJson<GuildDeleteEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_CREATE":
                        _eventDispatcher.DispatchFromJson<ChannelCreateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_UPDATE":
                        _eventDispatcher.DispatchFromJson<ChannelUpdateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_DELETE":
                        _eventDispatcher.DispatchFromJson<ChannelDeleteEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_ADD":
                        _eventDispatcher.DispatchFromJson<GuildMemberAddEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_UPDATE":
                        _eventDispatcher.DispatchFromJson<GuildMemberUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_MEMBER_REMOVE":
                        _eventDispatcher.DispatchFromJson<GuildMemberRemoveEvent>(eventType, eventData);
                        break;
                    case "INTERACTION_CREATE":
                        _eventDispatcher.DispatchFromJson<InteractionCreateEvent>(eventType, eventData);
                        break;
                    case "TYPING_START":
                        _eventDispatcher.DispatchFromJson<TypingStartEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_ADD":
                        _eventDispatcher.DispatchFromJson<MessageReactionAddEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_REMOVE":
                        _eventDispatcher.DispatchFromJson<MessageReactionRemoveEvent>(eventType, eventData);
                        break;
                    case "MESSAGE_REACTION_REMOVE_ALL":
                        _eventDispatcher.DispatchFromJson<MessageReactionRemoveAllEvent>(eventType, eventData);
                        break;
                    case "PRESENCE_UPDATE":
                        _eventDispatcher.DispatchFromJson<PresenceUpdateEvent>(eventType, eventData);
                        break;
                    case "CHANNEL_PINS_UPDATE":
                        _eventDispatcher.DispatchFromJson<ChannelPinsUpdateEvent>(eventType, eventData);
                        break;
                    case "GUILD_BAN_ADD":
                        _eventDispatcher.DispatchFromJson<GuildBanAddEvent>(eventType, eventData);
                        break;
                    case "GUILD_BAN_REMOVE":
                        _eventDispatcher.DispatchFromJson<GuildBanRemoveEvent>(eventType, eventData);
                        break;
                    case "VOICE_STATE_UPDATE":
                        _eventDispatcher.DispatchFromJson<VoiceStateUpdateEvent>(eventType, eventData);
                        break;
                    case "THREAD_CREATE":
                        _eventDispatcher.DispatchFromJson<ThreadCreateEvent>(eventType, eventData);
                        break;
                    case "THREAD_UPDATE":
                        _eventDispatcher.DispatchFromJson<ThreadUpdateEvent>(eventType, eventData);
                        break;
                    case "THREAD_DELETE":
                        _eventDispatcher.DispatchFromJson<ThreadDeleteEvent>(eventType, eventData);
                        break;
                    case "THREAD_LIST_SYNC":
                        _eventDispatcher.DispatchFromJson<ThreadListSyncEvent>(eventType, eventData);
                        break;
                    case "THREAD_MEMBER_UPDATE":
                        _eventDispatcher.DispatchFromJson<ThreadMemberUpdateEvent>(eventType, eventData);
                        break;
                    case "THREAD_MEMBERS_UPDATE":
                        _eventDispatcher.DispatchFromJson<ThreadMembersUpdateEvent>(eventType, eventData);
                        break;
                    default:
                        _logger.LogDebug($"Unhandled event type: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error dispatching event {eventType}");
            }
            
            await Task.CompletedTask;
        }

        private async Task HandleReadyEventAsync(string eventData)
        {
            try
            {
                using var doc = JsonDocument.Parse(eventData);
                var root = doc.RootElement;

                if (root.TryGetProperty("session_id", out var sessionIdProp))
                {
                    var sessionIdStr = sessionIdProp.GetString();
                    if (ulong.TryParse(sessionIdStr, out var sessionId))
                    {
                        _resumeSessionId = sessionId;
                        _logger.LogInformation($"Stored session ID for resumption: {sessionId}");
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
