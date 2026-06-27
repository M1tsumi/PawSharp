#nullable enable
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PawSharp.Interactions.Builders;
using PawSharp.Core.Entities;
using Xunit;

namespace PawSharp.Interactions.Tests;

public class SlashCommandBuilderTests
{
    [Fact]
    public void Constructor_SetsNameAndDescription()
    {
        var builder = new SlashCommandBuilder("ping", "Ping the bot");
        var cmd = builder.Build();
        cmd.Name.Should().Be("ping");
        cmd.Description.Should().Be("Ping the bot");
        cmd.Type.Should().Be(ApplicationCommandType.ChatInput);
    }

    [Fact]
    public void SetDefaultMemberPermissions_SetsPermissions()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.SetDefaultMemberPermissions((Core.Enums.Permissions)8);
        var cmd = builder.Build();
        cmd.DefaultMemberPermissions.Should().Be((Core.Enums.Permissions)8);
    }

    [Fact]
    public void SetDmPermission_SetsFlag()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.SetDmPermission(false);
        var cmd = builder.Build();
        cmd.DmPermission.Should().BeFalse();
    }

    [Fact]
    public void SetNsfw_SetsFlag()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.SetNsfw(true);
        var cmd = builder.Build();
        cmd.Nsfw.Should().BeTrue();
    }

    [Fact]
    public void AddStringOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddStringOption("name", "Enter name", required: true);
        var cmd = builder.Build();
        cmd.Options.Should().HaveCount(1);
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.String);
        cmd.Options[0].Name.Should().Be("name");
        cmd.Options[0].Required.Should().BeTrue();
    }

    [Fact]
    public void AddIntegerOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddIntegerOption("count", "How many", minValue: 1, maxValue: 10);
        var cmd = builder.Build();
        cmd.Options.Should().HaveCount(1);
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Integer);
        cmd.Options[0].MinValue.Should().Be(1);
        cmd.Options[0].MaxValue.Should().Be(10);
    }

    [Fact]
    public void AddBooleanOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddBooleanOption("enabled", "Enable feature");
        var cmd = builder.Build();
        cmd.Options.Should().HaveCount(1);
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Boolean);
    }

    [Fact]
    public void AddUserOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddUserOption("target", "Select user");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.User);
    }

    [Fact]
    public void AddChannelOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddChannelOption("channel", "Select channel");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Channel);
    }

    [Fact]
    public void AddRoleOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddRoleOption("role", "Select role");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Role);
    }

    [Fact]
    public void AddMentionableOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddMentionableOption("mentionable", "Select target");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Mentionable);
    }

    [Fact]
    public void AddNumberOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddNumberOption("price", "Enter price", minValue: 1.0, maxValue: 100.0);
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Number);
        cmd.Options[0].MinValue.Should().Be(1.0);
        cmd.Options[0].MaxValue.Should().Be(100.0);
    }

    [Fact]
    public void AddAttachmentOption_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddAttachmentOption("file", "Upload file");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.Attachment);
    }

    [Fact]
    public void AddSubcommand_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddSubcommand("create", "Create item");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.SubCommand);
    }

    [Fact]
    public void AddSubcommandGroup_AddsOption()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddSubcommandGroup("admin", "Admin commands");
        var cmd = builder.Build();
        cmd.Options![0].Type.Should().Be(ApplicationCommandOptionType.SubCommandGroup);
    }

    [Fact]
    public void AddMultipleOptions_Works()
    {
        var builder = new SlashCommandBuilder("test", "desc");
        builder.AddStringOption("name", "Name");
        builder.AddIntegerOption("count", "Count");
        builder.AddBooleanOption("flag", "Flag");
        var cmd = builder.Build();
        cmd.Options.Should().HaveCount(3);
    }
}
