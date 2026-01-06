#nullable enable
using System;

namespace PawSharp.Core.Entities;

/// <summary>
/// Guild verification level.
/// </summary>
public enum VerificationLevel
{
    /// <summary>
    /// Unrestricted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Must have verified email on account.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Must be registered on Discord for longer than 5 minutes.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Must be a member of the server for longer than 10 minutes.
    /// </summary>
    High = 3,

    /// <summary>
    /// Must have a verified phone number.
    /// </summary>
    VeryHigh = 4
}

/// <summary>
/// Default message notification level.
/// </summary>
public enum DefaultMessageNotificationLevel
{
    /// <summary>
    /// Members will receive notifications for all messages by default.
    /// </summary>
    AllMessages = 0,

    /// <summary>
    /// Members will receive notifications only for messages that @mention them by default.
    /// </summary>
    OnlyMentions = 1
}

/// <summary>
/// Explicit content filter level.
/// </summary>
public enum ExplicitContentFilterLevel
{
    /// <summary>
    /// Media content will not be scanned.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Media content sent by members without roles will be scanned.
    /// </summary>
    MembersWithoutRoles = 1,

    /// <summary>
    /// Media content sent by all members will be scanned.
    /// </summary>
    AllMembers = 2
}

/// <summary>
/// System channel flags.
/// </summary>
[Flags]
public enum SystemChannelFlags
{
    /// <summary>
    /// Suppress member join notifications.
    /// </summary>
    SuppressJoinNotifications = 1 << 0,

    /// <summary>
    /// Suppress server boost notifications.
    /// </summary>
    SuppressPremiumSubscriptions = 1 << 1,

    /// <summary>
    /// Suppress server setup tips.
    /// </summary>
    SuppressGuildReminderNotifications = 1 << 2,

    /// <summary>
    /// Hide member join sticker reply buttons.
    /// </summary>
    SuppressJoinNotificationReplies = 1 << 3
}