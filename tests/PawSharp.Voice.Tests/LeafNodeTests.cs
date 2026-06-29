#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Messages;
using Xunit;

namespace PawSharp.Voice.Tests;

public class LeafNodeTests
{
    [Fact]
    public void Generate_ProducesValidLeafNode()
    {
        var leaf = LeafNode.Generate(new byte[] { 0x01 }, out var hpkePriv, out var sigPriv);

        leaf.EncryptionKey.Should().HaveCount(65);
        leaf.SignatureKey.Should().HaveCount(65);
        leaf.Source.Should().Be(LeafNodeSource.KeyPackage);
        leaf.Signature.Should().NotBeEmpty();
        hpkePriv.Should().HaveCount(32);
        sigPriv.Should().HaveCount(32);
    }

    [Fact]
    public void Generate_EncodeDecode_RoundTrip()
    {
        var original = LeafNode.Generate(new byte[] { 0x01 }, out _, out _);

        var encoded = original.Encode();
        var decoded = LeafNode.Decode(encoded);

        decoded.EncryptionKey.Should().BeEquivalentTo(original.EncryptionKey);
        decoded.SignatureKey.Should().BeEquivalentTo(original.SignatureKey);
        decoded.Credential.Identity.Should().BeEquivalentTo(original.Credential.Identity);
        decoded.Source.Should().Be(original.Source);
    }

    [Fact]
    public void VerifySignature_ForGeneratedLeaf_ReturnsTrue()
    {
        var leaf = LeafNode.Generate(new byte[] { 0x01 }, out _, out _);
        leaf.VerifySignature().Should().BeTrue();
    }

    [Fact]
    public void Credential_IsBasicType()
    {
        var leaf = LeafNode.Generate(new byte[] { 0x01 }, out _, out _);
        leaf.Credential.Type.Should().Be(CredentialType.Basic);
    }

    [Fact]
    public void Generate_WithEmptyIdentity_Works()
    {
        var leaf = LeafNode.Generate(Array.Empty<byte>(), out _, out _);
        leaf.Should().NotBeNull();
    }
}
