#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Messages;
using Xunit;

namespace PawSharp.Voice.Tests;

public class KeyPackageTests
{
    [Fact]
    public void Generate_ProducesValidKeyPackage()
    {
        var kp = KeyPackage.Generate(new byte[] { 0x01 });

        kp.Version.Should().Be(ProtocolVersion.Mls10);
        kp.Suite.Should().Be(CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256);
        kp.InitKey.Should().HaveCount(65);
        kp.Signature.Should().NotBeEmpty();
    }

    [Fact]
    public void Generate_EncodeDecode_RoundTrip()
    {
        var identity = new byte[] { 0xDE, 0xAD };
        var original = KeyPackage.Generate(identity);

        var encoded = original.Encode();
        var decoded = KeyPackage.Decode(encoded);

        decoded.InitKey.Should().BeEquivalentTo(original.InitKey);
        decoded.Leaf.EncryptionKey.Should().BeEquivalentTo(original.Leaf.EncryptionKey);
        decoded.Leaf.SignatureKey.Should().BeEquivalentTo(original.Leaf.SignatureKey);
    }

    [Fact]
    public void VerifySignature_ForGeneratedKeyPackage_ReturnsTrue()
    {
        var kp = KeyPackage.Generate(new byte[] { 0x01 });
        kp.VerifySignature().Should().BeTrue();
    }

    [Fact]
    public void Decode_InvalidVersion_Throws()
    {
        using var w = new PawSharp.Voice.DAVE.MLS.Encoding.TlsWriter(64);
        w.WriteUint16(0xFFFF); // invalid version

        Action act = () => KeyPackage.Decode(w.ToArray());
        act.Should().Throw<PawSharp.Voice.DAVE.MLS.Encoding.MlsDecodeException>();
    }

    [Fact]
    public void Decode_InvalidSuite_Throws()
    {
        using var w = new PawSharp.Voice.DAVE.MLS.Encoding.TlsWriter(64);
        w.WriteUint16(1); // version OK
        w.WriteUint16(0); // invalid suite

        Action act = () => KeyPackage.Decode(w.ToArray());
        act.Should().Throw<PawSharp.Voice.DAVE.MLS.Encoding.MlsDecodeException>();
    }

    [Fact]
    public void Generate_DifferentIdentities_ProduceDifferentKeys()
    {
        var kp1 = KeyPackage.Generate(new byte[] { 0x01 });
        var kp2 = KeyPackage.Generate(new byte[] { 0x02 });

        kp1.InitKey.Should().NotBeEquivalentTo(kp2.InitKey);
    }

    [Fact]
    public void Generate_StoresPrivateKeys()
    {
        var kp = KeyPackage.Generate(new byte[] { 0x01 });

        kp.InitPrivateKey.Should().HaveCount(32);
        kp.LeafHpkePrivateKey.Should().HaveCount(32);
        kp.LeafSignPrivateKey.Should().HaveCount(32);
    }
}
