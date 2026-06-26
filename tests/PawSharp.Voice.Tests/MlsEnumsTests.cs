#nullable enable
using FluentAssertions;
using Xunit;

namespace PawSharp.Voice.Tests;

public class MlsEnumsTests
{
    [Theory]
    [InlineData(0x0002)]
    public void CipherSuite_Values(ushort value)
    {
        value.Should().Be(0x0002);
    }

    [Theory]
    [InlineData(1)]
    public void ProtocolVersion_Values(ushort value)
    {
        value.Should().Be(1);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void ContentType_Values(byte value, byte expected)
    {
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 6)]
    [InlineData(7, 7)]
    public void ProposalType_Values(ushort value, ushort expected)
    {
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void CredentialType_Values(ushort value, ushort expected)
    {
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    public void SenderType_Values(byte value, byte expected)
    {
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void LeafNodeSource_Values(byte value, byte expected)
    {
        value.Should().Be(expected);
    }
}
