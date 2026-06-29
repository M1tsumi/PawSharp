#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireBotPermissionAttributeTests
{
    [Fact]
    public async Task CheckAsync_NoGuild_ReturnsError()
    {
        var attr = new RequireBotPermissionAttribute(8);
        var ctx = TestContextBuilder.CreateContext();
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("server");
    }

    [Fact]
    public void Constructor_SetsRequiredPermissions()
    {
        var attr = new RequireBotPermissionAttribute(8);
        attr.RequiredPermissions.Should().Be(8);
    }
}
