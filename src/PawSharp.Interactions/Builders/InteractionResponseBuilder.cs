#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.API.Models;
using PawSharp.Core.Entities;

namespace PawSharp.Interactions.Builders;

/// <summary>
/// Fluent builder for <see cref="InteractionResponse"/> objects.
/// Handles <c>ChannelMessageWithSource</c> (type 4),
/// <c>DeferredChannelMessageWithSource</c> (type 5),
/// <c>DeferredUpdateMessage</c> (type 6),
/// <c>UpdateMessage</c> (type 7) response types, with optional ephemeral, embeds, content, and components.
/// </summary>
/// <example>
/// <code>
/// var response = new InteractionResponseBuilder()
///     .WithContent("Hello!")
///     .AddEmbed(embedBuilder.Build())
///     .AddActionRow(row => row.AddButton(new ButtonBuilder("btn", "Click me!")))
///     .AsEphemeral()
///     .Build();
///
/// await handler.RespondAsync(interaction.Id, interaction.Token, response);
/// </code>
/// </example>
public sealed class InteractionResponseBuilder
{
    private string? _content;
    private bool _ephemeral;
    private bool _updateMessage;
    private bool _deferredChannelMessage;
    private bool _deferredUpdateMessage;
    private int? _flags;
    private readonly List<Embed> _embeds = new();
    private readonly List<MessageComponent> _actionRows = new();

    // ── Content ───────────────────────────────────────────────────────────────

    /// <summary>Sets the text content of the response message.</summary>
    public InteractionResponseBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }

    // ── Embeds ────────────────────────────────────────────────────────────────

    /// <summary>Appends a pre-built embed to the response.</summary>
    public InteractionResponseBuilder AddEmbed(Embed embed)
    {
        if (_embeds.Count >= 10)
            throw new InvalidOperationException("A response cannot contain more than 10 embeds.");
        _embeds.Add(embed);
        return this;
    }

    // ── Components ────────────────────────────────────────────────────────────

    /// <summary>Appends an already-built <see cref="ActionRowBuilder"/> to the response.</summary>
    public InteractionResponseBuilder AddActionRow(MessageComponent actionRow)
    {
        if (_actionRows.Count >= 5)
            throw new InvalidOperationException("A response cannot contain more than 5 action rows.");
        _actionRows.Add(actionRow);
        return this;
    }

    /// <summary>Builds and appends an action row using a callback.</summary>
    public InteractionResponseBuilder AddActionRow(Action<ActionRowBuilder> configure)
    {
        var builder = new ActionRowBuilder();
        configure(builder);
        return AddActionRow(builder.Build());
    }

    // ── Flags ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks the response as ephemeral — only the user who triggered the interaction will see it.
    /// </summary>
    public InteractionResponseBuilder AsEphemeral(bool ephemeral = true)
    {
        _ephemeral = ephemeral;
        return this;
    }

    /// <summary>
    /// Sets message flags directly (useful for custom flag combinations).
    /// </summary>
    public InteractionResponseBuilder WithFlags(int flags)
    {
        if (_ephemeral) flags |= 64;
        _ephemeral = false;
        return this.WithFlagsInternal(flags);
    }

    private InteractionResponseBuilder WithFlagsInternal(int flags)
    {
        // Store flags to be applied in Build
        _flags = flags;
        return this;
    }

    /// <summary>
    /// Produces an <c>UpdateMessage</c> (type 7) response that edits the original component message
    /// instead of sending a new one. Used in button / select menu handlers.
    /// </summary>
    public InteractionResponseBuilder AsUpdateMessage(bool update = true)
    {
        _updateMessage = update;
        return this;
    }

    /// <summary>
    /// Produces a <c>DeferredChannelMessageWithSource</c> (type 5) response.
    /// ACKs the interaction and shows a loading state to the user.
    /// Use <see cref="InteractionHandler.EditOriginalResponseAsync"/> to edit the response later.
    /// </summary>
    public InteractionResponseBuilder AsDeferredChannelMessage(bool deferred = true)
    {
        _deferredChannelMessage = deferred;
        return this;
    }

    /// <summary>
    /// Produces a <c>DeferredUpdateMessage</c> (type 6) response.
    /// For component interactions: ACKs the interaction without showing a loading state.
    /// Use <see cref="InteractionHandler.EditOriginalResponseAsync"/> to edit the message later.
    /// </summary>
    public InteractionResponseBuilder AsDeferredUpdateMessage(bool deferred = true)
    {
        _deferredUpdateMessage = deferred;
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>Constructs the <see cref="InteractionResponse"/>.</summary>
    public InteractionResponse Build()
    {
        int type;
        if (_deferredChannelMessage)
            type = 5;  // DeferredChannelMessageWithSource
        else if (_deferredUpdateMessage)
            type = 6;  // DeferredUpdateMessage
        else if (_updateMessage)
            type = 7;  // UpdateMessage
        else
            type = 4;  // ChannelMessageWithSource

        // Deferred responses (types 5 and 6) should only send flags in data (for ephemeral).
        // Discord rejects deferred responses that include content/embeds/components.
        bool isDeferred = type is 5 or 6;

        var data = new InteractionCallbackData
        {
            Content    = isDeferred ? null : _content,
            Embeds     = isDeferred ? null : (_embeds.Count > 0 ? new List<Embed>(_embeds) : null),
            Components = isDeferred ? null : (_actionRows.Count > 0 ? new List<MessageComponent>(_actionRows) : null),
            Flags      = _flags ?? (_ephemeral ? 64 : null),
        };

        return new InteractionResponse { Type = type, Data = data };
    }
}
