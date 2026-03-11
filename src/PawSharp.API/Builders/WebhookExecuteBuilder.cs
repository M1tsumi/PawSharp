#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Core.Entities;

namespace PawSharp.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="Models.ExecuteWebhookRequest"/> objects.
/// </summary>
/// <example>
/// <code>
/// var request = new WebhookExecuteBuilder()
///     .WithContent("Hello from a webhook!")
///     .WithUsername("CoolBot")
///     .WithWait(true)
///     .Build();
/// await client.ExecuteWebhookAsync(webhookId, token, request);
/// </code>
/// </example>
public sealed class WebhookExecuteBuilder
{
    private string? _content;
    private string? _username;
    private string? _avatarUrl;
    private bool _tts;
    private bool _wait;
    private string? _threadName;
    private readonly List<Embed> _embeds = new();
    private readonly List<MessageComponent> _components = new();

    /// <summary>Sets the message content (max 2000 characters).</summary>
    public WebhookExecuteBuilder WithContent(string content)
    {
        if (content.Length > 2000)
            throw new ArgumentException("Content cannot exceed 2000 characters.", nameof(content));
        _content = content;
        return this;
    }

    /// <summary>Overrides the webhook's display name for this execution.</summary>
    public WebhookExecuteBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    /// <summary>Overrides the webhook's avatar URL for this execution.</summary>
    public WebhookExecuteBuilder WithAvatarUrl(string avatarUrl)
    {
        _avatarUrl = avatarUrl;
        return this;
    }

    /// <summary>Adds a rich embed to the message.</summary>
    public WebhookExecuteBuilder AddEmbed(Embed embed)
    {
        if (_embeds.Count >= 10)
            throw new InvalidOperationException("A webhook message may have at most 10 embeds.");
        _embeds.Add(embed);
        return this;
    }

    /// <summary>Adds a message component (action row, button, etc.).</summary>
    public WebhookExecuteBuilder AddComponent(MessageComponent component)
    {
        _components.Add(component);
        return this;
    }

    /// <summary>Enables text-to-speech for the message.</summary>
    public WebhookExecuteBuilder WithTts(bool tts = true)
    {
        _tts = tts;
        return this;
    }

    /// <summary>
    /// When <c>true</c>, Discord returns the created message object.
    /// The REST client will append <c>wait=true</c> to the query string automatically.
    /// </summary>
    public WebhookExecuteBuilder WithWait(bool wait = true)
    {
        _wait = wait;
        return this;
    }

    /// <summary>
    /// Sets the name of the thread to create (only valid for forum/media channel webhooks).
    /// </summary>
    public WebhookExecuteBuilder WithThreadName(string threadName)
    {
        _threadName = threadName;
        return this;
    }

    /// <summary>Builds and returns the <see cref="Models.ExecuteWebhookRequest"/>.</summary>
    public Models.ExecuteWebhookRequest Build()
    {
        if (_content is null && _embeds.Count == 0)
            throw new InvalidOperationException("A webhook message must have at least content or an embed.");

        return new Models.ExecuteWebhookRequest
        {
            Content = _content,
            Username = _username,
            AvatarUrl = _avatarUrl,
            Tts = _tts ? true : null,
            Embeds = _embeds.Count > 0 ? _embeds : null,
            Components = _components.Count > 0 ? _components : null,
            Wait = _wait,
            ThreadName = _threadName
        };
    }
}
