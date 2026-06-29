#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

public class DAVEEncryptionTests
{
    [Fact]
    public void EncryptFrameDecryptFrame_RoundTrip()
    {
        var key = new byte[16];
        var plaintext = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        var encrypted = DAVEEncryption.EncryptFrame(plaintext, key, 1u, 0u);
        encrypted.Should().HaveCountGreaterThan(0);

        var decrypted = DAVEEncryption.DecryptFrame(encrypted, key, 1u);
        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void EncryptFrame_EmptyPlaintext()
    {
        var key = new byte[16];
        var encrypted = DAVEEncryption.EncryptFrame(Array.Empty<byte>(), key, 1u, 0u);
        var decrypted = DAVEEncryption.DecryptFrame(encrypted, key, 1u);

        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void DecryptFrame_WrongSSRC_Fails()
    {
        var key = new byte[16];
        var plaintext = new byte[] { 0x01, 0x02, 0x03 };

        var encrypted = DAVEEncryption.EncryptFrame(plaintext, key, 1u, 0u);
        Action act = () => DAVEEncryption.DecryptFrame(encrypted, key, 2u);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void EncryptFrame_DifferentKeys_ProducesDifferentCiphertext()
    {
        var key1 = new byte[16];
        key1[0] = 0x01;
        var key2 = new byte[16];
        key2[0] = 0x02;
        var plaintext = new byte[] { 0x01, 0x02, 0x03 };

        var ct1 = DAVEEncryption.EncryptFrame(plaintext, key1, 1u, 0u);
        var ct2 = DAVEEncryption.EncryptFrame(plaintext, key2, 1u, 0u);

        ct1.Should().NotBeEquivalentTo(ct2);
    }

    [Fact]
    public void EncryptFrame_LargePlaintext_DoesNotThrow()
    {
        var key = new byte[16];
        var plaintext = new byte[4096];

        Action act = () => DAVEEncryption.EncryptFrame(plaintext, key, 1u, 0u);
        act.Should().NotThrow();
    }

    [Fact]
    public void DecryptFrame_Corrupted_Fails()
    {
        var key = new byte[16];
        var encrypted = DAVEEncryption.EncryptFrame(new byte[] { 0x01 }, key, 1u, 0u);
        encrypted[8] ^= 0xFF;

        Action act = () => DAVEEncryption.DecryptFrame(encrypted, key, 1u);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void TryDecryptFrame_Valid_ReturnsTrue()
    {
        var key = new byte[16];
        var plaintext = new byte[] { 0x01, 0x02, 0x03 };
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, key, 1u, 0u);

        DAVEEncryption.TryDecryptFrame(encrypted, key, 1u, out var result).Should().BeTrue();
        result.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void TryDecryptFrame_Invalid_ReturnsFalse()
    {
        var key = new byte[16];
        var encrypted = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        DAVEEncryption.TryDecryptFrame(encrypted, key, 1u, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void EncryptFrame_NullKey_Throws()
    {
        Action act = () => DAVEEncryption.EncryptFrame(new byte[] { 0x01 }, null!, 1u, 0u);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecryptFrame_NullKey_Throws()
    {
        Action act = () => DAVEEncryption.DecryptFrame(new byte[] { 0x01 }, null!, 1u);
        act.Should().Throw<ArgumentException>();
    }
}
