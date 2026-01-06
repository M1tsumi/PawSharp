#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents an auto moderation rule.
/// </summary>
public class AutoModerationRule : DiscordEntity
{
    /// <summary>
    /// The guild which this rule belongs to.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }
    
    /// <summary>
    /// The rule name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The user which first created this rule.
    /// </summary>
    [JsonPropertyName("creator_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong CreatorId { get; set; }
    
    /// <summary>
    /// The rule event type.
    /// </summary>
    [JsonPropertyName("event_type")]
    public AutoModerationEventType EventType { get; set; }
    
    /// <summary>
    /// The rule trigger type.
    /// </summary>
    [JsonPropertyName("trigger_type")]
    public AutoModerationTriggerType TriggerType { get; set; }
    
    /// <summary>
    /// The rule trigger metadata.
    /// </summary>
    [JsonPropertyName("trigger_metadata")]
    public AutoModerationTriggerMetadata TriggerMetadata { get; set; } = null!;
    
    /// <summary>
    /// The actions which will execute when the rule is triggered.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<AutoModerationAction> Actions { get; set; } = new();
    
    /// <summary>
    /// Whether the rule is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    
    /// <summary>
    /// The role ids that should not be affected by the rule (Maximum of 20).
    /// </summary>
    [JsonPropertyName("exempt_roles")]
    public List<ulong> ExemptRoles { get; set; } = new();
    
    /// <summary>
    /// The channel ids that should not be affected by the rule (Maximum of 50).
    /// </summary>
    [JsonPropertyName("exempt_channels")]
    public List<ulong> ExemptChannels { get; set; } = new();
}

/// <summary>
/// Represents auto moderation trigger metadata.
/// </summary>
public class AutoModerationTriggerMetadata
{
    /// <summary>
    /// Substrings which will be searched for in content (Maximum of 1000).
    /// </summary>
    [JsonPropertyName("keyword_filter")]
    public List<string>? KeywordFilter { get; set; }
    
    /// <summary>
    /// Regular expression patterns which will be matched against content (Maximum of 10).
    /// </summary>
    [JsonPropertyName("regex_patterns")]
    public List<string>? RegexPatterns { get; set; }
    
    /// <summary>
    /// The internally pre-defined wordsets which will be searched for in content.
    /// </summary>
    [JsonPropertyName("presets")]
    public List<AutoModerationKeywordPresetType>? Presets { get; set; }
    
    /// <summary>
    /// Substrings which should not trigger the rule (Maximum of 100 or 1000).
    /// </summary>
    [JsonPropertyName("allow_list")]
    public List<string>? AllowList { get; set; }
    
    /// <summary>
    /// Total number of unique role and user mentions allowed per message (Maximum of 50).
    /// </summary>
    [JsonPropertyName("mention_total_limit")]
    public int? MentionTotalLimit { get; set; }
    
    /// <summary>
    /// Whether to automatically detect mention raids.
    /// </summary>
    [JsonPropertyName("mention_raid_protection_enabled")]
    public bool? MentionRaidProtectionEnabled { get; set; }
}

/// <summary>
/// Represents an auto moderation action.
/// </summary>
public class AutoModerationAction
{
    /// <summary>
    /// The type of action.
    /// </summary>
    [JsonPropertyName("type")]
    public AutoModerationActionType Type { get; set; }
    
    /// <summary>
    /// Additional metadata needed during execution for this specific action type.
    /// </summary>
    [JsonPropertyName("metadata")]
    public AutoModerationActionMetadata? Metadata { get; set; }
}

/// <summary>
/// Represents auto moderation action metadata.
/// </summary>
public class AutoModerationActionMetadata
{
    /// <summary>
    /// Channel to which user content should be logged.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong? ChannelId { get; set; }
    
    /// <summary>
    /// Timeout duration in seconds (Maximum of 2419200).
    /// </summary>
    [JsonPropertyName("duration_seconds")]
    public int? DurationSeconds { get; set; }
    
    /// <summary>
    /// Additional explanation that will be shown to members whenever their message is blocked.
    /// </summary>
    [JsonPropertyName("custom_message")]
    public string? CustomMessage { get; set; }
}

/// <summary>
/// Auto moderation event type.
/// </summary>
public enum AutoModerationEventType
{
    /// <summary>
    /// When a member sends or edits a message in the guild.
    /// </summary>
    MessageSend = 1
}

/// <summary>
/// Auto moderation trigger type.
/// </summary>
public enum AutoModerationTriggerType
{
    /// <summary>
    /// Check if content contains words from a user defined list of keywords.
    /// </summary>
    Keyword = 1,
    
    /// <summary>
    /// Check if content represents generic spam.
    /// </summary>
    Spam = 3,
    
    /// <summary>
    /// Check if content contains words from internal pre-defined wordsets.
    /// </summary>
    KeywordPreset = 4,
    
    /// <summary>
    /// Check if content contains more unique mentions than allowed.
    /// </summary>
    MentionSpam = 5
}

/// <summary>
/// Auto moderation keyword preset types.
/// </summary>
public enum AutoModerationKeywordPresetType
{
    /// <summary>
    /// Words that may be considered forms of swearing or cursing.
    /// </summary>
    Profanity = 1,
    
    /// <summary>
    /// Words that refer to sexually explicit behavior or activity.
    /// </summary>
    SexualContent = 2,
    
    /// <summary>
    /// Personal insults or words that may be considered hate speech.
    /// </summary>
    Slurs = 3
}

/// <summary>
/// Auto moderation action type.
/// </summary>
public enum AutoModerationActionType
{
    /// <summary>
    /// Blocks the content of a message according to the rule.
    /// </summary>
    BlockMessage = 1,
    
    /// <summary>
    /// Logs user content to a specified channel.
    /// </summary>
    SendAlertMessage = 2,
    
    /// <summary>
    /// Timeout user for a specified duration.
    /// </summary>
    Timeout = 3
}