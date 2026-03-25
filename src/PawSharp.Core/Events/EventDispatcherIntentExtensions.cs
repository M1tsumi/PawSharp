using System;
using System.Collections.Generic;
using System.Linq;
using PawSharp.Core.Enums;
using PawSharp.Core.Logging;

namespace PawSharp.Core.Events;

/// <summary>
/// Extension methods for <see cref="EventDispatcher"/> to support event interest validation.
/// </summary>
public static class EventDispatcherIntentExtensions
{
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
        IDiscordLogger? logger = null)
    {
        if (dispatcher == null)
            throw new ArgumentNullException(nameof(dispatcher));

        var warnings = new List<(string EventType, EventInterestMetadata Metadata, GatewayIntents Missing)>();

        // Get all registered handlers by examining internal state through reflection
        // (EventDispatcher stores them in _handlers ConcurrentDictionary)
        var handlersField = dispatcher.GetType().GetField(
            "_handlers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (handlersField?.GetValue(dispatcher) is Dictionary<string, List<Delegate>> handlers)
        {
            foreach (var (eventType, handlerList) in handlers)
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
        }

        // Log all warnings
        if (warnings.Count > 0)
        {
            var summary = $"Found {warnings.Count} handler(s) with missing required intent(s):";
            logger?.Warn(summary);
            Console.WriteLine($"[WARN] {summary}");

            var groupedByMissing = warnings
                .GroupBy(w => w.Missing)
                .OrderByDescending(g => g.Count());

            foreach (var group in groupedByMissing)
            {
                var intentDesc = metadata.GetMissingIntentsDescription(group.Key);
                var handlerCount = group.Count();
                var handlerSummary = $"  Missing intent(s): {intentDesc} ({handlerCount} handler{(handlerCount > 1 ? "s" : "")})";
                
                logger?.Warn(handlerSummary);
                Console.WriteLine($"  [WARN] {handlerSummary}");

                // Show which event types and handlers are affected
                foreach (var (eventType, _, _) in group.Take(3))
                {
                    var eventSummary = $"    - Event: {eventType}";
                    logger?.Warn(eventSummary);
                    Console.WriteLine($"    {eventSummary}");
                }

                if (group.Count() > 3)
                {
                    var moreCount = group.Count() - 3;
                    var moreSummary = $"    ... and {moreCount} more";
                    logger?.Warn(moreSummary);
                    Console.WriteLine($"    {moreSummary}");
                }
            }

            Console.WriteLine();
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
            throw new ArgumentNullException(nameof(dispatcher));

        var result = new List<(string, GatewayIntents)>();

        var handlersField = dispatcher.GetType().GetField(
            "_handlers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (handlersField?.GetValue(dispatcher) is Dictionary<string, List<Delegate>> handlers)
        {
            foreach (var (eventType, handlerList) in handlers)
            {
                var intents = GatewayIntents.None;
                foreach (var handler in handlerList)
                {
                    var metadata = EventInterestMetadata.FromHandler(handler);
                    if (metadata != null)
                        intents |= metadata.RequiredIntents;
                }

                if (intents != GatewayIntents.None)
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
            throw new ArgumentNullException(nameof(dispatcher));

        GatewayIntents recommended = GatewayIntents.None;

        var handlersField = dispatcher.GetType().GetField(
            "_handlers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (handlersField?.GetValue(dispatcher) is Dictionary<string, List<Delegate>> handlers)
        {
            foreach (var handlerList in handlers.Values)
            {
                foreach (var handler in handlerList)
                {
                    var metadata = EventInterestMetadata.FromHandler(handler);
                    if (metadata != null)
                        recommended |= metadata.RequiredIntents;
                }
            }
        }

        return recommended;
    }
}
