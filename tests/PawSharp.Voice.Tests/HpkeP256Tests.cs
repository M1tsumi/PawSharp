#nullable enable
using System;
using System.Security.Cryptography;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Crypto;
using Xunit;

namespace PawSharp.Voice.Tests;

public class HpkeP256Tests
{
    [Fact]
    public void SealBase_ProducesEncAndCiphertext()
    {
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv, out var pub);

        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var result = HpkeP256.SealBase(pub, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, plaintext, out var enc);

        enc.Should().NotBeNullOrEmpty();
        enc.Length.Should().Be(65);
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(plaintext.Length);
    }

    [Fact]
    public void OpenBase_RecoversOriginalPlaintext()
    {
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv, out var pub);

        var plaintext = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var ciphertext = HpkeP256.SealBase(pub, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, plaintext, out var enc);
        var decrypted = HpkeP256.OpenBase(priv, enc, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ciphertext);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void OpenBase_WithWrongKey_Throws()
    {
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv1, out var pub1);
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv2, out _);

        var plaintext = new byte[] { 0x01, 0x02 };
        var ciphertext = HpkeP256.SealBase(pub1, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, plaintext, out var enc);

        Action act = () => HpkeP256.OpenBase(priv2, enc, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ciphertext);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SealBase_WithInfoAndAad_ProducesDifferentCiphertext()
    {
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv, out var pub);

        ReadOnlySpan<byte> info = new byte[] { 0x01 };
        ReadOnlySpan<byte> aad = new byte[] { 0x02 };
        var plaintext = new byte[] { 0xFF };

        var ct1 = HpkeP256.SealBase(pub, info, aad, plaintext, out _);
        var ct2 = HpkeP256.SealBase(pub, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, plaintext, out _);

        ct1.Should().NotBeEquivalentTo(ct2);
    }

    [Fact]
    public void OpenBase_WithInfoAndAad_RecoversPlaintext()
    {
        CryptoProviderFactory.Instance.GenerateP256KeyPair(out var priv, out var pub);

        var info = new ReadOnlySpan<byte>(new byte[] { 0xAA, 0xBB });
        var aad = new ReadOnlySpan<byte>(new byte[] { 0xCC, 0xDD });
        var plaintext = new byte[] { 0x11, 0x22, 0x33 };

        var ct = HpkeP256.SealBase(pub, info, aad, plaintext, out var enc);
        var decrypted = HpkeP256.OpenBase(priv, enc, info, aad, ct);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void SealBase_InvalidPublicKey_Throws()
    {
        var badKey = new byte[10];

        Action act = () => HpkeP256.SealBase(badKey, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Array.Empty<byte>(), out _);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OpenBase_InvalidPrivateKey_Throws()
    {
        var badPriv = new byte[10];

        Action act = () => HpkeP256.OpenBase(badPriv, new byte[65], ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, Array.Empty<byte>());
        act.Should().Throw<ArgumentException>();
    }
}
