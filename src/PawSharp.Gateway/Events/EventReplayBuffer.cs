#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Stores recent events for replay capability, useful for debugging and recovery scenarios.
/// Implements a circular buffer with configurable capacity.
/// </summary>
public class EventReplayBuffer
{
    private readonly int _capacity;
    private readonly ILogger? _logger;
    private readonly object _lock = new();
    private readonly Queue<ReplayEvent> _events;

    /// <summary>
    /// Creates a new event replay buffer.
    /// </summary>
    /// <param name="capacity">Maximum number of events to store</param>
    /// <param name="logger">Optional logger</param>
    public EventReplayBuffer(int capacity, ILogger? logger = null)
    {
        _capacity = Math.Max(1, capacity);
        _logger = logger;
        _events = new Queue<ReplayEvent>(_capacity);
    }

    /// <summary>
    /// Current number of events stored in the buffer.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _events.Count;
            }
        }
    }

    /// <summary>
    /// Maximum capacity of the buffer.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Records an event in the buffer.
    /// </summary>
    public void RecordEvent(string eventName, GatewayEvent eventData, string? rawJson = null)
    {
        var replayEvent = new ReplayEvent
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventName = eventName,
            EventData = eventData,
            RawJson = rawJson,
            SequenceNumber = eventData is GatewayEvent ge ? ge.SequenceNumber : null
        };

        int count;
        lock (_lock)
        {
            if (_events.Count >= _capacity)
            {
                _events.Dequeue(); // Remove oldest
            }
            _events.Enqueue(replayEvent);
            count = _events.Count;
        }

        _logger?.LogDebug("Recorded event {EventName} for replay buffer (count: {Count}/{Capacity})", 
            eventName, count, _capacity);
    }

    /// <summary>
    /// Gets all events in chronological order.
    /// </summary>
    public IReadOnlyList<ReplayEvent> GetAllEvents()
    {
        lock (_lock)
        {
            return _events.ToList();
        }
    }

    /// <summary>
    /// Gets events that occurred after a specific timestamp.
    /// </summary>
    public IReadOnlyList<ReplayEvent> GetEventsAfter(DateTimeOffset timestamp)
    {
        lock (_lock)
        {
            return _events.Where(e => e.Timestamp > timestamp).ToList();
        }
    }

    /// <summary>
    /// Gets events by name.
    /// </summary>
    public IReadOnlyList<ReplayEvent> GetEventsByName(string eventName)
    {
        lock (_lock)
        {
            return _events.Where(e => e.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    /// <summary>
    /// Gets the last N events.
    /// </summary>
    public IReadOnlyList<ReplayEvent> GetLastEvents(int count)
    {
        lock (_lock)
        {
            return _events.Skip(Math.Max(0, _events.Count - count)).ToList();
        }
    }

    /// <summary>
    /// Replays events through the specified dispatcher.
    /// </summary>
    /// <param name="dispatcher">Event dispatcher to replay events through</param>
    /// <param name="eventFilter">Optional filter to select which events to replay</param>
    public async Task ReplayAsync(EventDispatcher dispatcher, Func<ReplayEvent, bool>? eventFilter = null)
    {
        ReplayEvent[] eventsToReplay;
        
        lock (_lock)
        {
            eventsToReplay = eventFilter != null 
                ? _events.Where(eventFilter).ToArray()
                : _events.ToArray();
        }

        _logger?.LogInformation("Replaying {Count} events", eventsToReplay.Length);

        foreach (var replayEvent in eventsToReplay)
        {
            try
            {
                if (replayEvent.EventData != null)
                {
                    await dispatcher.DispatchAsync(replayEvent.EventName, replayEvent.EventData, replayEvent.RawJson).ConfigureAwait(false);
                }
                _logger?.LogDebug("Replayed event {EventName} from {Timestamp}", 
                    replayEvent.EventName, replayEvent.Timestamp);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to replay event {EventName}", replayEvent.EventName);
            }
        }

        _logger?.LogInformation("Replay completed");
    }

    /// <summary>
    /// Clears all events from the buffer.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
        _logger?.LogDebug("Replay buffer cleared");
    }

    /// <summary>
    /// Represents a stored event for replay.
    /// </summary>
    public class ReplayEvent
    {
        /// <summary>
        /// When the event was received.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Discord gateway event name (e.g., "MESSAGE_CREATE").
        /// </summary>
        public string EventName { get; set; } = string.Empty;

        /// <summary>
        /// The deserialized event data.
        /// </summary>
        public GatewayEvent? EventData { get; set; }

        /// <summary>
        /// Optional raw JSON payload.
        /// </summary>
        public string? RawJson { get; set; }

        /// <summary>
        /// Discord gateway sequence number.
        /// </summary>
        public int? SequenceNumber { get; set; }
    }
}

/// <summary>
/// Extension methods for EventDispatcher to integrate with replay buffer.
/// </summary>
public static class EventReplayExtensions
{
    /// <summary>
    /// Creates a replay buffer that automatically records all dispatched events.
    /// </summary>
    public static EventReplayBuffer WithReplayBuffer(this EventDispatcher dispatcher, int capacity, ILogger? logger = null)
    {
        var buffer = new EventReplayBuffer(capacity, logger);
        
        // Use middleware to capture all events
        dispatcher.Use(async (eventName, eventData) =>
        {
            if (eventData is GatewayEvent gatewayEvent)
            {
                // Note: We don't have access to raw JSON here, but the event data is sufficient
                buffer.RecordEvent(eventName, gatewayEvent, gatewayEvent.RawJson);
            }
            await Task.CompletedTask;
        });

        return buffer;
    }
}
