// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

internal interface ICryptoProvider
{
    // ── P-256 ECDH ──────────────────────────────────────────────────────────

    void GenerateP256KeyPair(out byte[] privateKey, out byte[] publicKey);

    byte[] P256SharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey);

    byte[] P256GetPublicKey(ReadOnlySpan<byte> privateKey);

    // ── ECDSA P-256 ─────────────────────────────────────────────────────────

    void GenerateEcdsaP256KeyPair(out byte[] privateKey, out byte[] publicKey);

    byte[] EcdsaP256GetPublicKey(ReadOnlySpan<byte> privateKey);

    byte[] EcdsaP256Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey);

    bool EcdsaP256Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);

    // ── HKDF ────────────────────────────────────────────────────────────────

    byte[] HkdfExtract(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> ikm);

    byte[] HkdfExpand(ReadOnlySpan<byte> prk, ReadOnlySpan<byte> info, int length);

    // ── SHA-256 ─────────────────────────────────────────────────────────────

    byte[] Sha256Hash(ReadOnlySpan<byte> data);

    // ── AES-128-GCM ─────────────────────────────────────────────────────────

    byte[] Aes128GcmEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad);

    byte[] Aes128GcmDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad);
}
