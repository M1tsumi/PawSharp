using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Providers
{
    public class MemoryCacheProvider : IEntityCache
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache;
        private readonly ConcurrentDictionary<ulong, Guild> _guilds;
        private readonly ConcurrentDictionary<ulong, Channel> _channels;
        private readonly ConcurrentDictionary<ulong, User> _users;
        private readonly ConcurrentDictionary<ulong, Message> _messages;
        private readonly ConcurrentDictionary<string, GuildMember> _members; // Key: guildId:userId
        private readonly ConcurrentDictionary<ulong, Role> _roles;

        public MemoryCacheProvider()
        {
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _guilds = new ConcurrentDictionary<ulong, Guild>();
            _channels = new ConcurrentDictionary<ulong, Channel>();
            _users = new ConcurrentDictionary<ulong, User>();
            _messages = new ConcurrentDictionary<ulong, Message>();
            _members = new ConcurrentDictionary<string, GuildMember>();
            _roles = new ConcurrentDictionary<ulong, Role>();
        }

        public void Add(string key, object entity)
        {
            AddInternal(key, entity, null);
        }

        private void AddInternal(string key, object value, TimeSpan? expiration = null)
        {
            var cacheItem = new CacheItem(value, expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null);
            _cache[key] = cacheItem;
        }

        public object? Get(string key)
        {
            if (_cache.TryGetValue(key, out var cacheItem))
            {
                if (!cacheItem.IsExpired)
                {
                    return cacheItem.Value;
                }
                else
                {
                    Remove(key);
                }
            }
            return null;
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
            _guilds.Clear();
            _channels.Clear();
            _users.Clear();
            _messages.Clear();
            _members.Clear();
            _roles.Clear();
        }

        public bool Exists(string key)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired;
        }

        // Typed entity operations
        public void CacheUser(User user)
        {
            _users[user.Id] = user;
        }

        public User? GetUser(ulong userId)
        {
            return _users.TryGetValue(userId, out var user) ? user : null;
        }

        public void CacheGuild(Guild guild)
        {
            _guilds[guild.Id] = guild;
        }

        public Guild? GetGuild(ulong guildId)
        {
            return _guilds.TryGetValue(guildId, out var guild) ? guild : null;
        }

        public IEnumerable<Guild> GetAllGuilds()
        {
            return _guilds.Values;
        }

        public void CacheChannel(Channel channel)
        {
            _channels[channel.Id] = channel;
        }

        public Channel? GetChannel(ulong channelId)
        {
            return _channels.TryGetValue(channelId, out var channel) ? channel : null;
        }

        public IEnumerable<Channel> GetGuildChannels(ulong guildId)
        {
            return _channels.Values.Where(c => c.GuildId == guildId);
        }

        public void CacheMessage(Message message)
        {
            _messages[message.Id] = message;
            
            // Keep only recent messages to prevent memory bloat
            if (_messages.Count > 10000)
            {
                var oldestKey = _messages.OrderBy(m => m.Value.Timestamp).First().Key;
                _messages.TryRemove(oldestKey, out _);
            }
        }

        public Message? GetMessage(ulong messageId)
        {
            return _messages.TryGetValue(messageId, out var message) ? message : null;
        }

        public IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50)
        {
            return _messages.Values
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.Timestamp)
                .Take(limit);
        }

        public void CacheGuildMember(ulong guildId, GuildMember member)
        {
            var key = $"{guildId}:{member.User?.Id}";
            _members[key] = member;
            
            // Also cache the user
            if (member.User != null)
            {
                CacheUser(member.User);
            }
        }

        public GuildMember? GetGuildMember(ulong guildId, ulong userId)
        {
            var key = $"{guildId}:{userId}";
            return _members.TryGetValue(key, out var member) ? member : null;
        }

        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId)
        {
            return _members.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Value);
        }

        public void CacheRole(ulong guildId, Role role)
        {
            _roles[role.Id] = role;
        }

        public Role? GetRole(ulong roleId)
        {
            return _roles.TryGetValue(roleId, out var role) ? role : null;
        }

        public IEnumerable<Role> GetGuildRoles(ulong guildId)
        {
            var guild = GetGuild(guildId);
            return guild?.Roles ?? Enumerable.Empty<Role>();
        }

        public void CacheGuildData(Guild guild)
        {
            // Cache the guild
            CacheGuild(guild);
            
            // Cache all channels
            if (guild.Channels != null)
            {
                foreach (var channel in guild.Channels)
                {
                    CacheChannel(channel);
                }
            }
            
            // Cache all members
            if (guild.Members != null)
            {
                foreach (var member in guild.Members)
                {
                    CacheGuildMember(guild.Id, member);
                }
            }
            
            // Cache all roles
            if (guild.Roles != null)
            {
                foreach (var role in guild.Roles)
                {
                    CacheRole(guild.Id, role);
                }
            }
        }

        public void RemoveGuild(ulong guildId)
        {
            _guilds.TryRemove(guildId, out _);
            
            // Remove channels
            var channelKeys = _channels.Where(kvp => kvp.Value.GuildId == guildId).Select(kvp => kvp.Key).ToList();
            foreach (var key in channelKeys)
            {
                _channels.TryRemove(key, out _);
            }
            
            // Remove members
            var memberKeys = _members.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Key).ToList();
            foreach (var key in memberKeys)
            {
                _members.TryRemove(key, out _);
            }
        }

        public int GetEntityCount()
        {
            return _cache.Count + _guilds.Count + _channels.Count + _users.Count + _messages.Count + _members.Count + _roles.Count;
        }

        public long GetMemoryUsage()
        {
            // Rough estimate - would need more sophisticated calculation for accurate numbers
            return GC.GetTotalMemory(false);
        }

        private class CacheItem
        {
            public object Value { get; }
            public DateTime? Expiration { get; }

            public CacheItem(object value, DateTime? expiration)
            {
                Value = value;
                Expiration = expiration;
            }

            public bool IsExpired => Expiration.HasValue && DateTime.UtcNow > Expiration.Value;
        }
    }
}