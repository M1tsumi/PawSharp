#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PawSharp.Core.Entities;

// ── Component Type Enums ─────────────────────────────────────────────────────

/// <summary>The type of a message component.</summary>
public enum ComponentType
{
    ActionRow          = 1,
    Button             = 2,
    StringSelect       = 3,
    TextInput          = 4,
    UserSelect         = 5,
    RoleSelect         = 6,
    MentionableSelect  = 7,
    ChannelSelect      = 8,
    // Components v2 (released 2025)
    Section            = 9,
    TextDisplay        = 10,
    Thumbnail          = 11,
    MediaGallery       = 12,
    File               = 13,
    Separator          = 14,
    Container          = 17,
}

/// <summary>Spacing size for a <see cref="Separator"/> component.</summary>
public enum SeparatorSpacing
{
    Small = 1,
    Large = 2,
}

/// <summary>Visual style of a button component.</summary>
public enum ButtonStyle
{
    Primary   = 1,
    Secondary = 2,
    Success   = 3,
    Danger    = 4,
    Link      = 5,
    Premium   = 6,
}

/// <summary>Text input style for modal components.</summary>
public enum TextInputStyle
{
    Short     = 1,
    Paragraph = 2,
}

// ── Polymorphic JSON Converter ────────────────────────────────────────────────

/// <summary>
/// Dispatches deserialization of message components to the correct concrete type
/// based on the <c>type</c> integer discriminator field.
/// </summary>
public sealed class MessageComponentJsonConverter : JsonConverter<MessageComponent>
{
    public override MessageComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            return null;

        var raw  = root.GetRawText();
        var type = typeProp.GetInt32();

        return (ComponentType)type switch
        {
            ComponentType.ActionRow         => JsonSerializer.Deserialize<ActionRow>(raw, options),
            ComponentType.Button            => JsonSerializer.Deserialize<Button>(raw, options),
            ComponentType.StringSelect      => JsonSerializer.Deserialize<SelectMenu>(raw, options),
            ComponentType.TextInput         => JsonSerializer.Deserialize<TextInput>(raw, options),
            ComponentType.UserSelect        => JsonSerializer.Deserialize<UserSelectMenu>(raw, options),
            ComponentType.RoleSelect        => JsonSerializer.Deserialize<RoleSelectMenu>(raw, options),
            ComponentType.MentionableSelect => JsonSerializer.Deserialize<MentionableSelectMenu>(raw, options),
            ComponentType.ChannelSelect     => JsonSerializer.Deserialize<ChannelSelectMenu>(raw, options),
            // Components v2
            ComponentType.Section           => JsonSerializer.Deserialize<Section>(raw, options),
            ComponentType.TextDisplay       => JsonSerializer.Deserialize<TextDisplay>(raw, options),
            ComponentType.Thumbnail         => JsonSerializer.Deserialize<ThumbnailComponent>(raw, options),
            ComponentType.MediaGallery      => JsonSerializer.Deserialize<MediaGallery>(raw, options),
            ComponentType.File              => JsonSerializer.Deserialize<FileComponent>(raw, options),
            ComponentType.Separator         => JsonSerializer.Deserialize<Separator>(raw, options),
            ComponentType.Container         => JsonSerializer.Deserialize<Container>(raw, options),
            _                               => JsonSerializer.Deserialize<UnknownComponent>(raw, options),
        };
    }

    public override void Write(Utf8JsonWriter writer, MessageComponent value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}

// ── Base ─────────────────────────────────────────────────────────────────────

/// <summary>Base class for all Discord message component types.</summary>
[JsonConverter(typeof(MessageComponentJsonConverter))]
public abstract class MessageComponent
{
    /// <summary>Component type discriminator.</summary>
    [JsonPropertyName("type")]
    public ComponentType Type { get; set; }
}

// ── ActionRow ─────────────────────────────────────────────────────────────────

/// <summary>
/// Container component. Holds up to 5 non-ActionRow components.
/// </summary>
public class ActionRow : MessageComponent
{
    public ActionRow() => Type = ComponentType.ActionRow;

    /// <summary>Child components (Buttons, select menus, or text inputs).</summary>
    [JsonPropertyName("components")]
    public List<MessageComponent> Components { get; set; } = new();
}

// ── Button ────────────────────────────────────────────────────────────────────

/// <summary>Interactive button component.</summary>
public class Button : MessageComponent
{
    public Button() => Type = ComponentType.Button;

    /// <summary>Button visual style.</summary>
    [JsonPropertyName("style")]
    public ButtonStyle Style { get; set; }

    /// <summary>Text that appears on the button (max 80 characters).</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    /// <summary>Partial emoji to display on the button.</summary>
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }

    /// <summary>
    /// Developer-defined identifier used in interaction payloads (max 100 char).
    /// Required for non-link and non-premium buttons.
    /// </summary>
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }

    /// <summary>URL opened when the button is clicked. Required for Link buttons.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>SKU ID for Premium buttons.</summary>
    [JsonPropertyName("sku_id")]
    public ulong? SkuId { get; set; }

    /// <summary>Whether the button is currently disabled.</summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }
}

// ── Select Menu base ──────────────────────────────────────────────────────────

/// <summary>
/// Shared properties for all select menu variants.
/// </summary>
public abstract class SelectMenuBase : MessageComponent
{
    /// <summary>Developer-defined identifier (max 100 characters).</summary>
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; } = string.Empty;

    /// <summary>Placeholder text when nothing is selected (max 150 characters).</summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>Minimum number of items that must be chosen (0–25). Default 1.</summary>
    [JsonPropertyName("min_values")]
    public int? MinValues { get; set; }

    /// <summary>Maximum number of items that can be chosen (1–25). Default 1.</summary>
    [JsonPropertyName("max_values")]
    public int? MaxValues { get; set; }

    /// <summary>Whether the select menu is currently disabled.</summary>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }

    /// <summary>Default selected values for auto-populated select menus.</summary>
    [JsonPropertyName("default_values")]
    public List<SelectDefaultValue>? DefaultValues { get; set; }
}

// ── String Select ─────────────────────────────────────────────────────────────

/// <summary>
/// String-based select menu (type 3). Also aliased as <see cref="StringSelectMenu"/>.
/// </summary>
public class SelectMenu : SelectMenuBase
{
    public SelectMenu() => Type = ComponentType.StringSelect;

    /// <summary>Choices available in this menu (max 25).</summary>
    [JsonPropertyName("options")]
    public List<SelectOption> Options { get; set; } = new();
}

/// <summary>Alias for <see cref="SelectMenu"/> for clarity.</summary>
public class StringSelectMenu : SelectMenu
{
    public StringSelectMenu() { }
}

// ── Auto-populated Select Menus ───────────────────────────────────────────────

/// <summary>Select from users in the guild (type 5).</summary>
public class UserSelectMenu : SelectMenuBase
{
    public UserSelectMenu() => Type = ComponentType.UserSelect;
}

/// <summary>Select from roles in the guild (type 6).</summary>
public class RoleSelectMenu : SelectMenuBase
{
    public RoleSelectMenu() => Type = ComponentType.RoleSelect;
}

/// <summary>Select from users and roles in the guild (type 7).</summary>
public class MentionableSelectMenu : SelectMenuBase
{
    public MentionableSelectMenu() => Type = ComponentType.MentionableSelect;
}

/// <summary>Select from channels in the guild (type 8).</summary>
public class ChannelSelectMenu : SelectMenuBase
{
    public ChannelSelectMenu() => Type = ComponentType.ChannelSelect;

    /// <summary>Specific channel types to include in the list.</summary>
    [JsonPropertyName("channel_types")]
    public List<int>? ChannelTypes { get; set; }
}

// ── TextInput ─────────────────────────────────────────────────────────────────

/// <summary>Text input component used inside modal dialogs (type 4).</summary>
public class TextInput : MessageComponent
{
    public TextInput() => Type = ComponentType.TextInput;

    /// <summary>Developer-defined identifier (max 100 characters).</summary>
    [JsonPropertyName("custom_id")]
    public string CustomId { get; set; } = string.Empty;

    /// <summary>Whether the input is single-line (Short) or multi-line (Paragraph).</summary>
    [JsonPropertyName("style")]
    public TextInputStyle Style { get; set; } = TextInputStyle.Short;

    /// <summary>Label above the text input (max 45 characters).</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Minimum input length (0–4000).</summary>
    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }

    /// <summary>Maximum input length (1–4000).</summary>
    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    /// <summary>Whether this component is required. Defaults to true.</summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>Pre-filled value for the text input (max 4000 characters).</summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>Placeholder text when the input is empty (max 100 characters).</summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }
}

// ── Unknown Component ─────────────────────────────────────────────────────────

/// <summary>Fallback for component types not yet known to this library.</summary>
public class UnknownComponent : MessageComponent { }

// ── Shared Sub-objects ────────────────────────────────────────────────────────

/// <summary>One option shown inside a string select menu.</summary>
public class SelectOption
{
    /// <summary>User-facing name (max 100 characters).</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    /// <summary>Developer-defined value returned in the interaction payload (max 100 characters).</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>Additional description shown beneath the label (max 100 characters).</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Partial emoji rendered alongside the label.</summary>
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }

    /// <summary>Whether this option is pre-selected by default.</summary>
    [JsonPropertyName("default")]
    public bool? Default { get; set; }
}

/// <summary>Default value for auto-populated select menus.</summary>
public class SelectDefaultValue
{
    /// <summary>Snowflake ID of the default user/role/channel.</summary>
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    /// <summary>Type of the default value: "user", "role", or "channel".</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

// ── Components v2 ─────────────────────────────────────────────────────────────

/// <summary>
/// Media item used in <see cref="ThumbnailComponent"/>, <see cref="MediaGallery"/>,
/// and <see cref="FileComponent"/> components.
/// </summary>
public class UnfurledMediaItem
{
    /// <summary>The URL of the media. Can be a discord CDN URL, attachment://, or external URL.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Proxied URL of the media (set by Discord).</summary>
    [JsonPropertyName("proxy_url")]
    public string? ProxyUrl { get; set; }

    /// <summary>Height of the media in pixels.</summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>Width of the media in pixels.</summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>Media content type.</summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
}

/// <summary>
/// One item inside a <see cref="MediaGallery"/> component.
/// </summary>
public class MediaGalleryItem
{
    /// <summary>The media for this item.</summary>
    [JsonPropertyName("media")]
    public UnfurledMediaItem Media { get; set; } = new();

    /// <summary>Optional description (alt-text) for the media item.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether this item is a spoiler (blurred until clicked).</summary>
    [JsonPropertyName("spoiler")]
    public bool? Spoiler { get; set; }
}

// ── Section (type 9) ─────────────────────────────────────────────────────────

/// <summary>
/// A Section component (type 9) groups TextDisplay child components alongside
/// an optional right-side accessory (Button or Thumbnail).
/// Only valid as a top-level component inside a Container.
/// </summary>
public class Section : MessageComponent
{
    public Section() => Type = ComponentType.Section;

    /// <summary>
    /// Child components — must contain only <see cref="TextDisplay"/> components.
    /// </summary>
    [JsonPropertyName("components")]
    public List<MessageComponent> Components { get; set; } = new();

    /// <summary>
    /// Optional right-side accessory — a <see cref="Button"/> or <see cref="ThumbnailComponent"/>.
    /// </summary>
    [JsonPropertyName("accessory")]
    public MessageComponent? Accessory { get; set; }
}

// ── TextDisplay (type 10) ─────────────────────────────────────────────────────

/// <summary>
/// A TextDisplay component (type 10) renders markdown text inside a Section or Container.
/// </summary>
public class TextDisplay : MessageComponent
{
    public TextDisplay() => Type = ComponentType.TextDisplay;

    /// <summary>Text content supporting Discord markdown (max 4000 characters).</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

// ── Thumbnail (type 11) ───────────────────────────────────────────────────────

/// <summary>
/// A Thumbnail component (type 11) shows an image; used as the accessory in a Section.
/// </summary>
public class ThumbnailComponent : MessageComponent
{
    public ThumbnailComponent() => Type = ComponentType.Thumbnail;

    /// <summary>The media to display.</summary>
    [JsonPropertyName("media")]
    public UnfurledMediaItem Media { get; set; } = new();

    /// <summary>Optional alt text / description for the image.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether to spoiler (blur) the image.</summary>
    [JsonPropertyName("spoiler")]
    public bool? Spoiler { get; set; }
}

// ── MediaGallery (type 12) ────────────────────────────────────────────────────

/// <summary>
/// A MediaGallery component (type 12) displays a sorted collection of media attachments.
/// </summary>
public class MediaGallery : MessageComponent
{
    public MediaGallery() => Type = ComponentType.MediaGallery;

    /// <summary>Items to display (1–10 items).</summary>
    [JsonPropertyName("items")]
    public List<MediaGalleryItem> Items { get; set; } = new();
}

// ── File (type 13) ────────────────────────────────────────────────────────────

/// <summary>
/// A File component (type 13) renders a file attachment inline in the message.
/// </summary>
public class FileComponent : MessageComponent
{
    public FileComponent() => Type = ComponentType.File;

    /// <summary>
    /// Reference to the attachment file.
    /// Use an <c>attachment://</c> URL to reference one of the message's attachments.
    /// </summary>
    [JsonPropertyName("file")]
    public UnfurledMediaItem File { get; set; } = new();

    /// <summary>Whether the file should be a spoiler.</summary>
    [JsonPropertyName("spoiler")]
    public bool? Spoiler { get; set; }
}

// ── Separator (type 14) ───────────────────────────────────────────────────────

/// <summary>
/// A Separator component (type 14) adds a visual divider between other components.
/// </summary>
public class Separator : MessageComponent
{
    public Separator() => Type = ComponentType.Separator;

    /// <summary>Whether to render a visible dividing line. Defaults to true.</summary>
    [JsonPropertyName("divider")]
    public bool? Divider { get; set; }

    /// <summary>Spacing above / below the separator.</summary>
    [JsonPropertyName("spacing")]
    public SeparatorSpacing? Spacing { get; set; }
}

// ── Container (type 17) ───────────────────────────────────────────────────────

/// <summary>
/// A Container component (type 17) is the top-level grouping element for Components v2.
/// It can contain ActionRows (for interactive elements), Sections, MediaGalleries,
/// Files, Separators, and TextDisplays.
/// </summary>
public class Container : MessageComponent
{
    public Container() => Type = ComponentType.Container;

    /// <summary>Child components.</summary>
    [JsonPropertyName("components")]
    public List<MessageComponent> Components { get; set; } = new();

    /// <summary>Optional side-bar accent colour (integer, same format as role/embed colours).</summary>
    [JsonPropertyName("accent_color")]
    public int? AccentColor { get; set; }

    /// <summary>Whether the entire container is a spoiler.</summary>
    [JsonPropertyName("spoiler")]
    public bool? Spoiler { get; set; }
}
