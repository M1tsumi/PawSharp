#nullable enable
using System;
using FluentAssertions;
using PawSharp.Interactions;
using Xunit;

namespace PawSharp.Interactions.Tests;

public class WebhookVerifierTests
{
    [Fact]
    public void Constructor_InvalidKeyLength_Throws()
    {
        Action act = () => new WebhookVerifier("ab");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullKey_Throws()
    {
        Action act = () => new WebhookVerifier(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ValidKey_CreatesInstance()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        verifier.Should().NotBeNull();
    }

    [Fact]
    public void Verify_EmptySignature_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var result = verifier.Verify("", "1234567890", "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidSignatureLength_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var result = verifier.Verify("ab", "1234567890", "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptyTimestamp_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var sig = new string('b', 128);
        var result = verifier.Verify(sig, "", "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidTimestamp_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var sig = new string('b', 128);
        var result = verifier.Verify(sig, "not-a-timestamp", "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_StaleTimestamp_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var sig = new string('b', 128);
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var result = verifier.Verify(sig, staleTimestamp, "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_InvalidHexInSignature_ReturnsFalse()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var sig = new string('z', 128);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var result = verifier.Verify(sig, timestamp, "{}");
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_StringBodyOverload_Works()
    {
        var key = new string('a', 64);
        var verifier = new WebhookVerifier(key);
        var sig = new string('b', 128);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var result = verifier.Verify(sig, timestamp, "test body");
        result.Should().BeFalse();
    }
}
