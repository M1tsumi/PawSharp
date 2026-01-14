using System;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Models;

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
    }
}