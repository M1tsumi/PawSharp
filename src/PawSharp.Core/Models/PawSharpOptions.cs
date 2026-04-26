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
    /// </summary>
    public bool EnableCompression { get; set; } = false;
    
    /// <summary>
    /// Maximum number of missed heartbeat acknowledgments before reconnecting (default: 3).
    /// </summary>
    public int MaxMissedHeartbeatAcks { get; set; } = 3;
    
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

        /// <summary>
        /// In-memory cache provider configuration.
        /// </summary>
        public MemoryCacheOptions? MemoryCache { get; set; }
    }

    /// <summary>
    /// Configuration options for the in-memory cache provider.
    /// </summary>
    public class MemoryCacheOptions
    {
        /// <summary>
        /// Maximum number of items in the general cache (default: 10000).
        /// </summary>
        public int MaxCacheSize { get; set; } = 10000;

        /// <summary>
        /// Maximum entities per type (default: 5000).
        /// </summary>
        public int MaxEntityCacheSize { get; set; } = 5000;

        /// <summary>
        /// Minimum interval between cleanup operations (default: 5 minutes).
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable memory-based eviction (default: false).
        /// When enabled, the cache will evict items based on memory pressure
        /// in addition to size limits.
        /// </summary>
        public bool EnableMemoryBasedEviction { get; set; } = false;

        /// <summary>
        /// Memory limit in bytes before eviction begins (default: 100MB).
        /// Only used when EnableMemoryBasedEviction is true.
        /// </summary>
        public long MemoryLimitBytes { get; set; } = 100 * 1024 * 1024;
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
}