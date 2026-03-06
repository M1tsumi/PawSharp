#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Voice.DAVE.MLS.Encoding;
using PawSharp.Voice.DAVE.MLS.Tree;

namespace PawSharp.Voice.DAVE.MLS.Messages;

/// <summary>
/// RFC 9420 §12.1 — MLS Proposal.
///
/// A proposal describes a pending modification to the group state.
/// DAVE primarily uses Add, Remove, and Update proposals.
/// Proposals are collected and applied atomically by the next Commit.
/// </summary>
internal sealed class Proposal
{
    public ProposalType Type { get; }

    // Add
    public KeyPackage? AddKeyPackage { get; }

    // Update
    public LeafNode? UpdateLeafNode { get; }

    // Remove
    public uint? RemoveLeafIndex { get; }

    private Proposal(ProposalType type,
        KeyPackage? kp = null,
        LeafNode? leaf = null,
        uint? removeIdx = null)
    {
        Type            = type;
        AddKeyPackage   = kp;
        UpdateLeafNode  = leaf;
        RemoveLeafIndex = removeIdx;
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static Proposal Add(KeyPackage kp)         => new Proposal(ProposalType.Add,    kp:   kp);
    public static Proposal Update(LeafNode leaf)      => new Proposal(ProposalType.Update, leaf: leaf);
    public static Proposal Remove(uint leafIndex)     => new Proposal(ProposalType.Remove, removeIdx: leafIndex);

    // ── Serialisation ─────────────────────────────────────────────────────────

    public byte[] Encode()
    {
        using var w = new TlsWriter(256);
        w.WriteUint16((ushort)Type);

        switch (Type)
        {
            case ProposalType.Add:
                w.WriteBytes(AddKeyPackage!.Encode());
                break;
            case ProposalType.Update:
                w.WriteBytes(UpdateLeafNode!.Encode());
                break;
            case ProposalType.Remove:
                w.WriteUint32(RemoveLeafIndex!.Value);
                break;
        }

        return w.ToArray();
    }

    public static Proposal Decode(ReadOnlySpan<byte> data)
    {
        var r    = new TlsReader(data);
        var type = (ProposalType)r.ReadUint16();
        int rem  = r.Remaining;

        switch (type)
        {
            case ProposalType.Add:
            {
                var kp = KeyPackage.Decode(data.Slice(r.Position));
                return new Proposal(type, kp: kp);
            }
            case ProposalType.Update:
            {
                var leaf = LeafNode.Decode(data.Slice(r.Position));
                return new Proposal(type, leaf: leaf);
            }
            case ProposalType.Remove:
            {
                var idx = r.ReadUint32();
                return new Proposal(type, removeIdx: idx);
            }
            default:
                throw new MlsDecodeException($"Unsupported proposal type: {type}");
        }
    }
}

/// <summary>
/// RFC 9420 §12.4 — MLS Commit.
///
/// A Commit applies a list of proposals and optionally supplies a new UpdatePath
/// to advance the epoch secret.  After a Commit the epoch number increments.
/// </summary>
internal sealed class Commit
{
    /// <summary>List of proposal references (by value) included in this Commit.</summary>
    public IReadOnlyList<Proposal> Proposals { get; }

    /// <summary>
    /// UpdatePath: one entry per direct-path node above the committer's leaf,
    /// containing the new HPKE key and encrypted path secrets.
    /// May be null for commits that only contain Remove proposals.
    /// </summary>
    public IReadOnlyList<UpdatePathNode>? UpdatePath { get; }

    public Commit(IReadOnlyList<Proposal> proposals, IReadOnlyList<UpdatePathNode>? path = null)
    {
        Proposals  = proposals;
        UpdatePath = path;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    public byte[] Encode()
    {
        using var w = new TlsWriter(512);

        // proposals vector (4-byte length prefix)
        using var propsW = new TlsWriter(256);
        foreach (var p in Proposals)
        {
            var pb = p.Encode();
            propsW.WriteVector32(pb);
        }
        w.WriteNested32(propsW);

        // optional UpdatePath
        if (UpdatePath != null)
        {
            w.WriteUint8(1); // present
            using var pathW = new TlsWriter(256);
            foreach (var n in UpdatePath)
            {
                pathW.WriteVector16(n.PublicKey);
                pathW.WriteUint32((uint)n.EncryptedPathSecrets.Count);
                foreach (var ct in n.EncryptedPathSecrets)
                {
                    pathW.WriteVector16(ct.Enc);
                    pathW.WriteVector32(ct.CipherText);
                }
            }
            w.WriteNested32(pathW);
        }
        else
        {
            w.WriteUint8(0); // absent
        }

        return w.ToArray();
    }

    public static Commit Decode(ReadOnlySpan<byte> data)
    {
        var r = new TlsReader(data);

        // Proposals
        var proposals = new List<Proposal>();
        var propsLen  = (int)r.ReadUint32();
        var propsSlice = r.Slice(propsLen);
        while (!propsSlice.IsEmpty)
        {
            int pLen = (int)propsSlice.ReadUint32();
            proposals.Add(Proposal.Decode(propsSlice.ReadBytes(pLen)));
        }

        // UpdatePath
        var hasPath = r.ReadUint8() != 0;
        List<UpdatePathNode>? updatePath = null;
        if (hasPath)
        {
            updatePath          = new List<UpdatePathNode>();
            var pathLen         = (int)r.ReadUint32();
            var pathSlice       = r.Slice(pathLen);
            while (!pathSlice.IsEmpty)
            {
                var pubKey = pathSlice.ReadVector16();
                var ctCount = pathSlice.ReadUint32();
                var ciphertexts = new List<HpkeCiphertext>((int)ctCount);
                for (uint i = 0; i < ctCount; i++)
                {
                    var enc = pathSlice.ReadVector16();
                    var ct  = pathSlice.ReadVector32();
                    ciphertexts.Add(new HpkeCiphertext(enc, ct));
                }
                updatePath.Add(new UpdatePathNode(pubKey, ciphertexts));
            }
        }

        return new Commit(proposals, updatePath);
    }
}
