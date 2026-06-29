#nullable enable
using System;
using System.Linq;
using FluentAssertions;
using PawSharp.Core.Entities;
using PawSharp.Interactions.Builders;
using Xunit;

namespace PawSharp.Interactions.Tests;

public class InteractionResponseBuilderTests
{
    [Fact]
    public void Build_Default_ReturnsChannelMessageWithSource()
    {
        var response = new InteractionResponseBuilder()
            .WithContent("Hello")
            .Build();

        response.Type.Should().Be(4);
        response.Data!.Content.Should().Be("Hello");
    }

    [Fact]
    public void AsEphemeral_SetsFlag()
    {
        var response = new InteractionResponseBuilder()
            .WithContent("Secret")
            .AsEphemeral()
            .Build();

        response.Data!.Flags.Should().Be(64);
    }

    [Fact]
    public void AsUpdateMessage_SetsType7()
    {
        var response = new InteractionResponseBuilder()
            .WithContent("Updated")
            .AsUpdateMessage()
            .Build();

        response.Type.Should().Be(7);
    }

    [Fact]
    public void AsDeferredChannelMessage_SetsType5()
    {
        var response = new InteractionResponseBuilder()
            .AsDeferredChannelMessage()
            .Build();

        response.Type.Should().Be(5);
        response.Data!.Content.Should().BeNull();
    }

    [Fact]
    public void AsDeferredUpdateMessage_SetsType6()
    {
        var response = new InteractionResponseBuilder()
            .AsDeferredUpdateMessage()
            .Build();

        response.Type.Should().Be(6);
        response.Data!.Content.Should().BeNull();
    }

    [Fact]
    public void AddEmbed_AddsToResponse()
    {
        var embed = new Embed { Title = "Test" };
        var response = new InteractionResponseBuilder()
            .AddEmbed(embed)
            .Build();

        response.Data!.Embeds.Should().HaveCount(1);
        response.Data.Embeds![0].Title.Should().Be("Test");
    }

    [Fact]
    public void AddEmbed_MoreThan10_Throws()
    {
        var builder = new InteractionResponseBuilder();
        for (int i = 0; i < 10; i++)
            builder.AddEmbed(new Embed { Title = $"Embed {i}" });

        Action act = () => builder.AddEmbed(new Embed());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddActionRow_AddsToResponse()
    {
        var row = new ActionRow { Components = new() };
        var response = new InteractionResponseBuilder()
            .AddActionRow(row)
            .Build();

        response.Data!.Components.Should().HaveCount(1);
    }

    [Fact]
    public void AddActionRow_ByCallback_Works()
    {
        var response = new InteractionResponseBuilder()
            .AddActionRow(row => row.AddButton(new ButtonBuilder("btn", "Click")))
            .Build();

        response.Data!.Components.Should().HaveCount(1);
    }

    [Fact]
    public void AddActionRow_MoreThan5_Throws()
    {
        var builder = new InteractionResponseBuilder();
        for (int i = 0; i < 5; i++)
            builder.AddActionRow(new ActionRow { Components = new() });

        Action act = () => builder.AddActionRow(new ActionRow());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithFlags_SetsFlags()
    {
        var response = new InteractionResponseBuilder()
            .WithFlags(128)
            .Build();

        response.Data!.Flags.Should().Be(128);
    }

    [Fact]
    public void DeferredResponse_StripsContent()
    {
        var response = new InteractionResponseBuilder()
            .WithContent("Should be null")
            .AsDeferredChannelMessage()
            .Build();

        response.Data!.Content.Should().BeNull();
        response.Data.Embeds.Should().BeNull();
        response.Data.Components.Should().BeNull();
    }

    [Fact]
    public void DeferredUpdateResponse_StripsContent()
    {
        var response = new InteractionResponseBuilder()
            .WithContent("Should be null")
            .AddEmbed(new Embed())
            .AsDeferredUpdateMessage()
            .Build();

        response.Data!.Content.Should().BeNull();
        response.Data.Embeds.Should().BeNull();
    }

    [Fact]
    public void AddEmbed_InDeferred_Stripped()
    {
        var response = new InteractionResponseBuilder()
            .AddEmbed(new Embed { Title = "Lost" })
            .AsDeferredChannelMessage()
            .Build();

        response.Data!.Embeds.Should().BeNull();
    }
}
