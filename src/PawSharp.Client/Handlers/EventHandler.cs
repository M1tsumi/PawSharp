using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PawSharp.Client.Handlers
{
    public class EventHandler
    {
        private readonly Dictionary<string, Func<object, Task>> _eventHandlers;

        public EventHandler()
        {
            _eventHandlers = new Dictionary<string, Func<object, Task>>();
        }

        public void RegisterEventHandler(string eventName, Func<object, Task> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = handler;
            }
        }

        public async Task HandleEventAsync(string eventName, object eventData)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handler))
            {
                await handler(eventData);
            }
        }
    }
}