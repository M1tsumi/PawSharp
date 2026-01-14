using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Events
{
    public class EventDispatcher
    {
        private readonly Dictionary<string, List<Delegate>> _eventHandlers;
        private readonly List<Func<string, object, Task>> _middleware;
        private readonly ILogger? _logger;

        public EventDispatcher(ILogger? logger = null)
        {
            _eventHandlers = new Dictionary<string, List<Delegate>>();
            _middleware = new List<Func<string, object, Task>>();
            _logger = logger;
        }

        /// <summary>
        /// Register a typed event handler.
        /// </summary>
        public IDisposable On<TEvent>(string eventName, Action<TEvent> handler) where TEvent : GatewayEvent
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
            return new EventSubscription(this, eventName, handler);
        }

        /// <summary>
        /// Register a raw event handler for unparsed JSON.
        /// </summary>
        public IDisposable OnRaw(string eventName, Action<string> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
            return new EventSubscription(this, eventName, handler);
        }

        /// <summary>
        /// Register middleware that runs for all events.
        /// </summary>
        public void Use(Func<string, object, Task> middleware)
        {
            _middleware.Add(middleware);
        }

        /// <summary>
        /// Dispatch a typed event.
        /// </summary>
        public async Task DispatchAsync<TEvent>(string eventName, TEvent eventData, string? rawJson = null) where TEvent : GatewayEvent
        {
            if (rawJson != null)
            {
                eventData.RawJson = rawJson;
            }

            // Run middleware
            foreach (var middleware in _middleware)
            {
                await middleware(eventName, eventData);
            }

            if (_eventHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _eventHandlers[eventName])
                {
                    try
                    {
                        if (handler is Action<TEvent> typedHandler)
                        {
                            typedHandler(eventData);
                        }
                        else if (handler is Action<string> rawHandler && rawJson != null)
                        {
                            rawHandler(rawJson);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error in event handler for {eventName}");
                    }
                }
            }
        }

        /// <summary>
        /// Dispatch an event from raw JSON data.
        /// </summary>
        public async Task DispatchFromJsonAsync<TEvent>(string eventName, string json) where TEvent : GatewayEvent
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<TEvent>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (eventData != null)
                {
                    await DispatchAsync(eventName, eventData, json);
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, $"Failed to deserialize {eventName} event. This event will be skipped. Raw JSON length: {json?.Length ?? 0}");
                // Still dispatch raw event so handlers can try to process it
                await DispatchRawAsync(eventName, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to deserialize {eventName} event");
                
                // Still dispatch raw event if anyone is listening
                if (_eventHandlers.ContainsKey(eventName))
                {
                    foreach (var handler in _eventHandlers[eventName])
                    {
                        if (handler is Action<string> rawHandler)
                        {
                            rawHandler(json);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Dispatch a raw event (JSON string) to handlers.
        /// </summary>
        public async Task DispatchRawAsync(string eventName, string json)
        {
            // Run middleware with raw JSON as object
            foreach (var middleware in _middleware)
            {
                await middleware(eventName, json);
            }

            if (_eventHandlers.ContainsKey(eventName))
            {
                foreach (var handler in _eventHandlers[eventName])
                {
                    try
                    {
                        if (handler is Action<string> rawHandler)
                        {
                            rawHandler(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error in raw event handler for {eventName}");
                    }
                }
            }
        }

        /// <summary>
        /// Represents a subscription to an event that can be disposed to unsubscribe.
        /// </summary>
        private class EventSubscription : IDisposable
        {
            private readonly EventDispatcher _dispatcher;
            private readonly string _eventName;
            private readonly Delegate _handler;
            private bool _disposed;

            public EventSubscription(EventDispatcher dispatcher, string eventName, Delegate handler)
            {
                _dispatcher = dispatcher;
                _eventName = eventName;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_dispatcher._eventHandlers.TryGetValue(_eventName, out var handlers))
                {
                    handlers.Remove(_handler);
                    if (handlers.Count == 0)
                    {
                        _dispatcher._eventHandlers.Remove(_eventName);
                    }
                }
            }
        }
    }}
