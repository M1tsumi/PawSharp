#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord message poll.
/// </summary>
public class Poll
{
    /// <summary>The question of the poll. Only text is supported.</summary>
    [JsonPropertyName("question")]
    public PollMedia Question { get; set; } = null!;

    /// <summary>Each of the answers available in the poll.</summary>
    [JsonPropertyName("answers")]
    public List<PollAnswer> Answers { get; set; } = new();

    /// <summary>The time when the poll expires.</summary>
    [JsonPropertyName("expiry")]
    public DateTimeOffset? Expiry { get; set; }

    /// <summary>Whether a user can select multiple answers.</summary>
    [JsonPropertyName("allow_multiselect")]
    public bool AllowMultiselect { get; set; }

    /// <summary>The layout type of the poll.</summary>
    [JsonPropertyName("layout_type")]
    public PollLayoutType LayoutType { get; set; }

    /// <summary>The results of the poll. Only sent if the poll has been finalised.</summary>
    [JsonPropertyName("results")]
    public PollResults? Results { get; set; }
}

/// <summary>
/// Represents a poll's media object (question or answer text/emoji).
/// </summary>
public class PollMedia
{
    /// <summary>The text of the field. Maximum 300 characters for question, 55 for answer.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>The emoji of the field.</summary>
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }
}

/// <summary>
/// Represents a single answer in a poll.
/// </summary>
public class PollAnswer
{
    /// <summary>The ID of the answer. This is only sent as part of responses from Discord's API/Gateway.</summary>
    [JsonPropertyName("answer_id")]
    public int? AnswerId { get; set; }

    /// <summary>The data of the answer.</summary>
    [JsonPropertyName("poll_media")]
    public PollMedia PollMedia { get; set; } = null!;
}

/// <summary>
/// Represents the results of a poll. This is only sent if the poll has been finalised (expired).
/// </summary>
public class PollResults
{
    /// <summary>Whether the votes have been precisely counted.</summary>
    [JsonPropertyName("is_finalized")]
    public bool IsFinalized { get; set; }

    /// <summary>The counts for each answer.</summary>
    [JsonPropertyName("answer_counts")]
    public List<PollAnswerCount> AnswerCounts { get; set; } = new();
}

/// <summary>
/// Represents the vote count for a single poll answer.
/// </summary>
public class PollAnswerCount
{
    /// <summary>The answer_id.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>The number of votes for this answer.</summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>Whether the current user voted for this answer.</summary>
    [JsonPropertyName("me_voted")]
    public bool MeVoted { get; set; }
}

/// <summary>
/// Layout type of a poll.
/// </summary>
public enum PollLayoutType
{
    /// <summary>The default layout type.</summary>
    Default = 1
}
