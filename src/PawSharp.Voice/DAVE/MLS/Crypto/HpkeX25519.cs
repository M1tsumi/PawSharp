// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using Enc = System.Text.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// RFC 9180 — HPKE (Hybrid Public Key Encryption).
///
/// Implements the specific KEM/KDF/AEAD combination used by the DAVE MLS ciphersuite:
///   KEM  : DHKEM(X25519, HKDF-SHA256)   — suite_id prefix "KEM\x00\x20"
///   KDF  : HKDF-SHA256                  — suite_id prefix "HPKE\x00\x20\x00\x01\x00\x01"
///   AEAD : AES-128-GCM                  — Nk = 16, Nn = 12, Nt = 16
///
/// Only the Base-mode (no authentication) single-shot Seal/Open operations are
/// needed by MLS (RFC 9420 §5.1.2) for encrypting GroupSecrets in Welcome messages.
///
/// References:
///   RFC 9180 — HPKE
///   RFC 9420 §5.1.2 — MLS HPKE usage
/// </summary>
internal static class HpkeX25519
{
    // ── Suite identifiers ─────────────────────────────────────────────────────

    // KEM id = 0x0020 (DHKEM(X25519, HKDF-SHA256))
    private static readonly byte[] KemId = { 0x00, 0x20 };

    // KDF id = 0x0001 (HKDF-SHA256)
    private static readonly byte[] KdfId = { 0x00, 0x01 };

    // AEAD id = 0x0001 (AES-128-GCM)
    private static readonly byte[] AeadId = { 0x00, 0x01 };

    // suite_id for KEM operations: concat("KEM", kem_id)
    private static readonly byte[] KemSuiteId = BuildSuiteId("KEM", KemId);

    // suite_id for KDF/AEAD operations: concat("HPKE", kem_id, kdf_id, aead_id)
    private static readonly byte[] HpkeSuiteId = BuildHpkeSuiteId();

    // Cache for derived public keys to avoid redundant X25519 scalar multiplication
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _publicKeyCache = new();

    // ── KEM constants ─────────────────────────────────────────────────────────

    private const int NSecret = 32; // Nsecret for DHKEM-X25519
    private const int NEnc    = 32; // Nenc — encapsulated key size
    private const int NHash   = 32; // Nh for HKDF-SHA256
    private const int NKey    = 16; // Nk for AES-128-GCM
    private const int NNonce  = 12; // Nn for AES-128-GCM
    private const int NTag    = 16; // Nt for AES-128-GCM

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// HPKE Base-mode SealBase: encrypts plaintext for recipientPublicKey.
    /// </summary>
    /// <param name="recipientPublicKey">32-byte recipient X25519 public key.</param>
    /// <param name="info">Application-specific info bytes (RFC 9420 §5.1.2 provides "MLS 1.0 " labels).</param>
    /// <param name="aad">Additional authenticated data (bound to ciphertext but not encrypted).</param>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <param name="enc">Output: 32-byte ephemeral encapsulated key (sent to recipient).</param>
    /// <returns>Ciphertext (same length as plaintext) followed by 16-byte GCM auth tag.</returns>
    public static byte[] SealBase(
        ReadOnlySpan<byte> recipientPublicKey,
        ReadOnlySpan<byte> info,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> plaintext,
        out byte[] enc)
    {
        if (recipientPublicKey.Length != NEnc)
            throw new ArgumentException($"recipientPublicKey must be {NEnc} bytes.", nameof(recipientPublicKey));
        
        // Generate ephemeral key pair
        Curve25519.GenerateKeyPair(out var ephemeralPriv, out var ephemeralPub);
        enc = ephemeralPub;

        // KEM: DH(ephemeral, recipient)
        var dh = Curve25519.SharedSecret(ephemeralPriv, recipientPublicKey);

        // KEM encapsulation — derive shared_secret
        var sharedSecret = ExtractAndExpand(dh, enc, recipientPublicKey);

        // KeySchedule: derive (key, base_nonce)
        var (key, baseNonce) = KeyScheduleBase(sharedSecret, info);

        // Single-shot encrypt with seq=0 nonce
        var nonce = ComputeNonce(baseNonce, 0);
        return AesGcmSeal(key, nonce, aad, plaintext);
    }

    /// <summary>
    /// HPKE Base-mode OpenBase: decrypts ciphertext using recipientPrivateKey.
    /// </summary>
    /// <param name="recipientPrivateKey">32-byte recipient X25519 private key.</param>
    /// <param name="enc">32-byte encapsulated key (received from sender).</param>
    /// <param name="info">Same info bytes used by the sender.</param>
    /// <param name="aad">Same AAD bytes used by the sender.</param>
    /// <param name="ciphertext">Ciphertext + 16-byte auth tag (as produced by SealBase).</param>
    /// <returns>Decrypted plaintext.</returns>
    /// <exception cref="CryptographicException">Thrown if authentication fails.</exception>
    public static byte[] OpenBase(
        ReadOnlySpan<byte> recipientPrivateKey,
        ReadOnlySpan<byte> enc,
        ReadOnlySpan<byte> info,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> ciphertext)
    {
        if (recipientPrivateKey.Length != NEnc)
            throw new ArgumentException($"recipientPrivateKey must be {NEnc} bytes.", nameof(recipientPrivateKey));
        if (enc.Length != NEnc)
            throw new ArgumentException($"enc must be {NEnc} bytes.", nameof(enc));

        // Derive recipient public key from private key
        var clampedPriv = recipientPrivateKey.ToArray();
        Curve25519.ClampScalar(clampedPriv);

        // KEM: DH(recipient, ephemeral)
        var dh = Curve25519.SharedSecret(clampedPriv, enc);

        // Derive recipient public key from private key using the X25519 base point (with caching)
        var privKeyHex = Convert.ToHexString(clampedPriv);
        if (!_publicKeyCache.TryGetValue(privKeyHex, out var recipientPub))
        {
            recipientPub = Curve25519.ScalarMult(clampedPriv, Curve25519.BasePoint);
            _publicKeyCache[privKeyHex] = recipientPub;
        }

        var sharedSecret = ExtractAndExpand(dh, enc, recipientPub);
        var (key, baseNonce) = KeyScheduleBase(sharedSecret, info);
        var nonce = ComputeNonce(baseNonce, 0);
        return AesGcmOpen(key, nonce, aad, ciphertext);
    }

    // ── DHKEM(X25519, HKDF-SHA256) — RFC 9180 §4.1 ────────────────────────────

    /// <summary>
    /// Derives the shared_secret from the DH output and the encapsulated key material.
    /// RFC 9180 §4.1 — ExtractAndExpand
    /// </summary>
    private static byte[] ExtractAndExpand(
        ReadOnlySpan<byte> dh,
        ReadOnlySpan<byte> enc,
        ReadOnlySpan<byte> recipientPub)
    {
        // kemContext = enc || pkR
        var kemContext = new byte[enc.Length + recipientPub.Length];
        enc.CopyTo(kemContext);
        recipientPub.CopyTo(kemContext.AsSpan(enc.Length));

        // prk_kem = LabeledExtract("", "shared_secret", dh)
        var prkKem = LabeledExtract(KemSuiteId, ReadOnlySpan<byte>.Empty, "shared_secret", dh);

        // shared_secret = LabeledExpand(prk_kem, "shared_secret", kemContext, Nsecret)
        return LabeledExpand(KemSuiteId, prkKem, "shared_secret", kemContext, NSecret);
    }

    // ── Key schedule — RFC 9180 §5.1 ──────────────────────────────────────────

    private static (byte[] key, byte[] baseNonce) KeyScheduleBase(
        ReadOnlySpan<byte> sharedSecret,
        ReadOnlySpan<byte> info)
    {
        // mode = 0 (Base)
        var pskIdHash = LabeledExtract(HpkeSuiteId, ReadOnlySpan<byte>.Empty, "psk_id_hash",
                                       ReadOnlySpan<byte>.Empty);
        var infoHash  = LabeledExtract(HpkeSuiteId, ReadOnlySpan<byte>.Empty, "info_hash", info);

        // ks_context = mode || pskIdHash || infoHash
        var ksContext = new byte[1 + NHash + NHash];
        ksContext[0] = 0; // mode = Base
        pskIdHash.CopyTo(ksContext, 1);
        infoHash.CopyTo(ksContext, 1 + NHash);

        // secret = LabeledExtract(shared_secret, "secret", psk="")
        var secret = LabeledExtract(HpkeSuiteId, sharedSecret, "secret", ReadOnlySpan<byte>.Empty);

        var key       = LabeledExpand(HpkeSuiteId, secret, "key",       ksContext, NKey);
        var baseNonce = LabeledExpand(HpkeSuiteId, secret, "base_nonce", ksContext, NNonce);

        return (key, baseNonce);
    }

    // ── Nonce computation — RFC 9180 §5.2 ─────────────────────────────────────

    private static byte[] ComputeNonce(ReadOnlySpan<byte> baseNonce, ulong seq)
    {
        // nonce = base_nonce XOR I2OSP(seq, Nn)
        var nonce    = baseNonce.ToArray();
        var seqBytes = new byte[NNonce];
        // seq as big-endian in last 8 bytes
        for (int i = 0; i < 8; i++)
            seqBytes[NNonce - 1 - i] = (byte)(seq >> (8 * i));
        for (int i = 0; i < NNonce; i++)
            nonce[i] ^= seqBytes[i];
        return nonce;
    }

    // ── LabeledExtract / LabeledExpand — RFC 9180 §4 ──────────────────────────

    private static byte[] LabeledExtract(
        ReadOnlySpan<byte> suiteId,
        ReadOnlySpan<byte> salt,
        string label,
        ReadOnlySpan<byte> ikm)
    {
        // labeled_ikm = concat("HPKE-v1", suite_id, label, ikm)
        var labelBytes  = Enc.ASCII.GetBytes(label);
        var version     = Enc.ASCII.GetBytes("HPKE-v1");
        var labeledIkm  = Concat(version, suiteId.ToArray(), labelBytes, ikm.ToArray());
        return MlsHkdf.Extract(salt, labeledIkm);
    }

    private static byte[] LabeledExpand(
        ReadOnlySpan<byte> suiteId,
        ReadOnlySpan<byte> prk,
        string label,
        ReadOnlySpan<byte> info,
        int length)
    {
        // labeled_info = concat(I2OSP(length, 2), "HPKE-v1", suite_id, label, info)
        var labelBytes  = Enc.ASCII.GetBytes(label);
        var version     = Enc.ASCII.GetBytes("HPKE-v1");
        var lenBytes    = new byte[] { (byte)(length >> 8), (byte)length };
        var labeledInfo = Concat(lenBytes, version, suiteId.ToArray(), labelBytes, info.ToArray());
        return MlsHkdf.Expand(prk, labeledInfo, length);
    }

    // ── AES-128-GCM ──────────────────────────────────────────────────────────

    private static byte[] AesGcmSeal(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> plaintext)
    {
        using var aes = new AesGcm(key.ToArray(), NTag);
        var ct  = new byte[plaintext.Length];
        var tag = new byte[NTag];
        aes.Encrypt(nonce, plaintext, ct, tag, aad.IsEmpty ? null : aad.ToArray());
        var result = new byte[ct.Length + NTag];
        ct.CopyTo(result, 0);
        tag.CopyTo(result, ct.Length);
        return result;
    }

    private static byte[] AesGcmOpen(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> ciphertext)
    {
        if (ciphertext.Length < NTag)
            throw new CryptographicException("HPKE ciphertext too short.");

        int ctLen = ciphertext.Length - NTag;
        var ct    = ciphertext.Slice(0, ctLen);
        var tag   = ciphertext.Slice(ctLen, NTag);

        using var aes = new AesGcm(key.ToArray(), NTag);
        var plain = new byte[ctLen];
        aes.Decrypt(nonce, ct, tag, plain, aad.IsEmpty ? null : aad.ToArray());
        return plain;
    }

    // ── Suite-id builders ─────────────────────────────────────────────────────

    private static byte[] BuildSuiteId(string type, ReadOnlySpan<byte> id)
    {
        var t   = Enc.ASCII.GetBytes(type);
        var r   = new byte[t.Length + id.Length];
        t.CopyTo(r, 0);
        id.CopyTo(r.AsSpan(t.Length));
        return r;
    }

    private static byte[] BuildHpkeSuiteId()
    {
        // concat("HPKE", 0x00 0x20, 0x00 0x01, 0x00 0x01)
        return new byte[] { 0x48, 0x50, 0x4B, 0x45,  // "HPKE"
                            0x00, 0x20,               // KEM id
                            0x00, 0x01,               // KDF id
                            0x00, 0x01 };             // AEAD id
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static byte[] Concat(params byte[][] parts)
    {
        int total = 0;
        foreach (var p in parts) total += p.Length;
        var r = new byte[total];
        int pos = 0;
        foreach (var p in parts) { p.CopyTo(r, pos); pos += p.Length; }
        return r;
    }
}
