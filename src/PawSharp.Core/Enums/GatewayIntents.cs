using System;

namespace PawSharp.Core.Enums;

/// <summary>
/// Bitfield flags for Gateway intents (Discord API v10).
/// Controls which events the bot receives.
/// </summary>
[Flags]
public enum GatewayIntents : uint
{
    /// <summary>
    /// Guild-related events (e.g., GUILD_CREATE).
    /// </summary>
    Guilds = 1 << 0,
    
    /// <summary>
    /// Guild member events (e.g., GUILD_MEMBER_ADD).
    /// </summary>
    GuildMembers = 1 << 1,
    
    /// <summary>
    /// Guild moderation events (e.g., GUILD_BAN_ADD).
    /// </summary>
    GuildModeration = 1 << 2,
    
    /// <summary>
    /// Guild emoji/sticker events.
    /// </summary>
    GuildEmojisAndStickers = 1 << 3,
    
    /// <summary>
    /// Guild integration events.
    /// </summary>
    GuildIntegrations = 1 << 4,
    
    /// <summary>
    /// Guild webhooks events.
    /// </summary>
    GuildWebhooks = 1 << 5,
    
    /// <summary>
    /// Guild invite events.
    /// </summary>
    GuildInvites = 1 << 6,
    
    /// <summary>
    /// Guild voice state events.
    /// </summary>
    GuildVoiceStates = 1 << 7,
    
    /// <summary>
    /// Guild presence events (requires privileged intent).
    /// </summary>
    GuildPresences = 1 << 8,
    
    /// <summary>
    /// Guild message events (e.g., MESSAGE_CREATE).
    /// </summary>
    GuildMessages = 1 << 9,
    
    /// <summary>
    /// Guild message reaction events.
    /// </summary>
    GuildMessageReactions = 1 << 10,
    
    /// <summary>
    /// Guild message typing events.
    /// </summary>
    GuildMessageTyping = 1 << 11,
    
    /// <summary>
    /// Direct message events.
    /// </summary>
    DirectMessages = 1 << 12,
    
    /// <summary>
    /// Direct message reaction events.
    /// </summary>
    DirectMessageReactions = 1 << 13,
    
    /// <summary>
    /// Direct message typing events.
    /// </summary>
    DirectMessageTyping = 1 << 14,
    
    /// <summary>
    /// Message content intent (privileged, for bots with access).
    /// </summary>
    MessageContent = 1 << 15,
    
    /// <summary>
    /// Guild scheduled event events.
    /// </summary>
    GuildScheduledEvents = 1 << 16,
    
    /// <summary>
    /// Auto-moderation configuration events.
    /// </summary>
    AutoModerationConfiguration = 1 << 20,
    
    /// <summary>
    /// Auto-moderation execution events.
    /// </summary>
    AutoModerationExecution = 1 << 21,
    
    /// <summary>
    /// All non-privileged intents.
    /// </summary>
    AllNonPrivileged = Guilds | GuildModeration | GuildEmojisAndStickers | GuildIntegrations | GuildWebhooks | GuildInvites | GuildVoiceStates | GuildMessages | GuildMessageReactions | GuildMessageTyping | DirectMessages | DirectMessageReactions | DirectMessageTyping | GuildScheduledEvents | AutoModerationConfiguration | AutoModerationExecution,
    
    /// <summary>
    /// All intents (including privileged).
    /// </summary>
    All = AllNonPrivileged | GuildMembers | GuildPresences | MessageContent,
}