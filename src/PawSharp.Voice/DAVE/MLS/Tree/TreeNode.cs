// Copyright (c) 2025 quefep. All rights reserved.
// PawSharp implementation of Discord's DAVE end-to-end encryption protocol.
// Attribution is required for any derivative use. See LICENSE.

#nullable enable
using System;

namespace PawSharp.Voice.DAVE.MLS.Tree;

/// <summary>
/// A node in the MLS ratchet tree (RFC 9420 §7).
///
/// Each node holds:
///   - An HPKE public key (and optionally the corresponding private key for leaf nodes
///     owned by this client).
///   - For leaf nodes: the member's <see cref="Credential"/> (identity material).
///   - A blank flag: blank nodes have no key material and represent absent tree positions.
///
/// The tree uses projective x-only coordinates (Curve25519).
/// Key sizes are Curve25519: 32 bytes.
/// </summary>
internal sealed class TreeNode
{
    // ── Constants ─────────────────────────────────────────────────────────────

    public const int HpkeKeySize = 32; // P-256 key size

    // ── Properties ───────────────────────────────────────────────────────────

    /// <summary>True when this node has been blanked (removed member or intermediate blank).</summary>
    public bool IsBlank { get; private set; }

    /// <summary>True when this is a leaf node (nodeIndex is even).</summary>
    public bool IsLeaf => (NodeIndex & 1) == 0;

    /// <summary>The node's position in the in-order indexing scheme.</summary>
    public uint NodeIndex { get; }

    /// <summary>HPKE public key for this node (65 bytes, P-256).</summary>
    public byte[]? HpkePublicKey { get; private set; }

    /// <summary>HPKE private key (only set for the local leaf node, 32 bytes).</summary>
    public byte[]? HpkePrivateKey { get; private set; }

    /// <summary>The member credential — only meaningful for (non-blank) leaf nodes.</summary>
    public byte[]? Credential { get; private set; }

    /// <summary>
    /// The signature public key (P-256 ECDSA, 65 bytes) for verifying leaf node signatures.
    /// Only meaningful for leaf nodes.
    /// </summary>
    public byte[]? SignatureKey { get; private set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    private TreeNode(uint nodeIndex)
    {
        NodeIndex = nodeIndex;
        IsBlank   = true;
    }

    /// <summary>Creates a blank node at the given tree index.</summary>
    public static TreeNode CreateBlank(uint nodeIndex) => new TreeNode(nodeIndex);

    /// <summary>
    /// Creates a populated leaf node from wire-format fields.
    /// </summary>
    public static TreeNode CreateLeaf(
        uint      nodeIndex,
        byte[]    hpkePublicKey,
        byte[]?   hpkePrivateKey,
        byte[]    signatureKey,
        byte[]    credential)
    {
        if ((nodeIndex & 1) != 0)
            throw new ArgumentException("Leaf node must have an even index.", nameof(nodeIndex));

        return new TreeNode(nodeIndex)
        {
            IsBlank        = false,
            HpkePublicKey  = hpkePublicKey,
            HpkePrivateKey = hpkePrivateKey,
            SignatureKey   = signatureKey,
            Credential     = credential,
        };
    }

    /// <summary>
    /// Creates a populated internal (parent) node from a path secret.
    /// The HPKE public key is derived from the path secret during Commit processing.
    /// </summary>
    public static TreeNode CreateParent(uint nodeIndex, byte[] hpkePublicKey)
    {
        if ((nodeIndex & 1) == 0)
            throw new ArgumentException("Parent node must have an odd index.", nameof(nodeIndex));

        return new TreeNode(nodeIndex)
        {
            IsBlank       = false,
            HpkePublicKey = hpkePublicKey,
        };
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    /// <summary>Blanks this node, clearing all key material.</summary>
    public void Blank()
    {
        IsBlank        = true;
        HpkePublicKey  = null;
        HpkePrivateKey = null;
        Credential     = null;
        SignatureKey   = null;
    }

    /// <summary>Sets the HPKE key pair (used when merging an UpdatePath).</summary>
    public void SetHpkeKeys(byte[] publicKey, byte[]? privateKey = null)
    {
        HpkePublicKey  = publicKey;
        HpkePrivateKey = privateKey;
        IsBlank        = false;
    }
}
