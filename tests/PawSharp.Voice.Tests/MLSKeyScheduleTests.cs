#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.State;
using Xunit;

namespace PawSharp.Voice.Tests;

public class MLSKeyScheduleTests
{
    private static byte[] GroupContextBytes()
    {
        var ctx = new PawSharp.Voice.DAVE.MLS.Messages.GroupContext(
            new byte[] { 0x01 }, 0, new byte[32], new byte[32]);
        return ctx.Encode();
    }

    [Fact]
    public void Constructor_InitializesFromCommitSecret()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());

        ks.InitSecret.Should().HaveCount(32);
        ks.JoinerSecret.Should().HaveCount(32);
        ks.EpochSecret.Should().HaveCount(32);
        ks.ExporterSecret.Should().HaveCount(32);
        ks.ConfirmationKey.Should().HaveCount(32);
        ks.WelcomeSecret.Should().HaveCount(32);
    }

    [Fact]
    public void FromJoinerSecret_ProducesValidSchedule()
    {
        var joiner = new byte[32];
        var ks = MLSKeySchedule.FromJoinerSecret(joiner, GroupContextBytes());

        ks.EpochSecret.Should().HaveCount(32);
        ks.ExporterSecret.Should().HaveCount(32);
    }

    [Fact]
    public void AdvanceEpoch_ChangesAllSecrets()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());
        var oldEpoch = ks.EpochSecret;

        var newCtx = new PawSharp.Voice.DAVE.MLS.Messages.GroupContext(
            new byte[] { 0x01 }, 1, new byte[32], new byte[32]);
        ks.AdvanceEpoch(new byte[32], newCtx.Encode());

        ks.EpochSecret.Should().NotBeEquivalentTo(oldEpoch);
    }

    [Fact]
    public void DeriveDaveEpochSecret_Produces32Bytes()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());
        var daveSecret = ks.DeriveDaveEpochSecret();

        daveSecret.Should().HaveCount(32);
    }

    [Fact]
    public void DeriveDaveEpochSecret_ChangesAfterAdvance()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());
        var old = ks.DeriveDaveEpochSecret();

        var newCtx = new PawSharp.Voice.DAVE.MLS.Messages.GroupContext(
            new byte[] { 0x01 }, 1, new byte[32], new byte[32]);
        ks.AdvanceEpoch(new byte[32], newCtx.Encode());
        var newSecret = ks.DeriveDaveEpochSecret();

        newSecret.Should().NotBeEquivalentTo(old);
    }

    [Fact]
    public void ExporterSecret_DifferentFromEpochSecret()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());
        ks.ExporterSecret.Should().NotBeEquivalentTo(ks.EpochSecret);
    }

    [Fact]
    public void ConfirmationKey_DifferentFromExporterSecret()
    {
        var ks = new MLSKeySchedule(new byte[32], GroupContextBytes());
        ks.ConfirmationKey.Should().NotBeEquivalentTo(ks.ExporterSecret);
    }
}
