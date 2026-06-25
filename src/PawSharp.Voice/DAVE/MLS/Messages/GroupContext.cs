// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §8.1 / §8.3 — GroupContext.
///
/// The GroupContext binds all group state into a compact descriptor used as
/// additional authenticated data throughout MLS (HPKE, transcript hashes).
///
/// struct GroupContext {
///   ProtocolVersion  version;        // uint16
///   CipherSuite      cipher_suite;   // uint16
///   opaque           group_id&lt;V&gt;;    // variable
///   uint64           epoch;
///   opaque           tree_hash&lt;V&gt;;   // SHA-256 of ratchet tree
///   opaque           confirmed_transcript_hash&lt;V&gt;;
///   Extension        extensions&lt;V&gt;;  // empty for DAVE
/// }
/// </summary>
internal sealed class GroupContext
{
    public ProtocolVersion Version    { get; } = ProtocolVersion.Mls10;
    public CipherSuite Suite          { get; } = CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256;

    /// <summary>The MLS group identifier (arbitrary bytes assigned by the creator).</summary>
    public byte[] GroupId { get; }

    /// <summary>Current epoch number.</summary>
    public ulong Epoch { get; }

    /// <summary>Tree hash of the current ratchet tree (32 bytes, SHA-256).</summary>
    public byte[] TreeHash { get; }

    /// <summary>Confirmed transcript hash (32 bytes, SHA-256).</summary>
    public byte[] ConfirmedTranscriptHash { get; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public GroupContext(
        byte[] groupId,
        ulong epoch,
        byte[] treeHash,
        byte[] confirmedTranscriptHash)
    {
        GroupId                  = groupId;
        Epoch                    = epoch;
        TreeHash                 = treeHash;
        ConfirmedTranscriptHash  = confirmedTranscriptHash;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    /// <summary>Encodes the GroupContext as TLS bytes.</summary>
    public byte[] Encode()
    {
        using var w = new TlsWriter(128);
        w.WriteUint16((ushort)Version);
        w.WriteUint16((ushort)Suite);
        w.WriteVector32(GroupId);
        w.WriteUint64(Epoch);
        w.WriteVector32(TreeHash);
        w.WriteVector32(ConfirmedTranscriptHash);
        w.WriteUint32(0); // extensions count = 0
        return w.ToArray();
    }

    /// <summary>Decodes a GroupContext from TLS bytes.</summary>
    public static GroupContext Decode(ReadOnlySpan<byte> data)
    {
        var r      = new TlsReader(data);
        r.ReadUint16(); // version
        r.ReadUint16(); // suite
        var gid    = r.ReadVector32();
        var epoch  = r.ReadUint64();
        var tHash  = r.ReadVector32();
        var ctHash = r.ReadVector32();
        r.ReadUint32(); // extensions count — consume for symmetrical encode/decode
        return new GroupContext(gid, epoch, tHash, ctHash);
    }
}
