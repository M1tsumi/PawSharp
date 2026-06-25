// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// DAVE frame encryption and decryption using AES-128-GCM.
///
/// Wire format:
///   [ 8-byte monotonic counter ][ ciphertext ][ 16-byte auth tag ]
///
/// The 8-byte counter is transmitted in-band so the receiver can reconstruct
/// the 12-byte GCM nonce without out-of-band tracking:
///   base_nonce = SHA-256(sender_key || I2OSP(ssrc, 4))[0..12]
///   nonce = base_nonce XOR I2OSP(counter, 12)
/// </summary>
public static class DAVEEncryption
{
    private const int CounterSize = 8;
    private const int NonceSize   = 12;
    private const int TagSize     = 16;

    /// <summary>
    /// Encrypts a voice frame with AES-128-GCM using the DAVE v1.1 format.
    /// </summary>
    /// <returns>[8-byte counter][ciphertext][16-byte tag].</returns>
    public static byte[] EncryptFrame(
        byte[] plaintext,
        byte[] key,
        uint ssrc,
        ulong frameCounter,
        byte[]? additionalData = null)
    {
        ValidateKey(key);

        var nonce = DeriveNonce(key, ssrc, frameCounter);

        using var aes = new AesGcm(key, TagSize);

        var ciphertext = new byte[plaintext.Length];
        var tag        = new byte[TagSize];
        aes.Encrypt(nonce, plaintext, ciphertext, tag, additionalData);

        var output = new byte[CounterSize + ciphertext.Length + TagSize];
        // Write counter big-endian (network byte order)
        for (int i = 0; i < CounterSize; i++)
            output[i] = (byte)(frameCounter >> (56 - 8 * i));
        ciphertext.CopyTo(output, CounterSize);
        tag.CopyTo(output, CounterSize + ciphertext.Length);

        CryptographicOperations.ZeroMemory(nonce);
        return output;
    }

    /// <summary>
    /// Decrypts a DAVE v1.1 frame. The 8-byte counter is read from the start of the frame.
    /// </summary>
    /// <returns>Decrypted plaintext.</returns>
    public static byte[] DecryptFrame(
        byte[] encryptedFrame,
        byte[] key,
        uint ssrc,
        byte[]? additionalData = null)
    {
        ValidateKey(key);

        int minLength = CounterSize + TagSize;
        if (encryptedFrame.Length < minLength)
            throw new ArgumentException($"Encrypted frame is too short (minimum {minLength} bytes).", nameof(encryptedFrame));

        // Read counter from first 8 bytes (big-endian)
        ulong frameCounter = 0;
        for (int i = 0; i < CounterSize; i++)
            frameCounter = (frameCounter << 8) | encryptedFrame[i];

        int cipherLen = encryptedFrame.Length - CounterSize - TagSize;
        var ciphertext = encryptedFrame.AsSpan(CounterSize, cipherLen);
        var tag = encryptedFrame.AsSpan(CounterSize + cipherLen, TagSize);

        var nonce = DeriveNonce(key, ssrc, frameCounter);

        using var aes = new AesGcm(key, TagSize);

        var plaintext = new byte[cipherLen];
        aes.Decrypt(nonce, ciphertext, tag, plaintext, additionalData);
        return plaintext;
    }

    public static bool TryDecryptFrame(
        byte[] encryptedFrame,
        byte[] key,
        uint ssrc,
        [NotNullWhen(true)] out byte[]? plaintext,
        byte[]? additionalData = null)
    {
        try
        {
            plaintext = DecryptFrame(encryptedFrame, key, ssrc, additionalData);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            plaintext = null;
            return false;
        }
    }

    private static byte[] DeriveNonce(byte[] key, uint ssrc, ulong frameCounter)
    {
        Span<byte> baseInput = stackalloc byte[key.Length + 4];
        key.CopyTo(baseInput);
        baseInput[key.Length]     = (byte)(ssrc >> 24);
        baseInput[key.Length + 1] = (byte)(ssrc >> 16);
        baseInput[key.Length + 2] = (byte)(ssrc >> 8);
        baseInput[key.Length + 3] = (byte)ssrc;

        var hash = SHA256.HashData(baseInput);

        var nonce = new byte[NonceSize];
        Array.Copy(hash, nonce, NonceSize);

        for (int i = 0; i < 8; i++)
            nonce[NonceSize - 1 - i] ^= (byte)(frameCounter >> (8 * i));

        return nonce;
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != 16)
            throw new ArgumentException("DAVE encryption key must be exactly 16 bytes (AES-128).", nameof(key));
    }
}
