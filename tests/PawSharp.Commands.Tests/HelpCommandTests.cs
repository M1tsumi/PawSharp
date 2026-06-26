#nullable enable
using System.Collections.Generic;
using FluentAssertions;
using PawSharp.Commands.Help;
using Xunit;

namespace PawSharp.Commands.Tests;

public class HelpCommandTests
{
    [Fact]
    public void GenerateHelp_WithCommands_ReturnsContent()
    {
        var commands = new List<CommandInfo>();
        var result = HelpCommand.GenerateHelp(commands);
        result.Should().NotBeNull();
    }

    [Fact]
    public void GenerateCommandHelp_WithCommand_ReturnsInfo()
    {
        var cmd = new CommandInfo("ping", new[] { "p" }, "Ping command");
        var help = HelpCommand.GenerateCommandHelp(cmd);
        help.Should().Contain("ping");
        help.Should().Contain("Ping command");
    }
}
