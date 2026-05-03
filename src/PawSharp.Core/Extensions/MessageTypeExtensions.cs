namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Enums.MessageType"/> enum.
/// </summary>
public static class MessageTypeExtensions
{
    /// <summary>
    /// Checks if the message type is a system message.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is a system message.</returns>
    public static bool IsSystemMessage(this Enums.MessageType type)
    {
        return type is Enums.MessageType.RecipientAdd or Enums.MessageType.RecipientRemove or
               Enums.MessageType.Call or Enums.MessageType.ChannelNameChange or
               Enums.MessageType.ChannelIconChange or Enums.MessageType.ChannelPinnedMessage or
               Enums.MessageType.UserJoin or Enums.MessageType.GuildBoost or
               Enums.MessageType.GuildBoostTier1 or Enums.MessageType.GuildBoostTier2 or
               Enums.MessageType.GuildBoostTier3 or Enums.MessageType.ChannelFollowAdd or
               Enums.MessageType.GuildDiscoveryDisqualified or Enums.MessageType.GuildDiscoveryRequalified or
               Enums.MessageType.GuildDiscoveryGracePeriodInitialWarning or
               Enums.MessageType.GuildDiscoveryGracePeriodFinalWarning or
               Enums.MessageType.ThreadCreated or Enums.MessageType.GuildInviteReminder or
               Enums.MessageType.ChatInputCommand or Enums.MessageType.ContextMenuCommand or
               Enums.MessageType.AutoModerationAction or
               Enums.MessageType.RoleSubscriptionPurchase or Enums.MessageType.InteractionPremiumUpsell or
               Enums.MessageType.GuildApplicationPremiumSubscription or Enums.MessageType.StageStart or
               Enums.MessageType.StageEnd or Enums.MessageType.StageSpeaker or Enums.MessageType.StageTopic or
               Enums.MessageType.GuildIncidentAlertModeEnabled or Enums.MessageType.GuildIncidentAlertModeDisabled or
               Enums.MessageType.GuildIncidentReportRaid or Enums.MessageType.GuildIncidentReportFalseAlarm or
               Enums.MessageType.PurchaseNotification or Enums.MessageType.PollResult;
    }

    /// <summary>
    /// Checks if the message type is a user-generated message (not a system message).
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is user-generated.</returns>
    public static bool IsUserMessage(this Enums.MessageType type)
    {
        return type == Enums.MessageType.Default;
    }

    /// <summary>
    /// Checks if the message type is a reply.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is a reply.</returns>
    public static bool IsReply(this Enums.MessageType type)
    {
        return type == Enums.MessageType.Reply;
    }

    /// <summary>
    /// Checks if the message type is a command (slash command or context menu).
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is a command.</returns>
    public static bool IsCommand(this Enums.MessageType type)
    {
        return type is Enums.MessageType.ChatInputCommand or Enums.MessageType.ContextMenuCommand;
    }

    /// <summary>
    /// Checks if the message type is from an auto moderation action.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is from auto moderation.</returns>
    public static bool IsAutoModeration(this Enums.MessageType type)
    {
        return type == Enums.MessageType.AutoModerationAction;
    }

    /// <summary>
    /// Checks if the message type is related to role subscriptions.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is related to role subscriptions.</returns>
    public static bool IsRoleSubscription(this Enums.MessageType type)
    {
        return type is Enums.MessageType.RoleSubscriptionPurchase or
               Enums.MessageType.InteractionPremiumUpsell or
               Enums.MessageType.GuildApplicationPremiumSubscription;
    }

    /// <summary>
    /// Checks if the message type is related to stage channels.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <returns>True if the message is related to stage channels.</returns>
    public static bool IsStageEvent(this Enums.MessageType type)
    {
        return type is Enums.MessageType.StageStart or Enums.MessageType.StageEnd or
               Enums.MessageType.StageSpeaker or Enums.MessageType.StageTopic;
    }
}
