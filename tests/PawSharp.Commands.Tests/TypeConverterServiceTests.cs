#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PawSharp.Commands.Conversion;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Commands.Tests;

public class TypeConverterServiceTests
{
    [Fact]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        var svc = new TypeConverterService(null);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void RegisterConverter_AddsConverter()
    {
        var svc = new TypeConverterService(NullLogger<TypeConverterService>.Instance);
        svc.RegisterConverter(new TestIntConverter());
    }

    [Fact]
    public async Task ConvertAsync_RegisteredConverter_ReturnsValue()
    {
        var svc = new TypeConverterService(NullLogger<TypeConverterService>.Instance);
        svc.RegisterConverter(new TestIntConverter());
        var ctx = CreateEmptyContext();
        var result = await svc.ConvertAsync(typeof(int), "42", ctx);
        result.Should().Be(42);
    }

    [Fact]
    public async Task ConvertAsync_UnregisteredType_ReturnsNull()
    {
        var svc = new TypeConverterService(NullLogger<TypeConverterService>.Instance);
        var ctx = CreateEmptyContext();
        var result = await svc.ConvertAsync(typeof(Guid), "test", ctx);
        result.Should().BeNull();
    }

    private static CommandContext CreateEmptyContext()
    {
        var options = new PawSharp.Core.Models.PawSharpOptions { Token = "Bot test.token" };
        var restMock = new Moq.Mock<API.Interfaces.IDiscordRestClient>();
        var cacheMock = new Moq.Mock<Cache.Interfaces.IEntityCache>();
        var gatewayMock = new Moq.Mock<Gateway.IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(new EventDispatcher());
        var client = new Client.DiscordClient(options, cacheMock.Object, NullLogger<Client.DiscordClient>.Instance, restMock.Object, gatewayMock.Object);

        return new CommandContext(
            client,
            new Core.Entities.Message { Id = 1, ChannelId = 1, Content = "!test" },
            "!", "test", System.Array.Empty<string>(), "");
    }
}
