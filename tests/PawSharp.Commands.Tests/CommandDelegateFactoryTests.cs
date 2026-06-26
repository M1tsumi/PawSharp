#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Commands.Execution;
using Xunit;

namespace PawSharp.Commands.Tests;

public class CommandDelegateFactoryTests
{
    private static Task SampleMethod(string arg) => Task.CompletedTask;

    [Fact]
    public void CreateDelegate_ReturnsDelegate()
    {
        var method = typeof(CommandDelegateFactoryTests).GetMethod(nameof(SampleMethod), BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var del = CommandDelegateFactory.CreateDelegate(method!);
        del.Should().NotBeNull();
    }

    [Fact]
    public void CreateDelegate_NullMethod_Throws()
    {
        Action act = () => CommandDelegateFactory.CreateDelegate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
