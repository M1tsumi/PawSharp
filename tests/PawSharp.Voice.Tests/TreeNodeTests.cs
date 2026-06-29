#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Tree;
using Xunit;

namespace PawSharp.Voice.Tests;

public class TreeNodeTests
{
    [Fact]
    public void CreateBlank_NodeIsBlank()
    {
        var node = TreeNode.CreateBlank(0);
        node.IsBlank.Should().BeTrue();
        node.NodeIndex.Should().Be(0);
    }

    [Fact]
    public void CreateLeaf_WithValidIndex_IsNotBlank()
    {
        var key = new byte[32];
        var sig = new byte[65];
        var cred = new byte[] { 0x01 };

        var node = TreeNode.CreateLeaf(0, key, null, sig, cred);

        node.IsBlank.Should().BeFalse();
        node.IsLeaf.Should().BeTrue();
        node.HpkePublicKey.Should().BeSameAs(key);
        node.SignatureKey.Should().BeSameAs(sig);
        node.Credential.Should().BeSameAs(cred);
    }

    [Fact]
    public void CreateLeaf_WithOddIndex_Throws()
    {
        Action act = () => TreeNode.CreateLeaf(1, new byte[32], null, new byte[65], new byte[1]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateParent_WithValidIndex_IsNotBlank()
    {
        var key = new byte[32];
        var node = TreeNode.CreateParent(1, key);

        node.IsBlank.Should().BeFalse();
        node.IsLeaf.Should().BeFalse();
        node.HpkePublicKey.Should().BeSameAs(key);
    }

    [Fact]
    public void CreateParent_WithEvenIndex_Throws()
    {
        Action act = () => TreeNode.CreateParent(0, new byte[32]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Blank_ClearsAllProperties()
    {
        var node = TreeNode.CreateLeaf(0, new byte[32], new byte[32], new byte[65], new byte[1]);
        node.Blank();

        node.IsBlank.Should().BeTrue();
        node.HpkePublicKey.Should().BeNull();
        node.HpkePrivateKey.Should().BeNull();
        node.Credential.Should().BeNull();
        node.SignatureKey.Should().BeNull();
    }

    [Fact]
    public void SetHpkeKeys_UpdatesPublicKey()
    {
        var node = TreeNode.CreateBlank(0);
        var pub = new byte[32];
        var priv = new byte[32];

        node.SetHpkeKeys(pub, priv);

        node.IsBlank.Should().BeFalse();
        node.HpkePublicKey.Should().BeSameAs(pub);
        node.HpkePrivateKey.Should().BeSameAs(priv);
    }

    [Fact]
    public void CreateLeaf_StoresHpkePrivateKey()
    {
        var privKey = new byte[32];
        var node = TreeNode.CreateLeaf(0, new byte[32], privKey, new byte[65], new byte[1]);

        node.HpkePrivateKey.Should().BeSameAs(privKey);
    }
}
