#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PawSharp.Core.Entities;

namespace PawSharp.Core.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="Embed"/> objects.
/// </summary>
/// <example>
/// <code>
/// var embed = new EmbedBuilder()
///     .WithTitle("Hello World")
///     .WithDescription("This is an embed built with PawSharp.")
///     .WithColor(0x5865F2)
///     .WithAuthor("My Bot", iconUrl: "https://example.com/icon.png")
///     .AddField("Field 1", "Value 1", inline: true)
///     .AddField("Field 2", "Value 2", inline: true)
///     .WithFooter("PawSharp v0.5.0")
///     .WithTimestamp()
///     .Build();
/// </code>
/// </example>
public sealed class EmbedBuilder
{
    private string?              _title;
    private string?              _description;
    private string?              _url;
    private int?                 _color;
    private DateTimeOffset?      _timestamp;
    private EmbedFooter?         _footer;
    private EmbedImage?          _image;
    private EmbedThumbnail?      _thumbnail;
    private EmbedAuthor?         _author;
    private readonly List<EmbedField> _fields = new();

    // ── Discord limits ────────────────────────────────────────────────────────

    /// <summary>Maximum characters allowed in an embed title.</summary>
    public const int MaxTitleLength       = 256;
    /// <summary>Maximum characters allowed in an embed description.</summary>
    public const int MaxDescriptionLength = 4096;
    /// <summary>Maximum number of fields in an embed.</summary>
    public const int MaxFields            = 25;
    /// <summary>Maximum characters allowed in a field name.</summary>
    public const int MaxFieldNameLength   = 256;
    /// <summary>Maximum characters allowed in a field value.</summary>
    public const int MaxFieldValueLength  = 1024;
    /// <summary>Maximum characters allowed in a footer text.</summary>
    public const int MaxFooterLength      = 2048;
    /// <summary>Maximum characters allowed in an author name.</summary>
    public const int MaxAuthorLength      = 256;
    /// <summary>Maximum total character count across all embed fields combined (Discord enforces 6000).</summary>
    public const int MaxTotalLength       = 6000;

    // ── Fluent setters ────────────────────────────────────────────────────────

    /// <summary>Sets the embed title (max 256 characters).</summary>
    public EmbedBuilder WithTitle(string title)
    {
        if (title.Length > MaxTitleLength)
            throw new ArgumentException($"Embed title must not exceed {MaxTitleLength} characters.", nameof(title));
        _title = title;
        return this;
    }

    /// <summary>Sets the embed description (max 4096 characters).</summary>
    public EmbedBuilder WithDescription(string description)
    {
        if (description.Length > MaxDescriptionLength)
            throw new ArgumentException($"Embed description must not exceed {MaxDescriptionLength} characters.", nameof(description));
        _description = description;
        return this;
    }

    /// <summary>Sets the URL that the title hyperlinks to.</summary>
    public EmbedBuilder WithUrl(string url)
    {
        _url = url;
        return this;
    }

    /// <summary>Sets the embed sidebar color as a 24-bit RGB integer (e.g. <c>0x5865F2</c>).</summary>
    public EmbedBuilder WithColor(int color)
    {
        _color = color;
        return this;
    }

    /// <summary>
    /// Sets the embed sidebar color from a <c>uint</c> hex literal (e.g. <c>0xFF5733u</c>).
    /// </summary>
    public EmbedBuilder WithColor(uint color)
    {
        _color = (int)(color & 0xFFFFFF);
        return this;
    }

    /// <summary>
    /// Sets the embed sidebar color from individual R, G, B components (0–255 each).
    /// </summary>
    public EmbedBuilder WithColor(byte r, byte g, byte b)
    {
        _color = (r << 16) | (g << 8) | b;
        return this;
    }

    // ── Color presets ───────────────────────────────────────────────────────────

    /// <summary>Sets the embed color to Discord's blurple (0x5865F2).</summary>
    public EmbedBuilder WithBlurpleColor()
    {
        _color = 0x5865F2;
        return this;
    }

    /// <summary>Sets the embed color to Discord's green (0x57F287).</summary>
    public EmbedBuilder WithGreenColor()
    {
        _color = 0x57F287;
        return this;
    }

    /// <summary>Sets the embed color to Discord's yellow (0xFEE75C).</summary>
    public EmbedBuilder WithYellowColor()
    {
        _color = 0xFEE75C;
        return this;
    }

    /// <summary>Sets the embed color to Discord's red (0xED4245).</summary>
    public EmbedBuilder WithRedColor()
    {
        _color = 0xED4245;
        return this;
    }

    /// <summary>Sets the embed color to white (0xFFFFFF).</summary>
    public EmbedBuilder WithWhiteColor()
    {
        _color = 0xFFFFFF;
        return this;
    }

    /// <summary>Sets the embed color to black (0x000000).</summary>
    public EmbedBuilder WithBlackColor()
    {
        _color = 0x000000;
        return this;
    }

    // ── Timestamp helpers ────────────────────────────────────────────────────────

    /// <summary>Sets the timestamp shown at the bottom of the embed.</summary>
    public EmbedBuilder WithTimestamp(DateTimeOffset? timestamp = null)
    {
        _timestamp = timestamp ?? DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>Sets the timestamp to the current UTC time.</summary>
    public EmbedBuilder WithCurrentTimestamp()
    {
        _timestamp = DateTimeOffset.UtcNow;
        return this;
    }

    // ── Quick field methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a field to the embed (max 25 fields total).
    /// </summary>
    /// <param name="name">Field name (max 256 characters).</param>
    /// <param name="value">Field value (max 1024 characters).</param>
    /// <param name="inline">Whether this field is displayed side-by-side with adjacent inline fields.</param>
    public EmbedBuilder AddField(string name, string value, bool inline = false)
    {
        if (_fields.Count >= MaxFields)
            throw new InvalidOperationException($"An embed cannot contain more than {MaxFields} fields.");
        if (name.Length > MaxFieldNameLength)
            throw new ArgumentException($"Field name must not exceed {MaxFieldNameLength} characters.", nameof(name));
        if (value.Length > MaxFieldValueLength)
            throw new ArgumentException($"Field value must not exceed {MaxFieldValueLength} characters.", nameof(value));

        _fields.Add(new EmbedField { Name = name, Value = value, Inline = inline });
        return this;
    }

    /// <summary>
    /// Appends an inline field to the embed (shortcut for AddField with inline: true).
    /// </summary>
    /// <param name="name">Field name (max 256 characters).</param>
    /// <param name="value">Field value (max 1024 characters).</param>
    public EmbedBuilder AddInlineField(string name, string value)
    {
        return AddField(name, value, inline: true);
    }

    /// <summary>Sets the embed footer.</summary>
    /// <param name="text">Footer text (max 2048 characters).</param>
    /// <param name="iconUrl">URL of a small icon displayed beside the footer text.</param>
    public EmbedBuilder WithFooter(string text, string? iconUrl = null)
    {
        if (text.Length > MaxFooterLength)
            throw new ArgumentException($"Footer text must not exceed {MaxFooterLength} characters.", nameof(text));
        _footer = new EmbedFooter { Text = text, IconUrl = iconUrl };
        return this;
    }

    /// <summary>Sets the embed footer from an existing <see cref="EmbedFooter"/>.</summary>
    public EmbedBuilder WithFooter(EmbedFooter footer)
    {
        _footer = footer;
        return this;
    }

    /// <summary>Sets the large image displayed inside the embed body.</summary>
    public EmbedBuilder WithImage(string url)
    {
        _image = new EmbedImage { Url = url };
        return this;
    }

    /// <summary>Sets the small thumbnail image in the top-right corner of the embed.</summary>
    public EmbedBuilder WithThumbnail(string url)
    {
        _thumbnail = new EmbedThumbnail { Url = url };
        return this;
    }

    /// <summary>Sets the embed author block.</summary>
    /// <param name="name">Author name (max 256 characters).</param>
    /// <param name="url">URL that the author name hyperlinks to.</param>
    /// <param name="iconUrl">URL of a small icon displayed beside the author name.</param>
    public EmbedBuilder WithAuthor(string name, string? url = null, string? iconUrl = null)
    {
        if (name.Length > MaxAuthorLength)
            throw new ArgumentException($"Author name must not exceed {MaxAuthorLength} characters.", nameof(name));
        _author = new EmbedAuthor { Name = name, Url = url, IconUrl = iconUrl };
        return this;
    }

    /// <summary>Sets the embed author from an existing <see cref="EmbedAuthor"/>.</summary>
    public EmbedBuilder WithAuthor(EmbedAuthor author)
    {
        _author = author;
        return this;
    }

    /// <summary>Appends a pre-built <see cref="EmbedField"/> to the embed.</summary>
    public EmbedBuilder AddField(EmbedField field)
    {
        if (_fields.Count >= MaxFields)
            throw new InvalidOperationException($"An embed cannot contain more than {MaxFields} fields.");
        _fields.Add(field);
        return this;
    }

    // ── Clear methods ─────────────────────────────────────────────────────────

    /// <summary>Removes the footer from the embed.</summary>
    public EmbedBuilder WithoutFooter() { _footer = null; return this; }

    /// <summary>Removes the author block from the embed.</summary>
    public EmbedBuilder WithoutAuthor() { _author = null; return this; }

    /// <summary>Removes the large body image from the embed.</summary>
    public EmbedBuilder WithoutImage() { _image = null; return this; }

    /// <summary>Removes the thumbnail from the embed.</summary>
    public EmbedBuilder WithoutThumbnail() { _thumbnail = null; return this; }

    /// <summary>Removes all fields from the embed.</summary>
    public EmbedBuilder ClearFields() { _fields.Clear(); return this; }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Constructs and returns the <see cref="Embed"/> object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the embed has no content or exceeds Discord's 6000-character total limit.</exception>
    public Embed Build()
    {
        if (_title == null && _description == null && _image == null && _fields.Count == 0 && _author == null)
            throw new InvalidOperationException("An embed must have at least one of: title, description, image, author, or a field.");

        // Enforce Discord's 6000-character total embed length limit
        int total = (_title?.Length ?? 0)
                  + (_description?.Length ?? 0)
                  + (_footer?.Text?.Length ?? 0)
                  + (_author?.Name?.Length ?? 0)
                  + _fields.Sum(f => (f.Name?.Length ?? 0) + (f.Value?.Length ?? 0));

        if (total > MaxTotalLength)
            throw new InvalidOperationException(
                $"Embed total character count ({total}) exceeds Discord's {MaxTotalLength}-character limit. " +
                "Reduce the length of your title, description, fields, footer, or author name.");

        return new Embed
        {
            Title       = _title,
            Description = _description,
            Url         = _url,
            Color       = _color,
            Timestamp   = _timestamp,
            Footer      = _footer,
            Image       = _image,
            Thumbnail   = _thumbnail,
            Author      = _author,
            Fields      = _fields.Count > 0 ? new List<EmbedField>(_fields) : null,
        };
    }
}
