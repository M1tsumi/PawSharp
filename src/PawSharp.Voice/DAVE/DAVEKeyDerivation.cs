// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using PawSharp.Voice.DAVE.MLS.Crypto;

namespace PawSharp.Voice.DAVE;

/// <summary>
/// DAVE key derivation using the MLS-Exporter pattern (RFC 9420 §8.5).
///
/// Per-sender encryption keys are derived from the epoch secret using:
///   sender_key = ExpandWithLabel(epoch_secret, "Discord Secure Frames v0 sender", userID_context, 16)
///
/// where userID_context is the UTF-8-encoded Discord user ID of the sender.
/// This binds each sender key to both the epoch and the specific user identity.
/// </summary>
public static class DAVEKeyDerivation
{
    private const int KeyLengthBytes = 16;

    /// <summary>
    /// Derives a 16-byte AES-128 sender key from the current epoch secret and the sender's user ID.
    /// Uses the MLS-Exporter pattern per RFC 9420 §8.5 with domain separation per sender:
    ///   sender_key = ExpandWithLabel(epoch_secret, "Discord Secure Frames v0 sender", userID_context, 16)
    /// </summary>
    public static byte[] DeriveEncryptionKey(byte[] epochSecret, byte[] userId)
    {
        if (epochSecret is null || epochSecret.Length == 0)
            throw new ArgumentException("Epoch secret must not be null or empty.", nameof(epochSecret));
        if (userId is null || userId.Length == 0)
            throw new ArgumentException("User ID must not be null or empty.", nameof(userId));

        return MlsHkdf.ExpandWithLabel(epochSecret, "Discord Secure Frames v0 sender", userId, KeyLengthBytes);
    }

    /// <summary>
    /// Legacy overload for backward compatibility: derives from SSRC using MLS-Exporter.
    /// </summary>
    public static byte[] DeriveEncryptionKey(byte[] epochSecret, uint ssrc)
    {
        if (epochSecret is null || epochSecret.Length == 0)
            throw new ArgumentException("Epoch secret must not be null or empty.", nameof(epochSecret));

        Span<byte> info = stackalloc byte[4];
        info[0] = (byte)(ssrc >> 24);
        info[1] = (byte)(ssrc >> 16);
        info[2] = (byte)(ssrc >> 8);
        info[3] = (byte)ssrc;

        return MlsHkdf.ExpandWithLabel(epochSecret, "Discord Secure Frames v0 sender", info, KeyLengthBytes);
    }

    /// <summary>
    /// Extracts a pseudo-random key from the input keying material using HKDF-Extract.
    /// Used during epoch transitions (MLS commit processing).
    /// </summary>
    public static byte[] ExtractEpochSecret(byte[] ikm, byte[]? salt = null)
    {
        return HKDF.Extract(HashAlgorithmName.SHA256, ikm, salt);
    }
}
