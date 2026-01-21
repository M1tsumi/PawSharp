namespace PawSharp.Core.Enums;

/// <summary>
/// Represents the status of a gateway shard.
/// </summary>
public enum ShardStatus
{
    /// <summary>
    /// The shard is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The shard is connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// The shard is connected and ready.
    /// </summary>
    Connected,

    /// <summary>
    /// The shard is reconnecting.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The shard has failed and cannot reconnect.
    /// </summary>
    Failed
}