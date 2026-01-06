#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord audit log.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// List of audit log entries.
    /// </summary>
    [JsonPropertyName("audit_log_entries")]
    public List<AuditLogEntry> AuditLogEntries { get; set; } = new();
    
    /// <summary>
    /// List of guild scheduled events found in the audit log.
    /// </summary>
    [JsonPropertyName("guild_scheduled_events")]
    public List<GuildScheduledEvent>? GuildScheduledEvents { get; set; }
    
    /// <summary>
    /// List of partial integration objects.
    /// </summary>
    [JsonPropertyName("integrations")]
    public List<object>? Integrations { get; set; }
    
    /// <summary>
    /// List of threads found in the audit log.
    /// </summary>
    [JsonPropertyName("threads")]
    public List<Channel>? Threads { get; set; }
    
    /// <summary>
    /// List of users found in the audit log.
    /// </summary>
    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new();
    
    /// <summary>
    /// List of webhooks found in the audit log.
    /// </summary>
    [JsonPropertyName("webhooks")]
    public List<Webhook>? Webhooks { get; set; }
}

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditLogEntry : DiscordEntity
{
    /// <summary>
    /// ID of the affected entity (webhook, user, role, etc.).
    /// </summary>
    [JsonPropertyName("target_id")]
    public string? TargetId { get; set; }
    
    /// <summary>
    /// Changes made to the target_id.
    /// </summary>
    [JsonPropertyName("changes")]
    public List<AuditLogChange>? Changes { get; set; }
    
    /// <summary>
    /// The user who made the changes.
    /// </summary>
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? UserId { get; set; }
    
    /// <summary>
    /// ID of the entry.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public new ulong Id { get; set; }
    
    /// <summary>
    /// Type of action that occurred.
    /// </summary>
    [JsonPropertyName("action_type")]
    public AuditLogEvent ActionType { get; set; }
    
    /// <summary>
    /// Additional info for certain action types.
    /// </summary>
    [JsonPropertyName("options")]
    public OptionalAuditEntryInfo? Options { get; set; }
    
    /// <summary>
    /// The reason for the change (0-512 characters).
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Represents an audit log change.
/// </summary>
public class AuditLogChange
{
    /// <summary>
    /// New value of the key.
    /// </summary>
    [JsonPropertyName("new_value")]
    public object? NewValue { get; set; }
    
    /// <summary>
    /// Old value of the key.
    /// </summary>
    [JsonPropertyName("old_value")]
    public object? OldValue { get; set; }
    
    /// <summary>
    /// Name of the changed entity, with a few exceptions.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Represents optional audit entry info.
/// </summary>
public class OptionalAuditEntryInfo
{
    /// <summary>
    /// Number of days after which inactive members were kicked.
    /// </summary>
    [JsonPropertyName("delete_member_days")]
    public string? DeleteMemberDays { get; set; }
    
    /// <summary>
    /// Number of members removed by the prune.
    /// </summary>
    [JsonPropertyName("members_removed")]
    public string? MembersRemoved { get; set; }
    
    /// <summary>
    /// Channel in which the entities were targeted.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }
    
    /// <summary>
    /// ID of the message that was targeted.
    /// </summary>
    [JsonPropertyName("message_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? MessageId { get; set; }
    
    /// <summary>
    /// Number of entities that were targeted.
    /// </summary>
    [JsonPropertyName("count")]
    public string? Count { get; set; }
    
    /// <summary>
    /// ID of the overwritten entity.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? Id { get; set; }
    
    /// <summary>
    /// Type of overwritten entity - "0" for "role" or "1" for "member".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    /// <summary>
    /// ID of the role if type is "0" (not present if type is "1").
    /// </summary>
    [JsonPropertyName("role_name")]
    public string? RoleName { get; set; }
}

/// <summary>
/// Audit log events.
/// </summary>
public enum AuditLogEvent
{
    GuildUpdate = 1,
    ChannelCreate = 10,
    ChannelUpdate = 11,
    ChannelDelete = 12,
    ChannelOverwriteCreate = 13,
    ChannelOverwriteUpdate = 14,
    ChannelOverwriteDelete = 15,
    MemberKick = 20,
    MemberPrune = 21,
    MemberBanAdd = 22,
    MemberBanRemove = 23,
    MemberUpdate = 24,
    MemberRoleUpdate = 25,
    MemberMove = 26,
    MemberDisconnect = 27,
    BotAdd = 28,
    RoleCreate = 30,
    RoleUpdate = 31,
    RoleDelete = 32,
    InviteCreate = 40,
    InviteUpdate = 41,
    InviteDelete = 42,
    WebhookCreate = 50,
    WebhookUpdate = 51,
    WebhookDelete = 52,
    EmojiCreate = 60,
    EmojiUpdate = 61,
    EmojiDelete = 62,
    MessageDelete = 72,
    MessageBulkDelete = 73,
    MessagePin = 74,
    MessageUnpin = 75,
    IntegrationCreate = 80,
    IntegrationUpdate = 81,
    IntegrationDelete = 82,
    StageInstanceCreate = 83,
    StageInstanceUpdate = 84,
    StageInstanceDelete = 85,
    StickerCreate = 90,
    StickerUpdate = 91,
    StickerDelete = 92,
    GuildScheduledEventCreate = 100,
    GuildScheduledEventUpdate = 101,
    GuildScheduledEventDelete = 102,
    ThreadCreate = 110,
    ThreadUpdate = 111,
    ThreadDelete = 112,
    ApplicationCommandPermissionUpdate = 121,
    AutoModerationRuleCreate = 140,
    AutoModerationRuleUpdate = 141,
    AutoModerationRuleDelete = 142,
    MessagePollVoteAdd = 143,
    MessagePollVoteRemove = 144
}