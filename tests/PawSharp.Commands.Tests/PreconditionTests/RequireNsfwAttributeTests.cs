#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.Core.Entities;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireNsfwAttributeTests
{
    [Fact]
    public async Task CheckAsync_NoGuild_ReturnsError()
    {
        var attr = new RequireNsfwAttribute();
        var ctx = TestContextBuilder.CreateContext();
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_NsfwChannel_ReturnsSuccess()
    {
        var attr = new RequireNsfwAttribute();
        var ctx = TestContextBuilder.CreateContext(guildId: 1);

        var restMock = Mock.Get(ctx.Client.Rest);
        restMock.Setup(r => r.GetChannelAsync(1))
            .ReturnsAsync(new Channel { Id = 1, Nsfw = true });

        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_NonNsfwChannel_ReturnsError()
    {
        var attr = new RequireNsfwAttribute();
        var ctx = TestContextBuilder.CreateContext(guildId: 1);

        var restMock = Mock.Get(ctx.Client.Rest);
        restMock.Setup(r => r.GetChannelAsync(1))
            .ReturnsAsync(new Channel { Id = 1, Nsfw = false });

        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
    }
}
