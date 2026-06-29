#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Crypto;
using Xunit;

namespace PawSharp.Voice.Tests;

public class ICryptoProviderTests
{
    private static ICryptoProvider GetProvider() => CryptoProviderFactory.Instance;

    [Fact]
    public void GenerateP256KeyPair_ProducesPublicAndPrivate()
    {
        var provider = GetProvider();
        provider.GenerateP256KeyPair(out var priv, out var pub);

        pub.Should().HaveCount(65);
        pub[0].Should().Be(0x04);
        priv.Should().HaveCount(32);
    }

    [Fact]
    public void P256SharedSecret_Produces32Bytes()
    {
        var provider = GetProvider();
        provider.GenerateP256KeyPair(out var privA, out var pubA);
        provider.GenerateP256KeyPair(out var privB, out var pubB);

        var secretA = provider.P256SharedSecret(privA, pubB);
        var secretB = provider.P256SharedSecret(privB, pubA);

        secretA.Should().HaveCount(32);
        secretA.Should().BeEquivalentTo(secretB);
    }

    [Fact]
    public void P256GetPublicKey_DerivesFromPrivate()
    {
        var provider = GetProvider();
        provider.GenerateP256KeyPair(out var priv, out var pub);

        var derived = provider.P256GetPublicKey(priv);
        derived.Should().BeEquivalentTo(pub);
    }

    [Fact]
    public void GenerateEcdsaP256KeyPair_ProducesKeys()
    {
        var provider = GetProvider();
        provider.GenerateEcdsaP256KeyPair(out var priv, out var pub);

        pub.Should().HaveCount(65);
        priv.Should().HaveCount(32);
    }

    [Fact]
    public void EcdsaP256SignAndVerify_RoundTrip()
    {
        var provider = GetProvider();
        provider.GenerateEcdsaP256KeyPair(out var priv, out var pub);
        var data = new byte[] { 0x01, 0x02, 0x03 };

        var signature = provider.EcdsaP256Sign(data, priv);
        signature.Length.Should().BeGreaterThan(0);

        provider.EcdsaP256Verify(data, signature, pub).Should().BeTrue();
    }

    [Fact]
    public void EcdsaP256Verify_WrongData_ReturnsFalse()
    {
        var provider = GetProvider();
        provider.GenerateEcdsaP256KeyPair(out var priv, out var pub);
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var wrong = new byte[] { 0x01, 0x02, 0x04 };

        var signature = provider.EcdsaP256Sign(data, priv);
        provider.EcdsaP256Verify(wrong, signature, pub).Should().BeFalse();
    }

    [Fact]
    public void HkdfExtractExpand_KnownInput()
    {
        var provider = GetProvider();
        var salt = new byte[32];
        var ikm = new byte[] { 0x01, 0x02, 0x03 };

        var prk = provider.HkdfExtract(salt, ikm);
        prk.Should().HaveCount(32);

        var output = provider.HkdfExpand(prk, new byte[] { 0x01 }, 32);
        output.Should().HaveCount(32);
    }

    [Fact]
    public void HkdfExtract_EmptySalt_Works()
    {
        var provider = GetProvider();
        var prk = provider.HkdfExtract(Array.Empty<byte>(), new byte[] { 0x01 });
        prk.Should().HaveCount(32);
    }

    [Fact]
    public void HkdfExpand_DifferentInfo_ProducesDifferentOutput()
    {
        var provider = GetProvider();
        var prk = new byte[32];
        var out1 = provider.HkdfExpand(prk, new byte[] { 0x01 }, 32);
        var out2 = provider.HkdfExpand(prk, new byte[] { 0x02 }, 32);

        out1.Should().NotBeEquivalentTo(out2);
    }

    [Fact]
    public void HkdfExpand_DifferentLengths()
    {
        var provider = GetProvider();
        var prk = new byte[32];
        var out16 = provider.HkdfExpand(prk, new byte[] { 0x01 }, 16);
        var out48 = provider.HkdfExpand(prk, new byte[] { 0x01 }, 48);

        out16.Should().HaveCount(16);
        out48.Should().HaveCount(48);
    }

    [Fact]
    public void Sha256Hash_Produces32Bytes()
    {
        var provider = GetProvider();
        var hash = provider.Sha256Hash(new byte[] { 0x01, 0x02, 0x03 });
        hash.Should().HaveCount(32);
    }

    [Fact]
    public void Aes128GcmEncryptDecrypt_RoundTrip()
    {
        var provider = GetProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var plaintext = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var aad = new byte[] { 0x01, 0x02 };

        var ciphertext = provider.Aes128GcmEncrypt(key, nonce, plaintext, aad);
        ciphertext.Should().HaveCountGreaterThan(plaintext.Length);

        var ctLen = ciphertext.Length - 16;
        var ct = ciphertext.AsSpan(0, ctLen);
        var tag = ciphertext.AsSpan(ctLen);
        var decrypted = provider.Aes128GcmDecrypt(key, nonce, ct, tag, aad);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void Aes128GcmDecrypt_WrongNonce_Fails()
    {
        var provider = GetProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var wrongNonce = new byte[12];
        wrongNonce[0] = 0xFF;
        var plaintext = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        var ciphertext = provider.Aes128GcmEncrypt(key, nonce, plaintext, Array.Empty<byte>());
        var ctLen = ciphertext.Length - 16;
        Action act = () => provider.Aes128GcmDecrypt(key, wrongNonce, ciphertext.AsSpan(0, ctLen), ciphertext.AsSpan(ctLen), Array.Empty<byte>());
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Aes128GcmEncryptDecrypt_LongPlaintext()
    {
        var provider = GetProvider();
        var key = new byte[16];
        var nonce = new byte[12];
        var plaintext = new byte[4096];

        var ct = provider.Aes128GcmEncrypt(key, nonce, plaintext, Array.Empty<byte>());
        var ctLen = ct.Length - 16;
        var pt = provider.Aes128GcmDecrypt(key, nonce, ct.AsSpan(0, ctLen), ct.AsSpan(ctLen), Array.Empty<byte>());

        pt.Should().BeEquivalentTo(plaintext);
    }
}
