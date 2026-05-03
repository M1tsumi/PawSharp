using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using PawSharp.Cache;
using PawSharp.Cache.Providers;
using PawSharp.Core.Entities;

namespace PawSharp.Cache.Tests
{
    public class MemoryCacheProviderTests : IDisposable
    {
        private readonly MemoryCacheProvider _cache;

        public MemoryCacheProviderTests()
        {
            _cache = new MemoryCacheProvider(new CacheOptions
            {
                MaxGuilds = 100,
                MaxChannels = 500,
                MaxUsers = 1000,
                MaxMessages = 1000,
                MaxMembers = 2000,
                MaxRoles = 500,
                MaxEmojis = 500,
                DefaultExpiration = TimeSpan.FromMinutes(30)
            });
        }

        [Fact]
        public void Constructor_WithDefaultOptions_CreatesInstance()
        {
            var cache = new MemoryCacheProvider();
            cache.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithCustomOptions_UsesProvidedOptions()
        {
            var options = new CacheOptions
            {
                MaxUsers = 500,
                DefaultExpiration = TimeSpan.FromHours(2)
            };
            var cache = new MemoryCacheProvider(options);
            cache.Should().NotBeNull();
        }

        [Fact]
        public void CacheUser_StoresAndRetrievesUser()
        {
            var user = new User
            {
                Id = 123456789UL,
                Username = "testuser",
                Discriminator = "1234"
            };

            _cache.CacheUser(user);
            var retrieved = _cache.GetUser(user.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(user.Id);
            retrieved.Username.Should().Be(user.Username);
        }

        [Fact]
        public void CacheGuild_StoresAndRetrievesGuild()
        {
            var guild = new Guild
            {
                Id = 987654321UL,
                Name = "Test Guild"
            };

            _cache.CacheGuild(guild);
            var retrieved = _cache.GetGuild(guild.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(guild.Id);
            retrieved.Name.Should().Be(guild.Name);
        }

        [Fact]
        public void CacheChannel_StoresAndRetrievesChannel()
        {
            var channel = new Channel
            {
                Id = 111222333UL,
                Name = "test-channel",
                Type = ChannelType.GuildText,
                GuildId = 987654321UL
            };

            _cache.CacheChannel(channel);
            var retrieved = _cache.GetChannel(channel.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(channel.Id);
            retrieved.Name.Should().Be(channel.Name);
        }

        [Fact]
        public void CacheMessage_StoresAndRetrievesMessage()
        {
            var message = new Message
            {
                Id = 444555666UL,
                Content = "Test message",
                ChannelId = 111222333UL,
                Author = new User { Id = 123456789UL, Username = "testuser" },
                Timestamp = DateTimeOffset.UtcNow
            };

            _cache.CacheMessage(message);
            var retrieved = _cache.GetMessage(message.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(message.Id);
            retrieved.Content.Should().Be(message.Content);
        }

        [Fact]
        public void CacheGuildMember_StoresAndRetrievesMember()
        {
            var member = new GuildMember
            {
                User = new User { Id = 123456789UL, Username = "testuser" },
                Nick = "TestNick"
            };

            _cache.CacheGuildMember(987654321UL, member);
            var retrieved = _cache.GetGuildMember(987654321UL, 123456789UL);

            retrieved.Should().NotBeNull();
            retrieved!.Nick.Should().Be(member.Nick);
        }

        [Fact]
        public void CacheRole_StoresAndRetrievesRole()
        {
            var role = new Role
            {
                Id = 777888999UL,
                Name = "Test Role",
                Color = 0xFF0000
            };

            _cache.CacheRole(987654321UL, role);
            var retrieved = _cache.GetRole(987654321UL, role.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(role.Id);
            retrieved.Name.Should().Be(role.Name);
        }

        [Fact]
        public void CacheEmoji_StoresAndRetrievesEmoji()
        {
            var emoji = new Emoji
            {
                Id = 999888777UL,
                Name = "testemoji"
            };

            _cache.CacheEmoji(987654321UL, emoji);
            var retrieved = _cache.GetEmoji(987654321UL, emoji.Id.Value);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(emoji.Id);
            retrieved.Name.Should().Be(emoji.Name);
        }

        [Fact]
        public void GenericCacheOperations_WorkCorrectly()
        {
            var testObject = new { Id = 123, Name = "Test" };

            _cache.Add("test_key", testObject);
            var retrieved = _cache.Get("test_key");

            retrieved.Should().NotBeNull();
            _cache.Exists("test_key").Should().BeTrue();

            _cache.Remove("test_key");
            _cache.Exists("test_key").Should().BeFalse();
        }

        [Fact]
        public void RemoveChannel_RemovesChannelFromCache()
        {
            var channel = new Channel
            {
                Id = 111222333UL,
                Name = "test-channel",
                Type = ChannelType.GuildText,
                GuildId = 987654321UL
            };

            _cache.CacheChannel(channel);
            _cache.RemoveChannel(channel.Id);
            var retrieved = _cache.GetChannel(channel.Id);

            retrieved.Should().BeNull();
        }

        [Fact]
        public void RemoveMessage_RemovesMessageFromCache()
        {
            var message = new Message
            {
                Id = 444555666UL,
                Content = "Test message",
                ChannelId = 111222333UL,
                Author = new User { Id = 123456789UL, Username = "testuser" },
                Timestamp = DateTimeOffset.UtcNow
            };

            _cache.CacheMessage(message);
            _cache.RemoveMessage(message.Id);
            var retrieved = _cache.GetMessage(message.Id);

            retrieved.Should().BeNull();
        }

        [Fact]
        public void RemoveGuildMember_RemovesMemberFromCache()
        {
            var member = new GuildMember
            {
                User = new User { Id = 123456789UL, Username = "testuser" },
                Nick = "TestNick"
            };

            _cache.CacheGuildMember(987654321UL, member);
            _cache.RemoveGuildMember(987654321UL, 123456789UL);
            var retrieved = _cache.GetGuildMember(987654321UL, 123456789UL);

            retrieved.Should().BeNull();
        }

        [Fact]
        public void RemoveRole_RemovesRoleFromCache()
        {
            var role = new Role
            {
                Id = 777888999UL,
                Name = "Test Role",
                Color = 0xFF0000
            };

            _cache.CacheRole(987654321UL, role);
            _cache.RemoveRole(987654321UL, role.Id);
            var retrieved = _cache.GetRole(987654321UL, role.Id);

            retrieved.Should().BeNull();
        }

        [Fact]
        public void CacheGuildData_CachesAllGuildEntities()
        {
            var guild = new Guild
            {
                Id = 987654321UL,
                Name = "Test Guild",
                Channels = new[]
                {
                    new Channel { Id = 111222333UL, Name = "channel1", Type = ChannelType.GuildText, GuildId = 987654321UL }
                },
                Members = new[]
                {
                    new GuildMember { User = new User { Id = 123456789UL, Username = "user1" } }
                },
                Roles = new[]
                {
                    new Role { Id = 777888999UL, Name = "role1" }
                },
                Emojis = new[]
                {
                    new Emoji { Id = 999888777UL, Name = "emoji1" }
                }
            };

            _cache.CacheGuildData(guild);

            _cache.GetGuild(guild.Id).Should().NotBeNull();
            _cache.GetChannel(111222333UL).Should().NotBeNull();
            _cache.GetGuildMember(987654321UL, 123456789UL).Should().NotBeNull();
            _cache.GetRole(987654321UL, 777888999UL).Should().NotBeNull();
            _cache.GetEmoji(987654321UL, 999888777UL).Should().NotBeNull();
        }

        [Fact]
        public void RemoveGuild_RemovesAllGuildData()
        {
            var guild = new Guild
            {
                Id = 987654321UL,
                Name = "Test Guild",
                Channels = new[]
                {
                    new Channel { Id = 111222333UL, Name = "channel1", Type = ChannelType.GuildText, GuildId = 987654321UL }
                },
                Members = new[]
                {
                    new GuildMember { User = new User { Id = 123456789UL, Username = "user1" } }
                },
                Roles = new[]
                {
                    new Role { Id = 777888999UL, Name = "role1" }
                },
                Emojis = new[]
                {
                    new Emoji { Id = 999888777UL, Name = "emoji1" }
                }
            };

            _cache.CacheGuildData(guild);
            _cache.RemoveGuild(guild.Id);

            _cache.GetGuild(guild.Id).Should().BeNull();
            _cache.GetChannel(111222333UL).Should().BeNull();
            _cache.GetGuildMember(987654321UL, 123456789UL).Should().BeNull();
            _cache.GetRole(987654321UL, 777888999UL).Should().BeNull();
            _cache.GetEmoji(987654321UL, 999888777UL).Should().BeNull();
        }

        [Fact]
        public void Clear_RemovesAllData()
        {
            var user = new User { Id = 123UL, Username = "test" };
            var guild = new Guild { Id = 456UL, Name = "Test Guild" };

            _cache.CacheUser(user);
            _cache.CacheGuild(guild);
            _cache.Clear();

            _cache.GetUser(user.Id).Should().BeNull();
            _cache.GetGuild(guild.Id).Should().BeNull();
        }

        [Fact]
        public void GetCacheStats_ReturnsValidStatistics()
        {
            var user = new User { Id = 123UL, Username = "test" };
            var guild = new Guild { Id = 456UL, Name = "Test Guild" };

            _cache.CacheUser(user);
            _cache.CacheGuild(guild);

            var stats = _cache.GetCacheStats();

            stats.Should().NotBeNull();
            stats.UserCount.Should().BeGreaterThanOrEqualTo(1);
            stats.GuildCount.Should().BeGreaterThanOrEqualTo(1);
            stats.MemoryUsage.Should().BeGreaterThan(0);
            stats.Hits.Should().Be(0);
            stats.Misses.Should().Be(0);
        }

        [Fact]
        public void CacheStatistics_TrackHitsAndMisses()
        {
            var user = new User { Id = 123UL, Username = "test" };
            _cache.CacheUser(user);

            // Hit
            _cache.GetUser(user.Id);

            // Miss
            _cache.GetUser(999999UL);

            var stats = _cache.GetCacheStats();
            stats.Hits.Should().Be(1);
            stats.Misses.Should().Be(1);
            stats.HitRatio.Should().Be(0.5);
        }

        [Fact]
        public void IsHealthy_ReturnsTrue()
        {
            _cache.IsHealthy().Should().BeTrue();
        }

        [Fact]
        public async Task AsyncOperations_WorkCorrectly()
        {
            var user = new User { Id = 123UL, Username = "test" };

            await _cache.CacheUserAsync(user);
            var retrieved = await _cache.GetUserAsync(user.Id);

            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task AsyncRemoveOperations_WorkCorrectly()
        {
            var channel = new Channel
            {
                Id = 111222333UL,
                Name = "test-channel",
                Type = ChannelType.GuildText,
                GuildId = 987654321UL
            };

            await _cache.CacheChannelAsync(channel);
            await _cache.RemoveChannelAsync(channel.Id);
            var retrieved = await _cache.GetChannelAsync(channel.Id);

            retrieved.Should().BeNull();
        }

        [Fact]
        public void EntityEvictedEvent_FiresOnEviction()
        {
            var options = new CacheOptions
            {
                MaxUsers = 2,
                DefaultExpiration = null
            };
            var cache = new MemoryCacheProvider(options);
            var eventFired = false;
            string? evictedEntityType = string.Empty;
            ulong evictedEntityId = 0;

            cache.EntityEvicted += (sender, args) =>
            {
                eventFired = true;
                evictedEntityType = args.EntityType;
                evictedEntityId = args.EntityId;
            };

            // Add more users than the max
            for (ulong i = 1; i <= 5; i++)
            {
                cache.CacheUser(new User { Id = i, Username = $"user{i}" });
            }

            eventFired.Should().BeTrue();
            evictedEntityType.Should().Be("User");
        }

        [Fact]
        public void CacheClearedEvent_FiresOnClear()
        {
            var eventFired = false;

            _cache.CacheCleared += (sender, args) =>
            {
                eventFired = true;
            };

            _cache.Clear();

            eventFired.Should().BeTrue();
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
