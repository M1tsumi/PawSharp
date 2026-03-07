#nullable enable
using System;
using System.Security.Cryptography;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

/// <summary>
/// Tests for <see cref="DAVEEncryption"/> — AES-128-GCM frame encrypt/decrypt.
/// </summary>
public class DAVEEncryptionTests
{
    // A fixed 16-byte test key (never use hardcoded keys in production)
    private static readonly byte[] TestKey = new byte[16]
        { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
          0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };

    private const uint TestSsrc    = 0xDEAD_BEEF;
    private const ulong FrameCount = 42;

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void EncryptDecrypt_RoundTrip_RecoversSamePlaintext()
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, DAVE E2EE!");

        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount);
        var decrypted = DAVEEncryption.DecryptFrame(encrypted, TestKey);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_WithAad_RoundTrip_RecoversSamePlaintext()
    {
        var plaintext = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        var aad       = new byte[] { 0x80, 0x00, 0x00, 0x01 }; // fake RTP header

        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount, aad);
        var decrypted = DAVEEncryption.DecryptFrame(encrypted, TestKey, aad);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void Encrypt_EmptyPlaintext_ProducesNoncePlusTag()
    {
        var encrypted = DAVEEncryption.EncryptFrame(Array.Empty<byte>(), TestKey, TestSsrc, FrameCount);

        // nonce(12) + ciphertext(0) + tag(16) = 28 bytes
        encrypted.Length.Should().Be(28);
    }

    // ── Output structure ──────────────────────────────────────────────────────

    [Fact]
    public void EncryptedFrame_ContainsExpectedNonce()
    {
        var plaintext = new byte[] { 0x00 };
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount);

        // Nonce bytes 0–3 = SSRC big-endian (unchecked so the compiler doesn't
        // reject truncating casts from the const uint 0xDEAD_BEEF)
        uint ssrc = TestSsrc;
        var ssrcBytes = new byte[]
        {
            (byte)(ssrc >> 24), (byte)(ssrc >> 16),
            (byte)(ssrc >> 8),  (byte)ssrc
        };
        encrypted[0..4].Should().BeEquivalentTo(ssrcBytes);

        // Nonce bytes 4–11 = frame counter little-endian
        ulong ctr = FrameCount;
        var counterBytes = new byte[8];
        for (int i = 0; i < 8; i++)
            counterBytes[i] = (byte)(ctr >> (8 * i));
        encrypted[4..12].Should().BeEquivalentTo(counterBytes);
    }

    [Fact]
    public void EncryptedFrameLength_Is_NonceSize_Plus_PlaintextSize_Plus_TagSize()
    {
        var plaintext = new byte[100];
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount);

        // 12 (nonce) + 100 (ciphertext) + 16 (tag) = 128
        encrypted.Length.Should().Be(128);
    }

    // ── Different counters produce different ciphertext ───────────────────────

    [Fact]
    public void EncryptSamePlaintextWithDifferentCounters_ProducesDifferentCiphertext()
    {
        var plaintext = new byte[] { 0x01, 0x02, 0x03 };

        var enc1 = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, frameCounter: 1);
        var enc2 = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, frameCounter: 2);

        enc1.Should().NotBeEquivalentTo(enc2);
    }

    // ── Authentication failures ───────────────────────────────────────────────

    [Fact]
    public void DecryptWithTamperedCiphertext_ThrowsCryptographicException()
    {
        var plaintext = new byte[] { 0xFF, 0xFE, 0xFD };
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount);

        // Flip a byte in the ciphertext (after the 12-byte nonce)
        encrypted[12] ^= 0xFF;

        Action act = () => DAVEEncryption.DecryptFrame(encrypted, TestKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void DecryptWithWrongKey_ThrowsCryptographicException()
    {
        var plaintext = new byte[] { 0x01 };
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount);

        var wrongKey = new byte[16];
        Action act = () => DAVEEncryption.DecryptFrame(encrypted, wrongKey);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void DecryptWithWrongAad_ThrowsCryptographicException()
    {
        var plaintext = new byte[] { 0x42 };
        var aad       = new byte[] { 0x01, 0x02 };
        var encrypted = DAVEEncryption.EncryptFrame(plaintext, TestKey, TestSsrc, FrameCount, aad);

        var wrongAad = new byte[] { 0xFF, 0xFF };
        Action act = () => DAVEEncryption.DecryptFrame(encrypted, TestKey, wrongAad);
        act.Should().Throw<CryptographicException>();
    }

    // ── Input validation ─────────────────────────────────────────────────────

    [Fact]
    public void EncryptWithNullKey_ThrowsArgumentException()
    {
        Action act = () => DAVEEncryption.EncryptFrame(new byte[] { 0x01 }, null!, 0, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EncryptWithWrongKeyLength_ThrowsArgumentException()
    {
        var badKey = new byte[32]; // AES-256 length — wrong for DAVE (needs AES-128)
        Action act = () => DAVEEncryption.EncryptFrame(new byte[] { 0x01 }, badKey, 0, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecryptFrameTooShort_ThrowsArgumentException()
    {
        // Less than nonce(12) + tag(16) = 28 bytes minimum
        var tooShort = new byte[10];
        Action act = () => DAVEEncryption.DecryptFrame(tooShort, TestKey);
        act.Should().Throw<ArgumentException>();
    }
}
