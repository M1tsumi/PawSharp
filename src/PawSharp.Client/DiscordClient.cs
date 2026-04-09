#nullable enable
using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.API.RateLimit;
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
        /// The bot's own user object, populated after <see cref="ConnectAsync"/> completes
        /// and the READY gateway event is received.
        /// </summary>
        public User? CurrentUser { get; private set; }

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

            // Cache CurrentUser from READY event
            _gatewayClient.Events.On<ReadyEvent>("READY", e =>
            {
                CurrentUser = e.User;
                // Apply initial presence if configured
                if (_options.Presence is { } presence)
                {
                    return _gatewayClient.UpdatePresenceAsync(
                        presence.Status,
                        presence.ActivityName,
                        presence.StreamUrl);
                }
                return Task.CompletedTask;
            });

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

        /// <summary>
        /// Gets whether the configured REST client exposes rate-limit telemetry events.
        /// </summary>
        public bool SupportsRateLimitTelemetry => _restClient is IRateLimitTelemetrySource;

        /// <summary>
        /// Raised when rate-limit telemetry is emitted by the underlying REST client.
        /// </summary>
        public event EventHandler<RateLimitTelemetryEvent>? RateLimitObserved
        {
            add
            {
                if (_restClient is IRateLimitTelemetrySource telemetry)
                {
                    telemetry.RateLimitObserved += value;
                }
            }
            remove
            {
                if (_restClient is IRateLimitTelemetrySource telemetry)
                {
                    telemetry.RateLimitObserved -= value;
                }
            }
        }

        // ── Connection ────────────────────────────────────────────────────────────

        /// <summary>Opens the WebSocket connection to Discord's gateway.</summary>
        public async Task ConnectAsync()
        {
            ValidateIntentConfiguration();
            _logger.LogInformation("Connecting to Discord...");
            await _gatewayClient.ConnectAsync();
            _logger.LogInformation("Connected to Discord.");
        }

        private void ValidateIntentConfiguration()
        {
            if (_options.IntentValidation == IntentValidationMode.Off)
            {
                return;
            }

            var result = this.ValidateIntents(_options.Intents);
            if (result.IsValid)
            {
                return;
            }

            var message = $"Intent validation failed: {result}";
            if (_options.IntentValidation == IntentValidationMode.Strict)
            {
                throw new InvalidOperationException(message);
            }

            _logger.LogWarning("{Message}", message);
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

        /// <summary>
        /// Forwards a source message into another channel using Discord's message snapshot forwarding model.
        /// </summary>
        public async Task<Message?> ForwardMessageAsync(
            ulong targetChannelId,
            ulong sourceChannelId,
            ulong sourceMessageId,
            string? content = null,
            bool failIfNotExists = true)
        {
            return await _restClient.ForwardMessageAsync(
                targetChannelId,
                sourceChannelId,
                sourceMessageId,
                content,
                failIfNotExists);
        }

        /// <summary>
        /// Forwards a source message using an explicit payload for additional fields such as allowed mentions.
        /// The provided payload's <see cref="CreateMessageRequest.MessageReference"/> will be overwritten.
        /// </summary>
        public async Task<Message?> ForwardMessageAsync(
            ulong targetChannelId,
            ulong sourceChannelId,
            ulong sourceMessageId,
            CreateMessageRequest request,
            bool failIfNotExists = true)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.MessageReference = MessageReference.Forward(sourceChannelId, sourceMessageId, failIfNotExists);
            return await _restClient.CreateMessageAsync(targetChannelId, request);
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

        // Messages ──────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the READY gateway event.</summary>
        public IDisposable OnReady(Func<ReadyEvent, Task> handler)
            => _gatewayClient.Events.On<ReadyEvent>("READY", handler);

        /// <summary>Subscribes to the MESSAGE_CREATE gateway event.</summary>
        public IDisposable OnMessageCreated(Func<MessageCreateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageCreateEvent>("MESSAGE_CREATE", handler);

        /// <summary>Subscribes to the MESSAGE_UPDATE gateway event.</summary>
        public IDisposable OnMessageUpdated(Func<MessageUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", handler);

        /// <summary>Subscribes to the MESSAGE_DELETE gateway event.</summary>
        public IDisposable OnMessageDeleted(Func<MessageDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<MessageDeleteEvent>("MESSAGE_DELETE", handler);

        /// <summary>Subscribes to the MESSAGE_DELETE_BULK gateway event.</summary>
        public IDisposable OnMessagesBulkDeleted(Func<MessageDeleteBulkEvent, Task> handler)
            => _gatewayClient.Events.On<MessageDeleteBulkEvent>("MESSAGE_DELETE_BULK", handler);

        // Reactions ─────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the MESSAGE_REACTION_ADD gateway event.</summary>
        public IDisposable OnReactionAdded(Func<MessageReactionAddEvent, Task> handler)
            => _gatewayClient.Events.On<MessageReactionAddEvent>("MESSAGE_REACTION_ADD", handler);

        /// <summary>Subscribes to the MESSAGE_REACTION_REMOVE gateway event.</summary>
        public IDisposable OnReactionRemoved(Func<MessageReactionRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<MessageReactionRemoveEvent>("MESSAGE_REACTION_REMOVE", handler);

        /// <summary>Subscribes to the MESSAGE_REACTION_REMOVE_ALL gateway event.</summary>
        public IDisposable OnAllReactionsRemoved(Func<MessageReactionRemoveAllEvent, Task> handler)
            => _gatewayClient.Events.On<MessageReactionRemoveAllEvent>("MESSAGE_REACTION_REMOVE_ALL", handler);

        /// <summary>Subscribes to the MESSAGE_REACTION_REMOVE_EMOJI gateway event.</summary>
        public IDisposable OnEmojiReactionsRemoved(Func<MessageReactionRemoveEmojiEvent, Task> handler)
            => _gatewayClient.Events.On<MessageReactionRemoveEmojiEvent>("MESSAGE_REACTION_REMOVE_EMOJI", handler);

        // Guilds ────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_CREATE gateway event.</summary>
        public IDisposable OnGuildAvailable(Func<GuildCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildCreateEvent>("GUILD_CREATE", handler);

        /// <summary>Subscribes to the GUILD_UPDATE gateway event.</summary>
        public IDisposable OnGuildUpdated(Func<GuildUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildUpdateEvent>("GUILD_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_DELETE gateway event.</summary>
        public IDisposable OnGuildUnavailable(Func<GuildDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<GuildDeleteEvent>("GUILD_DELETE", handler);

        // Members ───────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_MEMBER_ADD gateway event.</summary>
        public IDisposable OnGuildMemberJoined(Func<GuildMemberAddEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", handler);

        /// <summary>Subscribes to the GUILD_MEMBER_UPDATE gateway event.</summary>
        public IDisposable OnGuildMemberUpdated(Func<GuildMemberUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberUpdateEvent>("GUILD_MEMBER_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_MEMBER_REMOVE gateway event.</summary>
        public IDisposable OnGuildMemberLeft(Func<GuildMemberRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberRemoveEvent>("GUILD_MEMBER_REMOVE", handler);

        // Channels ──────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the CHANNEL_CREATE gateway event.</summary>
        public IDisposable OnChannelCreated(Func<ChannelCreateEvent, Task> handler)
            => _gatewayClient.Events.On<ChannelCreateEvent>("CHANNEL_CREATE", handler);

        /// <summary>Subscribes to the CHANNEL_UPDATE gateway event.</summary>
        public IDisposable OnChannelUpdated(Func<ChannelUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ChannelUpdateEvent>("CHANNEL_UPDATE", handler);

        /// <summary>Subscribes to the CHANNEL_DELETE gateway event.</summary>
        public IDisposable OnChannelDeleted(Func<ChannelDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<ChannelDeleteEvent>("CHANNEL_DELETE", handler);

        /// <summary>Subscribes to the CHANNEL_PINS_UPDATE gateway event.</summary>
        public IDisposable OnChannelPinsUpdated(Func<ChannelPinsUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ChannelPinsUpdateEvent>("CHANNEL_PINS_UPDATE", handler);

        // Roles ─────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_ROLE_CREATE gateway event.</summary>
        public IDisposable OnRoleCreated(Func<GuildRoleCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildRoleCreateEvent>("GUILD_ROLE_CREATE", handler);

        /// <summary>Subscribes to the GUILD_ROLE_UPDATE gateway event.</summary>
        public IDisposable OnRoleUpdated(Func<GuildRoleUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildRoleUpdateEvent>("GUILD_ROLE_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_ROLE_DELETE gateway event.</summary>
        public IDisposable OnRoleDeleted(Func<GuildRoleDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<GuildRoleDeleteEvent>("GUILD_ROLE_DELETE", handler);

        // Bans ──────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_BAN_ADD gateway event.</summary>
        public IDisposable OnBanAdded(Func<GuildBanAddEvent, Task> handler)
            => _gatewayClient.Events.On<GuildBanAddEvent>("GUILD_BAN_ADD", handler);

        /// <summary>Subscribes to the GUILD_BAN_REMOVE gateway event.</summary>
        public IDisposable OnBanRemoved(Func<GuildBanRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<GuildBanRemoveEvent>("GUILD_BAN_REMOVE", handler);

        // Typing ────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the TYPING_START gateway event.</summary>
        public IDisposable OnTypingStarted(Func<TypingStartEvent, Task> handler)
            => _gatewayClient.Events.On<TypingStartEvent>("TYPING_START", handler);

        // Presence / Voice ──────────────────────────────────────────────────────

        /// <summary>Subscribes to the PRESENCE_UPDATE gateway event.</summary>
        public IDisposable OnPresenceUpdated(Func<PresenceUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<PresenceUpdateEvent>("PRESENCE_UPDATE", handler);

        /// <summary>Subscribes to the VOICE_STATE_UPDATE gateway event.</summary>
        public IDisposable OnVoiceStateUpdated(Func<VoiceStateUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<VoiceStateUpdateEvent>("VOICE_STATE_UPDATE", handler);

        // Threads ───────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the THREAD_CREATE gateway event.</summary>
        public IDisposable OnThreadCreated(Func<ThreadCreateEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadCreateEvent>("THREAD_CREATE", handler);

        /// <summary>Subscribes to the THREAD_UPDATE gateway event.</summary>
        public IDisposable OnThreadUpdated(Func<ThreadUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadUpdateEvent>("THREAD_UPDATE", handler);

        /// <summary>Subscribes to the THREAD_DELETE gateway event.</summary>
        public IDisposable OnThreadDeleted(Func<ThreadDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadDeleteEvent>("THREAD_DELETE", handler);

        // Interactions ──────────────────────────────────────────────────────────

        /// <summary>Subscribes to the INTERACTION_CREATE gateway event.</summary>
        public IDisposable OnInteractionCreated(Func<InteractionCreateEvent, Task> handler)
            => _gatewayClient.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", handler);

        // Invites ───────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the INVITE_CREATE gateway event.</summary>
        public IDisposable OnInviteCreated(Func<InviteCreateEvent, Task> handler)
            => _gatewayClient.Events.On<InviteCreateEvent>("INVITE_CREATE", handler);

        /// <summary>Subscribes to the INVITE_DELETE gateway event.</summary>
        public IDisposable OnInviteDeleted(Func<InviteDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<InviteDeleteEvent>("INVITE_DELETE", handler);

        // Scheduled Events ─────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_SCHEDULED_EVENT_CREATE gateway event.</summary>
        public IDisposable OnScheduledEventCreated(Func<GuildScheduledEventCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildScheduledEventCreateEvent>("GUILD_SCHEDULED_EVENT_CREATE", handler);

        /// <summary>Subscribes to the GUILD_SCHEDULED_EVENT_UPDATE gateway event.</summary>
        public IDisposable OnScheduledEventUpdated(Func<GuildScheduledEventUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildScheduledEventUpdateEvent>("GUILD_SCHEDULED_EVENT_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_SCHEDULED_EVENT_DELETE gateway event.</summary>
        public IDisposable OnScheduledEventDeleted(Func<GuildScheduledEventDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<GuildScheduledEventDeleteEvent>("GUILD_SCHEDULED_EVENT_DELETE", handler);

        // Auto-Moderation ───────────────────────────────────────────────────────

        /// <summary>Subscribes to the AUTO_MODERATION_ACTION_EXECUTION gateway event.</summary>
        public IDisposable OnAutoModerationActionExecuted(Func<AutoModerationActionExecutionEvent, Task> handler)
            => _gatewayClient.Events.On<AutoModerationActionExecutionEvent>("AUTO_MODERATION_ACTION_EXECUTION", handler);

        // Voice ─────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the VOICE_SERVER_UPDATE gateway event.</summary>
        public IDisposable OnVoiceServerUpdated(Func<VoiceServerUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<VoiceServerUpdateEvent>("VOICE_SERVER_UPDATE", handler);

        // Guild content ─────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_EMOJIS_UPDATE gateway event.</summary>
        public IDisposable OnGuildEmojisUpdated(Func<GuildEmojisUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildEmojisUpdateEvent>("GUILD_EMOJIS_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_STICKERS_UPDATE gateway event.</summary>
        public IDisposable OnGuildStickersUpdated(Func<GuildStickersUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildStickersUpdateEvent>("GUILD_STICKERS_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_MEMBERS_CHUNK gateway event.</summary>
        public IDisposable OnGuildMembersChunked(Func<GuildMembersChunkEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMembersChunkEvent>("GUILD_MEMBERS_CHUNK", handler);

        /// <summary>Subscribes to the GUILD_AUDIT_LOG_ENTRY_CREATE gateway event.</summary>
        public IDisposable OnGuildAuditLogEntryCreated(Func<GuildAuditLogEntryCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildAuditLogEntryCreateEvent>("GUILD_AUDIT_LOG_ENTRY_CREATE", handler);

        // Webhooks ──────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the WEBHOOKS_UPDATE gateway event.</summary>
        public IDisposable OnWebhooksUpdated(Func<WebhooksUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<WebhooksUpdateEvent>("WEBHOOKS_UPDATE", handler);

        // Stage instances ───────────────────────────────────────────────────────

        /// <summary>Subscribes to the STAGE_INSTANCE_CREATE gateway event.</summary>
        public IDisposable OnStageInstanceCreated(Func<StageInstanceCreateEvent, Task> handler)
            => _gatewayClient.Events.On<StageInstanceCreateEvent>("STAGE_INSTANCE_CREATE", handler);

        /// <summary>Subscribes to the STAGE_INSTANCE_UPDATE gateway event.</summary>
        public IDisposable OnStageInstanceUpdated(Func<StageInstanceUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<StageInstanceUpdateEvent>("STAGE_INSTANCE_UPDATE", handler);

        /// <summary>Subscribes to the STAGE_INSTANCE_DELETE gateway event.</summary>
        public IDisposable OnStageInstanceDeleted(Func<StageInstanceDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<StageInstanceDeleteEvent>("STAGE_INSTANCE_DELETE", handler);

        // Scheduled event users ─────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_SCHEDULED_EVENT_USER_ADD gateway event.</summary>
        public IDisposable OnScheduledEventUserAdded(Func<GuildScheduledEventUserAddEvent, Task> handler)
            => _gatewayClient.Events.On<GuildScheduledEventUserAddEvent>("GUILD_SCHEDULED_EVENT_USER_ADD", handler);

        /// <summary>Subscribes to the GUILD_SCHEDULED_EVENT_USER_REMOVE gateway event.</summary>
        public IDisposable OnScheduledEventUserRemoved(Func<GuildScheduledEventUserRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<GuildScheduledEventUserRemoveEvent>("GUILD_SCHEDULED_EVENT_USER_REMOVE", handler);

        // Auto-Moderation rules ─────────────────────────────────────────────────

        /// <summary>Subscribes to the AUTO_MODERATION_RULE_CREATE gateway event.</summary>
        public IDisposable OnAutoModerationRuleCreated(Func<AutoModerationRuleCreateEvent, Task> handler)
            => _gatewayClient.Events.On<AutoModerationRuleCreateEvent>("AUTO_MODERATION_RULE_CREATE", handler);

        /// <summary>Subscribes to the AUTO_MODERATION_RULE_UPDATE gateway event.</summary>
        public IDisposable OnAutoModerationRuleUpdated(Func<AutoModerationRuleUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<AutoModerationRuleUpdateEvent>("AUTO_MODERATION_RULE_UPDATE", handler);

        /// <summary>Subscribes to the AUTO_MODERATION_RULE_DELETE gateway event.</summary>
        public IDisposable OnAutoModerationRuleDeleted(Func<AutoModerationRuleDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<AutoModerationRuleDeleteEvent>("AUTO_MODERATION_RULE_DELETE", handler);

        // Integrations ──────────────────────────────────────────────────────────

        /// <summary>Subscribes to the INTEGRATION_CREATE gateway event.</summary>
        public IDisposable OnIntegrationCreated(Func<IntegrationCreateEvent, Task> handler)
            => _gatewayClient.Events.On<IntegrationCreateEvent>("INTEGRATION_CREATE", handler);

        /// <summary>Subscribes to the INTEGRATION_UPDATE gateway event.</summary>
        public IDisposable OnIntegrationUpdated(Func<IntegrationUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<IntegrationUpdateEvent>("INTEGRATION_UPDATE", handler);

        /// <summary>Subscribes to the INTEGRATION_DELETE gateway event.</summary>
        public IDisposable OnIntegrationDeleted(Func<IntegrationDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<IntegrationDeleteEvent>("INTEGRATION_DELETE", handler);

        // Polls ─────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the MESSAGE_POLL_VOTE_ADD gateway event.</summary>
        public IDisposable OnMessagePollVoteAdded(Func<MessagePollVoteAddEvent, Task> handler)
            => _gatewayClient.Events.On<MessagePollVoteAddEvent>("MESSAGE_POLL_VOTE_ADD", handler);

        /// <summary>Subscribes to the MESSAGE_POLL_VOTE_REMOVE gateway event.</summary>
        public IDisposable OnMessagePollVoteRemoved(Func<MessagePollVoteRemoveEvent, Task> handler)
            => _gatewayClient.Events.On<MessagePollVoteRemoveEvent>("MESSAGE_POLL_VOTE_REMOVE", handler);

        // Entitlements ──────────────────────────────────────────────────────────

        /// <summary>Subscribes to the ENTITLEMENT_CREATE gateway event.</summary>
        public IDisposable OnEntitlementCreated(Func<EntitlementCreateEvent, Task> handler)
            => _gatewayClient.Events.On<EntitlementCreateEvent>("ENTITLEMENT_CREATE", handler);

        /// <summary>Subscribes to the ENTITLEMENT_UPDATE gateway event.</summary>
        public IDisposable OnEntitlementUpdated(Func<EntitlementUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<EntitlementUpdateEvent>("ENTITLEMENT_UPDATE", handler);

        /// <summary>Subscribes to the ENTITLEMENT_DELETE gateway event.</summary>
        public IDisposable OnEntitlementDeleted(Func<EntitlementDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<EntitlementDeleteEvent>("ENTITLEMENT_DELETE", handler);

        // Threads (extended) ────────────────────────────────────────────────────

        /// <summary>Subscribes to the THREAD_LIST_SYNC gateway event.</summary>
        public IDisposable OnThreadListSynced(Func<ThreadListSyncEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadListSyncEvent>("THREAD_LIST_SYNC", handler);

        /// <summary>Subscribes to the THREAD_MEMBER_UPDATE gateway event.</summary>
        public IDisposable OnThreadMemberUpdated(Func<ThreadMemberUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadMemberUpdateEvent>("THREAD_MEMBER_UPDATE", handler);

        /// <summary>Subscribes to the THREAD_MEMBERS_UPDATE gateway event.</summary>
        public IDisposable OnThreadMembersUpdated(Func<ThreadMembersUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ThreadMembersUpdateEvent>("THREAD_MEMBERS_UPDATE", handler);

        // Application commands ──────────────────────────────────────────────────

        /// <summary>Subscribes to the APPLICATION_COMMAND_PERMISSIONS_UPDATE gateway event.</summary>
        public IDisposable OnApplicationCommandPermissionsUpdated(Func<ApplicationCommandPermissionsUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<ApplicationCommandPermissionsUpdateEvent>("APPLICATION_COMMAND_PERMISSIONS_UPDATE", handler);

        // ── Internal ──────────────────────────────────────────────────────────────

        private async Task HandleInteractionAsync(InteractionCreateEvent interaction)
        {
            try
            {
                await _interactionHandler.HandleInteractionAsync(interaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in interaction handler for interaction {InteractionId}", interaction.Id);
            }
        }
    }
}
