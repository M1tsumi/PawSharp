#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Heartbeat
{
    public class HeartbeatManager
    {
        private readonly int _heartbeatInterval;
        private readonly Func<Task> _sendHeartbeat;
        private readonly ILogger? _logger;

        // volatile ensures the JIT/CPU does not cache this across threads:
        // the gateway receive loop writes it and the heartbeat loop reads it.
        private volatile bool _ackReceived = true;
        private int _missedAcks = 0;
        private readonly int _maxMissedAcks;

        private CancellationTokenSource? _cts;
        private Task? _heartbeatTask;

        /// <summary>
        /// Fired when a heartbeat is sent.
        /// </summary>
        public event Func<Task>? OnHeartbeatSent;

        /// <summary>
        /// Fired when a heartbeat ACK is received.
        /// </summary>
        public event Func<Task>? OnHeartbeatAckReceived;

        /// <summary>
        /// Fired when heartbeat ACKs are not being received (zombie connection).
        /// </summary>
        public event Func<Task>? OnZombieConnection;

        public HeartbeatManager(int heartbeatInterval, Func<Task>? sendHeartbeat = null, ILogger? logger = null, int maxMissedAcks = 2)
        {
            _heartbeatInterval = heartbeatInterval;
            _sendHeartbeat = sendHeartbeat ?? (() => Task.CompletedTask);
            _logger = logger;
            _maxMissedAcks = maxMissedAcks;
        }

        /// <summary>
        /// Gets whether the connection is healthy based on ACK tracking.
        /// </summary>
        public bool IsHealthy => _missedAcks < _maxMissedAcks;

        public void Start()
        {
            _ackReceived = true;
            _missedAcks = 0;
            _cts = new CancellationTokenSource();
            // Fire-and-store: exceptions are caught inside the loop, not propagated as async void.
            _heartbeatTask = RunHeartbeatLoopAsync(_cts.Token);
        }

        /// <summary>
        /// Stops the heartbeat manager and waits for the heartbeat task to complete.
        /// Use this overload during graceful shutdown to ensure proper cleanup.
        /// </summary>
        /// <param name="timeout">Maximum time to wait for the heartbeat task to complete (default: 5 seconds)</param>
        public async Task StopAsync(TimeSpan? timeout = null)
        {
            if (_cts?.IsCancellationRequested == false)
            {
                _cts.Cancel();
            }

            // Wait for the heartbeat task to complete (with timeout to prevent hanging)
            if (_heartbeatTask != null)
            {
                var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
                try
                {
                    await _heartbeatTask.WaitAsync(effectiveTimeout);
                }
                catch (TimeoutException)
                {
                    _logger?.LogWarning("Heartbeat task did not complete within {TimeoutMs}ms, forcing stop", effectiveTimeout.TotalMilliseconds);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger?.LogError(ex, "Error waiting for heartbeat task to complete");
                }
            }

            // Clean up resources
            _cts?.Dispose();
            _cts = null;
            _heartbeatTask = null;
        }

        /// <summary>
        /// Stops the heartbeat manager without waiting for task completion.
        /// Use StopAsync() for graceful shutdown to prevent task leaks.
        /// </summary>
        [Obsolete("Use StopAsync() for proper cleanup to prevent task leaks")]
        public void Stop()
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Mark that a heartbeat ACK was received.
        /// </summary>
        public async Task ReceiveAckAsync()
        {
            _ackReceived = true;
            _missedAcks = 0;
            _logger?.LogDebug("Heartbeat ACK received - connection healthy");
            if (OnHeartbeatAckReceived is { } ackHandler)
                await ackHandler();
        }

        private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_heartbeatInterval));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        if (!_ackReceived)
                        {
                            _missedAcks++;
                            _logger?.LogWarning("Heartbeat ACK not received - missed {Missed}/{Max}", _missedAcks, _maxMissedAcks);

                            if (!IsHealthy)
                            {
                                _logger?.LogError("Connection is zombie - no heartbeat ACKs received!");
                                if (OnZombieConnection is { } zombieHandler)
                                    await zombieHandler();
                            }
                        }
                        else
                        {
                            _ackReceived = false; // Expect a new ACK after the next heartbeat
                        }

                        await _sendHeartbeat();
                        if (OnHeartbeatSent is { } sentHandler)
                            await sentHandler();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Heartbeat loop iteration threw unexpectedly");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown via Stop() — not an error.
            }
        }
    }
}