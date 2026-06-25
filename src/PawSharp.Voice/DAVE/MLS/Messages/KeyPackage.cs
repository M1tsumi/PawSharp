// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §10 — MLS KeyPackage.
///
/// A KeyPackage advertises a member's desire to join an MLS group.
/// It bundles:
///   - The MLS protocol version and ciphersuite
///   - An HPKE init key (used to encrypt GroupSecrets in Welcome messages)
///   - A LeafNode (HPKE encryption key, signature key, credential)
///   - An ECDSA P-256 signature over the whole structure
///
/// Discord DAVE uses ciphersuite 0x0002.
/// </summary>
internal sealed class KeyPackage
{
    public ProtocolVersion Version    { get; } = ProtocolVersion.Mls10;
    public CipherSuite     Suite      { get; } = CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256;

    /// <summary>P-256 init public key (uncompressed SEC1, 65 bytes).</summary>
    public byte[] InitKey  { get; }

    public LeafNode Leaf   { get; }

    /// <summary>ECDSA P-256 signature (DER-encoded).</summary>
    public byte[] Signature { get; private set; }

    /// <summary>P-256 private key corresponding to <see cref="InitKey"/>.</summary>
    public byte[]? InitPrivateKey { get; }

    /// <summary>P-256 private key embedded in the leaf node (for HPKE).</summary>
    public byte[]? LeafHpkePrivateKey { get; }

    /// <summary>ECDSA P-256 private key embedded in the leaf node.</summary>
    public byte[]? LeafSignPrivateKey { get; }

    private KeyPackage(
        byte[] initKey, LeafNode leaf, byte[] signature,
        byte[]? initPriv = null, byte[]? leafHpkePriv = null, byte[]? leafSigPriv = null)
    {
        InitKey           = initKey;
        Leaf              = leaf;
        Signature         = signature;
        InitPrivateKey    = initPriv;
        LeafHpkePrivateKey = leafHpkePriv;
        LeafSignPrivateKey = leafSigPriv;
    }

    /// <summary>
    /// Generates a complete, self-signed KeyPackage.
    /// </summary>
    public static KeyPackage Generate(byte[] identity)
    {
        var provider = CryptoProviderFactory.Instance;

        provider.GenerateP256KeyPair(out var initPriv, out var initPub);

        var leaf = LeafNode.Generate(identity, out var leafHpkePriv, out var leafSigPriv);

        var kp = new KeyPackage(initPub, leaf, Array.Empty<byte>(), initPriv, leafHpkePriv, leafSigPriv);
        kp.Signature = provider.EcdsaP256Sign(kp.ToBeSigned(), leafSigPriv);
        return kp;
    }

    public byte[] Encode()
    {
        using var w = new TlsWriter(200);
        w.WriteUint16((ushort)Version);
        w.WriteUint16((ushort)Suite);
        w.WriteVector16(InitKey);
        w.WriteBytes(Leaf.Encode());
        w.WriteUint16(0);
        w.WriteVector16(Signature);
        return w.ToArray();
    }

    public static KeyPackage Decode(ReadOnlySpan<byte> data)
    {
        var r       = new TlsReader(data);
        var version = (ProtocolVersion)r.ReadUint16();
        var suite   = (CipherSuite)r.ReadUint16();

        if (version != ProtocolVersion.Mls10)
            throw new MlsDecodeException($"Unsupported MLS version: {version}");
        if (suite != CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256)
            throw new MlsDecodeException($"Unsupported ciphersuite: {suite}");

        var initKey = r.ReadVector16();
        var leafStartPosition = r.Position;
        var leaf = LeafNode.Decode(data.Slice(r.Position));
        var leafEndPosition = leafStartPosition + leaf.Encode().Length;

        var r2 = new TlsReader(data.Slice(leafEndPosition));
        r2.ReadVector16();
        var sig = r2.ReadVector16();

        return new KeyPackage(initKey, leaf, sig);
    }

    public bool VerifySignature()
        => CryptoProviderFactory.Instance.EcdsaP256Verify(ToBeSigned(), Signature, Leaf.SignatureKey);

    private byte[] ToBeSigned()
    {
        using var w = new TlsWriter(200);
        w.WriteUint16((ushort)Version);
        w.WriteUint16((ushort)Suite);
        w.WriteVector16(InitKey);
        w.WriteBytes(Leaf.Encode());
        w.WriteUint16(0);
        return w.ToArray();
    }
}
