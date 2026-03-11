#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.API.Models;

namespace PawSharp.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="CreatePollRequest"/> objects.
/// </summary>
/// <example>
/// <code>
/// var poll = new PollBuilder()
///     .WithQuestion("What is your favourite colour?")
///     .AddAnswer("Red", emojiName: "❤️")
///     .AddAnswer("Blue", emojiName: "💙")
///     .WithDuration(24)
///     .AllowMultiselect()
///     .Build();
/// await client.CreatePollAsync(channelId, poll);
/// </code>
/// </example>
public sealed class PollBuilder
{
    private string? _question;
    private readonly List<PollAnswerRequest> _answers = new();
    private int _duration = 24; // hours
    private bool _allowMultiselect;
    private int _layoutType = 1; // Default

    /// <summary>Sets the poll question text (max 300 characters).</summary>
    public PollBuilder WithQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Poll question cannot be empty.", nameof(question));
        if (question.Length > 300)
            throw new ArgumentException("Poll question cannot exceed 300 characters.", nameof(question));
        _question = question;
        return this;
    }

    /// <summary>Adds an answer option with optional emoji.</summary>
    /// <param name="text">Answer text (max 55 characters).</param>
    /// <param name="emojiId">Snowflake ID of a custom guild emoji (mutually exclusive with <paramref name="emojiName"/>).</param>
    /// <param name="emojiName">Unicode emoji string or custom emoji name.</param>
    public PollBuilder AddAnswer(string text, ulong? emojiId = null, string? emojiName = null)
    {
        if (_answers.Count >= 10)
            throw new InvalidOperationException("A poll may have at most 10 answers.");
        if (text.Length > 55)
            throw new ArgumentException("Answer text cannot exceed 55 characters.", nameof(text));

        object? emoji = null;
        if (emojiId.HasValue)
            emoji = new { id = emojiId.Value.ToString() };
        else if (emojiName != null)
            emoji = new { name = emojiName };

        _answers.Add(new PollAnswerRequest
        {
            PollMedia = new PollMediaRequest { Text = text, Emoji = emoji }
        });
        return this;
    }

    /// <summary>Sets the poll duration in hours (1–768, default 24).</summary>
    public PollBuilder WithDuration(int hours)
    {
        if (hours < 1 || hours > 768)
            throw new ArgumentOutOfRangeException(nameof(hours), "Duration must be between 1 and 768 hours.");
        _duration = hours;
        return this;
    }

    /// <summary>Allows users to select more than one answer.</summary>
    public PollBuilder AllowMultiselect(bool allow = true)
    {
        _allowMultiselect = allow;
        return this;
    }

    /// <summary>Sets the layout type (1 = Default).</summary>
    public PollBuilder WithLayoutType(int layoutType)
    {
        _layoutType = layoutType;
        return this;
    }

    /// <summary>Builds and returns the <see cref="CreatePollRequest"/>.</summary>
    public CreatePollRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_question))
            throw new InvalidOperationException("Poll question is required.");
        if (_answers.Count < 1)
            throw new InvalidOperationException("A poll must have at least one answer.");

        return new CreatePollRequest
        {
            Question = new PollMediaRequest { Text = _question },
            Answers = _answers,
            Duration = _duration,
            AllowMultiselect = _allowMultiselect,
            LayoutType = _layoutType
        };
    }
}
