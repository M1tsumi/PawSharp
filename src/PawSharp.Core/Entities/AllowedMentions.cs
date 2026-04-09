#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Controls which mentions (users, roles, @everyone) Discord will actually notify
/// when a message is sent.  Passing an <see cref="AllowedMentions"/> object in
/// <c>CreateMessageRequest.AllowedMentions</c> overrides Discord's default "ping everything"
/// behaviour so bots can quote/forward messages safely.
/// </summary>
/// <example>
/// <code>
/// // Ping only the reply target, suppress all other pings:
/// var safe = AllowedMentions.PingRepliedUser;
///
/// // Ping a specific set of users:
/// var specific = new AllowedMentions { Users = new() { userId1, userId2 } };
///
/// // Suppress all pings entirely:
/// var silent = AllowedMentions.None;
/// </code>
/// </example>
public sealed class AllowedMentions
{
    // ── Discord mention type strings ──────────────────────────────────────────

    private const string TypeRoles    = "roles";
    private const string TypeUsers    = "users";
    private const string TypeEveryone = "everyone";

    // ── Serialized properties ─────────────────────────────────────────────────

    /// <summary>
    /// An array of allowed mention types.  May contain <c>"roles"</c>, <c>"users"</c>,
    /// and/or <c>"everyone"</c>.  When <c>null</c> Discord falls back to its default behaviour.
    /// Mutually exclusive with <see cref="Users"/> / <see cref="Roles"/> — if you specify
    /// individual IDs, do NOT list the corresponding type here.
    /// </summary>
    [JsonPropertyName("parse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Parse { get; set; }

    /// <summary>
    /// Up to 100 user IDs to mention.  May only be used when <see cref="Parse"/> does NOT
    /// contain <c>"users"</c>.
    /// </summary>
    [JsonPropertyName("users")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? Users { get; set; }

    /// <summary>
    /// Up to 100 role IDs to mention.  May only be used when <see cref="Parse"/> does NOT
    /// contain <c>"roles"</c>.
    /// </summary>
    [JsonPropertyName("roles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ulong>? Roles { get; set; }

    /// <summary>
    /// When <c>true</c>, pings the author of the message being replied to.
    /// Ignored on non-reply messages.
    /// </summary>
    [JsonPropertyName("replied_user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RepliedUser { get; set; }

    // ── Static factory shortcuts ──────────────────────────────────────────────

    /// <summary>
    /// Suppresses all mentions — nothing in the message will ping anyone.
    /// </summary>
    public static AllowedMentions None => new() { Parse = new List<string>() };

    /// <summary>
    /// Allows all mention types (Discord's default).
    /// </summary>
    public static AllowedMentions All => new()
    {
        Parse = new List<string> { TypeRoles, TypeUsers, TypeEveryone }
    };

    /// <summary>
    /// Pings only the author of a replied-to message; suppresses all other pings.
    /// Equivalent to <c>new AllowedMentions { Parse = [], RepliedUser = true }</c>.
    /// </summary>
    public static AllowedMentions PingRepliedUser => new()
    {
        Parse       = new List<string>(),
        RepliedUser = true
    };

    /// <summary>
    /// Allows @role pings only.
    /// </summary>
    public static AllowedMentions OnlyRoles => new() { Parse = new List<string> { TypeRoles } };

    /// <summary>
    /// Allows @user pings only.
    /// </summary>
    public static AllowedMentions OnlyUsers => new() { Parse = new List<string> { TypeUsers } };
}

/// <summary>
/// Represents the target of a message reply / crosspost reference.
/// </summary>
public enum MessageReferenceType
{
    /// <summary>Standard reference used by replies.</summary>
    Default = 0,

    /// <summary>Reference used to capture a forwarded message snapshot.</summary>
    Forward = 1,
}

/// <summary>
/// Represents the target of a message reply / crosspost / forward reference.
/// </summary>
public sealed class MessageReference
{
    /// <summary>
    /// Type of reference. When omitted, Discord treats it as <see cref="MessageReferenceType.Default"/>.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Type { get; set; }

    /// <summary>
    /// ID of the originating message.
    /// </summary>
    [JsonPropertyName("message_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? MessageId { get; set; }

    /// <summary>
    /// ID of the originating channel. Optional — Discord infers it when omitted.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? ChannelId { get; set; }

    /// <summary>
    /// ID of the originating guild. Optional.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? GuildId { get; set; }

    /// <summary>
    /// When <c>false</c> (default) the API will return an error if the referenced message
    /// does not exist.  Set to <c>true</c> to send regardless.
    /// </summary>
    [JsonPropertyName("fail_if_not_exists")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FailIfNotExists { get; set; }

    /// <summary>
    /// Creates a reply reference pointing at the given message.
    /// </summary>
    public static MessageReference Reply(ulong messageId, bool failIfNotExists = false)
        => new() { Type = (int)MessageReferenceType.Default, MessageId = messageId, FailIfNotExists = failIfNotExists };

    /// <summary>
    /// Creates a forward reference using Discord's message snapshot model.
    /// Requires source channel and message IDs.
    /// </summary>
    public static MessageReference Forward(ulong channelId, ulong messageId, bool failIfNotExists = true)
        => new()
        {
            Type = (int)MessageReferenceType.Forward,
            ChannelId = channelId,
            MessageId = messageId,
            FailIfNotExists = failIfNotExists,
        };
}
