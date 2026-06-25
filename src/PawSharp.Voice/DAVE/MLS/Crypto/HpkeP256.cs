// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using Enc = System.Text.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

internal static class HpkeP256
{
    // KEM id = 0x0010 (DHKEM(P-256, HKDF-SHA256))
    private static readonly byte[] KemId = { 0x00, 0x10 };

    // KDF id = 0x0001 (HKDF-SHA256)
    private static readonly byte[] KdfId = { 0x00, 0x01 };

    // AEAD id = 0x0001 (AES-128-GCM)
    private static readonly byte[] AeadId = { 0x00, 0x01 };

    private static readonly byte[] KemSuiteId = BuildSuiteId("KEM", KemId);
    private static readonly byte[] HpkeSuiteId = BuildHpkeSuiteId();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _publicKeyCache = new();

    // KEM constants for DHKEM(P-256, HKDF-SHA256)
    private const int NSecret = 32; // shared secret size
    private const int NEnc    = 65; // encapsulated key size (uncompressed SEC1)
    private const int NHash   = 32;
    private const int NKey    = 16;
    private const int NNonce  = 12;
    private const int NTag    = 16;

    public static byte[] SealBase(
        ReadOnlySpan<byte> recipientPublicKey,
        ReadOnlySpan<byte> info,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> plaintext,
        out byte[] enc)
    {
        if (recipientPublicKey.Length != NEnc)
            throw new ArgumentException($"recipientPublicKey must be {NEnc} bytes.", nameof(recipientPublicKey));

        var provider = CryptoProviderFactory.Instance;

        provider.GenerateP256KeyPair(out var ephemeralPriv, out var ephemeralPub);
        enc = ephemeralPub;

        var dh = provider.P256SharedSecret(ephemeralPriv, recipientPublicKey);
        var sharedSecret = ExtractAndExpand(dh, enc, recipientPublicKey);

        var (key, baseNonce) = KeyScheduleBase(sharedSecret, info);
        var nonce = ComputeNonce(baseNonce, 0);
        return AesGcmSeal(key, nonce, aad, plaintext);
    }

    public static byte[] OpenBase(
        ReadOnlySpan<byte> recipientPrivateKey,
        ReadOnlySpan<byte> enc,
        ReadOnlySpan<byte> info,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> ciphertext)
    {
        if (recipientPrivateKey.Length != NSecret)
            throw new ArgumentException($"recipientPrivateKey must be {NSecret} bytes.", nameof(recipientPrivateKey));
        if (enc.Length != NEnc)
            throw new ArgumentException($"enc must be {NEnc} bytes.", nameof(enc));

        var provider = CryptoProviderFactory.Instance;

        var privKeyHex = Convert.ToHexString(recipientPrivateKey);
        if (!_publicKeyCache.TryGetValue(privKeyHex, out var recipientPub))
        {
            recipientPub = provider.P256GetPublicKey(recipientPrivateKey);
            _publicKeyCache[privKeyHex] = recipientPub;
        }

        var dh = provider.P256SharedSecret(recipientPrivateKey, enc);
        var sharedSecret = ExtractAndExpand(dh, enc, recipientPub);

        var (key, baseNonce) = KeyScheduleBase(sharedSecret, info);
        var nonce = ComputeNonce(baseNonce, 0);
        return AesGcmOpen(key, nonce, aad, ciphertext);
    }

    private static byte[] ExtractAndExpand(
        ReadOnlySpan<byte> dh,
        ReadOnlySpan<byte> enc,
        ReadOnlySpan<byte> recipientPub)
    {
        var kemContext = new byte[enc.Length + recipientPub.Length];
        enc.CopyTo(kemContext);
        recipientPub.CopyTo(kemContext.AsSpan(enc.Length));

        var prkKem = LabeledExtract(KemSuiteId, ReadOnlySpan<byte>.Empty, "shared_secret", dh);
        return LabeledExpand(KemSuiteId, prkKem, "shared_secret", kemContext, NSecret);
    }

    private static (byte[] key, byte[] baseNonce) KeyScheduleBase(
        ReadOnlySpan<byte> sharedSecret,
        ReadOnlySpan<byte> info)
    {
        var pskIdHash = LabeledExtract(HpkeSuiteId, ReadOnlySpan<byte>.Empty, "psk_id_hash",
                                       ReadOnlySpan<byte>.Empty);
        var infoHash  = LabeledExtract(HpkeSuiteId, ReadOnlySpan<byte>.Empty, "info_hash", info);

        var ksContext = new byte[1 + NHash + NHash];
        ksContext[0] = 0;
        pskIdHash.CopyTo(ksContext, 1);
        infoHash.CopyTo(ksContext, 1 + NHash);

        var secret = LabeledExtract(HpkeSuiteId, sharedSecret, "secret", ReadOnlySpan<byte>.Empty);
        var key       = LabeledExpand(HpkeSuiteId, secret, "key",       ksContext, NKey);
        var baseNonce = LabeledExpand(HpkeSuiteId, secret, "base_nonce", ksContext, NNonce);

        return (key, baseNonce);
    }

    private static byte[] ComputeNonce(ReadOnlySpan<byte> baseNonce, ulong seq)
    {
        var nonce    = baseNonce.ToArray();
        var seqBytes = new byte[NNonce];
        for (int i = 0; i < 8; i++)
            seqBytes[NNonce - 1 - i] = (byte)(seq >> (8 * i));
        for (int i = 0; i < NNonce; i++)
            nonce[i] ^= seqBytes[i];
        return nonce;
    }

    private static byte[] LabeledExtract(
        ReadOnlySpan<byte> suiteId,
        ReadOnlySpan<byte> salt,
        string label,
        ReadOnlySpan<byte> ikm)
    {
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
        var labelBytes  = Enc.ASCII.GetBytes(label);
        var version     = Enc.ASCII.GetBytes("HPKE-v1");
        var lenBytes    = new byte[] { (byte)(length >> 8), (byte)length };
        var labeledInfo = Concat(lenBytes, version, suiteId.ToArray(), labelBytes, info.ToArray());
        return MlsHkdf.Expand(prk, labeledInfo, length);
    }

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

    private static byte[] BuildSuiteId(string type, ReadOnlySpan<byte> id)
    {
        var t = Enc.ASCII.GetBytes(type);
        var r = new byte[t.Length + id.Length];
        t.CopyTo(r, 0);
        id.CopyTo(r.AsSpan(t.Length));
        return r;
    }

    private static byte[] BuildHpkeSuiteId()
    {
        return new byte[] { 0x48, 0x50, 0x4B, 0x45,  // "HPKE"
                            0x00, 0x10,               // KEM id (P-256)
                            0x00, 0x01,               // KDF id
                            0x00, 0x01 };             // AEAD id
    }

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
