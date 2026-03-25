using FluentAssertions;
using PawSharp.API.Security;
using Xunit;

namespace PawSharp.API.Tests;

public class LogSanitizerTests
{
    [Fact]
    public void RedactSensitiveEndpoint_RedactsWebhookTokenSegment()
    {
        var endpoint = "webhooks/123456789/abc.def/messages/@original?wait=true";

        var result = LogSanitizer.RedactSensitiveEndpoint(endpoint);

        result.Should().Be("webhooks/123456789/REDACTED/messages/@original?wait=true");
    }

    [Fact]
    public void RedactSensitiveEndpoint_RedactsInteractionTokenSegment()
    {
        var endpoint = "interactions/987654/token-value/callback";

        var result = LogSanitizer.RedactSensitiveEndpoint(endpoint);

        result.Should().Be("interactions/987654/REDACTED/callback");
    }

    [Fact]
    public void SanitizeHttpErrorBody_ReturnsEmptyMarker_WhenBodyMissing()
    {
        LogSanitizer.SanitizeHttpErrorBody(null).Should().Be("<empty>");
        LogSanitizer.SanitizeHttpErrorBody(" ").Should().Be("<empty>");
    }

    [Fact]
    public void SanitizeHttpErrorBody_RedactsCommonSecretFields_AndBearerTokens()
    {
        var body = "{\"token\":\"abc123\",\"client_secret\":\"shh\",\"message\":\"Authorization: Bearer xyz.123\"}";

        var result = LogSanitizer.SanitizeHttpErrorBody(body);

        result.Should().NotContain("abc123");
        result.Should().NotContain("shh");
        result.Should().NotContain("Bearer xyz.123");
        result.Should().Contain("\"token\":\"REDACTED\"");
        result.Should().Contain("\"client_secret\":\"REDACTED\"");
        result.Should().Contain("Bearer REDACTED");
    }

    [Fact]
    public void SanitizeHttpErrorBody_TruncatesLargePayloads()
    {
        var body = new string('A', 900);

        var result = LogSanitizer.SanitizeHttpErrorBody(body);

        result.Should().EndWith("... [truncated]");
        result.Length.Should().BeLessThanOrEqualTo(530);
    }
}
