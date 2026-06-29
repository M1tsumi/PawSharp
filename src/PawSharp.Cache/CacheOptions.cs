#nullable enable
namespace PawSharp.Cache;

/// <summary>
/// Configuration options for cache providers.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Maximum number of guilds to cache (default: 1000).
    /// </summary>
    public int MaxGuilds { get; set; } = 1000;

    /// <summary>
    /// Maximum number of channels to cache (default: 5000).
    /// </summary>
    public int MaxChannels { get; set; } = 5000;

    /// <summary>
    /// Maximum number of users to cache (default: 20000).
    /// </summary>
    public int MaxUsers { get; set; } = 20000;

    /// <summary>
    /// Maximum number of messages to cache (default: 10000).
    /// </summary>
    public int MaxMessages { get; set; } = 10000;

    /// <summary>
    /// Maximum number of guild members to cache (default: 50000).
    /// </summary>
    public int MaxMembers { get; set; } = 50000;

    /// <summary>
    /// Maximum number of roles to cache (default: 10000).
    /// </summary>
    public int MaxRoles { get; set; } = 10000;

    /// <summary>
    /// Maximum number of emojis to cache (default: 5000).
    /// </summary>
    public int MaxEmojis { get; set; } = 5000;

    /// <summary>
    /// Default expiration time for cached entities (default: null = no expiration).
    /// </summary>
    public TimeSpan? DefaultExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for users (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? UserExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for guilds (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? GuildExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for channels (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? ChannelExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for messages (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? MessageExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for guild members (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? MemberExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for roles (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? RoleExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for emojis (overrides DefaultExpiration if set).
    /// </summary>
    public TimeSpan? EmojiExpiration { get; set; } = null;

    /// <summary>
    /// Expiration time for generic cache entries (default: 1 hour).
    /// </summary>
    public TimeSpan GenericCacheExpiration { get; set; } = TimeSpan.FromHours(1);
}
