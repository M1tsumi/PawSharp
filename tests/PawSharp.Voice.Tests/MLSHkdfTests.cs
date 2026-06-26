#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Crypto;
using Xunit;

namespace PawSharp.Voice.Tests;

public class MlsHkdfTests
{
    [Fact]
    public void ExpandWithLabel_ProducesDerivedKey()
    {
        var secret = new byte[32];
        var output = MlsHkdf.ExpandWithLabel(secret, "test", new byte[] { 0x01, 0x02, 0x03 }, 32);
        output.Should().HaveCount(32);
    }

    [Fact]
    public void ExpandWithLabel_EmptyContext_Works()
    {
        var secret = new byte[32];
        var output = MlsHkdf.ExpandWithLabel(secret, "test", ReadOnlySpan<byte>.Empty, 16);
        output.Should().HaveCount(16);
    }

    [Fact]
    public void ExpandWithLabel_DifferentLabels_ProduceDifferentOutput()
    {
        var secret = new byte[32];
        var ctx = new byte[] { 0x01 };

        var out1 = MlsHkdf.ExpandWithLabel(secret, "label1", ctx, 32);
        var out2 = MlsHkdf.ExpandWithLabel(secret, "label2", ctx, 32);

        out1.Should().NotBeEquivalentTo(out2);
    }

    [Fact]
    public void ExpandWithLabel_DifferentContexts_ProduceDifferentOutput()
    {
        var secret = new byte[32];

        var out1 = MlsHkdf.ExpandWithLabel(secret, "test", new byte[] { 0x01 }, 32);
        var out2 = MlsHkdf.ExpandWithLabel(secret, "test", new byte[] { 0x02 }, 32);

        out1.Should().NotBeEquivalentTo(out2);
    }

    [Fact]
    public void ExpandWithLabel_ZeroLength_Works()
    {
        var secret = new byte[32];
        var output = MlsHkdf.ExpandWithLabel(secret, "test", new byte[] { 0x01 }, 0);
        output.Should().BeEmpty();
    }

    [Fact]
    public void DeriveSecret_ProducesDerivedKey()
    {
        var secret = new byte[32];
        var derived = MlsHkdf.DeriveSecret(secret, "test");
        derived.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveSecret_EmptyLabel_Works()
    {
        var secret = new byte[32];
        var derived = MlsHkdf.DeriveSecret(secret, "");
        derived.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveSecret_DifferentLabels_ProduceDifferentOutput()
    {
        var secret = new byte[32];
        var d1 = MlsHkdf.DeriveSecret(secret, "label1");
        var d2 = MlsHkdf.DeriveSecret(secret, "label2");
        d1.Should().NotBeEquivalentTo(d2);
    }

    [Fact]
    public void DeriveSecret_DifferentSecrets_ProduceDifferentOutput()
    {
        var secret1 = new byte[32];
        secret1[0] = 0x01;
        var secret2 = new byte[32];
        secret2[0] = 0x02;

        var d1 = MlsHkdf.DeriveSecret(secret1, "test");
        var d2 = MlsHkdf.DeriveSecret(secret2, "test");

        d1.Should().NotBeEquivalentTo(d2);
    }

    [Fact]
    public void Extract_Produces32Bytes()
    {
        var prk = MlsHkdf.Extract(new byte[32], new byte[] { 0x01 });
        prk.Should().HaveCount(32);
    }

    [Fact]
    public void Expand_ProducesCorrectLength()
    {
        var prk = new byte[32];
        var output = MlsHkdf.Expand(prk, new byte[] { 0x01 }, 48);
        output.Should().HaveCount(48);
    }

    [Fact]
    public void Hash_Produces32Bytes()
    {
        var hash = MlsHkdf.Hash(new byte[] { 0x01, 0x02 });
        hash.Should().HaveCount(32);
    }

    [Fact]
    public void Hash_Deterministic()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var h1 = MlsHkdf.Hash(data);
        var h2 = MlsHkdf.Hash(data);
        h1.Should().BeEquivalentTo(h2);
    }
}
