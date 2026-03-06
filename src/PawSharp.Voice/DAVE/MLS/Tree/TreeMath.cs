#nullable enable

namespace PawSharp.Voice.DAVE.MLS.Tree;

/// <summary>
/// RFC 9420 §7.1 — Left-balanced binary tree index arithmetic.
///
/// MLS uses a left-balanced binary tree (complete binary tree, left-subtree first)
/// to represent the ratchet tree.  Node indices are assigned by an in-order traversal:
///   - Leaf nodes occupy even indices: 0, 2, 4, ...
///   - Parent nodes occupy odd indices: 1, 3, 5, ...
///
/// For a tree of <c>n</c> leaves, there are <c>2n-1</c> total nodes.
///
/// All index arithmetic in this class follows RFC 9420 §7.1 exactly.
/// </summary>
internal static class TreeMath
{
    // ── Basic node classification ─────────────────────────────────────────────

    /// <summary>True when <paramref name="x"/> is a leaf node (even index).</summary>
    public static bool IsLeaf(uint x) => (x & 1) == 0;

    /// <summary>
    /// The level of node <paramref name="x"/> in the tree.
    /// Leaves are at level 0. Level increases by 1 for each parent step.
    /// Defined as: the number of trailing 1-bits in x.
    /// </summary>
    public static uint Level(uint x)
    {
        uint k = 0;
        while ((x & (1u << (int)k)) != 0) k++;
        return k;
    }

    // ── Tree size combinatorics ───────────────────────────────────────────────

    /// <summary>
    /// The total number of nodes in a tree with <paramref name="n"/> leaves.
    /// Formula: 2n - 1
    /// </summary>
    public static uint NodeWidth(uint n) => n == 0 ? 0 : 2 * n - 1;

    /// <summary>
    /// The index of the root node for a tree of <paramref name="n"/> leaves.
    /// </summary>
    public static uint Root(uint n)
    {
        uint w = NodeWidth(n);
        uint k = 0;
        while ((1u << (int)(k + 1)) < w) k++;
        return (1u << (int)k) - 1;
    }

    // ── Parent / child relationships ──────────────────────────────────────────

    /// <summary>
    /// Returns the left child of node <paramref name="x"/>.
    /// Requires <paramref name="x"/> to be an internal (non-leaf) node.
    /// </summary>
    public static uint Left(uint x)
    {
        uint k = Level(x);
        return x ^ (0x01u << (int)(k - 1));
    }

    /// <summary>
    /// Returns the right child of node <paramref name="x"/>.
    /// Requires <paramref name="x"/> to be an internal (non-leaf) node.
    /// </summary>
    public static uint Right(uint x, uint n)
    {
        uint k = Level(x);
        uint r = x ^ (0x03u << (int)(k - 1));
        while (r >= NodeWidth(n)) r = Left(r);
        return r;
    }

    /// <summary>
    /// Returns the parent of node <paramref name="x"/> in a tree of <paramref name="n"/> leaves.
    /// </summary>
    public static uint Parent(uint x, uint n)
    {
        if (x == Root(n))
            throw new System.ArgumentException("Root has no parent.", nameof(x));

        uint k = Level(x);
        uint b = (x >> (int)(k + 1)) & 1;
        return (x | (1u << (int)k)) ^ (b << (int)(k + 1));
    }

    /// <summary>
    /// Returns the sibling of node <paramref name="x"/> in a tree of <paramref name="n"/> leaves.
    /// </summary>
    public static uint Sibling(uint x, uint n)
    {
        uint p = Parent(x, n);
        return x < p ? Right(p, n) : Left(p);
    }

    // ── Path computations ─────────────────────────────────────────────────────

    /// <summary>
    /// The direct path of a leaf node: the sequence of ancestor nodes
    /// from the leaf's parent up to (but not including) the root.
    /// RFC 9420 §7.1
    /// </summary>
    public static uint[] DirectPath(uint x, uint n)
    {
        if (n == 0) return System.Array.Empty<uint>();

        uint r = Root(n);
        if (x == r) return System.Array.Empty<uint>();

        var path = new System.Collections.Generic.List<uint>();
        uint cur = x;
        while (cur != r)
        {
            cur = Parent(cur, n);
            path.Add(cur);
        }
        path.RemoveAt(path.Count - 1); // exclude root per RFC 9420 §7.1
        return path.ToArray();
    }

    /// <summary>
    /// The copath of a leaf node: for each node on the direct path,
    /// its sibling (the node that must be hashed to verify the tree).
    /// The copath has the same length as the direct path.
    /// </summary>
    public static uint[] CoPath(uint x, uint n)
    {
        var direct = DirectPath(x, n);
        var co     = new uint[direct.Length];
        uint cur = x;
        for (int i = 0; i < direct.Length; i++)
        {
            co[i] = Sibling(cur, n);
            cur   = direct[i];
        }
        return co;
    }

    // ── Leaf ↔ node index conversion ──────────────────────────────────────────

    /// <summary>Converts leaf index <paramref name="l"/> to its node index (2*l).</summary>
    public static uint LeafToNode(uint l) => l * 2;

    /// <summary>Converts node index <paramref name="x"/> to leaf index (x/2). Requires even x.</summary>
    public static uint NodeToLeaf(uint x) => x / 2;

    // ── Resolution ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the resolution of node <paramref name="x"/> in a tree of <paramref name="n"/> leaves:
    /// the set of non-blank descendants that cover the subtree.
    /// The blank list enumerates known-blank node indices.
    /// </summary>
    public static uint[] Resolution(uint x, uint n, System.Collections.Generic.ISet<uint> blank)
    {
        if (blank.Contains(x)) return System.Array.Empty<uint>();
        if (IsLeaf(x))         return new[] { x };

        var left  = Resolution(Left(x), n, blank);
        var right = Resolution(Right(x, n), n, blank);
        var result = new uint[left.Length + right.Length];
        left.CopyTo(result, 0);
        right.CopyTo(result, left.Length);
        return result;
    }
}
