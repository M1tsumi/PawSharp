#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class EventReplayBufferTests
{
    [Fact]
    public void Constructor_WithCapacity_InitializesBuffer()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.Capacity.Should().Be(10);
        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_UsesMinimumOfOne()
    {
        var buffer = new EventReplayBuffer(0);
        buffer.Capacity.Should().Be(1);
    }

    [Fact]
    public void RecordEvent_AddsToBuffer()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("TEST_EVENT", new ReadyEvent());
        buffer.Count.Should().Be(1);
    }

    [Fact]
    public void RecordEvent_ExceedsCapacity_RemovesOldest()
    {
        var buffer = new EventReplayBuffer(2);

        buffer.RecordEvent("EVENT_1", new ReadyEvent());
        buffer.RecordEvent("EVENT_2", new ReadyEvent());
        buffer.RecordEvent("EVENT_3", new ReadyEvent());

        buffer.Count.Should().Be(2);
    }

    [Fact]
    public void GetAllEvents_ReturnsAllEventsInOrder()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("EVENT_1", new ReadyEvent());
        buffer.RecordEvent("EVENT_2", new ReadyEvent());

        var events = buffer.GetAllEvents();
        events.Should().HaveCount(2);
        events[0].EventName.Should().Be("EVENT_1");
        events[1].EventName.Should().Be("EVENT_2");
    }

    [Fact]
    public void GetEventsByName_ReturnsMatchingEvents()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent());
        buffer.RecordEvent("GUILD_CREATE", new GuildCreateEvent());
        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent());

        var events = buffer.GetEventsByName("MESSAGE_CREATE");
        events.Should().HaveCount(2);
    }

    [Fact]
    public void GetEventsByName_WithDifferentCase_MatchesCaseInsensitive()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent());

        var events = buffer.GetEventsByName("message_create");
        events.Should().HaveCount(1);
    }

    [Fact]
    public void GetLastEvents_ReturnsCorrectCount()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("EVENT_1", new ReadyEvent());
        buffer.RecordEvent("EVENT_2", new ReadyEvent());
        buffer.RecordEvent("EVENT_3", new ReadyEvent());

        var lastEvents = buffer.GetLastEvents(2);
        lastEvents.Should().HaveCount(2);
        lastEvents[0].EventName.Should().Be("EVENT_2");
        lastEvents[1].EventName.Should().Be("EVENT_3");
    }

    [Fact]
    public void GetLastEvents_WithMoreThanAvailable_ReturnsAll()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("EVENT_1", new ReadyEvent());

        var lastEvents = buffer.GetLastEvents(5);
        lastEvents.Should().HaveCount(1);
    }

    [Fact]
    public void GetEventsAfter_ReturnsEventsAfterTimestamp()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("EVENT_1", new ReadyEvent());
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        buffer.RecordEvent("EVENT_2", new ReadyEvent());

        var events = buffer.GetEventsAfter(before);
        events.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReplayAsync_DispatchesEvents()
    {
        var dispatcher = new EventDispatcher();
        var buffer = new EventReplayBuffer(10);

        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent { Content = "Hello" });
        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent { Content = "World" });

        var replayCount = 0;
        dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", _ => replayCount++);

        await buffer.ReplayAsync(dispatcher);
        replayCount.Should().Be(2);
    }

    [Fact]
    public async Task ReplayAsync_WithFilter_DispatchesFilteredEvents()
    {
        var dispatcher = new EventDispatcher();
        var buffer = new EventReplayBuffer(10);

        buffer.RecordEvent("MESSAGE_CREATE", new MessageCreateEvent { Content = "Hello" });
        buffer.RecordEvent("GUILD_CREATE", new GuildCreateEvent { Id = 1UL });

        var replayCount = 0;
        dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", _ => replayCount++);

        await buffer.ReplayAsync(dispatcher, e => e.EventName == "MESSAGE_CREATE");
        replayCount.Should().Be(1);
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        var buffer = new EventReplayBuffer(10);
        buffer.RecordEvent("TEST", new ReadyEvent());
        buffer.RecordEvent("TEST", new ReadyEvent());

        buffer.Clear();
        buffer.Count.Should().Be(0);
    }

    [Fact]
    public void WithReplayBuffer_CreatesBufferAndRegistersMiddleware()
    {
        var dispatcher = new EventDispatcher();
        var buffer = dispatcher.WithReplayBuffer(10);

        buffer.Should().NotBeNull();
        buffer.Capacity.Should().Be(10);
    }

    [Fact]
    public async Task WithReplayBuffer_RecordsDispatchedEvents()
    {
        var dispatcher = new EventDispatcher();
        var buffer = dispatcher.WithReplayBuffer(10);

        await dispatcher.DispatchAsync("MESSAGE_CREATE", new MessageCreateEvent { Content = "Test" });

        buffer.Count.Should().Be(1);
    }
}
