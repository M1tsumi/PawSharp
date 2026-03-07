#nullable enable
using System;
using FluentAssertions;
using PawSharp.Voice.DAVE;
using Xunit;

namespace PawSharp.Voice.Tests;

/// <summary>
/// Tests for <see cref="MLSState"/> — MLS group state and epoch management.
/// </summary>
public class MLSStateTests : IDisposable
{
    private readonly MLSState _state = new();

    public void Dispose() => _state.Dispose();

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void NewState_IsNotInitialized()
    {
        _state.IsInitialized.Should().BeFalse();
        _state.EpochNumber.Should().Be(0);
        _state.EpochSecret.Should().BeNull();
    }

    // ── ProcessWelcome ────────────────────────────────────────────────────────

    [Fact]
    public void ProcessWelcome_SetsIsInitializedTrue()
    {
        _state.ProcessWelcome(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        _state.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void ProcessWelcome_SetsEpochTo1()
    {
        _state.ProcessWelcome(new byte[] { 0xAB, 0xCD });

        _state.EpochNumber.Should().Be(1);
    }

    [Fact]
    public void ProcessWelcome_PopulatesEpochSecret_As32Bytes()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });

        _state.EpochSecret.Should().NotBeNull();
        _state.EpochSecret!.Length.Should().Be(32);
    }

    [Fact]
    public void ProcessWelcome_EmptyPayload_ThrowsArgumentException()
    {
        Action act = () => _state.ProcessWelcome(Array.Empty<byte>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessWelcome_NullPayload_ThrowsArgumentException()
    {
        Action act = () => _state.ProcessWelcome(null!);
        act.Should().Throw<ArgumentException>();
    }

    // ── ProcessCommit ─────────────────────────────────────────────────────────

    [Fact]
    public void ProcessCommit_AdvancesEpochNumber()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });
        _state.ProcessCommit(new byte[] { 0x02 });

        _state.EpochNumber.Should().Be(2);
    }

    [Fact]
    public void ProcessCommit_ChangesEpochSecret()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });
        var secretAfterWelcome = (byte[])_state.EpochSecret!.Clone();

        _state.ProcessCommit(new byte[] { 0x02, 0x03 });

        _state.EpochSecret.Should().NotBeEquivalentTo(secretAfterWelcome,
            "each commit must rotate the epoch secret");
    }

    [Fact]
    public void ProcessCommit_EmptyPayload_ThrowsArgumentException()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });
        Action act = () => _state.ProcessCommit(Array.Empty<byte>());
        act.Should().Throw<ArgumentException>();
    }

    // ── GetSenderKey ──────────────────────────────────────────────────────────

    [Fact]
    public void GetSenderKey_BeforeWelcome_ThrowsInvalidOperationException()
    {
        Action act = () => _state.GetSenderKey(ssrc: 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetSenderKey_Returns16Bytes()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });

        var key = _state.GetSenderKey(ssrc: 0xDEAD);

        key.Should().HaveCount(16);
    }

    [Fact]
    public void GetSenderKey_SameSsrc_ReturnsSameKey()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });

        var k1 = _state.GetSenderKey(ssrc: 100);
        var k2 = _state.GetSenderKey(ssrc: 100);

        k1.Should().BeEquivalentTo(k2, "cached keys must be stable within an epoch");
    }

    [Fact]
    public void GetSenderKey_DifferentSsrcs_ReturnDifferentKeys()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });

        var k1 = _state.GetSenderKey(ssrc: 1);
        var k2 = _state.GetSenderKey(ssrc: 2);

        k1.Should().NotBeEquivalentTo(k2);
    }

    // ── Key cache invalidation on epoch advance ───────────────────────────────

    [Fact]
    public void AfterCommit_GetSenderKey_ReturnsDifferentKeyThanPreviousEpoch()
    {
        _state.ProcessWelcome(new byte[] { 0x01 });
        var keyEpoch1 = (byte[])_state.GetSenderKey(ssrc: 5).Clone();

        _state.ProcessCommit(new byte[] { 0x02 });
        var keyEpoch2 = _state.GetSenderKey(ssrc: 5);

        keyEpoch2.Should().NotBeEquivalentTo(keyEpoch1,
            "epoch rotation must invalidate all sender keys");
    }

    // ── Multiple commits ──────────────────────────────────────────────────────

    [Fact]
    public void MultipleCommits_EachAdvancesEpochByOne()
    {
        _state.ProcessWelcome(new byte[] { 0x00 });
        _state.ProcessCommit(new byte[] { 0x01 });
        _state.ProcessCommit(new byte[] { 0x02 });
        _state.ProcessCommit(new byte[] { 0x03 });

        _state.EpochNumber.Should().Be(4);
    }
}
