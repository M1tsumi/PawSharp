import re

filepath = r"c:\Users\pawso\OneDrive\Desktop\Github\API\PawSharp\src\PawSharp.Gateway\GatewayClient.cs"

with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Add GatewayCloseCode enum before GatewayClient class
enum_block = '''namespace PawSharp.Gateway
{
    /// <summary>
    /// Discord Gateway close event codes.
    /// See https://discord.com/developers/docs/topics/opcodes-and-status-codes#gateway-close-event-codes
    /// </summary>
    public enum GatewayCloseCode
    {
        UnknownOpcode = 4001,
        DecodeError = 4002,
        NotAuthenticated = 4003,
        AuthenticationFailed = 4004,
        AlreadyAuthenticated = 4005,
        InvalidSequence = 4007,
        RateLimited = 4008,
        SessionTimedOut = 4009,
        InvalidShard = 4010,
        ShardingRequired = 4011,
        InvalidApiVersion = 4012,
        InvalidIntent = 4013,
        DisallowedIntent = 4014,
        VoiceServerCrashed = 4015
    }

    public class GatewayClient : IGatewayClient'''

content = content.replace(
    'namespace PawSharp.Gateway\n{\n    public class GatewayClient : IGatewayClient',
    enum_block
)

# 2. Replace close code magic numbers with enum
content = content.replace(
    '''                            switch (closeCode)
                            {
                                case 4001: // Unknown opcode
                                case 4002: // Decode error
                                case 4005: // Already authenticated
                                    _logger.LogError("Gateway protocol error ({CloseCode}) - re-identifying", closeCode);
                                    _resumeSessionId = null;
                                    _resumeSequence = null;
                                    break;
                                    
                                case 4003: // Not authenticated
                                case 4004: // Authentication failed
                                    _logger.LogError("Gateway authentication failed ({CloseCode}) - check token", closeCode);
                                    await SetStateAsync(GatewayState.Failed);
                                    return; // Don't reconnect on auth failure
                                    
                                case 4007: // Invalid seq
                                case 4009: // Session timed out
                                    _logger.LogWarning("Gateway session invalid ({CloseCode}) - starting fresh", closeCode);
                                    _resumeSessionId = null;
                                    _resumeSequence = null;
                                    break;
                                    
                                case 4008: // Rate limited
                                    _logger.LogWarning("Gateway rate limited - waiting before reconnect");
                                    await Task.Delay(5000);
                                    break;
                                    
                                case 4010: // Invalid shard
                                case 4011: // Sharding required
                                    _logger.LogError("Gateway sharding error ({CloseCode}) - check shard configuration", closeCode);
                                    await SetStateAsync(GatewayState.Failed);
                                    return;
                                    
                                case 4012: // Invalid API version
                                    _logger.LogError("Invalid API version - update client");
                                    await SetStateAsync(GatewayState.Failed);
                                    return;
                                    
                                case 4013: // Invalid intent(s)
                                case 4014: // Disallowed intent(s)
                                    _logger.LogError("Gateway intent error ({CloseCode}) - check intent configuration", closeCode);
                                    await SetStateAsync(GatewayState.Failed);
                                    return;

                                case 4015: // Voice server crashed
                                    _logger.LogWarning("Voice server crashed ({CloseCode}) - reconnecting", closeCode);
                                    break;''',
    '''                            switch ((GatewayCloseCode)closeCode)
                            {
                                case GatewayCloseCode.UnknownOpcode:
                                case GatewayCloseCode.DecodeError:
                                case GatewayCloseCode.AlreadyAuthenticated:
                                    _logger.LogError("Gateway protocol error ({CloseCode}) - re-identifying", closeCode);
                                    _resumeSessionId = null;
                                    _resumeSequence = null;
                                    break;

                                case GatewayCloseCode.NotAuthenticated:
                                case GatewayCloseCode.AuthenticationFailed:
                                    _logger.LogError("Gateway authentication failed ({CloseCode}) - check token", closeCode);
                                    await SetStateAsync(GatewayState.Failed).ConfigureAwait(false);
                                    return; // Don't reconnect on auth failure

                                case GatewayCloseCode.InvalidSequence:
                                case GatewayCloseCode.SessionTimedOut:
                                    _logger.LogWarning("Gateway session invalid ({CloseCode}) - starting fresh", closeCode);
                                    _resumeSessionId = null;
                                    _resumeSequence = null;
                                    break;

                                case GatewayCloseCode.RateLimited:
                                    _logger.LogWarning("Gateway rate limited - waiting before reconnect");
                                    await Task.Delay(5000).ConfigureAwait(false);
                                    break;

                                case GatewayCloseCode.InvalidShard:
                                case GatewayCloseCode.ShardingRequired:
                                    _logger.LogError("Gateway sharding error ({CloseCode}) - check shard configuration", closeCode);
                                    await SetStateAsync(GatewayState.Failed).ConfigureAwait(false);
                                    return;

                                case GatewayCloseCode.InvalidApiVersion:
                                    _logger.LogError("Invalid API version - update client");
                                    await SetStateAsync(GatewayState.Failed).ConfigureAwait(false);
                                    return;

                                case GatewayCloseCode.InvalidIntent:
                                case GatewayCloseCode.DisallowedIntent:
                                    _logger.LogError("Gateway intent error ({CloseCode}) - check intent configuration", closeCode);
                                    await SetStateAsync(GatewayState.Failed).ConfigureAwait(false);
                                    return;

                                case GatewayCloseCode.VoiceServerCrashed:
                                    _logger.LogWarning("Voice server crashed ({CloseCode}) - reconnecting", closeCode);
                                    break;'''
)

# 3. Pass logger to WebSocketConnection
content = content.replace(
    '''            _webSocket = new WebSocketConnection(
                options.EnableCompression, 
                options.EventDispatch.EnableArrayPooling,
                options.WebSocketBufferSizeKb);''',
    '''            _webSocket = new WebSocketConnection(
                options.EnableCompression,
                options.EventDispatch.EnableArrayPooling,
                options.WebSocketBufferSizeKb,
                logger != null ? new Logger<WebSocketConnection>(logger) : null);'''
)

# 4. Add ConfigureAwait(false) to all await calls in library methods
# This is a broad regex that adds .ConfigureAwait(false) to await expressions
# We need to be careful not to break the code.

# Pattern: await <expression>(<args>);  (not already having ConfigureAwait)
def add_configure_await(match):
    expr = match.group(1)
    # Skip if already has ConfigureAwait
    if '.ConfigureAwait(' in expr:
        return match.group(0)
    # Skip await Task.CompletedTask and similar simple cases
    if expr.strip() in ('Task.CompletedTask', 'Task.Delay(5000)'):
        return f'await {expr}.ConfigureAwait(false)'
    return f'await {expr}.ConfigureAwait(false)'

# Match "await something();" where something doesn't already have ConfigureAwait
# Use a simpler approach: match specific patterns

# Add ConfigureAwait to common await patterns
replacements = [
    ('await _restClient.GetGatewayAsync();', 'await _restClient.GetGatewayAsync().ConfigureAwait(false);'),
    ('await _webSocket.ConnectAsync(uri, _cts.Token);', 'await _webSocket.ConnectAsync(uri, _cts.Token).ConfigureAwait(false);'),
    ('await SetStateAsync(GatewayState.Connected);', 'await SetStateAsync(GatewayState.Connected).ConfigureAwait(false);'),
    ('await SendResumeAsync();', 'await SendResumeAsync().ConfigureAwait(false);'),
    ('await SendIdentifyAsync();', 'await SendIdentifyAsync().ConfigureAwait(false);'),
    ('await SetStateAsync(GatewayState.Disconnected);', 'await SetStateAsync(GatewayState.Disconnected).ConfigureAwait(false);'),
    ('await _heartbeatManager.StopAsync();', 'await _heartbeatManager.StopAsync().ConfigureAwait(false);'),
    ('await _webSocket.DisconnectAsync(_cts?.Token ?? CancellationToken.None);', 'await _webSocket.DisconnectAsync(_cts?.Token ?? CancellationToken.None).ConfigureAwait(false);'),
    ('await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None);', 'await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);'),
    ('await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None, isHeartbeat: true);', 'await GatewaySendAsync(json, _cts?.Token ?? CancellationToken.None, isHeartbeat: true).ConfigureAwait(false);'),
    ('await _reconnectionManager.ReconnectAsync()', 'await _reconnectionManager.ReconnectAsync().ConfigureAwait(false)'),
    ('await ConnectAsync();', 'await ConnectAsync().ConfigureAwait(false);'),
    ('await DisconnectAsync();', 'await DisconnectAsync().ConfigureAwait(false);'),
    ('await handler(oldState, newState);', 'await handler(oldState, newState).ConfigureAwait(false);'),
    ('await _heartbeatTask.WaitAsync(effectiveTimeout);', 'await _heartbeatTask.WaitAsync(effectiveTimeout).ConfigureAwait(false);'),
    ('await _heartbeatManager.ReceiveAckAsync();', 'await _heartbeatManager.ReceiveAckAsync().ConfigureAwait(false);'),
    ('await ReconnectAsync();', 'await ReconnectAsync().ConfigureAwait(false);'),
    ('await HandleMessageAsync(message);', 'await HandleMessageAsync(message).ConfigureAwait(false);'),
    ('await HandleDispatchEventAsync(t, d.GetRawText());', 'await HandleDispatchEventAsync(t, d.GetRawText()).ConfigureAwait(false);'),
    ('await SendHeartbeatAsync();', 'await SendHeartbeatAsync().ConfigureAwait(false);'),
    ('await HandleHelloAsync(d);', 'await HandleHelloAsync(d).ConfigureAwait(false);'),
    ('await HandleReadyEventAsync(eventData);', 'await HandleReadyEventAsync(eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ReadyEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ReadyEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ResumedEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ResumedEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildAvailableEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildAvailableEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildUnavailableEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildUnavailableEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildEmojisUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildEmojisUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ChannelCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ChannelCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ChannelUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ChannelUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ChannelDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ChannelDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildMemberAddEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildMemberAddEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildMemberUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildMemberUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildMemberRemoveEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildMemberRemoveEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<InteractionCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<InteractionCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<TypingStartEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<TypingStartEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageReactionAddEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageReactionAddEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveAllEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveAllEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<PresenceUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<PresenceUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ChannelPinsUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ChannelPinsUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildBanAddEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildBanAddEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildBanRemoveEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildBanRemoveEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildRoleCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildRoleCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildRoleUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildRoleUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildRoleDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildRoleDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildMembersChunkEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildMembersChunkEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildStickersUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildStickersUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEmojiEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<MessageReactionRemoveEmojiEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildIntegrationsUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildIntegrationsUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<UserUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<UserUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<VoiceStateUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<VoiceStateUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await VoiceStateUpdate.Invoke(voiceStateEvent);', 'await VoiceStateUpdate.Invoke(voiceStateEvent).ConfigureAwait(false);'),
    ('await SetStateAsync(GatewayState.Ready);', 'await SetStateAsync(GatewayState.Ready).ConfigureAwait(false);'),
    ('await _webSocket.ReceiveAsync(cancellationToken);', 'await _webSocket.ReceiveAsync(cancellationToken).ConfigureAwait(false);'),
    ('await Task.Delay(TimeSpan.FromSeconds(resumable ? 1 : 5));', 'await Task.Delay(TimeSpan.FromSeconds(resumable ? 1 : 5)).ConfigureAwait(false);'),
    ('await _wsRateLimiter.WaitAsync(ct);', 'await _wsRateLimiter.WaitAsync(ct).ConfigureAwait(false);'),
    ('await _webSocket.SendAsync(json, ct);', 'await _webSocket.SendAsync(json, ct).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<VoiceServerUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<VoiceServerUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<InviteCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<InviteCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<InviteDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<InviteDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadListSyncEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadListSyncEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadMemberUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadMemberUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<ThreadMembersUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<ThreadMembersUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserAddEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserAddEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserRemoveEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildScheduledEventUserRemoveEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<AutoModerationRuleDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<AutoModerationActionExecutionEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<AutoModerationActionExecutionEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<VoiceChannelEffectSendEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<VoiceChannelEffectSendEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<EntitlementCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<EntitlementCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<EntitlementUpdateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<EntitlementUpdateEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<EntitlementDeleteEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<EntitlementDeleteEvent>(eventType, eventData).ConfigureAwait(false);'),
    ('await _eventDispatcher.DispatchFromJsonAsync<GuildAuditLogEntryCreateEvent>(eventType, eventData);', 'await _eventDispatcher.DispatchFromJsonAsync<GuildAuditLogEntryCreateEvent>(eventType, eventData).ConfigureAwait(false);'),
]

for old, new in replacements:
    content = content.replace(old, new)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Done")
