#nullable enable
using System;
using System.Text;
using FluentAssertions;
using PawSharp.Gateway.Connection;
using Xunit;

namespace PawSharp.Gateway.Tests;

public class ZlibStreamCompressionTests
{
    [Fact]
    public void Constructor_IsNotEnabled()
    {
        var comp = new ZlibStreamCompression();
        comp.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Initialize_EnablesCompression()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        comp.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Initialize_CalledTwice_DoesNotThrow()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        comp.Initialize();
    }

    [Fact]
    public void DecompressChunk_WithoutInitialize_Throws()
    {
        var comp = new ZlibStreamCompression();
        var act = () => comp.DecompressChunk(new byte[] { 0x00 });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DecompressChunk_PartialChunk_ReturnsNull()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        var result = comp.DecompressChunk(new byte[] { 0x78, 0x9C });
        result.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        comp.Reset();
        comp.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Reset_AfterInitialize_AllowsReinitialize()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        comp.Reset();
        comp.Initialize();
        comp.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhenNotEnabled_DoesNotThrow()
    {
        var comp = new ZlibStreamCompression();
        var act = () => comp.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenEnabled_DoesNotThrow()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        var act = () => comp.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        var act = () =>
        {
            comp.Dispose();
            comp.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void CompressMessage_WithoutInitialize_Throws()
    {
        var comp = new ZlibStreamCompression();
        var act = () => comp.CompressMessage("test");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompressMessage_ReturnsCompressedData()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        var result = comp.CompressMessage("Hello, World!");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MultipleMessages_BufferIsProperlyCleared()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();

        var msg1 = comp.CompressMessage("First message");
        var msg2 = comp.CompressMessage("Second message");

        msg1.Should().NotBeEmpty();
        msg2.Should().NotBeEmpty();
    }

    [Fact]
    public void Initialize_ClearsBufferBeforeSettingUp()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();
        var compressed = comp.CompressMessage("test");
        comp.Reset();
        comp.Initialize();
        comp.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DecompressChunk_HandlesZlibSuffix_Example1()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();

        byte[] suffix = { 0x00, 0x00, 0xFF, 0xFF };
        var result = comp.DecompressChunk(suffix);
        result.Should().BeEmpty();
    }

    [Fact]
    public void DecompressChunk_HandlesZlibSuffix_Example2()
    {
        var comp = new ZlibStreamCompression();
        comp.Initialize();

        var data = Encoding.UTF8.GetBytes("Hello");
        var compressed = comp.CompressMessage("Hello");
        var result = comp.DecompressChunk(compressed);
        result.Should().Be("Hello");
    }
}
