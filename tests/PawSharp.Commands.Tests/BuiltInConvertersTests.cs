#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.Cache.Interfaces;
using PawSharp.API.Interfaces;
using PawSharp.Commands.Conversion;
using PawSharp.Core.Entities;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Commands.Tests;

public class BuiltInConvertersTests
{
    private readonly TypeConverterService _service;
    private readonly CommandContext _ctx;

    public BuiltInConvertersTests()
    {
        var options = new Core.Models.PawSharpOptions { Token = "Bot test.token" };
        var restMock = new Mock<IDiscordRestClient>();
        var cacheMock = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(new EventDispatcher());
        var client = new Client.DiscordClient(options, cacheMock.Object, NullLogger<Client.DiscordClient>.Instance, restMock.Object, gatewayMock.Object);

        _service = new TypeConverterService(NullLogger<TypeConverterService>.Instance);
        _ctx = new CommandContext(client, new Message { Id = 1, ChannelId = 1 }, "!", "test", Array.Empty<string>(), "");
    }

    [Fact]
    public async Task ConvertAsync_Int_Success()
    {
        _service.RegisterConverter(new TestIntConverter());
        var result = await _service.ConvertAsync(typeof(int), "42", _ctx);
        result.Should().Be(42);
    }

    [Fact]
    public async Task ConvertAsync_Long_Success()
    {
        _service.RegisterConverter(new TestLongConverter());
        var result = await _service.ConvertAsync(typeof(long), "123456789", _ctx);
        result.Should().Be(123456789L);
    }

    [Fact]
    public async Task ConvertAsync_Double_Success()
    {
        _service.RegisterConverter(new TestDoubleConverter());
        var result = await _service.ConvertAsync(typeof(double), "3.14", _ctx);
        result.Should().Be(3.14);
    }

    [Fact]
    public async Task ConvertAsync_Bool_Success()
    {
        _service.RegisterConverter(new TestBoolConverter());
        var result = await _service.ConvertAsync(typeof(bool), "true", _ctx);
        result.Should().Be(true);
    }

    [Fact]
    public async Task ConvertAsync_Ulong_Success()
    {
        _service.RegisterConverter(new TestULongConverter());
        var result = await _service.ConvertAsync(typeof(ulong), "12345678901234567890", _ctx);
        result.Should().Be(12345678901234567890UL);
    }
}

internal class TestIntConverter : PawSharp.Commands.Conversion.SyncTypeConverter<int>
{
    protected override PawSharp.Commands.Conversion.TypeConverterResult<int> ConvertSync(string value, CommandContext context)
        => PawSharp.Commands.Conversion.TypeConverterResult<int>.FromSuccess(int.Parse(value));
}

internal class TestLongConverter : PawSharp.Commands.Conversion.SyncTypeConverter<long>
{
    protected override PawSharp.Commands.Conversion.TypeConverterResult<long> ConvertSync(string value, CommandContext context)
        => PawSharp.Commands.Conversion.TypeConverterResult<long>.FromSuccess(long.Parse(value));
}

internal class TestDoubleConverter : PawSharp.Commands.Conversion.SyncTypeConverter<double>
{
    protected override PawSharp.Commands.Conversion.TypeConverterResult<double> ConvertSync(string value, CommandContext context)
        => PawSharp.Commands.Conversion.TypeConverterResult<double>.FromSuccess(double.Parse(value));
}

internal class TestBoolConverter : PawSharp.Commands.Conversion.SyncTypeConverter<bool>
{
    protected override PawSharp.Commands.Conversion.TypeConverterResult<bool> ConvertSync(string value, CommandContext context)
        => PawSharp.Commands.Conversion.TypeConverterResult<bool>.FromSuccess(bool.Parse(value));
}

internal class TestULongConverter : PawSharp.Commands.Conversion.SyncTypeConverter<ulong>
{
    protected override PawSharp.Commands.Conversion.TypeConverterResult<ulong> ConvertSync(string value, CommandContext context)
        => PawSharp.Commands.Conversion.TypeConverterResult<ulong>.FromSuccess(ulong.Parse(value));
}
