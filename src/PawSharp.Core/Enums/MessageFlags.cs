using System;

namespace PawSharp.Core.Enums;

/// <summary>
/// Bitfield flags that can be set on a Discord message.
/// See: https://discord.com/developers/docs/resources/message#message-object-message-flags
/// </summary>
[Flags]
public enum MessageFlags
{
    None                          = 0,
    /// <summary>This message has been published to subscribed channels (via Channel Following).</summary>
    Crossposted                   = 1 << 0,
    /// <summary>This message originated from a message in another channel (via Channel Following).</summary>
    IsCrosspost                   = 1 << 1,
    /// <summary>Do not include any embeds when serializing this message.</summary>
    SuppressEmbeds                = 1 << 2,
    /// <summary>The source message for this crosspost has been deleted.</summary>
    SourceMessageDeleted          = 1 << 3,
    /// <summary>This message came from the urgent message system.</summary>
    Urgent                        = 1 << 4,
    /// <summary>This message has an associated thread with the same id as the message.</summary>
    HasThread                     = 1 << 5,
    /// <summary>This message is only visible to the user who invoked the Interaction.</summary>
    Ephemeral                     = 1 << 6,
    /// <summary>This message is an Interaction Response and the bot is "thinking".</summary>
    Loading                       = 1 << 7,
    /// <summary>This message failed to mention some roles and add their members to the thread.</summary>
    FailedToMentionSomeRoles      = 1 << 8,
    /// <summary>This message will not trigger push and desktop notifications.</summary>
    SuppressNotifications         = 1 << 12,
    /// <summary>This message is a voice message.</summary>
    IsVoiceMessage                = 1 << 13,
    /// <summary>This message uses the components v2 layout.</summary>
    IsComponentsV2                = 1 << 15,
}
