#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace PawSharp.Voice.Tests;

public class VoiceExtensionsTests
{
    [Fact]
    public void ConvertToHexString_ReturnsCorrectHex()
    {
        var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        Convert.ToHexString(data).Should().Be("DEADBEEF");
    }

    [Fact]
    public void ConvertToHexString_Empty()
    {
        Convert.ToHexString(Array.Empty<byte>()).Should().Be("");
    }

    [Fact]
    public void ConvertToHexString_SingleByte()
    {
        Convert.ToHexString(new byte[] { 0x0F }).Should().Be("0F");
    }

    [Fact]
    public void ConvertFromHexString_RoundTrip()
    {
        var original = "DEADBEEF";
        var bytes = Convert.FromHexString(original);
        Convert.ToHexString(bytes).Should().Be(original);
    }

    [Fact]
    public void ConvertFromHexString_Lowercase()
    {
        Convert.ToHexString(Convert.FromHexString("deadbeef")).Should().Be("DEADBEEF");
    }

    [Fact]
    public void ConvertFromHexString_Empty()
    {
        Convert.FromHexString("").Should().BeEmpty();
    }

    [Fact]
    public void ConvertFromHexString_InvalidLength_Throws()
    {
        Action act = () => Convert.FromHexString("ABC");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ConvertFromHexString_InvalidChars_Throws()
    {
        Action act = () => Convert.FromHexString("XXYYZZ");
        act.Should().Throw<FormatException>();
    }
}
