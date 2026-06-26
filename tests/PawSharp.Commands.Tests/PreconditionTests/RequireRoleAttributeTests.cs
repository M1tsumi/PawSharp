#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireRoleAttributeTests
{
    [Fact]
    public async Task CheckAsync_NoGuild_ReturnsError()
    {
        var attr = new RequireRoleAttribute(12345);
        var ctx = TestContextBuilder.CreateContext();
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("server");
    }

    [Fact]
    public async Task CheckAsync_HasRequiredRole_ReturnsSuccess()
    {
        var attr = new RequireRoleAttribute(42);
        var ctx = TestContextBuilder.CreateContext(guildId: 1, userId: 1);
        ctx.Member!.Roles = new System.Collections.Generic.List<ulong> { 42 };
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullRoleIds_Throws()
    {
        FluentAssertions.AssertionExtensions.Should(() => new RequireRoleAttribute(null!)).Throw<System.ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyRoleIds_Throws()
    {
        FluentAssertions.AssertionExtensions.Should(() => new RequireRoleAttribute()).Throw<System.ArgumentException>();
    }
}
