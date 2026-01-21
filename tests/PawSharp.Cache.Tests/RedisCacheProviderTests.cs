using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;
using FluentAssertions;
using PawSharp.Cache.Providers;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Tests
{
    public class RedisCacheProviderTests : IDisposable
    {
        private readonly RedisCacheProvider _cache;
        private readonly Mock<IConnectionMultiplexer> _mockRedis;
        private readonly Mock<IDatabase> _mockDb;

        public RedisCacheProviderTests()
        {
            // Mock Redis components for testing
            _mockDb = new Mock<IDatabase>();
            _mockRedis = new Mock<IConnectionMultiplexer>();

            // Setup the mock to return our mock database
            _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                     .Returns(_mockDb.Object);

            // Create cache with mocked Redis (in a real scenario, you'd use a test Redis instance)
            // For now, we'll use the constructor that takes a connection string
            // and assume Redis is available, or skip tests if not
            try
            {
                _cache = new RedisCacheProvider("localhost:6379");
            }
            catch
            {
                // Redis not available, skip tests
                _cache = null!;
            }
        }

        [Fact(Skip = "Requires Redis server")]
        public void Constructor_WithValidConnectionString_ConnectsSuccessfully()
        {
            // This test requires a running Redis instance
            // In CI/CD, you would set up a Redis test container

            var options = Options.Create(new RedisCacheOptions
            {
                ConnectionString = "localhost:6379"
            });

            var cache = new RedisCacheProvider(options);

            cache.Should().NotBeNull();
        }

        [Fact(Skip = "Requires Redis server")]
        public void CacheUser_StoresAndRetrievesUser()
        {
            // Arrange
            var user = new User
            {
                Id = 123456789UL,
                Username = "testuser",
                Discriminator = "1234"
            };

            // Act
            _cache.CacheUser(user);
            var retrieved = _cache.GetUser(user.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(user.Id);
            retrieved.Username.Should().Be(user.Username);
        }

        [Fact(Skip = "Requires Redis server")]
        public void CacheGuild_StoresAndRetrievesGuild()
        {
            // Arrange
            var guild = new Guild
            {
                Id = 987654321UL,
                Name = "Test Guild"
            };

            // Act
            _cache.CacheGuild(guild);
            var retrieved = _cache.GetGuild(guild.Id);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(guild.Id);
            retrieved.Name.Should().Be(guild.Name);
        }

        [Fact(Skip = "Requires Redis server")]
        public void GetCacheStats_ReturnsValidStatistics()
        {
            // Act
            var stats = _cache.GetCacheStats();

            // Assert
            stats.Should().NotBeNull();
            stats.UserCount.Should().BeGreaterThanOrEqualTo(0);
            stats.GuildCount.Should().BeGreaterThanOrEqualTo(0);
            stats.MemoryUsage.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact(Skip = "Requires Redis server")]
        public void Clear_RemovesAllData()
        {
            // Arrange
            var user = new User { Id = 123UL, Username = "test" };
            _cache.CacheUser(user);

            // Act
            _cache.Clear();
            var retrieved = _cache.GetUser(user.Id);

            // Assert
            retrieved.Should().BeNull();
        }

        public void Dispose()
        {
            _cache?.Dispose();
        }
    }
}