#nullable enable
using System;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Models;

/// <summary>
/// Controls how intent validation behaves when the client connects.
/// </summary>
public enum IntentValidationMode
{
    /// <summary>Skip intent validation completely.</summary>
    Off = 0,

    /// <summary>Log missing intents but continue connecting.</summary>
    Warn = 1,

    /// <summary>Throw when required intents are missing.</summary>
    Strict = 2,
}

/// <summary>
/// Configuration options for PawSharp.
/// </summary>
public class PawSharpOptions
{
    /// <summary>
    /// The Discord bot token.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Gateway intents to subscribe to.
    /// </summary>
    public GatewayIntents Intents { get; set; } = GatewayIntents.AllNonPrivileged;

    /// <summary>
    /// Determines how missing handler intents are handled during <c>ConnectAsync</c>.
    /// Defaults to <see cref="IntentValidationMode.Warn"/> for developer-friendly diagnostics.
    /// </summary>
    public IntentValidationMode IntentValidation { get; set; } = IntentValidationMode.Warn;
    
    /// <summary>
    /// Number of shards for this instance.
    /// </summary>
    public int Shards { get; set; } = 1;
    
    /// <summary>
    /// Total number of shards across all instances.
    /// </summary>
    public int ShardCount { get; set; } = 1;
    
    /// <summary>
    /// Delay in milliseconds between connecting each shard (default: 5000ms).
    /// Discord recommends 5 seconds between shard connections to avoid rate limiting.
    /// </summary>
    public int ShardConnectionDelayMs { get; set; } = 5000;
    
    /// <summary>
    /// API version to use (default: 10).
    /// Valid versions: 10 (Discord recommends always using the latest stable version)
    /// </summary>
    public int ApiVersion { get; set; } = 10;
    
    /// <summary>
    /// Minimum supported gateway API version.
    /// </summary>
    public const int MinSupportedApiVersion = 10;
    
    /// <summary>
    /// Maximum supported gateway API version.
    /// </summary>
    public const int MaxSupportedApiVersion = 10;
    
    /// <summary>
    /// Validates that the configured API version is supported.
    /// Throws ArgumentOutOfRangeException if version is not supported.
    /// </summary>
    public void ValidateApiVersion()
    {
        if (ApiVersion < MinSupportedApiVersion || ApiVersion > MaxSupportedApiVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(ApiVersion), 
                $"API version {ApiVersion} is not supported. Supported versions: {MinSupportedApiVersion}-{MaxSupportedApiVersion}");
        }
    }
    
    /// <summary>
    /// Custom gateway URL for testing/staging environments.
    /// When set, this URL is used instead of fetching from Discord's API.
    /// Format: wss://gateway.example.com (without query parameters)
    /// </summary>
    public string? CustomGatewayUrl { get; set; }
    
    /// <summary>
    /// Reconnection backoff configuration options.
    /// </summary>
    public ReconnectionOptions Reconnection { get; set; } = new ReconnectionOptions();
    
    /// <summary>
    /// Whether to enable gateway compression (default: false).
    /// When enabled, uses zlib-stream transport compression which can reduce
    /// bandwidth by up to 40% for high-volume bots.
    /// </summary>
    public bool EnableCompression { get; set; } = false;
    
    /// <summary>
    /// WebSocket receive buffer size in KB (default: 64).
    /// Larger values reduce loop iterations for large events (e.g., GUILD_CREATE 
    /// for servers with many members). Values above 1024KB (1MB) are not recommended.
    /// For bots in large guilds (10k+ members), consider 128KB or 256KB.
    /// </summary>
    public int WebSocketBufferSizeKb
    {
        get => _webSocketBufferSizeKb;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "WebSocket buffer size must be greater than 0.");
            if (value > 1024)
                throw new ArgumentOutOfRangeException(nameof(value), "WebSocket buffer size should not exceed 1024KB (1MB).");
            _webSocketBufferSizeKb = value;
        }
    }
    private int _webSocketBufferSizeKb = 64;
    
    /// <summary>
    /// Maximum number of missed heartbeat acknowledgments before reconnecting (default: 3).
    /// </summary>
    public int MaxMissedHeartbeatAcks
    {
        get => _maxMissedHeartbeatAcks;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Max missed heartbeat acks must be at least 1.");
            _maxMissedHeartbeatAcks = value;
        }
    }
    private int _maxMissedHeartbeatAcks = 3;

    /// <summary>
    /// Event dispatching configuration options.
    /// </summary>
    public EventDispatchOptions EventDispatch { get; set; } = new EventDispatchOptions();
    
    /// <summary>
    /// Cache configuration options.
    /// </summary>
    public CacheOptions Cache { get; set; } = new CacheOptions();

    /// <summary>
    /// REST API configuration options.
    /// </summary>
    public RestApiOptions RestApi { get; set; } = new RestApiOptions();

    /// <summary>
    /// Reconnection backoff configuration options.
    /// </summary>
    public class ReconnectionOptions
    {
        /// <summary>
        /// Maximum number of reconnection attempts before giving up (default: 10).
        /// </summary>
        public int MaxAttempts { get; set; } = 10;
        
        /// <summary>
        /// Initial backoff delay in milliseconds (default: 1000ms).
        /// </summary>
        public int InitialDelayMs { get; set; } = 1000;
        
        /// <summary>
        /// Maximum backoff delay in milliseconds (default: 16000ms).
        /// </summary>
        public int MaxDelayMs { get; set; } = 16000;
        
        /// <summary>
        /// Jitter factor for randomizing delays (default: 0.25 = ±25%).
        /// Helps prevent thundering herd issues when many shards reconnect.
        /// </summary>
        public double JitterFactor { get; set; } = 0.25;
    }

    /// <summary>
    /// Cache configuration options.
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// Maximum number of emojis to cache per guild (default: 100).
        /// </summary>
        public int MaxEmojisPerGuild { get; set; } = 100;

        /// <summary>
        /// Maximum number of messages to cache per channel (default: 100).
        /// </summary>
        public int MaxMessagesPerChannel { get; set; } = 100;

        /// <summary>
        /// Maximum number of members to cache per guild (default: 1000).
        /// </summary>
        public int MaxMembersPerGuild { get; set; } = 1000;
    }

    /// <summary>
    /// Initial bot presence shown immediately after connecting to the gateway.
    /// When <see langword="null"/> (default) no initial presence is set and Discord
    /// shows the bot as online with no activity.
    /// </summary>
    public PresenceOptions? Presence { get; set; }

    /// <summary>
    /// Initial presence configuration for the bot.
    /// </summary>
    public class PresenceOptions
    {
        /// <summary>Discord status string: "online", "idle", "dnd", or "invisible".</summary>
        public string Status { get; set; } = "online";

        /// <summary>Activity name shown in the user list (e.g. "with fire").</summary>
        public string? ActivityName { get; set; }

        /// <summary>Activity type. See <c>ActivityType</c> in PawSharp.Core.Entities.</summary>
        public int ActivityType { get; set; } = 0; // 0 = Playing

        /// <summary>Stream URL for Streaming (type 1) activities.</summary>
        public string? StreamUrl { get; set; }
    }

    /// <summary>
    /// Event dispatching configuration for controlling backpressure and parallelism.
    /// </summary>
    public class EventDispatchOptions
    {
        /// <summary>
        /// Maximum number of events to queue before applying backpressure (default: 1000).
        /// When the queue is full, the gateway receive loop will wait until space is available.
        /// Set to 0 to disable backpressure (unbounded queue - not recommended for production).
        /// </summary>
        public int MaxQueueSize
        {
            get => _maxQueueSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Max queue size cannot be negative.");
                _maxQueueSize = value;
            }
        }
        private int _maxQueueSize = 1000;

        /// <summary>
        /// Whether to dispatch event handlers in parallel (default: false).
        /// When enabled, independent handlers execute concurrently for better throughput.
        /// Handlers should be thread-safe when this is enabled.
        /// </summary>
        public bool EnableParallelDispatch { get; set; } = false;

        /// <summary>
        /// Maximum degree of parallelism for handler dispatch (default: 4).
        /// Only used when EnableParallelDispatch is true.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get => _maxDegreeOfParallelism;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Max degree of parallelism must be at least 1.");
                _maxDegreeOfParallelism = value;
            }
        }
        private int _maxDegreeOfParallelism = 4;

        /// <summary>
        /// Whether to enable array pooling for WebSocket receive buffers (default: true).
        /// Reduces GC pressure by reusing large byte arrays.
        /// </summary>
        public bool EnableArrayPooling { get; set; } = true;
        
        /// <summary>
        /// Timeout in milliseconds for async event handlers (default: 0 = disabled).
        /// When set, handlers that exceed this timeout will be cancelled to prevent
        /// slow handlers from blocking the dispatch pipeline.
        /// Set to 0 to disable timeout (not recommended for production).
        /// </summary>
        public int HandlerTimeoutMs
        {
            get => _handlerTimeoutMs;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Handler timeout cannot be negative.");
                _handlerTimeoutMs = value;
            }
        }
        private int _handlerTimeoutMs = 0;
    }

    /// <summary>
    /// REST API configuration options for rate limiting and retry behavior.
    /// </summary>
    public class RestApiOptions
    {
        /// <summary>
        /// Maximum number of retry attempts for rate-limited requests (default: 5).
        /// Set to 0 to disable automatic retries.
        /// </summary>
        public int MaxRateLimitRetries { get; set; } = 5;

        /// <summary>
        /// HTTP request timeout in seconds (default: 30).
        /// Set to 0 to use HttpClient's default timeout (100 seconds).
        /// </summary>
        public int TimeoutSeconds
        {
            get => _timeoutSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Timeout cannot be negative.");
                _timeoutSeconds = value;
            }
        }
        private int _timeoutSeconds = 30;

        /// <summary>
        /// Whether to throw exceptions on API errors instead of returning null (default: false).
        /// When enabled, methods will throw DiscordApiException on non-success responses.
        /// </summary>
        public bool ThrowOnApiError { get; set; } = false;

        /// <summary>
        /// Whether to retry on transient HTTP errors (500, 502, 503, 504) (default: true).
        /// </summary>
        public bool RetryOnTransientErrors { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts for transient errors (default: 3).
        /// </summary>
        public int MaxTransientErrorRetries { get; set; } = 3;
    }
}