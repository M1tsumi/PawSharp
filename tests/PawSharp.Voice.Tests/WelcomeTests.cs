#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.Messages;
using PawSharp.Voice.DAVE.MLS.Tree;
using Xunit;

namespace PawSharp.Voice.Tests;

public class WelcomeTests
{
    [Fact]
    public void GroupSecrets_EncodeDecode_RoundTrip()
    {
        var joiner = new byte[32];
        var secret = new GroupSecrets(joiner);

        var encoded = secret.Encode();
        var decoded = GroupSecrets.Decode(encoded);

        decoded.JoinerSecret.Should().BeEquivalentTo(joiner);
        decoded.PathSecret.Should().BeNull();
    }

    [Fact]
    public void GroupSecrets_WithPathSecret_EncodesAndDecodes()
    {
        var joiner = new byte[32];
        var path = new byte[32];
        var secret = new GroupSecrets(joiner, path);

        var encoded = secret.Encode();
        var decoded = GroupSecrets.Decode(encoded);

        decoded.PathSecret.Should().BeEquivalentTo(path);
    }

    [Fact]
    public void EncryptedGroupSecrets_EncodeDecode_RoundTrip()
    {
        var kpRef = new byte[32];
        var enc = new byte[65];
        var ct = new byte[42];
        var egs = new EncryptedGroupSecrets(kpRef, new HpkeCiphertext(enc, ct));

        var encoded = egs.Encode();
        var decoded = EncryptedGroupSecrets.Decode(encoded);

        decoded.KeyPackageRef.Should().BeEquivalentTo(kpRef);
        decoded.EncryptedSecret.Enc.Should().BeEquivalentTo(enc);
        decoded.EncryptedSecret.CipherText.Should().BeEquivalentTo(ct);
    }

    [Fact]
    public void WelcomeMessage_EncodeDecode_RoundTrip()
    {
        var entries = new List<EncryptedGroupSecrets>
        {
            new(new byte[32], new HpkeCiphertext(new byte[65], new byte[32])),
            new(new byte[32], new HpkeCiphertext(new byte[65], new byte[48]))
        };
        var encryptedGroupInfo = new byte[64];
        var welcome = new WelcomeMessage(
            CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256,
            entries,
            encryptedGroupInfo);

        var encoded = welcome.Encode();
        var decoded = WelcomeMessage.Decode(encoded);

        decoded.Suite.Should().Be(CipherSuite.MLS_128_DHKEMP256_AES128GCM_SHA256_P256);
        decoded.Secrets.Should().HaveCount(2);
        decoded.EncryptedGroupInfo.Should().BeEquivalentTo(encryptedGroupInfo);
    }

    [Fact]
    public void GroupInfo_EncodeDecode_RoundTrip()
    {
        var ctx = new GroupContext(new byte[] { 0x01 }, 1, new byte[32], new byte[32]);
        var info = new GroupInfo(ctx, new byte[32], 0, new byte[64]);

        var encoded = info.Encode();
        var decoded = GroupInfo.Decode(encoded);

        decoded.Context.GroupId.Should().BeEquivalentTo(ctx.GroupId);
        decoded.Context.Epoch.Should().Be(1);
        decoded.SignerLeafIndex.Should().Be(0);
        decoded.Signature.Should().HaveCount(64);
    }
}
