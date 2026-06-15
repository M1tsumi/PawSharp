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
using PawSharp.Core.Events;
using PawSharp.Interactions;

namespace PawSharp.Client
{
    /// <summary>
    /// Represents the current connection state of the Discord client.
    /// </summary>
    public enum ClientConnectionState
    {
        /// <summary>Not connected to Discord.</summary>
        Disconnected,
        /// <summary>Attempting to establish a connection.</summary>
        Connecting,
        /// <summary>Connected and ready.</summary>
        Connected,
        /// <summary>Gracefully disconnecting.</summary>
        Disconnecting
    }

    /// <summary>
    /// Primary entry point for bots interacting with Discord.
    /// Composes the REST client, gateway, cache, and interaction handler.
    /// </summary>
    public class DiscordClient : IDiscordClient
    {
        private readonly PawSharpOptions _options;
        private readonly ILogger<DiscordClient> _logger;
        private readonly IDiscordRestClient _restClient;
        private readonly IGatewayClient _gatewayClient;
        private readonly IEntityCache _cache;
        private readonly InteractionHandler _interactionHandler;
        private readonly CacheManager _cacheManager;
        private ClientConnectionState _connectionState = ClientConnectionState.Disconnected;

        /// <summary>
        /// Gets the current connection state of the client.
        /// </summary>
        public ClientConnectionState ConnectionState => _connectionState;

        /// <summary>
        /// Raised when the client's connection state changes.
        /// </summary>
        public event EventHandler<ClientConnectionState>? ConnectionStateChanged;

        /// <summary>
        /// Gets whether the client is currently connected to Discord.
        /// </summary>
        public bool IsConnected => _connectionState == ClientConnectionState.Connected;

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
            IGatewayClient gatewayClient,
            InteractionHandler? interactionHandler = null)
        {
            _options       = options       ?? throw new ArgumentNullException(nameof(options));
            _cache         = cache         ?? throw new ArgumentNullException(nameof(cache));
            _logger        = logger        ?? throw new ArgumentNullException(nameof(logger));
            _restClient    = restClient    ?? throw new ArgumentNullException(nameof(restClient));
            _gatewayClient = gatewayClient ?? throw new ArgumentNullException(nameof(gatewayClient));

            _interactionHandler = interactionHandler ?? new InteractionHandler(_restClient, null);

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

        /// <summary>
        /// Opens the WebSocket connection to Discord's gateway.
        /// <para>
        /// It is recommended to set up global exception handlers for your application domain:
        /// <code>
        /// AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        ///     logger.LogError((Exception)args.ExceptionObject, "Unhandled exception");
        /// TaskScheduler.UnobservedTaskException += (sender, args) =>
        ///     logger.LogError(args.Exception, "Unobserved task exception");
        /// </code>
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        /// var client = new DiscordClient(options, cache, logger, rest, gateway);
        /// try
        /// {
        ///     await client.ConnectAsync();
        ///     Console.WriteLine("Bot is online!");
        /// }
        /// catch (DiscordException ex)
        /// {
        ///     Console.WriteLine($"Connection failed: {ex.Message}");
        /// }
        /// </code>
        /// </example>
        public async Task ConnectAsync()
        {
            ValidateIntentConfiguration();
            _logger.LogInformation("Connecting to Discord...");
            SetConnectionState(ClientConnectionState.Connecting);
            try
            {
                await _gatewayClient.ConnectAsync();
                SetConnectionState(ClientConnectionState.Connected);
                _logger.LogInformation("Connected to Discord.");
            }
            catch
            {
                SetConnectionState(ClientConnectionState.Disconnected);
                throw;
            }
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
            SetConnectionState(ClientConnectionState.Disconnecting);
            try
            {
                await _gatewayClient.DisconnectAsync();
                SetConnectionState(ClientConnectionState.Disconnected);
                _logger.LogInformation("Disconnected from Discord.");
            }
            catch
            {
                SetConnectionState(ClientConnectionState.Disconnected);
                throw;
            }
        }

        private void SetConnectionState(ClientConnectionState newState)
        {
            if (_connectionState != newState)
            {
                _connectionState = newState;
                ConnectionStateChanged?.Invoke(this, newState);
            }
        }

        /// <summary>
        /// Disconnects and reconnects to Discord gracefully.
        /// </summary>
        /// <param name="delayMs">Optional delay in milliseconds before reconnecting.</param>
        public async Task ReconnectAsync(int delayMs = 1000)
        {
            _logger.LogInformation("Reconnecting to Discord in {DelayMs}ms...", delayMs);
            await DisconnectAsync();
            if (delayMs > 0)
                await Task.Delay(delayMs);
            await ConnectAsync();
        }

        /// <summary>
        /// Configures global exception handlers for unhandled exceptions and unobserved task exceptions.
        /// Call this once at application startup to ensure no exceptions go unnoticed.
        /// </summary>
        /// <param name="logger">Optional logger to record exceptions.</param>
        /// <param name="onUnhandledException">Optional callback for custom handling (e.g., environment exit).</param>
        public static void SetupGlobalExceptionHandlers(
            ILogger? logger = null,
            Action<Exception, string>? onUnhandledException = null)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                var message = $"Unhandled exception (terminating: {args.IsTerminating})";
                logger?.LogCritical(ex, message);
                onUnhandledException?.Invoke(ex ?? new Exception("Unknown unhandled exception"), message);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                logger?.LogError(args.Exception, "Unobserved task exception");
                onUnhandledException?.Invoke(args.Exception, "Unobserved task exception");
                args.SetObserved();
            };
        }

        // ── Typed REST helpers ────────────────────────────────────────────────────

        /// <summary>Sends a plain-text message to a channel.</summary>
        /// <example>
        /// <code>
        /// await client.SendMessageAsync(channelId, "Hello, world!");
        /// </code>
        /// </example>
        public async Task<Message?> SendMessageAsync(ulong channelId, string content)
        {
            return await _restClient.CreateMessageAsync(channelId, new CreateMessageRequest { Content = content });
        }

        /// <summary>Sends a message with an embed to a channel.</summary>
        public async Task<Message?> SendMessageAsync(ulong channelId, string content, Embed embed)
        {
            return await _restClient.CreateMessageAsync(channelId, new CreateMessageRequest
            {
                Content = content,
                Embeds = new List<Embed> { embed }
            });
        }

        /// <summary>Sends a message with a single embed.</summary>
        public async Task<Message?> SendEmbedAsync(ulong channelId, Embed embed)
        {
            return await SendMessageAsync(channelId, "", embed);
        }

        /// <summary>
        /// Attempts to send a message and returns null instead of throwing on failure.
        /// </summary>
        public async Task<Message?> TrySendMessageAsync(ulong channelId, string content)
        {
            try
            {
                return await _restClient.CreateMessageAsync(channelId, new CreateMessageRequest { Content = content });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send message to channel {ChannelId}", channelId);
                return null;
            }
        }

        /// <summary>Sends a fully specified message to a channel.</summary>
        public async Task<Message?> SendMessageAsync(ulong channelId, CreateMessageRequest request)
        {
            return await _restClient.CreateMessageAsync(channelId, request);
        }

        /// <summary>
        /// Forwards a source message into another channel using Discord's message snapshot forwarding model.
        /// </summary>
        /// <example>
        /// <code>
        /// var forwarded = await client.ForwardMessageAsync(
        ///     targetChannelId: 987654321098765432,
        ///     sourceChannelId: 123456789012345678,
        ///     sourceMessageId: 111111111111111111);
        /// if (forwarded != null)
        /// {
        ///     Console.WriteLine($"Message forwarded: {forwarded.Id}");
        /// }
        /// </code>
        /// </example>
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

        /// <summary>Edits a message in a channel.</summary>
        public async Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, string content)
        {
            return await _restClient.EditMessageAsync(channelId, messageId, new EditMessageRequest { Content = content });
        }

        /// <summary>Edits a message in a channel with full options.</summary>
        public async Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request)
        {
            return await _restClient.EditMessageAsync(channelId, messageId, request);
        }

        /// <summary>Deletes a message from a channel.</summary>
        public async Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.DeleteMessageAsync(channelId, messageId);
        }

        /// <summary>Gets a message from a channel.</summary>
        public async Task<Message?> GetMessageAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.GetMessageAsync(channelId, messageId);
        }

        /// <summary>Triggers the typing indicator in a channel.</summary>
        public async Task<bool> TriggerTypingAsync(ulong channelId)
        {
            return await _restClient.TriggerTypingIndicatorAsync(channelId);
        }

        /// <summary>Gets a channel by ID.</summary>
        public async Task<Channel?> GetChannelAsync(ulong channelId)
        {
            return await _restClient.GetChannelAsync(channelId);
        }

        /// <summary>Modifies a channel.</summary>
        public async Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request)
        {
            return await _restClient.ModifyChannelAsync(channelId, request);
        }

        /// <summary>Gets a guild by ID.</summary>
        /// <example>
        /// <code>
        /// var guild = await client.GetGuildAsync(123456789012345678);
        /// if (guild != null)
        /// {
        ///     Console.WriteLine($"Guild: {guild.Name} (Members: {guild.MemberCount})");
        /// }
        /// </code>
        /// </example>
        public async Task<Guild?> GetGuildAsync(ulong guildId)
        {
            return await _restClient.GetGuildAsync(guildId);
        }

        /// <summary>Gets a guild member by ID.</summary>
        public async Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId)
        {
            return await _restClient.GetGuildMemberAsync(guildId, userId);
        }

        /// <summary>Removes a member from a guild.</summary>
        public async Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId)
        {
            return await _restClient.RemoveGuildMemberAsync(guildId, userId);
        }

        /// <summary>Gets roles for a guild.</summary>
        public async Task<List<Role>?> GetGuildRolesAsync(ulong guildId)
        {
            return await _restClient.GetGuildRolesAsync(guildId);
        }

        /// <summary>Creates a role in a guild.</summary>
        public async Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request)
        {
            return await _restClient.CreateGuildRoleAsync(guildId, request);
        }

        /// <summary>Adds a reaction to a message.</summary>
        public async Task<bool> AddReactionAsync(ulong channelId, ulong messageId, string emoji)
        {
            return await _restClient.CreateReactionAsync(channelId, messageId, emoji);
        }

        /// <summary>Removes a reaction from a message.</summary>
        public async Task<bool> RemoveReactionAsync(ulong channelId, ulong messageId, string emoji)
        {
            return await _restClient.DeleteOwnReactionAsync(channelId, messageId, emoji);
        }

        /// <summary>Removes a user's reaction from a message.</summary>
        public async Task<bool> RemoveUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId)
        {
            return await _restClient.DeleteUserReactionAsync(channelId, messageId, emoji, userId);
        }

        /// <summary>Replies to a message with plain text.</summary>
        /// <example>
        /// <code>
        /// client.OnMessageCreated(async msg =>
        /// {
        ///     if (msg.Content.Contains("!ping"))
        ///     {
        ///         await client.ReplyAsync(msg, "Pong!");
        ///     }
        /// });
        /// </code>
        /// </example>
        public async Task<Message?> ReplyAsync(MessageCreateEvent message, string content)
        {
            return await SendMessageAsync(message.ChannelId, content);
        }

        /// <summary>Replies to a message with an embed.</summary>
        public async Task<Message?> ReplyAsync(MessageCreateEvent message, string content, Embed embed)
        {
            return await SendMessageAsync(message.ChannelId, content, embed);
        }

        /// <summary>Replies to a message with a full request.</summary>
        public async Task<Message?> ReplyAsync(MessageCreateEvent message, CreateMessageRequest request)
        {
            return await SendMessageAsync(message.ChannelId, request);
        }

        /// <summary>
        /// Attempts to reply to a message event gracefully, returning null on failure.
        /// </summary>
        public async Task<Message?> TryReplyAsync(MessageCreateEvent message, string content)
        {
            try
            {
                return await ReplyAsync(message, content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reply to message {MessageId}", message.Id);
                return null;
            }
        }

        // ── Additional REST helpers ───────────────────────────────────────────────

        // User operations ───────────────────────────────────────────────────────────

        /// <summary>Gets a user by ID.</summary>
        /// <example>
        /// <code>
        /// var user = await client.GetUserAsync(123456789012345678);
        /// if (user != null)
        /// {
        ///     Console.WriteLine($"User: {user.Username}");
        /// }
        /// </code>
        /// </example>
        public async Task<User?> GetUserAsync(ulong userId)
        {
            return await _restClient.GetUserAsync(userId);
        }

        /// <summary>Modifies the current bot user.</summary>
        public async Task ModifyCurrentUserAsync(string? username = null, string? avatar = null, string? banner = null, string? avatarDecorationData = null)
        {
            await _restClient.ModifyCurrentUserAsync(username, avatar, banner, avatarDecorationData);
        }

        /// <summary>Gets the current bot's guilds.</summary>
        public async Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null)
        {
            return await _restClient.GetCurrentUserGuildsAsync(limit, before, after);
        }

        /// <summary>Leaves a guild.</summary>
        public async Task<bool> LeaveGuildAsync(ulong guildId)
        {
            return await _restClient.LeaveGuildAsync(guildId);
        }

        // Additional Message operations ──────────────────────────────────────────────

        /// <summary>Sends a file to a channel.</summary>
        /// <example>
        /// <code>
        /// await using var fileStream = File.OpenRead("image.png");
        /// var message = await client.SendFileAsync(channelId, fileStream, "image.png");
        /// if (message != null)
        /// {
        ///     Console.WriteLine($"File sent: {message.Id}");
        /// }
        /// </code>
        /// </example>
        public async Task<Message?> SendFileAsync(ulong channelId, System.IO.Stream fileStream, string fileName, CreateMessageRequest? messageRequest = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return await _restClient.SendFileAsync(channelId, fileStream, fileName, messageRequest, cancellationToken);
        }

        /// <summary>Sends multiple files to a channel.</summary>
        public async Task<Message?> SendFilesAsync(ulong channelId, IEnumerable<(System.IO.Stream Stream, string FileName)> files, CreateMessageRequest? messageRequest = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return await _restClient.SendFilesAsync(channelId, files, messageRequest, cancellationToken);
        }

        /// <summary>Gets messages from a channel.</summary>
        public async Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null)
        {
            return await _restClient.GetChannelMessagesAsync(channelId, limit, around, before, after);
        }

        /// <summary>Bulk deletes messages from a channel.</summary>
        public async Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds)
        {
            return await _restClient.BulkDeleteMessagesAsync(channelId, messageIds);
        }

        /// <summary>Pins a message in a channel.</summary>
        public async Task<bool> PinMessageAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.PinMessageAsync(channelId, messageId);
        }

        /// <summary>Unpins a message in a channel.</summary>
        public async Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.UnpinMessageAsync(channelId, messageId);
        }

        /// <summary>Gets pinned messages from a channel.</summary>
        public async Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId)
        {
            return await _restClient.GetPinnedMessagesAsync(channelId);
        }

        /// <summary>Crossposts a message to following channels.</summary>
        public async Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.CrosspostMessageAsync(channelId, messageId);
        }

        // Channel operations ───────────────────────────────────────────────────────

        /// <summary>Deletes a channel.</summary>
        public async Task<bool> DeleteChannelAsync(ulong channelId)
        {
            return await _restClient.DeleteChannelAsync(channelId);
        }

        /// <summary>Creates a channel in a guild.</summary>
        public async Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request)
        {
            return await _restClient.CreateGuildChannelAsync(guildId, request);
        }

        /// <summary>Gets invites for a channel.</summary>
        public async Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId)
        {
            return await _restClient.GetChannelInvitesAsync(channelId);
        }

        /// <summary>Creates an invite for a channel.</summary>
        public async Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request)
        {
            return await _restClient.CreateChannelInviteAsync(channelId, request);
        }

        /// <summary>Deletes a channel permission overwrite.</summary>
        public async Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId)
        {
            return await _restClient.DeleteChannelPermissionAsync(channelId, overwriteId);
        }

        /// <summary>Edits channel permissions.</summary>
        public async Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request)
        {
            return await _restClient.EditChannelPermissionsAsync(channelId, overwriteId, request);
        }

        // Guild operations ───────────────────────────────────────────────────────────

        /// <summary>Creates a guild.</summary>
        public async Task<Guild?> CreateGuildAsync(CreateGuildRequest request)
        {
            return await _restClient.CreateGuildAsync(request);
        }

        /// <summary>Modifies a guild.</summary>
        public async Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request)
        {
            return await _restClient.ModifyGuildAsync(guildId, request);
        }

        /// <summary>Deletes a guild.</summary>
        public async Task<bool> DeleteGuildAsync(ulong guildId)
        {
            return await _restClient.DeleteGuildAsync(guildId);
        }

        /// <summary>Modifies a guild's MFA level.</summary>
        public async Task<int?> ModifyGuildMfaLevelAsync(ulong guildId, int level)
        {
            return await _restClient.ModifyGuildMfaLevelAsync(guildId, level);
        }

        /// <summary>Gets channels for a guild.</summary>
        public async Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId)
        {
            return await _restClient.GetGuildChannelsAsync(guildId);
        }

        /// <summary>Gets members for a guild.</summary>
        public async Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000, ulong? after = null)
        {
            return await _restClient.GetGuildMembersAsync(guildId, limit, after);
        }

        /// <summary>Adds a member to a guild.</summary>
        public async Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request)
        {
            return await _restClient.AddGuildMemberAsync(guildId, userId, request);
        }

        /// <summary>Modifies a guild member.</summary>
        public async Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request)
        {
            return await _restClient.ModifyGuildMemberAsync(guildId, userId, request);
        }

        /// <summary>Gets bans for a guild.</summary>
        public async Task<List<Ban>?> GetGuildBansAsync(ulong guildId, ulong? before = null, ulong? after = null, int? limit = null)
        {
            return await _restClient.GetGuildBansAsync(guildId, before, after, limit);
        }

        /// <summary>Gets a ban for a guild.</summary>
        public async Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId)
        {
            return await _restClient.GetGuildBanAsync(guildId, userId);
        }

        /// <summary>Creates a ban for a guild.</summary>
        public async Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null)
        {
            return await _restClient.CreateGuildBanAsync(guildId, userId, deleteMessageDays, reason);
        }

        /// <summary>Removes a ban from a guild.</summary>
        public async Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId)
        {
            return await _restClient.RemoveGuildBanAsync(guildId, userId);
        }

        // Role operations ──────────────────────────────────────────────────────────

        /// <summary>Modifies a guild role.</summary>
        public async Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request)
        {
            return await _restClient.ModifyGuildRoleAsync(guildId, roleId, request);
        }

        /// <summary>Deletes a guild role.</summary>
        public async Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId)
        {
            return await _restClient.DeleteGuildRoleAsync(guildId, roleId);
        }

        /// <summary>Adds a role to a guild member.</summary>
        public async Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
        {
            return await _restClient.AddGuildMemberRoleAsync(guildId, userId, roleId);
        }

        /// <summary>Removes a role from a guild member.</summary>
        public async Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
        {
            return await _restClient.RemoveGuildMemberRoleAsync(guildId, userId, roleId);
        }

        // Thread operations ──────────────────────────────────────────────────────────

        /// <summary>Creates a thread.</summary>
        /// <example>
        /// <code>
        /// var thread = await client.CreateThreadAsync(channelId, new CreateThreadRequest
        /// {
        ///     Name = "Discussion",
        ///     AutoArchiveDuration = 60
        /// });
        /// if (thread != null)
        /// {
        ///     Console.WriteLine($"Thread created: {thread.Name}");
        /// }
        /// </code>
        /// </example>
        public async Task<Channel?> CreateThreadAsync(ulong channelId, CreateThreadRequest request)
        {
            return await _restClient.CreateThreadAsync(channelId, request);
        }

        /// <summary>Creates a thread from a message.</summary>
        public async Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request)
        {
            return await _restClient.CreateThreadFromMessageAsync(channelId, messageId, request);
        }

        /// <summary>Creates a thread in a forum channel.</summary>
        public async Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request)
        {
            return await _restClient.CreateThreadInForumAsync(channelId, request);
        }

        /// <summary>Joins a thread.</summary>
        public async Task<bool> JoinThreadAsync(ulong channelId)
        {
            return await _restClient.JoinThreadAsync(channelId);
        }

        /// <summary>Adds a member to a thread.</summary>
        public async Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId)
        {
            return await _restClient.AddThreadMemberAsync(channelId, userId);
        }

        /// <summary>Leaves a thread.</summary>
        public async Task<bool> LeaveThreadAsync(ulong channelId)
        {
            return await _restClient.LeaveThreadAsync(channelId);
        }

        /// <summary>Removes a member from a thread.</summary>
        public async Task<bool> RemoveThreadMemberAsync(ulong channelId, ulong userId)
        {
            return await _restClient.RemoveThreadMemberAsync(channelId, userId);
        }

        /// <summary>Gets a thread member.</summary>
        public async Task<ThreadMember?> GetThreadMemberAsync(ulong channelId, ulong userId)
        {
            return await _restClient.GetThreadMemberAsync(channelId, userId);
        }

        /// <summary>Gets thread members.</summary>
        public async Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId, bool withMember = false, ulong? after = null, int? limit = null)
        {
            return await _restClient.GetThreadMembersAsync(channelId, withMember, after, limit);
        }

        /// <summary>Gets active threads for a guild.</summary>
        public async Task<ActiveThreadsResponse?> GetActiveThreadsAsync(ulong guildId)
        {
            return await _restClient.GetActiveThreadsAsync(guildId);
        }

        /// <summary>
        /// Gets an existing thread by name or creates a new one.
        /// </summary>
        public async Task<Channel?> GetOrCreateThreadAsync(ulong channelId, string threadName, int autoArchiveDuration = 60)
        {
            var channel = await _restClient.GetChannelAsync(channelId);
            if (channel == null) return null;

            var activeThreads = await _restClient.GetActiveThreadsAsync(channel.GuildId ?? 0);
            var existing = activeThreads?.Threads?.FirstOrDefault(t =>
                t.Name?.Equals(threadName, StringComparison.OrdinalIgnoreCase) == true);
            if (existing != null) return existing;

            return await _restClient.CreateThreadAsync(channelId, new CreateThreadRequest
            {
                Name = threadName,
                AutoArchiveDuration = autoArchiveDuration
            });
        }

        /// <summary>Gets public archived threads for a channel.</summary>
        public async Task<ArchivedThreadsResponse?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
        {
            return await _restClient.GetPublicArchivedThreadsAsync(channelId, before, limit);
        }

        /// <summary>Gets private archived threads for a channel.</summary>
        public async Task<ArchivedThreadsResponse?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
        {
            return await _restClient.GetPrivateArchivedThreadsAsync(channelId, before, limit);
        }

        /// <summary>Gets joined private archived threads for a channel.</summary>
        public async Task<ArchivedThreadsResponse?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
        {
            return await _restClient.GetJoinedPrivateArchivedThreadsAsync(channelId, before, limit);
        }

        // Webhook operations ─────────────────────────────────────────────────────────

        /// <summary>Creates a webhook for a channel.</summary>
        public async Task<Webhook?> CreateWebhookAsync(ulong channelId, CreateWebhookRequest request)
        {
            return await _restClient.CreateWebhookAsync(channelId, request);
        }

        /// <summary>Gets webhooks for a channel.</summary>
        public async Task<List<Webhook>?> GetChannelWebhooksAsync(ulong channelId)
        {
            return await _restClient.GetChannelWebhooksAsync(channelId);
        }

        /// <summary>Gets webhooks for a guild.</summary>
        public async Task<List<Webhook>?> GetGuildWebhooksAsync(ulong guildId)
        {
            return await _restClient.GetGuildWebhooksAsync(guildId);
        }

        /// <summary>Gets a webhook by ID.</summary>
        public async Task<Webhook?> GetWebhookAsync(ulong webhookId)
        {
            return await _restClient.GetWebhookAsync(webhookId);
        }

        /// <summary>Gets a webhook by ID and token.</summary>
        public async Task<Webhook?> GetWebhookWithTokenAsync(ulong webhookId, string token)
        {
            return await _restClient.GetWebhookWithTokenAsync(webhookId, token);
        }

        /// <summary>Modifies a webhook.</summary>
        public async Task<Webhook?> ModifyWebhookAsync(ulong webhookId, ModifyWebhookRequest request)
        {
            return await _restClient.ModifyWebhookAsync(webhookId, request);
        }

        /// <summary>Modifies a webhook with token.</summary>
        public async Task<Webhook?> ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request)
        {
            return await _restClient.ModifyWebhookWithTokenAsync(webhookId, token, request);
        }

        /// <summary>Deletes a webhook.</summary>
        public async Task<bool> DeleteWebhookAsync(ulong webhookId)
        {
            return await _restClient.DeleteWebhookAsync(webhookId);
        }

        /// <summary>Deletes a webhook with token.</summary>
        public async Task<bool> DeleteWebhookWithTokenAsync(ulong webhookId, string token)
        {
            return await _restClient.DeleteWebhookWithTokenAsync(webhookId, token);
        }

        /// <summary>Executes a webhook.</summary>
        public async Task<Message?> ExecuteWebhookAsync(ulong webhookId, string token, ExecuteWebhookRequest request, ulong? threadId = null)
        {
            return await _restClient.ExecuteWebhookAsync(webhookId, token, request, threadId);
        }

        /// <summary>Gets a webhook message.</summary>
        public async Task<Message?> GetWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null)
        {
            return await _restClient.GetWebhookMessageAsync(webhookId, token, messageId, threadId);
        }

        /// <summary>Edits a webhook message.</summary>
        public async Task<Message?> EditWebhookMessageAsync(ulong webhookId, string token, ulong messageId, EditMessageRequest request, ulong? threadId = null)
        {
            return await _restClient.EditWebhookMessageAsync(webhookId, token, messageId, request, threadId);
        }

        /// <summary>Deletes a webhook message.</summary>
        public async Task<bool> DeleteWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null)
        {
            return await _restClient.DeleteWebhookMessageAsync(webhookId, token, messageId, threadId);
        }

        /// <summary>Executes a Slack-compatible webhook.</summary>
        public async Task<bool> ExecuteSlackCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false)
        {
            return await _restClient.ExecuteSlackCompatibleWebhookAsync(webhookId, token, payload, wait);
        }

        /// <summary>Executes a GitHub-compatible webhook.</summary>
        public async Task<bool> ExecuteGitHubCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false)
        {
            return await _restClient.ExecuteGitHubCompatibleWebhookAsync(webhookId, token, payload, wait);
        }

        // DM operations ──────────────────────────────────────────────────────────────

        /// <summary>Creates a DM channel.</summary>
        public async Task<Channel?> CreateDmAsync(ulong recipientId)
        {
            return await _restClient.CreateDmAsync(recipientId);
        }

        /// <summary>Creates a group DM.</summary>
        public async Task<Channel?> CreateGroupDmAsync(List<string> accessTokens, Dictionary<string, string>? nicks = null)
        {
            return await _restClient.CreateGroupDmAsync(accessTokens, nicks);
        }

        /// <summary>
        /// Sends a direct message to a user by creating a DM channel first.
        /// </summary>
        /// <param name="userId">The user to send the DM to.</param>
        /// <param name="content">The message content.</param>
        /// <returns>The sent message, or null if the DM channel could not be created.</returns>
        public async Task<Message?> SendDirectMessageAsync(ulong userId, string content)
        {
            var dm = await _restClient.CreateDmAsync(userId);
            if (dm == null) return null;
            return await _restClient.CreateMessageAsync(dm.Id, new CreateMessageRequest { Content = content });
        }

        // Scheduled Event operations ───────────────────────────────────────────────────

        /// <summary>Gets scheduled events for a guild.</summary>
        public async Task<List<GuildScheduledEvent>?> GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount = null)
        {
            return await _restClient.GetGuildScheduledEventsAsync(guildId, withUserCount);
        }

        /// <summary>Gets a scheduled event for a guild.</summary>
        public async Task<GuildScheduledEvent?> GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount = null)
        {
            return await _restClient.GetGuildScheduledEventAsync(guildId, eventId, withUserCount);
        }

        /// <summary>Deletes a guild scheduled event.</summary>
        public async Task<bool> DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId)
        {
            return await _restClient.DeleteGuildScheduledEventAsync(guildId, eventId);
        }

        /// <summary>Gets users for a guild scheduled event.</summary>
        public async Task<List<User>?> GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit = null, bool? withMember = null, ulong? before = null, ulong? after = null)
        {
            return await _restClient.GetGuildScheduledEventUsersAsync(guildId, eventId, limit, withMember, before, after);
        }

        // Audit Log operations ───────────────────────────────────────────────────────

        /// <summary>Gets audit logs for a guild.</summary>
        public async Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, ulong? after = null, int? limit = null)
        {
            return await _restClient.GetGuildAuditLogsAsync(guildId, userId, actionType, before, after, limit);
        }

        // Auto-Moderation operations ──────────────────────────────────────────────────

        /// <summary>Lists auto-moderation rules for a guild.</summary>
        public async Task<List<AutoModerationRule>?> ListAutoModerationRulesAsync(ulong guildId)
        {
            return await _restClient.ListAutoModerationRulesAsync(guildId);
        }

        /// <summary>Gets an auto-moderation rule for a guild.</summary>
        public async Task<AutoModerationRule?> GetAutoModerationRuleAsync(ulong guildId, ulong ruleId)
        {
            return await _restClient.GetAutoModerationRuleAsync(guildId, ruleId);
        }

        /// <summary>Deletes an auto-moderation rule for a guild.</summary>
        public async Task<bool> DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId)
        {
            return await _restClient.DeleteAutoModerationRuleAsync(guildId, ruleId);
        }

        // Stage Instance operations ──────────────────────────────────────────────────

        /// <summary>Gets a stage instance.</summary>
        public async Task<StageInstance?> GetStageInstanceAsync(ulong channelId)
        {
            return await _restClient.GetStageInstanceAsync(channelId);
        }

        /// <summary>Deletes a stage instance.</summary>
        public async Task<bool> DeleteStageInstanceAsync(ulong channelId)
        {
            return await _restClient.DeleteStageInstanceAsync(channelId);
        }

        // Sticker operations ──────────────────────────────────────────────────────────

        /// <summary>Gets a sticker.</summary>
        public async Task<Sticker?> GetStickerAsync(ulong stickerId)
        {
            return await _restClient.GetStickerAsync(stickerId);
        }

        /// <summary>Gets sticker packs.</summary>
        public async Task<List<StickerPack>?> GetNitroStickerPacksAsync()
        {
            return await _restClient.GetNitroStickerPacksAsync();
        }

        /// <summary>Gets guild stickers.</summary>
        public async Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId)
        {
            return await _restClient.GetGuildStickersAsync(guildId);
        }

        /// <summary>Gets a guild sticker.</summary>
        public async Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId)
        {
            return await _restClient.GetGuildStickerAsync(guildId, stickerId);
        }

        /// <summary>Deletes a guild sticker.</summary>
        public async Task<bool> DeleteGuildStickerAsync(ulong guildId, ulong stickerId)
        {
            return await _restClient.DeleteGuildStickerAsync(guildId, stickerId);
        }

        // Voice Region operations ────────────────────────────────────────────────────

        /// <summary>Gets voice regions.</summary>
        public async Task<List<VoiceRegion>?> GetVoiceRegionsAsync()
        {
            return await _restClient.GetVoiceRegionsAsync();
        }

        /// <summary>Gets voice regions for a guild.</summary>
        public async Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId)
        {
            return await _restClient.GetGuildVoiceRegionsAsync(guildId);
        }

        // Application Command operations ──────────────────────────────────────────────

        /// <summary>Gets global application commands.</summary>
        public async Task<List<ApplicationCommand>?> GetGlobalApplicationCommandsAsync(ulong applicationId)
        {
            return await _restClient.GetGlobalApplicationCommandsAsync(applicationId);
        }

        /// <summary>Creates a global application command.</summary>
        public async Task<ApplicationCommand?> CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request)
        {
            return await _restClient.CreateGlobalApplicationCommandAsync(applicationId, request);
        }

        /// <summary>Overwrites global application commands.</summary>
        public async Task<List<ApplicationCommand>?> BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands)
        {
            return await _restClient.BulkOverwriteGlobalApplicationCommandsAsync(applicationId, commands);
        }

        /// <summary>Gets a global application command.</summary>
        public async Task<ApplicationCommand?> GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)
        {
            return await _restClient.GetGlobalApplicationCommandAsync(applicationId, commandId);
        }

        /// <summary>Edits a global application command.</summary>
        public async Task<ApplicationCommand?> EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request)
        {
            return await _restClient.EditGlobalApplicationCommandAsync(applicationId, commandId, request);
        }

        /// <summary>Deletes a global application command.</summary>
        public async Task<bool> DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)
        {
            return await _restClient.DeleteGlobalApplicationCommandAsync(applicationId, commandId);
        }

        /// <summary>Gets guild application commands.</summary>
        public async Task<List<ApplicationCommand>?> GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId)
        {
            return await _restClient.GetGuildApplicationCommandsAsync(applicationId, guildId);
        }

        /// <summary>Creates a guild application command.</summary>
        public async Task<ApplicationCommand?> CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request)
        {
            return await _restClient.CreateGuildApplicationCommandAsync(applicationId, guildId, request);
        }

        /// <summary>Overwrites guild application commands.</summary>
        public async Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands)
        {
            return await _restClient.BulkOverwriteGuildApplicationCommandsAsync(applicationId, guildId, commands);
        }

        /// <summary>Gets a guild application command.</summary>
        public async Task<ApplicationCommand?> GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)
        {
            return await _restClient.GetGuildApplicationCommandAsync(applicationId, guildId, commandId);
        }

        /// <summary>Edits a guild application command.</summary>
        public async Task<ApplicationCommand?> EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request)
        {
            return await _restClient.EditGuildApplicationCommandAsync(applicationId, guildId, commandId, request);
        }

        /// <summary>Deletes a guild application command.</summary>
        public async Task<bool> DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)
        {
            return await _restClient.DeleteGuildApplicationCommandAsync(applicationId, guildId, commandId);
        }

        // Application Command Permissions operations ────────────────────────────────────

        /// <summary>Gets guild application command permissions.</summary>
        public async Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId)
        {
            return await _restClient.GetGuildApplicationCommandPermissionsAsync(applicationId, guildId);
        }

        /// <summary>Gets application command permissions for a specific command.</summary>
        public async Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId)
        {
            return await _restClient.GetApplicationCommandPermissionsAsync(applicationId, guildId, commandId);
        }

        /// <summary>Edits application command permissions for a specific command.</summary>
        public async Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions)
        {
            return await _restClient.EditApplicationCommandPermissionsAsync(applicationId, guildId, commandId, permissions);
        }

        /// <summary>Batch edits application command permissions for all commands.</summary>
        public async Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions)
        {
            return await _restClient.BatchEditApplicationCommandPermissionsAsync(applicationId, guildId, permissions);
        }

        // Guild Emoji operations ────────────────────────────────────────────────────────

        /// <summary>Gets emojis for a guild.</summary>
        public async Task<List<Emoji>?> ListGuildEmojisAsync(ulong guildId)
        {
            return await _restClient.ListGuildEmojisAsync(guildId);
        }

        /// <summary>Gets an emoji for a guild.</summary>
        public async Task<Emoji?> GetGuildEmojiAsync(ulong guildId, ulong emojiId)
        {
            return await _restClient.GetGuildEmojiAsync(guildId, emojiId);
        }

        /// <summary>Deletes an emoji from a guild.</summary>
        public async Task<bool> DeleteGuildEmojiAsync(ulong guildId, ulong emojiId)
        {
            return await _restClient.DeleteGuildEmojiAsync(guildId, emojiId);
        }

        // ApplicationEmoji operations ───────────────────────────────────────────────────

        /// <summary>Gets emojis for the current application.</summary>
        public async Task<List<Emoji>?> ListApplicationEmojisAsync(ulong applicationId)
        {
            return await _restClient.ListApplicationEmojisAsync(applicationId);
        }

        /// <summary>Gets an emoji for the current application.</summary>
        public async Task<Emoji?> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId)
        {
            return await _restClient.GetApplicationEmojiAsync(applicationId, emojiId);
        }

        /// <summary>Deletes an emoji from the current application.</summary>
        public async Task<bool> DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId)
        {
            return await _restClient.DeleteApplicationEmojiAsync(applicationId, emojiId);
        }

        // Guild Integration operations ──────────────────────────────────────────────────

        /// <summary>Gets integrations for a guild.</summary>
        public async Task<List<GuildIntegration>?> GetGuildIntegrationsAsync(ulong guildId)
        {
            return await _restClient.GetGuildIntegrationsAsync(guildId);
        }

        /// <summary>Deletes an integration from a guild.</summary>
        public async Task<bool> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId)
        {
            return await _restClient.DeleteGuildIntegrationAsync(guildId, integrationId);
        }

        // Guild Invite operations ─────────────────────────────────────────────────────

        /// <summary>Gets invites for a guild.</summary>
        public async Task<List<Invite>?> GetGuildInvitesAsync(ulong guildId)
        {
            return await _restClient.GetGuildInvitesAsync(guildId);
        }

        // Guild Prune operations ──────────────────────────────────────────────────────

        /// <summary>Gets prune count for a guild.</summary>
        public async Task<GuildPruneResult?> GetGuildPruneCountAsync(ulong guildId, int? days = null, List<ulong>? includeRoles = null)
        {
            return await _restClient.GetGuildPruneCountAsync(guildId, days, includeRoles);
        }

        /// <summary>Begins a prune operation for a guild.</summary>
        public async Task<GuildPruneResult?> BeginGuildPruneAsync(ulong guildId, BeginGuildPruneRequest request, string? reason = null)
        {
            return await _restClient.BeginGuildPruneAsync(guildId, request, reason);
        }

        // Guild Template operations ─────────────────────────────────────────────────────

        /// <summary>Gets templates for a guild.</summary>
        public async Task<List<GuildTemplate>?> GetGuildTemplatesAsync(ulong guildId)
        {
            return await _restClient.GetGuildTemplatesAsync(guildId);
        }

        /// <summary>Gets a guild template.</summary>
        public async Task<GuildTemplate?> GetGuildTemplateAsync(string templateCode)
        {
            return await _restClient.GetGuildTemplateAsync(templateCode);
        }

        /// <summary>Syncs a guild template.</summary>
        public async Task<GuildTemplate?> SyncGuildTemplateAsync(ulong guildId, string templateCode)
        {
            return await _restClient.SyncGuildTemplateAsync(guildId, templateCode);
        }

        /// <summary>Modifies a guild template.</summary>
        public async Task<GuildTemplate?> ModifyGuildTemplateAsync(ulong guildId, string templateCode, ModifyGuildTemplateRequest request)
        {
            return await _restClient.ModifyGuildTemplateAsync(guildId, templateCode, request);
        }

        /// <summary>Deletes a guild template.</summary>
        public async Task<GuildTemplate?> DeleteGuildTemplateAsync(ulong guildId, string templateCode)
        {
            return await _restClient.DeleteGuildTemplateAsync(guildId, templateCode);
        }

        // OAuth2 operations ───────────────────────────────────────────────────────────

        /// <summary>Gets the current application.</summary>
        public async Task<Application?> GetCurrentApplicationAsync()
        {
            return await _restClient.GetCurrentApplicationAsync();
        }

        /// <summary>Gets the current bot application info.</summary>
        public async Task<Application?> GetCurrentBotApplicationInfoAsync()
        {
            return await _restClient.GetCurrentBotApplicationInfoAsync();
        }

        /// <summary>Gets authorization information.</summary>
        public async Task<OAuth2Info?> GetCurrentAuthorizationInfoAsync()
        {
            return await _restClient.GetCurrentAuthorizationInfoAsync();
        }

        /// <summary>Edits the current application.</summary>
        public async Task<Application?> EditCurrentApplicationAsync(EditCurrentApplicationRequest request)
        {
            return await _restClient.EditCurrentApplicationAsync(request);
        }

        // Poll operations ─────────────────────────────────────────────────────────────

        /// <summary>Gets voters for a poll answer.</summary>
        public async Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null)
        {
            return await _restClient.GetAnswerVotersAsync(channelId, messageId, answerId, limit, after);
        }

        /// <summary>Ends a poll.</summary>
        public async Task<Message?> EndPollAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.EndPollAsync(channelId, messageId);
        }

        // SKU/Entitlement/Subscription operations ───────────────────────────────────────

        /// <summary>Gets SKUs.</summary>
        public async Task<List<Sku>?> ListSkusAsync(ulong applicationId)
        {
            return await _restClient.ListSkusAsync(applicationId);
        }

        /// <summary>Gets entitlements.</summary>
        public async Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null)
        {
            return await _restClient.ListEntitlementsAsync(applicationId, userId, skuIds, before, after, limit, guildId, excludeEnded);
        }

        /// <summary>Gets an entitlement.</summary>
        public async Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId)
        {
            return await _restClient.GetEntitlementAsync(applicationId, entitlementId);
        }

        /// <summary>Creates a test entitlement.</summary>
        public async Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request)
        {
            return await _restClient.CreateTestEntitlementAsync(applicationId, request);
        }

        /// <summary>Deletes a test entitlement.</summary>
        public async Task<bool> DeleteTestEntitlementAsync(ulong applicationId, ulong entitlementId)
        {
            return await _restClient.DeleteTestEntitlementAsync(applicationId, entitlementId);
        }

        /// <summary>Consumes an entitlement.</summary>
        public async Task<bool> ConsumeEntitlementAsync(ulong applicationId, ulong entitlementId)
        {
            return await _restClient.ConsumeEntitlementAsync(applicationId, entitlementId);
        }

        /// <summary>Lists SKU subscriptions.</summary>
        public async Task<List<Subscription>?> ListSkuSubscriptionsAsync(ulong skuId, ulong? before = null, ulong? after = null, int? limit = null, ulong? userId = null)
        {
            return await _restClient.ListSkuSubscriptionsAsync(skuId, before, after, limit, userId);
        }

        /// <summary>Gets SKU subscription.</summary>
        public async Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId)
        {
            return await _restClient.GetSkuSubscriptionAsync(skuId, subscriptionId);
        }

        // Soundboard operations ──────────────────────────────────────────────────────────

        /// <summary>Lists default soundboard sounds.</summary>
        public async Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync()
        {
            return await _restClient.ListDefaultSoundboardSoundsAsync();
        }

        /// <summary>Lists guild soundboard sounds.</summary>
        public async Task<List<SoundboardSound>?> ListGuildSoundboardSoundsAsync(ulong guildId)
        {
            return await _restClient.ListGuildSoundboardSoundsAsync(guildId);
        }

        /// <summary>Gets a soundboard sound.</summary>
        public async Task<SoundboardSound?> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId)
        {
            return await _restClient.GetGuildSoundboardSoundAsync(guildId, soundId);
        }

        /// <summary>Deletes a soundboard sound.</summary>
        public async Task<bool> DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId)
        {
            return await _restClient.DeleteGuildSoundboardSoundAsync(guildId, soundId);
        }

        /// <summary>Sends a soundboard sound.</summary>
        public async Task<bool> SendSoundboardSoundAsync(ulong channelId, SendSoundboardSoundRequest request)
        {
            return await _restClient.SendSoundboardSoundAsync(channelId, request);
        }

        // Guild Onboarding operations ───────────────────────────────────────────────────

        /// <summary>Gets onboarding for a guild.</summary>
        public async Task<GuildOnboarding?> GetGuildOnboardingAsync(ulong guildId)
        {
            return await _restClient.GetGuildOnboardingAsync(guildId);
        }

        /// <summary>Modifies onboarding for a guild.</summary>
        public async Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request)
        {
            return await _restClient.ModifyGuildOnboardingAsync(guildId, request);
        }

        // Application Role Connection operations ────────────────────────────────────────

        /// <summary>Gets role connection metadata for an application.</summary>
        public async Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId)
        {
            return await _restClient.GetApplicationRoleConnectionMetadataAsync(applicationId);
        }

        /// <summary>Updates role connection metadata for an application.</summary>
        public async Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records)
        {
            return await _restClient.UpdateApplicationRoleConnectionMetadataAsync(applicationId, records);
        }

        /// <summary>Gets role connections for the current user.</summary>
        public async Task<ApplicationRoleConnection?> GetUserApplicationRoleConnectionAsync(ulong applicationId)
        {
            return await _restClient.GetUserApplicationRoleConnectionAsync(applicationId);
        }

        /// <summary>Updates role connections for the current user.</summary>
        public async Task<ApplicationRoleConnection?> UpdateUserApplicationRoleConnectionAsync(ulong applicationId, UpdateUserApplicationRoleConnectionRequest request)
        {
            return await _restClient.UpdateUserApplicationRoleConnectionAsync(applicationId, request);
        }

        // Reaction query operations ─────────────────────────────────────────────────────

        /// <summary>Gets reactions for a message.</summary>
        public async Task<List<User>?> GetReactionsAsync(ulong channelId, ulong messageId, string emoji, int? type = null, ulong? after = null, int? limit = null)
        {
            return await _restClient.GetReactionsAsync(channelId, messageId, emoji, type, after, limit);
        }

        /// <summary>Deletes all reactions for a message.</summary>
        public async Task<bool> DeleteAllReactionsAsync(ulong channelId, ulong messageId)
        {
            return await _restClient.DeleteAllReactionsAsync(channelId, messageId);
        }

        /// <summary>Deletes all reactions for an emoji on a message.</summary>
        public async Task<bool> DeleteAllReactionsForEmojiAsync(ulong channelId, ulong messageId, string emoji)
        {
            return await _restClient.DeleteAllReactionsForEmojiAsync(channelId, messageId, emoji);
        }

        // Guild widget operations ───────────────────────────────────────────────────────

        /// <summary>Gets guild widget.</summary>
        public async Task<GuildWidget?> GetGuildWidgetAsync(ulong guildId)
        {
            return await _restClient.GetGuildWidgetAsync(guildId);
        }

        /// <summary>Modifies guild widget.</summary>
        public async Task<GuildWidgetSettings?> ModifyGuildWidgetAsync(ulong guildId, ModifyGuildWidgetRequest request)
        {
            return await _restClient.ModifyGuildWidgetAsync(guildId, request);
        }

        /// <summary>Gets guild widget settings.</summary>
        public async Task<GuildWidgetSettings?> GetGuildWidgetSettingsAsync(ulong guildId)
        {
            return await _restClient.GetGuildWidgetSettingsAsync(guildId);
        }

        // Guild vanity URL operations ─────────────────────────────────────────────────────

        /// <summary>Gets guild vanity URL.</summary>
        public async Task<VanityUrl?> GetGuildVanityUrlAsync(ulong guildId)
        {
            return await _restClient.GetGuildVanityUrlAsync(guildId);
        }

        // Guild welcome screen operations ──────────────────────────────────────────────────

        /// <summary>Gets guild welcome screen.</summary>
        public async Task<WelcomeScreen?> GetGuildWelcomeScreenAsync(ulong guildId)
        {
            return await _restClient.GetGuildWelcomeScreenAsync(guildId);
        }

        /// <summary>Modifies guild welcome screen.</summary>
        public async Task<WelcomeScreen?> ModifyGuildWelcomeScreenAsync(ulong guildId, ModifyGuildWelcomeScreenRequest request)
        {
            return await _restClient.ModifyGuildWelcomeScreenAsync(guildId, request);
        }

        // Guild channel/role position operations ───────────────────────────────────────────

        /// <summary>Modifies guild channel positions.</summary>
        public async Task<bool> ModifyGuildChannelPositionsAsync(ulong guildId, List<ModifyChannelPositionRequest> positions)
        {
            return await _restClient.ModifyGuildChannelPositionsAsync(guildId, positions);
        }

        /// <summary>Modifies guild role positions.</summary>
        public async Task<List<Role>?> ModifyGuildRolePositionsAsync(ulong guildId, List<ModifyRolePositionRequest> positions)
        {
            return await _restClient.ModifyGuildRolePositionsAsync(guildId, positions);
        }

        // Invite lookup/deletion operations ────────────────────────────────────────────────

        /// <summary>Gets an invite.</summary>
        public async Task<Invite?> GetInviteAsync(string inviteCode, bool? withCounts = null, bool? withExpiration = null, ulong? guildScheduledEventId = null)
        {
            return await _restClient.GetInviteAsync(inviteCode, withCounts, withExpiration, guildScheduledEventId);
        }

        /// <summary>Deletes an invite.</summary>
        public async Task<Invite?> DeleteInviteAsync(string inviteCode, string? reason = null)
        {
            return await _restClient.DeleteInviteAsync(inviteCode, reason);
        }

        // Bulk ban operation ───────────────────────────────────────────────────────────────

        /// <summary>Bulk bans users from a guild.</summary>
        public async Task<BulkGuildBanResponse?> BulkGuildBanAsync(ulong guildId, BulkGuildBanRequest request, string? reason = null)
        {
            return await _restClient.BulkGuildBanAsync(guildId, request, reason);
        }

        // Guild role extras operations ───────────────────────────────────────────────────────

        /// <summary>Gets a guild role.</summary>
        public async Task<Role?> GetGuildRoleAsync(ulong guildId, ulong roleId)
        {
            return await _restClient.GetGuildRoleAsync(guildId, roleId);
        }

        /// <summary>Gets guild role member counts.</summary>
        public async Task<Dictionary<string, int>?> GetGuildRoleMemberCountsAsync(ulong guildId)
        {
            return await _restClient.GetGuildRoleMemberCountsAsync(guildId);
        }

        // Guild incident actions operation ───────────────────────────────────────────────────

        /// <summary>Modifies guild incident actions.</summary>
        public async Task<GuildIncidentActionsResponse?> ModifyGuildIncidentActionsAsync(ulong guildId, ModifyGuildIncidentActionsRequest request)
        {
            return await _restClient.ModifyGuildIncidentActionsAsync(guildId, request);
        }

        // Current user guild member operation ─────────────────────────────────────────────────

        /// <summary>Gets current user guild member.</summary>
        public async Task<GuildMember?> GetCurrentUserGuildMemberAsync(ulong guildId)
        {
            return await _restClient.GetCurrentUserGuildMemberAsync(guildId);
        }

        // Voice state modification operations ────────────────────────────────────────────────

        /// <summary>Modifies current user voice state.</summary>
        public async Task<bool> ModifyCurrentUserVoiceStateAsync(ulong guildId, ModifyCurrentUserVoiceStateRequest request)
        {
            return await _restClient.ModifyCurrentUserVoiceStateAsync(guildId, request);
        }

        /// <summary>Modifies user voice state.</summary>
        public async Task<bool> ModifyUserVoiceStateAsync(ulong guildId, ulong userId, ModifyUserVoiceStateRequest request)
        {
            return await _restClient.ModifyUserVoiceStateAsync(guildId, userId, request);
        }

        // Activity Instance operation ────────────────────────────────────────────────────────

        /// <summary>Gets activity instance.</summary>
        public async Task<ActivityInstance?> GetActivityInstanceAsync(ulong applicationId, string instanceId)
        {
            return await _restClient.GetActivityInstanceAsync(applicationId, instanceId);
        }

        // Gateway operations ────────────────────────────────────────────────────────────────

        /// <summary>Gets gateway.</summary>
        public async Task<GatewayInfo?> GetGatewayAsync()
        {
            return await _restClient.GetGatewayAsync();
        }

        /// <summary>Gets gateway bot.</summary>
        public async Task<GatewayBotInfo?> GetGatewayBotAsync()
        {
            return await _restClient.GetGatewayBotAsync();
        }

        // Current user connections operation ────────────────────────────────────────────────────

        /// <summary>Gets current user connections.</summary>
        public async Task<List<UserConnection>?> GetCurrentUserConnectionsAsync()
        {
            return await _restClient.GetCurrentUserConnectionsAsync();
        }

        // Guild member search operation ────────────────────────────────────────────────────────

        /// <summary>Searches guild members.</summary>
        public async Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int limit = 25)
        {
            return await _restClient.SearchGuildMembersAsync(guildId, query, limit);
        }

        // Modify current member operation ───────────────────────────────────────────────────────

        /// <summary>Modifies current guild member.</summary>
        public async Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick)
        {
            return await _restClient.ModifyCurrentMemberAsync(guildId, nick);
        }

        // Additional operations ──────────────────────────────────────────────────────

        /// <summary>Gets guild preview.</summary>
        public async Task<GuildPreview?> GetGuildPreviewAsync(ulong guildId)
        {
            return await _restClient.GetGuildPreviewAsync(guildId);
        }

        /// <summary>Follows an announcement channel.</summary>
        public async Task<FollowedChannel?> FollowAnnouncementChannelAsync(ulong channelId, ulong webhookChannelId)
        {
            return await _restClient.FollowAnnouncementChannelAsync(channelId, webhookChannelId);
        }

        /// <summary>Exchanges OAuth2 code for token.</summary>
        public async Task<OAuth2TokenResponse?> ExchangeCodeAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            return await _restClient.ExchangeCodeAsync(code, clientId, clientSecret, redirectUri);
        }

        /// <summary>Refreshes OAuth2 token.</summary>
        public async Task<OAuth2TokenResponse?> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret)
        {
            return await _restClient.RefreshTokenAsync(refreshToken, clientId, clientSecret);
        }

        /// <summary>Revokes OAuth2 token.</summary>
        public async Task<bool> RevokeTokenAsync(string token, string clientId, string clientSecret, string? tokenTypeHint = null)
        {
            return await _restClient.RevokeTokenAsync(token, clientId, clientSecret, tokenTypeHint);
        }

        // ── Convenience event subscriptions ───────────────────────────────────────

        // Messages ──────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the READY gateway event.</summary>
        [EventInterest("READY")]
        public IDisposable OnReady(Func<ReadyEvent, Task> handler)
            => _gatewayClient.Events.On<ReadyEvent>("READY", handler);

        /// <summary>Subscribes to the MESSAGE_CREATE gateway event.</summary>
        /// <example>
        /// <code>
        /// using var subscription = client.OnMessageCreated(async msg =>
        /// {
        ///     if (msg.Author?.Bot == true) return;
        ///     Console.WriteLine($"[{msg.ChannelId}] {msg.Author?.Username}: {msg.Content}");
        /// });
        /// </code>
        /// </example>
        [EventInterest("MESSAGE_CREATE")]
        public IDisposable OnMessageCreated(Func<MessageCreateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageCreateEvent>("MESSAGE_CREATE", handler);

        /// <summary>Subscribes to the MESSAGE_UPDATE gateway event.</summary>
        [EventInterest("MESSAGE_UPDATE")]
        public IDisposable OnMessageUpdated(Func<MessageUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", handler);

        /// <summary>Subscribes to the MESSAGE_DELETE gateway event.</summary>
        [EventInterest("MESSAGE_DELETE")]
        public IDisposable OnMessageDeleted(Func<MessageDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<MessageDeleteEvent>("MESSAGE_DELETE", handler);

        /// <summary>Subscribes to the MESSAGE_DELETE_BULK gateway event.</summary>
        [EventInterest("MESSAGE_BULK_DELETE")]
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
        [EventInterest("GUILD_CREATE")]
        public IDisposable OnGuildAvailable(Func<GuildCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildCreateEvent>("GUILD_CREATE", handler);

        /// <summary>Subscribes to the GUILD_UPDATE gateway event.</summary>
        [EventInterest("GUILD_UPDATE")]
        public IDisposable OnGuildUpdated(Func<GuildUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildUpdateEvent>("GUILD_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_DELETE gateway event.</summary>
        [EventInterest("GUILD_DELETE")]
        public IDisposable OnGuildUnavailable(Func<GuildDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<GuildDeleteEvent>("GUILD_DELETE", handler);

        // Members ───────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_MEMBER_ADD gateway event.</summary>
        [EventInterest("GUILD_MEMBER_ADD")]
        public IDisposable OnGuildMemberJoined(Func<GuildMemberAddEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", handler);

        /// <summary>Subscribes to the GUILD_MEMBER_UPDATE gateway event.</summary>
        [EventInterest("GUILD_MEMBER_UPDATE")]
        public IDisposable OnGuildMemberUpdated(Func<GuildMemberUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildMemberUpdateEvent>("GUILD_MEMBER_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_MEMBER_REMOVE gateway event.</summary>
        [EventInterest("GUILD_MEMBER_REMOVE")]
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

        // Guild integrations ─────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_INTEGRATIONS_UPDATE gateway event.</summary>
        public IDisposable OnGuildIntegrationsUpdated(Func<GuildIntegrationsUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildIntegrationsUpdateEvent>("GUILD_INTEGRATIONS_UPDATE", handler);

        // User ───────────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the USER_UPDATE gateway event.</summary>
        public IDisposable OnUserUpdated(Func<UserUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<UserUpdateEvent>("USER_UPDATE", handler);

        // Soundboard ─────────────────────────────────────────────────────────────

        /// <summary>Subscribes to the GUILD_SOUNDBOARD_SOUND_CREATE gateway event.</summary>
        public IDisposable OnSoundboardSoundCreated(Func<GuildSoundboardSoundCreateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildSoundboardSoundCreateEvent>("GUILD_SOUNDBOARD_SOUND_CREATE", handler);

        /// <summary>Subscribes to the GUILD_SOUNDBOARD_SOUND_UPDATE gateway event.</summary>
        public IDisposable OnSoundboardSoundUpdated(Func<GuildSoundboardSoundUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildSoundboardSoundUpdateEvent>("GUILD_SOUNDBOARD_SOUND_UPDATE", handler);

        /// <summary>Subscribes to the GUILD_SOUNDBOARD_SOUND_DELETE gateway event.</summary>
        public IDisposable OnSoundboardSoundDeleted(Func<GuildSoundboardSoundDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<GuildSoundboardSoundDeleteEvent>("GUILD_SOUNDBOARD_SOUND_DELETE", handler);

        /// <summary>Subscribes to the GUILD_SOUNDBOARD_SOUNDS_UPDATE gateway event.</summary>
        public IDisposable OnSoundboardSoundsUpdated(Func<GuildSoundboardSoundsUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<GuildSoundboardSoundsUpdateEvent>("GUILD_SOUNDBOARD_SOUNDS_UPDATE", handler);

        // Subscriptions ───────────────────────────────────────────────────────────

        /// <summary>Subscribes to the SUBSCRIPTION_CREATE gateway event.</summary>
        public IDisposable OnSubscriptionCreated(Func<SubscriptionCreateEvent, Task> handler)
            => _gatewayClient.Events.On<SubscriptionCreateEvent>("SUBSCRIPTION_CREATE", handler);

        /// <summary>Subscribes to the SUBSCRIPTION_UPDATE gateway event.</summary>
        public IDisposable OnSubscriptionUpdated(Func<SubscriptionUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<SubscriptionUpdateEvent>("SUBSCRIPTION_UPDATE", handler);

        /// <summary>Subscribes to the SUBSCRIPTION_DELETE gateway event.</summary>
        public IDisposable OnSubscriptionDeleted(Func<SubscriptionDeleteEvent, Task> handler)
            => _gatewayClient.Events.On<SubscriptionDeleteEvent>("SUBSCRIPTION_DELETE", handler);

        // Voice channel effects ─────────────────────────────────────────────────

        /// <summary>Subscribes to the VOICE_CHANNEL_EFFECT_SEND gateway event.</summary>
        public IDisposable OnVoiceChannelEffectSent(Func<VoiceChannelEffectSendEvent, Task> handler)
            => _gatewayClient.Events.On<VoiceChannelEffectSendEvent>("VOICE_CHANNEL_EFFECT_SEND", handler);

        /// <summary>Subscribes to the VOICE_CHANNEL_STATUS_UPDATE gateway event.</summary>
        public IDisposable OnVoiceChannelStatusUpdated(Func<VoiceChannelStatusUpdateEvent, Task> handler)
            => _gatewayClient.Events.On<VoiceChannelStatusUpdateEvent>("VOICE_CHANNEL_STATUS_UPDATE", handler);

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
