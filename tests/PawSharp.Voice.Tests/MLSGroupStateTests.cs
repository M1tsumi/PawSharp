#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE.MLS.State;
using Xunit;

namespace PawSharp.Voice.Tests;

public class MLSGroupStateTests
{
    [Fact]
    public void Constructor_NotInitialized()
    {
        using var state = new MLSGroupState();
        state.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void GetOrGenerateKeyPackage_ReturnsValidPackage()
    {
        using var state = new MLSGroupState();
        var kp = state.GetOrGenerateKeyPackage(new byte[] { 0x01 });

        kp.Should().NotBeNull();
        kp.VerifySignature().Should().BeTrue();
    }

    [Fact]
    public void GetOrGenerateKeyPackage_SameIdentity_ReturnsCached()
    {
        using var state = new MLSGroupState();
        var kp1 = state.GetOrGenerateKeyPackage(new byte[] { 0x01 });
        var kp2 = state.GetOrGenerateKeyPackage(new byte[] { 0x01 });

        kp1.InitKey.Should().BeEquivalentTo(kp2.InitKey);
    }

    [Fact]
    public void GetOrGenerateKeyPackage_DifferentIdentity_ReturnsSameCached()
    {
        using var state = new MLSGroupState();
        var kp1 = state.GetOrGenerateKeyPackage(new byte[] { 0x01 });
        var kp2 = state.GetOrGenerateKeyPackage(new byte[] { 0x02 });

        kp1.InitKey.Should().BeSameAs(kp2.InitKey, "because GetOrGenerateKeyPackage caches the first generated package");
    }

    [Fact]
    public void Reset_WipesState()
    {
        using var state = new MLSGroupState();
        state.GetOrGenerateKeyPackage(new byte[] { 0x01 });
        state.IsInitialized.Should().BeFalse();

        state.Reset();
        state.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void ProcessProposals_Empty_DoesNotThrow()
    {
        using var state = new MLSGroupState();
        Action act = () => state.ProcessProposals(Array.Empty<byte>());
        act.Should().NotThrow();
    }

    [Fact]
    public void ProcessCommit_BeforeWelcome_Throws()
    {
        using var state = new MLSGroupState();
        Action act = () => state.ProcessCommit(new byte[] { 0x01, 0x02 });
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetExternalSenderPackage_DoesNotThrow()
    {
        using var state = new MLSGroupState();
        Action act = () => state.SetExternalSenderPackage(new byte[] { 0x01 });
        act.Should().NotThrow();
    }
}
