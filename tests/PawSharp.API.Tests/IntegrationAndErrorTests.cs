#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PawSharp.API.Clients;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using PawSharp.Core.Exceptions;
using PawSharp.Core.Models;

namespace PawSharp.API.Tests;

/// <summary>
/// Integration tests for the REST client.
/// Run with environment variable: PAWSHARP_ENABLE_LIVE_TESTS=true
/// </summary>
public class RestClientIntegrationTests
{
    private readonly bool _enableLiveTests = 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PAWSHARP_ENABLE_LIVE_TESTS"));
    
    private readonly Mock<ILogger<DiscordRestClient>> _mockLogger = new();

    [Fact]
    public void IntegrationTests_RequireEnvironmentVariable()
    {
        // This documents that live tests require opt-in
        if (!_enableLiveTests)
        {
            // Skip test when not enabled
            Assert.True(true, "Live tests disabled. Set PAWSHARP_ENABLE_LIVE_TESTS=true to enable.");
        }
    }

    [Fact]
    public async Task LiveTest_AuthenticatesWithValidToken()
    {
        if (!_enableLiveTests)
            return; // live tests disabled

        var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrEmpty(token))
            return; // missing token

        using var client = new HttpClient();
        var options = new PawSharpOptions { Token = token, ApiVersion = 10 };
        var mockRateLimiter = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        var restClient = new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter.Object);

        var response = await restClient.GetCurrentUserAsync();
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

/// <summary>
/// Mock-based tests for error scenarios and edge cases.
/// </summary>
public class RestClientErrorScenarioTests
{
    private readonly Mock<ILogger<DiscordRestClient>> _mockLogger = new();

    private DiscordRestClient CreateClientWithMockResponse(HttpStatusCode statusCode, string responseContent = "{}")
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        var client = new HttpClient(mockHandler.Object);
        var options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
        var mockRateLimiter = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter.Object);
    }

    [Fact]
    public async Task HandlesBadRequest_Returns400()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.BadRequest, @"{ ""message"": ""Invalid request"" }");
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandlesUnauthorized_Returns401()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.Unauthorized);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandlesForbidden_Returns403()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.Forbidden);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task HandlesNotFound_Returns404()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.NotFound);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HandlesRateLimit_Returns429()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.TooManyRequests);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task HandlesServerError_Returns500()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.InternalServerError);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task HandlesServiceUnavailable_Returns503()
    {
        // Arrange
        var restClient = CreateClientWithMockResponse(HttpStatusCode.ServiceUnavailable);
        
        // Act
        var response = await restClient.GetCurrentUserAsync();
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ValidatesInputParameters_BeforeRequest()
    {
        // Arrange
        var options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
        var client = new HttpClient();
        var mockRateLimiter = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        var restClient = new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter.Object);

        // Act & Assert - Should throw ValidationException for invalid limit
        var ex = await Assert.ThrowsAsync<ValidationException>(async () => await restClient.GetCurrentUserGuildsAsync(limit: 999));
        ex.Message.Should().Contain("Limit");
    }

    [Fact]
    public async Task ValidatesSnowflakeIds_BeforeRequest()
    {
        // Arrange
        var options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
        var client = new HttpClient();
        var mockRateLimiter2 = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter2.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        var restClient = new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter2.Object);

        // Act & Assert - Should throw ValidationException for invalid snowflake
        var ex = await Assert.ThrowsAsync<ValidationException>(async () => await restClient.GetUserAsync(0));
        ex.Message.Should().NotBeEmpty();
    }
}

/// <summary>
/// Tests for cache interaction and state management.
/// </summary>
public class RestClientCacheInteractionTests
{
    private readonly Mock<ILogger<DiscordRestClient>> _mockLogger = new();

    [Fact]
    public async Task MultipleRequests_SameEndpoint_UsingSameHttpClient()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        int callCount = 0;
        
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => callCount++)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ ""id"": ""123"", ""username"": ""testuser"" }")
            });

        var client = new HttpClient(mockHandler.Object);
        var options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
        var mockRateLimiter3 = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter3.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        var restClient = new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter3.Object);

        // Act
        await restClient.GetCurrentUserAsync();
        await restClient.GetCurrentUserAsync();

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentRequests_HandleMultipleCalls()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;
        var lockObj = new object();
        
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => { lock (lockObj) { callCount++; } })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ ""id"": ""123"" }")
            });

        var client = new HttpClient(mockHandler.Object);
        var options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
        var mockRateLimiter4 = new Mock<PawSharp.API.RateLimit.IAdvancedRateLimiter>();
        mockRateLimiter4.Setup(x => x.WaitForRateLimitAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
        var restClient = new DiscordRestClient(client, options, _mockLogger.Object, mockRateLimiter4.Object);

        // Act
        var tasks = new[]
        {
            restClient.GetCurrentUserAsync(),
            restClient.GetCurrentUserAsync(),
            restClient.GetCurrentUserAsync()
        };
        await Task.WhenAll(tasks);

        // Assert
        callCount.Should().Be(3);
    }
}
