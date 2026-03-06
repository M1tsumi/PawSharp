#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// DAVE frame encryption and decryption using AES-128-GCM.
///
/// Wire format for an encrypted DAVE frame:
/// <code>
///   [ nonce (12 bytes) ][ ciphertext ][ auth tag (16 bytes) ]
/// </code>
/// Nonce construction:
///   bytes 0–3   : sender SSRC (big-endian uint32)
///   bytes 4–11  : frame counter (little-endian uint64)
///
/// Key lifecycle:
///   The caller owns the key byte array. Call <see cref="CryptographicOperations.ZeroMemory"/>
///   on it when it is no longer needed so the material is wiped from memory before GC.
///
/// Replay protection:
///   Callers must ensure <paramref name="frameCounter"/> is monotonically increasing
///   per (key, SSRC) pair. The library does not maintain counter state.
/// </summary>
public static class DAVEEncryption
{
    private const int NonceSize = 12;    // GCM standard
    private const int TagSize   = 16;    // 128-bit auth tag

    /// <summary>
    /// Encrypts a voice frame with AES-128-GCM.
    /// </summary>
    /// <param name="plaintext">The raw voice frame payload (Opus-encoded audio).</param>
    /// <param name="key">
    ///   The 16-byte sender key (from <see cref="DAVEKeyDerivation.DeriveEncryptionKey"/>).
    ///   Zero the array with <see cref="CryptographicOperations.ZeroMemory"/> when done.
    /// </param>
    /// <param name="ssrc">The sender's SSRC, used in the nonce.</param>
    /// <param name="frameCounter">A monotonically increasing counter that prevents replay attacks.</param>
    /// <param name="additionalData">
    ///   Optional additional authenticated data (AAD), e.g. the RTP header.
    ///   Authenticated but not encrypted.
    /// </param>
    /// <returns>nonce + ciphertext + tag.</returns>
    public static byte[] EncryptFrame(
        byte[] plaintext,
        byte[] key,
        uint ssrc,
        ulong frameCounter,
        byte[]? additionalData = null)
    {
        ValidateKey(key);

        Span<byte> nonce = stackalloc byte[NonceSize];
        BuildNonce(nonce, ssrc, frameCounter);

        try
        {
            using var aes = new AesGcm(key, TagSize);

            var ciphertext = new byte[plaintext.Length];
            var tag        = new byte[TagSize];
            aes.Encrypt(nonce, plaintext, ciphertext, tag, additionalData);

            // Output: nonce || ciphertext || tag
            var output = new byte[NonceSize + ciphertext.Length + TagSize];
            nonce.CopyTo(output);
            ciphertext.CopyTo(output.AsSpan(NonceSize));
            tag.CopyTo(output.AsSpan(NonceSize + ciphertext.Length));
            return output;
        }
        finally
        {
            // Wipe the stack-allocated nonce before the frame leaves scope
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    /// <summary>
    /// Decrypts a voice frame with AES-128-GCM.
    /// Throws <see cref="CryptographicException"/> when the auth tag does not match
    /// (tampered ciphertext, wrong key, replayed frame with wrong counter).
    /// Prefer <see cref="TryDecryptFrame"/> for non-fatal failure paths.
    /// </summary>
    /// <param name="encryptedFrame">nonce + ciphertext + tag (as produced by <see cref="EncryptFrame"/>).</param>
    /// <param name="key">The 16-byte sender key.</param>
    /// <param name="additionalData">The same AAD that was used during encryption.</param>
    /// <returns>Decrypted plaintext (Opus data).</returns>
    /// <exception cref="CryptographicException">Thrown when authentication fails.</exception>
    public static byte[] DecryptFrame(
        byte[] encryptedFrame,
        byte[] key,
        byte[]? additionalData = null)
    {
        ValidateKey(key);

        int minLength = NonceSize + TagSize;
        if (encryptedFrame.Length < minLength)
            throw new ArgumentException($"Encrypted frame is too short (minimum {minLength} bytes).", nameof(encryptedFrame));

        var nonce      = encryptedFrame.AsSpan(0, NonceSize);
        int cipherLen  = encryptedFrame.Length - NonceSize - TagSize;
        var ciphertext = encryptedFrame.AsSpan(NonceSize, cipherLen);
        var tag        = encryptedFrame.AsSpan(NonceSize + cipherLen, TagSize);

        using var aes = new AesGcm(key, TagSize);

        var plaintext = new byte[cipherLen];
        aes.Decrypt(nonce, ciphertext, tag, plaintext, additionalData);
        return plaintext;
    }

    /// <summary>
    /// Attempts to decrypt a voice frame with AES-128-GCM.
    /// Returns <see langword="false"/> and sets <paramref name="plaintext"/> to
    /// <see langword="null"/> when authentication fails instead of throwing.
    /// </summary>
    /// <param name="encryptedFrame">nonce + ciphertext + tag.</param>
    /// <param name="key">The 16-byte sender key.</param>
    /// <param name="plaintext">The decrypted Opus payload, or <see langword="null"/> on failure.</param>
    /// <param name="additionalData">The same AAD used during encryption.</param>
    /// <returns><see langword="true"/> on success; <see langword="false"/> when the frame is invalid or tampered.</returns>
    public static bool TryDecryptFrame(
        byte[] encryptedFrame,
        byte[] key,
        [NotNullWhen(true)] out byte[]? plaintext,
        byte[]? additionalData = null)
    {
        try
        {
            plaintext = DecryptFrame(encryptedFrame, key, additionalData);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            plaintext = null;
            return false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void BuildNonce(Span<byte> nonce, uint ssrc, ulong frameCounter)
    {
        // Bytes 0–3: SSRC big-endian
        nonce[0] = (byte)(ssrc >> 24);
        nonce[1] = (byte)(ssrc >> 16);
        nonce[2] = (byte)(ssrc >> 8);
        nonce[3] = (byte)ssrc;

        // Bytes 4–11: frame counter little-endian
        for (int i = 0; i < 8; i++)
            nonce[4 + i] = (byte)(frameCounter >> (8 * i));
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != 16)
            throw new ArgumentException("DAVE encryption key must be exactly 16 bytes (AES-128).", nameof(key));
    }
}

