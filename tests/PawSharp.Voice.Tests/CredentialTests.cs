#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Messages;
using Xunit;

namespace PawSharp.Voice.Tests;

public class CredentialTests
{
    [Fact]
    public void BasicCredential_EncodeDecode_RoundTrip()
    {
        var identity = new byte[] { 0x01, 0x02, 0x03 };
        var original = Credential.Basic(identity);

        var encoded = original.Encode();
        var decoded = Credential.Decode(encoded);

        decoded.Type.Should().Be(CredentialType.Basic);
        decoded.Identity.Should().BeEquivalentTo(identity);
        decoded.Certificates.Should().BeNull();
    }

    [Fact]
    public void BasicCredential_Encode_ContainsTypeAndIdentity()
    {
        var identity = new byte[] { 0xDE, 0xAD };
        var cred = Credential.Basic(identity);

        var encoded = cred.Encode();

        var r = new TlsReader(encoded);
        var type = r.ReadUint16();
        type.Should().Be((ushort)CredentialType.Basic);

        var decodedIdentity = r.ReadVector16();
        decodedIdentity.Should().BeEquivalentTo(identity);
    }

    [Fact]
    public void Decode_InvalidType_Throws()
    {
        using var w = new TlsWriter(4);
        w.WriteUint16(0xFFFF); // unknown type

        Action act = () => Credential.Decode(w.ToArray());
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void Decode_ShortBuffer_Throws()
    {
        Action act = () => Credential.Decode(new byte[] { 0x00 });
        act.Should().Throw<MlsDecodeException>();
    }

    [Fact]
    public void BasicCredential_EmptyIdentity_EncodesAndDecodes()
    {
        var original = Credential.Basic(Array.Empty<byte>());
        var encoded = original.Encode();
        var decoded = Credential.Decode(encoded);

        decoded.Identity.Should().BeEmpty();
    }

    [Fact]
    public void BasicCredential_IdentityProperty_IsCorrect()
    {
        var identity = System.Text.Encoding.UTF8.GetBytes("test-user");
        var cred = Credential.Basic(identity);

        cred.Identity.Should().BeSameAs(identity);
    }
}
