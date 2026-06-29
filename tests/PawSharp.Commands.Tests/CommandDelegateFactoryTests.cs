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
    [Fact]
    public void CreateDelegate_NullMethod_Throws()
    {
        Action act = () => CommandDelegateFactory.CreateDelegate(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
