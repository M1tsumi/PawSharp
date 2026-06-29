#nullable enable
using System;
using System.Collections.Generic;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Messages;
using PawSharp.Voice.DAVE.MLS.Tree;
using Xunit;

namespace PawSharp.Voice.Tests;

public class ProposalTests
{
    [Fact]
    public void AddProposal_EncodeDecode_RoundTrip()
    {
        var identity = new byte[] { 0x01 };
        var kp = KeyPackage.Generate(identity);
        var proposal = Proposal.Add(kp);

        var encoded = proposal.Encode();
        var decoded = Proposal.Decode(encoded);

        decoded.Type.Should().Be(ProposalType.Add);
        decoded.AddKeyPackage.Should().NotBeNull();
        decoded.AddKeyPackage!.InitKey.Should().BeEquivalentTo(kp.InitKey);
    }

    [Fact]
    public void UpdateProposal_EncodeDecode_RoundTrip()
    {
        var leaf = LeafNode.Generate(new byte[] { 0x01 }, out _, out _);
        var proposal = Proposal.Update(leaf);

        var encoded = proposal.Encode();
        var decoded = Proposal.Decode(encoded);

        decoded.Type.Should().Be(ProposalType.Update);
        decoded.UpdateLeafNode.Should().NotBeNull();
        decoded.UpdateLeafNode!.EncryptionKey.Should().BeEquivalentTo(leaf.EncryptionKey);
    }

    [Fact]
    public void RemoveProposal_EncodeDecode_RoundTrip()
    {
        var proposal = Proposal.Remove(5);

        var encoded = proposal.Encode();
        var decoded = Proposal.Decode(encoded);

        decoded.Type.Should().Be(ProposalType.Remove);
        decoded.RemoveLeafIndex.Should().Be(5u);
    }

    [Fact]
    public void Decode_UnknownType_Throws()
    {
        using var w = new PawSharp.Voice.DAVE.MLS.Encoding.TlsWriter(4);
        w.WriteUint16(0xFFFF);

        Action act = () => Proposal.Decode(w.ToArray());
        act.Should().Throw<PawSharp.Voice.DAVE.MLS.Encoding.MlsDecodeException>();
    }

    [Fact]
    public void Commit_NoProposals_NoUpdatePath_EncodesAndDecodes()
    {
        var commit = new Commit(Array.Empty<Proposal>(), null);

        var encoded = commit.Encode();
        var decoded = Commit.Decode(encoded);

        decoded.Proposals.Should().BeEmpty();
        decoded.UpdatePath.Should().BeNull();
    }

    [Fact]
    public void Commit_WithProposals_EncodesAndDecodes()
    {
        var proposals = new Proposal[]
        {
            Proposal.Remove(1),
            Proposal.Remove(2)
        };
        var commit = new Commit(proposals, null);

        var encoded = commit.Encode();
        var decoded = Commit.Decode(encoded);

        decoded.Proposals.Should().HaveCount(2);
    }

    [Fact]
    public void Commit_WithUpdatePath_EncodesAndDecodes()
    {
        var proposals = Array.Empty<Proposal>();
        var updatePath = new UpdatePathNode[]
        {
            new(new byte[65], Array.Empty<HpkeCiphertext>())
        };
        var commit = new Commit(proposals, updatePath);

        var encoded = commit.Encode();
        var decoded = Commit.Decode(encoded);

        decoded.UpdatePath.Should().HaveCount(1);
        decoded.UpdatePath![0].PublicKey.Should().HaveCount(65);
    }
}
