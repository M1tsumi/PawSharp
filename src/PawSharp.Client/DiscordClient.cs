using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.API.Clients;
using PawSharp.Cache.Interfaces;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Core.Models;
using PawSharp.Interactions;

namespace PawSharp.Client
{
    public class DiscordClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger<DiscordClient> _logger;
        private readonly IDiscordRestClient _restClient;
        private readonly GatewayClient _gatewayClient;
        private readonly IEntityCache _cache;
        private readonly InteractionHandler _interactionHandler;

        public DiscordClient(PawSharpOptions options, IEntityCache cache, ILogger<DiscordClient> logger, IDiscordRestClient restClient)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));

            _gatewayClient = new GatewayClient(options, logger);
            _interactionHandler = new InteractionHandler((DiscordRestClient)_restClient);
            
            // Subscribe to interaction events
            _gatewayClient.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", HandleInteractionAsync);
        }

        /// <summary>
        /// Access the Gateway client for event handling.
        /// </summary>
        public GatewayClient Gateway => _gatewayClient;
        
        /// <summary>
        /// Access the REST API client.
        /// </summary>
        public IDiscordRestClient Rest => _restClient;
        
        /// <summary>
        /// Access the entity cache.
        /// </summary>
        public IEntityCache Cache => _cache;
        
        /// <summary>
        /// Access the interaction handler.
        /// </summary>
        public InteractionHandler Interactions => _interactionHandler;

        public async Task ConnectAsync()
        {
            _logger.LogInformation("Connecting to Discord...");
            await _gatewayClient.ConnectAsync();
            _logger.LogInformation("Connected to Discord.");
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting from Discord...");
            await _gatewayClient.DisconnectAsync();
            _logger.LogInformation("Disconnected from Discord.");
        }

        private async void HandleInteractionAsync(InteractionCreateEvent interaction)
        {
            await _interactionHandler.HandleInteractionAsync(interaction);
        }

        public async Task SendMessageAsync(string channelId, string message)
        {
            _logger.LogInformation($"Sending message to channel {message}");
            // TODO: Implement typed method
            // await _restClient.SendMessageAsync(channelId, message);
        }

        // Additional methods for managing client state and interactions can be added here
    }
}