#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

/// <summary>
/// Tests for <see cref="DAVEKeyDerivation"/> — HKDF-SHA256 sender key derivation.
/// </summary>
public class DAVEKeyDerivationTests
{
    private static readonly byte[] EpochSecret32 = new byte[32]
    {
        0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
        0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7,
        0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
    };

    // ── Key length ────────────────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_Returns_16Bytes()
    {
        var key = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0x00000001);

        key.Should().HaveCount(16, "AES-128-GCM requires a 128-bit key");
    }

    // ── Determinism ───────────────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_SameInputs_ProducesSameKey()
    {
        var key1 = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0xCAFEBABE);
        var key2 = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0xCAFEBABE);

        key1.Should().BeEquivalentTo(key2);
    }

    // ── SSRC separation ───────────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_DifferentSsrc_ProducesDifferentKeys()
    {
        var key1 = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0x00000001);
        var key2 = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0x00000002);

        key1.Should().NotBeEquivalentTo(key2,
            "each sender must have a unique key to prevent cross-sender decryption");
    }

    // ── Epoch separation ─────────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_DifferentEpochSecrets_ProduceDifferentKeys()
    {
        var secret1 = new byte[32]; // all zeros
        var secret2 = new byte[32]; // all zeros except last byte
        secret2[31] = 0x01;

        var key1 = DAVEKeyDerivation.DeriveEncryptionKey(secret1, ssrc: 0x01);
        var key2 = DAVEKeyDerivation.DeriveEncryptionKey(secret2, ssrc: 0x01);

        key1.Should().NotBeEquivalentTo(key2,
            "rotating the epoch secret must produce new sender keys");
    }

    // ── SSRC zero edge case ───────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_ZeroSsrc_ReturnsSomething()
    {
        var key = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0);

        key.Should().HaveCount(16);
        key.Should().NotBeEquivalentTo(new byte[16], "HKDF should produce non-zero output");
    }

    // ── ExtractEpochSecret ────────────────────────────────────────────────────

    [Fact]
    public void ExtractEpochSecret_Returns32Bytes()
    {
        var ikm = new byte[] { 0x01, 0x02, 0x03 };
        var prk = DAVEKeyDerivation.ExtractEpochSecret(ikm);

        prk.Should().HaveCount(32, "HKDF-SHA256 PRK is always 32 bytes");
    }

    [Fact]
    public void ExtractEpochSecret_IsDeterministic()
    {
        var ikm = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var prk1 = DAVEKeyDerivation.ExtractEpochSecret(ikm);
        var prk2 = DAVEKeyDerivation.ExtractEpochSecret(ikm);

        prk1.Should().BeEquivalentTo(prk2);
    }

    // ── Input validation ─────────────────────────────────────────────────────

    [Fact]
    public void DeriveEncryptionKey_NullSecret_ThrowsArgumentException()
    {
        Action act = () => DAVEKeyDerivation.DeriveEncryptionKey(null!, ssrc: 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeriveEncryptionKey_EmptySecret_ThrowsArgumentException()
    {
        Action act = () => DAVEKeyDerivation.DeriveEncryptionKey(Array.Empty<byte>(), ssrc: 0);
        act.Should().Throw<ArgumentException>();
    }

    // ── Derived key is usable for encryption ─────────────────────────────────

    [Fact]
    public void DerivedKey_CanEncryptAndDecryptFrame()
    {
        var key = DAVEKeyDerivation.DeriveEncryptionKey(EpochSecret32, ssrc: 0x1234);
        var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var encrypted = DAVEEncryption.EncryptFrame(plaintext, key, ssrc: 0x1234, frameCounter: 1);
        var decrypted = DAVEEncryption.DecryptFrame(encrypted, key, ssrc: 0x1234);

        decrypted.Should().BeEquivalentTo(plaintext);
    }
}
