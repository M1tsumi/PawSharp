// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Messages;

internal sealed class LeafNode
{
    /// <summary>P-256 HPKE public key (uncompressed SEC1, 65 bytes).</summary>
    public byte[] EncryptionKey { get; }

    /// <summary>ECDSA P-256 signing public key (uncompressed SEC1, 65 bytes).</summary>
    public byte[] SignatureKey { get; }

    public Credential Credential { get; }

    public LeafNodeSource Source { get; }

    /// <summary>ECDSA P-256 signature (DER-encoded, ~70-73 bytes).</summary>
    public byte[] Signature { get; private set; }

    private LeafNode(byte[] encKey, byte[] sigKey, Credential cred, LeafNodeSource src, byte[] sig)
    {
        EncryptionKey = encKey;
        SignatureKey  = sigKey;
        Credential    = cred;
        Source        = src;
        Signature     = sig;
    }

    public static LeafNode Generate(
        byte[] identity,
        out byte[] hpkePrivateKeyOut,
        out byte[] sigPrivateKeyOut)
    {
        var provider = CryptoProviderFactory.Instance;
        provider.GenerateP256KeyPair(out hpkePrivateKeyOut, out var hpkePub);
        provider.GenerateEcdsaP256KeyPair(out sigPrivateKeyOut, out var sigPub);
        var cred   = Credential.Basic(identity);
        var node   = new LeafNode(hpkePub, sigPub, cred, LeafNodeSource.KeyPackage, Array.Empty<byte>());
        node.Signature = provider.EcdsaP256Sign(node.ToBeSigned(), sigPrivateKeyOut);
        return node;
    }

    public byte[] Encode()
    {
        using var w = new TlsWriter(100);
        w.WriteVector16(EncryptionKey);
        w.WriteVector16(SignatureKey);
        w.WriteBytes(Credential.Encode());
        w.WriteUint8((byte)Source);
        using var caps = new TlsWriter(4);
        caps.WriteUint16((ushort)CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256);
        w.WriteNested16(caps);
        w.WriteUint16(0);
        w.WriteVector16(Signature);
        return w.ToArray();
    }

    public static LeafNode Decode(ReadOnlySpan<byte> data)
    {
        var r      = new TlsReader(data);
        var encKey = r.ReadVector16();
        var sigKey = r.ReadVector16();

        var credType = (CredentialType)r.ReadUint16();
        Credential cred;
        if (credType == CredentialType.Basic)
        {
            var identity = r.ReadVector16();
            cred = Credential.Basic(identity);
        }
        else
        {
            throw new MlsDecodeException($"Unsupported credential type: {credType}");
        }

        var source = (LeafNodeSource)r.ReadUint8();
        r.ReadVector16();
        r.ReadVector16();
        var sig    = r.ReadVector16();
        return new LeafNode(encKey, sigKey, cred, source, sig);
    }

    public bool VerifySignature()
        => CryptoProviderFactory.Instance.EcdsaP256Verify(ToBeSigned(), Signature, SignatureKey);

    private byte[] ToBeSigned()
    {
        using var w = new TlsWriter(100);
        w.WriteVector16(EncryptionKey);
        w.WriteVector16(SignatureKey);
        w.WriteBytes(Credential.Encode());
        w.WriteUint8((byte)Source);
        using var caps = new TlsWriter(4);
        caps.WriteUint16((ushort)CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256);
        w.WriteNested16(caps);
        w.WriteUint16(0);
        return w.ToArray();
    }
}
