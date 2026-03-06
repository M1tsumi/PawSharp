// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §7.2 — LeafNode.
///
/// Represents a group member's entry in the MLS ratchet tree.  It bundles the
/// member's HPKE encryption key, Ed25519 signature key, credential, and
/// capabilities — all signed by the member's signature key.
///
/// For DAVE the capabilities list is fixed to the single supported ciphersuite.
/// </summary>
internal sealed class LeafNode
{
    // ── Fields ────────────────────────────────────────────────────────────────

    /// <summary>X25519 HPKE public key (32 bytes).</summary>
    public byte[] EncryptionKey { get; }

    /// <summary>Ed25519 signing public key (32 bytes).</summary>
    public byte[] SignatureKey { get; }

    /// <summary>Member identity credential.</summary>
    public Credential Credential { get; }

    /// <summary>Lifecycle source of this leaf node (KeyPackage, Update, or Commit).</summary>
    public LeafNodeSource Source { get; }

    /// <summary>
    /// Ed25519 signature over the serialised leaf node content
    /// (everything except the signature field itself).
    /// </summary>
    public byte[] Signature { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────

    private LeafNode(byte[] encKey, byte[] sigKey, Credential cred, LeafNodeSource src, byte[] sig)
    {
        EncryptionKey = encKey;
        SignatureKey  = sigKey;
        Credential    = cred;
        Source        = src;
        Signature     = sig;
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a new LeafNode with fresh X25519 and Ed25519 key pairs,
    /// signed by the generated Ed25519 private key.
    /// </summary>
    /// <param name="identity">The member's raw identity bytes (e.g. Discord user ID as UTF-8).</param>
    /// <param name="hpkePrivateKeyOut">Outputs the X25519 private key (caller must store securely).</param>
    /// <param name="sigPrivateKeyOut">Outputs the Ed25519 private seed (caller must store securely).</param>
    public static LeafNode Generate(
        byte[] identity,
        out byte[] hpkePrivateKeyOut,
        out byte[] sigPrivateKeyOut)
    {
        Curve25519.GenerateKeyPair(out hpkePrivateKeyOut, out var hpkePub);
        Ed25519.GenerateKeyPair(out sigPrivateKeyOut, out var sigPub);
        var cred   = Credential.Basic(identity);
        var node   = new LeafNode(hpkePub, sigPub, cred, LeafNodeSource.KeyPackage, Array.Empty<byte>());
        node.Signature = Ed25519.Sign(node.ToBeSigned(), sigPrivateKeyOut);
        return node;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    /// <summary>Encodes the full leaf node (including signature) as TLS bytes.</summary>
    public byte[] Encode()
    {
        using var w = new TlsWriter(100);
        w.WriteVector16(EncryptionKey);
        w.WriteVector16(SignatureKey);
        w.WriteBytes(Credential.Encode());
        w.WriteUint8((byte)Source);
        // Capabilities: just the one ciphersuite
        using var caps = new TlsWriter(4);
        caps.WriteUint16((ushort)CipherSuite.MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519);
        w.WriteNested16(caps);
        // Extensions: empty list
        w.WriteUint16(0);
        // Signature
        w.WriteVector16(Signature);
        return w.ToArray();
    }

    /// <summary>Decodes a LeafNode from TLS bytes.</summary>
    public static LeafNode Decode(ReadOnlySpan<byte> data)
    {
        var r      = new TlsReader(data);
        var encKey = r.ReadVector16();
        var sigKey = r.ReadVector16();

        // Decode credential inline (type + variable payload)
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
        r.ReadVector16(); // capabilities — skip
        r.ReadVector16(); // extensions   — skip
        var sig    = r.ReadVector16();
        return new LeafNode(encKey, sigKey, cred, source, sig);
    }

    /// <summary>Verifies the leaf node's self-signature (RFC 9420 §7.3).</summary>
    public bool VerifySignature()
        => Ed25519.Verify(ToBeSigned(), Signature, SignatureKey);

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>The byte sequence over which the signature is computed (all fields except signature).</summary>
    private byte[] ToBeSigned()
    {
        using var w = new TlsWriter(100);
        w.WriteVector16(EncryptionKey);
        w.WriteVector16(SignatureKey);
        w.WriteBytes(Credential.Encode());
        w.WriteUint8((byte)Source);
        using var caps = new TlsWriter(4);
        caps.WriteUint16((ushort)CipherSuite.MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519);
        w.WriteNested16(caps);
        w.WriteUint16(0); // extensions
        return w.ToArray();
    }
}
