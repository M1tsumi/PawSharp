namespace PawSharp.Core.Enums;

/// <summary>
/// Discord guild feature strings.
/// Discord returns guild features as a list of strings (e.g., "COMMUNITY", "PARTNERED").
/// This enum provides type-safe access to common guild features.
/// </summary>
public enum GuildFeature
{
    /// <summary>Guild has access to set an animated guild icon.</summary>
    AnimatedIcon,
    
    /// <summary>Guild has access to set a guild banner image.</summary>
    Banner,
    
    /// <summary>Guild can enable welcome screen and Membership Screening.</summary>
    Community,
    
    /// <summary>Guild has enabled ticketed events.</summary>
    TicketedEvents,
    
    /// <summary>Guild has access to set a guild directory channel.</summary>
    GuildDirectory,
    
    /// <summary>Guild is partnered.</summary>
    Partnered,
    
    /// <summary>Guild has enabled the role subscription feature.</summary>
    RoleSubscriptions,
    
    /// <summary>Guild has enabled monetization.</summary>
    CreatorMonetizable,
    
    /// <summary>Guild has enabled the creator store.</summary>
    CreatorStorePage,
    
    /// <summary>Guild has enabled soundboard.</summary>
    Soundboard,
    
    /// <summary>Guild has enabled vanity URL.</summary>
    VanityUrl,
    
    /// <summary>Guild is verified.</summary>
    Verified,
    
    /// <summary>Guild has enabled VIP regions.</summary>
    VIPRegions,
    
    /// <summary>Guild has enabled threads.</summary>
    Threads,
    
    /// <summary>Guild has enabled stage channels.</summary>
    StageChannels,
    
    /// <summary>Guild has enabled the premium tier.</summary>
    Premium,
    
    /// <summary>Guild has enabled the news channel.</summary>
    News,
    
    /// <summary>Guild is discoverable.</summary>
    Discoverable,
    
    /// <summary>Guild has enabled the invite splash.</summary>
    InviteSplash,
    
    /// <summary>Guild has enabled the member verification gate.</summary>
    MemberVerificationGate,
    
    /// <summary>Guild has enabled the preview features.</summary>
    PreviewEnabled,
    
    /// <summary>Guild has enabled the animated banner.</summary>
    AnimatedBanner,
    
    /// <summary>Guild has enabled auto moderation.</summary>
    AutoModeration,
    
    /// <summary>Guild has enabled the raid alerts.</summary>
    RaidAlertsEnabled,
    
    /// <summary>Guild has enabled the application commands.</summary>
    ApplicationCommands,
    
    /// <summary>Guild has enabled the embedded activities.</summary>
    EmbeddedActivities,
    
    /// <summary>Guild has enabled the guild webhooks.</summary>
    GuildWebhooks,
    
    /// <summary>Guild has enabled the guild member verification.</summary>
    GuildMemberVerification,
    
    /// <summary>Guild has enabled the guild onboarding.</summary>
    GuildOnboarding,
    
    /// <summary>Guild has enabled the guild scheduled events.</summary>
    GuildScheduledEvents,
    
    /// <summary>Guild has enabled the guild stickers.</summary>
    GuildStickers,
    
    /// <summary>Guild has enabled the guild welcome screen.</summary>
    GuildWelcomeScreen,
    
    /// <summary>Guild has enabled the guild widget.</summary>
    GuildWidget,
    
    /// <summary>Guild has enabled the guild insights.</summary>
    GuildInsights,
    
    /// <summary>Guild has enabled the guild invites.</summary>
    GuildInvites,
    
    /// <summary>Guild has enabled the guild discovery.</summary>
    GuildDiscovery,
    
    /// <summary>Guild has enabled the guild directory.</summary>
    GuildDirectoryChannels,
    
    /// <summary>Guild has enabled the guild roles.</summary>
    GuildRoles,
    
    /// <summary>Guild has enabled the guild emojis.</summary>
    GuildEmojis,
    
    /// <summary>Guild has enabled the guild voice states.</summary>
    GuildVoiceStates,
    
    /// <summary>Guild has enabled the guild presences.</summary>
    GuildPresences,
    
    /// <summary>Guild has enabled the guild messages.</summary>
    GuildMessages,
    
    /// <summary>Guild has enabled the guild message reactions.</summary>
    GuildMessageReactions,
    
    /// <summary>Guild has enabled the guild message typing.</summary>
    GuildMessageTyping,
    
    /// <summary>Guild has enabled the guild message mentions.</summary>
    GuildMessageMentions,
    
    /// <summary>Guild has enabled the guild message embeds.</summary>
    GuildMessageEmbeds,
    
    /// <summary>Guild has enabled the guild message attachments.</summary>
    GuildMessageAttachments,
    
    /// <summary>Guild has enabled the guild message content.</summary>
    GuildMessageContent,
    
    /// <summary>Guild has enabled the guild message history.</summary>
    GuildMessageHistory,
    
    /// <summary>Guild has enabled the guild message pins.</summary>
    GuildMessagePins,
    
    /// <summary>Guild has enabled the guild message polls.</summary>
    GuildMessagePolls,
    
    /// <summary>Guild has enabled the guild message stickers.</summary>
    GuildMessageStickers,
    
    /// <summary>Guild has enabled the guild message emojis.</summary>
    GuildMessageEmojis,
    
    /// <summary>Guild has enabled the guild message components.</summary>
    GuildMessageComponents,
    
    /// <summary>Guild has enabled the guild message interactions.</summary>
    GuildMessageInteractions,
    
    /// <summary>Guild has enabled the guild message threads.</summary>
    GuildMessageThreads,
    
    /// <summary>Guild has enabled the guild message forum posts.</summary>
    GuildMessageForumPosts,
    
    /// <summary>Guild has enabled the guild message media channels.</summary>
    GuildMessageMediaChannels,
}

/// <summary>
/// Extension methods for GuildFeature enum.
/// </summary>
public static class GuildFeatureExtensions
{
    /// <summary>
    /// Converts a GuildFeature enum to its Discord API string representation.
    /// </summary>
    /// <param name="feature">The guild feature.</param>
    /// <returns>The Discord API string representation.</returns>
    public static string ToApiString(this GuildFeature feature)
    {
        return feature switch
        {
            GuildFeature.AnimatedIcon => "ANIMATED_ICON",
            GuildFeature.Banner => "BANNER",
            GuildFeature.Community => "COMMUNITY",
            GuildFeature.TicketedEvents => "TICKETED_EVENTS",
            GuildFeature.GuildDirectory => "GUILD_DIRECTORY",
            GuildFeature.Partnered => "PARTNERED",
            GuildFeature.RoleSubscriptions => "ROLE_SUBSCRIPTIONS",
            GuildFeature.CreatorMonetizable => "CREATOR_MONETIZABLE",
            GuildFeature.CreatorStorePage => "CREATOR_STORE_PAGE",
            GuildFeature.Soundboard => "SOUNDBOARD",
            GuildFeature.VanityUrl => "VANITY_URL",
            GuildFeature.Verified => "VERIFIED",
            GuildFeature.VIPRegions => "VIP_REGIONS",
            GuildFeature.Threads => "THREADS",
            GuildFeature.StageChannels => "STAGE_CHANNELS",
            GuildFeature.Premium => "PREMIUM",
            GuildFeature.News => "NEWS",
            GuildFeature.Discoverable => "DISCOVERABLE",
            GuildFeature.InviteSplash => "INVITE_SPLASH",
            GuildFeature.MemberVerificationGate => "MEMBER_VERIFICATION_GATE",
            GuildFeature.PreviewEnabled => "PREVIEW_ENABLED",
            GuildFeature.AnimatedBanner => "ANIMATED_BANNER",
            GuildFeature.AutoModeration => "AUTO_MODERATION",
            GuildFeature.RaidAlertsEnabled => "RAID_ALERTS_ENABLED",
            GuildFeature.ApplicationCommands => "APPLICATION_COMMANDS",
            GuildFeature.EmbeddedActivities => "EMBEDDED_ACTIVITIES",
            GuildFeature.GuildWebhooks => "GUILD_WEBHOOKS",
            GuildFeature.GuildMemberVerification => "GUILD_MEMBER_VERIFICATION",
            GuildFeature.GuildOnboarding => "GUILD_ONBOARDING",
            GuildFeature.GuildScheduledEvents => "GUILD_SCHEDULED_EVENTS",
            GuildFeature.GuildStickers => "GUILD_STICKERS",
            GuildFeature.GuildWelcomeScreen => "GUILD_WELCOME_SCREEN",
            GuildFeature.GuildWidget => "GUILD_WIDGET",
            GuildFeature.GuildInsights => "GUILD_INSIGHTS",
            GuildFeature.GuildInvites => "GUILD_INVITES",
            GuildFeature.GuildDiscovery => "GUILD_DISCOVERY",
            GuildFeature.GuildDirectoryChannels => "GUILD_DIRECTORY_CHANNELS",
            GuildFeature.GuildRoles => "GUILD_ROLES",
            GuildFeature.GuildEmojis => "GUILD_EMOJIS",
            GuildFeature.GuildVoiceStates => "GUILD_VOICE_STATES",
            GuildFeature.GuildPresences => "GUILD_PRESENCES",
            GuildFeature.GuildMessages => "GUILD_MESSAGES",
            GuildFeature.GuildMessageReactions => "GUILD_MESSAGE_REACTIONS",
            GuildFeature.GuildMessageTyping => "GUILD_MESSAGE_TYPING",
            GuildFeature.GuildMessageMentions => "GUILD_MESSAGE_MENTIONS",
            GuildFeature.GuildMessageEmbeds => "GUILD_MESSAGE_EMBEDS",
            GuildFeature.GuildMessageAttachments => "GUILD_MESSAGE_ATTACHMENTS",
            GuildFeature.GuildMessageContent => "GUILD_MESSAGE_CONTENT",
            GuildFeature.GuildMessageHistory => "GUILD_MESSAGE_HISTORY",
            GuildFeature.GuildMessagePins => "GUILD_MESSAGE_PINS",
            GuildFeature.GuildMessagePolls => "GUILD_MESSAGE_POLLS",
            GuildFeature.GuildMessageStickers => "GUILD_MESSAGE_STICKERS",
            GuildFeature.GuildMessageEmojis => "GUILD_MESSAGE_EMOJIS",
            GuildFeature.GuildMessageComponents => "GUILD_MESSAGE_COMPONENTS",
            GuildFeature.GuildMessageInteractions => "GUILD_MESSAGE_INTERACTIONS",
            GuildFeature.GuildMessageThreads => "GUILD_MESSAGE_THREADS",
            GuildFeature.GuildMessageForumPosts => "GUILD_MESSAGE_FORUM_POSTS",
            GuildFeature.GuildMessageMediaChannels => "GUILD_MESSAGE_MEDIA_CHANNELS",
            _ => feature.ToString().ToUpperInvariant()
        };
    }
    
    /// <summary>
    /// Converts a Discord API string to a GuildFeature enum.
    /// </summary>
    /// <param name="featureString">The Discord API string.</param>
    /// <returns>The GuildFeature enum value, or null if not found.</returns>
    public static GuildFeature? FromApiString(string featureString)
    {
        return featureString?.ToUpperInvariant() switch
        {
            "ANIMATED_ICON" => GuildFeature.AnimatedIcon,
            "BANNER" => GuildFeature.Banner,
            "COMMUNITY" => GuildFeature.Community,
            "TICKETED_EVENTS" => GuildFeature.TicketedEvents,
            "GUILD_DIRECTORY" => GuildFeature.GuildDirectory,
            "PARTNERED" => GuildFeature.Partnered,
            "ROLE_SUBSCRIPTIONS" => GuildFeature.RoleSubscriptions,
            "CREATOR_MONETIZABLE" => GuildFeature.CreatorMonetizable,
            "CREATOR_STORE_PAGE" => GuildFeature.CreatorStorePage,
            "SOUNDBOARD" => GuildFeature.Soundboard,
            "VANITY_URL" => GuildFeature.VanityUrl,
            "VERIFIED" => GuildFeature.Verified,
            "VIP_REGIONS" => GuildFeature.VIPRegions,
            "THREADS" => GuildFeature.Threads,
            "STAGE_CHANNELS" => GuildFeature.StageChannels,
            "PREMIUM" => GuildFeature.Premium,
            "NEWS" => GuildFeature.News,
            "DISCOVERABLE" => GuildFeature.Discoverable,
            "INVITE_SPLASH" => GuildFeature.InviteSplash,
            "MEMBER_VERIFICATION_GATE" => GuildFeature.MemberVerificationGate,
            "PREVIEW_ENABLED" => GuildFeature.PreviewEnabled,
            "ANIMATED_BANNER" => GuildFeature.AnimatedBanner,
            "AUTO_MODERATION" => GuildFeature.AutoModeration,
            "RAID_ALERTS_ENABLED" => GuildFeature.RaidAlertsEnabled,
            "APPLICATION_COMMANDS" => GuildFeature.ApplicationCommands,
            "EMBEDDED_ACTIVITIES" => GuildFeature.EmbeddedActivities,
            "GUILD_WEBHOOKS" => GuildFeature.GuildWebhooks,
            "GUILD_MEMBER_VERIFICATION" => GuildFeature.GuildMemberVerification,
            "GUILD_ONBOARDING" => GuildFeature.GuildOnboarding,
            "GUILD_SCHEDULED_EVENTS" => GuildFeature.GuildScheduledEvents,
            "GUILD_STICKERS" => GuildFeature.GuildStickers,
            "GUILD_WELCOME_SCREEN" => GuildFeature.GuildWelcomeScreen,
            "GUILD_WIDGET" => GuildFeature.GuildWidget,
            "GUILD_INSIGHTS" => GuildFeature.GuildInsights,
            "GUILD_INVITES" => GuildFeature.GuildInvites,
            "GUILD_DISCOVERY" => GuildFeature.GuildDiscovery,
            "GUILD_DIRECTORY_CHANNELS" => GuildFeature.GuildDirectoryChannels,
            "GUILD_ROLES" => GuildFeature.GuildRoles,
            "GUILD_EMOJIS" => GuildFeature.GuildEmojis,
            "GUILD_VOICE_STATES" => GuildFeature.GuildVoiceStates,
            "GUILD_PRESENCES" => GuildFeature.GuildPresences,
            "GUILD_MESSAGES" => GuildFeature.GuildMessages,
            "GUILD_MESSAGE_REACTIONS" => GuildFeature.GuildMessageReactions,
            "GUILD_MESSAGE_TYPING" => GuildFeature.GuildMessageTyping,
            "GUILD_MESSAGE_MENTIONS" => GuildFeature.GuildMessageMentions,
            "GUILD_MESSAGE_EMBEDS" => GuildFeature.GuildMessageEmbeds,
            "GUILD_MESSAGE_ATTACHMENTS" => GuildFeature.GuildMessageAttachments,
            "GUILD_MESSAGE_CONTENT" => GuildFeature.GuildMessageContent,
            "GUILD_MESSAGE_HISTORY" => GuildFeature.GuildMessageHistory,
            "GUILD_MESSAGE_PINS" => GuildFeature.GuildMessagePins,
            "GUILD_MESSAGE_POLLS" => GuildFeature.GuildMessagePolls,
            "GUILD_MESSAGE_STICKERS" => GuildFeature.GuildMessageStickers,
            "GUILD_MESSAGE_EMOJIS" => GuildFeature.GuildMessageEmojis,
            "GUILD_MESSAGE_COMPONENTS" => GuildFeature.GuildMessageComponents,
            "GUILD_MESSAGE_INTERACTIONS" => GuildFeature.GuildMessageInteractions,
            "GUILD_MESSAGE_THREADS" => GuildFeature.GuildMessageThreads,
            "GUILD_MESSAGE_FORUM_POSTS" => GuildFeature.GuildMessageForumPosts,
            "GUILD_MESSAGE_MEDIA_CHANNELS" => GuildFeature.GuildMessageMediaChannels,
            _ => null
        };
    }
}
