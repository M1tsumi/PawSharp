#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Commands.Middleware;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Commands.Tests;

public class MiddlewarePipelineTests
{
    [Fact]
    public async Task ExecuteAsync_NoMiddleware_ExecutesAction()
    {
        var pipeline = new MiddlewarePipeline();
        var ctx = CreateContext();
        bool executed = false;

        await pipeline.ExecuteAsync(ctx, () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithMiddleware_ChainsCorrectly()
    {
        var pipeline = new MiddlewarePipeline();
        pipeline.Use(new TestMiddleware());

        var ctx = CreateContext();
        bool executed = false;

        await pipeline.ExecuteAsync(ctx, () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    private static CommandContext CreateContext()
    {
        var options = new PawSharpOptions { Token = "Bot test.token" };
        var restMock = new Mock<IDiscordRestClient>();
        var cacheMock = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(new EventDispatcher());
        var client = new Client.DiscordClient(options, cacheMock.Object, NullLogger<Client.DiscordClient>.Instance, restMock.Object, gatewayMock.Object);
        return new CommandContext(client, new Message { Id = 1, ChannelId = 1 }, "!", "test", Array.Empty<string>(), "");
    }
}

internal class TestMiddleware : IMiddleware
{
    public async Task InvokeAsync(CommandContext context, Func<Task> next)
    {
        await next();
    }
}
