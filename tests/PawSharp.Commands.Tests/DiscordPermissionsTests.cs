#nullable enable
using FluentAssertions;
using PawSharp.Commands.Permissions;
using Xunit;

namespace PawSharp.Commands.Tests;

public class DiscordPermissionsTests
{
    [Fact]
    public void Administrator_Value_IsCorrect()
    {
        DiscordPermissions.Administrator.Should().Be(8);
    }

    [Fact]
    public void ManageGuild_Value_IsCorrect()
    {
        DiscordPermissions.ManageGuild.Should().Be(32);
    }

    [Fact]
    public void SendMessages_Value_IsCorrect()
    {
        DiscordPermissions.SendMessages.Should().Be(2048);
    }

    [Fact]
    public void ManageMessages_Value_IsCorrect()
    {
        DiscordPermissions.ManageMessages.Should().Be(8192);
    }

    [Fact]
    public void KickMembers_Value_IsCorrect()
    {
        DiscordPermissions.KickMembers.Should().Be(2);
    }

    [Fact]
    public void BanMembers_Value_IsCorrect()
    {
        DiscordPermissions.BanMembers.Should().Be(4);
    }

    [Fact]
    public void ReadMessages_Value_IsCorrect()
    {
        DiscordPermissions.ReadMessages.Should().Be(1024);
    }
}
