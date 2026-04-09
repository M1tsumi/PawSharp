using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PawSharp.Core.Enums;
using PawSharp.Core.Events;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Extension methods for <see cref="EventDispatcher"/> to support event interest validation.
/// </summary>
public static class EventDispatcherIntentExtensions
{
    private static IEnumerable<KeyValuePair<string, List<Delegate>>> GetHandlers(EventDispatcher dispatcher)
    {
        var handlersField = dispatcher.GetType().GetField(
            "_eventHandlers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (handlersField?.GetValue(dispatcher) is ConcurrentDictionary<string, List<Delegate>> concurrentHandlers)
        {
            return concurrentHandlers.ToArray();
        }

        if (handlersField?.GetValue(dispatcher) is Dictionary<string, List<Delegate>> dictionaryHandlers)
        {
            return dictionaryHandlers.ToArray();
        }

        return Array.Empty<KeyValuePair<string, List<Delegate>>>();
    }

    private static void WriteWarning(ILogger? logger, string message)
    {
        if (logger != null)
        {
            logger.LogWarning(message);
        }
        else
        {
            Console.WriteLine($"[WARN] {message}");
        }
    }

    /// <summary>
    /// Validates that all registered handlers have their required intents enabled.
    /// Logs warnings for any mismatches.
    /// </summary>
    /// <remarks>
    /// This should be called after the client connects to Discord with its configured intents.
    /// It examines all currently registered event handlers and cross-references their
    /// EventInterestAttribute declarations against the enabled intents.
    /// 
    /// Warnings are logged at WARN level because missing intents are a configuration bug
    /// that will cause handlers to not receive expected events.
    /// </remarks>
    /// <param name="dispatcher">The event dispatcher to validate</param>
    /// <param name="enabledIntents">The intents that are currently enabled</param>
    /// <param name="logger">Optional logger for warnings. If null, uses console.WriteLine</param>
    public static void ValidateHandlerIntents(
        this EventDispatcher dispatcher,
        GatewayIntents enabledIntents,
        ILogger? logger = null)
    {
        if (dispatcher == null)
        {
            throw new ArgumentNullException(nameof(dispatcher));
        }

        var warnings = new List<(string EventType, EventInterestMetadata Metadata, GatewayIntents Missing)>();

        foreach (var (eventType, handlerList) in GetHandlers(dispatcher))
        {
            foreach (var handler in handlerList)
            {
                var metadata = EventInterestMetadata.FromHandler(handler);
                if (metadata != null && !metadata.ValidateIntents(enabledIntents, out var missingIntents))
                {
                    warnings.Add((eventType, metadata, missingIntents));
                }
            }
        }

        // Log all warnings
        if (warnings.Count > 0)
        {
            var summary = $"Found {warnings.Count} handler(s) with missing required intent(s):";
            WriteWarning(logger, summary);

            var groupedByMissing = warnings
                .GroupBy(w => w.Missing)
                .OrderByDescending(g => g.Count());

            foreach (var group in groupedByMissing)
            {
                var intentDesc = group.First().Metadata.GetMissingIntentsDescription(group.Key);
                var handlerCount = group.Count();
                var handlerSummary = $"  Missing intent(s): {intentDesc} ({handlerCount} handler{(handlerCount > 1 ? "s" : "")})";

                WriteWarning(logger, handlerSummary);

                // Show which event types and handlers are affected
                foreach (var (eventType, _, _) in group.Take(3))
                {
                    var eventSummary = $"    - Event: {eventType}";
                    WriteWarning(logger, eventSummary);
                }

                if (group.Count() > 3)
                {
                    var moreCount = group.Count() - 3;
                    var moreSummary = $"    ... and {moreCount} more";
                    WriteWarning(logger, moreSummary);
                }
            }

            if (logger == null)
            {
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Gets a summary of all event interests registered in the dispatcher.
    /// Useful for debugging and logging the dispatcher's current configuration.
    /// </summary>
    /// <param name="dispatcher">The event dispatcher to examine</param>
    /// <returns>A list of (EventType, RequiredIntents) tuples</returns>
    public static List<(string EventType, GatewayIntents RequiredIntents)> GetRegisteredIntents(
        this EventDispatcher dispatcher)
    {
        if (dispatcher == null)
        {
            throw new ArgumentNullException(nameof(dispatcher));
        }

        var result = new List<(string, GatewayIntents)>();

        foreach (var (eventType, handlerList) in GetHandlers(dispatcher))
        {
            var intents = (GatewayIntents)0;
            foreach (var handler in handlerList)
            {
                var metadata = EventInterestMetadata.FromHandler(handler);
                if (metadata != null)
                {
                    intents |= metadata.RequiredIntents;
                }
            }

            if (intents != (GatewayIntents)0)
            {
                result.Add((eventType, intents));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a recommended set of intents based on all currently registered handlers.
    /// This can be used to suggest what intents the client should enable.
    /// </summary>
    /// <param name="dispatcher">The event dispatcher to examine</param>
    /// <returns>A bitmask of recommended intents</returns>
    public static GatewayIntents GetRecommendedIntents(this EventDispatcher dispatcher)
    {
        if (dispatcher == null)
        {
            throw new ArgumentNullException(nameof(dispatcher));
        }

        GatewayIntents recommended = (GatewayIntents)0;

        foreach (var handlerList in GetHandlers(dispatcher).Select(x => x.Value))
        {
            foreach (var handler in handlerList)
            {
                var metadata = EventInterestMetadata.FromHandler(handler);
                if (metadata != null)
                {
                    recommended |= metadata.RequiredIntents;
                }
            }
        }

        return recommended;
    }
}
