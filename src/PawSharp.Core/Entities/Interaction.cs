#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord interaction.
/// </summary>
public class Interaction : DiscordEntity
{
    /// <summary>
    /// ID of the application this interaction is for.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }
    
    /// <summary>
    /// The type of interaction.
    /// </summary>
    [JsonPropertyName("type")]
    public InteractionType Type { get; set; }
    
    /// <summary>
    /// The command data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public InteractionData? Data { get; set; }
    
    /// <summary>
    /// The guild it was sent from.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// The channel it was sent from.
    /// </summary>
    [JsonPropertyName("channel_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ChannelId { get; set; }
    
    /// <summary>
    /// Guild member data for the invoking user, including permissions.
    /// </summary>
    [JsonPropertyName("member")]
    public GuildMember? Member { get; set; }
    
    /// <summary>
    /// User object for the invoking user, if invoked in a DM.
    /// </summary>
    [JsonPropertyName("user")]
    public User? User { get; set; }
    
    /// <summary>
    /// A continuation token for responding to the interaction.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Read-only property, always 1.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }
    
    /// <summary>
    /// For components, the message they were attached to.
    /// </summary>
    [JsonPropertyName("message")]
    public Message? Message { get; set; }
    
    /// <summary>
    /// Bitwise set of permissions the app or bot has within the channel the interaction was sent from.
    /// </summary>
    [JsonPropertyName("app_permissions")]
    public string? AppPermissions { get; set; }
    
    /// <summary>
    /// Selected language of the invoking user.
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
    
    /// <summary>
    /// Guild's preferred locale, if invoked in a guild.
    /// </summary>
    [JsonPropertyName("guild_locale")]
    public string? GuildLocale { get; set; }
}

/// <summary>
/// Interaction data payload.
/// </summary>
public class InteractionData
{
    /// <summary>
    /// The ID of the invoked command.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }
    
    /// <summary>
    /// The name of the invoked command.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of the invoked command.
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    /// <summary>
    /// The params + values from the user.
    /// </summary>
    [JsonPropertyName("options")]
    public List<ApplicationCommandInteractionDataOption>? Options { get; set; }
    
    /// <summary>
    /// The custom_id of the component.
    /// </summary>
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }
    
    /// <summary>
    /// The type of the component.
    /// </summary>
    [JsonPropertyName("component_type")]
    public int? ComponentType { get; set; }
    
    /// <summary>
    /// The values the user selected.
    /// </summary>
    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }
    
    /// <summary>
    /// ID of the user or message targeted by a user or message command.
    /// </summary>
    [JsonPropertyName("target_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? TargetId { get; set; }
    
    /// <summary>
    /// For monetized apps, any entitlements for the invoking user.
    /// </summary>
    [JsonPropertyName("entitlements")]
    public List<object>? Entitlements { get; set; }
}

/// <summary>
/// Application command interaction data option.
/// </summary>
public class ApplicationCommandInteractionDataOption
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Value of application command option type.
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }
    
    /// <summary>
    /// The value of the option resulting from user input.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
    
    /// <summary>
    /// Present if this option is a group or subcommand.
    /// </summary>
    [JsonPropertyName("options")]
    public List<ApplicationCommandInteractionDataOption>? Options { get; set; }
    
    /// <summary>
    /// True if this option is the currently focused option for autocomplete.
    /// </summary>
    [JsonPropertyName("focused")]
    public bool? Focused { get; set; }
}

/// <summary>
/// Interaction type enum.
/// </summary>
public enum InteractionType
{
    Ping = 1,
    ApplicationCommand = 2,
    MessageComponent = 3,
    ApplicationCommandAutocomplete = 4,
    ModalSubmit = 5
}