// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// Abstraction layer for cryptographic operations used by MLS/DAVE.
/// Allows swapping between different crypto backends (BCL, BouncyCastle, libsodium, etc.)
/// </summary>
internal interface ICryptoProvider
{
    // ── X25519 (Curve25519) ─────────────────────────────────────────────────────

    /// <summary>Generates an X25519 key pair.</summary>
    void GenerateX25519KeyPair(out byte[] privateKey, out byte[] publicKey);

    /// <summary>Computes X25519 shared secret: ECDH(privateKey, publicKey).</summary>
    byte[] X25519SharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey);

    /// <summary>Derives X25519 public key from private key.</summary>
    byte[] X25519GetPublicKey(ReadOnlySpan<byte> privateKey);

    // ── Ed25519 ─────────────────────────────────────────────────────────────────

    /// <summary>Generates an Ed25519 key pair.</summary>
    void GenerateEd25519KeyPair(out byte[] privateKey, out byte[] publicKey);

    /// <summary>Derives Ed25519 public key from private key.</summary>
    byte[] Ed25519GetPublicKey(ReadOnlySpan<byte> privateKey);

    /// <summary>Signs a message using Ed25519.</summary>
    byte[] Ed25519Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey);

    /// <summary>Verifies an Ed25519 signature.</summary>
    bool Ed25519Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);

    // ── HKDF ───────────────────────────────────────────────────────────────────

    /// <summary>HKDF-Extract using SHA-256.</summary>
    byte[] HkdfExtract(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> ikm);

    /// <summary>HKDF-Expand using SHA-256.</summary>
    byte[] HkdfExpand(ReadOnlySpan<byte> prk, ReadOnlySpan<byte> info, int length);

    // ── SHA-256 ────────────────────────────────────────────────────────────────

    /// <summary>Computes SHA-256 hash.</summary>
    byte[] Sha256Hash(ReadOnlySpan<byte> data);

    // ── AES-128-GCM ──────────────────────────────────────────────────────────

    /// <summary>Encrypts with AES-128-GCM.</summary>
    byte[] Aes128GcmEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad);

    /// <summary>Decrypts with AES-128-GCM.</summary>
    byte[] Aes128GcmDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad);
}
