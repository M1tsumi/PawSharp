namespace PawSharp.Commands.Permissions;

/// <summary>
/// Discord permission bit flags for use with <see cref="Preconditions.RequirePermissionsAttribute"/>.
/// </summary>
public static class DiscordPermissions
{
    /// <summary>Allows creation of instant invites.</summary>
    public const ulong CreateInstantInvite = 0x0000000001;

    /// <summary>Allows kicking members.</summary>
    public const ulong KickMembers = 0x0000000002;

    /// <summary>Allows banning members.</summary>
    public const ulong BanMembers = 0x0000000004;

    /// <summary>Allows all permissions and bypasses channel permission overwrites.</summary>
    public const ulong Administrator = 0x0000000008;

    /// <summary>Allows management and editing of channels.</summary>
    public const ulong ManageChannels = 0x0000000010;

    /// <summary>Allows management of the guild.</summary>
    public const ulong ManageGuild = 0x0000000020;

    /// <summary>Allows for the addition of reactions to messages.</summary>
    public const ulong AddReactions = 0x0000000040;

    /// <summary>Allows for viewing of audit logs.</summary>
    public const ulong ViewAuditLog = 0x0000000080;

    /// <summary>Allows for using the @everyone tag to mention all users in a channel.</summary>
    public const ulong PrioritySpeaker = 0x0000000100;

    /// <summary>Allows for streaming video.</summary>
    public const ulong Stream = 0x0000000200;

    /// <summary>Allows for reading messages in a channel.</summary>
    public const ulong ReadMessages = 0x0000000400;

    /// <summary>Allows for sending messages in a channel.</summary>
    public const ulong SendMessages = 0x0000000800;

    /// <summary>Allows for sending of text-to-speech messages.</summary>
    public const ulong SendTtsMessages = 0x0000001000;

    /// <summary>Allows for management of messages.</summary>
    public const ulong ManageMessages = 0x0000002000;

    /// <summary>Allows for embedding links in messages.</summary>
    public const ulong EmbedLinks = 0x0000004000;

    /// <summary>Allows for uploading files.</summary>
    public const ulong AttachFiles = 0x0000008000;

    /// <summary>Allows for reading of message history.</summary>
    public const ulong ReadHistory = 0x0000010000;

    /// <summary>Allows for using the @everyone tag to mention all users in the guild, and the @here tag to mention all online users.</summary>
    public const ulong MentionEveryone = 0x0000020000;

    /// <summary>Allows for usage of external emojis.</summary>
    public const ulong UseExternalEmojis = 0x0000040000;

    /// <summary>Allows for viewing guild insights.</summary>
    public const ulong ViewGuildInsights = 0x0000080000;

    /// <summary>Allows for joining voice channels.</summary>
    public const ulong Connect = 0x0000100000;

    /// <summary>Allows for speaking in voice channels.</summary>
    public const ulong Speak = 0x0000200000;

    /// <summary>Allows for muting members in voice channels.</summary>
    public const ulong MuteMembers = 0x0000400000;

    /// <summary>Allows for deafening members in voice channels.</summary>
    public const ulong DeafenMembers = 0x0000800000;

    /// <summary>Allows for moving members between voice channels.</summary>
    public const ulong MoveMembers = 0x0001000000;

    /// <summary>Allows for using voice-activity-detection.</summary>
    public const ulong UseVoiceActivation = 0x0002000000;

    /// <summary>Allows for changing nickname.</summary>
    public const ulong ChangeNickname = 0x0004000000;

    /// <summary>Allows for management of nicknames.</summary>
    public const ulong ManageNicknames = 0x0008000000;

    /// <summary>Allows for management of roles.</summary>
    public const ulong ManageRoles = 0x0010000000;

    /// <summary>Allows for management of webhooks.</summary>
    public const ulong ManageWebhooks = 0x0020000000;

    /// <summary>Allows for management of emojis and stickers.</summary>
    public const ulong ManageEmojis = 0x0040000000;

    /// <summary>Allows for using application commands in text channels.</summary>
    public const ulong UseApplicationCommands = 0x0000000080000000;

    /// <summary>Allows for requesting to speak in stage channels.</summary>
    public const ulong RequestToSpeak = 0x0000000100000000;

    /// <summary>Allows for management of events.</summary>
    public const ulong ManageEvents = 0x0000000200000000;

    /// <summary>Allows for management of threads.</summary>
    public const ulong ManageThreads = 0x0000000400000000;

    /// <summary>Allows for creating public threads.</summary>
    public const ulong CreatePublicThreads = 0x0000000800000000;

    /// <summary>Allows for creating private threads.</summary>
    public const ulong CreatePrivateThreads = 0x0000001000000000;

    /// <summary>Allows for using external stickers.</summary>
    public const ulong UseExternalStickers = 0x0000002000000000;

    /// <summary>Allows for sending messages in threads.</summary>
    public const ulong SendMessagesInThreads = 0x0000004000000000;

    /// <summary>Allows for starting activities in a voice channel.</summary>
    public const ulong UseEmbeddedActivities = 0x0000008000000000;

    /// <summary>Allows for moderation of members.</summary>
    public const ulong ModerateMembers = 0x0000010000000000;

    /// <summary>Allows for viewing role subscription insights.</summary>
    public const ulong ViewCreatorMonetizationAnalytics = 0x0000020000000000;

    /// <summary>Allows for using soundboard in voice channels.</summary>
    public const ulong UseSoundboard = 0x0000040000000000;

    /// <summary>Allows for creating expressions (emoji, stickers, soundboard sounds).</summary>
    public const ulong CreateExpressions = 0x0000080000000000;

    /// <summary>Allows for creating events.</summary>
    public const ulong CreateEvents = 0x0000100000000000;

    /// <summary>Allows for using activities, applications, or emojis as a soundboard sound.</summary>
    public const ulong UseExternalSounds = 0x0000200000000000;

    /// <summary>Allows sending voice messages.</summary>
    public const ulong SendVoiceMessages = 0x0000400000000000;

    /// <summary>Allows setting voice channel status.</summary>
    public const ulong SetVoiceChannelStatus = 0x0000800000000000;

    /// <summary>Allows sending polls.</summary>
    public const ulong SendPolls = 0x0001000000000000;

    /// <summary>Allows using external apps.</summary>
    public const ulong UseExternalApps = 0x0002000000000000;

    /// <summary>Allows pinning messages in a channel.</summary>
    public const ulong PinMessages = 0x0004000000000000;

    /// <summary>Allows bypassing slowmode rate limits.</summary>
    public const ulong BypassSlowmode = 0x0008000000000000;
}
