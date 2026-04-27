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
    /// API version to use (default: 10).
    /// </summary>
    public int ApiVersion { get; set; } = 10;
    
    /// <summary>
    /// Whether to enable gateway compression (default: false).
    /// When enabled, uses zlib-stream transport compression which can reduce
    /// bandwidth by up to 40% for high-volume bots.
    /// </summary>
    public bool EnableCompression { get; set; } = false;
    
    /// <summary>
    /// Maximum number of missed heartbeat acknowledgments before reconnecting (default: 3).
    /// </summary>
    public int MaxMissedHeartbeatAcks { get; set; } = 3;

    /// <summary>
    /// Event dispatching configuration options.
    /// </summary>
    public EventDispatchOptions EventDispatch { get; set; } = new EventDispatchOptions();
    
    /// <summary>
    /// Cache configuration options.
    /// </summary>
    public CacheOptions Cache { get; set; } = new CacheOptions();

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
        public int MaxQueueSize { get; set; } = 1000;

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
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// Whether to enable array pooling for WebSocket receive buffers (default: true).
        /// Reduces GC pressure by reusing large byte arrays.
        /// </summary>
        public bool EnableArrayPooling { get; set; } = true;
    }
}