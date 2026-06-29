#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Preconditions;
using Xunit;

namespace PawSharp.Commands.Tests.PreconditionTests;

public class CooldownAttributeTests
{
    [Fact]
    public async Task CheckAsync_FirstUse_ReturnsSuccess()
    {
        var cooldown = new CooldownAttribute(1, 60);
        var ctx = TestContextBuilder.CreateContext(guildId: 1);
        var result = await cooldown.CheckAsync(ctx);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ExceedMaxUses_ReturnsError()
    {
        var cooldown = new CooldownAttribute(1, 60);
        var ctx = TestContextBuilder.CreateContext(guildId: 1);
        await cooldown.CheckAsync(ctx);
        var result = await cooldown.CheckAsync(ctx);
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cooldown");
    }

    [Fact]
    public async Task CheckAsync_DifferentUsers_DifferentBuckets()
    {
        var cooldown = new CooldownAttribute(1, 60);
        var ctx1 = TestContextBuilder.CreateContext(guildId: 1, userId: 100);
        var ctx2 = TestContextBuilder.CreateContext(guildId: 1, userId: 200);

        (await cooldown.CheckAsync(ctx1)).IsSuccess.Should().BeTrue();
        (await cooldown.CheckAsync(ctx2)).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NegativeMaxUses_Throws()
    {
        Action act = () => new CooldownAttribute(0, 60);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ZeroPerSeconds_Throws()
    {
        Action act = () => new CooldownAttribute(1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

}
