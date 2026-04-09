#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PawSharp.API.Models;
using PawSharp.API.Interfaces;
using PawSharp.API.RateLimit;
using PawSharp.Cache.Interfaces;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Core.Events;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Client.Tests;

public class DiscordClientTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var cache = new Mock<IEntityCache>();
        var logger = new Mock<ILogger<DiscordClient>>();
        var rest = new Mock<IDiscordRestClient>();
        var gateway = new Mock<IGatewayClient>();

        Action act = () => new DiscordClient(null!, cache.Object, logger.Object, rest.Object, gateway.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        var options = new PawSharpOptions { Token = "Bot abc.def.ghi" };
        var logger = new Mock<ILogger<DiscordClient>>();
        var rest = new Mock<IDiscordRestClient>();
        var gateway = new Mock<IGatewayClient>();

        Action act = () => new DiscordClient(options, null!, logger.Object, rest.Object, gateway.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public async Task ConnectAsync_ForwardsCallToGateway()
    {
        var deps = CreateDependencies();
        var client = CreateClient(deps);

        await client.ConnectAsync();

        deps.Gateway.Verify(g => g.ConnectAsync(), Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WithWarnIntentValidation_LogsWarningAndContinues()
    {
        var options = new PawSharpOptions
        {
            Token = "Bot abc.def.ghi",
            Intents = PawSharp.Core.Enums.GatewayIntents.Guilds,
            IntentValidation = IntentValidationMode.Warn
        };

        var deps = CreateDependencies(options);
        var client = CreateClient(deps);
        deps.Dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", HandleAnnotatedMessageAsync);

        await client.ConnectAsync();

        deps.Gateway.Verify(g => g.ConnectAsync(), Times.Once);
        deps.Logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Intent validation failed", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConnectAsync_WithStrictIntentValidation_ThrowsAndDoesNotConnect()
    {
        var options = new PawSharpOptions
        {
            Token = "Bot abc.def.ghi",
            Intents = PawSharp.Core.Enums.GatewayIntents.Guilds,
            IntentValidation = IntentValidationMode.Strict
        };

        var deps = CreateDependencies(options);
        var client = CreateClient(deps);
        deps.Dispatcher.On<MessageCreateEvent>("MESSAGE_CREATE", HandleAnnotatedMessageAsync);

        await client.Invoking(c => c.ConnectAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Intent validation failed*");

        deps.Gateway.Verify(g => g.ConnectAsync(), Times.Never);
    }

    [Fact]
    public async Task DisconnectAsync_ForwardsCallToGateway()
    {
        var deps = CreateDependencies();
        var client = CreateClient(deps);

        await client.DisconnectAsync();

        deps.Gateway.Verify(g => g.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task ReadyEvent_SetsCurrentUser()
    {
        var deps = CreateDependencies();
        var client = CreateClient(deps);

        await deps.Dispatcher.DispatchAsync("READY", new ReadyEvent
        {
            User = new User { Id = 42UL, Username = "PawSharpBot" }
        });

        client.CurrentUser.Should().NotBeNull();
        client.CurrentUser!.Id.Should().Be(42UL);
        client.CurrentUser.Username.Should().Be("PawSharpBot");
    }

    [Fact]
    public async Task ReadyEvent_WithConfiguredPresence_UpdatesPresence()
    {
        var options = new PawSharpOptions
        {
            Token = "Bot abc.def.ghi",
            Presence = new PawSharpOptions.PresenceOptions
            {
                Status = "idle",
                ActivityName = "Testing",
                StreamUrl = "https://twitch.tv/example"
            }
        };

        var deps = CreateDependencies(options);
        var client = CreateClient(deps);

        await deps.Dispatcher.DispatchAsync("READY", new ReadyEvent
        {
            User = new User { Id = 7UL, Username = "PresenceBot" }
        });

        deps.Gateway.Verify(g => g.UpdatePresenceAsync("idle", "Testing", "https://twitch.tv/example"), Times.Once);
        client.CurrentUser.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsUser_WhenResponseIsSuccessful()
    {
        var deps = CreateDependencies();
        deps.Rest.Setup(r => r.GetCurrentUserAsync())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"123\",\"username\":\"pawsharp\"}")
            });

        var client = CreateClient(deps);

        var user = await client.GetCurrentUserAsync();

        user.Should().NotBeNull();
        user!.Id.Should().Be(123UL);
        user.Username.Should().Be("pawsharp");
    }

    [Fact]
    public async Task ForwardMessageAsync_StringOverload_UsesRestForwardMethod()
    {
        var deps = CreateDependencies();
        deps.Rest.Setup(r => r.ForwardMessageAsync(10UL, 20UL, 30UL, "Forwarded", true))
            .ReturnsAsync(new Message { Id = 555UL, Content = "Forwarded" });

        var client = CreateClient(deps);

        var result = await client.ForwardMessageAsync(10UL, 20UL, 30UL, "Forwarded");

        result.Should().NotBeNull();
        result!.Id.Should().Be(555UL);
        deps.Rest.Verify(r => r.ForwardMessageAsync(10UL, 20UL, 30UL, "Forwarded", true), Times.Once);
    }

    [Fact]
    public async Task ForwardMessageAsync_RequestOverload_SetsForwardMessageReference()
    {
        var deps = CreateDependencies();
        var expected = new Message { Id = 777UL };
        deps.Rest.Setup(r => r.CreateMessageAsync(
                10UL,
                It.Is<CreateMessageRequest>(req =>
                    req.MessageReference != null &&
                    req.MessageReference.Type == 1 &&
                    req.MessageReference.ChannelId == 20UL &&
                    req.MessageReference.MessageId == 30UL)))
            .ReturnsAsync(expected);

        var client = CreateClient(deps);
        var request = new CreateMessageRequest { Content = "Optional context" };

        var result = await client.ForwardMessageAsync(10UL, 20UL, 30UL, request);

        result.Should().NotBeNull();
        result!.Id.Should().Be(777UL);
        request.MessageReference.Should().NotBeNull();
        request.MessageReference!.Type.Should().Be(1);
        request.MessageReference.ChannelId.Should().Be(20UL);
        request.MessageReference.MessageId.Should().Be(30UL);
    }

    [Fact]
    public void RateLimitObserved_ForwardsEvents_WhenRestClientSupportsTelemetry()
    {
        var dispatcher = new EventDispatcher();
        var gateway = new Mock<IGatewayClient>();
        gateway.SetupGet(g => g.Events).Returns(dispatcher);
        gateway.SetupGet(g => g.CurrentState).Returns(GatewayState.Disconnected);

        var rest = new Mock<IDiscordRestClient>();
        var telemetry = rest.As<IRateLimitTelemetrySource>();

        var client = new DiscordClient(
            new PawSharpOptions { Token = "Bot abc.def.ghi" },
            new Mock<IEntityCache>().Object,
            new Mock<ILogger<DiscordClient>>().Object,
            rest.Object,
            gateway.Object);

        RateLimitTelemetryEvent? received = null;
        client.RateLimitObserved += (_, evt) => received = evt;

        var expected = new RateLimitTelemetryEvent
        {
            Kind = RateLimitTelemetryKind.RetryScheduled,
            Route = "GET users/@me",
            RetryCount = 1,
            RetryAfter = TimeSpan.FromMilliseconds(25)
        };

        telemetry.Raise(t => t.RateLimitObserved += null, telemetry.Object, expected);

        client.SupportsRateLimitTelemetry.Should().BeTrue();
        received.Should().NotBeNull();
        received!.Kind.Should().Be(RateLimitTelemetryKind.RetryScheduled);
        received.Route.Should().Be("GET users/@me");
    }

    [Fact]
    public void RateLimitObserved_SubscribeIsNoOp_WhenRestClientDoesNotSupportTelemetry()
    {
        var deps = CreateDependencies();
        var client = CreateClient(deps);

        client.SupportsRateLimitTelemetry.Should().BeFalse();

        Action act = () => client.RateLimitObserved += (_, _) => { };
        act.Should().NotThrow();
    }

    private static DiscordClient CreateClient(TestDependencies deps)
        => new(
            deps.Options,
            deps.Cache.Object,
            deps.Logger.Object,
            deps.Rest.Object,
            deps.Gateway.Object);

    [EventInterest("MESSAGE_CREATE")]
    private static Task HandleAnnotatedMessageAsync(MessageCreateEvent _)
        => Task.CompletedTask;

    private static TestDependencies CreateDependencies(PawSharpOptions? options = null)
    {
        var dispatcher = new EventDispatcher();
        var gateway = new Mock<IGatewayClient>();
        gateway.SetupGet(g => g.Events).Returns(dispatcher);
        gateway.SetupGet(g => g.CurrentState).Returns(GatewayState.Disconnected);
        gateway.Setup(g => g.ConnectAsync()).Returns(Task.CompletedTask);
        gateway.Setup(g => g.DisconnectAsync()).Returns(Task.CompletedTask);
        gateway.Setup(g => g.UpdatePresenceAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        return new TestDependencies(
            options ?? new PawSharpOptions { Token = "Bot abc.def.ghi" },
            new Mock<IEntityCache>(),
            new Mock<ILogger<DiscordClient>>(),
            new Mock<IDiscordRestClient>(),
            gateway,
            dispatcher);
    }

    private sealed record TestDependencies(
        PawSharpOptions Options,
        Mock<IEntityCache> Cache,
        Mock<ILogger<DiscordClient>> Logger,
        Mock<IDiscordRestClient> Rest,
        Mock<IGatewayClient> Gateway,
        EventDispatcher Dispatcher);
}
