#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireChannelAttributeTests
{
    [Fact]
    public async Task CheckAsync_AllowedChannel_ReturnsSuccess()
    {
        var attr = new RequireChannelAttribute(10, 20);
        var ctx = TestContextBuilder.CreateContext(channelId: 10);
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_DisallowedChannel_ReturnsError()
    {
        var attr = new RequireChannelAttribute(10, 20);
        var ctx = TestContextBuilder.CreateContext(channelId: 99);
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullChannelIds_Throws()
    {
        Action act = () => new RequireChannelAttribute(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyChannelIds_Throws()
    {
        Action act = () => new RequireChannelAttribute();
        act.Should().Throw<ArgumentException>();
    }
}
