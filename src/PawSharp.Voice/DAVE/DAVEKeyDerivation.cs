#nullable enable
using System;
using System.Security.Cryptography;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// DAVE (Discord's E2EE protocol) key derivation using HKDF-SHA256.
/// Per-sender encryption keys are derived from the epoch secret so that
/// each sender in an MLS group has a unique symmetrical key.
/// </summary>
public static class DAVEKeyDerivation
{
    // AES-128-GCM needs a 16-byte key.
    private const int KeyLengthBytes = 16;

    // Label used as HKDF info, matching Discord's DAVE spec:
    // "Discord DAVE 1.0 sender key\0" + 4-byte big-endian SSRC
    private static ReadOnlySpan<byte> InfoPrefix =>
        "Discord DAVE 1.0 sender key\0"u8;

    /// <summary>
    /// Derives a 16-byte AES-128 sender key from the current epoch secret and the sender's SSRC.
    /// </summary>
    /// <param name="epochSecret">The 32-byte epoch secret from the MLS group state.</param>
    /// <param name="ssrc">The sender's SSRC (synchronisation source) identifier.</param>
    /// <returns>A 16-byte key suitable for AES-128-GCM.</returns>
    public static byte[] DeriveEncryptionKey(byte[] epochSecret, uint ssrc)
    {
        if (epochSecret is null || epochSecret.Length == 0)
            throw new ArgumentException("Epoch secret must not be null or empty.", nameof(epochSecret));

        // Build info = InfoPrefix + SSRC as 4-byte big-endian
        Span<byte> info = stackalloc byte[InfoPrefix.Length + sizeof(uint)];
        InfoPrefix.CopyTo(info);
        info[InfoPrefix.Length]     = (byte)(ssrc >> 24);
        info[InfoPrefix.Length + 1] = (byte)(ssrc >> 16);
        info[InfoPrefix.Length + 2] = (byte)(ssrc >> 8);
        info[InfoPrefix.Length + 3] = (byte)ssrc;

        var key = new byte[KeyLengthBytes];
        HKDF.Expand(HashAlgorithmName.SHA256, epochSecret, key, info);
        return key;
    }

    /// <summary>
    /// Extracts a pseudo-random key from the input keying material using HKDF-Extract.
    /// This is used during epoch transitions (MLS commit processing).
    /// </summary>
    /// <param name="ikm">Input keying material (e.g. the MLS epoch exporter secret).</param>
    /// <param name="salt">Optional salt; pass <c>null</c> for the zero-length default.</param>
    /// <returns>A 32-byte extracted PRK.</returns>
    public static byte[] ExtractEpochSecret(byte[] ikm, byte[]? salt = null)
    {
        return HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
    }
}
