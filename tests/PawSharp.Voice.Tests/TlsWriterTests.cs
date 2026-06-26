#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Encoding;
using Xunit;

namespace PawSharp.Voice.Tests;

public class TlsWriterTests
{
    [Fact]
    public void WriteUint8_WritesSingleByte()
    {
        using var w = new TlsWriter();
        w.WriteUint8(0xAB);
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0xAB });
    }

    [Fact]
    public void WriteUint16_WritesBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint16(0x0102);
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });
    }

    [Fact]
    public void WriteUint32_WritesBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint32(0x01020304);
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04 });
    }

    [Fact]
    public void WriteUint64_WritesBigEndian()
    {
        using var w = new TlsWriter();
        w.WriteUint64(0x0102030405060708);
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });
    }

    [Fact]
    public void WriteVector8_WritesLengthPrefixAndData()
    {
        using var w = new TlsWriter();
        w.WriteVector8(new byte[] { 0xAA, 0xBB });
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x02, 0xAA, 0xBB });
    }

    [Fact]
    public void WriteVector8_Over255_Throws()
    {
        using var w = new TlsWriter();
        var data = new byte[256];
        Action act = () => w.WriteVector8(data);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WriteVector16_Writes2ByteLengthAndData()
    {
        using var w = new TlsWriter();
        w.WriteVector16(new byte[] { 0xCC, 0xDD, 0xEE });
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x00, 0x03, 0xCC, 0xDD, 0xEE });
    }

    [Fact]
    public void WriteVector32_Writes4ByteLengthAndData()
    {
        using var w = new TlsWriter();
        w.WriteVector32(new byte[] { 0xFF });
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x00, 0x00, 0x00, 0x01, 0xFF });
    }

    [Fact]
    public void WriteBytes_WritesRawData()
    {
        using var w = new TlsWriter();
        w.WriteBytes(new byte[] { 0x01, 0x02, 0x03 });
        w.ToArray().Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void WriteNested16_WritesInnerAsVector16()
    {
        using var inner = new TlsWriter();
        inner.WriteUint16(0x0102);

        using var outer = new TlsWriter();
        outer.WriteNested16(inner);
        outer.ToArray().Should().BeEquivalentTo(new byte[] { 0x00, 0x02, 0x01, 0x02 });
    }

    [Fact]
    public void WriteNested32_WritesInnerAsVector32()
    {
        using var inner = new TlsWriter();
        inner.WriteUint8(0x42);

        using var outer = new TlsWriter();
        outer.WriteNested32(inner);
        var result = outer.ToArray();
        result.Should().HaveCount(5); // 4 length + 1 data
        result[4].Should().Be(0x42);
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        using var w = new TlsWriter();
        w.Length.Should().Be(0);
        w.WriteUint32(0);
        w.Length.Should().Be(4);
    }

    [Fact]
    public void ToArray_ReturnsCopy()
    {
        using var w = new TlsWriter();
        w.WriteUint8(0x01);
        var arr1 = w.ToArray();
        var arr2 = w.ToArray();
        arr1.Should().BeEquivalentTo(arr2);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var w = new TlsWriter();
        w.Dispose();
        Action act = () => w.Dispose();
        act.Should().NotThrow();
    }
}
