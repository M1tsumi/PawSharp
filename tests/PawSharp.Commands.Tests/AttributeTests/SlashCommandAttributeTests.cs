#nullable enable
using System;
using FluentAssertions;
using PawSharp.Commands.Attributes;
using Xunit;

namespace PawSharp.Commands.Tests.AttributeTests;

public class SlashCommandAttributeTests
{
    [Fact]
    public void SlashCommandAttribute_SetsNameAndDescription()
    {
        var attr = new SlashCommandAttribute("ping", "Ping the bot");
        attr.Name.Should().Be("ping");
        attr.Description.Should().Be("Ping the bot");
    }

    [Fact]
    public void SlashCommandAttribute_NullName_Throws()
    {
        Action act = () => new SlashCommandAttribute(null!, "desc");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SlashOptionAttribute_SetsNameAndDescription()
    {
        var attr = new SlashOptionAttribute("option1", "An option");
        attr.Name.Should().Be("option1");
        attr.Description.Should().Be("An option");
        attr.Required.Should().BeTrue();
    }

    [Fact]
    public void SlashOptionAttribute_RequiredCanBeSetFalse()
    {
        var attr = new SlashOptionAttribute("opt", "desc") { Required = false };
        attr.Required.Should().BeFalse();
    }

    [Fact]
    public void SlashGroupAttribute_SetsNameAndDescription()
    {
        var attr = new SlashGroupAttribute("group", "A group");
        attr.Name.Should().Be("group");
        attr.Description.Should().Be("A group");
    }

    [Fact]
    public void SlashSubCommandAttribute_SetsNameAndDescription()
    {
        var attr = new SlashSubCommandAttribute("sub", "A subcommand");
        attr.Name.Should().Be("sub");
        attr.Description.Should().Be("A subcommand");
    }

    [Fact]
    public void SlashNsfwAttribute_Exists()
    {
        var attr = new SlashNsfwAttribute();
        attr.Should().NotBeNull();
    }

    [Fact]
    public void SlashDmPermissionAttribute_SetsAllowDm()
    {
        var attr = new SlashDmPermissionAttribute(true);
        attr.AllowDm.Should().BeTrue();
    }

    [Fact]
    public void SlashDefaultPermissionAttribute_SetsPermission()
    {
        var attr = new SlashDefaultPermissionAttribute(true);
        attr.Permission.Should().BeTrue();
    }

    [Fact]
    public void SlashDefaultMemberPermissionsAttribute_SetsPermissions()
    {
        var attr = new SlashDefaultMemberPermissionsAttribute(8UL);
        attr.Permissions.Should().Be(8);
    }

    [Fact]
    public void SlashIntegrationTypesAttribute_SetsIntegrationTypes()
    {
        var attr = new SlashIntegrationTypesAttribute(0, 1);
        attr.IntegrationTypes.Should().BeEquivalentTo(new[] { 0, 1 });
    }

    [Fact]
    public void SlashContextsAttribute_SetsContexts()
    {
        var attr = new SlashContextsAttribute(0, 1);
        attr.Contexts.Should().BeEquivalentTo(new[] { 0, 1 });
    }

    [Fact]
    public void SlashAutocompleteAttribute_Exists()
    {
        var attr = new SlashAutocompleteAttribute();
        attr.Should().NotBeNull();
    }

    [Fact]
    public void SlashMinValueAttribute_SetsMinValue()
    {
        var attr = new SlashMinValueAttribute(1.0);
        attr.MinValue.Should().Be(1.0);
    }

    [Fact]
    public void SlashMaxValueAttribute_SetsMaxValue()
    {
        var attr = new SlashMaxValueAttribute(100.0);
        attr.MaxValue.Should().Be(100.0);
    }

    [Fact]
    public void SlashMinLengthAttribute_SetsMinLength()
    {
        var attr = new SlashMinLengthAttribute(1);
        attr.MinLength.Should().Be(1);
    }

    [Fact]
    public void SlashMaxLengthAttribute_SetsMaxLength()
    {
        var attr = new SlashMaxLengthAttribute(100);
        attr.MaxLength.Should().Be(100);
    }

    [Fact]
    public void SlashChannelTypesAttribute_SetsChannelTypes()
    {
        var attr = new SlashChannelTypesAttribute(0, 1);
        attr.ChannelTypes.Should().BeEquivalentTo(new[] { 0, 1 });
    }

    [Fact]
    public void SlashChoiceAttribute_SetsNameAndValue()
    {
        var attr = new SlashChoiceAttribute("first", "1");
        attr.Name.Should().Be("first");
        attr.Value.Should().Be("1");
    }

    [Fact]
    public void SlashLocalizedNameAttribute_SetsLocaleAndName()
    {
        var attr = new SlashLocalizedNameAttribute("en-US", "ping");
        attr.Locale.Should().Be("en-US");
        attr.Name.Should().Be("ping");
    }

    [Fact]
    public void SlashLocalizedDescriptionAttribute_SetsLocaleAndDescription()
    {
        var attr = new SlashLocalizedDescriptionAttribute("en-US", "A ping command");
        attr.Locale.Should().Be("en-US");
        attr.Description.Should().Be("A ping command");
    }

    [Fact]
    public void UserContextMenuAttribute_SetsName()
    {
        var attr = new UserContextMenuAttribute("View Info");
        attr.Name.Should().Be("View Info");
    }

    [Fact]
    public void MessageContextMenuAttribute_SetsName()
    {
        var attr = new MessageContextMenuAttribute("Copy Text");
        attr.Name.Should().Be("Copy Text");
    }

    [Fact]
    public void AutocompleteHandlerAttribute_SetsCommandAndOption()
    {
        var attr = new AutocompleteHandlerAttribute("ping", "target");
        attr.CommandName.Should().Be("ping");
        attr.OptionName.Should().Be("target");
    }
}
