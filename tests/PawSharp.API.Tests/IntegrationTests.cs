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
using PawSharp.Core.Models;

namespace PawSharp.API.Tests
{
    public class RestClientTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly PawSharpOptions _options;
        private readonly Mock<ILogger<DiscordRestClient>> _mockLogger;
        private readonly DiscordRestClient _restClient;

        public RestClientTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _options = new PawSharpOptions { Token = "test-token", ApiVersion = 10 };
            _mockLogger = new Mock<ILogger<DiscordRestClient>>();
            _restClient = new DiscordRestClient(_httpClient, _options, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateWebhookAsync_ReturnsWebhook_WhenSuccessful()
        {
            // Arrange
            var channelId = 123456789UL;
            var request = new CreateWebhookRequest { Name = "Test Webhook", Avatar = "avatar_data" };
            var expectedWebhook = new Webhook
            {
                Id = 987654321UL,
                Name = "Test Webhook",
                Avatar = "avatar_data",
                ChannelId = channelId,
                GuildId = 111111111UL,
                Type = WebhookType.Incoming
            };

            var responseJson = @"{
                ""id"": ""987654321"",
                ""name"": ""Test Webhook"",
                ""avatar"": ""avatar_data"",
                ""channel_id"": ""123456789"",
                ""guild_id"": ""111111111"",
                ""type"": 1
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().EndsWith($"channels/{channelId}/webhooks")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.CreateWebhookAsync(channelId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedWebhook.Id);
            result.Name.Should().Be(expectedWebhook.Name);
            result.ChannelId.Should().Be(expectedWebhook.ChannelId);
        }

        [Fact]
        public async Task CreateWebhookAsync_ReturnsNull_WhenUnsuccessful()
        {
            // Arrange
            var channelId = 123456789UL;
            var request = new CreateWebhookRequest { Name = "Test Webhook" };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act
            var result = await _restClient.CreateWebhookAsync(channelId, request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetChannelWebhooksAsync_ReturnsWebhooks_WhenSuccessful()
        {
            // Arrange
            var channelId = 123456789UL;
            var expectedWebhooks = new List<Webhook>
            {
                new Webhook { Id = 987654321UL, Name = "Webhook 1", ChannelId = channelId },
                new Webhook { Id = 987654322UL, Name = "Webhook 2", ChannelId = channelId }
            };

            var responseJson = @"[
                {
                    ""id"": ""987654321"",
                    ""name"": ""Webhook 1"",
                    ""channel_id"": ""123456789"",
                    ""type"": 1
                },
                {
                    ""id"": ""987654322"",
                    ""name"": ""Webhook 2"",
                    ""channel_id"": ""123456789"",
                    ""type"": 1
                }
            ]";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"channels/{channelId}/webhooks")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetChannelWebhooksAsync(channelId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(expectedWebhooks[0].Id);
            result[1].Id.Should().Be(expectedWebhooks[1].Id);
        }

        [Fact]
        public async Task GetGuildWebhooksAsync_ReturnsWebhooks_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var expectedWebhooks = new List<Webhook>
            {
                new Webhook { Id = 987654321UL, Name = "Guild Webhook 1", GuildId = guildId },
                new Webhook { Id = 987654322UL, Name = "Guild Webhook 2", GuildId = guildId }
            };

            var responseJson = @"[
                {
                    ""id"": ""987654321"",
                    ""name"": ""Guild Webhook 1"",
                    ""guild_id"": ""111111111"",
                    ""type"": 1
                },
                {
                    ""id"": ""987654322"",
                    ""name"": ""Guild Webhook 2"",
                    ""guild_id"": ""111111111"",
                    ""type"": 1
                }
            ]";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/webhooks")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetGuildWebhooksAsync(guildId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].GuildId.Should().Be(guildId);
            result[1].GuildId.Should().Be(guildId);
        }

        [Fact]
        public async Task GetWebhookAsync_ReturnsWebhook_WhenSuccessful()
        {
            // Arrange
            var webhookId = 987654321UL;
            var expectedWebhook = new Webhook
            {
                Id = webhookId,
                Name = "Test Webhook",
                ChannelId = 123456789UL,
                Type = WebhookType.Incoming
            };

            var responseJson = @"{
                ""id"": ""987654321"",
                ""name"": ""Test Webhook"",
                ""channel_id"": ""123456789"",
                ""type"": 1
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"webhooks/{webhookId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetWebhookAsync(webhookId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedWebhook.Id);
            result.Name.Should().Be(expectedWebhook.Name);
        }

        [Fact]
        public async Task ModifyWebhookAsync_ReturnsUpdatedWebhook_WhenSuccessful()
        {
            // Arrange
            var webhookId = 987654321UL;
            var request = new ModifyWebhookRequest { Name = "Updated Webhook" };
            var expectedWebhook = new Webhook
            {
                Id = webhookId,
                Name = "Updated Webhook",
                ChannelId = 123456789UL,
                Type = WebhookType.Incoming
            };

            var responseJson = @"{
                ""id"": ""987654321"",
                ""name"": ""Updated Webhook"",
                ""channel_id"": ""123456789"",
                ""type"": 1
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Patch &&
                        req.RequestUri.ToString().EndsWith($"webhooks/{webhookId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.ModifyWebhookAsync(webhookId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedWebhook.Id);
            result.Name.Should().Be(expectedWebhook.Name);
        }

        [Fact]
        public async Task DeleteWebhookAsync_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var webhookId = 987654321UL;

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete &&
                        req.RequestUri.ToString().EndsWith($"webhooks/{webhookId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NoContent
                });

            // Act
            var result = await _restClient.DeleteWebhookAsync(webhookId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteWebhookAsync_ReturnsMessage_WhenSuccessful()
        {
            // Arrange
            var webhookId = 987654321UL;
            var token = "webhook-token";
            var request = new ExecuteWebhookRequest { Content = "Hello from webhook!" };
            var expectedMessage = new Message
            {
                Id = 555555555UL,
                Content = "Hello from webhook!",
                ChannelId = 123456789UL
            };

            var responseJson = @"{
                ""id"": ""555555555"",
                ""content"": ""Hello from webhook!"",
                ""channel_id"": ""123456789""
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().EndsWith($"webhooks/{webhookId}/{token}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.ExecuteWebhookAsync(webhookId, token, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedMessage.Id);
            result.Content.Should().Be(expectedMessage.Content);
        }

        // Scheduled Events Tests
        [Fact]
        public async Task CreateGuildScheduledEventAsync_ReturnsEvent_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var request = new CreateGuildScheduledEventRequest
            {
                Name = "Test Event",
                ScheduledStartTime = DateTimeOffset.UtcNow.AddHours(1),
                EntityType = (int)GuildScheduledEventEntityType.External,
                ChannelId = 123456789UL
            };
            var expectedEvent = new GuildScheduledEvent
            {
                Id = 999999999UL,
                Name = "Test Event",
                EntityType = GuildScheduledEventEntityType.External
            };

            var responseJson = @"{
                ""id"": ""999999999"",
                ""guild_id"": ""111111111"",
                ""name"": ""Test Event"",
                ""entity_type"": 3
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.CreateGuildScheduledEventAsync(guildId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedEvent.Id);
            result.Name.Should().Be(expectedEvent.Name);
        }

        [Fact]
        public async Task GetGuildScheduledEventsAsync_ReturnsEvents_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var expectedEvents = new List<GuildScheduledEvent>
            {
                new GuildScheduledEvent { Id = 999999999UL, Name = "Event 1" },
                new GuildScheduledEvent { Id = 999999998UL, Name = "Event 2" }
            };

            var responseJson = @"[
                {
                    ""id"": ""999999999"",
                    ""guild_id"": ""111111111"",
                    ""name"": ""Event 1""
                },
                {
                    ""id"": ""999999998"",
                    ""guild_id"": ""111111111"",
                    ""name"": ""Event 2""
                }
            ]";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetGuildScheduledEventsAsync(guildId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(expectedEvents[0].Id);
            result[1].Id.Should().Be(expectedEvents[1].Id);
        }

        [Fact]
        public async Task GetGuildScheduledEventAsync_ReturnsEvent_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var eventId = 999999999UL;
            var expectedEvent = new GuildScheduledEvent
            {
                Id = eventId,
                Name = "Test Event"
            };

            var responseJson = @"{
                ""id"": ""999999999"",
                ""guild_id"": ""111111111"",
                ""name"": ""Test Event""
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events/{eventId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetGuildScheduledEventAsync(guildId, eventId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedEvent.Id);
        }

        [Fact]
        public async Task ModifyGuildScheduledEventAsync_ReturnsUpdatedEvent_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var eventId = 999999999UL;
            var request = new ModifyGuildScheduledEventRequest { Name = "Updated Event" };
            var expectedEvent = new GuildScheduledEvent
            {
                Id = eventId,
                Name = "Updated Event"
            };

            var responseJson = @"{
                ""id"": ""999999999"",
                ""guild_id"": ""111111111"",
                ""name"": ""Updated Event""
            }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Patch &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events/{eventId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.ModifyGuildScheduledEventAsync(guildId, eventId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(expectedEvent.Id);
            result.Name.Should().Be(expectedEvent.Name);
        }

        [Fact]
        public async Task DeleteGuildScheduledEventAsync_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var eventId = 999999999UL;

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events/{eventId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NoContent
                });

            // Act
            var result = await _restClient.DeleteGuildScheduledEventAsync(guildId, eventId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetGuildScheduledEventUsersAsync_ReturnsUsers_WhenSuccessful()
        {
            // Arrange
            var guildId = 111111111UL;
            var eventId = 999999999UL;
            var expectedUsers = new List<User>
            {
                new User { Id = 777777777UL, Username = "User1" },
                new User { Id = 888888888UL, Username = "User2" }
            };

            var responseJson = @"[
                {
                    ""id"": ""777777777"",
                    ""username"": ""User1""
                },
                {
                    ""id"": ""888888888"",
                    ""username"": ""User2""
                }
            ]";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/scheduled-events/{eventId}/users")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetGuildScheduledEventUsersAsync(guildId, eventId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(expectedUsers[0].Id);
            result[1].Id.Should().Be(expectedUsers[1].Id);
        }

        [Fact]
        public async Task GetGuildAuditLogsAsync_ReturnsAuditLog_WhenSuccessful()
        {
            // Arrange
            var guildId = 123456789UL;
            var expectedAuditLog = new AuditLog
            {
                AuditLogEntries = new List<AuditLogEntry>
                {
                    new AuditLogEntry { Id = 999999999UL, ActionType = AuditLogEvent.GuildUpdate, TargetId = guildId.ToString() }
                },
                Users = new List<User>
                {
                    new User { Id = 111111111UL, Username = "TestUser" }
                }
            };
            var responseJson = JsonSerializer.Serialize(expectedAuditLog);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/audit-logs")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.GetGuildAuditLogsAsync(guildId);

            // Assert
            result.Should().NotBeNull();
            result.AuditLogEntries.Should().HaveCount(1);
            result.AuditLogEntries[0].ActionType.Should().Be(AuditLogEvent.GuildUpdate);
            result.Users.Should().HaveCount(1);
            result.Users[0].Username.Should().Be("TestUser");
        }

        [Fact]
        public async Task ListAutoModerationRulesAsync_ReturnsRules_WhenSuccessful()
        {
            // Arrange
            var guildId = 123456789UL;
            var expectedRules = new List<AutoModerationRule>
            {
                new AutoModerationRule { Id = 999999999UL, Name = "Rule 1", GuildId = guildId },
                new AutoModerationRule { Id = 999999998UL, Name = "Rule 2", GuildId = guildId }
            };
            var responseJson = JsonSerializer.Serialize(expectedRules);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().EndsWith($"guilds/{guildId}/auto-moderation/rules")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _restClient.ListAutoModerationRulesAsync(guildId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Rule 1");
            result[1].Name.Should().Be("Rule 2");
        }
    }
}