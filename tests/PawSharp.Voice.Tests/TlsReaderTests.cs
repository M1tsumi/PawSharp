#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Encoding;
using Xunit;

namespace PawSharp.Voice.Tests;

public class TlsReaderTests
{
    [Fact]
    public void ReadUint8_ReturnsSingleByte()
    {
        using var r = new TlsWriter();
        r.WriteUint8(0xAB);
        var data = r.ToArray();

        var reader = new TlsReader(data);
        reader.ReadUint8().Should().Be(0xAB);
        reader.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ReadUint16_ReadsBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint16(0x0102);
        var reader = new TlsReader(w.ToArray());
        reader.ReadUint16().Should().Be(0x0102);
    }

    [Fact]
    public void ReadUint32_ReadsBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint32(0x01020304u);
        var reader = new TlsReader(w.ToArray());
        reader.ReadUint32().Should().Be(0x01020304u);
    }

    [Fact]
    public void ReadUint64_ReadsBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint64(0x0102030405060708uL);
        var reader = new TlsReader(w.ToArray());
        reader.ReadUint64().Should().Be(0x0102030405060708uL);
    }

    [Fact]
    public void ReadBytes_ReturnsExactCount()
    {
        using var w = new TlsWriter();
        w.WriteBytes(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var reader = new TlsReader(w.ToArray());
        reader.ReadBytes(2).Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });
        reader.ReadBytes(2).Should().BeEquivalentTo(new byte[] { 0x03, 0x04 });
    }

    [Fact]
    public void ReadVector8_ReadsLengthPrefixedData()
    {
        using var w = new TlsWriter();
        w.WriteVector8(new byte[] { 0xAA, 0xBB, 0xCC });
        var reader = new TlsReader(w.ToArray());
        reader.ReadVector8().Should().BeEquivalentTo(new byte[] { 0xAA, 0xBB, 0xCC });
    }

    [Fact]
    public void ReadVector16_Reads2ByteLengthPrefixedData()
    {
        using var w = new TlsWriter();
        w.WriteVector16(new byte[] { 0xDE, 0xAD });
        var reader = new TlsReader(w.ToArray());
        reader.ReadVector16().Should().BeEquivalentTo(new byte[] { 0xDE, 0xAD });
    }

    [Fact]
    public void ReadVector32_Reads4ByteLengthPrefixedData()
    {
        using var w = new TlsWriter();
        w.WriteVector32(new byte[] { 0xFF });
        var reader = new TlsReader(w.ToArray());
        reader.ReadVector32().Should().BeEquivalentTo(new byte[] { 0xFF });
    }

    [Fact]
    public void ReadUint8_Overrun_Throws()
    {
        Action act = () => new TlsReader(Array.Empty<byte>()).ReadUint8();
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void ReadUint16_Overrun_Throws()
    {
        Action act = () => new TlsReader(new byte[] { 0x01 }).ReadUint16();
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void ReadUint32_Overrun_Throws()
    {
        Action act = () => new TlsReader(new byte[] { 0x01, 0x02, 0x03 }).ReadUint32();
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void ReadUint64_Overrun_Throws()
    {
        Action act = () => new TlsReader(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }).ReadUint64();
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void ReadBytes_Overrun_Throws()
    {
        Action act = () => new TlsReader(new byte[] { 0x01 }).ReadBytes(2);
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void Remaining_ReturnsCorrectCount()
    {
        var reader = new TlsReader(new byte[] { 0x01, 0x02, 0x03 });
        reader.Remaining.Should().Be(3);
        reader.ReadUint8();
        reader.Remaining.Should().Be(2);
    }

    [Fact]
    public void IsEmpty_InitiallyFalse()
    {
        var reader = new TlsReader(new byte[] { 0x01 });
        reader.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Position_StartsAtZero()
    {
        var reader = new TlsReader(new byte[] { 0x01, 0x02 });
        reader.Position.Should().Be(0);
        reader.ReadUint8();
        reader.Position.Should().Be(1);
    }

    [Fact]
    public void MlsDecodeException_CanBeThrown()
    {
        Action act = () => throw new MlsDecodeException("test error");
        act.Should().Throw<MlsDecodeException>().WithMessage("test error");
    }

    [Fact]
    public void MlsDecodeException_WithInnerException()
    {
        var inner = new Exception("inner");
        Action act = () => throw new MlsDecodeException("outer", inner);
        act.Should().Throw<MlsDecodeException>().WithMessage("outer");
    }
}
