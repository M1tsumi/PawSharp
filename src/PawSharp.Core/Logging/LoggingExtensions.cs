using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PawSharp.Core.Logging;

/// <summary>
/// Extensions for configuring logging in PawSharp applications.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds PawSharp logging configuration to the service collection.
    /// Configures structured logging with appropriate log levels for each component.
    /// </summary>
    public static ILoggingBuilder AddPawSharpLogging(this ILoggingBuilder builder)
    {
        builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information);
        
        // Set specific log levels for PawSharp components
        builder
            .AddFilter("PawSharp.API", LogLevel.Information)
            .AddFilter("PawSharp.Client", LogLevel.Information)
            .AddFilter("PawSharp.Gateway", LogLevel.Information)
            .AddFilter("PawSharp.Core", LogLevel.Warning);
        
        return builder;
    }

    /// <summary>
    /// Adds PawSharp logging with a custom minimum level.
    /// </summary>
    public static ILoggingBuilder AddPawSharpLogging(this ILoggingBuilder builder, LogLevel minimumLevel)
    {
        builder
            .AddConsole()
            .SetMinimumLevel(minimumLevel);
        
        builder
            .AddFilter("PawSharp.API", minimumLevel)
            .AddFilter("PawSharp.Client", minimumLevel)
            .AddFilter("PawSharp.Gateway", minimumLevel)
            .AddFilter("PawSharp.Core", minimumLevel);
        
        return builder;
    }
}

/// <summary>
/// Common logging message templates for PawSharp.
/// </summary>
public static class PawSharpLogEvents
{
    // API Events
    public const string ApiRequestStarted = "API request started: {Method} {Endpoint}";
    public const string ApiRequestCompleted = "API request completed: {Method} {Endpoint} - Status: {StatusCode} - Duration: {DurationMs}ms";
    public const string ApiRequestFailed = "API request failed: {Method} {Endpoint} - Status: {StatusCode}";
    public const string ApiRateLimitHit = "Rate limit hit on route: {BucketId} - Reset: {ResetTime}";
    public const string ApiRateLimitRetry = "Retrying request after rate limit - Route: {BucketId} - Wait: {WaitTimeMs}ms";
    
    // Cache Events
    public const string CacheHit = "Cache hit for {EntityType}: {EntityId}";
    public const string CacheMiss = "Cache miss for {EntityType}: {EntityId}";
    public const string CacheEviction = "Cache eviction triggered: {EntityType} - Items: {ItemCount}";
    public const string CacheCleared = "Cache cleared for {EntityType}";
    
    // Gateway Events
    public const string GatewayConnecting = "Gateway connecting... URL: {GatewayUrl}";
    public const string GatewayConnected = "Gateway connected successfully";
    public const string GatewayDisconnected = "Gateway disconnected - Code: {CloseCode} - Reason: {CloseReason}";
    public const string GatewayReconnecting = "Gateway reconnecting - Attempt: {Attempt}/{MaxAttempts} - Wait: {WaitTimeMs}ms";
    public const string GatewayReady = "Gateway ready - Session: {SessionId} - Resume URL: {ResumeUrl}";
    public const string HeartbeatSent = "Heartbeat sent - Sequence: {Sequence}";
    public const string HeartbeatAckReceived = "Heartbeat ACK received - Latency: {LatencyMs}ms";
    
    // Validation Events
    public const string ValidationFailed = "Validation failed: {Reason}";
    public const string ValidationWarning = "Validation warning: {Message}";
}
