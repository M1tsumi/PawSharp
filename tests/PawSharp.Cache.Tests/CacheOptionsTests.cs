#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace PawSharp.Cache.Tests;

public class CacheOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new CacheOptions();

        options.MaxGuilds.Should().Be(1000);
        options.MaxChannels.Should().Be(5000);
        options.MaxUsers.Should().Be(20000);
        options.MaxMessages.Should().Be(10000);
        options.MaxMembers.Should().Be(50000);
        options.MaxRoles.Should().Be(10000);
        options.MaxEmojis.Should().Be(5000);
        options.DefaultExpiration.Should().BeNull();
        options.GenericCacheExpiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var options = new CacheOptions
        {
            MaxGuilds = 100,
            MaxChannels = 200,
            MaxUsers = 300,
            MaxMessages = 400,
            MaxMembers = 500,
            MaxRoles = 600,
            MaxEmojis = 700,
            DefaultExpiration = TimeSpan.FromMinutes(30),
            UserExpiration = TimeSpan.FromMinutes(10),
            GuildExpiration = TimeSpan.FromMinutes(20),
            ChannelExpiration = TimeSpan.FromMinutes(15),
            MessageExpiration = TimeSpan.FromMinutes(5),
            MemberExpiration = TimeSpan.FromMinutes(10),
            RoleExpiration = TimeSpan.FromMinutes(10),
            EmojiExpiration = TimeSpan.FromMinutes(10),
            GenericCacheExpiration = TimeSpan.FromMinutes(45)
        };

        options.MaxGuilds.Should().Be(100);
        options.MaxChannels.Should().Be(200);
        options.MaxUsers.Should().Be(300);
        options.MaxMessages.Should().Be(400);
        options.MaxMembers.Should().Be(500);
        options.MaxRoles.Should().Be(600);
        options.MaxEmojis.Should().Be(700);
        options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
        options.UserExpiration.Should().Be(TimeSpan.FromMinutes(10));
        options.GuildExpiration.Should().Be(TimeSpan.FromMinutes(20));
        options.ChannelExpiration.Should().Be(TimeSpan.FromMinutes(15));
        options.MessageExpiration.Should().Be(TimeSpan.FromMinutes(5));
        options.MemberExpiration.Should().Be(TimeSpan.FromMinutes(10));
        options.RoleExpiration.Should().Be(TimeSpan.FromMinutes(10));
        options.EmojiExpiration.Should().Be(TimeSpan.FromMinutes(10));
        options.GenericCacheExpiration.Should().Be(TimeSpan.FromMinutes(45));
    }
}
