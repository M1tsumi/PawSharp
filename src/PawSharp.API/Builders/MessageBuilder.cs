#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.API.Models;
using PawSharp.Core.Builders;
using PawSharp.Core.Entities;

namespace PawSharp.API.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="CreateMessageRequest"/> objects.
/// </summary>
/// <example>
/// <code>
/// var request = new MessageBuilder()
///     .WithContent("Hello, world!")
///     .AddEmbed(e => e.WithTitle("Embed Title").WithDescription("Some text").WithColor(0x5865F2))
///     .WithReply(1234567890ul)
///     .WithAllowedMentions(AllowedMentions.PingRepliedUser)
///     .Build();
///
/// await client.Rest.CreateMessageAsync(channelId, request);
/// </code>
/// </example>
public sealed class MessageBuilder
{
    // ── Discord limits ─────────────────────────────────────────────────────────

    /// <summary>Maximum characters allowed in message content.</summary>
    public const int MaxContentLength = 2000;

    /// <summary>Maximum number of embeds per message.</summary>
    public const int MaxEmbeds = 10;

    /// <summary>Maximum number of stickers per message.</summary>
    public const int MaxStickers = 3;

    /// <summary>Maximum nonce length (Discord enforces 25 characters).</summary>
    public const int MaxNonceLength = 25;

    // ── Message flags ──────────────────────────────────────────────────────────

    /// <summary>Suppresses link embeds in the message.</summary>
    public const int FlagSuppressEmbeds = 1 << 2;          // 4

    /// <summary>Suppresses desktop push notifications.</summary>
    public const int FlagSuppressNotifications = 1 << 12;  // 4096

    // ── State ──────────────────────────────────────────────────────────────────

    private string?              _content;
    private bool                 _tts;
    private int                  _flags;
    private AllowedMentions?     _allowedMentions;
    private MessageReference?    _messageReference;
    private CreatePollRequest?   _poll;
    private string?              _nonce;
    private bool                 _enforceNonce;
    private readonly List<Embed>           _embeds     = new();
    private readonly List<MessageComponent> _components = new();
    private readonly List<ulong>           _stickerIds = new();

    // ── Content ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the message text content (max <see cref="MaxContentLength"/> characters).
    /// </summary>
    /// <param name="content">The text to send.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> exceeds the limit.</exception>
    public MessageBuilder WithContent(string content)
    {
        if (content.Length > MaxContentLength)
            throw new ArgumentException(
                $"Message content must not exceed {MaxContentLength} characters (got {content.Length}).",
                nameof(content));

        _content = content;
        return this;
    }

    /// <summary>Clears the text content (sends an embed-only or component-only message).</summary>
    public MessageBuilder ClearContent()
    {
        _content = null;
        return this;
    }

    // ── Embeds ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a pre-built <see cref="Embed"/> to the message (max <see cref="MaxEmbeds"/>).
    /// </summary>
    public MessageBuilder AddEmbed(Embed embed)
    {
        if (_embeds.Count >= MaxEmbeds)
            throw new InvalidOperationException(
                $"A message cannot contain more than {MaxEmbeds} embeds.");

        _embeds.Add(embed ?? throw new ArgumentNullException(nameof(embed)));
        return this;
    }

    /// <summary>
    /// Builds and appends an embed using the provided <see cref="EmbedBuilder"/> configuration action.
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="EmbedBuilder"/>.</param>
    public MessageBuilder AddEmbed(Action<EmbedBuilder> configure)
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));
        var builder = new EmbedBuilder();
        configure(builder);
        return AddEmbed(builder.Build());
    }

    /// <summary>Removes all embeds from the builder.</summary>
    public MessageBuilder ClearEmbeds()
    {
        _embeds.Clear();
        return this;
    }

    // ── Components ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a message component (button, select menu, action row, etc.).
    /// </summary>
    public MessageBuilder AddComponent(MessageComponent component)
    {
        _components.Add(component ?? throw new ArgumentNullException(nameof(component)));
        return this;
    }

    /// <summary>Removes all components from the builder.</summary>
    public MessageBuilder ClearComponents()
    {
        _components.Clear();
        return this;
    }

    // ── Stickers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a sticker by ID (max <see cref="MaxStickers"/> stickers). Stickers must belong to the server.
    /// </summary>
    public MessageBuilder AddSticker(ulong stickerId)
    {
        if (_stickerIds.Count >= MaxStickers)
            throw new InvalidOperationException(
                $"A message cannot contain more than {MaxStickers} stickers.");

        _stickerIds.Add(stickerId);
        return this;
    }

    // ── Behaviour flags ───────────────────────────────────────────────────────

    /// <summary>Enables text-to-speech for this message.</summary>
    public MessageBuilder AsTts()
    {
        _tts = true;
        return this;
    }

    /// <summary>
    /// Prevents Discord from creating link-preview embeds for any URLs in the message content.
    /// </summary>
    public MessageBuilder SuppressEmbeds()
    {
        _flags |= FlagSuppressEmbeds;
        return this;
    }

    /// <summary>
    /// Sends the message without triggering desktop push notifications for mentioned users.
    /// Useful for bulk announcements or status updates that don't need to interrupt users.
    /// </summary>
    public MessageBuilder SuppressNotifications()
    {
        _flags |= FlagSuppressNotifications;
        return this;
    }

    // ── Allowed mentions ──────────────────────────────────────────────────────

    /// <summary>
    /// Controls who Discord will mention when the message is delivered.
    /// </summary>
    /// <param name="allowedMentions">
    /// Use factory shortcuts such as <see cref="AllowedMentions.None"/>,
    /// <see cref="AllowedMentions.PingRepliedUser"/>, or construct a custom instance.
    /// </param>
    public MessageBuilder WithAllowedMentions(AllowedMentions allowedMentions)
    {
        _allowedMentions = allowedMentions;
        return this;
    }

    // ── Reply / message reference ─────────────────────────────────────────────

    /// <summary>
    /// Sends this message as an inline reply to <paramref name="messageId"/>.
    /// </summary>
    /// <param name="messageId">The ID of the message to reply to.</param>
    /// <param name="failIfNotExists">
    /// When <c>true</c>, Discord will return an error if the target message has been deleted.
    /// Defaults to <c>false</c> so the message is sent as a standalone message if the target is gone.
    /// </param>
    public MessageBuilder WithReply(ulong messageId, bool failIfNotExists = false)
    {
        _messageReference = MessageReference.Reply(messageId, failIfNotExists);
        return this;
    }

    /// <summary>Sets a raw <see cref="MessageReference"/> on the message.</summary>
    public MessageBuilder WithMessageReference(MessageReference reference)
    {
        _messageReference = reference;
        return this;
    }

    // ── Poll ──────────────────────────────────────────────────────────────────

    /// <summary>Attaches a poll to the message.</summary>
    public MessageBuilder WithPoll(CreatePollRequest poll)
    {
        _poll = poll ?? throw new ArgumentNullException(nameof(poll));
        return this;
    }

    // ── Nonce ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets an opaque nonce string (max <see cref="MaxNonceLength"/> characters)
    /// that clients can use to verify a message was sent.
    /// </summary>
    /// <param name="nonce">The nonce value.</param>
    /// <param name="enforce">
    /// When <c>true</c>, Discord checks that this nonce was not used recently
    /// and rejects the message if it was (idempotency protection).
    /// </param>
    public MessageBuilder WithNonce(string nonce, bool enforce = false)
    {
        if (nonce.Length > MaxNonceLength)
            throw new ArgumentException(
                $"Nonce must not exceed {MaxNonceLength} characters.", nameof(nonce));

        _nonce        = nonce;
        _enforceNonce = enforce;
        return this;
    }

    // ── Build ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates and constructs the <see cref="CreateMessageRequest"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the request has no sendable content (no content, embeds, stickers, components, or poll).
    /// </exception>
    public CreateMessageRequest Build()
    {
        var hasContent    = !string.IsNullOrEmpty(_content);
        var hasEmbeds     = _embeds.Count     > 0;
        var hasComponents = _components.Count > 0;
        var hasStickers   = _stickerIds.Count > 0;
        var hasPoll       = _poll             != null;

        if (!hasContent && !hasEmbeds && !hasComponents && !hasStickers && !hasPoll)
            throw new InvalidOperationException(
                "A message must have at least one of: content, embeds, components, stickers, or a poll.");

        return new CreateMessageRequest
        {
            Content          = _content,
            Tts              = _tts ? true : null,
            Flags            = _flags != 0 ? _flags : null,
            AllowedMentions  = _allowedMentions,
            MessageReference = _messageReference,
            Poll             = _poll,
            Nonce            = _nonce,
            EnforceNonce     = _enforceNonce && _nonce != null ? true : null,
            Embeds           = hasEmbeds     ? new List<Embed>(_embeds)                  : null,
            Components       = hasComponents ? new List<MessageComponent>(_components)   : null,
            StickerIds       = hasStickers   ? new List<ulong>(_stickerIds)              : null,
        };
    }
}
