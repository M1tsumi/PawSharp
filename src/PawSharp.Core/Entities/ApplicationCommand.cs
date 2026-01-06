#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents a Discord application command.
/// </summary>
public class ApplicationCommand : DiscordEntity
{
    /// <summary>
    /// The type of command, defaults to 1.
    /// </summary>
    [JsonPropertyName("type")]
    public ApplicationCommandType Type { get; set; } = ApplicationCommandType.ChatInput;
    
    /// <summary>
    /// ID of the parent application.
    /// </summary>
    [JsonPropertyName("application_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong ApplicationId { get; set; }
    
    /// <summary>
    /// Guild id of the command, if not global.
    /// </summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(NullableSnowflakeJsonConverter))]
    public ulong? GuildId { get; set; }
    
    /// <summary>
    /// Name of command, 1-32 characters.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description for CHAT_INPUT commands, 1-100 characters. Empty string for USER and MESSAGE commands.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The parameters for the command, max of 25.
    /// </summary>
    [JsonPropertyName("options")]
    public List<ApplicationCommandOption>? Options { get; set; }
    
    /// <summary>
    /// Set of permissions represented as a bit set.
    /// </summary>
    [JsonPropertyName("default_member_permissions")]
    public string? DefaultMemberPermissions { get; set; }
    
    /// <summary>
    /// Indicates whether the command is available in DMs with the app, only for globally-scoped commands.
    /// </summary>
    [JsonPropertyName("dm_permission")]
    public bool? DmPermission { get; set; }
    
    /// <summary>
    /// Indicates whether the command is enabled by default when the app is added to a guild.
    /// </summary>
    [JsonPropertyName("default_permission")]
    public bool? DefaultPermission { get; set; }
    
    /// <summary>
    /// Indicates whether the command is age-restricted.
    /// </summary>
    [JsonPropertyName("nsfw")]
    public bool? Nsfw { get; set; }
    
    /// <summary>
    /// Autoincrementing version identifier updated during substantial record changes.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Version { get; set; }
}

/// <summary>
/// Application command option.
/// </summary>
public class ApplicationCommandOption
{
    /// <summary>
    /// The type of option.
    /// </summary>
    [JsonPropertyName("type")]
    public ApplicationCommandOptionType Type { get; set; }
    
    /// <summary>
    /// 1-32 character name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 1-100 character description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// If the parameter is required or optional--default false.
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }
    
    /// <summary>
    /// Choices for STRING, INTEGER, and NUMBER types for the user to pick from, max 25.
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ApplicationCommandOptionChoice>? Choices { get; set; }
    
    /// <summary>
    /// If the option is a subcommand or subcommand group type, these nested options will be the parameters.
    /// </summary>
    [JsonPropertyName("options")]
    public List<ApplicationCommandOption>? Options { get; set; }
    
    /// <summary>
    /// If the option is a channel type, the channels shown will be restricted to these types.
    /// </summary>
    [JsonPropertyName("channel_types")]
    public List<int>? ChannelTypes { get; set; }
    
    /// <summary>
    /// For integer options, the minimum value permitted.
    /// </summary>
    [JsonPropertyName("min_value")]
    public object? MinValue { get; set; }
    
    /// <summary>
    /// For integer options, the maximum value permitted.
    /// </summary>
    [JsonPropertyName("max_value")]
    public object? MaxValue { get; set; }
    
    /// <summary>
    /// For string options, the minimum allowed length (minimum of 0, maximum of 6000).
    /// </summary>
    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }
    
    /// <summary>
    /// For string options, the maximum allowed length (minimum of 1, maximum of 6000).
    /// </summary>
    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }
    
    /// <summary>
    /// If autocomplete interactions are enabled for this STRING, INTEGER, or NUMBER type option.
    /// </summary>
    [JsonPropertyName("autocomplete")]
    public bool? Autocomplete { get; set; }
}

/// <summary>
/// Application command option choice.
/// </summary>
public class ApplicationCommandOptionChoice
{
    /// <summary>
    /// 1-100 character choice name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Value for the choice, up to 100 characters if string.
    /// </summary>
    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;
    
    /// <summary>
    /// Localizations object for the name field.
    /// </summary>
    [JsonPropertyName("name_localizations")]
    public Dictionary<string, string>? NameLocalizations { get; set; }
}

/// <summary>
/// Application command type.
/// </summary>
public enum ApplicationCommandType
{
    ChatInput = 1,
    User = 2,
    Message = 3
}

/// <summary>
/// Application command option type.
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