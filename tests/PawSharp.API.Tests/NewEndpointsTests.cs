#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using Xunit;

namespace PawSharp.API.Tests;

/// <summary>
/// Tests for the REST endpoint additions introduced in alpha11.
/// Uses mocked <see cref="IDiscordRestClient"/> to verify interface contracts.
/// </summary>
public class NewEndpointsTests
{
    private readonly Mock<IDiscordRestClient> _mock = new();

    // ─────────────────────────────────────────────
    //  Stage Instance
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateStageInstanceAsync_Returns_StageInstance_On_Success()
    {
        var expected = new StageInstance { ChannelId = 123UL, Topic = "Dev Hangout" };
        _mock.Setup(r => r.CreateStageInstanceAsync(It.IsAny<CreateStageInstanceRequest>()))
             .ReturnsAsync(expected);

        var result = await _mock.Object.CreateStageInstanceAsync(
            new CreateStageInstanceRequest { ChannelId = 123UL, Topic = "Dev Hangout" });

        result.Should().NotBeNull();
        result!.Topic.Should().Be("Dev Hangout");
    }

    [Fact]
    public async Task GetStageInstanceAsync_Returns_Null_When_Not_Found()
    {
        _mock.Setup(r => r.GetStageInstanceAsync(It.IsAny<ulong>()))
             .ReturnsAsync((StageInstance?)null);

        var result = await _mock.Object.GetStageInstanceAsync(999UL);

        result.Should().BeNull();
    }

    // ─────────────────────────────────────────────
    //  Sticker endpoints
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetGuildStickersAsync_Returns_List()
    {
        var stickers = new List<Sticker>
        {
            new() { Id = 1UL, Name = "wave" },
            new() { Id = 2UL, Name = "thumbsup" }
        };
        _mock.Setup(r => r.GetGuildStickersAsync(It.IsAny<ulong>()))
             .ReturnsAsync(stickers);

        var result = await _mock.Object.GetGuildStickersAsync(100UL);

        result.Should().HaveCount(2);
        result![0].Name.Should().Be("wave");
    }

    [Fact]
    public async Task GetStickerAsync_Returns_Sticker()
    {
        var sticker = new Sticker { Id = 42UL, Name = "cool", Tags = "cool" };
        _mock.Setup(r => r.GetStickerAsync(42UL)).ReturnsAsync(sticker);

        var result = await _mock.Object.GetStickerAsync(42UL);

        result.Should().NotBeNull();
        result!.Name.Should().Be("cool");
    }

    // ─────────────────────────────────────────────
    //  Direct Messages
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CreateDmAsync_Returns_Channel()
    {
        var channel = new Channel { Id = 500UL };
        _mock.Setup(r => r.CreateDmAsync(It.IsAny<ulong>())).ReturnsAsync(channel);

        var result = await _mock.Object.CreateDmAsync(999UL);

        result.Should().NotBeNull();
        result!.Id.Should().Be(500UL);
    }

    // ─────────────────────────────────────────────
    //  Gateway Bot
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetGatewayBotAsync_Returns_GatewayBotInfo()
    {
        var info = new GatewayBotInfo
        {
            Url = "wss://gateway.discord.gg",
            Shards = 4,
            SessionStartLimit = new SessionStartLimit { Total = 1000, Remaining = 998 }
        };
        _mock.Setup(r => r.GetGatewayBotAsync()).ReturnsAsync(info);

        var result = await _mock.Object.GetGatewayBotAsync();

        result.Should().NotBeNull();
        result!.Shards.Should().Be(4);
        result.SessionStartLimit.Remaining.Should().Be(998);
    }

    // ─────────────────────────────────────────────
    //  Voice regions
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetVoiceRegionsAsync_Returns_Regions()
    {
        var regions = new List<VoiceRegion>
        {
            new() { Id = "us-east", Name = "US East", Optimal = true }
        };
        _mock.Setup(r => r.GetVoiceRegionsAsync()).ReturnsAsync(regions);

        var result = await _mock.Object.GetVoiceRegionsAsync();

        result.Should().HaveCount(1);
        result![0].Optimal.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Crosspost
    // ─────────────────────────────────────────────

    [Fact]
    public async Task CrosspostMessageAsync_Returns_Message_On_Success()
    {
        var message = new Message { Id = 77UL, Content = "News!" };
        _mock.Setup(r => r.CrosspostMessageAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
             .ReturnsAsync(message);

        var result = await _mock.Object.CrosspostMessageAsync(100UL, 77UL);

        result.Should().NotBeNull();
        result!.Content.Should().Be("News!");
    }

    // ─────────────────────────────────────────────
    //  Channel permission overwrites
    // ─────────────────────────────────────────────

    [Fact]
    public async Task EditChannelPermissionsAsync_Returns_True_On_Success()
    {
        _mock.Setup(r => r.EditChannelPermissionsAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<EditChannelPermissionsRequest>()))
             .ReturnsAsync(true);

        var result = await _mock.Object.EditChannelPermissionsAsync(
            100UL, 200UL, new EditChannelPermissionsRequest { Allow = "8", Deny = "0", Type = 1 });

        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  User connections
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserConnectionsAsync_Returns_List()
    {
        var connections = new List<UserConnection>
        {
            new() { Id = "github-123", Name = "myuser", Type = "github" }
        };
        _mock.Setup(r => r.GetCurrentUserConnectionsAsync()).ReturnsAsync(connections);

        var result = await _mock.Object.GetCurrentUserConnectionsAsync();

        result.Should().HaveCount(1);
        result![0].Type.Should().Be("github");
    }
}
