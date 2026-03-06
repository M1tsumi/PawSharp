#nullable enable
namespace PawSharp.Core.CDN;

/// <summary>
/// Builds Discord CDN URLs for avatars, icons, banners, emojis, and stickers.
/// </summary>
public static class DiscordCdn
{
    private const string BaseUrl = "https://cdn.discordapp.com";

    /// <summary>The supported image file formats.</summary>
    public static class Format
    {
        public const string Png  = "png";
        public const string Jpg  = "jpg";
        public const string WebP = "webp";
        public const string Gif  = "gif";
    }

    // ── Users ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// URL for a user's avatar. Returns the default avatar URL when <paramref name="avatarHash"/> is null.
    /// Animated avatars (hash starts with "a_") are returned as GIF when <paramref name="format"/> is
    /// <see cref="Format.Gif"/> or when format is not explicitly specified.
    /// </summary>
    public static string GetUserAvatar(ulong userId, string? avatarHash, int size = 256, string? format = null)
    {
        if (avatarHash is null)
            return GetDefaultAvatar(userId);

        var ext = format ?? (avatarHash.StartsWith("a_") ? Format.Gif : Format.WebP);
        return $"{BaseUrl}/avatars/{userId}/{avatarHash}.{ext}?size={size}";
    }

    /// <summary>
    /// URL for the default avatar displayed when a user has no custom avatar.
    /// </summary>
    public static string GetDefaultAvatar(ulong userId)
    {
        // Discord shifts the discriminator index: (userId >> 22) % 6 for the new username system.
        var index = (userId >> 22) % 6;
        return $"{BaseUrl}/embed/avatars/{index}.png";
    }

    /// <summary>URL for a user's banner image.</summary>
    public static string? GetUserBanner(ulong userId, string? bannerHash, int size = 512, string? format = null)
    {
        if (bannerHash is null) return null;
        var ext = format ?? (bannerHash.StartsWith("a_") ? Format.Gif : Format.WebP);
        return $"{BaseUrl}/banners/{userId}/{bannerHash}.{ext}?size={size}";
    }

    // ── Guilds ─────────────────────────────────────────────────────────────────

    /// <summary>URL for a guild's icon.</summary>
    public static string? GetGuildIcon(ulong guildId, string? iconHash, int size = 256, string? format = null)
    {
        if (iconHash is null) return null;
        var ext = format ?? (iconHash.StartsWith("a_") ? Format.Gif : Format.WebP);
        return $"{BaseUrl}/icons/{guildId}/{iconHash}.{ext}?size={size}";
    }

    /// <summary>URL for a guild's invite splash image.</summary>
    public static string? GetGuildSplash(ulong guildId, string? splashHash, int size = 512, string? format = null)
    {
        if (splashHash is null) return null;
        return $"{BaseUrl}/splashes/{guildId}/{splashHash}.{format ?? Format.WebP}?size={size}";
    }

    /// <summary>URL for a guild's discovery splash image.</summary>
    public static string? GetGuildDiscoverySplash(ulong guildId, string? splashHash, int size = 512, string? format = null)
    {
        if (splashHash is null) return null;
        return $"{BaseUrl}/discovery-splashes/{guildId}/{splashHash}.{format ?? Format.WebP}?size={size}";
    }

    /// <summary>URL for a guild's banner image.</summary>
    public static string? GetGuildBanner(ulong guildId, string? bannerHash, int size = 512, string? format = null)
    {
        if (bannerHash is null) return null;
        var ext = format ?? (bannerHash.StartsWith("a_") ? Format.Gif : Format.WebP);
        return $"{BaseUrl}/banners/{guildId}/{bannerHash}.{ext}?size={size}";
    }

    // ── Emojis ─────────────────────────────────────────────────────────────────

    /// <summary>URL for a custom guild emoji.</summary>
    public static string GetEmoji(ulong emojiId, bool animated = false, int size = 64)
    {
        var ext = animated ? Format.Gif : Format.WebP;
        return $"{BaseUrl}/emojis/{emojiId}.{ext}?size={size}";
    }

    // ── Stickers ───────────────────────────────────────────────────────────────

    /// <summary>URL for a sticker. PNG and APNG stickers use "png"; Lottie stickers use "json".</summary>
    public static string GetSticker(ulong stickerId, string format = Format.Png)
        => $"{BaseUrl}/stickers/{stickerId}.{format}";

    // ── Applications ───────────────────────────────────────────────────────────

    /// <summary>URL for an application's icon.</summary>
    public static string? GetApplicationIcon(ulong applicationId, string? iconHash, int size = 256, string? format = null)
    {
        if (iconHash is null) return null;
        return $"{BaseUrl}/app-icons/{applicationId}/{iconHash}.{format ?? Format.WebP}?size={size}";
    }

    /// <summary>URL for an application's cover image (used on the store).</summary>
    public static string? GetApplicationCover(ulong applicationId, string? coverHash, int size = 256, string? format = null)
    {
        if (coverHash is null) return null;
        return $"{BaseUrl}/app-icons/{applicationId}/{coverHash}.{format ?? Format.WebP}?size={size}";
    }

    // ── Role icons ─────────────────────────────────────────────────────────────

    /// <summary>URL for a role's icon image.</summary>
    public static string? GetRoleIcon(ulong roleId, string? iconHash, int size = 64, string? format = null)
    {
        if (iconHash is null) return null;
        return $"{BaseUrl}/role-icons/{roleId}/{iconHash}.{format ?? Format.WebP}?size={size}";
    }
}
