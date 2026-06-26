#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class EventDispatcherTests
{
    [Fact]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        var dispatcher = new EventDispatcher(new Mock<ILogger>().Object);
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public void On_ReturnsSubscriptionToken()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.On<ReadyEvent>("READY", _ => { });
        subscription.Should().NotBeNull();
    }

    [Fact]
    public void On_WithAction_RegistersHandler()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.On<ReadyEvent>("READY", _ => { });
        dispatcher.HandlerCount("READY").Should().Be(1);
    }

    [Fact]
    public void On_WithFunc_RegistersHandler()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.On<ReadyEvent>("READY", _ => Task.CompletedTask);
        dispatcher.HandlerCount("READY").Should().Be(1);
    }

    [Fact]
    public void OnRaw_RegistersHandler()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.OnRaw("READY", _ => { });
        dispatcher.HandlerCount("READY").Should().Be(1);
    }

    [Fact]
    public void Subscription_Dispose_UnregistersHandler()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.On<ReadyEvent>("READY", _ => { });
        subscription.Dispose();
        dispatcher.HandlerCount("READY").Should().Be(0);
    }

    [Fact]
    public void Subscription_DisposeTwice_DoesNotThrow()
    {
        var dispatcher = new EventDispatcher();
        var subscription = dispatcher.On<ReadyEvent>("READY", _ => { });
        var act = () =>
        {
            subscription.Dispose();
            subscription.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchAsync_DispatchesToRegisteredHandler()
    {
        var dispatcher = new EventDispatcher();
        ReadyEvent? received = null;

        dispatcher.On<ReadyEvent>("READY", evt => received = evt);
        await dispatcher.DispatchAsync("READY", new ReadyEvent { Version = 10 });

        received.Should().NotBeNull();
        received!.Version.Should().Be(10);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_FiresAll()
    {
        var dispatcher = new EventDispatcher();
        var count = 0;

        dispatcher.On<ReadyEvent>("READY", _ => count++);
        dispatcher.On<ReadyEvent>("READY", _ => count++);
        await dispatcher.DispatchAsync("READY", new ReadyEvent());

        count.Should().Be(2);
    }

    [Fact]
    public async Task DispatchAsync_ToDifferentEvent_NotFired()
    {
        var dispatcher = new EventDispatcher();
        var fired = false;

        dispatcher.On<ReadyEvent>("READY", _ => fired = true);
        await dispatcher.DispatchAsync("GUILD_CREATE", new GuildCreateEvent());

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchAsync_WithAsyncHandler_FiresHandler()
    {
        var dispatcher = new EventDispatcher();
        var received = false;

        dispatcher.On<ReadyEvent>("READY", async _ =>
        {
            await Task.Yield();
            received = true;
        });
        await dispatcher.DispatchAsync("READY", new ReadyEvent());

        received.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithRawJson_SetsRawJsonOnEvent()
    {
        var dispatcher = new EventDispatcher();
        string? captured = null;

        dispatcher.On<ReadyEvent>("READY", evt => captured = evt.RawJson);
        await dispatcher.DispatchAsync("READY", new ReadyEvent(), rawJson: "{\"test\":true}");

        captured.Should().Be("{\"test\":true}");
    }

    [Fact]
    public void Use_AddsMiddleware()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.Use(async (name, data) => await Task.CompletedTask);
    }

    [Fact]
    public async Task DispatchAsync_RunsMiddleware()
    {
        var dispatcher = new EventDispatcher();
        var middlewareRan = false;

        dispatcher.Use(async (name, data) =>
        {
            middlewareRan = true;
            await Task.CompletedTask;
        });

        await dispatcher.DispatchAsync("TEST", new ReadyEvent());
        middlewareRan.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_MiddlewareReceivesEventName()
    {
        var dispatcher = new EventDispatcher();
        string? capturedName = null;

        dispatcher.Use(async (name, data) =>
        {
            capturedName = name;
            await Task.CompletedTask;
        });

        await dispatcher.DispatchAsync("READY", new ReadyEvent());
        capturedName.Should().Be("READY");
    }

    [Fact]
    public async Task DispatchAsync_MiddlewareCanFilterEvent()
    {
        var dispatcher = new EventDispatcher();
        var handlerFired = false;

        dispatcher.Use(async (name, data) =>
        {
            if (name == "READY")
                throw new EventFilteredException("READY");
        });

        dispatcher.On<ReadyEvent>("READY", _ => handlerFired = true);
        await dispatcher.DispatchAsync("READY", new ReadyEvent());

        handlerFired.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchFromJsonAsync_DeserializesCorrectly()
    {
        var dispatcher = new EventDispatcher();
        MessageCreateEvent? received = null;

        dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", evt => received = evt);
        var json = """{"id":"123456789012345678","channel_id":"111111111111111111","guild_id":"222222222222222222","content":"Hello","author":{"id":"333333333333333333","username":"Test","discriminator":"0000"},"timestamp":"2026-01-01T00:00:00+00:00","tts":false,"mention_everyone":false,"mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"pinned":false,"type":0}""";
        await dispatcher.DispatchFromJsonAsync<MessageCreateEvent>("MESSAGE_CREATE", json);

        received.Should().NotBeNull();
        received!.Content.Should().Be("Hello");
        received.Author.Should().NotBeNull();
        received.Author!.Username.Should().Be("Test");
    }

    [Fact]
    public async Task DispatchFromJsonAsync_WithInvalidJson_FallsBackToRaw()
    {
        var dispatcher = new EventDispatcher();
        string? rawReceived = null;

        dispatcher.OnRaw("MESSAGE_CREATE", raw => rawReceived = raw);
        await dispatcher.DispatchFromJsonAsync<MessageCreateEvent>("MESSAGE_CREATE", "invalid json");

        rawReceived.Should().Be("invalid json");
    }

    [Fact]
    public async Task DispatchRawAsync_DispatchesRawHandler()
    {
        var dispatcher = new EventDispatcher();
        string? captured = null;

        dispatcher.OnRaw("TEST_RAW", raw => captured = raw);
        await dispatcher.DispatchRawAsync("TEST_RAW", "{\"key\":\"value\"}");

        captured.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public async Task DispatchRawAsync_RunsMiddleware()
    {
        var dispatcher = new EventDispatcher();
        var middlewareRan = false;

        dispatcher.Use(async (name, data) =>
        {
            middlewareRan = true;
            await Task.CompletedTask;
        });

        await dispatcher.DispatchRawAsync("TEST", "{}");
        middlewareRan.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var dispatcher = new EventDispatcher();
        var act = () => dispatcher.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var dispatcher = new EventDispatcher();
        var act = () =>
        {
            dispatcher.Dispose();
            dispatcher.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchAsync_WithQueueEnabled_UsesQueue()
    {
        var dispatcher = new EventDispatcher(maxQueueSize: 10);
        var received = false;

        dispatcher.On<ReadyEvent>("READY", _ => received = true);
        await dispatcher.DispatchAsync("READY", new ReadyEvent());

        await Task.Delay(200);
        received.Should().BeTrue();
    }

    [Fact]
    public void HandlerCount_ForUnknownEvent_ReturnsZero()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.HandlerCount("UNKNOWN").Should().Be(0);
    }

    [Fact]
    public void QueueDepth_WithoutQueue_ReturnsZero()
    {
        var dispatcher = new EventDispatcher();
        dispatcher.QueueDepth.Should().Be(0);
    }

    [Fact]
    public void MultipleHandlers_AllInvokedOnDispatch()
    {
        var dispatcher = new EventDispatcher();
        var count = 0;
        dispatcher.On<ReadyEvent>("READY", _ => count++);
        dispatcher.On<ReadyEvent>("READY", _ => count++);

        Task.Run(async () =>
        {
            await dispatcher.DispatchAsync("READY", new ReadyEvent());
            count.Should().Be(2);
        });
    }

    [Fact]
    public void Use_RegistersMiddlewareThatCanBeRead()
    {
        var dispatcher = new EventDispatcher();
        int middlewareCount = 0;

        dispatcher.Use(async (n, d) => { middlewareCount++; await Task.CompletedTask; });
        dispatcher.Use(async (n, d) => { middlewareCount++; await Task.CompletedTask; });

        Task.Run(async () =>
        {
            await dispatcher.DispatchAsync("TEST", new ReadyEvent());
            middlewareCount.Should().Be(2);
        });
    }
}
