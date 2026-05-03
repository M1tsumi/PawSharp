#nullable enable
using System;
using System.Threading.Tasks;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Extension methods for filtering events before they reach handlers.
/// Provides convenient methods for common filtering patterns.
/// </summary>
public static class EventFilteringExtensions
{
    /// <summary>
    /// Registers an event handler that only processes events matching a predicate.
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="dispatcher">The event dispatcher</param>
    /// <param name="eventName">The event name</param>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="handler">The event handler</param>
    /// <returns>An IDisposable to unsubscribe</returns>
    public static IDisposable OnWhere<TEvent>(
        this EventDispatcher dispatcher,
        string eventName,
        Func<TEvent, bool> predicate,
        Func<TEvent, Task> handler) where TEvent : GatewayEvent
    {
        return dispatcher.On(eventName, async evt =>
        {
            if (predicate(evt))
            {
                await handler(evt);
            }
        });
    }

    /// <summary>
    /// Registers an event handler that only processes events matching a predicate (synchronous).
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="dispatcher">The event dispatcher</param>
    /// <param name="eventName">The event name</param>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="handler">The event handler</param>
    /// <returns>An IDisposable to unsubscribe</returns>
    public static IDisposable OnWhere<TEvent>(
        this EventDispatcher dispatcher,
        string eventName,
        Func<TEvent, bool> predicate,
        Action<TEvent> handler) where TEvent : GatewayEvent
    {
        return dispatcher.On(eventName, evt =>
        {
            if (predicate(evt))
            {
                handler(evt);
            }
        });
    }

    // Message filtering extensions

    /// <summary>
    /// Registers a handler for messages from a specific guild.
    /// </summary>
    public static IDisposable OnMessageFromGuild(
        this EventDispatcher dispatcher,
        ulong guildId,
        Func<MessageCreateEvent, Task> handler)
    {
        return dispatcher.OnWhere("MESSAGE_CREATE", evt => evt.GuildId == guildId, handler);
    }

    /// <summary>
    /// Registers a handler for messages from a specific channel.
    /// </summary>
    public static IDisposable OnMessageFromChannel(
        this EventDispatcher dispatcher,
        ulong channelId,
        Func<MessageCreateEvent, Task> handler)
    {
        return dispatcher.OnWhere("MESSAGE_CREATE", evt => evt.ChannelId == channelId, handler);
    }

    /// <summary>
    /// Registers a handler for messages from a specific user.
    /// </summary>
    public static IDisposable OnMessageFromUser(
        this EventDispatcher dispatcher,
        ulong userId,
        Func<MessageCreateEvent, Task> handler)
    {
        return dispatcher.OnWhere("MESSAGE_CREATE", evt => evt.Author.Id == userId, handler);
    }

    /// <summary>
    /// Registers a handler for messages starting with a specific prefix.
    /// </summary>
    public static IDisposable OnMessageWithPrefix(
        this EventDispatcher dispatcher,
        string prefix,
        Func<MessageCreateEvent, Task> handler)
    {
        return dispatcher.OnWhere("MESSAGE_CREATE", evt => evt.Content.StartsWith(prefix), handler);
    }

    /// <summary>
    /// Registers a handler for messages containing a specific substring.
    /// </summary>
    public static IDisposable OnMessageContaining(
        this EventDispatcher dispatcher,
        string substring,
        Func<MessageCreateEvent, Task> handler)
    {
        return dispatcher.OnWhere("MESSAGE_CREATE", evt => evt.Content.Contains(substring), handler);
    }

    // Guild filtering extensions

    /// <summary>
    /// Registers a handler for events from a specific guild.
    /// </summary>
    public static IDisposable OnGuildEvent<TEvent>(
        this EventDispatcher dispatcher,
        string eventName,
        ulong guildId,
        Func<TEvent, Task> handler) where TEvent : GatewayEvent
    {
        return dispatcher.OnWhere(eventName, evt =>
        {
            if (evt is IGuildEvent guildEvent)
            {
                return guildEvent.GuildId == guildId;
            }
            return false;
        }, handler);
    }

    // User filtering extensions

    /// <summary>
    /// Registers a handler for events from a specific user.
    /// </summary>
    public static IDisposable OnUserEvent<TEvent>(
        this EventDispatcher dispatcher,
        string eventName,
        ulong userId,
        Func<TEvent, Task> handler) where TEvent : GatewayEvent
    {
        return dispatcher.OnWhere(eventName, evt =>
        {
            if (evt is IUserEvent userEvent)
            {
                return userEvent.UserId == userId;
            }
            return false;
        }, handler);
    }
}

/// <summary>
/// Interface for events that have a GuildId property.
/// </summary>
public interface IGuildEvent
{
    ulong GuildId { get; }
}

/// <summary>
/// Interface for events that have a UserId property.
/// </summary>
public interface IUserEvent
{
    ulong UserId { get; }
}
