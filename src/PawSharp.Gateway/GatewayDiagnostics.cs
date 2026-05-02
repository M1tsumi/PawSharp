#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PawSharp.Gateway;

/// <summary>
/// Provides detailed connection diagnostics for debugging gateway issues.
/// Tracks state changes, events, and connection metrics.
/// </summary>
public class GatewayDiagnostics
{
    private readonly Stopwatch _uptime = new();
    private readonly List<StateChangeRecord> _stateChanges = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, long> _eventCounts = new();
    private DateTimeOffset? _lastHeartbeatSent;
    private DateTimeOffset? _lastHeartbeatAck;
    private DateTimeOffset? _lastEventReceived;
    private int _messagesReceived;
    private int _messagesSent;
    private int _reconnectCount;
    private int _missedAckCount;
    private string? _lastError;
    private string? _currentGatewayUrl;
    private string? _sessionId;
    private int? _sequenceNumber;

    /// <summary>
    /// Records a state change.
    /// </summary>
    public void RecordStateChange(GatewayState oldState, GatewayState newState, string? reason = null)
    {
        lock (_lock)
        {
            _stateChanges.Add(new StateChangeRecord
            {
                Timestamp = DateTimeOffset.UtcNow,
                OldState = oldState,
                NewState = newState,
                Reason = reason
            });

            if (newState == GatewayState.Ready && !_uptime.IsRunning)
            {
                _uptime.Start();
            }
            else if (newState == GatewayState.Disconnected || newState == GatewayState.Failed)
            {
                _uptime.Reset();
            }
        }
    }

    /// <summary>
    /// Records an event being received.
    /// </summary>
    public void RecordEventReceived(string eventName)
    {
        lock (_lock)
        {
            _lastEventReceived = DateTimeOffset.UtcNow;
            _messagesReceived++;
            
            if (!_eventCounts.TryGetValue(eventName, out var count))
            {
                _eventCounts[eventName] = 1;
            }
            else
            {
                _eventCounts[eventName] = count + 1;
            }
        }
    }

    /// <summary>
    /// Records a message being sent.
    /// </summary>
    public void RecordMessageSent()
    {
        lock (_lock)
        {
            _messagesSent++;
        }
    }

    /// <summary>
    /// Records heartbeat information.
    /// </summary>
    public void RecordHeartbeatSent()
    {
        lock (_lock)
        {
            _lastHeartbeatSent = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Records heartbeat ACK received.
    /// </summary>
    public void RecordHeartbeatAck()
    {
        lock (_lock)
        {
            _lastHeartbeatAck = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Records a missed heartbeat ACK.
    /// </summary>
    public void RecordMissedAck()
    {
        lock (_lock)
        {
            _missedAckCount++;
        }
    }

    /// <summary>
    /// Records a reconnection.
    /// </summary>
    public void RecordReconnection(string reason)
    {
        lock (_lock)
        {
            _reconnectCount++;
            RecordStateChange(GatewayState.Connected, GatewayState.Connecting, reason);
        }
    }

    /// <summary>
    /// Records an error.
    /// </summary>
    public void RecordError(string error)
    {
        lock (_lock)
        {
            _lastError = error;
        }
    }

    /// <summary>
    /// Updates connection info.
    /// </summary>
    public void UpdateConnectionInfo(string gatewayUrl, string? sessionId, int? sequenceNumber)
    {
        lock (_lock)
        {
            _currentGatewayUrl = gatewayUrl;
            _sessionId = sessionId;
            _sequenceNumber = sequenceNumber;
        }
    }

    /// <summary>
    /// Gets a snapshot of current diagnostics.
    /// </summary>
    public DiagnosticsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new DiagnosticsSnapshot
            {
                CurrentTime = DateTimeOffset.UtcNow,
                Uptime = _uptime.IsRunning ? _uptime.Elapsed : TimeSpan.Zero,
                CurrentState = _stateChanges.LastOrDefault()?.NewState ?? GatewayState.Disconnected,
                LastStateChange = _stateChanges.LastOrDefault(),
                StateChangeHistory = _stateChanges.TakeLast(10).ToList(),
                LastHeartbeatSent = _lastHeartbeatSent,
                LastHeartbeatAck = _lastHeartbeatAck,
                LastEventReceived = _lastEventReceived,
                HeartbeatLatency = _lastHeartbeatSent.HasValue && _lastHeartbeatAck.HasValue
                    ? (TimeSpan?)(_lastHeartbeatAck.Value - _lastHeartbeatSent.Value)
                    : null,
                MessagesReceived = _messagesReceived,
                MessagesSent = _messagesSent,
                ReconnectCount = _reconnectCount,
                MissedAckCount = _missedAckCount,
                LastError = _lastError,
                CurrentGatewayUrl = _currentGatewayUrl,
                SessionId = _sessionId,
                SequenceNumber = _sequenceNumber,
                TopEvents = _eventCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }
    }

    /// <summary>
    /// Resets all diagnostics data.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _stateChanges.Clear();
            _eventCounts.Clear();
            _uptime.Reset();
            _lastHeartbeatSent = null;
            _lastHeartbeatAck = null;
            _lastEventReceived = null;
            _messagesReceived = 0;
            _messagesSent = 0;
            _reconnectCount = 0;
            _missedAckCount = 0;
            _lastError = null;
            _currentGatewayUrl = null;
            _sessionId = null;
            _sequenceNumber = null;
        }
    }

    /// <summary>
    /// Represents a state change record.
    /// </summary>
    public class StateChangeRecord
    {
        public DateTimeOffset Timestamp { get; set; }
        public GatewayState OldState { get; set; }
        public GatewayState NewState { get; set; }
        public string? Reason { get; set; }

        public override string ToString() => 
            $"[{Timestamp:HH:mm:ss}] {OldState} -> {NewState}{(Reason != null ? $" ({Reason})" : "")}";
    }

    /// <summary>
    /// A snapshot of gateway diagnostics at a point in time.
    /// </summary>
    public class DiagnosticsSnapshot
    {
        public DateTimeOffset CurrentTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public GatewayState CurrentState { get; set; }
        public StateChangeRecord? LastStateChange { get; set; }
        public IReadOnlyList<StateChangeRecord> StateChangeHistory { get; set; } = new List<StateChangeRecord>();
        public DateTimeOffset? LastHeartbeatSent { get; set; }
        public DateTimeOffset? LastHeartbeatAck { get; set; }
        public DateTimeOffset? LastEventReceived { get; set; }
        public TimeSpan? HeartbeatLatency { get; set; }
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }
        public int ReconnectCount { get; set; }
        public int MissedAckCount { get; set; }
        public string? LastError { get; set; }
        public string? CurrentGatewayUrl { get; set; }
        public string? SessionId { get; set; }
        public int? SequenceNumber { get; set; }
        public Dictionary<string, long> TopEvents { get; set; } = new();

        /// <summary>
        /// Gets a formatted summary of the diagnostics.
        /// </summary>
        public string GetSummary()
        {
            var lines = new List<string>
            {
                "=== Gateway Diagnostics ===",
                $"Current Time: {CurrentTime:yyyy-MM-dd HH:mm:ss} UTC",
                $"Uptime: {Uptime.TotalMinutes:N1} minutes",
                $"State: {CurrentState}",
                $"Session ID: {SessionId ?? "N/A"}",
                $"Sequence: {SequenceNumber?.ToString() ?? "N/A"}",
                $"Gateway URL: {CurrentGatewayUrl ?? "N/A"}",
                "",
                "=== Heartbeat ===",
                $"Last Sent: {LastHeartbeatSent?.ToString("HH:mm:ss") ?? "N/A"}",
                $"Last ACK: {LastHeartbeatAck?.ToString("HH:mm:ss") ?? "N/A"}",
                $"Latency: {HeartbeatLatency?.TotalMilliseconds.ToString("F0") ?? "N/A"} ms",
                $"Missed ACKs: {MissedAckCount}",
                "",
                "=== Traffic ===",
                $"Messages Received: {MessagesReceived:N0}",
                $"Messages Sent: {MessagesSent:N0}",
                $"Reconnect Count: {ReconnectCount}",
                "",
                "=== Top Events ==="
            };

            foreach (var evt in TopEvents)
            {
                lines.Add($"  {evt.Key}: {evt.Value:N0}");
            }

            if (LastError != null)
            {
                lines.Add("");
                lines.Add("=== Last Error ===");
                lines.Add(LastError);
            }

            if (StateChangeHistory.Any())
            {
                lines.Add("");
                lines.Add("=== Recent State Changes ===");
                foreach (var change in StateChangeHistory.TakeLast(5))
                {
                    lines.Add($"  {change}");
                }
            }

            return string.Join("\n", lines);
        }
    }
}
