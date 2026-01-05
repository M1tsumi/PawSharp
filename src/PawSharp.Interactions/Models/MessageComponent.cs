using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PawSharp.Interactions.Models;

/// <summary>
/// Component types.
/// </summary>
public enum ComponentType
{
    ActionRow = 1,
    Button = 2,
    StringSelect = 3,
    TextInput = 4,
    UserSelect = 5,
    RoleSelect = 6,
    MentionableSelect = 7,
    ChannelSelect = 8
}

/// <summary>
/// Button styles.
/// </summary>
public enum ButtonStyle
{
    Primary = 1,
    Secondary = 2,
    Success = 3,
    Danger = 4,
    Link = 5
}

/// <summary>
/// Represents a message component.
/// </summary>
public class MessageComponent
{
    [JsonPropertyName("type")]
    public ComponentType Type { get; set; }
    
    [JsonPropertyName("custom_id")]
    public string? CustomId { get; set; }
    
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }
    
    [JsonPropertyName("style")]
    public int? Style { get; set; }
    
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    
    [JsonPropertyName("emoji")]
    public object? Emoji { get; set; }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; }
    
    [JsonPropertyName("options")]
    public List<SelectMenuOption>? Options { get; set; }
    
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }
    
    [JsonPropertyName("min_values")]
    public int? MinValues { get; set; }
    
    [JsonPropertyName("max_values")]
    public int? MaxValues { get; set; }
    
    [JsonPropertyName("components")]
    public List<MessageComponent>? Components { get; set; }
}

/// <summary>
/// Represents a select menu option.
/// </summary>
public class SelectMenuOption
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("emoji")]
    public object? Emoji { get; set; }
    
    [JsonPropertyName("default")]
    public bool? Default { get; set; }
}
