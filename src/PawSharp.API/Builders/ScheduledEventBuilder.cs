#nullable enable
using System;

namespace PawSharp.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="Models.CreateGuildScheduledEventRequest"/> objects.
/// </summary>
/// <example>
/// <code>
/// var evt = new ScheduledEventBuilder()
///     .WithName("Dev Meetup")
///     .WithEntityType(ScheduledEventEntityType.External)
///     .WithLocation("Discord Stage")
///     .WithStartTime(DateTimeOffset.UtcNow.AddDays(3))
///     .WithEndTime(DateTimeOffset.UtcNow.AddDays(3).AddHours(2))
///     .Build();
/// </code>
/// </example>
public sealed class ScheduledEventBuilder
{
    // Entity type constants matching Discord's API
    /// <summary>Stage instance (requires a stage channel).</summary>
    public const int StageInstance = 1;
    /// <summary>Voice channel event.</summary>
    public const int Voice = 2;
    /// <summary>External event (requires location and end time).</summary>
    public const int External = 3;

    private ulong _channelId;
    private string _name = string.Empty;
    private string? _description;
    private DateTimeOffset _startTime;
    private DateTimeOffset? _endTime;
    private int _privacyLevel = 2; // GUILD_ONLY
    private int _entityType;
    private string? _location;
    private string? _image;

    /// <summary>Sets the voice or stage channel ID (not required for external events).</summary>
    public ScheduledEventBuilder WithChannelId(ulong channelId)
    {
        _channelId = channelId;
        return this;
    }

    /// <summary>Sets the event name (1–100 characters).</summary>
    public ScheduledEventBuilder WithName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters.", nameof(name));
        _name = name;
        return this;
    }

    /// <summary>Sets an optional description (max 1000 characters).</summary>
    public ScheduledEventBuilder WithDescription(string description)
    {
        if (description.Length > 1000)
            throw new ArgumentException("Description cannot exceed 1000 characters.", nameof(description));
        _description = description;
        return this;
    }

    /// <summary>Sets the scheduled start time (must be in the future).</summary>
    public ScheduledEventBuilder WithStartTime(DateTimeOffset startTime)
    {
        _startTime = startTime;
        return this;
    }

    /// <summary>Sets the scheduled end time (required for external events).</summary>
    public ScheduledEventBuilder WithEndTime(DateTimeOffset endTime)
    {
        _endTime = endTime;
        return this;
    }

    /// <summary>
    /// Sets the entity type: 1 = StageInstance, 2 = Voice, 3 = External.
    /// Use the <see cref="StageInstance"/>, <see cref="Voice"/>, or <see cref="External"/> constants.
    /// </summary>
    public ScheduledEventBuilder WithEntityType(int entityType)
    {
        _entityType = entityType;
        return this;
    }

    /// <summary>
    /// Sets the event location string (required for external events, max 100 characters).
    /// </summary>
    public ScheduledEventBuilder WithLocation(string location)
    {
        if (location.Length > 100)
            throw new ArgumentException("Location cannot exceed 100 characters.", nameof(location));
        _location = location;
        return this;
    }

    /// <summary>Sets the cover image as a base64-encoded data URI (e.g. <c>data:image/png;base64,...</c>).</summary>
    public ScheduledEventBuilder WithImage(string imageDataUri)
    {
        _image = imageDataUri;
        return this;
    }

    /// <summary>Builds and returns the <see cref="Models.CreateGuildScheduledEventRequest"/>.</summary>
    public Models.CreateGuildScheduledEventRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Event name is required.");
        if (_entityType == 0)
            throw new InvalidOperationException("Entity type must be set.");
        if (_startTime == default)
            throw new InvalidOperationException("Scheduled start time is required.");
        if (_entityType == External && _endTime is null)
            throw new InvalidOperationException("End time is required for external events.");
        if (_entityType == External && string.IsNullOrWhiteSpace(_location))
            throw new InvalidOperationException("Location is required for external events.");

        return new Models.CreateGuildScheduledEventRequest
        {
            ChannelId = _channelId,
            Name = _name,
            Description = _description,
            ScheduledStartTime = _startTime,
            ScheduledEndTime = _endTime,
            PrivacyLevel = _privacyLevel,
            EntityType = _entityType,
            EntityMetadataLocation = _location,
            Image = _image
        };
    }
}
