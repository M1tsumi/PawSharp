using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway;

/// <summary>
/// Manages automatic reconnection with exponential backoff.
/// </summary>
public class ReconnectionManager
{
    private const int MaxReconnectionAttempts = 10;
    private const int InitialBackoffMs = 1000; // 1 second
    private const int MaxBackoffMs = 16000; // 16 seconds

    private readonly ILogger _logger;
    private int _reconnectionAttempts;
    private int _currentBackoffMs;

    /// <summary>
    /// Fired when reconnection is about to be attempted.
    /// </summary>
    public event Func<int, Task> OnReconnectionAttempt;

    /// <summary>
    /// Fired when all reconnection attempts have been exhausted.
    /// </summary>
    public event Func<Task> OnReconnectionFailed;

    public ReconnectionManager(ILogger logger)
    {
        _logger = logger;
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
            OnReconnectionFailed?.Invoke();
            return false;
        }

        _reconnectionAttempts++;
        _logger.LogWarning($"Reconnection attempt {_reconnectionAttempts}/{MaxReconnectionAttempts} in {_currentBackoffMs}ms");

        await Task.Delay(_currentBackoffMs);

        OnReconnectionAttempt?.Invoke(_reconnectionAttempts);

        // Exponential backoff: double the backoff time, capped at 16 seconds
        _currentBackoffMs = Math.Min(_currentBackoffMs * 2, MaxBackoffMs);

        return true;
    }

    /// <summary>
    /// Get the current backoff duration in milliseconds.
    /// </summary>
    public int GetCurrentBackoffMs() => _currentBackoffMs;
}
