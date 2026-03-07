#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Interfaces
{
    /// <summary>
    /// Interface for caching Discord entities.
    /// </summary>
    public interface IEntityCache
    {
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
        Role? GetRole(ulong roleId);
        IEnumerable<Role> GetGuildRoles(ulong guildId);
        
        void CacheEmoji(ulong guildId, Emoji emoji);
        Emoji? GetEmoji(ulong emojiId);
        IEnumerable<Emoji> GetGuildEmojis(ulong guildId);
        
        // Bulk operations
        void CacheGuildData(Guild guild);
        void RemoveGuild(ulong guildId);
        
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
        Task<Role?> GetRoleAsync(ulong roleId);
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
    }
}