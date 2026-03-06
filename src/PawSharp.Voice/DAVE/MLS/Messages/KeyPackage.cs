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
///   - An Ed25519 signature over the whole structure
///
/// Discord DAVE uses ciphersuite 0x0001 only.
/// </summary>
internal sealed class KeyPackage
{
    // ── Properties ────────────────────────────────────────────────────────────

    public ProtocolVersion Version    { get; } = ProtocolVersion.Mls10;
    public CipherSuite     Suite      { get; } = CipherSuite.MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519;

    /// <summary>X25519 init public key (different from the leaf HPKE key, used once per Welcome).</summary>
    public byte[] InitKey  { get; }

    /// <summary>The member LeafNode embedded in this KeyPackage.</summary>
    public LeafNode Leaf   { get; }

    /// <summary>Ed25519 signature over the TBS (to-be-signed) content.</summary>
    public byte[] Signature { get; private set; }

    // ── Private key material (not serialised) ──────────────────────────────────

    /// <summary>X25519 private key corresponding to <see cref="InitKey"/>.</summary>
    public byte[]? InitPrivateKey { get; }

    /// <summary>X25519 private key embedded in the leaf node.</summary>
    public byte[]? LeafHpkePrivateKey { get; }

    /// <summary>Ed25519 private seed embedded in the leaf node.</summary>
    public byte[]? LeafSignPrivateKey { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

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

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a complete, self-signed KeyPackage.
    /// </summary>
    /// <param name="identity">The caller's identity bytes (Discord user ID as UTF-8).</param>
    public static KeyPackage Generate(byte[] identity)
    {
        // Generate init key pair (separate from the leaf HPKE key per RFC 9420 §10)
        Curve25519.GenerateKeyPair(out var initPriv, out var initPub);

        // Generate the leaf node (has its own HPKE + signing key pair)
        var leaf = LeafNode.Generate(identity, out var leafHpkePriv, out var leafSigPriv);

        var kp = new KeyPackage(initPub, leaf, Array.Empty<byte>(), initPriv, leafHpkePriv, leafSigPriv);
        kp.Signature = Ed25519.Sign(kp.ToBeSigned(), leafSigPriv);
        return kp;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    /// <summary>Encodes the KeyPackage as TLS bytes (for sending to Discord).</summary>
    public byte[] Encode()
    {
        using var w = new TlsWriter(200);
        w.WriteUint16((ushort)Version);
        w.WriteUint16((ushort)Suite);
        w.WriteVector16(InitKey);
        w.WriteBytes(Leaf.Encode());
        // Extensions: empty
        w.WriteUint16(0);
        w.WriteVector16(Signature);
        return w.ToArray();
    }

    /// <summary>Decodes a KeyPackage from TLS bytes.</summary>
    public static KeyPackage Decode(ReadOnlySpan<byte> data)
    {
        var r       = new TlsReader(data);
        var version = (ProtocolVersion)r.ReadUint16();
        var suite   = (CipherSuite)r.ReadUint16();

        if (version != ProtocolVersion.Mls10)
            throw new MlsDecodeException($"Unsupported MLS version: {version}");
        if (suite != CipherSuite.MLS_128_DHKEMX25519_AES128GCM_SHA256_Ed25519)
            throw new MlsDecodeException($"Unsupported ciphersuite: {suite}");

        var initKey = r.ReadVector16();
        var leaf    = LeafNode.Decode(data.Slice(r.Position));
        // Advance past the leaf bytes (re-read length from the encoded form)
        var leafBytes = leaf.Encode();
        var r2 = new TlsReader(data.Slice(r.Position + leafBytes.Length));
        r2.ReadVector16(); // extensions
        var sig = r2.ReadVector16();

        return new KeyPackage(initKey, leaf, sig);
    }

    /// <summary>Verifies the KeyPackage self-signature.</summary>
    public bool VerifySignature()
        => Ed25519.Verify(ToBeSigned(), Signature, Leaf.SignatureKey);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private byte[] ToBeSigned()
    {
        using var w = new TlsWriter(200);
        w.WriteUint16((ushort)Version);
        w.WriteUint16((ushort)Suite);
        w.WriteVector16(InitKey);
        w.WriteBytes(Leaf.Encode());
        w.WriteUint16(0); // extensions
        return w.ToArray();
    }
}
