#nullable enable
using System;
using FluentAssertions;
using PawSharp.Commands.Utilities;
using Xunit;

namespace PawSharp.Commands.Tests;

public class SlashCommandValidatorTests
{
    [Theory]
    [InlineData("ping")]
    [InlineData("hello-world")]
    [InlineData("command_name")]
    [InlineData("a")]
    [InlineData("valid-command-123")]
    public void ValidateName_ValidNames_ReturnTrue(string name)
    {
        var result = SlashCommandValidator.ValidateName(name);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    [InlineData("has space")]
    [InlineData("special!char")]
    [InlineData("-starts-with-hyphen")]
    [InlineData("_starts-with-underscore")]
    [InlineData("this-name-is-way-too-long-for-a-slash-command")]
    public void ValidateName_InvalidNames_ReturnFalse(string name)
    {
        var result = SlashCommandValidator.ValidateName(name);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("A description")]
    [InlineData("Short")]
    [InlineData("a")]
    public void ValidateDescription_ValidDescriptions_ReturnTrue(string desc)
    {
        var result = SlashCommandValidator.ValidateDescription(desc);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateDescription_InvalidDescriptions_ReturnFalse(string desc)
    {
        var result = SlashCommandValidator.ValidateDescription(desc);
        result.IsValid.Should().BeFalse();
    }
}
