#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Gateway.Events;

/// <summary>
/// Provides event filtering capabilities to reduce processing for large bots.
/// Filters can be applied based on guild, channel, user, or custom predicates.
/// </summary>
public static class EventFilteringMiddleware
{
    /// <summary>
    /// Filter context containing event metadata for filtering decisions.
    /// </summary>
    public class FilterContext
    {
        public string EventName { get; set; } = string.Empty;
        public ulong? GuildId { get; set; }
        public ulong? ChannelId { get; set; }
        public ulong? UserId { get; set; }
        public GatewayEvent? EventData { get; set; }
    }

    /// <summary>
    /// Adds middleware that filters events based on a predicate.
    /// Events that don't match the predicate are dropped.
    /// </summary>
    public static void UseFilter(this EventDispatcher dispatcher, Func<FilterContext, bool> predicate)
    {
        dispatcher.Use(async (eventName, eventData) =>
        {
            var context = CreateFilterContext(eventName, eventData);
            if (!predicate(context))
            {
                // Skip further processing by throwing a specific exception that we catch
                throw new EventFilteredException(eventName);
            }
        });
    }

    /// <summary>
    /// Adds middleware that only allows events from specific guilds.
    /// </summary>
    public static void UseGuildFilter(this EventDispatcher dispatcher, IEnumerable<ulong> allowedGuildIds)
    {
        var guildSet = new HashSet<ulong>(allowedGuildIds);
        dispatcher.UseFilter(ctx => !ctx.GuildId.HasValue || guildSet.Contains(ctx.GuildId.Value));
    }

    /// <summary>
    /// Adds middleware that only allows events from specific channels.
    /// </summary>
    public static void UseChannelFilter(this EventDispatcher dispatcher, IEnumerable<ulong> allowedChannelIds)
    {
        var channelSet = new HashSet<ulong>(allowedChannelIds);
        dispatcher.UseFilter(ctx => !ctx.ChannelId.HasValue || channelSet.Contains(ctx.ChannelId.Value));
    }

    /// <summary>
    /// Adds middleware that only allows events from specific users.
    /// </summary>
    public static void UseUserFilter(this EventDispatcher dispatcher, IEnumerable<ulong> allowedUserIds)
    {
        var userSet = new HashSet<ulong>(allowedUserIds);
        dispatcher.UseFilter(ctx => !ctx.UserId.HasValue || userSet.Contains(ctx.UserId.Value));
    }

    /// <summary>
    /// Adds middleware that excludes events from specific guilds (blacklist).
    /// </summary>
    public static void UseGuildBlacklist(this EventDispatcher dispatcher, IEnumerable<ulong> blockedGuildIds)
    {
        var guildSet = new HashSet<ulong>(blockedGuildIds);
        dispatcher.UseFilter(ctx => !ctx.GuildId.HasValue || !guildSet.Contains(ctx.GuildId.Value));
    }

    /// <summary>
    /// Adds middleware that only allows events where the guild has at least the specified member count.
    /// Useful for filtering small guilds when the bot only operates in larger communities.
    /// </summary>
    public static void UseMinimumGuildSizeFilter(this EventDispatcher dispatcher, int minMemberCount)
    {
        dispatcher.UseFilter(ctx =>
        {
            if (ctx.EventData is GuildCreateEvent guildCreate)
                return guildCreate.MemberCount >= minMemberCount;
            if (ctx.EventData is GuildUpdateEvent guildUpdate && guildUpdate.MemberCount.HasValue)
                return guildUpdate.MemberCount.Value >= minMemberCount;
            return true; // Allow events without guild size info
        });
    }

    /// <summary>
    /// Adds middleware that samples events (processes only 1/N events).
    /// Useful for reducing load when debugging high-volume bots.
    /// </summary>
    public static void UseSamplingFilter(this EventDispatcher dispatcher, int sampleRate)
    {
        if (sampleRate <= 0) throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));
        
        var counter = 0;
        dispatcher.UseFilter(_ =>
        {
            var shouldProcess = Interlocked.Increment(ref counter) % sampleRate == 0;
            return shouldProcess;
        });
    }

    private static FilterContext CreateFilterContext(string eventName, object eventData)
    {
        var context = new FilterContext { EventName = eventName };

        if (eventData is GatewayEvent gatewayEvent)
        {
            context.EventData = gatewayEvent;
            
            // Extract guild/channel/user IDs based on event type
            switch (gatewayEvent)
            {
                case GuildCreateEvent e:
                    context.GuildId = e.Id;
                    break;
                case GuildUpdateEvent e:
                    context.GuildId = e.Id;
                    break;
                case GuildDeleteEvent e:
                    context.GuildId = e.Id;
                    break;
                case ChannelCreateEvent e:
                    context.ChannelId = e.Id;
                    context.GuildId = e.GuildId;
                    break;
                case ChannelUpdateEvent e:
                    context.ChannelId = e.Id;
                    context.GuildId = e.GuildId;
                    break;
                case ChannelDeleteEvent e:
                    context.ChannelId = e.Id;
                    context.GuildId = e.GuildId;
                    break;
                case MessageCreateEvent e:
                    context.ChannelId = e.ChannelId;
                    context.GuildId = e.GuildId;
                    context.UserId = e.Author?.Id;
                    break;
                case MessageUpdateEvent e:
                    context.ChannelId = e.ChannelId;
                    context.GuildId = e.GuildId;
                    break;
                case MessageDeleteEvent e:
                    context.ChannelId = e.ChannelId;
                    context.GuildId = e.GuildId;
                    break;
                case GuildMemberAddEvent e:
                    context.GuildId = e.GuildId;
                    context.UserId = e.User?.Id;
                    break;
                case GuildMemberUpdateEvent e:
                    context.GuildId = e.GuildId;
                    context.UserId = e.User?.Id;
                    break;
                case GuildMemberRemoveEvent e:
                    context.GuildId = e.GuildId;
                    context.UserId = e.User?.Id;
                    break;
                case PresenceUpdateEvent e:
                    context.GuildId = e.GuildId;
                    context.UserId = e.User?.Id;
                    break;
                case InteractionCreateEvent e:
                    context.GuildId = e.GuildId;
                    context.ChannelId = e.ChannelId;
                    context.UserId = e.User?.Id ?? e.Member?.User?.Id;
                    break;
                case TypingStartEvent e:
                    context.ChannelId = e.ChannelId;
                    context.GuildId = e.GuildId;
                    context.UserId = e.UserId;
                    break;
            }
        }

        return context;
    }
}

/// <summary>
/// Exception thrown when an event is filtered out and should not be processed further.
/// </summary>
public class EventFilteredException : Exception
{
    public string EventName { get; }

    public EventFilteredException(string eventName) 
        : base($"Event '{eventName}' was filtered out")
    {
        EventName = eventName;
    }
}
