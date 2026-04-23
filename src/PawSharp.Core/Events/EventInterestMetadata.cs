using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Events;

/// <summary>
/// Metadata extracted from <see cref="EventInterestAttribute"/> describing
/// which events a handler is interested in and which intents are required.
/// </summary>
public sealed class EventInterestMetadata
{
    /// <summary>
    /// The handler method or type this metadata describes.
    /// </summary>
    public object Target { get; }

    /// <summary>
    /// Set of event type names this handler is interested in.
    /// </summary>
    public IReadOnlySet<string> EventTypes { get; }

    /// <summary>
    /// Bitmask of required intents for these events.
    /// </summary>
    public GatewayIntents RequiredIntents { get; }

    /// <summary>
    /// True if this metadata was explicitly declared, false if inferred or default.
    /// </summary>
    public bool IsExplicit { get; }

    public EventInterestMetadata(
        object target,
        IReadOnlySet<string> eventTypes,
        GatewayIntents requiredIntents,
        bool isExplicit = true)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        EventTypes = eventTypes ?? throw new ArgumentNullException(nameof(eventTypes));
        RequiredIntents = requiredIntents;
        IsExplicit = isExplicit;
    }

    /// <summary>
    /// Extracts event interest metadata from a method, class, or delegate.
    /// Returns null if no interest is declared.
    /// </summary>
    /// <remarks>
    /// Priority: Method attribute > Class attribute > None
    /// </remarks>
    public static EventInterestMetadata? FromHandler(Delegate handler)
    {
        if (handler == null) return null;

        var method = handler.Method;
        
        // Check method-level attribute first
        var methodAttr = method.GetCustomAttribute<EventInterestAttribute>();
        if (methodAttr != null)
            return new EventInterestMetadata(
                handler,
                methodAttr.EventTypes,
                methodAttr.RequiredIntents,
                isExplicit: true);

        // Check class-level attribute
        var classAttr = method.DeclaringType?.GetCustomAttribute<EventInterestAttribute>();
        if (classAttr != null)
            return new EventInterestMetadata(
                handler,
                classAttr.EventTypes,
                classAttr.RequiredIntents,
                isExplicit: true);

        return null;
    }

    /// <summary>
    /// Extracts event interest metadata from a generic event handler type.
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <returns>Metadata if attribute is present, null otherwise</returns>
    public static EventInterestMetadata? FromEventType<TEvent>() where TEvent : class
    {
        return FromEventType(typeof(TEvent));
    }

    /// <summary>
    /// Extracts event interest metadata from an event type.
    /// </summary>
    /// <param name="eventType">The event type to examine</param>
    /// <returns>Metadata if attribute is present, null otherwise</returns>
    public static EventInterestMetadata? FromEventType(Type eventType)
    {
        if (eventType == null) return null;

        var attr = eventType.GetCustomAttribute<EventInterestAttribute>();
        if (attr != null)
            return new EventInterestMetadata(
                eventType,
                attr.EventTypes,
                attr.RequiredIntents,
                isExplicit: true);

        return null;
    }

    /// <summary>
    /// Validates that the provided intents cover the required intents for this handler.
    /// </summary>
    /// <param name="enabledIntents">The currently enabled intents</param>
    /// <param name="missingIntents">Out parameter: intents that are required but missing</param>
    /// <returns>True if all required intents are enabled, false otherwise</returns>
    public bool ValidateIntents(GatewayIntents enabledIntents, out GatewayIntents missingIntents)
    {
        missingIntents = RequiredIntents & ~enabledIntents;
        return missingIntents == (GatewayIntents)0;
    }

    /// <summary>
    /// Gets a human-readable description of which intents are missing.
    /// </summary>
    public string GetMissingIntentsDescription(GatewayIntents missingIntents)
    {
        if (missingIntents == (GatewayIntents)0)
            return "All required intents are enabled";

        var intentNames = new List<string>();

        // Check each intent flag - use GetValues<TEnum> for AOT compatibility
        foreach (GatewayIntents intent in Enum.GetValues<GatewayIntents>())
        {
            if (intent != (GatewayIntents)0 && (missingIntents & intent) != 0)
                intentNames.Add(intent.ToString());
        }

        return intentNames.Count == 0
            ? "Unknown intents"
            : string.Join(", ", intentNames);
    }

    /// <summary>
    /// Returns true if this handler is interested in the specified event type.
    /// </summary>
    public bool IsInterestedIn(string eventType)
    {
        return EventTypes.Contains(eventType);
    }

    public override string ToString()
    {
        var eventList = string.Join(", ", EventTypes.OrderBy(x => x));
        var intentList = RequiredIntents == (GatewayIntents)0
            ? "None"
            : RequiredIntents.ToString();
        return $"EventInterest {{ Events: [{eventList}], Intents: {intentList} }}";
    }
}
