#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord guild (server).
/// </summary>
public class Guild : DiscordEntity
{
    /// <summary>
    /// Guild name (2-100 characters, excluding trailing and leading whitespace).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    private string? _validatedName;

    /// <summary>
    /// Gets or sets the validated guild name (2-100 characters, excluding trailing and leading whitespace).
    /// </summary>
    public string? ValidatedName
    {
        get => _validatedName;
        set
        {
            if (value != null)
            {
                var trimmed = value.Trim();
                if (trimmed.Length < 2 || trimmed.Length > 100)
                    throw new ArgumentException("Guild name must be between 2 and 100 characters.", nameof(value));
            }
            _validatedName = value;
        }
    }
    
    /// <summary>
    /// Icon hash.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    /// <summary>
    /// Splash hash.
    /// </summary>
    [JsonPropertyName("splash")]
    public string? Splash { get; set; }
    
    /// <summary>
    /// Discovery splash hash.
    /// </summary>
    [JsonPropertyName("discovery_splash")]
    public string? DiscoverySplash { get; set; }
    
    /// <summary>
    /// Id of owner.
    /// </summary>
    [JsonPropertyName("owner_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong OwnerId { get; set; }
    
    /// <summary>
    /// Total permissions for the user in the guild (excludes overwrites).
    /// </summary>
    [JsonPropertyName("permissions")]
    [JsonConverter(typeof(NullablePermissionsJsonConverter))]
    public Permissions? Permissions { get; set; }
    
    /// <summary>
    /// Voice region id for the guild.
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    
    /// <summary>
    /// Id of afk channel.
    /// </summary>
    [JsonPropertyName("afk_channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? AfkChannelId { get; set; }
    
    /// <summary>
    /// Afk timeout in seconds.
    /// </summary>
    [JsonPropertyName("afk_timeout")]
    public int AfkTimeout { get; set; }
    
    /// <summary>
    /// True if the server widget is enabled.
    /// </summary>
    [JsonPropertyName("widget_enabled")]
    public bool? WidgetEnabled { get; set; }
    
    /// <summary>
    /// The channel id that the widget will generate an invite to.
    /// </summary>
    [JsonPropertyName("widget_channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? WidgetChannelId { get; set; }
    
    /// <summary>
    /// Verification level required for the guild.
    /// </summary>
    [JsonPropertyName("verification_level")]
    public VerificationLevel VerificationLevel { get; set; }
    
    /// <summary>
    /// Default message notifications level.
    /// </summary>
    [JsonPropertyName("default_message_notifications")]
    public DefaultMessageNotificationLevel DefaultMessageNotifications { get; set; }
    
    /// <summary>
    /// Explicit content filter level.
    /// </summary>
    [JsonPropertyName("explicit_content_filter")]
    public ExplicitContentFilterLevel ExplicitContentFilter { get; set; }
    
    /// <summary>
    /// Roles in the guild.
    /// </summary>
    [JsonPropertyName("roles")]
    public List<Role> Roles { get; set; } = new();
    
    /// <summary>
    /// Custom guild emojis.
    /// </summary>
    [JsonPropertyName("emojis")]
    public List<Emoji> Emojis { get; set; } = new();
    
    /// <summary>
    /// Enabled guild features.
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
    
    /// <summary>
    /// Required MFA level for the guild.
    /// </summary>
    [JsonPropertyName("mfa_level")]
    public int MfaLevel { get; set; }
    
    /// <summary>
    /// Application id of the guild creator if it is bot-created.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
    
    /// <summary>
    /// The id of the channel where guild notices such as welcome messages and boost events are posted.
    /// </summary>
    [JsonPropertyName("system_channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? SystemChannelId { get; set; }
    
    /// <summary>
    /// System channel flags.
    /// </summary>
    [JsonPropertyName("system_channel_flags")]
    public SystemChannelFlags SystemChannelFlags { get; set; }
    
    /// <summary>
    /// The id of the channel where Community guilds can display rules and/or guidelines.
    /// </summary>
    [JsonPropertyName("rules_channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? RulesChannelId { get; set; }
    
    /// <summary>
    /// The maximum number of presences for the guild.
    /// </summary>
    [JsonPropertyName("max_presences")]
    public int? MaxPresences { get; set; }
    
    /// <summary>
    /// The maximum number of members for the guild.
    /// </summary>
    [JsonPropertyName("max_members")]
    public int? MaxMembers { get; set; }
    
    /// <summary>
    /// The vanity url code for the guild.
    /// </summary>
    [JsonPropertyName("vanity_url_code")]
    public string? VanityUrlCode { get; set; }
    
    /// <summary>
    /// The description of a guild.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Banner hash.
    /// </summary>
    [JsonPropertyName("banner")]
    public string? Banner { get; set; }
    
    /// <summary>
    /// Premium tier (Server Boost level).
    /// </summary>
    [JsonPropertyName("premium_tier")]
    public int PremiumTier { get; set; }
    
    /// <summary>
    /// The number of boosts this guild currently has.
    /// </summary>
    [JsonPropertyName("premium_subscription_count")]
    public int? PremiumSubscriptionCount { get; set; }
    
    /// <summary>
    /// The preferred locale of a Community guild.
    /// </summary>
    [JsonPropertyName("preferred_locale")]
    public string PreferredLocale { get; set; } = "en-US";
    
    /// <summary>
    /// The id of the channel where admins and moderators of Community guilds receive notices from Discord.
    /// </summary>
    [JsonPropertyName("public_updates_channel_id")]    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]    public ulong? PublicUpdatesChannelId { get; set; }
    
    /// <summary>
    /// The maximum amount of users in a video channel.
    /// </summary>
    [JsonPropertyName("max_video_channel_users")]
    public int? MaxVideoChannelUsers { get; set; }
    
    /// <summary>
    /// Approximate number of members in this guild.
    /// </summary>
    [JsonPropertyName("approximate_member_count")]
    public int? ApproximateMemberCount { get; set; }
    
    /// <summary>
    /// Approximate number of non-offline members in this guild.
    /// </summary>
    [JsonPropertyName("approximate_presence_count")]
    public int? ApproximatePresenceCount { get; set; }
    
    /// <summary>
    /// Channels in the guild.
    /// </summary>
    [JsonPropertyName("channels")]
    public List<Channel>? Channels { get; set; }
    
    /// <summary>
    /// Custom guild stickers.
    /// </summary>
    [JsonPropertyName("stickers")]
    public List<Sticker>? Stickers { get; set; }
    
    /// <summary>
    /// Members in the guild.
    /// </summary>
    [JsonPropertyName("members")]
    public List<GuildMember>? Members { get; set; }
    
    /// <summary>
    /// Whether this guild is unavailable due to an outage.
    /// </summary>
    [JsonPropertyName("unavailable")]
    public bool? Unavailable { get; set; }

    /// <summary>
    /// The id of the channel where admins and moderators of Community guilds receive safety alerts.
    /// </summary>
    [JsonPropertyName("safety_alerts_channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? SafetyAlertsChannelId { get; set; }

    /// <summary>
    /// Active incidents and raid-detection data for the guild.
    /// </summary>
    [JsonPropertyName("incidents_data")]
    public GuildIncidentsData? IncidentsData { get; set; }

    /// <summary>
    /// Hash of the guild's home header banner (server guide feature).
    /// </summary>
    [JsonPropertyName("home_header")]
    public string? HomeHeader { get; set; }

    /// <summary>
    /// ID of the most recent onboarding question shown to new members.
    /// </summary>
    [JsonPropertyName("latest_onboarding_question_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LatestOnboardingQuestionId { get; set; }
}

/// <summary>
/// Represents a guild member.
/// </summary>
public class GuildMember
{
    /// <summary>
    /// The user this guild member represents.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    /// <summary>
    /// This user's guild nickname.
    /// </summary>
    [JsonPropertyName("nick")]
    public string? Nick { get; set; }
    
    /// <summary>
    /// The member's guild avatar hash.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    /// <summary>
    /// Array of role object ids.
    /// </summary>
    [JsonPropertyName("roles")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> Roles { get; set; } = new();
    
    /// <summary>
    /// When the user joined the guild.
    /// </summary>
    [JsonPropertyName("joined_at")]
    public DateTimeOffset? JoinedAt { get; set; }
    
    /// <summary>
    /// When the user started boosting the guild.
    /// </summary>
    [JsonPropertyName("premium_since")]
    public DateTimeOffset? PremiumSince { get; set; }
    
    /// <summary>
    /// Whether the user is deafened in voice channels.
    /// </summary>
    [JsonPropertyName("deaf")]
    public bool Deaf { get; set; }
    
    /// <summary>
    /// Whether the user is muted in voice channels.
    /// </summary>
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
    
    /// <summary>
    /// Guild member flags.
    /// </summary>
    [JsonPropertyName("flags")]
    public int Flags { get; set; }
    
    /// <summary>
    /// Whether the user has not yet passed the guild's Membership Screening requirements.
    /// </summary>
    [JsonPropertyName("pending")]
    public bool? Pending { get; set; }
    
    /// <summary>
    /// Total permissions of the member in the channel.
    /// </summary>
    [JsonPropertyName("permissions")]
    [JsonConverter(typeof(NullablePermissionsJsonConverter))]
    public Permissions? Permissions { get; set; }
    
    /// <summary>
    /// When the user's timeout will expire and the user will be able to communicate in the guild again.
    /// </summary>
    [JsonPropertyName("communication_disabled_until")]
    public DateTimeOffset? CommunicationDisabledUntil { get; set; }

    /// <summary>
    /// Timestamp until which the member will have unusual DM activity restricted.
    /// </summary>
    [JsonPropertyName("unusual_dm_activity_until")]
    public DateTimeOffset? UnusualDmActivityUntil { get; set; }

    /// <summary>
    /// Data for the member's avatar decoration (the SKU and asset hash).
    /// </summary>
    [JsonPropertyName("avatar_decoration_data")]
    public AvatarDecorationData? AvatarDecorationData { get; set; }
}

/// <summary>
/// Active incidents detected in a guild (raid mode, DM spam lockdown, etc.).
/// </summary>
public class GuildIncidentsData
{
    /// <summary>Timestamp until when DMs from non-friends are disabled.</summary>
    [JsonPropertyName("dms_disabled_until")]
    public DateTimeOffset? DmsDisabledUntil { get; set; }

    /// <summary>Timestamp until when invites are disabled.</summary>
    [JsonPropertyName("invites_disabled_until")]
    public DateTimeOffset? InvitesDisabledUntil { get; set; }

    /// <summary>Timestamp when a raid was detected.</summary>
    [JsonPropertyName("raid_detected_at")]
    public DateTimeOffset? RaidDetectedAt { get; set; }

    /// <summary>Timestamp until when DMs to unknown users are disabled.</summary>
    [JsonPropertyName("dm_spam_detected_at")]
    public DateTimeOffset? DmSpamDetectedAt { get; set; }
}

/// <summary>
/// Decoration data attached to a user's or member's avatar (cosmetic SKU item).
/// </summary>
public class AvatarDecorationData
{
    /// <summary>The hash of the avatar decoration asset.</summary>
    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    /// <summary>The id of the avatar decoration's SKU.</summary>
    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }
}
