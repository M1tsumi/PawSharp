#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequirePermissionsAttributeTests
{
    [Fact]
    public async Task CheckAsync_NoGuild_ReturnsError()
    {
        var attr = new RequirePermissionsAttribute(8);
        var ctx = TestContextBuilder.CreateContext();
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("server");
    }

    [Fact]
    public async Task CheckAsync_WithPermissions_Delegates()
    {
        var attr = new RequirePermissionsAttribute(8);
        var ctx = TestContextBuilder.CreateContext(guildId: 1);
        var result = await attr.CheckAsync(ctx);
        result.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_SetsRequiredPermissions()
    {
        var attr = new RequirePermissionsAttribute(8);
        attr.RequiredPermissions.Should().Be(8);
    }

    [Fact]
    public void IgnoreAdmins_DefaultIsTrue()
    {
        var attr = new RequirePermissionsAttribute(8);
        attr.IgnoreAdmins.Should().BeTrue();
    }
}
