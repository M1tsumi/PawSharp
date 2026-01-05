using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PawSharp.Interactions.Models;

/// <summary>
/// Application command types.
/// </summary>
public enum ApplicationCommandType
{
    /// <summary>
    /// Slash commands; a text-based command that shows up when a user types /
    /// </summary>
    ChatInput = 1,
    
    /// <summary>
    /// A UI-based command that shows up when you right click or tap on a user
    /// </summary>
    User = 2,
    
    /// <summary>
    /// A UI-based command that shows up when you right click or tap on a message
    /// </summary>
    Message = 3
}

/// <summary>
/// Application command option types.
/// </summary>
public enum ApplicationCommandOptionType
{
    SubCommand = 1,
    SubCommandGroup = 2,
    String = 3,
    Integer = 4,
    Boolean = 5,
    User = 6,
    Channel = 7,
    Role = 8,
    Mentionable = 9,
    Number = 10,
    Attachment = 11
}

/// <summary>
/// Represents an application command.
/// </summary>
public class ApplicationCommand
{
    [JsonPropertyName("id")]
    public ulong? Id { get; set; }
    
    [JsonPropertyName("type")]
    public ApplicationCommandType Type { get; set; } = ApplicationCommandType.ChatInput;
    
    [JsonPropertyName("application_id")]
    public ulong? ApplicationId { get; set; }
    
    [JsonPropertyName("guild_id")]
    public ulong? GuildId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("options")]
    public List<ApplicationCommandOption>? Options { get; set; }
    
    [JsonPropertyName("default_member_permissions")]
    public string? DefaultMemberPermissions { get; set; }
    
    [JsonPropertyName("dm_permission")]
    public bool? DmPermission { get; set; }
    
    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }
}

/// <summary>
/// Represents a command option.
/// </summary>
public class ApplicationCommandOption
{
    [JsonPropertyName("type")]
    public ApplicationCommandOptionType Type { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("required")]
    public bool? Required { get; set; }
    
    [JsonPropertyName("choices")]
    public List<ApplicationCommandOptionChoice>? Choices { get; set; }
    
    [JsonPropertyName("options")]
    public List<ApplicationCommandOption>? Options { get; set; }
    
    [JsonPropertyName("min_value")]
    public double? MinValue { get; set; }
    
    [JsonPropertyName("max_value")]
    public double? MaxValue { get; set; }
    
    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }
    
    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }
    
    [JsonPropertyName("autocomplete")]
    public bool? Autocomplete { get; set; }
}

/// <summary>
/// Represents a command option choice.
/// </summary>
public class ApplicationCommandOptionChoice
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public object Value { get; set; } = null!;
}
