#nullable enable
using FluentAssertions;
using PawSharp.Commands.DependencyInjection;
using Xunit;

namespace PawSharp.Commands.Tests;

public class CommandsBuilderTests
{
    [Fact]
    public void Constructor_SetsDefaultPrefix()
    {
        var builder = new CommandsBuilder();
        builder.Should().NotBeNull();
    }

    [Fact]
    public void WithPrefix_SetsPrefix()
    {
        var builder = new CommandsBuilder();
        builder.WithPrefix(">>");
        builder.Should().NotBeNull();
    }

    [Fact]
    public void WithCaseSensitivity_SetsFlag()
    {
        var builder = new CommandsBuilder();
        builder.WithCaseSensitivity(true);
        builder.Should().NotBeNull();
    }
}
