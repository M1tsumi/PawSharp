#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using PawSharp.Core.Entities;
using PawSharp.Cache.Telemetry;

namespace PawSharp.Cache.Interfaces;

/// <summary>
/// Event arguments for cache invalidation events.
/// </summary>
public class CacheInvalidationEventArgs : EventArgs
{
    /// <summary>
    /// The type of entity that was invalidated.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity that was invalidated.
    /// </summary>
    public ulong EntityId { get; set; }

    /// <summary>
    /// The guild ID (if applicable).
    /// </summary>
    public ulong? GuildId { get; set; }
}

/// <summary>
/// Defines the contract for a cache provider that stores Discord entities.
/// </summary>
/// <example>
/// <code>
/// public class MyCache : IEntityCache
/// {
///     private readonly ConcurrentDictionary&lt;ulong, User&gt; _users = new();
/// 
///     public void CacheUser(User user) =&gt; _users[user.Id] = user;
///     public User? GetUser(ulong userId) =&gt; _users.TryGetValue(userId, out var u) ? u : null;
///     // ... implement remaining members
/// }
/// </code>
/// </example>
public interface IEntityCache
{
    /// <summary>
    /// Optional telemetry provider for monitoring cache performance.
    /// </summary>
    ICacheTelemetry? Telemetry { get; set; }
        // Cache invalidation events
        event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        event EventHandler? CacheCleared;

        // Generic cache operations
        void Add(string key, object entity);
        object? Get(string key);
        void Remove(string key);
        void Clear();
        bool Exists(string key);

        // Typed entity operations
        void CacheUser(User user);
        User? GetUser(ulong userId);

        void CacheGuild(Guild guild);
        Guild? GetGuild(ulong guildId);
        IEnumerable<Guild> GetAllGuilds();

        void CacheChannel(Channel channel);
        Channel? GetChannel(ulong channelId);
        IEnumerable<Channel> GetGuildChannels(ulong guildId);

        void CacheMessage(Message message);
        Message? GetMessage(ulong messageId);
        IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50);

        void CacheGuildMember(ulong guildId, GuildMember member);
        GuildMember? GetGuildMember(ulong guildId, ulong userId);
        IEnumerable<GuildMember> GetGuildMembers(ulong guildId);

        void CacheRole(ulong guildId, Role role);
        Role? GetRole(ulong guildId, ulong roleId);
        IEnumerable<Role> GetGuildRoles(ulong guildId);

        void CacheEmoji(ulong guildId, Emoji emoji);
        Emoji? GetEmoji(ulong guildId, ulong emojiId);
        IEnumerable<Emoji> GetGuildEmojis(ulong guildId);

        // Bulk operations
        void CacheGuildData(Guild guild);
        void RemoveGuild(ulong guildId);

        // Typed remove operations
        void RemoveChannel(ulong channelId);
        void RemoveMessage(ulong messageId);
        void RemoveGuildMember(ulong guildId, ulong userId);
        void RemoveRole(ulong guildId, ulong roleId);

        // Cache statistics
        int GetEntityCount();
        long GetMemoryUsage();

        /// <summary>
        /// Gets cache statistics including counts per entity type and memory usage.
        /// </summary>
        CacheStats GetCacheStats();

        // Async overloads — enable async-capable backends (Redis, distributed caches)
        Task<User?> GetUserAsync(ulong userId);
        Task<Guild?> GetGuildAsync(ulong guildId);
        Task<Channel?> GetChannelAsync(ulong channelId);
        Task<Message?> GetMessageAsync(ulong messageId);
        Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId);
        Task<Role?> GetRoleAsync(ulong guildId, ulong roleId);
        Task<Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId);

        // Async write operations
        Task CacheUserAsync(User user);
        Task CacheGuildAsync(Guild guild);
        Task CacheChannelAsync(Channel channel);
        Task CacheMessageAsync(Message message);
        Task CacheGuildMemberAsync(ulong guildId, GuildMember member);
        Task CacheRoleAsync(ulong guildId, Role role);
        Task CacheEmojiAsync(ulong guildId, Emoji emoji);
        Task CacheGuildDataAsync(Guild guild);
        Task RemoveGuildAsync(ulong guildId);
        Task ClearAsync();

        // Async remove operations
        Task RemoveChannelAsync(ulong channelId);
        Task RemoveMessageAsync(ulong messageId);
        Task RemoveGuildMemberAsync(ulong guildId, ulong userId);
        Task RemoveRoleAsync(ulong guildId, ulong roleId);

        /// <summary>
        /// Performs a health check on the cache provider.
        /// </summary>
        /// <returns>True if the cache is healthy, false otherwise.</returns>
        bool IsHealthy();
}

/// <summary>
/// Cache statistics information.
/// </summary>
public class CacheStats
{
    /// <summary>
    /// Number of users cached.
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Number of guilds cached.
    /// </summary>
    public int GuildCount { get; set; }

    /// <summary>
    /// Number of channels cached.
    /// </summary>
    public int ChannelCount { get; set; }

    /// <summary>
    /// Number of messages cached.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Number of guild members cached.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Number of roles cached.
    /// </summary>
    public int RoleCount { get; set; }

    /// <summary>
    /// Number of emojis cached.
    /// </summary>
    public int EmojiCount { get; set; }

    /// <summary>
    /// Estimated memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Total number of cache misses.
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0.0;
}