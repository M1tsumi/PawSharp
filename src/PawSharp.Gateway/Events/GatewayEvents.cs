#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Entities;
using PawSharp.Core.Serialization;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Base class for all gateway events.
/// </summary>
public abstract class GatewayEvent
{
    /// <summary>
    /// The raw JSON payload from Discord.
    /// </summary>
    [JsonIgnore]
    public string? RawJson { get; set; }
}

/// <summary>
/// READY event - contains initial state information.
/// </summary>
public class ReadyEvent : GatewayEvent
{
    [JsonPropertyName("v")]
    public int Version { get; set; }
    
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    [JsonPropertyName("guilds")]
    public List<Guild> Guilds { get; set; } = new();
    
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonPropertyName("resume_gateway_url")]
    public string ResumeGatewayUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("shard")]
    public int[]? Shard { get; set; }
    
    [JsonPropertyName("application")]
    public PartialApplication? Application { get; set; }
}

/// <summary>
/// MESSAGE_CREATE event.
/// </summary>
public class MessageCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("author")]
    public User Author { get; set; } = null!;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    
    [JsonPropertyName("edited_timestamp")]
    public DateTimeOffset? EditedTimestamp { get; set; }
    
    [JsonPropertyName("tts")]
    public bool Tts { get; set; }
    
    [JsonPropertyName("mention_everyone")]
    public bool MentionEveryone { get; set; }
    
    [JsonPropertyName("mentions")]
    public List<User> Mentions { get; set; } = new();
    
    [JsonPropertyName("mention_roles")]
    public List<ulong> MentionRoles { get; set; } = new();
    
    [JsonPropertyName("attachments")]
    public List<Attachment> Attachments { get; set; } = new();
    
    [JsonPropertyName("embeds")]
    public List<Embed> Embeds { get; set; } = new();
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }

    [JsonPropertyName("poll")]
    public Poll? Poll { get; set; }
    
    public Message ToMessage()
    {
        return new Message
        {
            Id = Id,
            ChannelId = ChannelId,
            Author = Author,
            Content = Content,
            Timestamp = Timestamp,
            EditedTimestamp = EditedTimestamp,
            Tts = Tts,
            MentionEveryone = MentionEveryone,
            Mentions = Mentions,
            MentionRoles = MentionRoles,
            Attachments = Attachments,
            Embeds = Embeds,
            GuildId = GuildId
        };
    }
}

/// <summary>
/// MESSAGE_UPDATE event.
/// </summary>
public class MessageUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("edited_timestamp")]
    public DateTimeOffset? EditedTimestamp { get; set; }
    
    [JsonPropertyName("embeds")]
    public List<Embed>? Embeds { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("poll")]
    public Poll? Poll { get; set; }
}

/// <summary>
/// MESSAGE_DELETE event.
/// </summary>
public class MessageDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
}

/// <summary>
/// GUILD_CREATE event.
/// </summary>
public class GuildCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("splash")]
    public string? Splash { get; set; }

    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("vanity_url_code")]
    public string? VanityUrlCode { get; set; }

    [JsonPropertyName("premium_tier")]
    public int PremiumTier { get; set; }

    [JsonPropertyName("premium_subscription_count")]
    public int? PremiumSubscriptionCount { get; set; }

    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }

    [JsonPropertyName("approximate_member_count")]
    public int? ApproximateMemberCount { get; set; }

    [JsonPropertyName("approximate_presence_count")]
    public int? ApproximatePresenceCount { get; set; }

    [JsonPropertyName("preferred_locale")]
    public string PreferredLocale { get; set; } = "en-US";
    
    [JsonPropertyName("owner_id")]
    public ulong OwnerId { get; set; }
    
    [JsonPropertyName("roles")]
    public List<Role> Roles { get; set; } = new();
    
    [JsonPropertyName("emojis")]
    public List<Emoji> Emojis { get; set; } = new();

    [JsonPropertyName("stickers")]
    public List<Sticker>? Stickers { get; set; }
    
    [JsonPropertyName("channels")]
    public List<Channel> Channels { get; set; } = new();
    
    [JsonPropertyName("members")]
    public List<GuildMember> Members { get; set; } = new();
    
    [JsonPropertyName("unavailable")]
    public bool? Unavailable { get; set; }
    
    public Guild ToGuild()
    {
        return new Guild
        {
            Id = Id,
            Name = Name,
            Icon = Icon,
            Splash = Splash,
            Banner = Banner,
            Description = Description,
            VanityUrlCode = VanityUrlCode,
            PremiumTier = PremiumTier,
            PremiumSubscriptionCount = PremiumSubscriptionCount,
            ApproximateMemberCount = ApproximateMemberCount ?? MemberCount,
            ApproximatePresenceCount = ApproximatePresenceCount,
            PreferredLocale = PreferredLocale,
            OwnerId = OwnerId,
            Roles = Roles,
            Emojis = Emojis,
            Stickers = Stickers,
            Channels = Channels,
            Members = Members,
            Unavailable = Unavailable
        };
    }
}

/// <summary>
/// GUILD_UPDATE event.
/// </summary>
public class GuildUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
    
    [JsonPropertyName("owner_id")]
    public ulong OwnerId { get; set; }
}

/// <summary>
/// GUILD_DELETE event.
/// </summary>
public class GuildDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("unavailable")]
    public bool? Unavailable { get; set; }
}

/// <summary>
/// GUILD_EMOJIS_UPDATE event.
/// </summary>
public class GuildEmojisUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("emojis")]
    public List<Emoji> Emojis { get; set; } = new();
}

/// <summary>
/// CHANNEL_CREATE event.
/// </summary>
public class ChannelCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }

    [JsonPropertyName("bitrate")]
    public int? Bitrate { get; set; }

    [JsonPropertyName("user_limit")]
    public int? UserLimit { get; set; }

    [JsonPropertyName("rate_limit_per_user")]
    public int? RateLimitPerUser { get; set; }

    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ParentId { get; set; }

    [JsonPropertyName("last_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LastMessageId { get; set; }

    [JsonPropertyName("rtc_region")]
    public string? RtcRegion { get; set; }

    [JsonPropertyName("last_pin_timestamp")]
    public DateTimeOffset? LastPinTimestamp { get; set; }
    
    public Channel ToChannel()
    {
        return new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name,
            Position = Position,
            Topic = Topic,
            Nsfw = Nsfw,
            Bitrate = Bitrate,
            UserLimit = UserLimit,
            RateLimitPerUser = RateLimitPerUser,
            ParentId = ParentId,
            LastMessageId = LastMessageId,
            RtcRegion = RtcRegion,
            LastPinTimestamp = LastPinTimestamp
        };
    }
}

/// <summary>
/// CHANNEL_UPDATE event.
/// </summary>
public class ChannelUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public int? Position { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }

    [JsonPropertyName("bitrate")]
    public int? Bitrate { get; set; }

    [JsonPropertyName("user_limit")]
    public int? UserLimit { get; set; }

    [JsonPropertyName("rate_limit_per_user")]
    public int? RateLimitPerUser { get; set; }

    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ParentId { get; set; }

    [JsonPropertyName("last_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LastMessageId { get; set; }

    [JsonPropertyName("rtc_region")]
    public string? RtcRegion { get; set; }

    [JsonPropertyName("last_pin_timestamp")]
    public DateTimeOffset? LastPinTimestamp { get; set; }
    
    public Channel ToChannel()
    {
        return new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name,
            Position = Position,
            Topic = Topic,
            Nsfw = Nsfw,
            Bitrate = Bitrate,
            UserLimit = UserLimit,
            RateLimitPerUser = RateLimitPerUser,
            ParentId = ParentId,
            LastMessageId = LastMessageId,
            RtcRegion = RtcRegion,
            LastPinTimestamp = LastPinTimestamp
        };
    }
}

/// <summary>
/// CHANNEL_DELETE event.
/// </summary>
public class ChannelDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    public Channel ToChannel()
    {
        return new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name
        };
    }
}

/// <summary>
/// GUILD_MEMBER_ADD event.
/// </summary>
public class GuildMemberAddEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    [JsonPropertyName("nick")]
    public string? Nick { get; set; }
    
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    [JsonPropertyName("roles")]
    public List<ulong> Roles { get; set; } = new();
    
    [JsonPropertyName("joined_at")]
    public DateTimeOffset JoinedAt { get; set; }
    
    [JsonPropertyName("premium_since")]
    public DateTimeOffset? PremiumSince { get; set; }
    
    [JsonPropertyName("deaf")]
    public bool Deaf { get; set; }
    
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
    
    [JsonPropertyName("pending")]
    public bool? Pending { get; set; }
    
    public GuildMember ToGuildMember()
    {
        return new GuildMember
        {
            User = User,
            Nick = Nick,
            Avatar = Avatar,
            Roles = Roles,
            JoinedAt = JoinedAt,
            PremiumSince = PremiumSince,
            Deaf = Deaf,
            Mute = Mute,
            Pending = Pending
        };
    }
}

/// <summary>
/// GUILD_MEMBER_UPDATE event.
/// </summary>
public class GuildMemberUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("roles")]
    public List<ulong> Roles { get; set; } = new();
    
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    [JsonPropertyName("nick")]
    public string? Nick { get; set; }
    
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    [JsonPropertyName("joined_at")]
    public DateTimeOffset? JoinedAt { get; set; }
    
    [JsonPropertyName("premium_since")]
    public DateTimeOffset? PremiumSince { get; set; }
}

/// <summary>
/// GUILD_MEMBER_REMOVE event.
/// </summary>
public class GuildMemberRemoveEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
}

/// <summary>
/// INTERACTION_CREATE event.
/// </summary>
public class InteractionCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("data")]
    public InteractionData? Data { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
    
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("version")]
    public int Version { get; set; }
    
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
    
    [JsonPropertyName("app_permissions")]
    public string? AppPermissions { get; set; }
    
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
    
    [JsonPropertyName("guild_locale")]
    public string? GuildLocale { get; set; }
}

/// <summary>
/// Interaction data for slash commands and components.
/// </summary>
public class InteractionData
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("resolved")]
    public InteractionResolvedData? Resolved { get; set; }
    
    [JsonPropertyName("options")]
    public List<ApplicationCommandInteractionDataOption>? Options { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("target_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? TargetId { get; set; }
    
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }
    
    [JsonPropertyName("component_type")]
    public int? ComponentType { get; set; }
    
    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }

    /// <summary>
    /// Modal and component submit data. Populated for modal submissions (type=5) and
    /// component interactions that include sub-components.
    /// </summary>
    [JsonPropertyName("components")]
    public List<MessageComponent>? Components { get; set; }
}

/// <summary>
/// Resolved data for interactions.
/// Keys are Discord snowflake IDs (<see cref="ulong"/>).
/// Deserialised via <see cref="SnowflakeDictionaryJsonConverterFactory"/> which
/// translates Discord's string-keyed JSON objects transparently.
/// </summary>
public class InteractionResolvedData
{
    [JsonPropertyName("users")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, User>? Users { get; set; }

    [JsonPropertyName("members")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, GuildMember>? Members { get; set; }

    [JsonPropertyName("roles")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, Role>? Roles { get; set; }

    [JsonPropertyName("channels")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, Channel>? Channels { get; set; }

    [JsonPropertyName("messages")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, Message>? Messages { get; set; }

    [JsonPropertyName("attachments")]
    [JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]
    public Dictionary<ulong, Attachment>? Attachments { get; set; }
}

/// <summary>
/// Option data for application command interactions.
/// </summary>
public class ApplicationCommandInteractionDataOption
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
    
    [JsonPropertyName("options")]
    public List<ApplicationCommandInteractionDataOption>? Options { get; set; }
    
    [JsonPropertyName("focused")]
    public bool? Focused { get; set; }
}

/// <summary>
/// TYPING_START event.
/// </summary>
public class TypingStartEvent : GatewayEvent
{
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("user_id")]
    public ulong UserId { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
}

/// <summary>
/// MESSAGE_REACTION_ADD event.
/// </summary>
public class MessageReactionAddEvent : GatewayEvent
{
    [JsonPropertyName("user_id")]
    public ulong UserId { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
    
    [JsonPropertyName("emoji")]
    public Emoji Emoji { get; set; } = null!;
}

/// <summary>
/// MESSAGE_REACTION_REMOVE event.
/// </summary>
public class MessageReactionRemoveEvent : GatewayEvent
{
    [JsonPropertyName("user_id")]
    public ulong UserId { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("emoji")]
    public Emoji Emoji { get; set; } = null!;
}

/// <summary>
/// MESSAGE_REACTION_REMOVE_ALL event.
/// </summary>
public class MessageReactionRemoveAllEvent : GatewayEvent
{
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("message_id")]
    public ulong MessageId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
}

/// <summary>
/// PRESENCE_UPDATE event.
/// </summary>
public class PresenceUpdateEvent : GatewayEvent
{
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
    
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("activities")]
    public List<Activity> Activities { get; set; } = new();
    
    [JsonPropertyName("client_status")]
    public ClientStatus ClientStatus { get; set; } = null!;
}

/// <summary>
/// CHANNEL_PINS_UPDATE event.
/// </summary>
public class ChannelPinsUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong ChannelId { get; set; }
    
    [JsonPropertyName("last_pin_timestamp")]
    public DateTimeOffset? LastPinTimestamp { get; set; }
}

/// <summary>
/// GUILD_BAN_ADD event.
/// </summary>
public class GuildBanAddEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
}

/// <summary>
/// GUILD_BAN_REMOVE event.
/// </summary>
public class GuildBanRemoveEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("user")]
    public User User { get; set; } = null!;
}

/// <summary>
/// VOICE_STATE_UPDATE event.
/// </summary>
public class VoiceStateUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    public ulong? ChannelId { get; set; }
    
    [JsonPropertyName("user_id")]
    public ulong UserId { get; set; }
    
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
    
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;
    
    [JsonPropertyName("deaf")]
    public bool Deaf { get; set; }
    
    [JsonPropertyName("mute")]
    public bool Mute { get; set; }
    
    [JsonPropertyName("self_deaf")]
    public bool SelfDeaf { get; set; }
    
    [JsonPropertyName("self_mute")]
    public bool SelfMute { get; set; }
    
    [JsonPropertyName("self_stream")]
    public bool? SelfStream { get; set; }
    
    [JsonPropertyName("self_video")]
    public bool SelfVideo { get; set; }
    
    [JsonPropertyName("suppress")]
    public bool Suppress { get; set; }
    
    [JsonPropertyName("request_to_speak_timestamp")]
    public DateTimeOffset? RequestToSpeakTimestamp { get; set; }
}

/// <summary>
/// VOICE_SERVER_UPDATE event.
/// </summary>
public class VoiceServerUpdateEvent : GatewayEvent
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("guild_id")]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;
}

/// <summary>
/// Represents an activity.
/// </summary>
public class Activity
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
    
    [JsonPropertyName("timestamps")]
    public ActivityTimestamps? Timestamps { get; set; }
    
    [JsonPropertyName("application_id")]
    public ulong? ApplicationId { get; set; }
    
    [JsonPropertyName("details")]
    public string? Details { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; }
    
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }
    
    [JsonPropertyName("party")]
    public ActivityParty? Party { get; set; }
    
    [JsonPropertyName("assets")]
    public ActivityAssets? Assets { get; set; }
    
    [JsonPropertyName("secrets")]
    public ActivitySecrets? Secrets { get; set; }
    
    [JsonPropertyName("instance")]
    public bool? Instance { get; set; }
    
    [JsonPropertyName("flags")]
    public int? Flags { get; set; }
    
    [JsonPropertyName("buttons")]
    public List<ActivityButton>? Buttons { get; set; }
}

/// <summary>
/// Activity timestamps.
/// </summary>
public class ActivityTimestamps
{
    [JsonPropertyName("start")]
    public long? Start { get; set; }
    
    [JsonPropertyName("end")]
    public long? End { get; set; }
}

/// <summary>
/// Activity party.
/// </summary>
public class ActivityParty
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("size")]
    public List<int>? Size { get; set; }
}

/// <summary>
/// Activity assets.
/// </summary>
public class ActivityAssets
{
    [JsonPropertyName("large_image")]
    public string? LargeImage { get; set; }
    
    [JsonPropertyName("large_text")]
    public string? LargeText { get; set; }
    
    [JsonPropertyName("small_image")]
    public string? SmallImage { get; set; }
    
    [JsonPropertyName("small_text")]
    public string? SmallText { get; set; }
}

/// <summary>
/// Activity secrets.
/// </summary>
public class ActivitySecrets
{
    [JsonPropertyName("join")]
    public string? Join { get; set; }
    
    [JsonPropertyName("spectate")]
    public string? Spectate { get; set; }
    
    [JsonPropertyName("match")]
    public string? Match { get; set; }
}

/// <summary>
/// Activity button.
/// </summary>
public class ActivityButton
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Client status.
/// </summary>
public class ClientStatus
{
    [JsonPropertyName("desktop")]
    public string? Desktop { get; set; }
    
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }
    
    [JsonPropertyName("web")]
    public string? Web { get; set; }
}

/// <summary>
/// THREAD_CREATE event.
/// </summary>
public class ThreadCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ParentId { get; set; }
    
    [JsonPropertyName("owner_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong OwnerId { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }
    
    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }
    
    [JsonPropertyName("thread_metadata")]
    public ThreadMetadata ThreadMetadata { get; set; } = null!;
    
    [JsonPropertyName("member")]
    public ThreadMember? Member { get; set; }
    
    [JsonPropertyName("last_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LastMessageId { get; set; }
    
    [JsonPropertyName("rate_limit_per_user")]
    public int? RateLimitPerUser { get; set; }
    
    [JsonPropertyName("flags")]
    public int? Flags { get; set; }
}

/// <summary>
/// THREAD_UPDATE event.
/// </summary>
public class ThreadUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ParentId { get; set; }
    
    [JsonPropertyName("owner_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong OwnerId { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }
    
    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }
    
    [JsonPropertyName("thread_metadata")]
    public ThreadMetadata ThreadMetadata { get; set; } = null!;
    
    [JsonPropertyName("last_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? LastMessageId { get; set; }
    
    [JsonPropertyName("rate_limit_per_user")]
    public int? RateLimitPerUser { get; set; }
    
    [JsonPropertyName("flags")]
    public int? Flags { get; set; }
}

/// <summary>
/// THREAD_DELETE event.
/// </summary>
public class ThreadDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("parent_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ParentId { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

/// <summary>
/// THREAD_LIST_SYNC event.
/// </summary>
public class ThreadListSyncEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("channel_ids")]
    public List<ulong>? ChannelIds { get; set; }
    
    [JsonPropertyName("threads")]
    public List<Channel> Threads { get; set; } = new();
    
    [JsonPropertyName("members")]
    public List<ThreadMember> Members { get; set; } = new();
}

/// <summary>
/// THREAD_MEMBER_UPDATE event.
/// </summary>
public class ThreadMemberUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }
    
    [JsonPropertyName("join_timestamp")]
    public DateTimeOffset JoinTimestamp { get; set; }
    
    [JsonPropertyName("flags")]
    public int Flags { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// THREAD_MEMBERS_UPDATE event.
/// </summary>
public class ThreadMembersUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    [JsonPropertyName("member_count")]
    public int MemberCount { get; set; }
    
    [JsonPropertyName("added_members")]
    public List<ThreadMember>? AddedMembers { get; set; }
    
    [JsonPropertyName("removed_member_ids")]
    public List<ulong>? RemovedMemberIds { get; set; }
}

/// <summary>
/// Base class for shard-related events.
/// </summary>
public abstract class ShardEvent : GatewayEvent
{
    /// <summary>
    /// The shard ID that triggered this event.
    /// </summary>
    public int ShardId { get; set; }
}

/// <summary>
/// Fired when a shard connects.
/// </summary>
public class ShardConnectedEvent : ShardEvent
{
}

/// <summary>
/// Fired when a shard disconnects.
/// </summary>
public class ShardDisconnectedEvent : ShardEvent
{
}

/// <summary>
/// Fired when a shard fails.
/// </summary>
public class ShardFailedEvent : ShardEvent
{
}

// ── New events added in alpha11 ───────────────────────────────────────────────

/// <summary>
/// GUILD_ROLE_CREATE event — fired when a role is created in a guild.
/// </summary>
public class GuildRoleCreateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("role")]
    public Role Role { get; set; } = null!;
}

/// <summary>
/// GUILD_ROLE_UPDATE event — fired when a role is updated in a guild.
/// </summary>
public class GuildRoleUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("role")]
    public Role Role { get; set; } = null!;
}

/// <summary>
/// GUILD_ROLE_DELETE event — fired when a role is deleted from a guild.
/// </summary>
public class GuildRoleDeleteEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("role_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong RoleId { get; set; }
}

/// <summary>
/// GUILD_MEMBERS_CHUNK event — sent in response to opcode 8 (Request Guild Members).
/// </summary>
public class GuildMembersChunkEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("members")]
    public List<GuildMember> Members { get; set; } = new();

    [JsonPropertyName("chunk_index")]
    public int ChunkIndex { get; set; }

    [JsonPropertyName("chunk_count")]
    public int ChunkCount { get; set; }

    [JsonPropertyName("not_found")]
    public List<ulong>? NotFound { get; set; }

    /// <summary>Presences of the returned members. Only present when presences=true was sent on the op8 payload.</summary>
    [JsonPropertyName("presences")]
    public List<PresenceUpdateEvent>? Presences { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }
}

/// <summary>
/// GUILD_STICKERS_UPDATE event — fired when a guild's sticker list changes.
/// </summary>
public class GuildStickersUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("stickers")]
    public List<Sticker> Stickers { get; set; } = new();
}

/// <summary>
/// MESSAGE_REACTION_REMOVE_EMOJI event — fired when all reactions for a specific emoji are removed.
/// </summary>
public class MessageReactionRemoveEmojiEvent : GatewayEvent
{
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong MessageId { get; set; }

    [JsonPropertyName("emoji")]
    public Emoji Emoji { get; set; } = null!;
}

/// <summary>
/// GUILD_INTEGRATIONS_UPDATE event — fired when a guild's integrations are updated.
/// </summary>
public class GuildIntegrationsUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// USER_UPDATE event — fired when the bot user's own settings change.
/// </summary>
public class UserUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; } = string.Empty;

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("bot")]
    public bool? Bot { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("verified")]
    public bool? Verified { get; set; }
}

// ── New events added in alpha12 ───────────────────────────────────────────────

/// <summary>
/// GUILD_SCHEDULED_EVENT_CREATE event.
/// </summary>
public class GuildScheduledEventCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("creator")]
    public User? Creator { get; set; }
}

/// <summary>
/// GUILD_SCHEDULED_EVENT_UPDATE event.
/// </summary>
public class GuildScheduledEventUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

/// <summary>
/// GUILD_SCHEDULED_EVENT_DELETE event.
/// </summary>
public class GuildScheduledEventDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// GUILD_SCHEDULED_EVENT_USER_ADD event — fired when a user subscribes to an event.
/// </summary>
public class GuildScheduledEventUserAddEvent : GatewayEvent
{
    [JsonPropertyName("guild_scheduled_event_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildScheduledEventId { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// GUILD_SCHEDULED_EVENT_USER_REMOVE event — fired when a user unsubscribes from an event.
/// </summary>
public class GuildScheduledEventUserRemoveEvent : GatewayEvent
{
    [JsonPropertyName("guild_scheduled_event_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildScheduledEventId { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// AUTO_MODERATION_RULE_CREATE event.
/// </summary>
public class AutoModerationRuleCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("trigger_type")]
    public int TriggerType { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>
/// AUTO_MODERATION_RULE_UPDATE event.
/// </summary>
public class AutoModerationRuleUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("trigger_type")]
    public int TriggerType { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>
/// AUTO_MODERATION_RULE_DELETE event.
/// </summary>
public class AutoModerationRuleDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// AUTO_MODERATION_ACTION_EXECUTION event — fired when any rule action is executed.
/// </summary>
public class AutoModerationActionExecutionEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("action")]
    public AutoModerationActionObject Action { get; set; } = null!;

    [JsonPropertyName("rule_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong RuleId { get; set; }

    [JsonPropertyName("rule_trigger_type")]
    public int RuleTriggerType { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }

    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? MessageId { get; set; }

    [JsonPropertyName("alert_system_message_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? AlertSystemMessageId { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("matched_keyword")]
    public string? MatchedKeyword { get; set; }

    [JsonPropertyName("matched_content")]
    public string? MatchedContent { get; set; }
}

/// <summary>
/// Embedded action for AUTO_MODERATION_ACTION_EXECUTION.
/// </summary>
public class AutoModerationActionObject
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }
}

/// <summary>
/// STAGE_INSTANCE_CREATE event.
/// </summary>
public class StageInstanceCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("privacy_level")]
    public int PrivacyLevel { get; set; }

    [JsonPropertyName("discoverable_disabled")]
    public bool? DiscoverableDisabled { get; set; }

    [JsonPropertyName("guild_scheduled_event_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildScheduledEventId { get; set; }
}

/// <summary>
/// STAGE_INSTANCE_UPDATE event.
/// </summary>
public class StageInstanceUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("privacy_level")]
    public int PrivacyLevel { get; set; }
}

/// <summary>
/// STAGE_INSTANCE_DELETE event.
/// </summary>
public class StageInstanceDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }
}

/// <summary>
/// GUILD_AUDIT_LOG_ENTRY_CREATE event — fired when an audit log entry is created.
/// </summary>
public class GuildAuditLogEntryCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("action_type")]
    public int ActionType { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? UserId { get; set; }

    [JsonPropertyName("target_id")]
    public string? TargetId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("changes")]
    public List<object>? Changes { get; set; }

    [JsonPropertyName("options")]
    public object? Options { get; set; }
}

/// <summary>
/// ENTITLEMENT_CREATE event — fired when a user subscribes to an SKU.
/// </summary>
public class EntitlementCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? UserId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>
/// ENTITLEMENT_UPDATE event — fired when a user's subscription renews.
/// </summary>
public class EntitlementUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? UserId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("starts_at")]
    public DateTimeOffset? StartsAt { get; set; }

    [JsonPropertyName("ends_at")]
    public DateTimeOffset? EndsAt { get; set; }
}

/// <summary>
/// ENTITLEMENT_DELETE event — fired when a user's entitlement is deleted (not expiry).
/// </summary>
public class EntitlementDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("sku_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SkuId { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? UserId { get; set; }
}

/// <summary>
/// MESSAGE_POLL_VOTE_ADD event — fired when a user votes in a poll.
/// </summary>
public class MessagePollVoteAddEvent : GatewayEvent
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong MessageId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("answer_id")]
    public int AnswerId { get; set; }
}

/// <summary>
/// MESSAGE_POLL_VOTE_REMOVE event — fired when a user retracts a poll vote.
/// </summary>
public class MessagePollVoteRemoveEvent : GatewayEvent
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong MessageId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("answer_id")]
    public int AnswerId { get; set; }
}

/// <summary>
/// GUILD_SOUNDBOARD_SOUND_CREATE event.
/// </summary>
public class GuildSoundboardSoundCreateEvent : GatewayEvent
{
    [JsonPropertyName("sound_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SoundId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("volume")]
    public double Volume { get; set; }

    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }

    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}

/// <summary>
/// GUILD_SOUNDBOARD_SOUND_UPDATE event.
/// </summary>
public class GuildSoundboardSoundUpdateEvent : GatewayEvent
{
    [JsonPropertyName("sound_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SoundId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("volume")]
    public double Volume { get; set; }

    [JsonPropertyName("emoji_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? EmojiId { get; set; }

    [JsonPropertyName("emoji_name")]
    public string? EmojiName { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("available")]
    public bool Available { get; set; }
}

/// <summary>
/// GUILD_SOUNDBOARD_SOUND_DELETE event.
/// </summary>
public class GuildSoundboardSoundDeleteEvent : GatewayEvent
{
    [JsonPropertyName("sound_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong SoundId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// GUILD_SOUNDBOARD_SOUNDS_UPDATE event — fired when multiple soundboard sounds are updated.
/// </summary>
public class GuildSoundboardSoundsUpdateEvent : GatewayEvent
{
    [JsonPropertyName("soundboard_sounds")]
    public List<SoundboardSound> SoundboardSounds { get; set; } = new();

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
}

/// <summary>
/// SUBSCRIPTION_CREATE event — fired when a subscription is created.
/// </summary>
public class SubscriptionCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("sku_ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> SkuIds { get; set; } = new();

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("current_period_start")]
    public DateTimeOffset CurrentPeriodStart { get; set; }

    [JsonPropertyName("current_period_end")]
    public DateTimeOffset CurrentPeriodEnd { get; set; }
}

/// <summary>
/// SUBSCRIPTION_UPDATE event — fired when a subscription is updated.
/// </summary>
public class SubscriptionUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    [JsonPropertyName("sku_ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> SkuIds { get; set; } = new();

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("current_period_start")]
    public DateTimeOffset CurrentPeriodStart { get; set; }

    [JsonPropertyName("current_period_end")]
    public DateTimeOffset CurrentPeriodEnd { get; set; }
}

/// <summary>
/// SUBSCRIPTION_DELETE event — fired when a subscription is deleted.
/// </summary>
public class SubscriptionDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }
}

/// <summary>
/// MESSAGE_DELETE_BULK event — fired when multiple messages are deleted at once.
/// </summary>
public class MessageDeleteBulkEvent : GatewayEvent
{
    [JsonPropertyName("ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> Ids { get; set; } = new();

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
}

/// <summary>
/// INVITE_CREATE event.
/// </summary>
public class InviteCreateEvent : GatewayEvent
{
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("inviter")]
    public User? Inviter { get; set; }

    [JsonPropertyName("max_age")]
    public int MaxAge { get; set; }

    [JsonPropertyName("max_uses")]
    public int MaxUses { get; set; }

    [JsonPropertyName("target_type")]
    public int? TargetType { get; set; }

    [JsonPropertyName("target_user")]
    public User? TargetUser { get; set; }

    [JsonPropertyName("temporary")]
    public bool Temporary { get; set; }

    [JsonPropertyName("uses")]
    public int Uses { get; set; }
}

/// <summary>
/// INVITE_DELETE event.
/// </summary>
public class InviteDeleteEvent : GatewayEvent
{
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// WEBHOOKS_UPDATE event — fired when a channel's webhooks change.
/// </summary>
public class WebhooksUpdateEvent : GatewayEvent
{
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }
}

/// <summary>
/// APPLICATION_COMMAND_PERMISSIONS_UPDATE event — fired when application command permissions for a guild are updated.
/// </summary>
public class ApplicationCommandPermissionsUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("permissions")]
    public List<ApplicationCommandPermission>? Permissions { get; set; }
}

/// <summary>
/// INTEGRATION_CREATE event — fired when a guild integration is created.
/// </summary>
public class IntegrationCreateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
}

/// <summary>
/// INTEGRATION_UPDATE event — fired when a guild integration is updated.
/// </summary>
public class IntegrationUpdateEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
}

/// <summary>
/// INTEGRATION_DELETE event — fired when a guild integration is deleted.
/// </summary>
public class IntegrationDeleteEvent : GatewayEvent
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? ApplicationId { get; set; }
}

/// <summary>
/// VOICE_CHANNEL_EFFECT_SEND event — fired when someone sends an emoji reaction or soundboard sound
/// in a voice channel the current user is connected to.
/// </summary>
public class VoiceChannelEffectSendEvent : GatewayEvent
{
    /// <summary>The ID of the channel the effect was sent in.</summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    /// <summary>The ID of the guild.</summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    /// <summary>The ID of the user who sent the effect.</summary>
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong UserId { get; set; }

    /// <summary>The emoji sent (null if a soundboard sound was used instead).</summary>
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }

    /// <summary>
    /// The type of emoji animation: 0 = PREMIUM (super-reaction), 1 = BASIC.
    /// Null when a soundboard sound is the effect.
    /// </summary>
    [JsonPropertyName("animation_type")]
    public int? AnimationType { get; set; }

    /// <summary>The ID of the soundboard sound, if the effect is a soundboard sound.</summary>
    [JsonPropertyName("sound_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? SoundId { get; set; }

    /// <summary>The volume of the soundboard sound, from 0 to 1. Null when not a soundboard effect.</summary>
    [JsonPropertyName("sound_volume")]
    public double? SoundVolume { get; set; }
}

/// <summary>
/// VOICE_CHANNEL_STATUS_UPDATE event — fired when a voice channel's status text changes.
/// </summary>
public class VoiceChannelStatusUpdateEvent : GatewayEvent
{
    /// <summary>The ID of the voice channel whose status changed.</summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }

    /// <summary>The ID of the guild.</summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    /// <summary>The new status text, or null if the status was cleared.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
