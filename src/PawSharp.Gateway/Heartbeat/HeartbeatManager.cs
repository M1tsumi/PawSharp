using System;
using System.Timers;
using System.Threading.Tasks;

namespace PawSharp.Gateway.Heartbeat
{
    public class HeartbeatManager
    {
        private readonly Timer _heartbeatTimer;
        private readonly int _heartbeatInterval;
        private bool _isConnected;
        private readonly Func<Task> _sendHeartbeat;

        public HeartbeatManager(int heartbeatInterval, Func<Task> sendHeartbeat = null)
        {
            _heartbeatInterval = heartbeatInterval;
            _sendHeartbeat = sendHeartbeat ?? (() => Task.CompletedTask);
            _heartbeatTimer = new Timer(_heartbeatInterval);
            _heartbeatTimer.Elapsed += OnHeartbeatElapsed;
        }

        public void Start()
        {
            _isConnected = true;
            _heartbeatTimer.Start();
        }

        public void Stop()
        {
            _isConnected = false;
            _heartbeatTimer.Stop();
        }

        private async void OnHeartbeatElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isConnected)
            {
                await _sendHeartbeat();
            }
        }
    }
}