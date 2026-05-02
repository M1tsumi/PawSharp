#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Providers
{
    public class MemoryCacheProvider : IEntityCache
    {
        private readonly ConcurrentDictionary<ulong, Guild> _guilds;
        private readonly ConcurrentDictionary<ulong, Channel> _channels;
        private readonly ConcurrentDictionary<ulong, User> _users;
        private readonly ConcurrentDictionary<ulong, Message> _messages;
        private readonly ConcurrentDictionary<string, GuildMember> _members; // Key: guildId:userId
        private readonly ConcurrentDictionary<string, Role> _roles; // Key: guildId:roleId
        private readonly ConcurrentDictionary<string, Emoji> _emojis; // Key: guildId:emojiId

        // Bounded caching configuration
        private readonly int _maxGuilds;
        private readonly int _maxChannels;
        private readonly int _maxUsers;
        private readonly int _maxMessages;
        private readonly int _maxMembers;
        private readonly int _maxRoles;
        private readonly int _maxEmojis;
        private readonly object _evictionLock = new object();

        // Expiration configuration
        private readonly TimeSpan? _userExpiration;
        private readonly TimeSpan? _guildExpiration;
        private readonly TimeSpan? _channelExpiration;
        private readonly TimeSpan? _messageExpiration;
        private readonly TimeSpan? _memberExpiration;
        private readonly TimeSpan? _roleExpiration;
        private readonly TimeSpan? _emojiExpiration;

        // Metrics tracking
        private long _hits;
        private long _misses;

        // Cache invalidation events
        public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        public event EventHandler? CacheCleared;

        // Statistics
        public int GuildCacheSize => _guilds.Count;
        public int ChannelCacheSize => _channels.Count;
        public int UserCacheSize => _users.Count;
        public int MessageCacheSize => _messages.Count;
        public int MemberCacheSize => _members.Count;
        public int RoleCacheSize => _roles.Count;
        public int EmojiCacheSize => _emojis.Count;

        public MemoryCacheProvider(CacheOptions? options = null)
        {
            var opts = options ?? new CacheOptions();

            _maxGuilds = opts.MaxGuilds;
            _maxChannels = opts.MaxChannels;
            _maxUsers = opts.MaxUsers;
            _maxMessages = opts.MaxMessages;
            _maxMembers = opts.MaxMembers;
            _maxRoles = opts.MaxRoles;
            _maxEmojis = opts.MaxEmojis;

            _userExpiration = opts.UserExpiration ?? opts.DefaultExpiration;
            _guildExpiration = opts.GuildExpiration ?? opts.DefaultExpiration;
            _channelExpiration = opts.ChannelExpiration ?? opts.DefaultExpiration;
            _messageExpiration = opts.MessageExpiration ?? opts.DefaultExpiration;
            _memberExpiration = opts.MemberExpiration ?? opts.DefaultExpiration;
            _roleExpiration = opts.RoleExpiration ?? opts.DefaultExpiration;
            _emojiExpiration = opts.EmojiExpiration ?? opts.DefaultExpiration;

            _guilds = new ConcurrentDictionary<ulong, Guild>();
            _channels = new ConcurrentDictionary<ulong, Channel>();
            _users = new ConcurrentDictionary<ulong, User>();
            _messages = new ConcurrentDictionary<ulong, Message>();
            _members = new ConcurrentDictionary<string, GuildMember>();
            _roles = new ConcurrentDictionary<string, Role>();
            _emojis = new ConcurrentDictionary<string, Emoji>();
        }

        public void Add(string key, object entity)
        {
            // Generic cache operations are not used by typed entity operations
            // This method is kept for interface compatibility but does nothing
            // Use typed operations (CacheUser, CacheGuild, etc.) instead
        }

        public object? Get(string key)
        {
            // Generic cache operations are not used by typed entity operations
            // This method is kept for interface compatibility but returns null
            // Use typed operations (GetUser, GetGuild, etc.) instead
            return null;
        }

        public void Remove(string key)
        {
            // Generic cache operations are not used by typed entity operations
            // This method is kept for interface compatibility but does nothing
            // Use typed operations or RemoveGuild for entity removal
        }

        private void EnforceEntityCacheBounds<TKey, TValue>(ConcurrentDictionary<TKey, TValue> cache, int maxSize, string entityType)
            where TKey : notnull
        {
            if (cache.Count <= maxSize) return;

            // Lock to ensure atomic snapshot and removal, preventing race conditions
            lock (_evictionLock)
            {
                if (cache.Count <= maxSize) return;

                // Remove oldest entries (simple FIFO eviction)
                var keysToRemove = cache.Keys.Take(cache.Count - maxSize).ToList();
                foreach (var key in keysToRemove)
                {
                    if (cache.TryRemove(key, out _))
                    {
                        // Trigger eviction event for entity keys that are ulong IDs
                        if (key is ulong entityId)
                        {
                            EntityEvicted?.Invoke(this, new CacheInvalidationEventArgs
                            {
                                EntityType = entityType,
                                EntityId = entityId
                            });
                        }
                    }
                }
            }
        }

        public bool Exists(string key)
        {
            // Generic cache operations are not used by typed entity operations
            // This method is kept for interface compatibility but returns false
            return false;
        }

        public void Clear()
        {
            _guilds.Clear();
            _channels.Clear();
            _users.Clear();
            _messages.Clear();
            _members.Clear();
            _roles.Clear();
            _emojis.Clear();
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }

        // Typed entity operations
        public void CacheUser(User user)
        {
            _users[user.Id] = user;
            EnforceEntityCacheBounds(_users, _maxUsers, "User");
        }

        public User? GetUser(ulong userId)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                Interlocked.Increment(ref _hits);
                return user;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public void CacheGuild(Guild guild)
        {
            _guilds[guild.Id] = guild;
            EnforceEntityCacheBounds(_guilds, _maxGuilds, "Guild");
        }

        public Guild? GetGuild(ulong guildId)
        {
            if (_guilds.TryGetValue(guildId, out var guild))
            {
                Interlocked.Increment(ref _hits);
                return guild;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public IEnumerable<Guild> GetAllGuilds()
        {
            return _guilds.Values;
        }

        public void CacheChannel(Channel channel)
        {
            _channels[channel.Id] = channel;
            EnforceEntityCacheBounds(_channels, _maxChannels, "Channel");
        }

        public Channel? GetChannel(ulong channelId)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                Interlocked.Increment(ref _hits);
                return channel;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public IEnumerable<Channel> GetGuildChannels(ulong guildId)
        {
            return _channels.Values.Where(c => c.GuildId == guildId);
        }

        public void CacheMessage(Message message)
        {
            _messages[message.Id] = message;
            EnforceEntityCacheBounds(_messages, _maxMessages, "Message");
        }

        public Message? GetMessage(ulong messageId)
        {
            if (_messages.TryGetValue(messageId, out var message))
            {
                Interlocked.Increment(ref _hits);
                return message;
            }
            Interlocked.Increment(ref _misses);
            return null;
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
            EnforceEntityCacheBounds(_members, _maxMembers, "Member");
            
            // Also cache the user
            if (member.User != null)
            {
                CacheUser(member.User);
            }
        }

        public GuildMember? GetGuildMember(ulong guildId, ulong userId)
        {
            var key = $"{guildId}:{userId}";
            if (_members.TryGetValue(key, out var member))
            {
                Interlocked.Increment(ref _hits);
                return member;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId)
        {
            return _members.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Value);
        }

        public void CacheRole(ulong guildId, Role role)
        {
            var key = $"{guildId}:{role.Id}";
            _roles[key] = role;
            EnforceEntityCacheBounds(_roles, _maxRoles, "Role");
        }

        public Role? GetRole(ulong guildId, ulong roleId)
        {
            var key = $"{guildId}:{roleId}";
            if (_roles.TryGetValue(key, out var role))
            {
                Interlocked.Increment(ref _hits);
                return role;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public IEnumerable<Role> GetGuildRoles(ulong guildId)
        {
            return _roles.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Value);
        }

        public void CacheEmoji(ulong guildId, Emoji emoji)
        {
            if (emoji.Id.HasValue)
            {
                var key = $"{guildId}:{emoji.Id.Value}";
                _emojis[key] = emoji;
                EnforceEntityCacheBounds(_emojis, _maxEmojis, "Emoji");
            }
        }

        public Emoji? GetEmoji(ulong guildId, ulong emojiId)
        {
            var key = $"{guildId}:{emojiId}";
            if (_emojis.TryGetValue(key, out var emoji))
            {
                Interlocked.Increment(ref _hits);
                return emoji;
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId)
        {
            return _emojis.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Value);
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
            
            // Remove roles
            var roleKeys = _roles.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Key).ToList();
            foreach (var key in roleKeys)
            {
                _roles.TryRemove(key, out _);
            }
            
            // Remove emojis
            var emojiKeys = _emojis.Where(kvp => kvp.Key.StartsWith($"{guildId}:")).Select(kvp => kvp.Key).ToList();
            foreach (var key in emojiKeys)
            {
                _emojis.TryRemove(key, out _);
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
                MemoryUsage = GetMemoryUsage(),
                Hits = Interlocked.Read(ref _hits),
                Misses = Interlocked.Read(ref _misses)
            };
        }

        public int GetEntityCount()
        {
            return _guilds.Count + _channels.Count + _users.Count + _messages.Count + _members.Count + _roles.Count + _emojis.Count;
        }

        public long GetMemoryUsage()
        {
            // Estimate based on entity counts and average sizes
            // These are rough estimates: User~1KB, Guild~2KB, Channel~1KB, Message~2KB, Member~1KB, Role~0.5KB, Emoji~0.5KB
            return (_users.Count * 1024L) +
                   (_guilds.Count * 2048L) +
                   (_channels.Count * 1024L) +
                   (_messages.Count * 2048L) +
                   (_members.Count * 1024L) +
                   (_roles.Count * 512L) +
                   (_emojis.Count * 512L);
        }

        // Async overloads — in-memory provider delegates to sync methods via Task.FromResult
        public Task<User?> GetUserAsync(ulong userId) => Task.FromResult(GetUser(userId));
        public Task<Guild?> GetGuildAsync(ulong guildId) => Task.FromResult(GetGuild(guildId));
        public Task<Channel?> GetChannelAsync(ulong channelId) => Task.FromResult(GetChannel(channelId));
        public Task<Message?> GetMessageAsync(ulong messageId) => Task.FromResult(GetMessage(messageId));
        public Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId) => Task.FromResult(GetGuildMember(guildId, userId));
        public Task<Role?> GetRoleAsync(ulong guildId, ulong roleId) => Task.FromResult(GetRole(guildId, roleId));
        public Task<Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId) => Task.FromResult(GetEmoji(guildId, emojiId));

        public bool IsHealthy()
        {
            // In-memory cache is always healthy as long as it's accessible
            return true;
        }
    }
}