#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Interactivity.Builders;
using Xunit;

namespace PawSharp.Interactivity.Tests;

public class InteractivityFlowBuilderTests
{
    private static (DiscordClient client, Channel channel, User user) BuildTestContext()
    {
        var dispatcher = new EventDispatcher();
        var restMock = new Mock<IDiscordRestClient>();
        var cacheMock = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(dispatcher);
        var options = new PawSharpOptions { Token = "Bot test.token" };
        var client = new DiscordClient(options, cacheMock.Object, NullLogger<DiscordClient>.Instance, restMock.Object, gatewayMock.Object);
        var channel = new Channel { Id = 1 };
        var user = new User { Id = 42 };
        return (client, channel, user);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var (client, channel, user) = BuildTestContext();
        var builder = new InteractivityFlowBuilder(client, channel, user, TimeSpan.FromSeconds(30), CancellationToken.None);
        builder.Should().NotBeNull();
    }

    [Fact]
    public void WithTimeout_ReturnsBuilder()
    {
        var (client, channel, user) = BuildTestContext();
        var builder = new InteractivityFlowBuilder(client, channel, user, TimeSpan.FromSeconds(30), CancellationToken.None);
        builder.Should().NotBeNull();
    }
}
