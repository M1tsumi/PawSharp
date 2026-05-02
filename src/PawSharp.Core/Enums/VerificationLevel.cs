namespace PawSharp.Core.Enums;

/// <summary>
/// Represents the verification level required for a guild.
/// </summary>
public enum VerificationLevel
{
    /// <summary>None - unrestricted.</summary>
    None = 0,

    /// <summary>Low - must have verified email on account.</summary>
    Low = 1,

    /// <summary>Medium - must be registered on Discord for longer than 5 minutes.</summary>
    Medium = 2,

    /// <summary>High - must be a member of the server for longer than 10 minutes.</summary>
    High = 3,

    /// <summary>Very High - must have a verified phone number.</summary>
    VeryHigh = 4
}
