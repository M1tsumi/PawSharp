// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

/// <summary>
/// Crypto provider using BouncyCastle for X25519/Ed25519 and BCL for HKDF/AES-GCM.
/// Provides hardware-accelerated operations where available via BouncyCastle's optimized C# code.
/// </summary>
internal sealed class BouncyCastleCryptoProvider : ICryptoProvider
{
    private const int X25519KeySize = 32;
    private const int Ed25519KeySize = 32;
    private const int Ed25519SignatureSize = 64;
    private const int Aes128KeySize = 16;
    private const int GcmNonceSize = 12;
    private const int GcmTagSize = 16;
    private const int Sha256HashSize = 32;

    // ── X25519 ─────────────────────────────────────────────────────────────────

    public void GenerateX25519KeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        var keyGen = new X25519KeyPairGenerator();
        keyGen.Init(new X25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = keyGen.GenerateKeyPair();

        privateKey = ((X25519PrivateKeyParameters)keyPair.Private).GetEncoded();
        publicKey = ((X25519PublicKeyParameters)keyPair.Public).GetEncoded();
    }

    public byte[] X25519SharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
    {
        if (privateKey.Length != X25519KeySize)
            throw new ArgumentException($"Private key must be {X25519KeySize} bytes.", nameof(privateKey));
        if (publicKey.Length != X25519KeySize)
            throw new ArgumentException($"Public key must be {X25519KeySize} bytes.", nameof(publicKey));

        var privParams = new X25519PrivateKeyParameters(privateKey.ToArray());
        var pubParams = new X25519PublicKeyParameters(publicKey.ToArray());

        var agreement = new X25519Agreement();
        agreement.Init(privParams);
        var shared = new byte[X25519KeySize];
        agreement.CalculateAgreement(pubParams, shared, 0);
        return shared;
    }

    public byte[] X25519GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != X25519KeySize)
            throw new ArgumentException($"Private key must be {X25519KeySize} bytes.", nameof(privateKey));

        var privParams = new X25519PrivateKeyParameters(privateKey.ToArray());
        return privParams.GeneratePublicKey().GetEncoded();
    }

    // ── Ed25519 ────────────────────────────────────────────────────────────────

    public void GenerateEd25519KeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        var keyGen = new Ed25519KeyPairGenerator();
        keyGen.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = keyGen.GenerateKeyPair();

        privateKey = ((Ed25519PrivateKeyParameters)keyPair.Private).GetEncoded();
        publicKey = ((Ed25519PublicKeyParameters)keyPair.Public).GetEncoded();
    }

    public byte[] Ed25519GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != Ed25519KeySize)
            throw new ArgumentException($"Private key must be {Ed25519KeySize} bytes.", nameof(privateKey));

        var privParams = new Ed25519PrivateKeyParameters(privateKey.ToArray());
        return privParams.GeneratePublicKey().GetEncoded();
    }

    public byte[] Ed25519Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != Ed25519KeySize)
            throw new ArgumentException($"Private key must be {Ed25519KeySize} bytes.", nameof(privateKey));

        var privParams = new Ed25519PrivateKeyParameters(privateKey.ToArray());
        var signer = new Ed25519Signer();
        signer.Init(true, privParams);
        signer.BlockUpdate(message);
        return signer.GenerateSignature();
    }

    public bool Ed25519Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        if (signature.Length != Ed25519SignatureSize)
            throw new ArgumentException($"Signature must be {Ed25519SignatureSize} bytes.", nameof(signature));
        if (publicKey.Length != Ed25519KeySize)
            throw new ArgumentException($"Public key must be {Ed25519KeySize} bytes.", nameof(publicKey));

        var pubParams = new Ed25519PublicKeyParameters(publicKey.ToArray());
        var verifier = new Ed25519Signer();
        verifier.Init(false, pubParams);
        verifier.BlockUpdate(message);
        return verifier.VerifySignature(signature.ToArray());
    }

    // ── HKDF ───────────────────────────────────────────────────────────────────

    public byte[] HkdfExtract(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> ikm)
    {
        var hMac = new HMac(new Sha256Digest());
        hMac.Init(new KeyParameter(salt.IsEmpty ? new byte[Sha256HashSize] : salt.ToArray()));
        hMac.BlockUpdate(ikm);
        var result = new byte[Sha256HashSize];
        hMac.DoFinal(result, 0);
        return result;
    }

    public byte[] HkdfExpand(ReadOnlySpan<byte> prk, ReadOnlySpan<byte> info, int length)
    {
        var hMac = new HMac(new Sha256Digest());
        hMac.Init(new KeyParameter(prk.ToArray()));

        var result = new byte[length];
        int offset = 0;
        byte counter = 1;

        while (offset < length)
        {
            if (counter > 1)
            {
                hMac.BlockUpdate(result, offset - Sha256HashSize, Sha256HashSize);
            }
            hMac.BlockUpdate(info);
            hMac.Update(counter);
            var step = new byte[Sha256HashSize];
            hMac.DoFinal(step, 0);

            var copyLen = Math.Min(Sha256HashSize, length - offset);
            Array.Copy(step, 0, result, offset, copyLen);
            offset += copyLen;
            counter++;
        }

        return result;
    }

    // ── SHA-256 ────────────────────────────────────────────────────────────────

    public byte[] Sha256Hash(ReadOnlySpan<byte> data)
    {
        var digest = new Sha256Digest();
        digest.BlockUpdate(data);
        var result = new byte[Sha256HashSize];
        digest.DoFinal(result, 0);
        return result;
    }

    // ── AES-128-GCM ──────────────────────────────────────────────────────────

    public byte[] Aes128GcmEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad)
    {
        if (key.Length != Aes128KeySize)
            throw new ArgumentException($"Key must be {Aes128KeySize} bytes.", nameof(key));
        if (nonce.Length != GcmNonceSize)
            throw new ArgumentException($"Nonce must be {GcmNonceSize} bytes.", nameof(nonce));

        using var aes = new AesGcm(key, GcmTagSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[GcmTagSize];
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad.Length == 0 ? null : aad.ToArray());

        var result = new byte[ciphertext.Length + GcmTagSize];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, GcmTagSize);
        return result;
    }

    public byte[] Aes128GcmDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> aad)
    {
        if (key.Length != Aes128KeySize)
            throw new ArgumentException($"Key must be {Aes128KeySize} bytes.", nameof(key));
        if (nonce.Length != GcmNonceSize)
            throw new ArgumentException($"Nonce must be {GcmNonceSize} bytes.", nameof(nonce));
        if (tag.Length != GcmTagSize)
            throw new ArgumentException($"Tag must be {GcmTagSize} bytes.", nameof(tag));

        using var aes = new AesGcm(key, GcmTagSize);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext, aad.Length == 0 ? null : aad.ToArray());
        return plaintext;
    }
}
