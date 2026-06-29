#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Tree;
using Xunit;

namespace PawSharp.Voice.Tests;

public class TreeMathTests
{
    [Theory]
    [InlineData(0u, true)]
    [InlineData(1u, false)]
    [InlineData(2u, true)]
    [InlineData(3u, false)]
    [InlineData(10u, true)]
    [InlineData(11u, false)]
    public void IsLeaf_ReturnsCorrectly(uint x, bool expected)
    {
        TreeMath.IsLeaf(x).Should().Be(expected);
    }

    [Theory]
    [InlineData(0u, 0u)]
    [InlineData(1u, 1u)]
    [InlineData(2u, 0u)]
    [InlineData(3u, 2u)]
    [InlineData(4u, 0u)]
    [InlineData(5u, 1u)]
    public void Level_ReturnsCorrectValue(uint x, uint expected)
    {
        TreeMath.Level(x).Should().Be(expected);
    }

    [Theory]
    [InlineData(1u, 1u)]
    [InlineData(2u, 3u)]
    [InlineData(3u, 5u)]
    [InlineData(4u, 7u)]
    [InlineData(5u, 9u)]
    public void NodeWidth_ReturnsCorrectValue(uint n, uint expected)
    {
        TreeMath.NodeWidth(n).Should().Be(expected);
    }

    [Theory]
    [InlineData(1u, 0u)]
    [InlineData(2u, 1u)]
    [InlineData(3u, 3u)]
    [InlineData(4u, 3u)]
    [InlineData(5u, 7u)]
    public void Root_ReturnsCorrectValue(uint n, uint expected)
    {
        TreeMath.Root(n).Should().Be(expected);
    }

    [Fact]
    public void Left_OfTwoLeafTree_ReturnsCorrectValue()
    {
        TreeMath.Left(1).Should().Be(0u);
    }

    [Fact]
    public void Right_OfTwoLeafTree_ReturnsCorrectValue()
    {
        TreeMath.Right(1, 2).Should().Be(2u);
    }

    [Fact]
    public void Parent_OfLeaf0_InTwoLeafTree_Returns1()
    {
        TreeMath.Parent(0, 2).Should().Be(1u);
    }

    [Fact]
    public void Parent_OfLeaf2_InTwoLeafTree_Returns1()
    {
        TreeMath.Parent(2, 2).Should().Be(1u);
    }

    [Fact]
    public void Parent_OfRoot_Throws()
    {
        Action act = () => TreeMath.Parent(TreeMath.Root(2), 2);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Sibling_ReturnsOppositeChild()
    {
        TreeMath.Sibling(0, 2).Should().Be(2u);
        TreeMath.Sibling(2, 2).Should().Be(0u);
    }

    [Fact]
    public void DirectPath_OfLeaf_InTwoLeafTree_IsEmpty_BecauseParentIsRoot()
    {
        var path = TreeMath.DirectPath(0, 2);
        path.Should().BeEmpty("because Parent(0,2) = 1 = Root(2), which is excluded");
    }

    [Fact]
    public void DirectPath_OfRoot_ReturnsEmpty()
    {
        var path = TreeMath.DirectPath(3, 4);
        path.Should().BeEmpty();
    }

    [Fact]
    public void CoPath_OfLeaf0_InTwoLeafTree_IsEmpty_BecauseDirectPathIsEmpty()
    {
        var co = TreeMath.CoPath(0, 2);
        co.Should().BeEmpty("because DirectPath(0, 2) is empty (parent is root)");
    }

    [Fact]
    public void LeafToNode_ReturnsEvenIndex()
    {
        TreeMath.LeafToNode(0).Should().Be(0u);
        TreeMath.LeafToNode(1).Should().Be(2u);
        TreeMath.LeafToNode(5).Should().Be(10u);
    }

    [Fact]
    public void NodeToLeaf_ReturnsCorrectLeafIndex()
    {
        TreeMath.NodeToLeaf(0).Should().Be(0u);
        TreeMath.NodeToLeaf(2).Should().Be(1u);
        TreeMath.NodeToLeaf(10).Should().Be(5u);
    }

    [Fact]
    public void Resolution_OfNonBlankLeaf_ReturnsLeafOnly()
    {
        var blank = new System.Collections.Generic.HashSet<uint>();
        var res = TreeMath.Resolution(0, 2, blank);
        res.Should().BeEquivalentTo(new uint[] { 0 });
    }

    [Fact]
    public void Resolution_OfBlankLeaf_ReturnsEmpty()
    {
        var blank = new System.Collections.Generic.HashSet<uint> { 0 };
        var res = TreeMath.Resolution(0, 2, blank);
        res.Should().BeEmpty();
    }

    [Fact]
    public void NodeWidth_ZeroLeaves_ReturnsZero()
    {
        TreeMath.NodeWidth(0).Should().Be(0u);
    }

    [Fact]
    public void DirectPath_ZeroLeaves_ReturnsEmpty()
    {
        TreeMath.DirectPath(0, 0).Should().BeEmpty();
    }
}
