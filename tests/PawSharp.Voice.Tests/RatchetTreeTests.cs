#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Tree;
using Xunit;

namespace PawSharp.Voice.Tests;

public class RatchetTreeTests
{
    [Fact]
    public void Constructor_LeafCountZero()
    {
        var tree = new RatchetTree();
        tree.LeafCount.Should().Be(0u);
        tree.LocalLeafIndex.Should().BeNull();
    }

    [Fact]
    public void AddLeaf_IncreasesLeafCount()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        tree.LeafCount.Should().Be(1u);
    }

    [Fact]
    public void AddLeaf_ReturnsNodeIndex()
    {
        var tree = new RatchetTree();
        var idx = tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        idx.Should().Be(0u);
    }

    [Fact]
    public void AddLeaf_IsLocal_SetsLocalLeafIndex()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 }, isLocal: true);
        tree.LocalLeafIndex.Should().Be(0u);
    }

    [Fact]
    public void AddLeaf_MultipleLeaves()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x02 });
        tree.LeafCount.Should().Be(2u);
    }

    [Fact]
    public void FindLeafByCredential_ReturnsCorrectIndex()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0xAA });
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0xBB });

        var idx = tree.FindLeafByCredential(new byte[] { 0xAA });
        idx.Should().Be(0u);
    }

    [Fact]
    public void FindLeafByCredential_NotFound_ReturnsNull()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        tree.FindLeafByCredential(new byte[] { 0xFF }).Should().BeNull();
    }

    [Fact]
    public void BlankPath_BlanksLeaf()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        tree.BlankPath(0u);

        tree.FindLeafByCredential(new byte[] { 0x01 }).Should().BeNull();
    }

    [Fact]
    public void ReplaceLeafHpkeKey_UpdatesKey()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        var newKey = new byte[65];
        newKey[0] = 0x02;
        tree.ReplaceLeafHpkeKey(0u, newKey);
    }

    [Fact]
    public void TreeHash_Returns32Bytes()
    {
        var tree = new RatchetTree();
        tree.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });
        var hash = tree.TreeHash();
        hash.Should().HaveCount(32);
    }

    [Fact]
    public void TreeHash_EmptyTree_Returns32Bytes()
    {
        var tree = new RatchetTree();
        var hash = tree.TreeHash();
        hash.Should().HaveCount(32);
    }

    [Fact]
    public void TreeHash_Deterministic()
    {
        var tree1 = new RatchetTree();
        tree1.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });

        var tree2 = new RatchetTree();
        tree2.AddLeaf(new byte[65], new byte[65], new byte[] { 0x01 });

        tree1.TreeHash().Should().BeEquivalentTo(tree2.TreeHash());
    }
}
