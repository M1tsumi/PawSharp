#nullable enable
using System.Linq;
using FluentAssertions;
using PawSharp.Commands.Conversion;
using Xunit;

namespace PawSharp.Commands.Tests;

public class ArgumentParserTests
{
    [Fact]
    public void ExtractCommand_WithPrefix_ReturnsCommandAndArgs()
    {
        var (cmd, args) = ArgumentParser.ExtractCommand("!ping hello world", "!");
        cmd.Should().Be("ping");
        args.Should().Be("hello world");
    }

    [Fact]
    public void ExtractCommand_NoPrefix_ReturnsEmptyStrings()
    {
        var (cmd, args) = ArgumentParser.ExtractCommand("hello world", "!");
        cmd.Should().BeEmpty();
        args.Should().BeEmpty();
    }

    [Fact]
    public void ExtractCommand_EmptyContent_ReturnsEmptyStrings()
    {
        var (cmd, args) = ArgumentParser.ExtractCommand("", "!");
        cmd.Should().BeEmpty();
        args.Should().BeEmpty();
    }

    [Fact]
    public void ParseArguments_SimpleArgs_ReturnsArray()
    {
        var result = ArgumentParser.ParseArguments("hello world");
        result.Should().HaveCount(2);
        result[0].Should().Be("hello");
        result[1].Should().Be("world");
    }

    [Fact]
    public void ParseArguments_QuotedArgs_PreservesSpaces()
    {
        var result = ArgumentParser.ParseArguments("\"hello world\" test");
        result.Should().HaveCount(2);
        result[0].Should().Be("hello world");
        result[1].Should().Be("test");
    }

    [Fact]
    public void ParseArguments_EscapedQuotes_Works()
    {
        var result = ArgumentParser.ParseArguments("say \\\"hello\\\"");
        result.Should().HaveCount(2);
        result[0].Should().Be("say");
        result[1].Should().Be("\"hello\"");
    }

    [Fact]
    public void ParseArguments_EmptyString_ReturnsEmpty()
    {
        var result = ArgumentParser.ParseArguments("");
        result.Should().BeEmpty();
    }
}
