using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Core.Models;
using PawSharp.Interactions;

namespace PawSharp.Client
{
    /// <summary>
    /// Primary entry point for bots interacting with Discord.
    /// Composes the REST client, gateway, cache, and interaction handler.
    /// </summary>
    public class DiscordClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger<DiscordClient> _logger;
        private readonly IDiscordRestClient _restClient;
        private readonly IGatewayClient _gatewayClient;
        private readonly IEntityCache _cache;
        private readonly InteractionHandler _interactionHandler;
        private readonly CacheManager _cacheManager;

        /// <summary>
        /// Creates a <see cref="DiscordClient"/> with all dependencies supplied externally.
        /// Prefer the <c>AddPawSharp</c> DI extension for wiring everything automatically.
        /// </summary>
        public DiscordClient(
            PawSharpOptions options,
            IEntityCache cache,
            ILogger<DiscordClient> logger,
            IDiscordRestClient restClient,
            IGatewayClient gatewayClient)
        {
            _options       = options       ?? throw new ArgumentNullException(nameof(options));
            _cache         = cache         ?? throw new ArgumentNullException(nameof(cache));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
            _restClient    = restClient    ?? throw new ArgumentNullException(nameof(restClient));
            _gatewayClient = gatewayClient ?? throw new ArgumentNullException(nameof(gatewayClient));

            _interactionHandler = new InteractionHandler(_restClient);

            // Wire cache to gateway events automatically
            _cacheManager = new CacheManager(cache, null);
            _cacheManager.SubscribeToGateway(_gatewayClient);

            // Subscribe to interaction events
            _gatewayClient.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", HandleInteractionAsync);
        }

        // ── Public surface ────────────────────────────────────────────────────────

        /// <summary>Access the gateway client for low-level event handling and presence.</summary>
        public IGatewayClient Gateway => _gatewayClient;

        /// <summary>Access the REST API client for all HTTP operations.</summary>
        public IDiscordRestClient Rest => _restClient;

        /// <summary>Access the entity cache.</summary>
        public IEntityCache Cache => _cache;

        /// <summary>Access the interaction handler for registering slash commands and components.</summary>
        public InteractionHandler Interactions => _interactionHandler;

        // ── Connection ────────────────────────────────────────────────────────────

        /// <summary>Opens the WebSocket connection to Discord's gateway.</summary>
        public async Task ConnectAsync()
        {
            _logger.LogInformation("Connecting to Discord...");
            await _gatewayClient.ConnectAsync();
            _logger.LogInformation("Connected to Discord.");
        }

        /// <summary>Closes the WebSocket connection gracefully.</summary>
        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting from Discord...");
            await _gatewayClient.DisconnectAsync();
            _logger.LogInformation("Disconnected from Discord.");
        }

        // ── Typed REST helpers ────────────────────────────────────────────────────

        /// <summary>Sends a plain-text message to a channel.</summary>
        public async Task<Message?> SendMessageAsync(ulong channelId, string content)
        {
            return await _restClient.CreateMessageAsync(channelId, new CreateMessageRequest { Content = content });
        }

        /// <summary>Sends a fully specified message to a channel.</summary>
        public async Task<Message?> SendMessageAsync(ulong channelId, CreateMessageRequest request)
        {
            return await _restClient.CreateMessageAsync(channelId, request);
        }

        /// <summary>Returns the current bot user from the Discord API.</summary>
        public async Task<User?> GetCurrentUserAsync()
        {
            var response = await _restClient.GetCurrentUserAsync();
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<User>();
            }
            return null;
        }

        // ── Convenience event subscriptions ───────────────────────────────────────

        /// <summary>Subscribes to the MESSAGE_CREATE gateway event.</summary>
        public IDisposable OnMessageCreated(Func<MessageCreateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageCreateEvent>("MESSAGE_CREATE", e => { _ = handler(e); });

        /// <summary>Subscribes to the MESSAGE_UPDATE gateway event.</summary>
        public IDisposable OnMessageUpdated(Func<MessageUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", e => { _ = handler(e); });

        /// <summary>Subscribes to the MESSAGE_DELETE gateway event.</summary>
        public IDisposable OnMessageDeleted(Func<MessageDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<MessageDeleteEvent>("MESSAGE_DELETE", e => { _ = handler(e); });

        /// <summary>Subscribes to the GUILD_CREATE gateway event.</summary>
        public IDisposable OnGuildAvailable(Func<GuildCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildCreateEvent>("GUILD_CREATE", e => { _ = handler(e); });

        /// <summary>Subscribes to the GUILD_MEMBER_ADD gateway event.</summary>
        public IDisposable OnGuildMemberJoined(Func<GuildMemberAddEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", e => { _ = handler(e); });

        /// <summary>Subscribes to the GUILD_MEMBER_REMOVE gateway event.</summary>
        public IDisposable OnGuildMemberLeft(Func<GuildMemberRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberRemoveEvent>("GUILD_MEMBER_REMOVE", e => { _ = handler(e); });

        /// <summary>Subscribes to the INTERACTION_CREATE gateway event.</summary>
        public IDisposable OnInteractionCreated(Func<InteractionCreateEvent, Task> handler)
            => _gatewayClient.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", e => { _ = handler(e); });

        /// <summary>Subscribes to the READY gateway event.</summary>
        public IDisposable OnReady(Func<ReadyEvent, Task> handler)
            => _gatewayClient.Events.On<ReadyEvent>("READY", e => { _ = handler(e); });

        // ── Internal ──────────────────────────────────────────────────────────────

        private async void HandleInteractionAsync(InteractionCreateEvent interaction)
        {
            await _interactionHandler.HandleInteractionAsync(interaction);
        }
    }
}
