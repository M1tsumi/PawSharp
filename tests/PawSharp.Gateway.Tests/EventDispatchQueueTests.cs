#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class EventDispatchQueueTests
{
    [Fact]
    public void Constructor_WithMaxQueueSize_CreatesBoundedChannel()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        queue.QueueDepth.Should().Be(0);
    }

    [Fact]
    public void QueueDepth_InitiallyZero()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        queue.QueueDepth.Should().Be(0);
    }

    [Fact]
    public async Task EnqueueAsync_IncreasesQueueDepth()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        await queue.EnqueueAsync(new EventDispatchItem
        {
            EventName = "TEST_EVENT",
            EventData = null
        });

        queue.QueueDepth.Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAsync_DispatchesEvent()
    {
        var dispatcher = new EventDispatcher();
        var dispatched = false;
        dispatcher.On<ReadyEvent>("TEST_EVENT", _ => dispatched = true);

        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        await queue.EnqueueAsync(new EventDispatchItem
        {
            EventName = "TEST_EVENT",
            EventData = new ReadyEvent(),
            EventType = typeof(ReadyEvent)
        });

        await Task.Delay(200);
        dispatched.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenEmpty_DoesNotThrow()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        var act = () => queue.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_DrainsPendingEvents()
    {
        var dispatcher = new EventDispatcher();
        var dispatchCount = 0;

        dispatcher.On<ReadyEvent>("TEST_EVENT", _ => dispatchCount++);

        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);

        for (int i = 0; i < 10; i++)
        {
            await queue.EnqueueAsync(new EventDispatchItem
            {
                EventName = "TEST_EVENT",
                EventData = new ReadyEvent(),
                EventType = typeof(ReadyEvent)
            });
        }

        queue.Dispose();
        await queue.WaitForDrainAsync();

        dispatchCount.Should().Be(10);
    }

    [Fact]
    public async Task DisposeAsync_CompletesDrain()
    {
        var dispatcher = new EventDispatcher();
        var dispatchCount = 0;

        dispatcher.On<ReadyEvent>("TEST_EVENT", _ => dispatchCount++);

        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);

        for (int i = 0; i < 5; i++)
        {
            await queue.EnqueueAsync(new EventDispatchItem
            {
                EventName = "TEST_EVENT",
                EventData = new ReadyEvent(),
                EventType = typeof(ReadyEvent)
            });
        }

        await queue.DisposeAsync();
        dispatchCount.Should().Be(5);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        var act = () =>
        {
            queue.Dispose();
            queue.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        await queue.DisposeAsync();
        await queue.DisposeAsync();
    }

    [Fact]
    public async Task EnqueueAsync_AfterDispose_Throws()
    {
        var dispatcher = new EventDispatcher();
        var queue = new EventDispatchQueue(dispatcher, maxQueueSize: 100);
        queue.Dispose();

        var act = async () => await queue.EnqueueAsync(new EventDispatchItem
        {
            EventName = "TEST",
            EventData = null
        });

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }
}
