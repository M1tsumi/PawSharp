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
        private CancellationTokenSource _cts;
        private Task _receiveTask;

        public GatewayClient(PawSharpOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
            _webSocket = new WebSocketConnection();
            _heartbeatManager = new HeartbeatManager(41250, SendHeartbeatAsync);
            _eventDispatcher = new EventDispatcher(logger);
        }

        /// <summary>
        /// Access the event dispatcher to register event handlers.
        /// </summary>
        public EventDispatcher Events => _eventDispatcher;

        public async Task ConnectAsync()
        {
            _cts = new CancellationTokenSource();
            var uri = new Uri($"wss://gateway.discord.gg/?v={_options.ApiVersion}&encoding=json");

            _logger.LogInformation("Connecting to Discord Gateway...");
            await _webSocket.ConnectAsync(uri, _cts.Token);

            // Start receiving messages
            _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));

            // Send identify payload
            await SendIdentifyAsync();

            _heartbeatManager.Start();
            _logger.LogInformation("Connected to Discord Gateway.");
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting from Discord Gateway...");
            _heartbeatManager.Stop();
            _cts?.Cancel();
            await _webSocket.DisconnectAsync(_cts.Token);
            _logger.LogInformation("Disconnected from Discord Gateway.");
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

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket.IsConnected)
            {
                try
                {
                    var message = await _webSocket.ReceiveAsync(cancellationToken);
                    await HandleMessageAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving message from Gateway.");
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
                string t = root.TryGetProperty("t", out var tProp) ? tProp.GetString() : null;
                var d = root.TryGetProperty("d", out var dProp) ? dProp : default;

                _logger.LogInformation($"Received Gateway message: op={op}, t={t}");

                switch (op)
                {
                    case 0: // Dispatch
                        if (!string.IsNullOrEmpty(t))
                        {
                            _logger.LogDebug($"Dispatching event: {t}");
                            await HandleDispatchEventAsync(t, d.GetRawText());
                        }
                        break;
                    case 1: // Heartbeat
                        await SendHeartbeatAsync();
                        break;
                    case 9: // Invalid Session
                        _logger.LogError("Invalid session - token may be incorrect");
                        break;
                    case 10: // Hello
                        if (d.TryGetProperty("heartbeat_interval", out var intervalProp))
                        {
                            int interval = intervalProp.GetInt32();
                            _heartbeatManager = new HeartbeatManager(interval, SendHeartbeatAsync);
                            _heartbeatManager.Start();
                        }
                        break;
                    case 11: // Heartbeat ACK
                        _logger.LogDebug("Heartbeat acknowledged.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Gateway message.");
            }
        }

        private async Task SendHeartbeatAsync()
        {
            var heartbeatPayload = new { op = 1, d = (object)null };
            var json = JsonSerializer.Serialize(heartbeatPayload);
            await _webSocket.SendAsync(json, _cts.Token);
            _logger.LogDebug("Sent heartbeat.");
        }

        private async Task HandleDispatchEventAsync(string eventType, string eventData)
        {
            try
            {
                switch (eventType)
                {
                    case "READY":
                        _logger.LogInformation("Bot is now READY and online!");
                        _eventDispatcher.DispatchFromJson<ReadyEvent>(eventType, eventData);
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
                    default:
                        _logger.LogDebug($"Unhandled event type: {eventType}");
                        // Dispatch raw event for custom handling
                        if (_eventDispatcher != null)
                        {
                            _eventDispatcher.OnRaw(eventType, (json) => { });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error dispatching event {eventType}");
            }
            
            await Task.CompletedTask;
        }
    }
}