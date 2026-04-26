#nullable enable
namespace PawSharp.Core.Validation;

/// <summary>
/// Discord API limits and constraints for validation.
/// </summary>
public static class DiscordLimits
{
    // ── Message Limits ────────────────────────────────────────────────────────
    
    /// <summary>Maximum message content length.</summary>
    public const int MaxMessageContentLength = 2000;
    
    /// <summary>Maximum message title length.</summary>
    public const int MaxMessageTitleLength = 2000;
    
    // ── Embed Limits ─────────────────────────────────────────────────────────
    
    /// <summary>Maximum embed title length.</summary>
    public const int MaxEmbedTitleLength = 256;
    
    /// <summary>Maximum embed description length.</summary>
    public const int MaxEmbedDescriptionLength = 4096;
    
    /// <summary>Maximum number of embed fields.</summary>
    public const int MaxEmbedFieldCount = 25;
    
    /// <summary>Maximum embed field name length.</summary>
    public const int MaxEmbedFieldNameLength = 256;
    
    /// <summary>Maximum embed field value length.</summary>
    public const int MaxEmbedFieldValueLength = 1024;
    
    /// <summary>Maximum embed footer text length.</summary>
    public const int MaxEmbedFooterTextLength = 2048;
    
    /// <summary>Maximum embed author name length.</summary>
    public const int MaxEmbedAuthorNameLength = 256;
    
    /// <summary>Maximum total embed character count.</summary>
    public const int MaxEmbedTotalLength = 6000;
    
    // ── Component Limits ──────────────────────────────────────────────────────
    
    /// <summary>Maximum components per ActionRow.</summary>
    public const int MaxComponentsPerActionRow = 5;
    
    /// <summary>Maximum ActionRows per message.</summary>
    public const int MaxActionRowsPerMessage = 5;
    
    /// <summary>Maximum button label length.</summary>
    public const int MaxButtonLabelLength = 80;
    
    /// <summary>Maximum button custom ID length.</summary>
    public const int MaxButtonCustomIdLength = 100;
    
    /// <summary>Maximum select menu custom ID length.</summary>
    public const int MaxSelectMenuCustomIdLength = 100;
    
    /// <summary>Maximum select menu placeholder length.</summary>
    public const int MaxSelectMenuPlaceholderLength = 150;
    
    /// <summary>Maximum select menu options.</summary>
    public const int MaxSelectMenuOptions = 25;
    
    /// <summary>Maximum select menu option label length.</summary>
    public const int MaxSelectMenuOptionLabelLength = 100;
    
    /// <summary>Maximum select menu option value length.</summary>
    public const int MaxSelectMenuOptionValueLength = 100;
    
    /// <summary>Maximum select menu option description length.</summary>
    public const int MaxSelectMenuOptionDescriptionLength = 100;
    
    /// <summary>Maximum select menu minimum values.</summary>
    public const int MaxSelectMenuMinValues = 25;
    
    /// <summary>Maximum select menu maximum values.</summary>
    public const int MaxSelectMenuMaxValues = 25;
    
    /// <summary>Maximum text input custom ID length.</summary>
    public const int MaxTextInputCustomIdLength = 100;
    
    /// <summary>Maximum text input label length.</summary>
    public const int MaxTextInputLabelLength = 45;
    
    /// <summary>Maximum text input placeholder length.</summary>
    public const int MaxTextInputPlaceholderLength = 100;
    
    /// <summary>Maximum text input value length.</summary>
    public const int MaxTextInputValueLength = 4000;
    
    /// <summary>Maximum text input minimum length.</summary>
    public const int MaxTextInputMinLength = 4000;
    
    /// <summary>Maximum text input maximum length.</summary>
    public const int MaxTextInputMaxLength = 4000;
    
    /// <summary>Maximum text display content length.</summary>
    public const int MaxTextDisplayContentLength = 4000;
    
    /// <summary>Maximum media gallery items.</summary>
    public const int MaxMediaGalleryItems = 10;
    
    /// <summary>Minimum media gallery items.</summary>
    public const int MinMediaGalleryItems = 1;
    
    // ── Application Command Limits ───────────────────────────────────────────────
    
    /// <summary>Maximum command name length.</summary>
    public const int MaxCommandNameLength = 32;
    
    /// <summary>Minimum command name length.</summary>
    public const int MinCommandNameLength = 1;
    
    /// <summary>Maximum command description length.</summary>
    public const int MaxCommandDescriptionLength = 100;
    
    /// <summary>Maximum command options.</summary>
    public const int MaxCommandOptions = 25;
    
    /// <summary>Maximum command option name length.</summary>
    public const int MaxCommandOptionNameLength = 32;
    
    /// <summary>Maximum command option description length.</summary>
    public const int MaxCommandOptionDescriptionLength = 100;
    
    /// <summary>Maximum command option choices.</summary>
    public const int MaxCommandOptionChoices = 25;
    
    /// <summary>Maximum command option choice name length.</summary>
    public const int MaxCommandOptionChoiceNameLength = 100;
    
    /// <summary>Maximum command option choice value length (string).</summary>
    public const int MaxCommandOptionChoiceValueLength = 100;
    
    /// <summary>Maximum command option minimum string length.</summary>
    public const int MaxCommandOptionMinStringLength = 6000;
    
    /// <summary>Maximum command option maximum string length.</summary>
    public const int MaxCommandOptionMaxStringLength = 6000;
    
    // ── Channel Limits ────────────────────────────────────────────────────────
    
    /// <summary>Maximum channel name length.</summary>
    public const int MaxChannelNameLength = 100;
    
    /// <summary>Minimum channel name length.</summary>
    public const int MinChannelNameLength = 1;
    
    /// <summary>Maximum channel topic length.</summary>
    public const int MaxChannelTopicLength = 1024;
    
    /// <summary>Maximum channel rate limit per user (slowmode).</summary>
    public const int MaxChannelRateLimitPerUser = 21600;
    
    /// <summary>Maximum voice channel bitrate.</summary>
    public const int MaxVoiceBitrate = 96000;
    
    /// <summary>Maximum voice channel user limit.</summary>
    public const int MaxVoiceUserLimit = 99;
    
    // ── Guild Limits ──────────────────────────────────────────────────────────
    
    /// <summary>Maximum guild name length.</summary>
    public const int MaxGuildNameLength = 100;
    
    /// <summary>Minimum guild name length.</summary>
    public const int MinGuildNameLength = 2;
    
    /// <summary>Maximum guild description length.</summary>
    public const int MaxGuildDescriptionLength = 1000;
    
    // ── URL Limits ────────────────────────────────────────────────────────────
    
    /// <summary>Maximum URL length.</summary>
    public const int MaxUrlLength = 2048;
    
    // ── Interaction Limits ────────────────────────────────────────────────────
    
    /// <summary>Interaction response deadline in milliseconds.</summary>
    public const int InteractionResponseDeadlineMs = 3000;
    
    /// <summary>Ephemeral message response deadline in milliseconds.</summary>
    public const int EphemeralResponseDeadlineMs = 3000;
}
