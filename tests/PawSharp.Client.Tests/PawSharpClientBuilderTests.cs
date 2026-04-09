#nullable enable
using System;
using System.Reflection;
using FluentAssertions;
using Moq;
using PawSharp.Cache.Interfaces;
using PawSharp.Client;
using PawSharp.Core.Enums;
using PawSharp.Core.Models;
using Xunit;

namespace PawSharp.Client.Tests;

public class PawSharpClientBuilderTests
{
    [Fact]
    public void Build_WithoutToken_ThrowsInvalidOperationException()
    {
        var builder = new PawSharpClientBuilder();

        Action act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Call WithToken*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-discord-token")]
    [InlineData("Bot not-a-discord-token")]
    public void WithToken_WithInvalidToken_ThrowsArgumentException(string token)
    {
        var builder = new PawSharpClientBuilder();

        Action act = () => builder.WithToken(token);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithToken_WithRawToken_PrefixesWithBot()
    {
        var builder = new PawSharpClientBuilder()
            .WithToken("abc.def.ghi");

        var token = typeof(PawSharpClientBuilder)
            .GetField("_token", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(builder) as string;

        token.Should().Be("Bot abc.def.ghi");
    }

    [Fact]
    public void WithApiVersion_LessThanSix_ThrowsArgumentOutOfRangeException()
    {
        var builder = new PawSharpClientBuilder();

        Action act = () => builder.WithApiVersion(5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithSharding_WithInvalidShardParameters_ThrowsArgumentOutOfRangeException()
    {
        var builder = new PawSharpClientBuilder();

        Action act = () => builder.WithSharding(1, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Build_WithPresenceAndIntents_PropagatesOptionsIntoClient()
    {
        var client = new PawSharpClientBuilder()
            .WithToken("abc.def.ghi")
            .WithIntents(GatewayIntents.Guilds | GatewayIntents.GuildMessages)
            .WithPresence("Running tests", status: "dnd")
            .Build();

        var options = ReadPrivateField<PawSharpOptions>(client, "_options");

        options.Token.Should().Be("Bot abc.def.ghi");
        options.Intents.Should().Be(GatewayIntents.Guilds | GatewayIntents.GuildMessages);
        options.Presence.Should().NotBeNull();
        options.Presence!.Status.Should().Be("dnd");
        options.Presence.ActivityName.Should().Be("Running tests");
    }

    [Fact]
    public void UseCache_WithCustomCache_UsesSameInstanceInClient()
    {
        var cache = new Mock<IEntityCache>();

        var client = new PawSharpClientBuilder()
            .WithToken("abc.def.ghi")
            .UseCache(cache.Object)
            .Build();

        var cacheField = ReadPrivateField<IEntityCache>(client, "_cache");
        cacheField.Should().BeSameAs(cache.Object);
    }

    private static T ReadPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull($"Field {fieldName} should exist for test validation.");

        var value = field!.GetValue(instance);
        value.Should().BeAssignableTo<T>();
        return (T)value!;
    }
}
