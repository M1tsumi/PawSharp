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
}