using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using PawSharp.Core.Models;
using PawSharp.Core.Enums;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;
using FluentAssertions;

namespace PawSharp.Gateway.Tests;

public class ShardManagerTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly PawSharpOptions _options;

    public ShardManagerTests()
    {
        _loggerMock = new Mock<ILogger>();
        _options = new PawSharpOptions
        {
            Token = "test-token",
            Shards = 2,
            ShardCount = 2
        };
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var shardManager = new ShardManager(_options, _loggerMock.Object);

        // Assert
        shardManager.ShardCount.Should().Be(2);
        shardManager.Events.Should().NotBeNull();
    }

    [Fact]
    public void CalculateRecommendedShardCount_WithGuildCount_ReturnsCorrectValue()
    {
        // Arrange
        int guildCount = 2500;

        // Act
        int result = ShardManager.CalculateRecommendedShardCount(guildCount);

        // Assert
        result.Should().Be(3); // ceil(2500/1000) = 3
    }

    [Fact]
    public void CalculateRecommendedShardCount_WithZeroGuilds_ReturnsOne()
    {
        // Arrange
        int guildCount = 0;

        // Act
        int result = ShardManager.CalculateRecommendedShardCount(guildCount);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetShardIdForGuild_CalculatesCorrectly()
    {
        // Arrange
        var shardManager = new ShardManager(_options, _loggerMock.Object);
        ulong guildId = 123456789012345678; // Some test ID

        // Act
        int shardId = shardManager.GetShardIdForGuild(guildId);

        // Assert
        shardId.Should().BeGreaterOrEqualTo(0).And.BeLessThan(_options.ShardCount);
    }

    [Fact]
    public void GetShardStatus_ForNonExistentShard_ReturnsDisconnected()
    {
        // Arrange
        var shardManager = new ShardManager(_options, _loggerMock.Object);

        // Act
        var status = shardManager.GetShardStatus(999);

        // Assert
        status.Should().Be(ShardStatus.Disconnected);
    }

    [Fact]
    public void GetAllShardStatuses_InitiallyEmpty()
    {
        // Arrange
        var shardManager = new ShardManager(_options, _loggerMock.Object);

        // Act
        var statuses = shardManager.GetAllShardStatuses();

        // Assert
        statuses.Should().BeEmpty();
    }

    [Fact]
    public void ConnectedShardCount_InitiallyZero()
    {
        // Arrange
        var shardManager = new ShardManager(_options, _loggerMock.Object);

        // Act
        var count = shardManager.ConnectedShardCount;

        // Assert
        count.Should().Be(0);
    }
}