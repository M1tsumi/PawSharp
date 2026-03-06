// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using Enc = System.Text.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// MLS RFC 9420 §7.4 — HKDF label functions.
///
/// All MLS-specific HKDF derivations prepend the string "MLS 1.0 " to the label
/// before calling HKDF-Expand, ensuring domain separation from other protocols.
///
/// The DAVE ciphersuite uses SHA-256 throughout (hash output length = 32 bytes).
/// </summary>
internal static class MlsHkdf
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Output length of SHA-256 in bytes.</summary>
    public const int HashLen = 32;

    /// <summary>MLS version label prefix, prepended to every HKDF label.</summary>
    private const string MlsPrefix = "MLS 1.0 ";

    // ── Core RFC 9420 functions ────────────────────────────────────────────────

    /// <summary>
    /// HKDF-Extract(salt, IKM) → PRK using SHA-256.
    /// RFC 5869 §2.2
    /// </summary>
    /// <param name="salt">Salt (may be zero-length, treated as HashLen zero bytes per RFC 5869).</param>
    /// <param name="ikm">Input keying material.</param>
    /// <returns>32-byte pseudo-random key.</returns>
    public static byte[] Extract(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> ikm)
    {
        var saltArr = salt.IsEmpty ? new byte[HashLen] : salt.ToArray();
        return HKDF.Extract(HashAlgorithmName.SHA256, ikm.ToArray(), saltArr);
    }

    /// <summary>
    /// HKDF-Expand(PRK, info, length) → OKM using SHA-256.
    /// RFC 5869 §2.3
    /// </summary>
    public static byte[] Expand(ReadOnlySpan<byte> prk, ReadOnlySpan<byte> info, int length)
    {
        var output = new byte[length];
        HKDF.Expand(HashAlgorithmName.SHA256, prk.ToArray(), output, info.ToArray());
        return output;
    }

    /// <summary>
    /// ExpandWithLabel(Secret, Label, Context, Length) → bytes
    ///
    /// RFC 9420 §7.4:
    ///   ExpandWithLabel(Secret, Label, Context, Length) =
    ///     HKDF-Expand(Secret, KDFLabel, Length)
    ///
    ///   struct {
    ///     uint16 length;
    ///     opaque label&lt;7..255&gt;;   // "MLS 1.0 " + Label
    ///     opaque context&lt;0..2^32-1&gt;;
    ///   } KDFLabel;
    /// </summary>
    /// <param name="secret">The base secret (PRK).</param>
    /// <param name="label">Label string (without the "MLS 1.0 " prefix).</param>
    /// <param name="context">Context bytes (can be empty).</param>
    /// <param name="length">Output length in bytes.</param>
    public static byte[] ExpandWithLabel(
        ReadOnlySpan<byte> secret,
        string label,
        ReadOnlySpan<byte> context,
        int length)
    {
        var fullLabel    = Enc.ASCII.GetBytes(MlsPrefix + label);
        var info         = BuildKdfLabel((ushort)length, fullLabel, context);
        return Expand(secret, info, length);
    }

    /// <summary>
    /// DeriveSecret(Secret, Label) → bytes of length HashLen.
    ///
    /// RFC 9420 §7.4: DeriveSecret(Secret, Label) = ExpandWithLabel(Secret, Label, "", HashLen)
    /// </summary>
    public static byte[] DeriveSecret(ReadOnlySpan<byte> secret, string label)
        => ExpandWithLabel(secret, label, ReadOnlySpan<byte>.Empty, HashLen);

    // ── Key derivation shortcuts ───────────────────────────────────────────────

    /// <summary>
    /// Derives a secret of a specific length from a labeled context.
    /// Used when the length differs from HashLen (e.g., AES key material).
    /// </summary>
    public static byte[] ExpandWithLabelN(
        ReadOnlySpan<byte> secret,
        string label,
        ReadOnlySpan<byte> context,
        int n)
        => ExpandWithLabel(secret, label, context, n);

    /// <summary>
    /// SHA-256 hash of arbitrary data. Used for transcript hashes.
    /// </summary>
    public static byte[] Hash(ReadOnlySpan<byte> data)
        => SHA256.HashData(data);

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Encodes the KDFLabel structure per RFC 9420 §7.4 (TLS presentation language).
    ///
    /// struct {
    ///   uint16 length;
    ///   opaque label&lt;7..255&gt;;    // 1-byte length-prefix + label bytes
    ///   opaque context&lt;0..2^32-1&gt;; // 4-byte length-prefix + context bytes
    /// } KDFLabel;
    /// </summary>
    private static byte[] BuildKdfLabel(ushort length, ReadOnlySpan<byte> label, ReadOnlySpan<byte> context)
    {
        // Size: 2 (length) + 1 (label len) + label.Length + 4 (ctx len) + context.Length
        int totalSize = 2 + 1 + label.Length + 4 + context.Length;
        var buf       = new byte[totalSize];
        int pos       = 0;

        // uint16 length (big-endian)
        buf[pos++] = (byte)(length >> 8);
        buf[pos++] = (byte)length;

        // opaque label<7..255>  — single-byte length prefix
        buf[pos++] = (byte)label.Length;
        label.CopyTo(buf.AsSpan(pos));
        pos += label.Length;

        // opaque context<0..2^32-1> — 4-byte big-endian length prefix
        uint ctxLen = (uint)context.Length;
        buf[pos++] = (byte)(ctxLen >> 24);
        buf[pos++] = (byte)(ctxLen >> 16);
        buf[pos++] = (byte)(ctxLen >> 8);
        buf[pos++] = (byte)ctxLen;
        context.CopyTo(buf.AsSpan(pos));

        return buf;
    }
}
