#nullable enable
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class RequireOwnerAttributeTests
{
    [Fact]
    public async Task CheckAsync_MatchingOwner_ReturnsSuccess()
    {
        var attr = new RequireOwnerAttribute(42);
        var ctx = TestContextBuilder.CreateContext(userId: 42);
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_NonMatchingOwner_ReturnsError()
    {
        var attr = new RequireOwnerAttribute(42);
        var ctx = TestContextBuilder.CreateContext(userId: 99);
        var result = await attr.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("owner");
    }
}
