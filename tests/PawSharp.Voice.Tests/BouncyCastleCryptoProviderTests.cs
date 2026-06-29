#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Crypto;
using Xunit;

namespace PawSharp.Voice.Tests;

public class BouncyCastleCryptoProviderTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        Action act = () => new BouncyCastleCryptoProvider();
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateP256KeyPair_ProducesP256Keys()
    {
        var prov = new BouncyCastleCryptoProvider();
        prov.GenerateP256KeyPair(out var priv, out var pub);

        pub.Should().HaveCount(65);
        pub[0].Should().Be(0x04);
        priv.Should().HaveCount(32);
    }

    [Fact]
    public void P256SharedSecret_WithSelf_ProducesConsistentSecret()
    {
        var prov = new BouncyCastleCryptoProvider();
        prov.GenerateP256KeyPair(out var priv, out var pub);

        var secret1 = prov.P256SharedSecret(priv, pub);
        var secret2 = prov.P256SharedSecret(priv, pub);

        secret1.Should().BeEquivalentTo(secret2);
    }

    [Fact]
    public void HkdfExtractExpand_ConsistentResults()
    {
        var prov = new BouncyCastleCryptoProvider();
        var salt = new byte[32];
        var ikm = new byte[] { 0x01, 0x02, 0x03 };

        var prk1 = prov.HkdfExtract(salt, ikm);
        var prk2 = prov.HkdfExtract(salt, ikm);

        prk1.Should().BeEquivalentTo(prk2);
    }

    [Fact]
    public void EcdsaP256SignAndVerify_ConsistentResults()
    {
        var prov = new BouncyCastleCryptoProvider();
        prov.GenerateEcdsaP256KeyPair(out var priv, out var pub);
        var data = new byte[] { 0x01, 0x02, 0x03 };

        var sig1 = prov.EcdsaP256Sign(data, priv);
        var sig2 = prov.EcdsaP256Sign(data, priv);

        prov.EcdsaP256Verify(data, sig1, pub).Should().BeTrue();
        prov.EcdsaP256Verify(data, sig2, pub).Should().BeTrue();
    }

    [Fact]
    public void Aes128GcmEncryptDecrypt_LongPlaintext_DoesNotThrow()
    {
        var prov = new BouncyCastleCryptoProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var plaintext = new byte[4096];

        var ct = prov.Aes128GcmEncrypt(key, nonce, plaintext, Array.Empty<byte>());
        var ctLen = ct.Length - 16;
        var pt = prov.Aes128GcmDecrypt(key, nonce, ct.AsSpan(0, ctLen), ct.AsSpan(ctLen), Array.Empty<byte>());

        pt.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void Aes128GcmEncryptDecrypt_WithAad_Succeeds()
    {
        var prov = new BouncyCastleCryptoProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var aad = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var plaintext = new byte[] { 0x01, 0x02 };

        var ct = prov.Aes128GcmEncrypt(key, nonce, plaintext, aad);
        var ctLen = ct.Length - 16;
        var pt = prov.Aes128GcmDecrypt(key, nonce, ct.AsSpan(0, ctLen), ct.AsSpan(ctLen), aad);

        pt.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void Aes128GcmDecrypt_WithWrongAad_Fails()
    {
        var prov = new BouncyCastleCryptoProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var aad = new byte[] { 0x01 };
        var wrongAad = new byte[] { 0x02 };
        var ct = prov.Aes128GcmEncrypt(key, nonce, new byte[] { 0x01 }, aad);
        var ctLen = ct.Length - 16;

        Action act = () => prov.Aes128GcmDecrypt(key, nonce, ct.AsSpan(0, ctLen), ct.AsSpan(ctLen), wrongAad);
        act.Should().Throw<Exception>();
    }
}
