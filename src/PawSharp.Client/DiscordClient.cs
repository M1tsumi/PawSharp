using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Gateway;
using PawSharp.Core.Models;

namespace PawSharp.Client
{
    public class DiscordClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger<DiscordClient> _logger;
        private readonly IDiscordRestClient _restClient;
        private readonly GatewayClient _gatewayClient;
        private readonly IEntityCache _cache;

        public DiscordClient(PawSharpOptions options, IEntityCache cache, ILogger<DiscordClient> logger, IDiscordRestClient restClient)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));

            _gatewayClient = new GatewayClient(options, logger);
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

        public async Task SendMessageAsync(string channelId, string message)
        {
            _logger.LogInformation($"Sending message to channel {channelId}: {message}");
            // TODO: Implement typed method
            // await _restClient.SendMessageAsync(channelId, message);
        }

        // Additional methods for managing client state and interactions can be added here
    }
}