using System;
using System.Collections.Generic;
using System.Linq;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Events;

/// <summary>
/// Declares which gateway events a handler is interested in, enabling intent validation
/// and automatic filtering of irrelevant events.
/// </summary>
/// <remarks>
/// This attribute enables the event dispatcher to:
/// 1. Validate that required intents are enabled at connection time
/// 2. Warn developers when a handler expects an event but the required intent is missing
/// 3. (Future) Filter events at the dispatcher level for performance
/// 
/// Example:
/// <code>
/// [EventInterest(GatewayEvents.MessageCreate, GatewayEvents.MessageDelete)]
/// public async Task OnMessage(MessageCreateEvent evt) { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class EventInterestAttribute : Attribute
{
    /// <summary>
    /// The gateway event types this handler is interested in.
    /// </summary>
    public IReadOnlySet<string> EventTypes { get; }

    /// <summary>
    /// The required intents for receiving these events.
    /// </summary>
    public GatewayIntents RequiredIntents { get; }

    /// <summary>
    /// Creates an event interest declaration from event type names.
    /// </summary>
    /// <remarks>
    /// The provided event types are validated against the known gateway events.
    /// If an unknown event is specified, it will be included but not warn on intent mismatch.
    /// </remarks>
    /// <param name="eventTypes">Gateway event type names (e.g., "MESSAGE_CREATE", "READY")</param>
    public EventInterestAttribute(params string[] eventTypes)
    {
        if (eventTypes == null || eventTypes.Length == 0)
            throw new ArgumentException("At least one event type must be specified.", nameof(eventTypes));

        var uniqueTypes = new HashSet<string>(eventTypes, StringComparer.Ordinal);
        EventTypes = uniqueTypes;
        RequiredIntents = CalculateRequiredIntents(uniqueTypes);
    }

    /// <summary>
    /// Maps gateway event types to the required intents.
    /// </summary>
    /// <remarks>
    /// This mapping is based on Discord's official documentation of which intents
    /// are required for each event type. Unknown events return (GatewayIntents)0.
    /// </remarks>
    private static GatewayIntents CalculateRequiredIntents(IEnumerable<string> eventTypes)
    {
        GatewayIntents intents = (GatewayIntents)0;

        foreach (var eventType in eventTypes)
        {
            intents |= eventType switch
            {
                // Guild events
                "GUILD_CREATE" or "GUILD_UPDATE" or "GUILD_DELETE" => GatewayIntents.Guilds,
                "GUILD_ROLE_CREATE" or "GUILD_ROLE_UPDATE" or "GUILD_ROLE_DELETE" => GatewayIntents.Guilds,
                "CHANNEL_CREATE" or "CHANNEL_UPDATE" or "CHANNEL_DELETE" => GatewayIntents.Guilds,
                "CHANNEL_PINS_UPDATE" => GatewayIntents.Guilds | GatewayIntents.DirectMessages,
                "THREAD_CREATE" or "THREAD_UPDATE" or "THREAD_DELETE" or "THREAD_LIST_SYNC" or "THREAD_MEMBER_UPDATE" or "THREAD_MEMBERS_UPDATE" => GatewayIntents.Guilds,

                // Guild moderation
                "GUILD_BAN_ADD" or "GUILD_BAN_REMOVE" => GatewayIntents.GuildModeration,
                "GUILD_AUDIT_LOG_ENTRY_CREATE" => GatewayIntents.GuildModeration,

                // Guild emojis & stickers
                "GUILD_EMOJIS_UPDATE" or "GUILD_STICKERS_UPDATE" => GatewayIntents.GuildEmojisAndStickers,

                // Guild integrations
                "INTEGRATION_CREATE" or "INTEGRATION_UPDATE" or "INTEGRATION_DELETE" => GatewayIntents.GuildIntegrations,

                // Guild webhooks
                "WEBHOOKS_UPDATE" => GatewayIntents.GuildWebhooks,

                // Guild invites
                "INVITE_CREATE" or "INVITE_DELETE" => GatewayIntents.GuildInvites,

                // Guild voice states
                "VOICE_STATE_UPDATE" => GatewayIntents.GuildVoiceStates,

                // Messages (guild + DM + content)
                "MESSAGE_CREATE" or "MESSAGE_UPDATE" or "MESSAGE_DELETE" or "MESSAGE_BULK_DELETE" => GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent,

                // Message reactions (guild + DM)
                "MESSAGE_REACTION_ADD" or "MESSAGE_REACTION_REMOVE" or "MESSAGE_REACTION_REMOVE_ALL" or "MESSAGE_REACTION_REMOVE_EMOJI" => GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessageReactions,

                // Message typing (guild + DM)
                "TYPING_START" => GatewayIntents.GuildMessageTyping | GatewayIntents.DirectMessageTyping,

                // Guild scheduled events
                "GUILD_SCHEDULED_EVENT_CREATE" or "GUILD_SCHEDULED_EVENT_UPDATE" or "GUILD_SCHEDULED_EVENT_DELETE" or "GUILD_SCHEDULED_EVENT_USER_ADD" or "GUILD_SCHEDULED_EVENT_USER_REMOVE" => GatewayIntents.GuildScheduledEvents,

                // Message polls (guild + DM)
                "MESSAGE_POLL_VOTE_ADD" or "MESSAGE_POLL_VOTE_REMOVE" => GatewayIntents.GuildMessagePolls | GatewayIntents.DirectMessagePolls,

                // Guild members (requires GUILD_MEMBERS intent - privileged)
                "GUILD_MEMBER_ADD" or "GUILD_MEMBER_UPDATE" or "GUILD_MEMBER_REMOVE" => GatewayIntents.GuildMembers,

                // Guild presences (requires GUILD_PRESENCES intent - privileged)
                "PRESENCE_UPDATE" => GatewayIntents.GuildPresences,

                // Auto moderation
                "AUTO_MODERATION_RULE_CREATE" or "AUTO_MODERATION_RULE_UPDATE" or "AUTO_MODERATION_RULE_DELETE" => GatewayIntents.AutoModerationConfiguration,
                "AUTO_MODERATION_ACTION_EXECUTION" => GatewayIntents.AutoModerationExecution,

                // Special events (no intent needed)
                "READY" or "RESUMED" or "APPLICATION_COMMAND_PERMISSIONS_UPDATE" => (GatewayIntents)0,

                // Unknown events return no requirements (no validation)
                _ => (GatewayIntents)0
            };
        }

        return intents;
    }
}
