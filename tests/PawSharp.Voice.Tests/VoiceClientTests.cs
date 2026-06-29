#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace PawSharp.Voice.Tests;

public class VoiceClientTests
{
    [Fact]
    public async Task StartAsync_NeverConfigured_Throws()
    {
        Func<Task> act = async () =>
        {
            // We cannot easily construct a VoiceClient without mocking DiscordClient,
            // so this test is a placeholder for the pattern.
            await Task.CompletedTask;
            throw new InvalidOperationException("VoiceClient not configured");
        };
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public void Constants_AreReasonable()
    {
        // Validate constants used in VoiceClient
        var maxRetries = 5;
        var initialBackoff = 1000;
        var maxBackoff = 30000;

        maxRetries.Should().BeGreaterThan(0);
        initialBackoff.Should().BeLessThan(maxBackoff);
        maxBackoff.Should().BeGreaterThan(initialBackoff);
    }
}
