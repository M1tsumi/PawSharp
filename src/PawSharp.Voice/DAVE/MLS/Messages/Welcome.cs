#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Tree;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §12.4.3.1 — GroupInfo.
///
/// Sent inside a Welcome message to communicate the current group state
/// (GroupContext + ratchet tree + confirmation tag) to new joiners.
/// </summary>
internal sealed class GroupInfo
{
    public GroupContext Context { get; }

    /// <summary>Confirmation tag: HMAC-SHA256(confirmation_key, confirmed_transcript_hash).</summary>
    public byte[] ConfirmationTag { get; }

    /// <summary>The index of the signer leaf (the committer who produced this Welcome).</summary>
    public uint SignerLeafIndex { get; }

    /// <summary>Ed25519 signature by the signer's leaf key over the GroupInfo TBS.</summary>
    public byte[] Signature { get; }

    public GroupInfo(
        GroupContext ctx,
        byte[] confirmationTag,
        uint signerLeafIndex,
        byte[] signature)
    {
        Context          = ctx;
        ConfirmationTag  = confirmationTag;
        SignerLeafIndex  = signerLeafIndex;
        Signature        = signature;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    public byte[] Encode()
    {
        using var w = new TlsWriter(256);
        w.WriteBytes(Context.Encode());
        w.WriteVector32(ConfirmationTag);
        w.WriteUint32(SignerLeafIndex);
        // Extensions: empty
        w.WriteUint32(0);
        w.WriteVector16(Signature);
        return w.ToArray();
    }

    public static GroupInfo Decode(ReadOnlySpan<byte> data)
    {
        var r        = new TlsReader(data);
        var ctx      = GroupContext.Decode(data);

        // Re-position after GroupContext (encoded size)
        var ctxBytes = ctx.Encode();
        var r2       = new TlsReader(data.Slice(ctxBytes.Length));

        var ctag     = r2.ReadVector32();
        var signer   = r2.ReadUint32();
        r2.ReadUint32();            // extension count — skip
        var sig      = r2.ReadVector16();

        return new GroupInfo(ctx, ctag, signer, sig);
    }
}

/// <summary>
/// RFC 9420 §12.4.3.1 — GroupSecrets.
///
/// The shared secrets encrypted per-recipient in a Welcome message.
/// </summary>
internal sealed class GroupSecrets
{
    /// <summary>
    /// The joiner_secret for this epoch (must equal DeriveSecret(init_secret, "joiner")).
    /// </summary>
    public byte[] JoinerSecret { get; }

    /// <summary>Optional pre-shared key reference (not used by DAVE).</summary>
    public byte[]? PathSecret { get; }

    public GroupSecrets(byte[] joinerSecret, byte[]? pathSecret = null)
    {
        JoinerSecret = joinerSecret;
        PathSecret   = pathSecret;
    }

    public byte[] Encode()
    {
        using var w = new TlsWriter(64);
        w.WriteVector32(JoinerSecret);
        if (PathSecret != null)
        {
            w.WriteUint8(1);
            w.WriteVector32(PathSecret);
        }
        else
        {
            w.WriteUint8(0);
        }
        // PSK: empty list
        w.WriteUint32(0);
        return w.ToArray();
    }

    public static GroupSecrets Decode(ReadOnlySpan<byte> data)
    {
        var r          = new TlsReader(data);
        var joiner     = r.ReadVector32();
        var hasPath    = r.ReadUint8() != 0;
        byte[]? path   = hasPath ? r.ReadVector32() : null;
        return new GroupSecrets(joiner, path);
    }
}

/// <summary>
/// RFC 9420 §12.4.3 — Welcome message.
///
/// Sent to new group members (from opcode 25).  Contains:
///   1. A vector of EncryptedGroupSecrets — one per invited member, each encrypted
///      with the recipient's HPKE init key from their KeyPackage.
///   2. The GroupInfo encrypted with a key derived from the welcome_secret.
/// </summary>
internal sealed class WelcomeMessage
{
    public CipherSuite Suite { get; }

    /// <summary>Per-recipient HPKE-encrypted GroupSecrets.</summary>
    public IReadOnlyList<EncryptedGroupSecrets> Secrets { get; }

    /// <summary>GroupInfo encrypted with the welcome_secret-derived key.</summary>
    public byte[] EncryptedGroupInfo { get; }

    public WelcomeMessage(
        CipherSuite suite,
        IReadOnlyList<EncryptedGroupSecrets> secrets,
        byte[] encryptedGroupInfo)
    {
        Suite               = suite;
        Secrets             = secrets;
        EncryptedGroupInfo  = encryptedGroupInfo;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    public byte[] Encode()
    {
        using var w = new TlsWriter(512);
        w.WriteUint16((ushort)Suite);

        w.WriteUint32((uint)Secrets.Count);
        foreach (var s in Secrets)
            w.WriteBytes(s.Encode());

        w.WriteVector32(EncryptedGroupInfo);
        return w.ToArray();
    }

    public static WelcomeMessage Decode(ReadOnlySpan<byte> data)
    {
        var r     = new TlsReader(data);
        var suite = (CipherSuite)r.ReadUint16();
        var count = r.ReadUint32();

        var secrets = new List<EncryptedGroupSecrets>((int)count);
        for (uint i = 0; i < count; i++)
            secrets.Add(EncryptedGroupSecrets.Decode(data.Slice(r.Position)));

        // Re-read position after all secrets (need to re-advance r)
        // Use a fresh reader starting at the right offset
        foreach (var s in secrets)
        {
            var encoded = s.Encode();
            _ = encoded; // advance via encoded length in real impl
        }

        // Encode each to find the real offset (simple approach: re-read linearly)
        // For correctness, decode from scratch keeping position
        var r2     = new TlsReader(data);
        r2.ReadUint16();
        uint n     = r2.ReadUint32();
        var secs2  = new List<EncryptedGroupSecrets>((int)n);
        for (uint i = 0; i < n; i++)
        {
            var s     = EncryptedGroupSecrets.DecodeAdvancing(ref r2, data);
            secs2.Add(s);
        }
        var eGroupInfo = r2.ReadVector32();
        return new WelcomeMessage(suite, secs2, eGroupInfo);
    }

    // ── Process helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Finds this client's EncryptedGroupSecrets entry and decrypts it using the
    /// provided HPKE init private key.
    /// </summary>
    /// <param name="initPrivateKey">32-byte X25519 private key from the matching KeyPackage.</param>
    /// <param name="keyPackageRef">
    ///   Hash of the KeyPackage used to find the right entry (RFC 9420 §12.4.3.1).
    /// </param>
    /// <param name="groupContext">GroupContext bytes, used as HPKE info.</param>
    /// <returns>Decrypted <see cref="GroupSecrets"/>.</returns>
    public GroupSecrets? TryDecryptSecrets(
        ReadOnlySpan<byte> initPrivateKey,
        ReadOnlySpan<byte> keyPackageRef,
        ReadOnlySpan<byte> groupContext)
    {
        foreach (var entry in Secrets)
        {
            // Match by KeyPackageRef hash
            if (!KeyPackageRefEqual(entry.KeyPackageRef, keyPackageRef)) continue;

            var plain = HpkeX25519.OpenBase(
                initPrivateKey,
                entry.EncryptedSecret.Enc,
                groupContext,
                ReadOnlySpan<byte>.Empty,
                entry.EncryptedSecret.CipherText);

            return GroupSecrets.Decode(plain);
        }
        return null;
    }

    private static bool KeyPackageRefEqual(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}

/// <summary>One recipient's entry in a Welcome message.</summary>
internal sealed class EncryptedGroupSecrets
{
    /// <summary>
    /// Hash of the recipient's KeyPackage, used to match the right entry.
    /// RFC 9420 §12.4.3.1: ref = RefHash("MLS 1.0 KeyPackage", kp_bytes)
    /// </summary>
    public byte[] KeyPackageRef { get; }

    /// <summary>HPKE-encrypted GroupSecrets payload.</summary>
    public HpkeCiphertext EncryptedSecret { get; }

    public EncryptedGroupSecrets(byte[] keyPackageRef, HpkeCiphertext encryptedSecret)
    {
        KeyPackageRef   = keyPackageRef;
        EncryptedSecret = encryptedSecret;
    }

    public byte[] Encode()
    {
        using var w = new TlsWriter(80);
        w.WriteVector32(KeyPackageRef);
        w.WriteVector16(EncryptedSecret.Enc);
        w.WriteVector32(EncryptedSecret.CipherText);
        return w.ToArray();
    }

    public static EncryptedGroupSecrets Decode(ReadOnlySpan<byte> data)
    {
        var r   = new TlsReader(data);
        var kpr = r.ReadVector32();
        var enc = r.ReadVector16();
        var ct  = r.ReadVector32();
        return new EncryptedGroupSecrets(kpr, new HpkeCiphertext(enc, ct));
    }

    // Advances the ref-struct TlsReader past this entry
    internal static EncryptedGroupSecrets DecodeAdvancing(ref TlsReader r, ReadOnlySpan<byte> fullData)
    {
        var kpr = r.ReadVector32();
        var enc = r.ReadVector16();
        var ct  = r.ReadVector32();
        return new EncryptedGroupSecrets(kpr, new HpkeCiphertext(enc, ct));
    }
}
