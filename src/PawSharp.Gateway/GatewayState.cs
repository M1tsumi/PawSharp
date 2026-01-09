namespace PawSharp.Gateway;

/// <summary>
/// Represents the current state of the gateway connection.
/// </summary>
public enum GatewayState
{
    /// <summary>
    /// Not connected to the gateway.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Currently attempting to connect to the gateway.
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Connected to the gateway but not yet received READY.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connected and READY event received - fully operational.
    /// </summary>
    Ready = 3,

    /// <summary>
    /// Connection failed and all reconnection attempts exhausted.
    /// </summary>
    Failed = 4
}
