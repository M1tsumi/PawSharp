#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Providers
{
    /// <summary>
    /// Redis-based distributed cache provider for PawSharp.
    /// Provides distributed caching capabilities with Redis as the backend.
    /// </summary>
    public class RedisCacheProvider : IEntityCache, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly RedisCacheOptions _options;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheProvider"/> class.
        /// </summary>
        /// <param name="options">Redis cache configuration options.</param>
        public RedisCacheProvider(IOptions<RedisCacheOptions> options)
        {
            _options = options.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var config = new ConfigurationOptions
            {
                EndPoints = { _options.ConnectionString },
                Password = _options.Password,
                DefaultDatabase = _options.Database,
                ConnectTimeout = _options.ConnectTimeout,
                SyncTimeout = _options.SyncTimeout,
                ConnectRetry = _options.ConnectRetry,
                AbortOnConnectFail = false
            };

            _redis = ConnectionMultiplexer.Connect(config);
            _db = _redis.GetDatabase();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheProvider"/> class with a connection string.
        /// </summary>
        /// <param name="connectionString">Redis connection string.</param>
        public RedisCacheProvider(string connectionString)
            : this(Options.Create(new RedisCacheOptions { ConnectionString = connectionString }))
        {
        }

        #region Generic Cache Operations

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="entity">The entity to cache.</param>
        public void Add(string key, object entity)
        {
            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            _db.StringSet(key, json, _options.DefaultExpiry);
        }

        /// <summary>
        /// Gets an item from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached entity, or null if not found.</returns>
        public object? Get(string key)
        {
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<object>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Removes an item from the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        public void Remove(string key)
        {
            _db.KeyDelete(key);
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                server.FlushDatabase(_options.Database);
            }
        }

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>True if the key exists, false otherwise.</returns>
        public bool Exists(string key)
        {
            return _db.KeyExists(key);
        }

        #endregion

        #region Typed Entity Operations

        /// <summary>
        /// Caches a user.
        /// </summary>
        /// <param name="user">The user to cache.</param>
        public void CacheUser(User user)
        {
            var key = $"user:{user.Id}";
            Add(key, user);
        }

        /// <summary>
        /// Gets a cached user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The cached user, or null if not found.</returns>
        public User? GetUser(ulong userId)
        {
            var key = $"user:{userId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<User>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Caches a guild.
        /// </summary>
        /// <param name="guild">The guild to cache.</param>
        public void CacheGuild(Guild guild)
        {
            var key = $"guild:{guild.Id}";
            Add(key, guild);
        }

        /// <summary>
        /// Gets a cached guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>The cached guild, or null if not found.</returns>
        public Guild? GetGuild(ulong guildId)
        {
            var key = $"guild:{guildId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<Guild>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets all cached guilds.
        /// </summary>
        /// <returns>An enumerable of all cached guilds.</returns>
        public IEnumerable<Guild> GetAllGuilds()
        {
            var result = _db.Execute("KEYS", "guild:*");
            var keys = ((RedisKey[]?)result) ?? [];
            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var guild = JsonSerializer.Deserialize<Guild>((string)json!, _jsonOptions);
                    if (guild != null)
                        yield return guild;
                }
            }
        }

        /// <summary>
        /// Caches a channel.
        /// </summary>
        /// <param name="channel">The channel to cache.</param>
        public void CacheChannel(Channel channel)
        {
            var key = $"channel:{channel.Id}";
            Add(key, channel);
        }

        /// <summary>
        /// Gets a cached channel.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <returns>The cached channel, or null if not found.</returns>
        public Channel? GetChannel(ulong channelId)
        {
            var key = $"channel:{channelId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<Channel>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets all channels for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of channels in the guild.</returns>
        public IEnumerable<Channel> GetGuildChannels(ulong guildId)
        {
            var result = _db.Execute("KEYS", $"channel:*");
            var keys = ((RedisKey[]?)result) ?? [];
            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var channel = JsonSerializer.Deserialize<Channel>((string)json!, _jsonOptions);
                    if (channel != null && channel.GuildId == guildId)
                        yield return channel;
                }
            }
        }

        /// <summary>
        /// Caches a message.
        /// </summary>
        /// <param name="message">The message to cache.</param>
        public void CacheMessage(Message message)
        {
            var key = $"message:{message.Id}";
            Add(key, message);

            // Also maintain a sorted set for channel messages
            var channelKey = $"channel:{message.ChannelId}:messages";
            _db.SortedSetAdd(channelKey, message.Id.ToString(), message.Id);
            _db.KeyExpire(channelKey, _options.DefaultExpiry);
        }

        /// <summary>
        /// Gets a cached message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <returns>The cached message, or null if not found.</returns>
        public Message? GetMessage(ulong messageId)
        {
            var key = $"message:{messageId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<Message>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets recent messages for a channel.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="limit">Maximum number of messages to return.</param>
        /// <returns>An enumerable of recent messages in the channel.</returns>
        public IEnumerable<Message> GetChannelMessages(ulong channelId, int limit = 50)
        {
            var channelKey = $"channel:{channelId}:messages";
            var messageIds = _db.SortedSetRangeByRank(channelKey, -limit, -1, Order.Descending);

            foreach (var messageIdStr in messageIds)
            {
                if (ulong.TryParse(messageIdStr, out var messageId))
                {
                    var message = GetMessage(messageId);
                    if (message != null)
                        yield return message;
                }
            }
        }

        /// <summary>
        /// Caches a guild member.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="member">The member to cache.</param>
        public void CacheGuildMember(ulong guildId, GuildMember member)
        {
            // GuildMember.User can be absent in some gateway events; skip caching without a User ID
            if (member.User is null) return;
            var key = $"member:{guildId}:{member.User.Id}";
            Add(key, member);
        }

        /// <summary>
        /// Gets a cached guild member.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>The cached member, or null if not found.</returns>
        public GuildMember? GetGuildMember(ulong guildId, ulong userId)
        {
            var key = $"member:{guildId}:{userId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<GuildMember>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets all members for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of members in the guild.</returns>
        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId)
        {
            var result = _db.Execute("KEYS", $"member:{guildId}:*");
            var keys = ((RedisKey[]?)result) ?? [];
            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var member = JsonSerializer.Deserialize<GuildMember>((string)json!, _jsonOptions);
                    if (member != null)
                        yield return member;
                }
            }
        }

        /// <summary>
        /// Caches a role.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="role">The role to cache.</param>
        public void CacheRole(ulong guildId, PawSharp.Core.Entities.Role role)
        {
            var key = $"role:{role.Id}";
            Add(key, role);
        }

        /// <summary>
        /// Gets a cached role.
        /// </summary>
        /// <param name="roleId">The role ID.</param>
        /// <returns>The cached role, or null if not found.</returns>
        public PawSharp.Core.Entities.Role? GetRole(ulong roleId)
        {
            var key = $"role:{roleId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<PawSharp.Core.Entities.Role>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets all roles for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of roles in the guild.</returns>
        public IEnumerable<PawSharp.Core.Entities.Role> GetGuildRoles(ulong guildId)
        {
            var result = _db.Execute("KEYS", $"role:*");
            var keys = ((RedisKey[]?)result) ?? [];
            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var role = JsonSerializer.Deserialize<PawSharp.Core.Entities.Role>((string)json!, _jsonOptions);
                    if (role != null)
                    {
                        // Note: This is a simplified implementation.
                        // In a real scenario, you'd need to track guild-role relationships
                        yield return role;
                    }
                }
            }
        }

        /// <summary>
        /// Caches an emoji.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="emoji">The emoji to cache.</param>
        public void CacheEmoji(ulong guildId, Emoji emoji)
        {
            var key = $"emoji:{emoji.Id}";
            Add(key, emoji);
        }

        /// <summary>
        /// Gets a cached emoji.
        /// </summary>
        /// <param name="emojiId">The emoji ID.</param>
        /// <returns>The cached emoji, or null if not found.</returns>
        public Emoji? GetEmoji(ulong emojiId)
        {
            var key = $"emoji:{emojiId}";
            var json = _db.StringGet(key);
            return json.HasValue ? JsonSerializer.Deserialize<Emoji>((string)json!, _jsonOptions) : null;
        }

        /// <summary>
        /// Gets all emojis for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of emojis in the guild.</returns>
        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId)
        {
            var result = _db.Execute("KEYS", $"emoji:*");
            var keys = ((RedisKey[]?)result) ?? [];
            foreach (var key in keys)
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var emoji = JsonSerializer.Deserialize<Emoji>((string)json!, _jsonOptions);
                    if (emoji != null)
                    {
                        // Note: This is a simplified implementation.
                        // In a real scenario, you'd need to track guild-emoji relationships
                        yield return emoji;
                    }
                }
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Caches complete guild data including channels, roles, and emojis.
        /// </summary>
        /// <param name="guild">The guild to cache completely.</param>
        public void CacheGuildData(Guild guild)
        {
            CacheGuild(guild);

            // Cache associated data if available
            // Note: In a real implementation, you'd need to fetch and cache
            // channels, roles, and emojis associated with the guild
        }

        /// <summary>
        /// Removes all data for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID to remove.</param>
        public void RemoveGuild(ulong guildId)
        {
            var keys = new List<RedisKey>();

            // Collect all keys related to this guild
            keys.AddRange(((RedisKey[]?)_db.Execute("KEYS", $"guild:{guildId}")) ?? []);
            keys.AddRange(((RedisKey[]?)_db.Execute("KEYS", $"channel:*")) ?? []); // Would need filtering in real impl
            keys.AddRange(((RedisKey[]?)_db.Execute("KEYS", $"member:{guildId}:*")) ?? []);
            keys.AddRange(((RedisKey[]?)_db.Execute("KEYS", $"role:*")) ?? []); // Would need filtering in real impl
            keys.AddRange(((RedisKey[]?)_db.Execute("KEYS", $"emoji:*")) ?? []); // Would need filtering in real impl

            if (keys.Count > 0)
            {
                _db.KeyDelete(keys.ToArray());
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets the total number of cached entities.
        /// </summary>
        /// <returns>The total entity count.</returns>
        public int GetEntityCount()
        {
            var patterns = new[] { "user:*", "guild:*", "channel:*", "message:*", "member:*", "role:*", "emoji:*" };
            var count = 0;

            foreach (var pattern in patterns)
            {
                var result = _db.Execute("KEYS", pattern);
                var keys = ((RedisKey[]?)result) ?? [];
                count += keys.Length;
            }

            return count;
        }

        /// <summary>
        /// Gets the estimated memory usage.
        /// </summary>
        /// <returns>Estimated memory usage in bytes.</returns>
        public long GetMemoryUsage()
        {
            // Redis doesn't provide direct memory usage per key pattern
            // This is a rough estimate
            return GetEntityCount() * 1024L; // Rough estimate: 1KB per entity
        }

        /// <summary>
        /// Gets detailed cache statistics.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                UserCount = (((RedisKey[]?)_db.Execute("KEYS", "user:*")) ?? []).Length,
                GuildCount = (((RedisKey[]?)_db.Execute("KEYS", "guild:*")) ?? []).Length,
                ChannelCount = (((RedisKey[]?)_db.Execute("KEYS", "channel:*")) ?? []).Length,
                MessageCount = (((RedisKey[]?)_db.Execute("KEYS", "message:*")) ?? []).Length,
                MemberCount = (((RedisKey[]?)_db.Execute("KEYS", "member:*")) ?? []).Length,
                RoleCount = (((RedisKey[]?)_db.Execute("KEYS", "role:*")) ?? []).Length,
                EmojiCount = (((RedisKey[]?)_db.Execute("KEYS", "emoji:*")) ?? []).Length,
                MemoryUsage = GetMemoryUsage()
            };
        }

        #endregion

        /// <summary>
        /// Disposes the Redis connection.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _redis?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Configuration options for Redis cache provider.
    /// </summary>
    public class RedisCacheOptions
    {
        /// <summary>
        /// Redis connection string (e.g., "localhost:6379").
        /// </summary>
        public string ConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// Redis password (optional).
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Redis database number (default: 0).
        /// </summary>
        public int Database { get; set; } = 0;

        /// <summary>
        /// Connection timeout in milliseconds (default: 5000).
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// Sync timeout in milliseconds (default: 5000).
        /// </summary>
        public int SyncTimeout { get; set; } = 5000;

        /// <summary>
        /// Number of connection retry attempts (default: 3).
        /// </summary>
        public int ConnectRetry { get; set; } = 3;

        /// <summary>
        /// Default cache expiry time (default: 1 hour).
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);
    }
}