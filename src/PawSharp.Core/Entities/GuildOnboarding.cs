#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PawSharp.Core.Serialization;

namespace PawSharp.Core.Entities;

/// <summary>
/// Represents the onboarding flow for a guild.
/// </summary>
public class GuildOnboarding
{
    /// <summary>ID of the guild this onboarding is part of.</summary>
    [JsonPropertyName("guild_id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong GuildId { get; set; }

    /// <summary>Prompts shown during onboarding and in customize community.</summary>
    [JsonPropertyName("prompts")]
    public List<OnboardingPrompt> Prompts { get; set; } = new();

    /// <summary>Channel IDs that members get opted into automatically.</summary>
    [JsonPropertyName("default_channel_ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> DefaultChannelIds { get; set; } = new();

    /// <summary>Whether onboarding is enabled for the guild.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Current mode of onboarding, defines the criteria used to satisfy the constraints.</summary>
    [JsonPropertyName("mode")]
    public OnboardingMode Mode { get; set; }
}

/// <summary>
/// Represents a prompt in the guild onboarding flow.
/// </summary>
public class OnboardingPrompt
{
    /// <summary>ID of the prompt.</summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>Type of prompt.</summary>
    [JsonPropertyName("type")]
    public OnboardingPromptType Type { get; set; }

    /// <summary>Options available within the prompt.</summary>
    [JsonPropertyName("options")]
    public List<OnboardingPromptOption> Options { get; set; } = new();

    /// <summary>Title of the prompt.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Indicates whether users are limited to selecting one option for the prompt.</summary>
    [JsonPropertyName("single_select")]
    public bool SingleSelect { get; set; }

    /// <summary>Indicates whether the prompt is required before a user completes the onboarding flow.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>Indicates whether the prompt is present in the onboarding flow. False means not shown to users.</summary>
    [JsonPropertyName("in_onboarding")]
    public bool InOnboarding { get; set; }
}

/// <summary>
/// Represents an option within an onboarding prompt.
/// </summary>
public class OnboardingPromptOption
{
    /// <summary>ID of the prompt option.</summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(SnowflakeJsonConverter))]
    public ulong Id { get; set; }

    /// <summary>IDs for channels a member is added to when the option is selected.</summary>
    [JsonPropertyName("channel_ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> ChannelIds { get; set; } = new();

    /// <summary>IDs for roles assigned to a member when the option is selected.</summary>
    [JsonPropertyName("role_ids")]
    [JsonConverter(typeof(SnowflakeListJsonConverter))]
    public List<ulong> RoleIds { get; set; } = new();

    /// <summary>Emoji of the option. Currently only supports custom emoji in the guild.</summary>
    [JsonPropertyName("emoji")]
    public Emoji? Emoji { get; set; }

    /// <summary>Title of the option.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Description of the option.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Defines the criteria used to satisfy the constraints of an onboarding prompt.
/// </summary>
public enum OnboardingMode
{
    /// <summary>Counts only Default Channels towards constraints.</summary>
    OnboardingDefault = 0,

    /// <summary>Counts Default Channels and Questions towards constraints.</summary>
    OnboardingAdvanced = 1
}

/// <summary>
/// Type of an onboarding prompt.
/// </summary>
public enum OnboardingPromptType
{
    MultipleChoice = 0,
    Dropdown = 1
}
