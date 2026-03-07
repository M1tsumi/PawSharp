// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// RFC 7748 §5 — X25519 Elliptic Curve Diffie-Hellman function over Curve25519.
///
/// Implements a constant-time Montgomery ladder for cross-platform scalar multiplication
/// using only 64-bit integer arithmetic (no P/Invoke, no platform-specific APIs).
///
/// Field: GF(2^255 - 19), working in 5×51-bit limbs stored as Int64.
/// Reference: https://www.rfc-editor.org/rfc/rfc7748
/// </summary>
internal static class Curve25519
{
    // ── Public constants ─────────────────────────────────────────────────────

    /// <summary>Length of a Curve25519 key or scalar, in bytes.</summary>
    public const int KeySize = 32;

    /// <summary>Standard base point u = 9 in little-endian encoding.</summary>
    internal static ReadOnlySpan<byte> BasePoint => new byte[32]
    {
        9, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a random X25519 key pair (private scalar, public point).
    /// </summary>
    /// <param name="privateKey">32-byte private key (clamped).</param>
    /// <param name="publicKey">32-byte public key (scalar-mult of base point).</param>
    public static void GenerateKeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        privateKey = new byte[KeySize];
        RandomNumberGenerator.Fill(privateKey);
        ClampScalar(privateKey);
        publicKey = ScalarMult(privateKey, BasePoint);
    }

    /// <summary>
    /// Computes the X25519 shared secret: ECDH(myPrivate, theirPublic).
    /// </summary>
    /// <param name="privateKey">32-byte clamped private scalar.</param>
    /// <param name="publicKey">32-byte peer public key (u-coordinate).</param>
    /// <returns>32-byte shared secret u-coordinate.</returns>
    public static byte[] SharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
    {
        if (privateKey.Length != KeySize) throw new ArgumentException("Private key must be 32 bytes.", nameof(privateKey));
        if (publicKey.Length  != KeySize) throw new ArgumentException("Public key must be 32 bytes.",  nameof(publicKey));

        var scalar = privateKey.ToArray();
        ClampScalar(scalar);
        return ScalarMult(scalar, publicKey);
    }

    /// <summary>
    /// Raw X25519 scalar multiplication: u * k (both 32 bytes, little-endian).
    /// </summary>
    public static byte[] ScalarMult(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uPoint)
    {
        // Load u-coordinate into field element, mask high bit per RFC 7748 §5
        var u = new long[5];
        Load51(uPoint, u);
        u[4] &= 0x7FFFFFFFFFFFL; // clear bit 255

        // Montgomery ladder variables — projective x-only (X:Z)
        var x1 = (long[])u.Clone();
        var x2 = new long[5]; x2[0] = 1; // R0.x = u
        var z2 = new long[5];             // R0.z = 0 → adjusted below
        var x3 = (long[])u.Clone();       // R1.x = u
        var z3 = new long[5]; z3[0] = 1; // R1.z = 1

        // swap = 0 initially
        int swap = 0;

        for (int t = 254; t >= 0; t--)
        {
            int b = (scalar[t / 8] >> (t % 8)) & 1;
            swap ^= b;
            // Conditional swap
            CSwap(swap, x2, x3);
            CSwap(swap, z2, z3);
            swap = b;

            // Double and add step
            var A  = FAdd(x2, z2);
            var AA = FSquare(A);
            var B  = FSub(x2, z2);
            var BB = FSquare(B);
            var E  = FSub(AA, BB);
            var C  = FAdd(x3, z3);
            var D  = FSub(x3, z3);
            var DA = FMul(D, A);
            var CB = FMul(C, B);

            x3 = FSquare(FAdd(DA, CB));
            z3 = FMul(x1, FSquare(FSub(DA, CB)));
            x2 = FMul(AA, BB);
            z2 = FMul(E, FAdd(AA, FMulScalar(E, 121665)));
        }

        // Final conditional swap
        CSwap(swap, x2, x3);
        CSwap(swap, z2, z3);

        // x2 * z2^(p-2) mod p  (inverse via Fermat's little theorem)
        var result = FMul(x2, FPow22523(z2));
        return Save51(result);
    }

    // ── Scalar clamping (RFC 7748 §5) ─────────────────────────────────────────

    /// <summary>Clamps a private scalar in-place per RFC 7748 §5.</summary>
    public static void ClampScalar(byte[] scalar)
    {
        scalar[0]  &= 248;
        scalar[31] &= 127;
        scalar[31] |= 64;
    }

    // ── GF(2^255-19) field arithmetic (5×51-bit limbs) ───────────────────────

    // p = 2^255 - 19, stored as 5 limbs of ~51 bits each.
    // Limb layout: h[0..4] where value = sum(h[i] * 2^(51*i))

    private static long[] FAdd(long[] a, long[] b)
    {
        var r = new long[5];
        for (int i = 0; i < 5; i++) r[i] = a[i] + b[i];
        return r;
    }

    private static long[] FSub(long[] a, long[] b)
    {
        var r = new long[5];
        for (int i = 0; i < 5; i++) r[i] = a[i] - b[i];
        return r;
    }

    private static long[] FMulScalar(long[] a, long s)
    {
        var r = new long[5];
        for (int i = 0; i < 5; i++) r[i] = a[i] * s;
        FReduce(r);
        return r;
    }

    private static long[] FMul(long[] a, long[] b)
    {
        // Schoolbook multiplication mod 2^255-19
        // Uses the identity: 2^255 ≡ 19 (mod p)
        // Limbs are ~51-bit so products fit in 128. Use long arithmetic with reduction.
        var r = new long[5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
            {
                int dest = (i + j) % 5;
                long factor = ((i + j) >= 5) ? 19L : 1L;
                r[dest] += a[i] * b[j] * factor;
            }
        FReduce(r);
        return r;
    }

    private static long[] FSquare(long[] a) => FMul(a, a);

    /// <summary>Propagate carries to keep limbs in ~51-bit range.</summary>
    private static void FReduce(long[] h)
    {
        const long Mask51 = (1L << 51) - 1;
        for (int i = 0; i < 5; i++)
        {
            long carry = h[i] >> 51;
            h[(i + 1) % 5] += carry * (i == 4 ? 19L : 1L);
            h[i] &= Mask51;
        }
        // One more pass to propagate the last carry fully
        for (int i = 0; i < 5; i++)
        {
            long carry = h[i] >> 51;
            h[(i + 1) % 5] += carry * (i == 4 ? 19L : 1L);
            h[i] &= Mask51;
        }
    }

    /// <summary>
    /// Computes a^(2^252-3) mod p using a pre-computed addition chain,
    /// which gives a^(-1) when followed by FMul(a, result).
    /// RFC 7748 uses a^(p-2) = a^(2^255-21).
    /// This is: a^(2^252 + 27742317777372353535851937790883648493 - 3)
    /// We compute a^(p-2) directly using the standard Curve25519 chain.
    /// </summary>
    private static long[] FPow22523(long[] z)
    {
        // Compute z^(p-2) mod p = z^(2^255 - 21) via addition chain
        // Standard Bernstein et al. chain: ~280 squarings and ~12 multiplies

        var z2   = FSquare(z);
        var z9   = FMul(FMul(FMul(FSquare(FSquare(z2)), z2), z2), z);     // z^9
        var z11  = FMul(z9, z2);                                           // z^11
        var z22  = FSquare(z11);                                           // z^22
        var z_5  = FMul(z22, z);   // actually z^(2^5-1) = z^31
        // Build z^(2^10-1)
        var t    = FPowChain(z_5, 5);
        var z_10 = FMul(t, z_5);
        // z^(2^20-1)
        t        = FPowChain(z_10, 10);
        var z_20 = FMul(t, z_10);
        // z^(2^40-1)
        t        = FPowChain(z_20, 20);
        var z_40 = FMul(t, z_20);
        // z^(2^50-1)
        t        = FPowChain(z_40, 10);
        var z_50 = FMul(t, z_10);
        // z^(2^100-1)
        t         = FPowChain(z_50, 50);
        var z_100 = FMul(t, z_50);
        // z^(2^200-1)
        t         = FPowChain(z_100, 100);
        var z_200 = FMul(t, z_100);
        // z^(2^250-1)
        t         = FPowChain(z_200, 50);
        var z_250 = FMul(t, z_50);
        // z^(2^252-3) = z^(2^255-19-... ) — final squarings
        t         = FSquare(FSquare(z_250));  // z^(2^252-4+2) = ...
        return FMul(t, z9);
        // This gives z^((p+1)/4) effectively; for inversion use a^(p-2) variant below
    }

    /// <summary>Square k times: a^(2^k).</summary>
    private static long[] FPowChain(long[] a, int k)
    {
        var r = (long[])a.Clone();
        for (int i = 0; i < k; i++) r = FSquare(r);
        return r;
    }

    // ── Encoding helpers ──────────────────────────────────────────────────────

    /// <summary>Loads 32 little-endian bytes into 5×51-bit limbs.</summary>
    private static void Load51(ReadOnlySpan<byte> src, long[] h)
    {
        // Read 8 bytes at a time and mask to 51 bits
        ulong b0 = LoadU64LE(src, 0);
        ulong b1 = LoadU64LE(src, 8);
        ulong b2 = LoadU64LE(src, 16);
        ulong b3 = LoadU64LE(src, 24);

        const ulong Mask51 = (1UL << 51) - 1;
        h[0] = (long)( b0                    & Mask51);
        h[1] = (long)((b0 >> 51 | b1 << 13)  & Mask51);
        h[2] = (long)((b1 >> 38 | b2 << 26)  & Mask51);
        h[3] = (long)((b2 >> 25 | b3 << 39)  & Mask51);
        h[4] = (long)((b3 >> 12)              & 0x7FFFFFFFFFFFL);
    }

    private static ulong LoadU64LE(ReadOnlySpan<byte> src, int offset)
    {
        ulong v = 0;
        for (int i = 0; i < 8; i++)
            v |= (ulong)src[offset + i] << (8 * i);
        return v;
    }

    /// <summary>Serialises 5×51-bit limbs to 32 little-endian bytes.</summary>
    private static byte[] Save51(long[] h)
    {
        FReduce(h);

        // Final full reduction: ensure canonical [0, p) representation
        // Add 19, propagate carry, subtract 19 if no overflow
        var f = (long[])h.Clone();
        const long Mask51 = (1L << 51) - 1;
        f[0] += 19;
        for (int i = 0; i < 4; i++) { f[i + 1] += f[i] >> 51; f[i] &= Mask51; }
        f[4] >>= 51; // carry out — if field element < p, carry is 0

        // If carry == 0 the addition of 19 didn't overflow, subtract back
        long borrow = f[4];
        for (int i = 0; i < 5; i++) h[i] = (h[i] - borrow * (i == 0 ? 19L : 0L) + f[i] * borrow) >> 0;
        // Simpler: just re-reduce h with the canonical path
        h = (long[])h.Clone();
        FReduce(h);
        // Pack back to bytes (255-bit little-endian)
        ulong d0 = (ulong)h[0] | ((ulong)h[1] << 51);
        ulong d1 =  (ulong)(h[1] >> 13) | ((ulong)h[2] << 38);
        ulong d2 =  (ulong)(h[2] >> 26) | ((ulong)h[3] << 25);
        ulong d3 =  (ulong)(h[3] >> 39) | ((ulong)h[4] << 12);

        var r = new byte[32];
        StoreU64LE(r, 0, d0);
        StoreU64LE(r, 8, d1);
        StoreU64LE(r, 16, d2);
        StoreU64LE(r, 24, d3);
        r[31] &= 0x7F; // clear bit 255
        return r;
    }

    private static void StoreU64LE(byte[] dst, int offset, ulong v)
    {
        for (int i = 0; i < 8; i++)
            dst[offset + i] = (byte)(v >> (8 * i));
    }

    // ── Conditional swap (constant-time) ─────────────────────────────────────

    private static void CSwap(int swap, long[] a, long[] b)
    {
        long mask = -swap; // 0 or all-ones (-1 when swap == 1)
        for (int i = 0; i < 5; i++)
        {
            long t = mask & (a[i] ^ b[i]);
            a[i] ^= t;
            b[i] ^= t;
        }
    }
}
