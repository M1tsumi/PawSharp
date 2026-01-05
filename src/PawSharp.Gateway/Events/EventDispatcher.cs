using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PawSharp.Gateway.Events
{
    public class EventDispatcher
    {
        private readonly Dictionary<string, List<Delegate>> _eventHandlers;
        private readonly ILogger? _logger;

        public EventDispatcher(ILogger? logger = null)
        {
            _eventHandlers = new Dictionary<string, List<Delegate>>();
            _logger = logger;
        }

        /// <summary>
        /// Register a typed event handler.
        /// </summary>
        public void On<TEvent>(string eventName, Action<TEvent> handler) where TEvent : GatewayEvent
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// Register a raw event handler for unparsed JSON.
        /// </summary>
        public void OnRaw(string eventName, Action<string> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = new List<Delegate>();
            }
            _eventHandlers[eventName].Add(handler);
        }

        /// <summary>
        /// Dispatch a typed event.
        /// </summary>
        public void Dispatch<TEvent>(string eventName, TEvent eventData, string? rawJson = null) where TEvent : GatewayEvent
        {
            if (rawJson != null)
            {
                eventData.RawJson = rawJson;
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
        public void DispatchFromJson<TEvent>(string eventName, string json) where TEvent : GatewayEvent
        {
            try
            {
                var eventData = JsonSerializer.Deserialize<TEvent>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (eventData != null)
                {
                    Dispatch(eventName, eventData, json);
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, $"Failed to deserialize {eventName} event. This event will be skipped. Raw JSON length: {json?.Length ?? 0}");
                // Still dispatch raw event so handlers can try to process it
                if (_eventHandlers.ContainsKey(eventName))
                {
                    foreach (var handler in _eventHandlers[eventName])
                    {
                        if (handler is Action<string> rawHandler)
                        {
                            try
                            {
                                rawHandler(json);
                            }
                            catch (Exception handlerEx)
                            {
                                _logger?.LogError(handlerEx, $"Error in raw event handler for {eventName}");
                            }
                        }
                    }
                }
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
    }
}