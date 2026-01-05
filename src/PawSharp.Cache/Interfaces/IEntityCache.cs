using System.Collections.Generic;
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
        
        // Bulk operations
        void CacheGuildData(Guild guild);
        void RemoveGuild(ulong guildId);
        
        // Cache statistics
        int GetEntityCount();
        long GetMemoryUsage();
    }
}