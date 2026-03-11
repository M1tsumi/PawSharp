#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Metrics;

namespace PawSharp.Gateway;

/// <summary>
/// Manages automatic reconnection with exponential backoff.
/// </summary>
public class ReconnectionManager
{
    private const int MaxReconnectionAttempts = 10;
    private const int InitialBackoffMs = 1000; // 1 second
    private const int MaxBackoffMs = 16000; // 16 seconds
    // Up to ±25 % jitter is applied so that many shards reconnecting at the same
    // time do not all hammer the gateway in lock-step (thundering-herd prevention).
    private const double JitterFactor = 0.25;

    private readonly ILogger _logger;
    private readonly IPerformanceMetrics? _metrics;
    private readonly Random _rng = new();
    private int _reconnectionAttempts;
    private int _currentBackoffMs;

    /// <summary>
    /// Fired when reconnection is about to be attempted.
    /// </summary>
    public event Func<int, Task>? OnReconnectionAttempt;

    /// <summary>
    /// Fired when all reconnection attempts have been exhausted.
    /// </summary>
    public event Func<Task>? OnReconnectionFailed;

    public ReconnectionManager(ILogger logger, IPerformanceMetrics? metrics = null)
    {
        _logger = logger;
        _metrics = metrics;
        Reset();
    }

    /// <summary>
    /// Gets the current number of reconnection attempts made.
    /// </summary>
    public int AttemptsCount => _reconnectionAttempts;

    /// <summary>
    /// Gets whether we can still attempt to reconnect.
    /// </summary>
    public bool CanReconnect => _reconnectionAttempts < MaxReconnectionAttempts;

    /// <summary>
    /// Reset the reconnection counter for a new connection.
    /// </summary>
    public void Reset()
    {
        _reconnectionAttempts = 0;
        _currentBackoffMs = InitialBackoffMs;
    }

    /// <summary>
    /// Attempt to reconnect with exponential backoff.
    /// </summary>
    public async Task<bool> ReconnectAsync()
    {
        if (!CanReconnect)
        {
            _logger.LogError("Maximum reconnection attempts exceeded. Giving up.");
            if (OnReconnectionFailed is { } failedHandler) await failedHandler();
            return false;
        }

        _reconnectionAttempts++;
        
        // Apply ±JitterFactor jitter to spread reconnects across time.
        var jitter = (int)(_currentBackoffMs * JitterFactor * (2.0 * _rng.NextDouble() - 1.0));
        var delayMs = Math.Max(0, _currentBackoffMs + jitter);
        
        _logger.LogWarning("Reconnection attempt {Attempt}/{Max} in {BackoffMs}ms (jitter: {JitterMs}ms)", _reconnectionAttempts, MaxReconnectionAttempts, delayMs, jitter);

        _metrics?.RecordReconnection();

        await Task.Delay(delayMs);

        if (OnReconnectionAttempt is { } attemptHandler) await attemptHandler(_reconnectionAttempts);

        // Exponential backoff: double the backoff time, capped at 16 seconds
        _currentBackoffMs = Math.Min(_currentBackoffMs * 2, MaxBackoffMs);

        return true;
    }

    /// <summary>
    /// Get the current backoff duration in milliseconds.
    /// </summary>
    public int GetCurrentBackoffMs() => _currentBackoffMs;
}
