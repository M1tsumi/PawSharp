#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Metrics;
using PawSharp.Core.Models;

namespace PawSharp.Gateway;

/// <summary>
/// Manages automatic reconnection with exponential backoff.
/// </summary>
public class ReconnectionManager
{
    private readonly int _maxReconnectionAttempts;
    private readonly int _initialBackoffMs;
    private readonly int _maxBackoffMs;
    private readonly double _jitterFactor;

    private readonly ILogger _logger;
    private readonly IPerformanceMetrics? _metrics;
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

    /// <summary>
    /// Creates a new ReconnectionManager with configurable backoff parameters.
    /// </summary>
    public ReconnectionManager(ILogger logger, IPerformanceMetrics? metrics = null, PawSharpOptions.ReconnectionOptions? options = null)
    {
        _logger = logger;
        _metrics = metrics;
        
        var opts = options ?? new PawSharpOptions.ReconnectionOptions();
        _maxReconnectionAttempts = opts.MaxAttempts;
        _initialBackoffMs = opts.InitialDelayMs;
        _maxBackoffMs = opts.MaxDelayMs;
        _jitterFactor = opts.JitterFactor;
        
        Reset();
    }

    /// <summary>
    /// Gets the current number of reconnection attempts made.
    /// </summary>
    public int AttemptsCount => _reconnectionAttempts;

    /// <summary>
    /// Gets whether we can still attempt to reconnect.
    /// </summary>
    public bool CanReconnect => _reconnectionAttempts < _maxReconnectionAttempts;

    /// <summary>
    /// Gets the maximum number of reconnection attempts configured.
    /// </summary>
    public int MaxAttempts => _maxReconnectionAttempts;

    /// <summary>
    /// Reset the reconnection counter for a new connection.
    /// </summary>
    public void Reset()
    {
        _reconnectionAttempts = 0;
        _currentBackoffMs = _initialBackoffMs;
    }

    /// <summary>
    /// Attempt to reconnect with exponential backoff.
    /// </summary>
    public async Task<bool> ReconnectAsync()
    {
        if (!CanReconnect)
        {
            _logger.LogError("Maximum reconnection attempts exceeded. Giving up.");
            if (OnReconnectionFailed is { } failedHandler) await failedHandler().ConfigureAwait(false);
            return false;
        }

        _reconnectionAttempts++;
        
        // Apply jitter to spread reconnects across time.
        // Using Random.Shared for thread-safe, allocation-free random numbers.
        var jitter = (int)(_currentBackoffMs * _jitterFactor * (2.0 * Random.Shared.NextDouble() - 1.0));
        var delayMs = Math.Max(0, _currentBackoffMs + jitter);
        
        _logger.LogWarning("Reconnection attempt {Attempt}/{Max} in {BackoffMs}ms (jitter: {JitterMs}ms)", 
            _reconnectionAttempts, _maxReconnectionAttempts, delayMs, jitter);

        _metrics?.RecordReconnection();

        await Task.Delay(delayMs).ConfigureAwait(false);

        if (OnReconnectionAttempt is { } attemptHandler) await attemptHandler(_reconnectionAttempts).ConfigureAwait(false);

        // Exponential backoff: double the backoff time
        _currentBackoffMs = Math.Min(_currentBackoffMs * 2, _maxBackoffMs);

        return true;
    }

    /// <summary>
    /// Get the current backoff duration in milliseconds.
    /// </summary>
    public int GetCurrentBackoffMs() => _currentBackoffMs;
}
