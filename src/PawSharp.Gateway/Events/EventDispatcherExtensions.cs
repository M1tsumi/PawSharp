#nullable enable
using System;
using System.Threading.Tasks;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Strongly-typed extension methods for EventDispatcher to eliminate string literals in event subscription.
/// Provides compile-time safety and IntelliSense for all Discord gateway events.
/// </summary>
public static class EventDispatcherExtensions
{
    // Core Events

    /// <summary>
    /// Subscribes to READY events.
    /// </summary>
    public static IDisposable OnReady(this EventDispatcher dispatcher, Func<ReadyEvent, Task> handler)
        => dispatcher.On("READY", handler);

    /// <summary>
    /// Subscribes to READY events (synchronous).
    /// </summary>
    public static IDisposable OnReady(this EventDispatcher dispatcher, Action<ReadyEvent> handler)
        => dispatcher.On("READY", handler);

    /// <summary>
    /// Subscribes to RESUMED events.
    /// </summary>
    public static IDisposable OnResumed(this EventDispatcher dispatcher, Func<ResumedEvent, Task> handler)
        => dispatcher.On("RESUMED", handler);

    /// <summary>
    /// Subscribes to RESUMED events (synchronous).
    /// </summary>
    public static IDisposable OnResumed(this EventDispatcher dispatcher, Action<ResumedEvent> handler)
        => dispatcher.On("RESUMED", handler);

    // Message Events

    /// <summary>
    /// Subscribes to MESSAGE_CREATE events.
    /// </summary>
    public static IDisposable OnMessageCreate(this EventDispatcher dispatcher, Func<MessageCreateEvent, Task> handler)
        => dispatcher.On("MESSAGE_CREATE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnMessageCreate(this EventDispatcher dispatcher, Action<MessageCreateEvent> handler)
        => dispatcher.On("MESSAGE_CREATE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_UPDATE events.
    /// </summary>
    public static IDisposable OnMessageUpdate(this EventDispatcher dispatcher, Func<MessageUpdateEvent, Task> handler)
        => dispatcher.On("MESSAGE_UPDATE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnMessageUpdate(this EventDispatcher dispatcher, Action<MessageUpdateEvent> handler)
        => dispatcher.On("MESSAGE_UPDATE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_DELETE events.
    /// </summary>
    public static IDisposable OnMessageDelete(this EventDispatcher dispatcher, Func<MessageDeleteEvent, Task> handler)
        => dispatcher.On("MESSAGE_DELETE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnMessageDelete(this EventDispatcher dispatcher, Action<MessageDeleteEvent> handler)
        => dispatcher.On("MESSAGE_DELETE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_DELETE_BULK events.
    /// </summary>
    public static IDisposable OnMessageDeleteBulk(this EventDispatcher dispatcher, Func<MessageDeleteBulkEvent, Task> handler)
        => dispatcher.On("MESSAGE_DELETE_BULK", handler);

    /// <summary>
    /// Subscribes to MESSAGE_DELETE_BULK events (synchronous).
    /// </summary>
    public static IDisposable OnMessageDeleteBulk(this EventDispatcher dispatcher, Action<MessageDeleteBulkEvent> handler)
        => dispatcher.On("MESSAGE_DELETE_BULK", handler);

    // Reaction Events

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_ADD events.
    /// </summary>
    public static IDisposable OnMessageReactionAdd(this EventDispatcher dispatcher, Func<MessageReactionAddEvent, Task> handler)
        => dispatcher.On("MESSAGE_REACTION_ADD", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_ADD events (synchronous).
    /// </summary>
    public static IDisposable OnMessageReactionAdd(this EventDispatcher dispatcher, Action<MessageReactionAddEvent> handler)
        => dispatcher.On("MESSAGE_REACTION_ADD", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE events.
    /// </summary>
    public static IDisposable OnMessageReactionRemove(this EventDispatcher dispatcher, Func<MessageReactionRemoveEvent, Task> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE events (synchronous).
    /// </summary>
    public static IDisposable OnMessageReactionRemove(this EventDispatcher dispatcher, Action<MessageReactionRemoveEvent> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE_ALL events.
    /// </summary>
    public static IDisposable OnMessageReactionRemoveAll(this EventDispatcher dispatcher, Func<MessageReactionRemoveAllEvent, Task> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE_ALL", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE_ALL events (synchronous).
    /// </summary>
    public static IDisposable OnMessageReactionRemoveAll(this EventDispatcher dispatcher, Action<MessageReactionRemoveAllEvent> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE_ALL", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE_EMOJI events.
    /// </summary>
    public static IDisposable OnMessageReactionRemoveEmoji(this EventDispatcher dispatcher, Func<MessageReactionRemoveEmojiEvent, Task> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE_EMOJI", handler);

    /// <summary>
    /// Subscribes to MESSAGE_REACTION_REMOVE_EMOJI events (synchronous).
    /// </summary>
    public static IDisposable OnMessageReactionRemoveEmoji(this EventDispatcher dispatcher, Action<MessageReactionRemoveEmojiEvent> handler)
        => dispatcher.On("MESSAGE_REACTION_REMOVE_EMOJI", handler);

    // Poll Events

    /// <summary>
    /// Subscribes to MESSAGE_POLL_VOTE_ADD events.
    /// </summary>
    public static IDisposable OnMessagePollVoteAdd(this EventDispatcher dispatcher, Func<MessagePollVoteAddEvent, Task> handler)
        => dispatcher.On("MESSAGE_POLL_VOTE_ADD", handler);

    /// <summary>
    /// Subscribes to MESSAGE_POLL_VOTE_ADD events (synchronous).
    /// </summary>
    public static IDisposable OnMessagePollVoteAdd(this EventDispatcher dispatcher, Action<MessagePollVoteAddEvent> handler)
        => dispatcher.On("MESSAGE_POLL_VOTE_ADD", handler);

    /// <summary>
    /// Subscribes to MESSAGE_POLL_VOTE_REMOVE events.
    /// </summary>
    public static IDisposable OnMessagePollVoteRemove(this EventDispatcher dispatcher, Func<MessagePollVoteRemoveEvent, Task> handler)
        => dispatcher.On("MESSAGE_POLL_VOTE_REMOVE", handler);

    /// <summary>
    /// Subscribes to MESSAGE_POLL_VOTE_REMOVE events (synchronous).
    /// </summary>
    public static IDisposable OnMessagePollVoteRemove(this EventDispatcher dispatcher, Action<MessagePollVoteRemoveEvent> handler)
        => dispatcher.On("MESSAGE_POLL_VOTE_REMOVE", handler);

    // Guild Events

    /// <summary>
    /// Subscribes to GUILD_CREATE events.
    /// </summary>
    public static IDisposable OnGuildCreate(this EventDispatcher dispatcher, Func<GuildCreateEvent, Task> handler)
        => dispatcher.On("GUILD_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildCreate(this EventDispatcher dispatcher, Action<GuildCreateEvent> handler)
        => dispatcher.On("GUILD_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildUpdate(this EventDispatcher dispatcher, Func<GuildUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildUpdate(this EventDispatcher dispatcher, Action<GuildUpdateEvent> handler)
        => dispatcher.On("GUILD_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_DELETE events.
    /// </summary>
    public static IDisposable OnGuildDelete(this EventDispatcher dispatcher, Func<GuildDeleteEvent, Task> handler)
        => dispatcher.On("GUILD_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildDelete(this EventDispatcher dispatcher, Action<GuildDeleteEvent> handler)
        => dispatcher.On("GUILD_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_AVAILABLE events.
    /// </summary>
    public static IDisposable OnGuildAvailable(this EventDispatcher dispatcher, Func<GuildAvailableEvent, Task> handler)
        => dispatcher.On("GUILD_AVAILABLE", handler);

    /// <summary>
    /// Subscribes to GUILD_AVAILABLE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildAvailable(this EventDispatcher dispatcher, Action<GuildAvailableEvent> handler)
        => dispatcher.On("GUILD_AVAILABLE", handler);

    /// <summary>
    /// Subscribes to GUILD_UNAVAILABLE events.
    /// </summary>
    public static IDisposable OnGuildUnavailable(this EventDispatcher dispatcher, Func<GuildUnavailableEvent, Task> handler)
        => dispatcher.On("GUILD_UNAVAILABLE", handler);

    /// <summary>
    /// Subscribes to GUILD_UNAVAILABLE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildUnavailable(this EventDispatcher dispatcher, Action<GuildUnavailableEvent> handler)
        => dispatcher.On("GUILD_UNAVAILABLE", handler);

    /// <summary>
    /// Subscribes to GUILD_EMOJIS_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildEmojisUpdate(this EventDispatcher dispatcher, Func<GuildEmojisUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_EMOJIS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_EMOJIS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildEmojisUpdate(this EventDispatcher dispatcher, Action<GuildEmojisUpdateEvent> handler)
        => dispatcher.On("GUILD_EMOJIS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_STICKERS_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildStickersUpdate(this EventDispatcher dispatcher, Func<GuildStickersUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_STICKERS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_STICKERS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildStickersUpdate(this EventDispatcher dispatcher, Action<GuildStickersUpdateEvent> handler)
        => dispatcher.On("GUILD_STICKERS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_BAN_ADD events.
    /// </summary>
    public static IDisposable OnGuildBanAdd(this EventDispatcher dispatcher, Func<GuildBanAddEvent, Task> handler)
        => dispatcher.On("GUILD_BAN_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_BAN_ADD events (synchronous).
    /// </summary>
    public static IDisposable OnGuildBanAdd(this EventDispatcher dispatcher, Action<GuildBanAddEvent> handler)
        => dispatcher.On("GUILD_BAN_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_BAN_REMOVE events.
    /// </summary>
    public static IDisposable OnGuildBanRemove(this EventDispatcher dispatcher, Func<GuildBanRemoveEvent, Task> handler)
        => dispatcher.On("GUILD_BAN_REMOVE", handler);

    /// <summary>
    /// Subscribes to GUILD_BAN_REMOVE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildBanRemove(this EventDispatcher dispatcher, Action<GuildBanRemoveEvent> handler)
        => dispatcher.On("GUILD_BAN_REMOVE", handler);

    /// <summary>
    /// Subscribes to GUILD_INTEGRATIONS_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildIntegrationsUpdate(this EventDispatcher dispatcher, Func<GuildIntegrationsUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_INTEGRATIONS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_INTEGRATIONS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildIntegrationsUpdate(this EventDispatcher dispatcher, Action<GuildIntegrationsUpdateEvent> handler)
        => dispatcher.On("GUILD_INTEGRATIONS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_AUDIT_LOG_ENTRY_CREATE events.
    /// </summary>
    public static IDisposable OnGuildAuditLogEntryCreate(this EventDispatcher dispatcher, Func<GuildAuditLogEntryCreateEvent, Task> handler)
        => dispatcher.On("GUILD_AUDIT_LOG_ENTRY_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_AUDIT_LOG_ENTRY_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildAuditLogEntryCreate(this EventDispatcher dispatcher, Action<GuildAuditLogEntryCreateEvent> handler)
        => dispatcher.On("GUILD_AUDIT_LOG_ENTRY_CREATE", handler);

    // Guild Member Events

    /// <summary>
    /// Subscribes to GUILD_MEMBER_ADD events.
    /// </summary>
    public static IDisposable OnGuildMemberAdd(this EventDispatcher dispatcher, Func<GuildMemberAddEvent, Task> handler)
        => dispatcher.On("GUILD_MEMBER_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBER_ADD events (synchronous).
    /// </summary>
    public static IDisposable OnGuildMemberAdd(this EventDispatcher dispatcher, Action<GuildMemberAddEvent> handler)
        => dispatcher.On("GUILD_MEMBER_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBER_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildMemberUpdate(this EventDispatcher dispatcher, Func<GuildMemberUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_MEMBER_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBER_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildMemberUpdate(this EventDispatcher dispatcher, Action<GuildMemberUpdateEvent> handler)
        => dispatcher.On("GUILD_MEMBER_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBER_REMOVE events.
    /// </summary>
    public static IDisposable OnGuildMemberRemove(this EventDispatcher dispatcher, Func<GuildMemberRemoveEvent, Task> handler)
        => dispatcher.On("GUILD_MEMBER_REMOVE", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBER_REMOVE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildMemberRemove(this EventDispatcher dispatcher, Action<GuildMemberRemoveEvent> handler)
        => dispatcher.On("GUILD_MEMBER_REMOVE", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBERS_CHUNK events.
    /// </summary>
    public static IDisposable OnGuildMembersChunk(this EventDispatcher dispatcher, Func<GuildMembersChunkEvent, Task> handler)
        => dispatcher.On("GUILD_MEMBERS_CHUNK", handler);

    /// <summary>
    /// Subscribes to GUILD_MEMBERS_CHUNK events (synchronous).
    /// </summary>
    public static IDisposable OnGuildMembersChunk(this EventDispatcher dispatcher, Action<GuildMembersChunkEvent> handler)
        => dispatcher.On("GUILD_MEMBERS_CHUNK", handler);

    // Guild Role Events

    /// <summary>
    /// Subscribes to GUILD_ROLE_CREATE events.
    /// </summary>
    public static IDisposable OnGuildRoleCreate(this EventDispatcher dispatcher, Func<GuildRoleCreateEvent, Task> handler)
        => dispatcher.On("GUILD_ROLE_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_ROLE_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildRoleCreate(this EventDispatcher dispatcher, Action<GuildRoleCreateEvent> handler)
        => dispatcher.On("GUILD_ROLE_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_ROLE_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildRoleUpdate(this EventDispatcher dispatcher, Func<GuildRoleUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_ROLE_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_ROLE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildRoleUpdate(this EventDispatcher dispatcher, Action<GuildRoleUpdateEvent> handler)
        => dispatcher.On("GUILD_ROLE_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_ROLE_DELETE events.
    /// </summary>
    public static IDisposable OnGuildRoleDelete(this EventDispatcher dispatcher, Func<GuildRoleDeleteEvent, Task> handler)
        => dispatcher.On("GUILD_ROLE_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_ROLE_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildRoleDelete(this EventDispatcher dispatcher, Action<GuildRoleDeleteEvent> handler)
        => dispatcher.On("GUILD_ROLE_DELETE", handler);

    // Guild Scheduled Events

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_CREATE events.
    /// </summary>
    public static IDisposable OnGuildScheduledEventCreate(this EventDispatcher dispatcher, Func<GuildScheduledEventCreateEvent, Task> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildScheduledEventCreate(this EventDispatcher dispatcher, Action<GuildScheduledEventCreateEvent> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildScheduledEventUpdate(this EventDispatcher dispatcher, Func<GuildScheduledEventUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildScheduledEventUpdate(this EventDispatcher dispatcher, Action<GuildScheduledEventUpdateEvent> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_DELETE events.
    /// </summary>
    public static IDisposable OnGuildScheduledEventDelete(this EventDispatcher dispatcher, Func<GuildScheduledEventDeleteEvent, Task> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildScheduledEventDelete(this EventDispatcher dispatcher, Action<GuildScheduledEventDeleteEvent> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_USER_ADD events.
    /// </summary>
    public static IDisposable OnGuildScheduledEventUserAdd(this EventDispatcher dispatcher, Func<GuildScheduledEventUserAddEvent, Task> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_USER_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_USER_ADD events (synchronous).
    /// </summary>
    public static IDisposable OnGuildScheduledEventUserAdd(this EventDispatcher dispatcher, Action<GuildScheduledEventUserAddEvent> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_USER_ADD", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_USER_REMOVE events.
    /// </summary>
    public static IDisposable OnGuildScheduledEventUserRemove(this EventDispatcher dispatcher, Func<GuildScheduledEventUserRemoveEvent, Task> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_USER_REMOVE", handler);

    /// <summary>
    /// Subscribes to GUILD_SCHEDULED_EVENT_USER_REMOVE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildScheduledEventUserRemove(this EventDispatcher dispatcher, Action<GuildScheduledEventUserRemoveEvent> handler)
        => dispatcher.On("GUILD_SCHEDULED_EVENT_USER_REMOVE", handler);

    // Guild Soundboard Events

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_CREATE events.
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundCreate(this EventDispatcher dispatcher, Func<GuildSoundboardSoundCreateEvent, Task> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundCreate(this EventDispatcher dispatcher, Action<GuildSoundboardSoundCreateEvent> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundUpdate(this EventDispatcher dispatcher, Func<GuildSoundboardSoundUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundUpdate(this EventDispatcher dispatcher, Action<GuildSoundboardSoundUpdateEvent> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_DELETE events.
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundDelete(this EventDispatcher dispatcher, Func<GuildSoundboardSoundDeleteEvent, Task> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUND_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundDelete(this EventDispatcher dispatcher, Action<GuildSoundboardSoundDeleteEvent> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUND_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUNDS_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundsUpdate(this EventDispatcher dispatcher, Func<GuildSoundboardSoundsUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUNDS_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_SOUNDBOARD_SOUNDS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildSoundboardSoundsUpdate(this EventDispatcher dispatcher, Action<GuildSoundboardSoundsUpdateEvent> handler)
        => dispatcher.On("GUILD_SOUNDBOARD_SOUNDS_UPDATE", handler);

    // Channel Events

    /// <summary>
    /// Subscribes to CHANNEL_CREATE events.
    /// </summary>
    public static IDisposable OnChannelCreate(this EventDispatcher dispatcher, Func<ChannelCreateEvent, Task> handler)
        => dispatcher.On("CHANNEL_CREATE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnChannelCreate(this EventDispatcher dispatcher, Action<ChannelCreateEvent> handler)
        => dispatcher.On("CHANNEL_CREATE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_UPDATE events.
    /// </summary>
    public static IDisposable OnChannelUpdate(this EventDispatcher dispatcher, Func<ChannelUpdateEvent, Task> handler)
        => dispatcher.On("CHANNEL_UPDATE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnChannelUpdate(this EventDispatcher dispatcher, Action<ChannelUpdateEvent> handler)
        => dispatcher.On("CHANNEL_UPDATE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_DELETE events.
    /// </summary>
    public static IDisposable OnChannelDelete(this EventDispatcher dispatcher, Func<ChannelDeleteEvent, Task> handler)
        => dispatcher.On("CHANNEL_DELETE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnChannelDelete(this EventDispatcher dispatcher, Action<ChannelDeleteEvent> handler)
        => dispatcher.On("CHANNEL_DELETE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_PINS_UPDATE events.
    /// </summary>
    public static IDisposable OnChannelPinsUpdate(this EventDispatcher dispatcher, Func<ChannelPinsUpdateEvent, Task> handler)
        => dispatcher.On("CHANNEL_PINS_UPDATE", handler);

    /// <summary>
    /// Subscribes to CHANNEL_PINS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnChannelPinsUpdate(this EventDispatcher dispatcher, Action<ChannelPinsUpdateEvent> handler)
        => dispatcher.On("CHANNEL_PINS_UPDATE", handler);

    // Thread Events

    /// <summary>
    /// Subscribes to THREAD_CREATE events.
    /// </summary>
    public static IDisposable OnThreadCreate(this EventDispatcher dispatcher, Func<ThreadCreateEvent, Task> handler)
        => dispatcher.On("THREAD_CREATE", handler);

    /// <summary>
    /// Subscribes to THREAD_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnThreadCreate(this EventDispatcher dispatcher, Action<ThreadCreateEvent> handler)
        => dispatcher.On("THREAD_CREATE", handler);

    /// <summary>
    /// Subscribes to THREAD_UPDATE events.
    /// </summary>
    public static IDisposable OnThreadUpdate(this EventDispatcher dispatcher, Func<ThreadUpdateEvent, Task> handler)
        => dispatcher.On("THREAD_UPDATE", handler);

    /// <summary>
    /// Subscribes to THREAD_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnThreadUpdate(this EventDispatcher dispatcher, Action<ThreadUpdateEvent> handler)
        => dispatcher.On("THREAD_UPDATE", handler);

    /// <summary>
    /// Subscribes to THREAD_DELETE events.
    /// </summary>
    public static IDisposable OnThreadDelete(this EventDispatcher dispatcher, Func<ThreadDeleteEvent, Task> handler)
        => dispatcher.On("THREAD_DELETE", handler);

    /// <summary>
    /// Subscribes to THREAD_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnThreadDelete(this EventDispatcher dispatcher, Action<ThreadDeleteEvent> handler)
        => dispatcher.On("THREAD_DELETE", handler);

    /// <summary>
    /// Subscribes to THREAD_LIST_SYNC events.
    /// </summary>
    public static IDisposable OnThreadListSync(this EventDispatcher dispatcher, Func<ThreadListSyncEvent, Task> handler)
        => dispatcher.On("THREAD_LIST_SYNC", handler);

    /// <summary>
    /// Subscribes to THREAD_LIST_SYNC events (synchronous).
    /// </summary>
    public static IDisposable OnThreadListSync(this EventDispatcher dispatcher, Action<ThreadListSyncEvent> handler)
        => dispatcher.On("THREAD_LIST_SYNC", handler);

    /// <summary>
    /// Subscribes to THREAD_MEMBER_UPDATE events.
    /// </summary>
    public static IDisposable OnThreadMemberUpdate(this EventDispatcher dispatcher, Func<ThreadMemberUpdateEvent, Task> handler)
        => dispatcher.On("THREAD_MEMBER_UPDATE", handler);

    /// <summary>
    /// Subscribes to THREAD_MEMBER_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnThreadMemberUpdate(this EventDispatcher dispatcher, Action<ThreadMemberUpdateEvent> handler)
        => dispatcher.On("THREAD_MEMBER_UPDATE", handler);

    /// <summary>
    /// Subscribes to THREAD_MEMBERS_UPDATE events.
    /// </summary>
    public static IDisposable OnThreadMembersUpdate(this EventDispatcher dispatcher, Func<ThreadMembersUpdateEvent, Task> handler)
        => dispatcher.On("THREAD_MEMBERS_UPDATE", handler);

    /// <summary>
    /// Subscribes to THREAD_MEMBERS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnThreadMembersUpdate(this EventDispatcher dispatcher, Action<ThreadMembersUpdateEvent> handler)
        => dispatcher.On("THREAD_MEMBERS_UPDATE", handler);

    // Interaction Events

    /// <summary>
    /// Subscribes to INTERACTION_CREATE events.
    /// </summary>
    public static IDisposable OnInteractionCreate(this EventDispatcher dispatcher, Func<InteractionCreateEvent, Task> handler)
        => dispatcher.On("INTERACTION_CREATE", handler);

    /// <summary>
    /// Subscribes to INTERACTION_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnInteractionCreate(this EventDispatcher dispatcher, Action<InteractionCreateEvent> handler)
        => dispatcher.On("INTERACTION_CREATE", handler);

    // Voice Events

    /// <summary>
    /// Subscribes to VOICE_STATE_UPDATE events.
    /// </summary>
    public static IDisposable OnVoiceStateUpdate(this EventDispatcher dispatcher, Func<VoiceStateUpdateEvent, Task> handler)
        => dispatcher.On("VOICE_STATE_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_STATE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnVoiceStateUpdate(this EventDispatcher dispatcher, Action<VoiceStateUpdateEvent> handler)
        => dispatcher.On("VOICE_STATE_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_SERVER_UPDATE events.
    /// </summary>
    public static IDisposable OnVoiceServerUpdate(this EventDispatcher dispatcher, Func<VoiceServerUpdateEvent, Task> handler)
        => dispatcher.On("VOICE_SERVER_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_SERVER_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnVoiceServerUpdate(this EventDispatcher dispatcher, Action<VoiceServerUpdateEvent> handler)
        => dispatcher.On("VOICE_SERVER_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_EFFECT_SEND events.
    /// </summary>
    public static IDisposable OnVoiceChannelEffectSend(this EventDispatcher dispatcher, Func<VoiceChannelEffectSendEvent, Task> handler)
        => dispatcher.On("VOICE_CHANNEL_EFFECT_SEND", handler);

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_EFFECT_SEND events (synchronous).
    /// </summary>
    public static IDisposable OnVoiceChannelEffectSend(this EventDispatcher dispatcher, Action<VoiceChannelEffectSendEvent> handler)
        => dispatcher.On("VOICE_CHANNEL_EFFECT_SEND", handler);

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_STATUS_UPDATE events.
    /// </summary>
    public static IDisposable OnVoiceChannelStatusUpdate(this EventDispatcher dispatcher, Func<VoiceChannelStatusUpdateEvent, Task> handler)
        => dispatcher.On("VOICE_CHANNEL_STATUS_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_STATUS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnVoiceChannelStatusUpdate(this EventDispatcher dispatcher, Action<VoiceChannelStatusUpdateEvent> handler)
        => dispatcher.On("VOICE_CHANNEL_STATUS_UPDATE", handler);

    // Presence Events

    /// <summary>
    /// Subscribes to PRESENCE_UPDATE events.
    /// </summary>
    public static IDisposable OnPresenceUpdate(this EventDispatcher dispatcher, Func<PresenceUpdateEvent, Task> handler)
        => dispatcher.On("PRESENCE_UPDATE", handler);

    /// <summary>
    /// Subscribes to PRESENCE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnPresenceUpdate(this EventDispatcher dispatcher, Action<PresenceUpdateEvent> handler)
        => dispatcher.On("PRESENCE_UPDATE", handler);

    /// <summary>
    /// Subscribes to TYPING_START events.
    /// </summary>
    public static IDisposable OnTypingStart(this EventDispatcher dispatcher, Func<TypingStartEvent, Task> handler)
        => dispatcher.On("TYPING_START", handler);

    /// <summary>
    /// Subscribes to TYPING_START events (synchronous).
    /// </summary>
    public static IDisposable OnTypingStart(this EventDispatcher dispatcher, Action<TypingStartEvent> handler)
        => dispatcher.On("TYPING_START", handler);

    // Auto Moderation Events

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_CREATE events.
    /// </summary>
    public static IDisposable OnAutoModerationRuleCreate(this EventDispatcher dispatcher, Func<AutoModerationRuleCreateEvent, Task> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_CREATE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnAutoModerationRuleCreate(this EventDispatcher dispatcher, Action<AutoModerationRuleCreateEvent> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_CREATE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_UPDATE events.
    /// </summary>
    public static IDisposable OnAutoModerationRuleUpdate(this EventDispatcher dispatcher, Func<AutoModerationRuleUpdateEvent, Task> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_UPDATE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnAutoModerationRuleUpdate(this EventDispatcher dispatcher, Action<AutoModerationRuleUpdateEvent> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_UPDATE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_DELETE events.
    /// </summary>
    public static IDisposable OnAutoModerationRuleDelete(this EventDispatcher dispatcher, Func<AutoModerationRuleDeleteEvent, Task> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_DELETE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_RULE_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnAutoModerationRuleDelete(this EventDispatcher dispatcher, Action<AutoModerationRuleDeleteEvent> handler)
        => dispatcher.On("AUTO_MODERATION_RULE_DELETE", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_ACTION_EXECUTION events.
    /// </summary>
    public static IDisposable OnAutoModerationActionExecution(this EventDispatcher dispatcher, Func<AutoModerationActionExecutionEvent, Task> handler)
        => dispatcher.On("AUTO_MODERATION_ACTION_EXECUTION", handler);

    /// <summary>
    /// Subscribes to AUTO_MODERATION_ACTION_EXECUTION events (synchronous).
    /// </summary>
    public static IDisposable OnAutoModerationActionExecution(this EventDispatcher dispatcher, Action<AutoModerationActionExecutionEvent> handler)
        => dispatcher.On("AUTO_MODERATION_ACTION_EXECUTION", handler);

    // Stage Events

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_CREATE events.
    /// </summary>
    public static IDisposable OnStageInstanceCreate(this EventDispatcher dispatcher, Func<StageInstanceCreateEvent, Task> handler)
        => dispatcher.On("STAGE_INSTANCE_CREATE", handler);

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnStageInstanceCreate(this EventDispatcher dispatcher, Action<StageInstanceCreateEvent> handler)
        => dispatcher.On("STAGE_INSTANCE_CREATE", handler);

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_UPDATE events.
    /// </summary>
    public static IDisposable OnStageInstanceUpdate(this EventDispatcher dispatcher, Func<StageInstanceUpdateEvent, Task> handler)
        => dispatcher.On("STAGE_INSTANCE_UPDATE", handler);

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnStageInstanceUpdate(this EventDispatcher dispatcher, Action<StageInstanceUpdateEvent> handler)
        => dispatcher.On("STAGE_INSTANCE_UPDATE", handler);

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_DELETE events.
    /// </summary>
    public static IDisposable OnStageInstanceDelete(this EventDispatcher dispatcher, Func<StageInstanceDeleteEvent, Task> handler)
        => dispatcher.On("STAGE_INSTANCE_DELETE", handler);

    /// <summary>
    /// Subscribes to STAGE_INSTANCE_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnStageInstanceDelete(this EventDispatcher dispatcher, Action<StageInstanceDeleteEvent> handler)
        => dispatcher.On("STAGE_INSTANCE_DELETE", handler);

    // Entitlement Events

    /// <summary>
    /// Subscribes to ENTITLEMENT_CREATE events.
    /// </summary>
    public static IDisposable OnEntitlementCreate(this EventDispatcher dispatcher, Func<EntitlementCreateEvent, Task> handler)
        => dispatcher.On("ENTITLEMENT_CREATE", handler);

    /// <summary>
    /// Subscribes to ENTITLEMENT_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnEntitlementCreate(this EventDispatcher dispatcher, Action<EntitlementCreateEvent> handler)
        => dispatcher.On("ENTITLEMENT_CREATE", handler);

    /// <summary>
    /// Subscribes to ENTITLEMENT_UPDATE events.
    /// </summary>
    public static IDisposable OnEntitlementUpdate(this EventDispatcher dispatcher, Func<EntitlementUpdateEvent, Task> handler)
        => dispatcher.On("ENTITLEMENT_UPDATE", handler);

    /// <summary>
    /// Subscribes to ENTITLEMENT_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnEntitlementUpdate(this EventDispatcher dispatcher, Action<EntitlementUpdateEvent> handler)
        => dispatcher.On("ENTITLEMENT_UPDATE", handler);

    /// <summary>
    /// Subscribes to ENTITLEMENT_DELETE events.
    /// </summary>
    public static IDisposable OnEntitlementDelete(this EventDispatcher dispatcher, Func<EntitlementDeleteEvent, Task> handler)
        => dispatcher.On("ENTITLEMENT_DELETE", handler);

    /// <summary>
    /// Subscribes to ENTITLEMENT_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnEntitlementDelete(this EventDispatcher dispatcher, Action<EntitlementDeleteEvent> handler)
        => dispatcher.On("ENTITLEMENT_DELETE", handler);

    // Subscription Events

    /// <summary>
    /// Subscribes to SUBSCRIPTION_CREATE events.
    /// </summary>
    public static IDisposable OnSubscriptionCreate(this EventDispatcher dispatcher, Func<SubscriptionCreateEvent, Task> handler)
        => dispatcher.On("SUBSCRIPTION_CREATE", handler);

    /// <summary>
    /// Subscribes to SUBSCRIPTION_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnSubscriptionCreate(this EventDispatcher dispatcher, Action<SubscriptionCreateEvent> handler)
        => dispatcher.On("SUBSCRIPTION_CREATE", handler);

    /// <summary>
    /// Subscribes to SUBSCRIPTION_UPDATE events.
    /// </summary>
    public static IDisposable OnSubscriptionUpdate(this EventDispatcher dispatcher, Func<SubscriptionUpdateEvent, Task> handler)
        => dispatcher.On("SUBSCRIPTION_UPDATE", handler);

    /// <summary>
    /// Subscribes to SUBSCRIPTION_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnSubscriptionUpdate(this EventDispatcher dispatcher, Action<SubscriptionUpdateEvent> handler)
        => dispatcher.On("SUBSCRIPTION_UPDATE", handler);

    /// <summary>
    /// Subscribes to SUBSCRIPTION_DELETE events.
    /// </summary>
    public static IDisposable OnSubscriptionDelete(this EventDispatcher dispatcher, Func<SubscriptionDeleteEvent, Task> handler)
        => dispatcher.On("SUBSCRIPTION_DELETE", handler);

    /// <summary>
    /// Subscribes to SUBSCRIPTION_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnSubscriptionDelete(this EventDispatcher dispatcher, Action<SubscriptionDeleteEvent> handler)
        => dispatcher.On("SUBSCRIPTION_DELETE", handler);

    // Invite Events

    /// <summary>
    /// Subscribes to INVITE_CREATE events.
    /// </summary>
    public static IDisposable OnInviteCreate(this EventDispatcher dispatcher, Func<InviteCreateEvent, Task> handler)
        => dispatcher.On("INVITE_CREATE", handler);

    /// <summary>
    /// Subscribes to INVITE_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnInviteCreate(this EventDispatcher dispatcher, Action<InviteCreateEvent> handler)
        => dispatcher.On("INVITE_CREATE", handler);

    /// <summary>
    /// Subscribes to INVITE_DELETE events.
    /// </summary>
    public static IDisposable OnInviteDelete(this EventDispatcher dispatcher, Func<InviteDeleteEvent, Task> handler)
        => dispatcher.On("INVITE_DELETE", handler);

    /// <summary>
    /// Subscribes to INVITE_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnInviteDelete(this EventDispatcher dispatcher, Action<InviteDeleteEvent> handler)
        => dispatcher.On("INVITE_DELETE", handler);

    // Webhook Events

    /// <summary>
    /// Subscribes to WEBHOOKS_UPDATE events.
    /// </summary>
    public static IDisposable OnWebhooksUpdate(this EventDispatcher dispatcher, Func<WebhooksUpdateEvent, Task> handler)
        => dispatcher.On("WEBHOOKS_UPDATE", handler);

    /// <summary>
    /// Subscribes to WEBHOOKS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnWebhooksUpdate(this EventDispatcher dispatcher, Action<WebhooksUpdateEvent> handler)
        => dispatcher.On("WEBHOOKS_UPDATE", handler);

    // Permission Events

    /// <summary>
    /// Subscribes to APPLICATION_COMMAND_PERMISSIONS_UPDATE events.
    /// </summary>
    public static IDisposable OnApplicationCommandPermissionsUpdate(this EventDispatcher dispatcher, Func<ApplicationCommandPermissionsUpdateEvent, Task> handler)
        => dispatcher.On("APPLICATION_COMMAND_PERMISSIONS_UPDATE", handler);

    /// <summary>
    /// Subscribes to APPLICATION_COMMAND_PERMISSIONS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnApplicationCommandPermissionsUpdate(this EventDispatcher dispatcher, Action<ApplicationCommandPermissionsUpdateEvent> handler)
        => dispatcher.On("APPLICATION_COMMAND_PERMISSIONS_UPDATE", handler);

    // App Command Events

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_CREATE events.
    /// </summary>
    public static IDisposable OnGuildAppCommandCreate(this EventDispatcher dispatcher, Func<GuildAppCommandCreateEvent, Task> handler)
        => dispatcher.On("GUILD_APP_COMMAND_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildAppCommandCreate(this EventDispatcher dispatcher, Action<GuildAppCommandCreateEvent> handler)
        => dispatcher.On("GUILD_APP_COMMAND_CREATE", handler);

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_UPDATE events.
    /// </summary>
    public static IDisposable OnGuildAppCommandUpdate(this EventDispatcher dispatcher, Func<GuildAppCommandUpdateEvent, Task> handler)
        => dispatcher.On("GUILD_APP_COMMAND_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildAppCommandUpdate(this EventDispatcher dispatcher, Action<GuildAppCommandUpdateEvent> handler)
        => dispatcher.On("GUILD_APP_COMMAND_UPDATE", handler);

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_DELETE events.
    /// </summary>
    public static IDisposable OnGuildAppCommandDelete(this EventDispatcher dispatcher, Func<GuildAppCommandDeleteEvent, Task> handler)
        => dispatcher.On("GUILD_APP_COMMAND_DELETE", handler);

    /// <summary>
    /// Subscribes to GUILD_APP_COMMAND_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnGuildAppCommandDelete(this EventDispatcher dispatcher, Action<GuildAppCommandDeleteEvent> handler)
        => dispatcher.On("GUILD_APP_COMMAND_DELETE", handler);

    // Integration Events

    /// <summary>
    /// Subscribes to INTEGRATION_CREATE events.
    /// </summary>
    public static IDisposable OnIntegrationCreate(this EventDispatcher dispatcher, Func<IntegrationCreateEvent, Task> handler)
        => dispatcher.On("INTEGRATION_CREATE", handler);

    /// <summary>
    /// Subscribes to INTEGRATION_CREATE events (synchronous).
    /// </summary>
    public static IDisposable OnIntegrationCreate(this EventDispatcher dispatcher, Action<IntegrationCreateEvent> handler)
        => dispatcher.On("INTEGRATION_CREATE", handler);

    /// <summary>
    /// Subscribes to INTEGRATION_UPDATE events.
    /// </summary>
    public static IDisposable OnIntegrationUpdate(this EventDispatcher dispatcher, Func<IntegrationUpdateEvent, Task> handler)
        => dispatcher.On("INTEGRATION_UPDATE", handler);

    /// <summary>
    /// Subscribes to INTEGRATION_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnIntegrationUpdate(this EventDispatcher dispatcher, Action<IntegrationUpdateEvent> handler)
        => dispatcher.On("INTEGRATION_UPDATE", handler);

    /// <summary>
    /// Subscribes to INTEGRATION_DELETE events.
    /// </summary>
    public static IDisposable OnIntegrationDelete(this EventDispatcher dispatcher, Func<IntegrationDeleteEvent, Task> handler)
        => dispatcher.On("INTEGRATION_DELETE", handler);

    /// <summary>
    /// Subscribes to INTEGRATION_DELETE events (synchronous).
    /// </summary>
    public static IDisposable OnIntegrationDelete(this EventDispatcher dispatcher, Action<IntegrationDeleteEvent> handler)
        => dispatcher.On("INTEGRATION_DELETE", handler);

    // Voice Channel Status Events

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_STATUS_UPDATE events.
    /// </summary>
    public static IDisposable OnVoiceChannelStatusUpdate(this EventDispatcher dispatcher, Func<VoiceChannelStatusUpdateEvent, Task> handler)
        => dispatcher.On("VOICE_CHANNEL_STATUS_UPDATE", handler);

    /// <summary>
    /// Subscribes to VOICE_CHANNEL_STATUS_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnVoiceChannelStatusUpdate(this EventDispatcher dispatcher, Action<VoiceChannelStatusUpdateEvent> handler)
        => dispatcher.On("VOICE_CHANNEL_STATUS_UPDATE", handler);

    // User Events

    /// <summary>
    /// Subscribes to USER_UPDATE events.
    /// </summary>
    public static IDisposable OnUserUpdate(this EventDispatcher dispatcher, Func<UserUpdateEvent, Task> handler)
        => dispatcher.On("USER_UPDATE", handler);

    /// <summary>
    /// Subscribes to USER_UPDATE events (synchronous).
    /// </summary>
    public static IDisposable OnUserUpdate(this EventDispatcher dispatcher, Action<UserUpdateEvent> handler)
        => dispatcher.On("USER_UPDATE", handler);
}
