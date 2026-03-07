// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;
using System.Collections.Generic;
using PawSharp.Voice.DAVE.MLS.Crypto;
using PawSharp.Voice.DAVE.MLS.Encoding;

namespace PawSharp.Voice.DAVE.MLS.Tree;

/// <summary>
/// RFC 9420 §7 — MLS Ratchet Tree.
///
/// Manages the left-balanced binary tree of HPKE nodes used by TreeKEM to
/// establish shared secrets among all group members.  Each leaf holds a
/// member's HPKE public key; internal nodes hold derived keys used to
/// propagate secrets down the tree during Commit processing.
///
/// Key operations:
///   1. <see cref="AddLeaf"/>   — join a new member (from a KeyPackage or Welcome).
///   2. <see cref="BlankPath"/> — blank leaf + ancestor path on Remove.
///   3. <see cref="MergeUpdatePath"/> — apply a Commit's UpdatePath to advance epoch.
///   4. <see cref="TreeHash"/>  — compute the RFC 9420 tree hash (used in GroupContext).
/// </summary>
internal sealed class RatchetTree
{
    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>All nodes, indexed by in-order index (0 = first leaf, 1 = root of 2-leaf tree, …).</summary>
    private TreeNode[] _nodes;

    /// <summary>Number of leaves currently in the tree.</summary>
    public uint LeafCount { get; private set; }

    /// <summary>Set of blank node indices (for resolution queries).</summary>
    private readonly HashSet<uint> _blank = new();

    /// <summary>The (even) node index of the local client's leaf, or null if not set.</summary>
    public uint? LocalLeafIndex { get; private set; }

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>Initialises an empty ratchet tree.</summary>
    public RatchetTree()
    {
        _nodes    = Array.Empty<TreeNode>();
        LeafCount = 0;
    }

    // ── Leaf management ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new leaf at the next available position with the given HPKE key and credential.
    /// Returns the leaf node index (even).
    /// </summary>
    public uint AddLeaf(byte[] hpkePublicKey, byte[] signatureKey, byte[] credential,
                        byte[]? hpkePrivateKey = null, bool isLocal = false)
    {
        uint leafIndex = LeafCount;
        uint nodeIndex = TreeMath.LeafToNode(leafIndex);
        LeafCount++;
        EnsureCapacity();

        var node = TreeNode.CreateLeaf(nodeIndex, hpkePublicKey, hpkePrivateKey, signatureKey, credential);
        _nodes[nodeIndex] = node;
        _blank.Remove(nodeIndex);

        if (isLocal) LocalLeafIndex = nodeIndex;
        return nodeIndex;
    }

    /// <summary>
    /// Replaces the HPKE public key on a leaf node (Update proposal, RFC 9420 §12.1.2).
    /// </summary>
    /// <param name="leafNodeIndex">Even in-order node index of the leaf to update.</param>
    /// <param name="newHpkePublicKey">32-byte replacement X25519 public key.</param>
    public void ReplaceLeafHpkeKey(uint leafNodeIndex, byte[] newHpkePublicKey)
    {
        if ((leafNodeIndex & 1) != 0)
            throw new ArgumentException("Expected even leaf node index.", nameof(leafNodeIndex));
        if (leafNodeIndex >= _nodes.Length || _nodes[leafNodeIndex].IsBlank)
            return;

        var old = _nodes[leafNodeIndex];
        _nodes[leafNodeIndex] = TreeNode.CreateLeaf(
            leafNodeIndex,
            newHpkePublicKey,
            old.NodeIndex == LocalLeafIndex ? old.HpkePrivateKey : null,
            old.SignatureKey  ?? Array.Empty<byte>(),
            old.Credential    ?? Array.Empty<byte>());

        // Blank the direct path so ancestors receive fresh keys from the next Commit
        var directPath = TreeMath.DirectPath(leafNodeIndex, LeafCount);
        foreach (var idx in directPath)
        {
            if (idx < _nodes.Length)
            {
                _nodes[idx].Blank();
                _blank.Add(idx);
            }
        }
    }

    /// <summary>
    /// Finds the in-order node index of the first leaf whose credential bytes match
    /// <paramref name="identity"/>. Returns null when not found.
    /// </summary>
    public uint? FindLeafByCredential(ReadOnlySpan<byte> identity)
    {
        for (uint i = 0; i < LeafCount; i++)
        {
            uint nodeIdx = TreeMath.LeafToNode(i);
            if (nodeIdx < (uint)_nodes.Length && !_nodes[nodeIdx].IsBlank)
            {
                var cred = _nodes[nodeIdx].Credential;
                if (cred != null && identity.SequenceEqual(cred))
                    return nodeIdx;
            }
        }
        return null;
    }

    /// <summary>
    /// Blanks the direct path from a leaf up to (but not including) the root.
    /// Used when removing a member (RFC 9420 §12.4 step 2).
    /// </summary>
    public void BlankPath(uint leafNodeIndex)
    {
        if ((leafNodeIndex & 1) != 0)
            throw new ArgumentException("Expected even leaf node index.", nameof(leafNodeIndex));

        _nodes[leafNodeIndex].Blank();
        _blank.Add(leafNodeIndex);

        var directPath = TreeMath.DirectPath(leafNodeIndex, LeafCount);
        foreach (var idx in directPath)
        {
            if (idx < _nodes.Length && !_nodes[idx].IsBlank)
            {
                _nodes[idx].Blank();
                _blank.Add(idx);
            }
        }
    }

    // ── UpdatePath processing (Commit) ────────────────────────────────────────

    /// <summary>
    /// Applies a Commit's UpdatePath to the tree, deriving the new commit secret
    /// from the path secret decryptable by the local leaf.
    ///
    /// RFC 9420 §12.4 step 4.
    /// </summary>
    /// <param name="senderLeafIndex">Leaf index (NOT node index) of the committer.</param>
    /// <param name="updatePath">
    ///   Sequence of (HPKE public key, encrypted path secrets) for each level on the
    ///   sender's direct path.  Each entry's encrypted list is indexed by the copath
    ///   resolution nodes.
    /// </param>
    /// <param name="groupContext">The GroupContext bytes used as AAD for HPKE.</param>
    /// <returns>
    ///   The commit secret (path secret at root level) if the local key can decrypt it;
    ///   otherwise the path secret is from an unrelated subtree and the caller must
    ///   have received it via Welcome.
    /// </returns>
    public byte[]? MergeUpdatePath(
        uint senderLeafIndex,
        IReadOnlyList<UpdatePathNode> updatePath,
        ReadOnlySpan<byte> groupContext)
    {
        uint senderNodeIndex = TreeMath.LeafToNode(senderLeafIndex);
        var directPath = TreeMath.DirectPath(senderNodeIndex, LeafCount);
        var coPath     = TreeMath.CoPath(senderNodeIndex, LeafCount);

        byte[]? pathSecret = null;

        for (int i = 0; i < updatePath.Count && i < directPath.Length; i++)
        {
            uint parentIdx  = directPath[i];
            uint cophathIdx = coPath[i];

            // Update the parent node's public key
            EnsureCapacity();
            _nodes[parentIdx] = TreeNode.CreateParent(parentIdx, updatePath[i].PublicKey);
            _blank.Remove(parentIdx);

            // Try to decrypt our path secret if our leaf is in the copath resolution
            if (pathSecret == null && LocalLeafIndex.HasValue)
            {
                var resolution = TreeMath.Resolution(cophathIdx, LeafCount, _blank);
                for (int r = 0; r < resolution.Length; r++)
                {
                    uint resNode = resolution[r];
                    if (resNode == LocalLeafIndex.Value)
                    {
                        var localNode = _nodes[LocalLeafIndex.Value];
                        if (localNode.HpkePrivateKey != null && r < updatePath[i].EncryptedPathSecrets.Count)
                        {
                            try
                            {
                                pathSecret = HpkeX25519.OpenBase(
                                    localNode.HpkePrivateKey,
                                    updatePath[i].EncryptedPathSecrets[r].Enc,
                                    groupContext,
                                    ReadOnlySpan<byte>.Empty,
                                    updatePath[i].EncryptedPathSecrets[r].CipherText);
                            }
                            catch (System.Security.Cryptography.CryptographicException)
                            {
                                // Not intended for this node or corrupted ciphertext
                            }
                        }
                        break;
                    }
                }
            }

            // Ratchet path secret if we have it
            if (pathSecret != null && i + 1 < directPath.Length)
                pathSecret = MlsHkdf.DeriveSecret(pathSecret, "path");
        }

        return pathSecret;
    }

    // ── Tree hash ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes the RFC 9420 §7.8 tree hash for the full tree.
    /// </summary>
    public byte[] TreeHash()
    {
        if (LeafCount == 0) return MlsHkdf.Hash(ReadOnlySpan<byte>.Empty);
        uint root = TreeMath.Root(LeafCount);
        return NodeHash(root);
    }

    private byte[] NodeHash(uint x)
    {
        if (x >= _nodes.Length || _nodes[x].IsBlank)
        {
            // Blank node: hash of a zero-byte "blank" struct
            using var w = new TlsWriter(2);
            w.WriteUint8(0); // blank flag
            return MlsHkdf.Hash(w.ToArray());
        }

        var node = _nodes[x];

        if (TreeMath.IsLeaf(x))
        {
            // Leaf node hash: hash of (1 || public_key || credential)
            using var w = new TlsWriter(70);
            w.WriteUint8(1); // leaf node present
            w.WriteVector16(node.HpkePublicKey ?? Array.Empty<byte>());
            w.WriteVector16(node.SignatureKey  ?? Array.Empty<byte>());
            w.WriteVector32(node.Credential    ?? Array.Empty<byte>());
            return MlsHkdf.Hash(w.ToArray());
        }
        else
        {
            // Internal node hash: hash of (1 || public_key || left_hash || right_hash)
            var leftHash  = NodeHash(TreeMath.Left(x));
            var rightHash = NodeHash(TreeMath.Right(x, LeafCount));
            using var w   = new TlsWriter(100);
            w.WriteUint8(1); // node present
            w.WriteVector16(node.HpkePublicKey ?? Array.Empty<byte>());
            w.WriteBytes(leftHash);
            w.WriteBytes(rightHash);
            return MlsHkdf.Hash(w.ToArray());
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureCapacity()
    {
        uint needed = TreeMath.NodeWidth(LeafCount);
        if (needed <= _nodes.Length) return;

        var old  = _nodes;
        _nodes   = new TreeNode[needed];
        for (uint i = 0; i < old.Length; i++) _nodes[i] = old[i];
        for (uint i = (uint)old.Length; i < needed; i++)
        {
            _nodes[i] = TreeNode.CreateBlank(i);
            _blank.Add(i);
        }
    }
}

/// <summary>
/// One node in an MLS UpdatePath (RFC 9420 §7.6).
/// Contains the new HPKE public key and a list of encrypted path secrets,
/// one per node in the copath resolution.
/// </summary>
internal sealed class UpdatePathNode
{
    public byte[] PublicKey { get; }
    public IReadOnlyList<HpkeCiphertext> EncryptedPathSecrets { get; }

    public UpdatePathNode(byte[] publicKey, IReadOnlyList<HpkeCiphertext> encryptedPathSecrets)
    {
        PublicKey             = publicKey;
        EncryptedPathSecrets  = encryptedPathSecrets;
    }
}

/// <summary>
/// An HPKE ciphertext (enc || ciphertext) carried in an UpdatePath or Welcome.
/// </summary>
internal sealed class HpkeCiphertext
{
    /// <summary>32-byte ephemeral encapsulated key.</summary>
    public byte[] Enc { get; }
    /// <summary>Encrypted payload (ciphertext + 16-byte tag).</summary>
    public byte[] CipherText { get; }

    public HpkeCiphertext(byte[] enc, byte[] cipherText)
    {
        Enc        = enc;
        CipherText = cipherText;
    }
}
