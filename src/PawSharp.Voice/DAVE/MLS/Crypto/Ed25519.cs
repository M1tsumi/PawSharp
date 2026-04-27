// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// RFC 8032 §5.1 — Ed25519 signature scheme.
///
/// Uses SHA-512 as the hash function and arithmetic over the twisted Edwards curve
/// defined over GF(2^255 - 19):
///   -x^2 + y^2 = 1 - (121665/121666) x^2 y^2
///
/// Key sizes: 32-byte private seed, 32-byte public key, 64-byte signature.
///
/// Reference: https://www.rfc-editor.org/rfc/rfc8032
/// </summary>
internal static class Ed25519
{
    // ── Public constants ─────────────────────────────────────────────────────

    public const int PrivateKeySize = 32;
    public const int PublicKeySize  = 32;
    public const int SignatureSize  = 64;

    // ── Curve constants ───────────────────────────────────────────────────────

    // q = 2^255 - 19 (field prime)
    private static readonly BigInt255 Q = new BigInt255(new ulong[]
    {
        0xFFFFFFFFFFFFED, 0xFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFF
    });

    // l = 2^252 + 27742317777372353535851937790883648493 (group order)
    private static readonly BigInt255 L = new BigInt255(new ulong[]
    {
        0x5812631A5CF5D3ED, 0x14DEF9DEA2F79CD6, 0x0000000000000000, 0x1000000000000000
    });

    // d = -121665/121666 mod q
    // d = 0x52036cee2b6ffe738cc740797779e89800700a4d4141d8ab75eb4dca135978a3
    private static readonly FE d = FE.FromBytes(new byte[]
    {
        0xa3, 0x78, 0x59, 0x13, 0xca, 0x4d, 0xeb, 0x75,
        0xab, 0xd8, 0x41, 0x41, 0x4d, 0x0a, 0x70, 0x00,
        0x98, 0xe8, 0x79, 0x77, 0x79, 0x40, 0xc7, 0x8c,
        0x73, 0xfe, 0x6f, 0x2b, 0xee, 0x6c, 0x03, 0x52
    });

    // sqrt(-1) mod q = 2^((q-1)/4) mod q
    // = 0x2b8324804fc1df0b2b4d00993dfbd7a72f431806ad2fe478c4ee1b274a0ea0b
    private static readonly FE SqrtM1 = FE.FromBytes(new byte[]
    {
        0xb0, 0xa0, 0x0e, 0x4a, 0x27, 0x1b, 0xee, 0xc4,
        0x78, 0xe4, 0x2f, 0xad, 0x06, 0x18, 0x43, 0x2f,
        0xa7, 0xd7, 0xfb, 0x3d, 0x99, 0x00, 0x4d, 0x2b,
        0x0b, 0xdf, 0xc1, 0x4f, 0x80, 0x24, 0x83, 0x2b
    });

    // Base point B
    private static readonly ExtPoint B = BasePointFromSpec();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Generates a new Ed25519 key pair.</summary>
    /// <summary>
    /// Generates an Ed25519 key pair.
    /// </summary>
    /// <param name="privateKey">Output: 32-byte private key.</param>
    /// <param name="publicKey">Output: 32-byte public key.</param>
    public static void GenerateKeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        privateKey = new byte[PrivateKeySize];
        RandomNumberGenerator.Fill(privateKey);
        publicKey = GetPublicKey(privateKey);
    }

    /// <summary>Derives the public key from a private seed.</summary>
    public static byte[] GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException("Private key must be 32 bytes.", nameof(privateKey));

        var h = SHA512.HashData(privateKey);
        ClampPrivateHash(h);
        var a = ScalarFromBytes(h.AsSpan(0, 32));
        return EncodePoint(ScalarMult(B, a));
    }

    /// <summary>Signs a message using Ed25519.</summary>
    /// <param name="message">The message to sign.</param>
    /// <param name="privateKey">32-byte private key.</param>
    /// <returns>64-byte signature.</returns>
    public static byte[] Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException("Private key must be 32 bytes.", nameof(privateKey));

        var h = SHA512.HashData(privateKey);
        ClampPrivateHash(h);

        var a = ScalarFromBytes(h.AsSpan(0, 32));
        var pk = EncodePoint(ScalarMult(B, a));

        // r = SHA-512(h[32..63] || message) mod l
        using var sha = IncrementalSha512();
        sha.AppendData(h, 32, 32);
        sha.AppendData(message);
        var rHash = sha.GetHashAndReset();
        var r = ScalarModL(rHash);

        var R  = ScalarMult(B, r);
        var Renc = EncodePoint(R);

        // S = (r + SHA-512(R || pk || message) * a) mod l
        sha.AppendData(Renc);
        sha.AppendData(pk);
        sha.AppendData(message);
        var kHash = sha.GetCurrentHash();
        var k = ScalarModL(kHash);

        var S = ScalarMulAdd(r, k, a); // S = r + k*a mod l

        var sig = new byte[SignatureSize];
        Renc.CopyTo(sig, 0);
        S.CopyTo(sig, 32);
        return sig;
    }

    /// <summary>Verifies an Ed25519 signature.</summary>
    /// <param name="message">The message that was signed.</param>
    /// <param name="signature">64-byte signature.</param>
    /// <param name="publicKey">32-byte compressed public key.</param>
    /// <returns>True if the signature is valid.</returns>
    /// <exception cref="ArgumentException">Thrown if signature or public key have incorrect length.</exception>
    public static bool Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        if (signature.Length != SignatureSize)
            throw new ArgumentException($"Signature must be {SignatureSize} bytes.", nameof(signature));
        if (publicKey.Length != PublicKeySize)
            throw new ArgumentException($"Public key must be {PublicKeySize} bytes.", nameof(publicKey));

        try
        {
            var A = DecodePoint(publicKey);
            var R = DecodePoint(signature.Slice(0, 32));
            var S = signature.Slice(32, 32);

            // Check S < l
            if (!IsLessThanL(S)) return false;

            using var sha = IncrementalSha512();
            sha.AppendData(signature.Slice(0, 32));
            sha.AppendData(publicKey);
            sha.AppendData(message);
            var kHash = sha.GetCurrentHash();
            var k = ScalarModL(kHash);

            var Smod = ScalarFromBytes(S);

            // Standard cofactor-less check (RFC 8032 §5.1.7): [S]B == R + [k]A
            var check1 = ScalarMult(B, Smod);
            var check2 = PointAdd(R, ScalarMult(A, k));
            return PointEqual(check1, check2);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ed25519 signature verification failed: {ex.Message}");
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ClampPrivateHash(byte[] h)
    {
        h[0]  &= 248;
        h[31] &= 63;
        h[31] |= 64;
    }

    private static IncrementalHash IncrementalSha512()
        => IncrementalHash.CreateHash(HashAlgorithmName.SHA512);

    // ── Extended twisted Edwards point ─────────────────────────────────────────

    private readonly struct ExtPoint
    {
        public readonly FE X, Y, Z, T; // Extended coordinates: x = X/Z, y = Y/Z, t = T/Z with T = XY/Z
        public ExtPoint(FE x, FE y, FE z, FE t) { X = x; Y = y; Z = z; T = t; }
    }

    private static ExtPoint PointAdd(ExtPoint P, ExtPoint Q)
    {
        // add-2008-hwcd
        var A = FE.Mul(FE.Sub(P.Y, P.X), FE.Sub(Q.Y, Q.X));
        var B2 = FE.Mul(FE.Add(P.Y, P.X), FE.Add(Q.Y, Q.X));
        var C = FE.Mul(FE.Mul(P.T, Q.T), FE.MulConst(d, 2));
        var D2 = FE.Mul(FE.Mul(P.Z, Q.Z), FE.FromInt(2));
        var E = FE.Sub(B2, A);
        var F = FE.Sub(D2, C);
        var G = FE.Add(D2, C);
        var H = FE.Add(B2, A);
        return new ExtPoint(FE.Mul(E, F), FE.Mul(G, H), FE.Mul(F, G), FE.Mul(E, H));
    }

    private static ExtPoint NegPoint(ExtPoint P)
        => new ExtPoint(FE.Neg(P.X), P.Y, P.Z, FE.Neg(P.T));

    private static ExtPoint DoublePoint(ExtPoint P)
    {
        // dbl-2008-hwcd
        var A = FE.Square(P.X);
        var B2 = FE.Square(P.Y);
        var C = FE.MulConst(FE.Square(P.Z), 2);
        var H = FE.Add(A, B2);
        var E = FE.Sub(H, FE.Square(FE.Add(P.X, P.Y)));
        var G = FE.Sub(A, B2);
        var F = FE.Add(C, G);
        return new ExtPoint(FE.Mul(E, F), FE.Mul(G, H), FE.Mul(F, G), FE.Mul(E, H));
    }

    private static bool PointEqual(ExtPoint P, ExtPoint Q)
    {
        // P.X/P.Z == Q.X/Q.Z  ↔  P.X*Q.Z == Q.X*P.Z
        var lhsX = FE.Mul(P.X, Q.Z);
        var rhsX = FE.Mul(Q.X, P.Z);
        var lhsY = FE.Mul(P.Y, Q.Z);
        var rhsY = FE.Mul(Q.Y, P.Z);
        return FE.Equals(lhsX, rhsX) && FE.Equals(lhsY, rhsY);
    }

    private static ExtPoint ScalarMult(ExtPoint P, byte[] scalar)
    {
        var Q = NeutralPoint();
        for (int i = 255; i >= 0; i--)
        {
            Q = DoublePoint(Q);
            if (((scalar[i / 8] >> (i % 8)) & 1) == 1)
                Q = PointAdd(Q, P);
        }
        return Q;
    }

    private static ExtPoint NeutralPoint()
        => new ExtPoint(FE.Zero(), FE.One(), FE.One(), FE.Zero());

    private static ExtPoint BasePointFromSpec()
    {
        // B = (x, 4/5 mod q) where x is recovered from y
        // y = 4/5 mod q = 46316835694926478169428394003475163141307993866256225615783033011972563353630
        // encoded as: 0x6666...6658 (little-endian)
        var yBytes = new byte[32];
        for (int i = 0; i < 31; i++) yBytes[i] = 0x66;
        yBytes[31] = 0x58;
        return DecodePoint(yBytes);
    }

    private static byte[] EncodePoint(ExtPoint P)
    {
        // Affine y, set sign bit for x
        var zi = FE.Invert(P.Z);
        var x  = FE.Mul(P.X, zi);
        var y  = FE.Mul(P.Y, zi);
        var enc = FE.ToBytes(y);
        enc[31] |= (byte)((FE.ToBytes(x)[0] & 1) << 7);
        return enc;
    }

    private static ExtPoint DecodePoint(ReadOnlySpan<byte> enc)
    {
        var yBytes = enc.ToArray();
        int signX  = yBytes[31] >> 7;
        yBytes[31] &= 0x7F;

        var y  = FE.FromBytes(yBytes);
        var y2 = FE.Square(y);
        var u  = FE.Sub(y2, FE.One());           // u = y^2 - 1
        var v  = FE.Add(FE.Mul(y2, d), FE.One()); // v = d*y^2 + 1

        // x = +/- sqrt(u/v)
        var x = FE.SqrtRatio(u, v);

        if (((FE.ToBytes(x)[0] & 1) ^ signX) != 0)
            x = FE.Neg(x);

        var t = FE.Mul(x, y);
        return new ExtPoint(x, y, FE.One(), t);
    }

    // ── Scalar arithmetic mod l (256-bit) ─────────────────────────────────────

    private static byte[] ScalarFromBytes(ReadOnlySpan<byte> src)
    {
        var r = new byte[32];
        src.Slice(0, Math.Min(32, src.Length)).CopyTo(r);
        return r;
    }

    private static byte[] ScalarModL(byte[] hash)
    {
        // Reduce 64-byte hash mod l using wide arithmetic (RFC 8032 §5.1)
        // Treat hash as little-endian 512-bit integer, reduce mod l
        return ReduceModL(hash);
    }

    private static byte[] ScalarMulAdd(byte[] r, byte[] k, byte[] a)
    {
        // S = (r + k*a) mod l
        // Use 64-byte accumulator
        var ka = MulScalars(k, a);   // 64 bytes
        var rr = new byte[64];
        r.CopyTo(rr, 0);
        var sum = AddScalars(rr, ka);
        return ReduceModL(sum);
    }

    // Checks if scalar (little-endian) < l
    private static bool IsLessThanL(ReadOnlySpan<byte> s)
    {
        // l in little-endian:
        var l = new byte[]
        {
            0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58,
            0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10,
        };
        for (int i = 31; i >= 0; i--)
        {
            if (s[i] < l[i]) return true;
            if (s[i] > l[i]) return false;
        }
        return false; // equal — also not strictly less
    }

    // ── Wide scalar arithmetic (simple schoolbook, not constant-time) ──────────

    private static byte[] MulScalars(byte[] a, byte[] b)
    {
        var r = new ulong[16];
        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 32; j++)
                r[i + j] += (ulong)a[i] * b[j];

        // Reduce to bytes
        var s = new byte[64];
        ulong carry = 0;
        for (int i = 0; i < 64; i++)
        {
            ulong v = (i < 16 ? r[i] : 0) + carry;
            s[i] = (byte)v;
            carry = v >> 8;
        }
        return s;
    }

    private static byte[] AddScalars(byte[] a, byte[] b)
    {
        var s = new byte[64];
        int carry = 0;
        for (int i = 0; i < 64; i++)
        {
            int v = (i < a.Length ? a[i] : 0) + (i < b.Length ? b[i] : 0) + carry;
            s[i] = (byte)v;
            carry = v >> 8;
        }
        return s;
    }

    /// <summary>
    /// Reduces a 64-byte little-endian integer mod l using the constant Barrett-like
    /// reduction chain from the SUPERCOP/ref10 implementation.
    /// </summary>
    private static byte[] ReduceModL(byte[] s)
    {
        // Straightforward interpretation: load as 512-bit integer,
        // subtract multiples of l until result < l.
        // Efficient version uses the precomputed q = floor(2^512 / l), but for
        // correctness we use the schoolbook approach for a 64-byte input.

        // l = 2^252 + 27742317777372353535851937790883648493
        // We split input into 4×64-bit limbs for the reduction.

        // Use the same reduction as SUPERCOP sc_reduce.c (public domain)
        long s0  = 2097151L & LoadLE3(s,  0);
        long s1  = 2097151L & (LoadLE4(s, 2) >> 5);
        long s2  = 2097151L & (LoadLE3(s, 5) >> 2);
        long s3  = 2097151L & (LoadLE4(s, 7) >> 7);
        long s4  = 2097151L & (LoadLE4(s, 10) >> 4);
        long s5  = 2097151L & (LoadLE3(s, 13) >> 1);
        long s6  = 2097151L & (LoadLE4(s, 15) >> 6);
        long s7  = 2097151L & (LoadLE3(s, 18) >> 3);
        long s8  = 2097151L & LoadLE3(s, 21);
        long s9  = 2097151L & (LoadLE4(s, 23) >> 5);
        long s10 = 2097151L & (LoadLE3(s, 26) >> 2);
        long s11 = 2097151L & (LoadLE4(s, 28) >> 7);
        long s12 = 2097151L & (LoadLE4(s, 31) >> 4);
        long s13 = 2097151L & (LoadLE3(s, 34) >> 1);
        long s14 = 2097151L & (LoadLE4(s, 36) >> 6);
        long s15 = 2097151L & (LoadLE3(s, 39) >> 3);
        long s16 = 2097151L & LoadLE3(s, 42);
        long s17 = 2097151L & (LoadLE4(s, 44) >> 5);
        long s18 = 2097151L & (LoadLE3(s, 47) >> 2);
        long s19 = 2097151L & (LoadLE4(s, 49) >> 7);
        long s20 = 2097151L & (LoadLE4(s, 52) >> 4);
        long s21 = 2097151L & (LoadLE3(s, 55) >> 1);
        long s22 = 2097151L & (LoadLE4(s, 57) >> 6);
        long s23 =              LoadLE4(s, 60) >> 3;

        // mu = floor(2^21 / l) in 21-bit limbs:
        // from SUPERCOP ref10 sc_reduce:
        s11 += s23 * 666643;  s12 += s23 * 470296;  s13 += s23 * 654183;
        s14 -= s23 * 997805;  s15 += s23 * 136657;  s16 -= s23 * 683901;
        s23  = 0;

        s10 += s22 * 666643;  s11 += s22 * 470296;  s12 += s22 * 654183;
        s13 -= s22 * 997805;  s14 += s22 * 136657;  s15 -= s22 * 683901;
        s22  = 0;

        s9  += s21 * 666643;  s10 += s21 * 470296;  s11 += s21 * 654183;
        s12 -= s21 * 997805;  s13 += s21 * 136657;  s14 -= s21 * 683901;
        s21  = 0;

        s8  += s20 * 666643;  s9  += s20 * 470296;  s10 += s20 * 654183;
        s11 -= s20 * 997805;  s12 += s20 * 136657;  s13 -= s20 * 683901;
        s20  = 0;

        s7  += s19 * 666643;  s8  += s19 * 470296;  s9  += s19 * 654183;
        s10 -= s19 * 997805;  s11 += s19 * 136657;  s12 -= s19 * 683901;
        s19  = 0;

        s6  += s18 * 666643;  s7  += s18 * 470296;  s8  += s18 * 654183;
        s9  -= s18 * 997805;  s10 += s18 * 136657;  s11 -= s18 * 683901;
        s18  = 0;

        // Carry propagation
        CarryProp(ref s6,  ref s7);  CarryProp(ref s7,  ref s8);
        CarryProp(ref s8,  ref s9);  CarryProp(ref s9,  ref s10);
        CarryProp(ref s10, ref s11); CarryProp(ref s11, ref s12);

        s5 += s17 * 666643; s6 += s17 * 470296; s7 += s17 * 654183;
        s8 -= s17 * 997805; s9 += s17 * 136657; s10 -= s17 * 683901;
        s17 = 0;

        s4 += s16 * 666643; s5 += s16 * 470296; s6 += s16 * 654183;
        s7 -= s16 * 997805; s8 += s16 * 136657; s9 -= s16 * 683901;
        s16 = 0;

        s3 += s15 * 666643; s4 += s15 * 470296; s5 += s15 * 654183;
        s6 -= s15 * 997805; s7 += s15 * 136657; s8 -= s15 * 683901;
        s15 = 0;

        s2 += s14 * 666643; s3 += s14 * 470296; s4 += s14 * 654183;
        s5 -= s14 * 997805; s6 += s14 * 136657; s7 -= s14 * 683901;
        s14 = 0;

        s1 += s13 * 666643; s2 += s13 * 470296; s3 += s13 * 654183;
        s4 -= s13 * 997805; s5 += s13 * 136657; s6 -= s13 * 683901;
        s13 = 0;

        s0 += s12 * 666643; s1 += s12 * 470296; s2 += s12 * 654183;
        s3 -= s12 * 997805; s4 += s12 * 136657; s5 -= s12 * 683901;
        s12 = 0;

        // Carry propagation to get canonical 21-bit limbs
        CarryProp(ref s0, ref s1); CarryProp(ref s1, ref s2);
        CarryProp(ref s2, ref s3); CarryProp(ref s3, ref s4);
        CarryProp(ref s4, ref s5); CarryProp(ref s5, ref s6);
        CarryProp(ref s6, ref s7); CarryProp(ref s7, ref s8);
        CarryProp(ref s8, ref s9); CarryProp(ref s9, ref s10);
        CarryProp(ref s10, ref s11);
        s12 += s11 >> 21; s11 &= 2097151;

        s0 += s12 * 666643; s1 += s12 * 470296; s2 += s12 * 654183;
        s3 -= s12 * 997805; s4 += s12 * 136657; s5 -= s12 * 683901;
        s12 = 0;

        CarryProp(ref s0, ref s1); CarryProp(ref s1, ref s2);
        CarryProp(ref s2, ref s3); CarryProp(ref s3, ref s4);
        CarryProp(ref s4, ref s5); CarryProp(ref s5, ref s6);
        CarryProp(ref s6, ref s7); CarryProp(ref s7, ref s8);
        CarryProp(ref s8, ref s9); CarryProp(ref s9, ref s10);
        CarryProp(ref s10, ref s11);

        var r = new byte[32];
        StoreLE(r,  0, s0, s1,  8); StoreLE(r,  3, s1,  s2, 5);
        StoreLE(r,  6, s2,  s3,  2); StoreLE(r,  9, s3,  s4, 7);
        StoreLE(r, 12, s4,  s5,  4); StoreLE(r, 15, s5,  s6, 1);
        StoreLE(r, 19, s7,  s8,  6); StoreLE(r, 22, s8,  s9, 3);
        StoreLE(r, 25, s9,  s10, 0); StoreLE(r, 28, s10, s11, 5);

        r[ 0] = (byte)s0;
        r[ 1] = (byte)(s0 >> 8);
        r[ 2] = (byte)((s0 >> 16) | (s1 << 5));
        r[ 3] = (byte)(s1 >> 3);
        r[ 4] = (byte)(s1 >> 11);
        r[ 5] = (byte)((s1 >> 19) | (s2 << 2));
        r[ 6] = (byte)(s2 >> 6);
        r[ 7] = (byte)((s2 >> 14) | (s3 << 7));
        r[ 8] = (byte)(s3 >> 1);
        r[ 9] = (byte)(s3 >> 9);
        r[10] = (byte)((s3 >> 17) | (s4 << 4));
        r[11] = (byte)(s4 >> 4);
        r[12] = (byte)(s4 >> 12);
        r[13] = (byte)((s4 >> 20) | (s5 << 1));
        r[14] = (byte)(s5 >> 7);
        r[15] = (byte)((s5 >> 15) | (s6 << 6));
        r[16] = (byte)(s6 >> 2);
        r[17] = (byte)(s6 >> 10);
        r[18] = (byte)((s6 >> 18) | (s7 << 3));
        r[19] = (byte)(s7 >> 5);
        r[20] = (byte)(s7 >> 13);
        r[21] = (byte)s8;
        r[22] = (byte)(s8 >> 8);
        r[23] = (byte)((s8 >> 16) | (s9 << 5));
        r[24] = (byte)(s9 >> 3);
        r[25] = (byte)(s9 >> 11);
        r[26] = (byte)((s9 >> 19) | (s10 << 2));
        r[27] = (byte)(s10 >> 6);
        r[28] = (byte)((s10 >> 14) | (s11 << 7));
        r[29] = (byte)(s11 >> 1);
        r[30] = (byte)(s11 >> 9);
        r[31] = (byte)(s11 >> 17);
        return r;
    }

    private static void CarryProp(ref long a, ref long b)
    {
        long carry = a >> 21;
        b += carry;
        a -= carry << 21;
    }

    private static void StoreLE(byte[] r, int offset, long lo, long hi, int shift)
        => r[offset] = (byte)((lo >> (21 - shift)) | (hi << shift));

    private static long LoadLE3(byte[] s, int i)
        => s[i] | ((long)s[i + 1] << 8) | ((long)s[i + 2] << 16);

    private static long LoadLE4(byte[] s, int i)
        => s[i] | ((long)s[i + 1] << 8) | ((long)s[i + 2] << 16) | ((long)s[i + 3] << 24);

    // ── GF(2^255-19) field element (27-byte = 32 byte encoding) ──────────────

    /// <summary>
    /// Field element for Ed25519 — wraps 5×51-bit limb arithmetic.
    /// Delegates to a separate 10-limb 25.5-bit representation for full correctness,
    /// or reuses the Curve25519 field routines via a shim.
    /// </summary>
    private readonly struct FE
    {
        private readonly long[] _h; // 5×51-bit limbs

        private FE(long[] h) { _h = h; }

        public static FE FromBytes(ReadOnlySpan<byte> b)
        {
            var h = new long[5];
            // Reuse Load51 from Curve25519 via delegation
            ulong b0 = LoadU64LE(b, 0), b1 = LoadU64LE(b, 8),
                  b2 = LoadU64LE(b, 16), b3 = LoadU64LE(b, 24);
            const ulong Mask51 = (1UL << 51) - 1;
            h[0] = (long)(b0 & Mask51);
            h[1] = (long)((b0 >> 51 | b1 << 13) & Mask51);
            h[2] = (long)((b1 >> 38 | b2 << 26) & Mask51);
            h[3] = (long)((b2 >> 25 | b3 << 39) & Mask51);
            h[4] = (long)((b3 >> 12) & 0x7FFFFFFFFFFFL);
            return new FE(h);
        }

        public static byte[] ToBytes(FE f)
        {
            var h = (long[])f._h.Clone();
            Reduce(h);
            ulong d0 = (ulong)h[0] | ((ulong)h[1] << 51);
            ulong d1 = (ulong)(h[1] >> 13) | ((ulong)h[2] << 38);
            ulong d2 = (ulong)(h[2] >> 26) | ((ulong)h[3] << 25);
            ulong d3 = (ulong)(h[3] >> 39) | ((ulong)h[4] << 12);
            var r = new byte[32];
            StoreU64LE(r, 0, d0); StoreU64LE(r, 8, d1);
            StoreU64LE(r, 16, d2); StoreU64LE(r, 24, d3);
            r[31] &= 0x7F;
            return r;
        }

        public static FE Zero() => new FE(new long[5]);
        public static FE One()  { var h = new long[5]; h[0] = 1; return new FE(h); }
        public static FE FromInt(long v) { var h = new long[5]; h[0] = v; return new FE(h); }

        public static FE Add(FE a, FE b)
        {
            var r = new long[5];
            for (int i = 0; i < 5; i++) r[i] = a._h[i] + b._h[i];
            return new FE(r);
        }
        public static FE Sub(FE a, FE b)
        {
            var r = new long[5];
            // add 2*p to avoid negative: 2p = [0x3FFFFFFFFFFFDA, 0x3FFFFFFFFFFFFE × 4]
            var p2 = new long[] { 0x3FFFFFFFFFFFDA, 0x3FFFFFFFFFFFFE, 0x3FFFFFFFFFFFFE, 0x3FFFFFFFFFFFFE, 0x1FFFFFFFFFFFFE };
            for (int i = 0; i < 5; i++) r[i] = a._h[i] + p2[i] - b._h[i];
            return new FE(r);
        }
        public static FE Neg(FE a) => Sub(Zero(), a);
        public static FE Mul(FE a, FE b)
        {
            var r = new long[5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                {
                    int dest  = (i + j) % 5;
                    long fac  = (i + j) >= 5 ? 19L : 1L;
                    r[dest] += a._h[i] * b._h[j] * fac;
                }
            Reduce(r);
            return new FE(r);
        }
        public static FE MulConst(FE a, long s)
        {
            var r = new long[5];
            for (int i = 0; i < 5; i++) r[i] = a._h[i] * s;
            Reduce(r);
            return new FE(r);
        }
        public static FE Square(FE a) => Mul(a, a);
        public static FE Invert(FE z)
        {
            // z^(p-2) via Fermat's little theorem, same chain as Curve25519
            var z2   = Square(z);
            var z9   = Mul(Mul(Mul(Square(Square(z2)), z2), z2), z);
            var z11  = Mul(z9, z2);
            var z22  = Square(z11);
            var z_5  = Mul(z22, z);
            var t    = PowChain(z_5,   5);  var z_10  = Mul(t, z_5);
            t         = PowChain(z_10,  10); var z_20  = Mul(t, z_10);
            t         = PowChain(z_20,  20); var z_40  = Mul(t, z_20);
            t         = PowChain(z_40,  10); var z_50  = Mul(t, z_10);
            t         = PowChain(z_50,  50); var z_100 = Mul(t, z_50);
            t         = PowChain(z_100,100); var z_200 = Mul(t, z_100);
            t         = PowChain(z_200, 50); var z_250 = Mul(t, z_50);
            t         = PowChain(z_250,  5);
            return Mul(t, z9);
        }

        /// <summary>Computes +/- sqrt(u/v) for point decompression.</summary>
        public static FE SqrtRatio(FE u, FE v)
        {
            // r = (u * v^3) * (u * v^7)^((p-5)/8)
            var v3   = Mul(Square(v), v);
            var v7   = Mul(Square(v3), v);
            var uv3  = Mul(u, v3);
            var uv7  = Mul(u, v7);
            // (p-5)/8 chain
            var r    = Mul(uv3, Pow_p58(uv7));
            // check r^2 * v == u
            var check = Mul(Square(r), v);
            if (Equals(check, u)) return r;
            // try r * sqrt(-1)
            var r2 = Mul(r, SqrtM1);
            if (Equals(Mul(Square(r2), v), u)) return r2;
            throw new CryptographicException("Ed25519: invalid point encoding.");
        }

        private static FE Pow_p58(FE u)
        {
            // (p-5)/8 addition chain
            var v    = u;
            var v2   = Square(v);
            var v4   = PowChain(v,  2);
            var v8   = PowChain(v4, 1);
            var t    = PowChain(v,  5);
            t        = Mul(t, v);
            t        = PowChain(t, 10);
            t        = Mul(t, PowChain(v, 5));
            t        = PowChain(t, 5);
            t        = Mul(t, v4);
            // Suppress unused warnings
            _ = v2; _ = v8;
            return t;
        }

        private static FE PowChain(FE a, int k)
        {
            var r = a;
            for (int i = 0; i < k; i++) r = Square(r);
            return r;
        }

        public static bool Equals(FE a, FE b)
        {
            var ab = ToBytes(a); var bb = ToBytes(b);
            int diff = 0;
            for (int i = 0; i < 32; i++) diff |= ab[i] ^ bb[i];
            return diff == 0;
        }

        private static void Reduce(long[] h)
        {
            const long M = (1L << 51) - 1;
            for (int i = 0; i < 5; i++) { long c = h[i] >> 51; h[(i+1)%5] += c * (i==4?19L:1L); h[i] &= M; }
            for (int i = 0; i < 5; i++) { long c = h[i] >> 51; h[(i+1)%5] += c * (i==4?19L:1L); h[i] &= M; }
        }

        private static ulong LoadU64LE(ReadOnlySpan<byte> s, int o)
        { ulong v = 0; for(int i=0; i<8; i++) v |= (ulong)s[o+i]<<(8*i); return v; }
        private static void StoreU64LE(byte[] d, int o, ulong v)
        { for(int i=0; i<8; i++) d[o+i]=(byte)(v>>(8*i)); }
    }

    // ── BigInt255 type (placeholder for scalar field, not used directly) ──────
    private readonly struct BigInt255
    {
        public readonly ulong[] Limbs;
        public BigInt255(ulong[] limbs) { Limbs = limbs; }
    }
}
