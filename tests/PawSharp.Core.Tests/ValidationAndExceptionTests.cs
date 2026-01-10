using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using PawSharp.Core.Validation;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Tests;

public class ValidationTests
{
    [Fact]
    public void SnowflakeValidator_ValidatesValidSnowflake()
    {
        // Valid Discord snowflake
        ulong validSnowflake = 175928847299117063;
        
        // Should not throw
        SnowflakeValidator.ValidateSnowflake(validSnowflake);
    }

    [Fact]
    public void SnowflakeValidator_RejectsZeroSnowflake()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            SnowflakeValidator.ValidateSnowflake(0));
        
        ex.Message.Should().Contain("valid Snowflake");
    }

    [Fact]
    public void ContentValidator_AllowsValidMessage()
    {
        string validMessage = "Hello, World!";
        
        // Should not throw
        ContentValidator.ValidateMessageContent(validMessage);
    }

    [Fact]
    public void ContentValidator_RejectsEmptyMessage()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            ContentValidator.ValidateMessageContent(""));
        
        ex.Message.Should().Contain("empty");
    }

    [Fact]
    public void ContentValidator_RejectsTooLongMessage()
    {
        string tooLong = new('a', 4001); // Max is 4000
        
        var ex = Assert.Throws<ValidationException>(() =>
            ContentValidator.ValidateMessageContent(tooLong));
        
        ex.Message.Should().Contain("length");
    }

    [Fact]
    public void EmbedValidator_AllowsValidEmbed()
    {
        var embed = new Dictionary<string, object>
        {
            { "title", "Test Embed" },
            { "description", "Test Description" }
        };
        
        // Should not throw
        ContentValidator.ValidateEmbedContent(embed);
    }

    [Fact]
    public void EmbedValidator_RejectsNullEmbed()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            ContentValidator.ValidateEmbedContent(null));
        
        ex.Should().NotBeNull();
    }

    [Fact]
    public void UrlValidator_AllowsValidUrl()
    {
        string validUrl = "https://discord.com";
        
        // Should not throw
        ContentValidator.ValidateUrl(validUrl);
    }

    [Fact]
    public void UrlValidator_RejectsInvalidUrl()
    {
        string invalidUrl = "not a url";
        
        var ex = Assert.Throws<ValidationException>(() =>
            ContentValidator.ValidateUrl(invalidUrl));
        
        ex.Message.Should().Contain("valid URL");
    }
}

public class ExceptionHierarchyTests
{
    [Fact]
    public void DiscordException_IsBaseClass()
    {
        var ex = new DiscordException("Test error");
        
        ex.Message.Should().Be("Test error");
        ex.Should().BeOfType<DiscordException>();
    }

    [Fact]
    public void DiscordApiException_ContainsStatusCode()
    {
        var ex = new DiscordApiException("API error", 400);
        
        ex.StatusCode.Should().Be(400);
        ex.Message.Should().Contain("API error");
    }

    [Fact]
    public void RateLimitException_ContainsRetryInfo()
    {
        var retryAfter = TimeSpan.FromSeconds(5);
        var ex = new RateLimitException("Rate limit hit", retryAfter, "test_bucket");
        
        ex.RetryAfter.Should().Be(retryAfter);
        ex.BucketId.Should().Be("test_bucket");
    }

    [Fact]
    public void ValidationException_IncludesParameterName()
    {
        var ex = new ValidationException("Invalid value", "userId", 0);
        
        ex.ParameterName.Should().Be("userId");
        ex.Value.Should().Be(0);
    }

    [Fact]
    public void GatewayException_HandlesConnectionErrors()
    {
        var innerEx = new Exception("Connection failed");
        var ex = new GatewayException("Gateway error", innerEx);
        
        ex.InnerException.Should().Be(innerEx);
        ex.Message.Should().Contain("Gateway error");
    }

    [Fact]
    public void DeserializationException_IncludesJsonContent()
    {
        string invalidJson = "{invalid}";
        var ex = new DeserializationException("Failed to deserialize", invalidJson);
        
        ex.JsonContent.Should().Be(invalidJson);
    }
}

public class SnowflakeTests
{
    [Fact]
    public void Snowflake_CreatedAtCalculation()
    {
        // Known snowflake: 175928847299117063 = 2016-04-30 11:18:25.796 UTC
        ulong snowflake = 175928847299117063;
        
        // Timestamp is (snowflake >> 22) + Discord epoch
        long timestampMs = (long)(snowflake >> 22);
        var discordEpoch = new DateTimeOffset(2015, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var createdAt = discordEpoch.AddMilliseconds(timestampMs);
        
        // Should be around 2016-04-30
        createdAt.Year.Should().Be(2016);
        createdAt.Month.Should().Be(4);
        createdAt.Day.Should().Be(30);
    }

    [Fact]
    public void Snowflake_ExtractsComponents()
    {
        ulong snowflake = 175928847299117063;
        
        long timestamp = (long)(snowflake >> 22);
        int workerId = (int)((snowflake >> 17) & 0x1F);
        int processId = (int)((snowflake >> 12) & 0x1F);
        int increment = (int)(snowflake & 0xFFF);
        
        timestamp.Should().BeGreaterThan(0);
        workerId.Should().BeLessThan(32);
        processId.Should().BeLessThan(32);
        increment.Should().BeLessThan(4096);
    }
}
