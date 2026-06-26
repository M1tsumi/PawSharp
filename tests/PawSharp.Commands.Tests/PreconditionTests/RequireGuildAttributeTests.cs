#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireGuildAttributeTests
{
    [Fact]
    public async Task CheckAsync_InGuild_ReturnsSuccess()
    {
        var attr = new RequireGuildAttribute();
        var ctx = TestContextBuilder.CreateContext(guildId: 1);
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_NotInGuild_ReturnsError()
    {
        var attr = new RequireGuildAttribute();
        var ctx = TestContextBuilder.CreateContext();
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("server");
    }
}
