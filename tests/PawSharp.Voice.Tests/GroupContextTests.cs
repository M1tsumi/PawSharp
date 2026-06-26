#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Messages;
using Xunit;

namespace PawSharp.Voice.Tests;

public class GroupContextTests
{
    [Fact]
    public void EncodeDecode_RoundTrip()
    {
        var ctx = new GroupContext(
            new byte[] { 0x00, 0x01, 0x02, 0x03 },
            epoch: 1,
            new byte[32],
            new byte[32]);

        var encoded = ctx.Encode();
        var decoded = GroupContext.Decode(encoded);

        decoded.GroupId.Should().BeEquivalentTo(ctx.GroupId);
        decoded.Epoch.Should().Be(1);
        decoded.TreeHash.Should().BeEquivalentTo(ctx.TreeHash);
        decoded.ConfirmedTranscriptHash.Should().BeEquivalentTo(ctx.ConfirmedTranscriptHash);
    }

    [Fact]
    public void Encode_ZeroEpoch_EncodesCorrectly()
    {
        var ctx = new GroupContext(new byte[1], 0, new byte[32], new byte[32]);
        var encoded = ctx.Encode();

        var r = new TlsReader(encoded);
        r.ReadUint16(); // version
        r.ReadUint16(); // suite
        r.ReadVector32(); // group_id
        r.ReadUint64().Should().Be(0uL);
    }

    [Fact]
    public void Encode_LargeEpoch_EncodesCorrectly()
    {
        var ctx = new GroupContext(new byte[1], ulong.MaxValue, new byte[32], new byte[32]);
        var encoded = ctx.Encode();

        var r = new TlsReader(encoded);
        r.ReadUint16();
        r.ReadUint16();
        r.ReadVector32();
        r.ReadUint64().Should().Be(ulong.MaxValue);
    }

    [Fact]
    public void Decode_WithExtensions_ReadsOverExtensions()
    {
        var w = new TlsWriter(128);
        w.WriteUint16(1); // version
        w.WriteUint16(2); // suite
        w.WriteVector32(new byte[] { 0x00, 0x01 }); // group_id
        w.WriteUint64(5); // epoch
        w.WriteVector32(new byte[32]); // tree_hash
        w.WriteVector32(new byte[32]); // confirmed_transcript_hash
        w.WriteUint32(2); // extensions count
        w.WriteUint16(1);
        w.WriteVector32(new byte[] { 0x01 });
        w.WriteUint16(2);
        w.WriteVector32(new byte[] { 0x02, 0x03 });

        var decoded = GroupContext.Decode(w.ToArray());
        decoded.Epoch.Should().Be(5);
    }

    [Fact]
    public void VersionAndSuite_AreSet()
    {
        var ctx = new GroupContext(new byte[1], 0, new byte[32], new byte[32]);
        ctx.Version.Should().Be(ProtocolVersion.Mls10);
        ctx.Suite.Should().Be(CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256);
    }
}
