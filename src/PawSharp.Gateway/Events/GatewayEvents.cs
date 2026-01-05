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
    public object? Application { get; set; }
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
    
    [JsonPropertyName("owner_id")]
    public ulong OwnerId { get; set; }
    
    [JsonPropertyName("roles")]
    public List<Role> Roles { get; set; } = new();
    
    [JsonPropertyName("emojis")]
    public List<Emoji> Emojis { get; set; } = new();
    
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
            OwnerId = OwnerId,
            Roles = Roles,
            Emojis = Emojis,
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
    
    public Channel ToChannel()
    {
        var channel = new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name
        };
        return channel;
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
    
    public Channel ToChannel()
    {
        var channel = new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name
        };
        return channel;
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
        var channel = new Channel
        {
            Id = Id,
            Type = (Core.Enums.ChannelType)Type,
            GuildId = GuildId,
            Name = Name
        };
        return channel;
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
    public ulong ApplicationId { get; set; }
    
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("channel_id")]
    public ulong? ChannelId { get; set; }
    
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
}
