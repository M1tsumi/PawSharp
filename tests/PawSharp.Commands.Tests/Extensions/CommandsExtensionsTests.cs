#nullable enable
using System;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Commands.Conversion;
using PawSharp.Commands.Middleware;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Commands.Tests.Extensions;

public class CommandsExtensionsTests
{
    [Fact]
    public void Constructor_DefaultPrefix_Works()
    {
        var ext = new CommandsExtension();
        ext.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_CustomPrefix_Works()
    {
        var ext = new CommandsExtension(">>");
        ext.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullPrefix_Throws()
    {
        Action act = () => new CommandsExtension(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisterModule_WithClient_WiresEvents()
    {
        var ext = new CommandsExtension("!");
        var cacheMock = new Mock<IEntityCache>();
        var restMock = new Mock<IDiscordRestClient>();
        var gatewayMock = new Mock<IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(new EventDispatcher());
        var options = new PawSharpOptions { Token = "Bot test" };
        var client = new Client.DiscordClient(options, cacheMock.Object, NullLogger<Client.DiscordClient>.Instance, restMock.Object, gatewayMock.Object);

        var module = new TestModule();
        ext.RegisterModule(client, module);
        ext.GetRegisteredCommands().Should().HaveCount(1);
    }

    [Fact]
    public void GetRegisteredCommands_InitialEmpty()
    {
        var ext = new CommandsExtension("!");
        ext.GetRegisteredCommands().Should().BeEmpty();
    }
}

public class TestModule : BaseCommandModule
{
    [Command("ping")]
    [Description("Ping command")]
    public Task PingAsync() => Task.CompletedTask;
}
