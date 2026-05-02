using PawSharp.Core.Enums;

namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for checking guild features on a <see cref="Entities.Guild"/>.
/// </summary>
public static class GuildFeatureExtensions
{
    /// <summary>
    /// Checks if the guild has a specific feature.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <param name="feature">The feature to check for.</param>
    /// <returns>True if the guild has the feature.</returns>
    public static bool HasFeature(this Entities.Guild guild, GuildFeature feature)
    {
        return guild.Features.Contains(feature.ToApiString());
    }

    /// <summary>
    /// Checks if the guild is partnered.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild is partnered.</returns>
    public static bool IsPartnered(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Partnered);
    }

    /// <summary>
    /// Checks if the guild is verified.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild is verified.</returns>
    public static bool IsVerified(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Verified);
    }

    /// <summary>
    /// Checks if the guild is a community guild.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild is a community guild.</returns>
    public static bool IsCommunity(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Community);
    }

    /// <summary>
    /// Checks if the guild has enabled monetization.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has monetization enabled.</returns>
    public static bool HasMonetization(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.CreatorMonetizable);
    }

    /// <summary>
    /// Checks if the guild has enabled the creator store.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has the creator store enabled.</returns>
    public static bool HasCreatorStore(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.CreatorStorePage);
    }

    /// <summary>
    /// Checks if the guild has enabled soundboard.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has soundboard enabled.</returns>
    public static bool HasSoundboard(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Soundboard);
    }

    /// <summary>
    /// Checks if the guild has enabled vanity URL.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has vanity URL enabled.</returns>
    public static bool HasVanityUrl(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.VanityUrl);
    }

    /// <summary>
    /// Checks if the guild has enabled threads.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has threads enabled.</returns>
    public static bool HasThreads(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Threads);
    }

    /// <summary>
    /// Checks if the guild has enabled stage channels.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has stage channels enabled.</returns>
    public static bool HasStageChannels(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.StageChannels);
    }

    /// <summary>
    /// Checks if the guild has enabled premium (Server Boosting).
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has premium enabled.</returns>
    public static bool HasPremium(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Premium);
    }

    /// <summary>
    /// Checks if the guild has enabled news channels.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has news channels enabled.</returns>
    public static bool HasNewsChannels(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.News);
    }

    /// <summary>
    /// Checks if the guild is discoverable.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild is discoverable.</returns>
    public static bool IsDiscoverable(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.Discoverable);
    }

    /// <summary>
    /// Checks if the guild has enabled auto moderation.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has auto moderation enabled.</returns>
    public static bool HasAutoModeration(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.AutoModeration);
    }

    /// <summary>
    /// Checks if the guild has enabled role subscriptions.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has role subscriptions enabled.</returns>
    public static bool HasRoleSubscriptions(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.RoleSubscriptions);
    }

    /// <summary>
    /// Checks if the guild has enabled the welcome screen.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has the welcome screen enabled.</returns>
    public static bool HasWelcomeScreen(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.GuildWelcomeScreen);
    }

    /// <summary>
    /// Checks if the guild has enabled member verification gate.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has member verification gate enabled.</returns>
    public static bool HasMemberVerificationGate(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.MemberVerificationGate);
    }

    /// <summary>
    /// Checks if the guild has enabled guild onboarding.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has guild onboarding enabled.</returns>
    public static bool HasOnboarding(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.GuildOnboarding);
    }

    /// <summary>
    /// Checks if the guild has enabled scheduled events.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has scheduled events enabled.</returns>
    public static bool HasScheduledEvents(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.GuildScheduledEvents);
    }

    /// <summary>
    /// Checks if the guild has enabled stickers.
    /// </summary>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the guild has stickers enabled.</returns>
    public static bool HasStickers(this Entities.Guild guild)
    {
        return guild.HasFeature(GuildFeature.GuildStickers);
    }
}
