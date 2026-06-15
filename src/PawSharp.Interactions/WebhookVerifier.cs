#nullable enable
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace PawSharp.Interactions;

/// <summary>
/// Verifies Discord interaction webhook signatures (X-Signature-Ed25519 /
/// X-Signature-Timestamp headers) per the Discord HTTP Interactions spec.
///
/// Discord signs each HTTP interaction request using Ed25519. The message that
/// is signed is the concatenation of the raw timestamp header value and the raw
/// request body bytes.  A request is valid only when:
///   1. The X-Signature-Timestamp is not older than 5 minutes (replay protection).
///   2. The Ed25519 signature covers <c>timestamp_bytes || body_bytes</c> and
///      verifies against the application's Ed25519 public key shown in the
///      Developer Portal.
///
/// Usage:
/// <code>
/// var verifier = new WebhookVerifier("your_application_public_key_hex");
/// bool valid = verifier.Verify(signatureHeader, timestampHeader, requestBodyBytes);
/// if (!valid) { return Results.Unauthorized(); }
/// </code>
///
/// The Ed25519 verification is implemented using <see cref="System.Numerics.BigInteger"/>
/// arithmetic for full portability across platforms.
/// <para>
/// <b>Security Note:</b> BigInteger operations are not constant-time, which may enable 
/// timing side-channel attacks in high-throughput or adversarial environments. 
/// For production deployments handling sensitive interactions, consider replacing 
/// the inner <see cref="VerifyEd25519"/> method with a constant-time implementation 
/// (e.g., NSec.Cryptography or BouncyCastle).
/// </para>
/// </summary>
public sealed class WebhookVerifier
{
    // Maximum age of a timestamp before the request is considered a replay.
    private static readonly TimeSpan MaxTimestampAge = TimeSpan.FromMinutes(5);

    private readonly byte[] _publicKey;

    /// <param name="publicKeyHex">
    /// The application's Ed25519 public key in lower-case hex (64 hex chars / 32 bytes).
    /// Found in the Discord Developer Portal → Application → General Information.
    /// </param>
    public WebhookVerifier(string publicKeyHex)
    {
        if (string.IsNullOrEmpty(publicKeyHex) || publicKeyHex.Length != 64)
            throw new ArgumentException("Ed25519 public key must be exactly 64 hex characters (32 bytes).", nameof(publicKeyHex));

        _publicKey = HexToBytes(publicKeyHex);
    }

    /// <summary>
    /// Verifies an incoming Discord interaction webhook request.
    /// </summary>
    /// <param name="signatureHex">Value of the X-Signature-Ed25519 header (128 hex chars).</param>
    /// <param name="timestamp">Value of the X-Signature-Timestamp header.</param>
    /// <param name="bodyBytes">Raw request body bytes.</param>
    /// <returns><see langword="true"/> if the signature is valid and the timestamp is fresh.</returns>
    public bool Verify(string signatureHex, string timestamp, ReadOnlySpan<byte> bodyBytes)
    {
        if (string.IsNullOrEmpty(signatureHex) || signatureHex.Length != 128)
            return false;
        if (string.IsNullOrEmpty(timestamp))
            return false;

        // Replay protection: reject requests with stale timestamps.
        if (!TryParseUnixTimestamp(timestamp, out var requestTime) ||
            DateTimeOffset.UtcNow - requestTime > MaxTimestampAge)
            return false;

        byte[] signature;
        try { signature = HexToBytes(signatureHex); }
        catch (Exception)
        {
            // Hex parsing failure means invalid signature format
            return false;
        }

        // Build signed message: timestamp_utf8 || body
        var timestampBytes = Encoding.UTF8.GetBytes(timestamp);
        var message = new byte[timestampBytes.Length + bodyBytes.Length];
        timestampBytes.CopyTo(message, 0);
        bodyBytes.CopyTo(message.AsSpan(timestampBytes.Length));

        return VerifyEd25519(_publicKey, message, signature);
    }

    /// <summary>
    /// Convenience overload accepting the body as a UTF-8 string.
    /// </summary>
    public bool Verify(string signatureHex, string timestamp, string body)
        => Verify(signatureHex, timestamp, Encoding.UTF8.GetBytes(body));

    // ── Ed25519 verification (RFC 8032 §5.1) ─────────────────────────────────
    //
    // Curve: -x^2 + y^2 = 1 + d·x^2·y^2  over GF(p),  p = 2^255 - 19
    // Group order: l = 2^252 + 27742317777372353535851937790883648493

    private static readonly BigInteger P =
        BigInteger.Pow(2, 255) - 19;

    private static readonly BigInteger L =
        BigInteger.Pow(2, 252) +
        BigInteger.Parse("27742317777372353535851937790883648493");

    // d = -121665 * ModInverse(121666, p) mod p
    private static readonly BigInteger D =
        Mod(-121665 * ModInverse(121666, P), P);

    // I = 2^((p-1)/4) mod p
    private static readonly BigInteger I =
        BigInteger.ModPow(2, (P - 1) / 4, P);

    // Base point B
    private static readonly (BigInteger X, BigInteger Y) B = RecoverBasePoint();

    private static bool VerifyEd25519(byte[] publicKey, byte[] message, byte[] signature)
    {
        if (signature.Length != 64 || publicKey.Length != 32)
            return false;

        // Decode R (first 32 bytes of signature) and S (last 32 bytes)
        var Rbytes = signature[..32];
        var Sbytes = signature[32..];

        var R = DecompressPoint(Rbytes);
        if (R is null) return false;

        var A = DecompressPoint(publicKey);
        if (A is null) return false;

        var S = DecodeLittleEndianScalar(Sbytes);
        if (S < 0 || S >= L) return false;

        // h = SHA-512(R || A || M) mod l
        using var sha = SHA512.Create();
        var rAm = new byte[32 + 32 + message.Length];
        Rbytes.CopyTo(rAm, 0);
        publicKey.CopyTo(rAm, 32);
        message.CopyTo(rAm, 64);
        var hBytes = sha.ComputeHash(rAm);
        var h = Mod(DecodeLittleEndianScalar512(hBytes), L);

        // Check: [S]B == R + [h]A  (projective Edwards arithmetic)
        var SB = ScalarMul(B, S);
        var hA = ScalarMul(A.Value, h);
        var RhA = PointAdd(R.Value, hA);

        return PointEqual(SB, RhA);
    }

    // ── Edwards curve helpers ─────────────────────────────────────────────────

    private static (BigInteger X, BigInteger Y)? DecompressPoint(byte[] encodedPoint)
    {
        if (encodedPoint.Length != 32) return null;

        var bytes = (byte[])encodedPoint.Clone();
        var sign = (bytes[31] >> 7) & 1;
        bytes[31] &= 0x7F;

        var y = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        if (y >= P) return null;

        // x^2 = (y^2 - 1) / (d*y^2 + 1) mod p
        var y2  = Mod(y * y, P);
        var num = Mod(y2 - 1, P);
        var den = Mod(D * y2 + 1, P);
        var x2  = Mod(num * ModInverse(den, P), P);

        if (x2 == 0)
        {
            if (sign == 1) return null;
            return (0, y);
        }

        var x = BigInteger.ModPow(x2, (P + 3) / 8, P);

        if (Mod(x * x - x2, P) != 0)
            x = Mod(x * I, P);

        if (Mod(x * x - x2, P) != 0) return null;

        if ((int)(x & 1) != sign)
            x = P - x;

        return (x, y);
    }

    private static (BigInteger X, BigInteger Y) PointAdd(
        (BigInteger X, BigInteger Y) p1,
        (BigInteger X, BigInteger Y) p2)
    {
        var (x1, y1) = p1;
        var (x2, y2) = p2;

        var dxy = Mod(D * x1 % P * x2 % P * y1 % P * y2, P);
        var x3  = Mod((x1 * y2 + x2 * y1) * ModInverse(1 + dxy, P), P);
        var y3  = Mod((y1 * y2 + x1 * x2) * ModInverse(1 - dxy, P), P);

        return (x3, y3);
    }

    private static (BigInteger X, BigInteger Y) ScalarMul(
        (BigInteger X, BigInteger Y) p,
        BigInteger scalar)
    {
        var result = (X: BigInteger.Zero, Y: BigInteger.One); // identity
        var addend = p;

        while (scalar > 0)
        {
            if ((scalar & 1) == 1)
                result = PointAdd(result, addend);
            addend = PointAdd(addend, addend);
            scalar >>= 1;
        }

        return result;
    }

    private static bool PointEqual(
        (BigInteger X, BigInteger Y) p1,
        (BigInteger X, BigInteger Y) p2)
        => p1.X == p2.X && p1.Y == p2.Y;

    private static (BigInteger X, BigInteger Y) RecoverBasePoint()
    {
        // y = 4/5 mod p
        var y  = Mod(4 * ModInverse(5, P), P);
        var y2 = Mod(y * y, P);
        var x2 = Mod((y2 - 1) * ModInverse(D * y2 + 1, P), P);
        var x  = BigInteger.ModPow(x2, (P + 3) / 8, P);

        if (Mod(x * x - x2, P) != 0)
            x = Mod(x * I, P);

        if ((int)(x & 1) != 0)
            x = P - x;

        return (x, y);
    }

    // ── Scalar helpers ────────────────────────────────────────────────────────

    private static BigInteger DecodeLittleEndianScalar(byte[] bytes)
        => new BigInteger(bytes, isUnsigned: true, isBigEndian: false);

    private static BigInteger DecodeLittleEndianScalar512(byte[] bytes)
        => new BigInteger(bytes, isUnsigned: true, isBigEndian: false);

    // ── Field helpers ─────────────────────────────────────────────────────────

    private static BigInteger Mod(BigInteger a, BigInteger m)
    {
        var r = a % m;
        return r < 0 ? r + m : r;
    }

    private static BigInteger ModInverse(BigInteger a, BigInteger m)
        => BigInteger.ModPow(Mod(a, m), m - 2, m); // valid when m is prime

    // ── Encoding helpers ──────────────────────────────────────────────────────

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static bool TryParseUnixTimestamp(string timestamp, out DateTimeOffset result)
    {
        if (long.TryParse(timestamp, out var unix))
        {
            result = DateTimeOffset.FromUnixTimeSeconds(unix);
            return true;
        }
        result = default;
        return false;
    }
}
