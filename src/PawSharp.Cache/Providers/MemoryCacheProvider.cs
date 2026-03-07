#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly ConcurrentDictionary<ulong, Emoji> _emojis;

        // Bounded caching configuration
        private const int MaxCacheSize = 10000; // Maximum number of items in general cache
        private const int MaxEntityCacheSize = 5000; // Maximum entities per type
        private readonly object _cleanupLock = new object();
        private DateTime _lastCleanup = DateTime.UtcNow;
        // Min-heap ordered by expiration: O(log n) insert, O(1) peek, O(log n) dequeue.
        // Items without expiration are excluded; they are evicted last.
        private readonly PriorityQueue<string, DateTime> _expirationQueue = new();

        // Statistics
        public int CacheSize => _cache.Count;
        public int GuildCacheSize => _guilds.Count;
        public int ChannelCacheSize => _channels.Count;
        public int UserCacheSize => _users.Count;
        public int MessageCacheSize => _messages.Count;
        public int MemberCacheSize => _members.Count;
        public int RoleCacheSize => _roles.Count;
        public int EmojiCacheSize => _emojis.Count;

        public MemoryCacheProvider()
        {
            _cache = new ConcurrentDictionary<string, CacheItem>();
            _guilds = new ConcurrentDictionary<ulong, Guild>();
            _channels = new ConcurrentDictionary<ulong, Channel>();
            _users = new ConcurrentDictionary<ulong, User>();
            _messages = new ConcurrentDictionary<ulong, Message>();
            _members = new ConcurrentDictionary<string, GuildMember>();
            _roles = new ConcurrentDictionary<ulong, Role>();
            _emojis = new ConcurrentDictionary<ulong, Emoji>();
        }

        public void Add(string key, object entity)
        {
            AddInternal(key, entity, null);
        }

        private void AddInternal(string key, object value, TimeSpan? expiration = null)
        {
            var cacheItem = new CacheItem(value, expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null);
            _cache[key] = cacheItem;

            // Track expiring items in the heap so cleanup is O(log n) not O(n log n)
            if (cacheItem.Expiration.HasValue)
            {
                lock (_cleanupLock)
                {
                    _expirationQueue.Enqueue(key, cacheItem.Expiration.Value);
                }
            }

            // Perform bounded caching cleanup if necessary
            if (_cache.Count > MaxCacheSize)
            {
                PerformCleanup();
            }
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

        private void EnforceEntityCacheBounds<TKey, TValue>(ConcurrentDictionary<TKey, TValue> cache, int maxSize)
            where TKey : notnull
        {
            if (cache.Count <= maxSize) return;

            // Remove oldest entries (simple FIFO eviction)
            var keysToRemove = cache.Keys.Take(cache.Count - maxSize).ToList();
            foreach (var key in keysToRemove)
            {
                cache.TryRemove(key, out _);
            }
        }

        private void PerformCleanup()
        {
            lock (_cleanupLock)
            {
                // Only perform cleanup if it's been more than 5 minutes since last cleanup
                if ((DateTime.UtcNow - _lastCleanup).TotalMinutes < 5)
                    return;

                _lastCleanup = DateTime.UtcNow;

                // Drain the heap: dequeue all entries whose expiration has passed.
                // This is O(k log n) where k = number of expired items, vs O(n log n) for full sort.
                var now = DateTime.UtcNow;
                while (_expirationQueue.TryPeek(out _, out var soonest) && soonest <= now)
                {
                    if (_expirationQueue.TryDequeue(out var expiredKey, out _))
                        _cache.TryRemove(expiredKey, out _);
                }

                // If still over limit, evict by soonest expiration first (cheapest to lose).
                // Non-expiring items are not in the heap and are kept longest.
                while (_cache.Count > MaxCacheSize && _expirationQueue.TryDequeue(out var victimKey, out _))
                {
                    _cache.TryRemove(victimKey, out _);
                }

                // Last resort: the cache is over limit and no expiring items remain.
                // Evict an arbitrary batch (keys() snapshot is O(n) but this path is rare).
                if (_cache.Count > MaxCacheSize)
                {
                    var overflow = _cache.Count - MaxCacheSize;
                    foreach (var key in _cache.Keys.Take(overflow).ToList())
                        _cache.TryRemove(key, out _);
                }
            }
        }

        public bool Exists(string key)
        {
            return _cache.ContainsKey(key) && !_cache[key].IsExpired;
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

        // Typed entity operations
        public void CacheUser(User user)
        {
            _users[user.Id] = user;
            EnforceEntityCacheBounds(_users, MaxEntityCacheSize);
        }

        public User? GetUser(ulong userId)
        {
            return _users.TryGetValue(userId, out var user) ? user : null;
        }

        public void CacheGuild(Guild guild)
        {
            _guilds[guild.Id] = guild;
            EnforceEntityCacheBounds(_guilds, MaxEntityCacheSize);
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
            EnforceEntityCacheBounds(_channels, MaxEntityCacheSize);
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
            EnforceEntityCacheBounds(_messages, MaxEntityCacheSize);
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
            EnforceEntityCacheBounds(_members, MaxEntityCacheSize);
            
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
            EnforceEntityCacheBounds(_roles, MaxEntityCacheSize);
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

        public void CacheEmoji(ulong guildId, Emoji emoji)
        {
            if (emoji.Id.HasValue)
            {
                _emojis[emoji.Id.Value] = emoji;
                EnforceEntityCacheBounds(_emojis, MaxEntityCacheSize);
            }
        }

        public Emoji? GetEmoji(ulong emojiId)
        {
            return _emojis.TryGetValue(emojiId, out var emoji) ? emoji : null;
        }

        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId)
        {
            var guild = GetGuild(guildId);
            return guild?.Emojis ?? Enumerable.Empty<Emoji>();
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
            
            // Cache all emojis
            if (guild.Emojis != null)
            {
                foreach (var emoji in guild.Emojis)
                {
                    CacheEmoji(guild.Id, emoji);
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

        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                UserCount = _users.Count,
                GuildCount = _guilds.Count,
                ChannelCount = _channels.Count,
                MessageCount = _messages.Count,
                MemberCount = _members.Count,
                RoleCount = _roles.Count,
                EmojiCount = _emojis.Count,
                MemoryUsage = GetMemoryUsage()
            };
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

        // Async overloads — in-memory provider delegates to sync methods via Task.FromResult
        public Task<User?> GetUserAsync(ulong userId) => Task.FromResult(GetUser(userId));
        public Task<Guild?> GetGuildAsync(ulong guildId) => Task.FromResult(GetGuild(guildId));
        public Task<Channel?> GetChannelAsync(ulong channelId) => Task.FromResult(GetChannel(channelId));
        public Task<Message?> GetMessageAsync(ulong messageId) => Task.FromResult(GetMessage(messageId));
        public Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId) => Task.FromResult(GetGuildMember(guildId, userId));
        public Task<Role?> GetRoleAsync(ulong roleId) => Task.FromResult(GetRole(roleId));

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