#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Core.Entities;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class EventFilteringMiddlewareTests
{
    [Fact]
    public async Task UseGuildFilter_AllowsMatchingGuild()
    {
        var dispatcher = new EventDispatcher();
        var allowedGuilds = new List<ulong> { 12345UL, 67890UL };
        dispatcher.UseGuildFilter(allowedGuilds);

        var received = false;
        dispatcher.On<GuildCreateEvent>("GUILD_CREATE", _ => received = true);

        var evt = new GuildCreateEvent { Id = 12345UL };
        await dispatcher.DispatchAsync("GUILD_CREATE", evt);

        received.Should().BeTrue();
    }

    [Fact]
    public async Task UseGuildFilter_FiltersNonMatchingGuild()
    {
        var dispatcher = new EventDispatcher();
        var allowedGuilds = new List<ulong> { 12345UL };
        dispatcher.UseGuildFilter(allowedGuilds);

        var received = false;
        dispatcher.On<GuildCreateEvent>("GUILD_CREATE", _ => received = true);

        var evt = new GuildCreateEvent { Id = 99999UL };
        await dispatcher.DispatchAsync("GUILD_CREATE", evt);

        received.Should().BeFalse();
    }

    [Fact]
    public async Task UseGuildFilter_AllowsEventsWithoutGuildId()
    {
        var dispatcher = new EventDispatcher();
        var allowedGuilds = new List<ulong> { 12345UL };
        dispatcher.UseGuildFilter(allowedGuilds);

        var received = false;
        dispatcher.On<UserUpdateEvent>("USER_UPDATE", _ => received = true);

        var evt = new UserUpdateEvent { Id = 1UL };
        await dispatcher.DispatchAsync("USER_UPDATE", evt);

        received.Should().BeTrue();
    }

    [Fact]
    public async Task UseChannelFilter_AllowsMatchingChannel()
    {
        var dispatcher = new EventDispatcher();
        var allowedChannels = new List<ulong> { 111UL, 222UL };
        dispatcher.UseChannelFilter(allowedChannels);

        var received = false;
        dispatcher.On<ChannelCreateEvent>("CHANNEL_CREATE", _ => received = true);

        var evt = new ChannelCreateEvent { Id = 111UL };
        await dispatcher.DispatchAsync("CHANNEL_CREATE", evt);

        received.Should().BeTrue();
    }

    [Fact]
    public async Task UseChannelFilter_FiltersNonMatchingChannel()
    {
        var dispatcher = new EventDispatcher();
        var allowedChannels = new List<ulong> { 111UL };
        dispatcher.UseChannelFilter(allowedChannels);

        var received = false;
        dispatcher.On<ChannelCreateEvent>("CHANNEL_CREATE", _ => received = true);

        var evt = new ChannelCreateEvent { Id = 333UL };
        await dispatcher.DispatchAsync("CHANNEL_CREATE", evt);

        received.Should().BeFalse();
    }

    [Fact]
    public async Task UseUserFilter_AllowsMatchingUser()
    {
        var dispatcher = new EventDispatcher();
        var allowedUsers = new List<ulong> { 555UL };
        dispatcher.UseUserFilter(allowedUsers);

        var received = false;
        dispatcher.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", _ => received = true);

        var evt = new GuildMemberAddEvent
        {
            GuildId = 1UL,
            User = new User { Id = 555UL }
        };
        await dispatcher.DispatchAsync("GUILD_MEMBER_ADD", evt);

        received.Should().BeTrue();
    }

    [Fact]
    public async Task UseUserFilter_FiltersNonMatchingUser()
    {
        var dispatcher = new EventDispatcher();
        var allowedUsers = new List<ulong> { 555UL };
        dispatcher.UseUserFilter(allowedUsers);

        var received = false;
        dispatcher.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", _ => received = true);

        var evt = new GuildMemberAddEvent
        {
            GuildId = 1UL,
            User = new User { Id = 999UL }
        };
        await dispatcher.DispatchAsync("GUILD_MEMBER_ADD", evt);

        received.Should().BeFalse();
    }

    [Fact]
    public async Task UseGuildBlacklist_FiltersBlockedGuild()
    {
        var dispatcher = new EventDispatcher();
        var blockedGuilds = new List<ulong> { 999UL };
        dispatcher.UseGuildBlacklist(blockedGuilds);

        var received = false;
        dispatcher.On<GuildCreateEvent>("GUILD_CREATE", _ => received = true);

        var evt = new GuildCreateEvent { Id = 999UL };
        await dispatcher.DispatchAsync("GUILD_CREATE", evt);

        received.Should().BeFalse();
    }

    [Fact]
    public async Task UseSamplingFilter_ProcessesSomeEvents()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.UseSamplingFilter(2);

        var count = 0;
        dispatcher.On<ReadyEvent>("READY", _ => count++);

        for (int i = 0; i < 10; i++)
        {
            await dispatcher.DispatchAsync("READY", new ReadyEvent());
        }

        count.Should().Be(5);
    }

    [Fact]
    public void UseSamplingFilter_WithZero_Throws()
    {
        var dispatcher = new EventDispatcher();
        var act = () => dispatcher.UseSamplingFilter(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task MultipleFilters_AllMustPass()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.UseGuildFilter(new List<ulong> { 100UL });
        dispatcher.UseChannelFilter(new List<ulong> { 200UL });

        var received = false;
        dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", _ => received = true);

        var evt = new MessageCreateEvent
        {
            GuildId = 100UL,
            ChannelId = 200UL
        };
        await dispatcher.DispatchAsync("MESSAGE_CREATE", evt);

        received.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleFilters_OneFails_BlocksEvent()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.UseGuildFilter(new List<ulong> { 100UL });
        dispatcher.UseChannelFilter(new List<ulong> { 999UL });

        var received = false;
        dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", _ => received = true);

        var evt = new MessageCreateEvent
        {
            GuildId = 100UL,
            ChannelId = 200UL
        };
        await dispatcher.DispatchAsync("MESSAGE_CREATE", evt);

        received.Should().BeFalse();
    }
}
