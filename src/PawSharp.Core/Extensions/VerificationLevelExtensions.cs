namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Enums.VerificationLevel"/> enum.
/// </summary>
public static class VerificationLevelExtensions
{
    /// <summary>
    /// Checks if the verification level requires email verification.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if email verification is required.</returns>
    public static bool RequiresEmail(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.Low;
    }

    /// <summary>
    /// Checks if the verification level requires a verified phone number.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if phone verification is required.</returns>
    public static bool RequiresPhone(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.Medium;
    }

    /// <summary>
    /// Checks if the verification level requires the account to be registered for at least 5 minutes.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if 5-minute account age is required.</returns>
    public static bool RequiresFiveMinutes(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.Medium;
    }

    /// <summary>
    /// Checks if the verification level requires the account to be registered for at least 30 minutes.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if 30-minute account age is required.</returns>
    public static bool RequiresThirtyMinutes(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.High;
    }

    /// <summary>
    /// Checks if the verification level requires the account to be a member of the guild for at least 10 minutes.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if 10-minute membership is required.</returns>
    public static bool RequiresTenMinutesMembership(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.High;
    }

    /// <summary>
    /// Checks if the verification level requires the account to be a member of the guild for at least 30 minutes.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if 30-minute membership is required.</returns>
    public static bool RequiresThirtyMinutesMembership(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.VeryHigh;
    }

    /// <summary>
    /// Checks if the verification level requires the account to be a member of the guild for at least 10 minutes AND have a verified phone.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>True if both membership and phone verification are required.</returns>
    public static bool RequiresMembershipAndPhone(this Enums.VerificationLevel level)
    {
        return level >= Enums.VerificationLevel.VeryHigh;
    }

    /// <summary>
    /// Gets a human-readable description of the verification level.
    /// </summary>
    /// <param name="level">The verification level.</param>
    /// <returns>A description of the verification requirements.</returns>
    public static string GetDescription(this Enums.VerificationLevel level)
    {
        return level switch
        {
            Enums.VerificationLevel.None => "No verification required",
            Enums.VerificationLevel.Low => "Must have verified email on account",
            Enums.VerificationLevel.Medium => "Must have verified email on account, and account must be registered for at least 5 minutes",
            Enums.VerificationLevel.High => "Must have verified email on account, account must be registered for at least 30 minutes, and must be a member of the guild for at least 10 minutes",
            Enums.VerificationLevel.VeryHigh => "Must have verified phone on account, account must be registered for at least 30 minutes, and must be a member of the guild for at least 30 minutes",
            _ => "Unknown verification level"
        };
    }
}
