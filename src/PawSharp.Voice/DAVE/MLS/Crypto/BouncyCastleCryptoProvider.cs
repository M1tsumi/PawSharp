// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace PawSharp.Voice.DAVE.MLS.Crypto;

internal sealed class BouncyCastleCryptoProvider : ICryptoProvider
{
    private const int P256KeySize = 32;
    private const int Aes128KeySize = 16;
    private const int GcmNonceSize = 12;
    private const int GcmTagSize = 16;
    private const int Sha256HashSize = 32;

    private static readonly X9ECParameters P256Params = ECNamedCurveTable.GetByOid(X9ObjectIdentifiers.Prime256v1)!;
    private static readonly ECDomainParameters P256Domain = new(P256Params.Curve, P256Params.G, P256Params.N, P256Params.H);

    // ── P-256 ECDH ──────────────────────────────────────────────────────────

    public void GenerateP256KeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        var keyGen = new ECKeyPairGenerator("ECDH");
        keyGen.Init(new ECKeyGenerationParameters(P256Domain, new SecureRandom()));
        var keyPair = keyGen.GenerateKeyPair();

        var priv = (ECPrivateKeyParameters)keyPair.Private;
        var pub = (ECPublicKeyParameters)keyPair.Public;

        privateKey = priv.D.ToByteArrayUnsigned();
        // Ensure exactly 32 bytes
        if (privateKey.Length < P256KeySize)
        {
            var tmp = new byte[P256KeySize];
            privateKey.CopyTo(tmp, P256KeySize - privateKey.Length);
            privateKey = tmp;
        }
        publicKey = pub.Q.GetEncoded(false); // 65 bytes SEC1 uncompressed
    }

    public byte[] P256SharedSecret(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey)
    {
        if (privateKey.Length != P256KeySize)
            throw new ArgumentException($"Private key must be {P256KeySize} bytes.", nameof(privateKey));

        var privParams = new ECPrivateKeyParameters(
            new BigInteger(1, privateKey.ToArray()), P256Domain);

        var pubPoint = P256Params.Curve.DecodePoint(publicKey.ToArray());
        var pubParams = new ECPublicKeyParameters(pubPoint, P256Domain);

        var agreement = new ECDHBasicAgreement();
        agreement.Init(privParams);
        var shared = agreement.CalculateAgreement(pubParams);

        // Shared secret is x-coordinate, encoded as unsigned big-endian, 32 bytes
        var sharedBytes = shared.ToByteArrayUnsigned();
        if (sharedBytes.Length < P256KeySize)
        {
            var tmp = new byte[P256KeySize];
            sharedBytes.CopyTo(tmp, P256KeySize - sharedBytes.Length);
            return tmp;
        }
        return sharedBytes;
    }

    public byte[] P256GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != P256KeySize)
            throw new ArgumentException($"Private key must be {P256KeySize} bytes.", nameof(privateKey));

        var privParams = new ECPrivateKeyParameters(
            new BigInteger(1, privateKey.ToArray()), P256Domain);

        var q = P256Domain.G.Multiply(privParams.D);
        return q.GetEncoded(false);
    }

    // ── ECDSA P-256 ─────────────────────────────────────────────────────────

    public void GenerateEcdsaP256KeyPair(out byte[] privateKey, out byte[] publicKey)
    {
        // For DAVE, ECDH and ECDSA keys use the same curve but different key material.
        // Reuse the P-256 generator.
        var keyGen = new ECKeyPairGenerator("ECDSA");
        keyGen.Init(new ECKeyGenerationParameters(P256Domain, new SecureRandom()));
        var keyPair = keyGen.GenerateKeyPair();

        var priv = (ECPrivateKeyParameters)keyPair.Private;
        var pub = (ECPublicKeyParameters)keyPair.Public;

        privateKey = priv.D.ToByteArrayUnsigned();
        if (privateKey.Length < P256KeySize)
        {
            var tmp = new byte[P256KeySize];
            privateKey.CopyTo(tmp, P256KeySize - privateKey.Length);
            privateKey = tmp;
        }
        publicKey = pub.Q.GetEncoded(false);
    }

    public byte[] EcdsaP256GetPublicKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != P256KeySize)
            throw new ArgumentException($"Private key must be {P256KeySize} bytes.", nameof(privateKey));

        var privParams = new ECPrivateKeyParameters(
            new BigInteger(1, privateKey.ToArray()), P256Domain);

        var q = P256Domain.G.Multiply(privParams.D);
        return q.GetEncoded(false);
    }

    public byte[] EcdsaP256Sign(ReadOnlySpan<byte> message, ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != P256KeySize)
            throw new ArgumentException($"Private key must be {P256KeySize} bytes.", nameof(privateKey));

        var privParams = new ECPrivateKeyParameters(
            new BigInteger(1, privateKey.ToArray()), P256Domain);
        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, privParams);

        var hash = Sha256Hash(message);
        var sig = signer.GenerateSignature(hash);

        // Encode as DER (standard ECDSA signature format)
        var derSig = new Org.BouncyCastle.Asn1.DerSequence(
            new Org.BouncyCastle.Asn1.DerInteger(sig[0]),
            new Org.BouncyCastle.Asn1.DerInteger(sig[1]));
        return derSig.GetDerEncoded();
    }

    public bool EcdsaP256Verify(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        var hash = Sha256Hash(message);

        var pubPoint = P256Params.Curve.DecodePoint(publicKey.ToArray());
        var pubParams = new ECPublicKeyParameters(pubPoint, P256Domain);
        var verifier = new ECDsaSigner();
        verifier.Init(false, pubParams);

        // Parse DER signature
        try
        {
            var derSig = Org.BouncyCastle.Asn1.DerSequence.GetInstance(
                Org.BouncyCastle.Asn1.Asn1Object.FromByteArray(signature.ToArray()));
            var r = Org.BouncyCastle.Math.BigInteger.ValueOf(((Org.BouncyCastle.Asn1.DerInteger)derSig[0]).Value.LongValue);
            var s = Org.BouncyCastle.Math.BigInteger.ValueOf(((Org.BouncyCastle.Asn1.DerInteger)derSig[1]).Value.LongValue);
            // Use proper BigInteger extraction
            r = ((Org.BouncyCastle.Asn1.DerInteger)derSig[0]).Value;
            s = ((Org.BouncyCastle.Asn1.DerInteger)derSig[1]).Value;

            return verifier.VerifySignature(hash, r, s);
        }
        catch
        {
            return false;
        }
    }

    // ── HKDF ────────────────────────────────────────────────────────────────

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
                hMac.BlockUpdate(result, offset - Sha256HashSize, Sha256HashSize);
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

    // ── SHA-256 ─────────────────────────────────────────────────────────────

    public byte[] Sha256Hash(ReadOnlySpan<byte> data)
    {
        var digest = new Sha256Digest();
        digest.BlockUpdate(data);
        var result = new byte[Sha256HashSize];
        digest.DoFinal(result, 0);
        return result;
    }

    // ── AES-128-GCM ─────────────────────────────────────────────────────────

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
