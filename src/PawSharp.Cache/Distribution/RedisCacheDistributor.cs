#nullable enable
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.Cache.Exceptions;
using PawSharp.Core.Entities;
using StackExchange.Redis;

namespace PawSharp.Cache.Distribution
{
    /// <summary>
    /// Distributes cache invalidation events across multiple bot instances using Redis pub/sub.
    /// </summary>
    public class RedisCacheDistributor : IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly string _channelPrefix;
        private readonly ISubscriber? _subscriber;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        /// <summary>
        /// Event raised when a cache invalidation is received from another instance.
        /// </summary>
        public event EventHandler<CacheInvalidationMessage>? CacheInvalidationReceived;

        /// <summary>
        /// Creates a new RedisCacheDistributor instance.
        /// </summary>
        /// <param name="redis">The Redis connection multiplexer.</param>
        /// <param name="channelPrefix">Prefix for Redis pub/sub channels (default: "pawsharp:cache").</param>
        public RedisCacheDistributor(IConnectionMultiplexer redis, string channelPrefix = "pawsharp:cache")
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _channelPrefix = channelPrefix ?? throw new ArgumentNullException(nameof(channelPrefix));
            _subscriber = redis.GetSubscriber();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Starts listening for cache invalidation events.
        /// </summary>
        public void StartListening()
        {
            if (_listenerTask != null)
                return;

            _listenerTask = Task.Run(() => ListenForInvalidationsAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stops listening for cache invalidation events.
        /// </summary>
        public void StopListening()
        {
            _cancellationTokenSource.Cancel();
            
            if (_listenerTask != null)
            {
                try
                {
                    _listenerTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                _listenerTask = null;
            }
        }

        private async Task ListenForInvalidationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var channel = $"{_channelPrefix}:invalidations";
                await _subscriber!.SubscribeAsync(channel, (channel, message) =>
                {
                    try
                    {
                        var invalidation = JsonSerializer.Deserialize<CacheInvalidationMessage>((string)message!);
                        if (invalidation != null)
                        {
                            CacheInvalidationReceived?.Invoke(this, invalidation);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[RedisCacheDistributor] Failed to deserialize invalidation message: {ex.Message}");
                    }
                });

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                throw new CacheDistributionException($"Failed to listen for cache invalidations: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Publishes a cache invalidation event to all other instances.
        /// </summary>
        /// <param name="entityType">The type of entity that was invalidated.</param>
        /// <param name="entityId">The ID of the entity that was invalidated.</param>
        /// <param name="guildId">The guild ID (if applicable).</param>
        public async Task PublishInvalidationAsync(string entityType, ulong entityId, ulong? guildId = null)
        {
            try
            {
                var message = new CacheInvalidationMessage
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    GuildId = guildId,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message);
                var channel = $"{_channelPrefix}:invalidations";
                
                await _subscriber!.PublishAsync(channel, json, StackExchange.Redis.CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                throw new CacheDistributionException($"Failed to publish cache invalidation: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Publishes a cache clear event to all other instances.
        /// </summary>
        public async Task PublishClearAsync()
        {
            try
            {
                var message = new CacheInvalidationMessage
                {
                    EntityType = "CLEAR_ALL",
                    EntityId = 0,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message);
                var channel = $"{_channelPrefix}:invalidations";
                
                await _subscriber!.PublishAsync(channel, json, StackExchange.Redis.CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                throw new CacheDistributionException($"Failed to publish cache clear: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if the distributor is healthy (Redis connection is active).
        /// </summary>
        public bool IsHealthy()
        {
            try
            {
                _redis.GetDatabase().Ping();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopListening();
            _cancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Message format for cache invalidation events.
    /// </summary>
    public class CacheInvalidationMessage
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

        /// <summary>
        /// When the invalidation occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
