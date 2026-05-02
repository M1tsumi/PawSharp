#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Metrics;
using PawSharp.Core.Serialization;
using PawSharp.Gateway.Serialization;

namespace PawSharp.Gateway.Events
{
    /// <summary>
    /// Thread-safe event dispatcher for Discord gateway events.
    /// Supports both synchronous (<see cref="Action{T}"/>) and asynchronous
    /// (<see cref="Func{T, Task}"/>) event handlers.
    /// </summary>
    public class EventDispatcher
    {
        // ConcurrentDictionary<eventName, snapshot copy on write list of delegates>
        private readonly ConcurrentDictionary<string, List<Delegate>> _eventHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _handlersLock = new();
        private readonly List<Func<string, object, Task>> _middleware = new();
        private readonly object _middlewareLock = new();
        private readonly ILogger? _logger;
        private readonly IPerformanceMetrics? _metrics;
        private readonly EventDispatchQueue? _dispatchQueue;
        private readonly bool _useQueue;
        private readonly int _handlerTimeoutMs;

        // Shared options instance – created once, reused for every deserialization call.
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                // Discord sends ALL snowflake IDs as strings.  Register the converters
                // globally so every ulong / ulong? field is handled automatically even
                // when no [JsonConverter] attribute is present on the property.
                new SnowflakeJsonConverter(),
                new NullableSnowflakeJsonConverter()
            },
            // Enable source generator for better AOT compatibility
            TypeInfoResolver = PawSharpGatewayJsonContext.Default
        };

        public EventDispatcher(ILogger? logger = null, int maxQueueSize = 0, bool enableParallelDispatch = false, int maxDegreeOfParallelism = 4, IPerformanceMetrics? metrics = null, int handlerTimeoutMs = 0)
        {
            _logger = logger;
            _metrics = metrics;
            _handlerTimeoutMs = handlerTimeoutMs;
            _useQueue = maxQueueSize > 0;
            
            if (_useQueue)
            {
                _dispatchQueue = new EventDispatchQueue(
                    this, 
                    maxQueueSize, 
                    enableParallelDispatch, 
                    maxDegreeOfParallelism, 
                    logger);
            }
        }

        /// <summary>
        /// Internal accessor for event handlers. Used by extension methods to avoid reflection.
        /// </summary>
        internal IEnumerable<KeyValuePair<string, List<Delegate>>> GetEventHandlers()
        {
            lock (_handlersLock)
            {
                return _eventHandlers.ToArray();
            }
        }

        // ---- Registration ----

        /// <summary>
        /// Registers a synchronous typed event handler.
        /// Dispose the returned <see cref="IDisposable"/> to unsubscribe.
        /// </summary>
        public IDisposable On<TEvent>(string eventName, Action<TEvent> handler) where TEvent : GatewayEvent
            => AddHandler(eventName, (Delegate)handler);

        /// <summary>
        /// Registers an asynchronous typed event handler.
        /// Dispose the returned <see cref="IDisposable"/> to unsubscribe.
        /// </summary>
        public IDisposable On<TEvent>(string eventName, Func<TEvent, Task> handler) where TEvent : GatewayEvent
            => AddHandler(eventName, (Delegate)handler);

        /// <summary>
        /// Registers a raw (JSON string) event handler.
        /// </summary>
        public IDisposable OnRaw(string eventName, Action<string> handler)
            => AddHandler(eventName, (Delegate)handler);

        private IDisposable AddHandler(string eventName, Delegate handler)
        {
            lock (_handlersLock)
            {
                var list = _eventHandlers.GetOrAdd(eventName, _ => new List<Delegate>());
                // Replace list with a new copy (copy-on-write pattern for safe iteration)
                var newList = new List<Delegate>(list) { handler };
                _eventHandlers[eventName] = newList;
            }
            return new EventSubscription(this, eventName, handler);
        }

        internal void RemoveHandler(string eventName, Delegate handler)
        {
            lock (_handlersLock)
            {
                if (!_eventHandlers.TryGetValue(eventName, out var list)) return;
                var newList = new List<Delegate>(list);
                newList.Remove(handler);
                _eventHandlers[eventName] = newList;
            }
        }

        /// <summary>
        /// Registers middleware that runs before every event dispatch.
        /// </summary>
        public void Use(Func<string, object, Task> middleware)
        {
            lock (_middlewareLock)
            {
                _middleware.Add(middleware);
            }
        }

        // ---- Dispatch ----

        /// <summary>
        /// Dispatches a typed event to all registered handlers.
        /// </summary>
        public async Task DispatchAsync<TEvent>(string eventName, TEvent eventData, string? rawJson = null) where TEvent : GatewayEvent
        {
            if (rawJson != null) eventData.RawJson = rawJson;

            // If queue is enabled, enqueue for background processing
            if (_useQueue && _dispatchQueue != null)
            {
                await _dispatchQueue.EnqueueAsync(new EventDispatchItem
                {
                    EventName = eventName,
                    EventData = eventData,
                    RawJson = rawJson,
                    EventType = typeof(TEvent)
                });
                return;
            }

            // Direct dispatch (legacy behavior)
            await DispatchDirectAsync(eventName, eventData, rawJson);
        }

        /// <summary>
        /// Direct dispatch without queueing (used when queue is disabled or for internal calls).
        /// </summary>
        private async Task DispatchDirectAsync<TEvent>(string eventName, TEvent eventData, string? rawJson) where TEvent : GatewayEvent
        {
            var sw = Stopwatch.StartNew();
            
            // Run middleware
            List<Func<string, object, Task>> middlewareCopy;
            lock (_middlewareLock) middlewareCopy = new List<Func<string, object, Task>>(_middleware);
            foreach (var mw in middlewareCopy)
            {
                try 
                { 
                    await mw(eventName, eventData); 
                }
                catch (EventFilteredException)
                {
                    // Event was filtered out - stop processing silently
                    sw.Stop();
                    return;
                }
                catch (Exception ex) 
                { 
                    _logger?.LogError(ex, "Error in event middleware for {Event}", eventName); 
                }
            }

            // Dispatch to handlers – snapshot copy ensures iteration is safe even if handlers mutate list
            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                sw.Stop();
                _metrics?.RecordEventDispatch(eventName, sw.ElapsedMilliseconds);
                return;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Func<TEvent, Task> asyncHandler)
                    {
                        if (_handlerTimeoutMs > 0)
                        {
                            using var cts = new System.Threading.CancellationTokenSource(_handlerTimeoutMs);
                            try
                            {
                                await asyncHandler(eventData).WaitAsync(cts.Token);
                            }
                            catch (TimeoutException)
                            {
                                _logger?.LogWarning("Handler for event {EventName} timed out after {TimeoutMs}ms", eventName, _handlerTimeoutMs);
                            }
                        }
                        else
                        {
                            await asyncHandler(eventData);
                        }
                    }
                    else if (handler is Action<TEvent> syncHandler)
                        syncHandler(eventData);
                    else if (handler is Action<string> rawHandler && rawJson != null)
                        rawHandler(rawJson);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in event handler for {Event}", eventName);
                }
            }
            
            sw.Stop();
            _metrics?.RecordEventDispatch(eventName, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Deserializes the raw JSON payload and dispatches the resulting event.
        /// Falls back to raw-JSON handlers on deserialization failure.
        /// Uses source-generated serialization for AOT compatibility.
        /// </summary>
        public async Task DispatchFromJsonAsync<TEvent>(string eventName, string json) where TEvent : GatewayEvent
        {
            try
            {
                // Use source-generated deserialization with JsonTypeInfo
                var typeInfo = _jsonOptions.TypeInfoResolver?.GetTypeInfo(typeof(TEvent), _jsonOptions);
                var eventData = typeInfo != null
                    ? JsonSerializer.Deserialize(json, typeInfo) as TEvent
                    : JsonSerializer.Deserialize<TEvent>(json, _jsonOptions);
                
                if (eventData != null)
                    await DispatchAsync(eventName, eventData, json);
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex,
                    "Failed to deserialize {Event} event (JSON length: {Len}). Falling back to raw dispatch.",
                    eventName, json?.Length ?? 0);
                await DispatchRawAsync(eventName, json!);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error dispatching {Event} event", eventName);
                await DispatchRawAsync(eventName, json!);
            }
        }

        /// <summary>
        /// Dispatches a raw JSON string to handlers registered via <see cref="OnRaw"/>.
        /// </summary>
        public async Task DispatchRawAsync(string eventName, string json)
        {
            List<Func<string, object, Task>> middlewareCopy;
            lock (_middlewareLock) middlewareCopy = new List<Func<string, object, Task>>(_middleware);
            foreach (var mw in middlewareCopy)
            {
                try { await mw(eventName, json); }
                catch (Exception ex) { _logger?.LogError(ex, "Error in middleware for raw {Event}", eventName); }
            }

            if (!_eventHandlers.TryGetValue(eventName, out var handlers)) return;
            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<string> rawHandler) rawHandler(json);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in raw event handler for {Event}", eventName);
                }
            }
        }

        /// <summary>
        /// Dispatches a typed event without requiring generic type parameter at call site.
        /// Used internally by EventDispatchQueue for AOT-compatible event dispatching.
        /// </summary>
        internal async Task DispatchTypedAsync(string eventName, GatewayEvent eventData, string? rawJson = null)
        {
            var sw = Stopwatch.StartNew();
            
            if (rawJson != null) eventData.RawJson = rawJson;

            // Run middleware
            List<Func<string, object, Task>> middlewareCopy;
            lock (_middlewareLock) middlewareCopy = new List<Func<string, object, Task>>(_middleware);
            foreach (var mw in middlewareCopy)
            {
                try 
                { 
                    await mw(eventName, eventData); 
                }
                catch (EventFilteredException)
                {
                    // Event was filtered out - stop processing silently
                    sw.Stop();
                    return;
                }
                catch (Exception ex) 
                { 
                    _logger?.LogError(ex, "Error in event middleware for {Event}", eventName); 
                }
            }

            // Dispatch to handlers – snapshot copy ensures iteration is safe
            if (!_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                sw.Stop();
                _metrics?.RecordEventDispatch(eventName, sw.ElapsedMilliseconds);
                return;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    // Try to invoke handler with the event data
                    // Handlers are stored as typed delegates, so we need to invoke them dynamically
                    switch (handler)
                    {
                        case Func<GatewayEvent, Task> asyncHandler:
                            await asyncHandler(eventData);
                            break;
                        case Action<GatewayEvent> syncHandler:
                            syncHandler(eventData);
                            break;
                        case Action<string> rawHandler when rawJson != null:
                            rawHandler(rawJson);
                            break;
                        default:
                            // Try to invoke via dynamic dispatch for typed handlers
                            // This handles cases where handler is Func<SpecificEventType, Task>
                            var result = handler.DynamicInvoke(eventData);
                            if (result is Task task)
                                await task;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in event handler for {Event}", eventName);
                }
            }
            
            sw.Stop();
            _metrics?.RecordEventDispatch(eventName, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Returns the number of handlers registered for the given event name.
        /// Useful for diagnostics.
        /// </summary>
        public int HandlerCount(string eventName)
            => _eventHandlers.TryGetValue(eventName, out var list) ? list.Count : 0;

        /// <summary>
        /// Gets the current queue depth if backpressure is enabled.
        /// </summary>
        public int QueueDepth
        {
            get
            {
                var depth = _dispatchQueue?.QueueDepth ?? 0;
                _metrics?.RecordQueueDepth(depth);
                return depth;
            }
        }

        public void Dispose()
        {
            _dispatchQueue?.Dispose();
        }

        // ---- Subscription token ----

        private sealed class EventSubscription : IDisposable
        {
            private readonly EventDispatcher _dispatcher;
            private readonly string _eventName;
            private readonly Delegate _handler;
            private bool _disposed;

            public EventSubscription(EventDispatcher dispatcher, string eventName, Delegate handler)
            {
                _dispatcher = dispatcher;
                _eventName  = eventName;
                _handler    = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _dispatcher.RemoveHandler(_eventName, _handler);
            }
        }
    }
}
