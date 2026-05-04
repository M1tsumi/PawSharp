#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Telemetry;
using PawSharp.Core.Entities;
using PawSharp.Core.Serialization;
using StackExchange.Redis;

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
        private readonly ICacheTelemetry? _telemetry;
        private bool _disposed;

        public ICacheTelemetry? Telemetry
        {
            get => _telemetry;
            set => throw new InvalidOperationException("Telemetry is set at construction time.");
        }

        // Metrics tracking
        private long _hits;
        private long _misses;

        // Cache invalidation events
        public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        public event EventHandler? CacheCleared;

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
            _options = new RedisCacheOptions { ConnectionString = connectionString };
            _telemetry = new CacheTelemetry();
        }

        #region Generic Cache Operations

        /// <summary>
        /// Helper method to get keys matching a pattern using Keys (blocking but compatible).
        /// Iterates through all endpoints to support clustered Redis configurations.
        /// </summary>
        private IEnumerable<RedisKey> ScanKeys(string pattern)
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                foreach (var key in server.Keys(pattern: pattern, database: _options.Database))
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="entity">The entity to cache.</param>
        public void Add(string key, object entity)
        {
            var json = JsonSerializer.Serialize(entity, _jsonOptions);
            _db.StringSet(key, json, _options.DefaultExpiration);
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
            CacheCleared?.Invoke(this, EventArgs.Empty);
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
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var expiry = _options.UserExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);
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
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<User>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Caches a guild.
        /// </summary>
        /// <param name="guild">The guild to cache.</param>
        public void CacheGuild(Guild guild)
        {
            var key = $"guild:{guild.Id}";
            var json = JsonSerializer.Serialize(guild, _jsonOptions);
            var expiry = _options.GuildExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);
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
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Guild>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Gets all cached guilds.
        /// </summary>
        /// <returns>An enumerable of all cached guilds.</returns>
        public IEnumerable<Guild> GetAllGuilds()
        {
            foreach (var key in ScanKeys("guild:*"))
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
            var json = JsonSerializer.Serialize(channel, _jsonOptions);
            var expiry = _options.ChannelExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);

            // Maintain a set of channel IDs per guild for efficient lookup
            if (channel.GuildId.HasValue)
            {
                var guildChannelsKey = $"guild:{channel.GuildId}:channels";
                _db.SetAdd(guildChannelsKey, channel.Id.ToString());
                _db.KeyExpire(guildChannelsKey, expiry);
            }
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
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Channel>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Gets all channels for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of channels in the guild.</returns>
        public IEnumerable<Channel> GetGuildChannels(ulong guildId)
        {
            var guildChannelsKey = $"guild:{guildId}:channels";
            var channelIds = _db.SetMembers(guildChannelsKey);

            foreach (var channelIdStr in channelIds)
            {
                if (ulong.TryParse((string?)channelIdStr, out var channelId))
                {
                    var channel = GetChannel(channelId);
                    if (channel != null)
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
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var expiry = _options.MessageExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);

            // Also maintain a sorted set for channel messages
            var channelKey = $"channel:{message.ChannelId}:messages";
            _db.SortedSetAdd(channelKey, message.Id.ToString(), message.Id);
            _db.KeyExpire(channelKey, expiry);
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
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Message>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
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
                if (ulong.TryParse((string?)messageIdStr, out var messageId))
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
            var json = JsonSerializer.Serialize(member, _jsonOptions);
            var expiry = _options.MemberExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);
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
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<GuildMember>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Gets all members for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of members in the guild.</returns>
        public IEnumerable<GuildMember> GetGuildMembers(ulong guildId)
        {
            foreach (var key in ScanKeys($"member:{guildId}:*"))
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
            var key = $"role:{guildId}:{role.Id}";
            var json = JsonSerializer.Serialize(role, _jsonOptions);
            var expiry = _options.RoleExpiration ?? _options.DefaultExpiration;
            _db.StringSet(key, json, expiry);
        }

        /// <summary>
        /// Gets a cached role.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="roleId">The role ID.</param>
        /// <returns>The cached role, or null if not found.</returns>
        public PawSharp.Core.Entities.Role? GetRole(ulong guildId, ulong roleId)
        {
            var key = $"role:{guildId}:{roleId}";
            var json = _db.StringGet(key);
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<PawSharp.Core.Entities.Role>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Gets all roles for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of roles in the guild.</returns>
        public IEnumerable<PawSharp.Core.Entities.Role> GetGuildRoles(ulong guildId)
        {
            foreach (var key in ScanKeys($"role:{guildId}:*"))
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var role = JsonSerializer.Deserialize<PawSharp.Core.Entities.Role>((string)json!, _jsonOptions);
                    if (role != null)
                    {
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
            if (emoji.Id.HasValue)
            {
                var key = $"emoji:{guildId}:{emoji.Id.Value}";
                var json = JsonSerializer.Serialize(emoji, _jsonOptions);
                var expiry = _options.EmojiExpiration ?? _options.DefaultExpiration;
                _db.StringSet(key, json, expiry);
            }
        }

        /// <summary>
        /// Gets a cached emoji.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <param name="emojiId">The emoji ID.</param>
        /// <returns>The cached emoji, or null if not found.</returns>
        public Emoji? GetEmoji(ulong guildId, ulong emojiId)
        {
            var key = $"emoji:{guildId}:{emojiId}";
            var json = _db.StringGet(key);
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Emoji>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        /// <summary>
        /// Gets all emojis for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID.</param>
        /// <returns>An enumerable of emojis in the guild.</returns>
        public IEnumerable<Emoji> GetGuildEmojis(ulong guildId)
        {
            foreach (var key in ScanKeys($"emoji:{guildId}:*"))
            {
                var json = _db.StringGet(key);
                if (json.HasValue)
                {
                    var emoji = JsonSerializer.Deserialize<Emoji>((string)json!, _jsonOptions);
                    if (emoji != null)
                    {
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

            // Cache channels if available
            if (guild.Channels != null)
            {
                foreach (var channel in guild.Channels)
                {
                    CacheChannel(channel);
                }
            }

            // Cache members if available
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

        /// <summary>
        /// Removes all data for a guild.
        /// </summary>
        /// <param name="guildId">The guild ID to remove.</param>
        public void RemoveGuild(ulong guildId)
        {
            var keys = new List<RedisKey>();

            // Collect all keys related to this guild using SCAN
            foreach (var key in ScanKeys($"guild:{guildId}"))
                keys.Add(key);
            foreach (var key in ScanKeys($"member:{guildId}:*"))
                keys.Add(key);
            foreach (var key in ScanKeys($"role:{guildId}:*"))
                keys.Add(key);
            foreach (var key in ScanKeys($"emoji:{guildId}:*"))
                keys.Add(key);

            // Remove the guild channels set
            keys.Add($"guild:{guildId}:channels");

            // For channels, use the guild channels set for efficient lookup
            var guildChannelsKey = $"guild:{guildId}:channels";
            var channelIds = _db.SetMembers(guildChannelsKey);
            foreach (var channelIdStr in channelIds)
            {
                if (ulong.TryParse((string?)channelIdStr, out var channelId))
                {
                    keys.Add($"channel:{channelId}");
                    keys.Add($"channel:{channelId}:messages");
                }
            }

            if (keys.Count > 0)
            {
                _db.KeyDelete(keys.ToArray());
            }
        }

        public void RemoveChannel(ulong channelId)
        {
            _db.KeyDelete($"channel:{channelId}");
            _db.KeyDelete($"channel:{channelId}:messages");
        }

        public void RemoveMessage(ulong messageId)
        {
            _db.KeyDelete($"message:{messageId}");
        }

        public void RemoveGuildMember(ulong guildId, ulong userId)
        {
            _db.KeyDelete($"member:{guildId}:{userId}");
        }

        public void RemoveRole(ulong guildId, ulong roleId)
        {
            _db.KeyDelete($"role:{guildId}:{roleId}");
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
                foreach (var key in ScanKeys(pattern))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets the estimated memory usage.
        /// </summary>
        /// <returns>Estimated memory usage in bytes.</returns>
        public long GetMemoryUsage()
        {
            // Estimate based on entity counts and average sizes
            // These are rough estimates: User~1KB, Guild~2KB, Channel~1KB, Message~2KB, Member~1KB, Role~0.5KB, Emoji~0.5KB
            var stats = GetCacheStats();
            return (stats.UserCount * 1024L) +
                   (stats.GuildCount * 2048L) +
                   (stats.ChannelCount * 1024L) +
                   (stats.MessageCount * 2048L) +
                   (stats.MemberCount * 1024L) +
                   (stats.RoleCount * 512L) +
                   (stats.EmojiCount * 512L);
        }

        /// <summary>
        /// Gets detailed cache statistics.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                UserCount = ScanKeys("user:*").Count(),
                GuildCount = ScanKeys("guild:*").Count(),
                ChannelCount = ScanKeys("channel:*").Count(),
                MessageCount = ScanKeys("message:*").Count(),
                MemberCount = ScanKeys("member:*").Count(),
                RoleCount = ScanKeys("role:*").Count(),
                EmojiCount = ScanKeys("emoji:*").Count(),
                MemoryUsage = GetMemoryUsage(),
                Hits = Interlocked.Read(ref _hits),
                Misses = Interlocked.Read(ref _misses)
            };
        }

        #endregion

        // Async overloads — leverage Redis native async API
        public async Task<User?> GetUserAsync(ulong userId)
        {
            var json = await _db.StringGetAsync($"user:{userId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<User>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<Guild?> GetGuildAsync(ulong guildId)
        {
            var json = await _db.StringGetAsync($"guild:{guildId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Guild>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<Channel?> GetChannelAsync(ulong channelId)
        {
            var json = await _db.StringGetAsync($"channel:{channelId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Channel>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<Message?> GetMessageAsync(ulong messageId)
        {
            var json = await _db.StringGetAsync($"message:{messageId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Message>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId)
        {
            var json = await _db.StringGetAsync($"member:{guildId}:{userId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<GuildMember>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<PawSharp.Core.Entities.Role?> GetRoleAsync(ulong guildId, ulong roleId)
        {
            var json = await _db.StringGetAsync($"role:{guildId}:{roleId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<PawSharp.Core.Entities.Role>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        public async Task<Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId)
        {
            var json = await _db.StringGetAsync($"emoji:{guildId}:{emojiId}");
            if (json.HasValue)
            {
                Interlocked.Increment(ref _hits);
                return JsonSerializer.Deserialize<Emoji>((string)json!, _jsonOptions);
            }
            Interlocked.Increment(ref _misses);
            return null;
        }

        #region Async Cache Operations

        public async Task CacheUserAsync(User user)
        {
            var key = $"user:{user.Id}";
            var json = JsonSerializer.Serialize(user, _jsonOptions);
            var expiry = _options.UserExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);
        }

        public async Task CacheGuildAsync(Guild guild)
        {
            var key = $"guild:{guild.Id}";
            var json = JsonSerializer.Serialize(guild, _jsonOptions);
            var expiry = _options.GuildExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);
        }

        public async Task CacheChannelAsync(Channel channel)
        {
            var key = $"channel:{channel.Id}";
            var json = JsonSerializer.Serialize(channel, _jsonOptions);
            var expiry = _options.ChannelExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);

            // Maintain a set of channel IDs per guild for efficient lookup
            if (channel.GuildId.HasValue)
            {
                var guildChannelsKey = $"guild:{channel.GuildId}:channels";
                await _db.SetAddAsync(guildChannelsKey, channel.Id.ToString());
                await _db.KeyExpireAsync(guildChannelsKey, expiry);
            }
        }

        public async Task CacheMessageAsync(Message message)
        {
            var key = $"message:{message.Id}";
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var expiry = _options.MessageExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);

            // Also maintain a sorted set for channel messages
            var channelKey = $"channel:{message.ChannelId}:messages";
            await _db.SortedSetAddAsync(channelKey, message.Id.ToString(), message.Id);
            await _db.KeyExpireAsync(channelKey, expiry);
        }

        public async Task CacheGuildMemberAsync(ulong guildId, GuildMember member)
        {
            if (member.User is null) return;
            var key = $"member:{guildId}:{member.User.Id}";
            var json = JsonSerializer.Serialize(member, _jsonOptions);
            var expiry = _options.MemberExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);
        }

        public async Task CacheRoleAsync(ulong guildId, PawSharp.Core.Entities.Role role)
        {
            var key = $"role:{guildId}:{role.Id}";
            var json = JsonSerializer.Serialize(role, _jsonOptions);
            var expiry = _options.RoleExpiration ?? _options.DefaultExpiration;
            await _db.StringSetAsync(key, json, expiry);
        }

        public async Task CacheEmojiAsync(ulong guildId, Emoji emoji)
        {
            if (emoji.Id.HasValue)
            {
                var key = $"emoji:{guildId}:{emoji.Id.Value}";
                var json = JsonSerializer.Serialize(emoji, _jsonOptions);
                var expiry = _options.EmojiExpiration ?? _options.DefaultExpiration;
                await _db.StringSetAsync(key, json, expiry);
            }
        }

        public async Task CacheGuildDataAsync(Guild guild)
        {
            await CacheGuildAsync(guild);

            if (guild.Channels != null)
            {
                foreach (var channel in guild.Channels)
                {
                    await CacheChannelAsync(channel);
                }
            }

            if (guild.Members != null)
            {
                foreach (var member in guild.Members)
                {
                    await CacheGuildMemberAsync(guild.Id, member);
                }
            }

            if (guild.Roles != null)
            {
                foreach (var role in guild.Roles)
                {
                    await CacheRoleAsync(guild.Id, role);
                }
            }

            if (guild.Emojis != null)
            {
                foreach (var emoji in guild.Emojis)
                {
                    await CacheEmojiAsync(guild.Id, emoji);
                }
            }
        }

        public async Task RemoveGuildAsync(ulong guildId)
        {
            var keys = new List<RedisKey>();

            // Collect all keys related to this guild using SCAN
            foreach (var key in ScanKeys($"guild:{guildId}"))
                keys.Add(key);
            foreach (var key in ScanKeys($"member:{guildId}:*"))
                keys.Add(key);
            foreach (var key in ScanKeys($"role:{guildId}:*"))
                keys.Add(key);
            foreach (var key in ScanKeys($"emoji:{guildId}:*"))
                keys.Add(key);

            // Remove the guild channels set
            keys.Add($"guild:{guildId}:channels");

            // For channels, use the guild channels set for efficient lookup
            var guildChannelsKey = $"guild:{guildId}:channels";
            var channelIds = _db.SetMembers(guildChannelsKey);
            foreach (var channelIdStr in channelIds)
            {
                if (ulong.TryParse((string?)channelIdStr, out var channelId))
                {
                    keys.Add($"channel:{channelId}");
                    keys.Add($"channel:{channelId}:messages");
                }
            }

            if (keys.Count > 0)
            {
                await _db.KeyDeleteAsync(keys.ToArray());
            }
        }

        public async Task ClearAsync()
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                server.FlushDatabase(_options.Database);
            }
            CacheCleared?.Invoke(this, EventArgs.Empty);
        }

        public async Task RemoveChannelAsync(ulong channelId)
        {
            await _db.KeyDeleteAsync($"channel:{channelId}");
            await _db.KeyDeleteAsync($"channel:{channelId}:messages");
        }

        public async Task RemoveMessageAsync(ulong messageId)
        {
            await _db.KeyDeleteAsync($"message:{messageId}");
        }

        public async Task RemoveGuildMemberAsync(ulong guildId, ulong userId)
        {
            await _db.KeyDeleteAsync($"member:{guildId}:{userId}");
        }

        public async Task RemoveRoleAsync(ulong guildId, ulong roleId)
        {
            await _db.KeyDeleteAsync($"role:{guildId}:{roleId}");
        }

        #endregion

        /// <summary>
        /// Performs a health check on the Redis cache provider.
        /// </summary>
        /// <returns>True if the cache is healthy, false otherwise.</returns>
        public bool IsHealthy()
        {
            try
            {
                if (!_redis.IsConnected)
                    return false;

                // Perform a simple PING operation to verify actual connectivity
                return _db.Ping() > TimeSpan.Zero;
            }
            catch
            {
                return false;
            }
        }

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
        /// Default cache expiration time (default: 1 hour).
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

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
    }
}