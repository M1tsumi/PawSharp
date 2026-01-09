using System;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Heartbeat
{
    public class HeartbeatManager
    {
        private readonly Timer _heartbeatTimer;
        private readonly int _heartbeatInterval;
        private bool _isConnected;
        private readonly Func<Task> _sendHeartbeat;
        private readonly ILogger _logger;
        
        private bool _ackReceived = true;
        private int _missedAcks = 0;
        private const int MaxMissedAcks = 2; // If we miss 2 ACKs, connection is zombie

        /// <summary>
        /// Fired when a heartbeat is sent.
        /// </summary>
        public event Func<Task> OnHeartbeatSent;

        /// <summary>
        /// Fired when a heartbeat ACK is received.
        /// </summary>
        public event Func<Task> OnHeartbeatAckReceived;

        /// <summary>
        /// Fired when heartbeat ACKs are not being received (zombie connection).
        /// </summary>
        public event Func<Task> OnZombieConnection;

        public HeartbeatManager(int heartbeatInterval, Func<Task> sendHeartbeat = null, ILogger logger = null)
        {
            _heartbeatInterval = heartbeatInterval;
            _sendHeartbeat = sendHeartbeat ?? (() => Task.CompletedTask);
            _logger = logger;
            _heartbeatTimer = new Timer(_heartbeatInterval);
            _heartbeatTimer.Elapsed += OnHeartbeatElapsed;
        }

        /// <summary>
        /// Gets whether the connection is healthy based on ACK tracking.
        /// </summary>
        public bool IsHealthy => _missedAcks < MaxMissedAcks;

        public void Start()
        {
            _isConnected = true;
            _ackReceived = true;
            _missedAcks = 0;
            _heartbeatTimer.Start();
        }

        public void Stop()
        {
            _isConnected = false;
            _heartbeatTimer.Stop();
        }

        /// <summary>
        /// Mark that a heartbeat ACK was received.
        /// </summary>
        public async Task ReceiveAckAsync()
        {
            _ackReceived = true;
            _missedAcks = 0;
            _logger?.LogDebug("Heartbeat ACK received - connection healthy");
            OnHeartbeatAckReceived?.Invoke();
            await Task.CompletedTask;
        }

        private async void OnHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isConnected)
            {
                // Check if we received the ACK from the last heartbeat
                if (!_ackReceived)
                {
                    _missedAcks++;
                    _logger?.LogWarning($"Heartbeat ACK not received - missed {_missedAcks}/{MaxMissedAcks}");

                    if (!IsHealthy)
                    {
                        _logger?.LogError("Connection is zombie - no heartbeat ACKs received!");
                        OnZombieConnection?.Invoke();
                    }
                }
                else
                {
                    _ackReceived = false; // Expect new ACK after next heartbeat
                }

                await _sendHeartbeat();
                OnHeartbeatSent?.Invoke();
            }
        }
    }
}