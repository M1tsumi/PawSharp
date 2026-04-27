#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Cache.Interfaces;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using PawSharp.Interactivity;
using PawSharp.Interactivity.Extensions;
using Xunit;

namespace PawSharp.Interactivity.Tests;

/// <summary>
/// Unit tests for the PawSharp.Interactivity extension.
/// </summary>
public class InteractivityTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DiscordClient client, EventDispatcher dispatcher, Mock<IDiscordRestClient> restMock) BuildTestClient()
    {
        var dispatcher  = new EventDispatcher();
        var restMock    = new Mock<IDiscordRestClient>();
        var cacheMock   = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();

        gatewayMock.SetupGet(g => g.Events).Returns(dispatcher);
        gatewayMock.Setup(g => g.CurrentState).Returns(GatewayState.Connected);

        restMock.Setup(r => r.CreateMessageAsync(It.IsAny<ulong>(), It.IsAny<CreateMessageRequest>()))
                .ReturnsAsync(new Message { Id = 1UL });
        restMock.Setup(r => r.CreateReactionAsync(It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<string>()))
                .ReturnsAsync(true);
        restMock.Setup(r => r.DeleteUserReactionAsync(
                    It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<ulong>()))
                .ReturnsAsync(true);
        restMock.Setup(r => r.DeleteAllReactionsAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(true);
        restMock.Setup(r => r.GetAnswerVotersAsync(
                    It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<ulong?>()))
                .ReturnsAsync(new List<User>());

        var options = new PawSharpOptions { Token = "Bot test.token.value" };
        var client  = new DiscordClient(
            options, cacheMock.Object, NullLogger<DiscordClient>.Instance,
            restMock.Object, gatewayMock.Object);

        return (client, dispatcher, restMock);
    }

    // ── Configuration defaults ────────────────────────────────────────────────

    [Fact]
    public void InteractivityConfiguration_Has_Correct_Defaults()
    {
        var config = new InteractivityConfiguration();

        config.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        config.PollBehaviour.Should().Be(PollBehaviour.DeleteEmojis);
    }

    [Fact]
    public void PaginationEmojis_Has_Correct_Defaults()
    {
        var emojis = new PaginationEmojis();

        emojis.Left.Should().Be("◀");
        emojis.Right.Should().Be("▶");
        emojis.SkipLeft.Should().Be("⏮");
        emojis.SkipRight.Should().Be("⏭");
        emojis.Stop.Should().Be("⏹");
    }

    [Fact]
    public void InteractivityExtension_Exposes_Config_Properties()
    {
        var customConfig = new InteractivityConfiguration
        {
            Timeout      = TimeSpan.FromMinutes(2),
            PollBehaviour = PollBehaviour.KeepEmojis,
            PaginationEmojis = new PaginationEmojis { Left = "⬅", Right = "➡" },
        };
        var ext = new InteractivityExtension(customConfig);

        ext.Timeout.Should().Be(TimeSpan.FromMinutes(2));
        ext.PollBehaviour.Should().Be(PollBehaviour.KeepEmojis);
        ext.PaginationEmojis.Left.Should().Be("⬅");
        ext.PaginationEmojis.Right.Should().Be("➡");
    }

    // ── Page generation ───────────────────────────────────────────────────────

    [Fact]
    public void GeneratePagesInContent_Returns_Empty_For_Empty_Input()
    {
        var ext   = new InteractivityExtension();
        var pages = ext.GeneratePagesInContent(string.Empty);

        pages.Should().BeEmpty();
    }

    [Fact]
    public void GeneratePagesInContent_Returns_Single_Page_For_Short_Content()
    {
        var ext   = new InteractivityExtension();
        var pages = ext.GeneratePagesInContent("Hello, world!", maxLength: 2000).ToList();

        pages.Should().HaveCount(1);
        pages[0].Content.Should().Be("Hello, world!");
    }

    [Fact]
    public void GeneratePagesInContent_Splits_Content_Correctly()
    {
        var ext     = new InteractivityExtension();
        var content = new string('A', 4500);
        var pages   = ext.GeneratePagesInContent(content, maxLength: 2000).ToList();

        pages.Should().HaveCount(3);                 // 2000 + 2000 + 500
        pages[0].Content.Should().HaveLength(2000);
        pages[1].Content.Should().HaveLength(2000);
        pages[2].Content.Should().HaveLength(500);
    }

    [Fact]
    public void GeneratePagesInEmbed_Splits_Into_Embeds()
    {
        var ext     = new InteractivityExtension();
        var content = new string('X', 8500);
        var pages   = ext.GeneratePagesInEmbed(content, maxLength: 4000).ToList();

        pages.Should().HaveCount(3);                 // 4000 + 4000 + 500
        pages[0].Embed.Should().NotBeNull();
        pages[0].Embed!.Description.Should().HaveLength(4000);
        pages[2].Embed!.Description.Should().HaveLength(500);
    }

    // ── InteractivityResult ───────────────────────────────────────────────────

    [Fact]
    public void InteractivityResult_TimedOut_Defaults_To_False()
    {
        var result = new InteractivityResult<string> { Result = "hello" };

        result.TimedOut.Should().BeFalse();
        result.Result.Should().Be("hello");
    }

    [Fact]
    public void InteractivityResult_With_TimedOut_True_Has_No_Result()
    {
        var result = new InteractivityResult<int> { TimedOut = true };

        result.TimedOut.Should().BeTrue();
        result.Result.Should().Be(0);
    }

    // ── UseInteractivity / GetExtension ──────────────────────────────────────

    [Fact]
    public void UseInteractivity_Returns_Same_Instance_For_Same_Client()
    {
        var (client, _, _) = BuildTestClient();

        var ext1 = client.UseInteractivity();
        var ext2 = client.UseInteractivity();

        ext1.Should().BeSameAs(ext2);
    }

    [Fact]
    public void UseInteractivity_Applies_Custom_Configuration()
    {
        var (client, _, _) = BuildTestClient();
        var config = new InteractivityConfiguration { Timeout = TimeSpan.FromSeconds(60) };

        var ext = client.UseInteractivity(config);

        ext.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    // ── WaitForReactionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task WaitForReactionAsync_Returns_Result_When_Reaction_Matches()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 500UL, ChannelId = 1UL };
        var user    = new User    { Id = 42UL };

        var waitTask = message.WaitForReactionAsync(
            client, user, emoji: "👍", timeout: TimeSpan.FromSeconds(5));

        // Simulate the matching reaction event
        await dispatcher.DispatchAsync("MESSAGE_REACTION_ADD", new MessageReactionAddEvent
        {
            UserId    = 42UL,
            MessageId = 500UL,
            ChannelId = 1UL,
            Emoji     = new Emoji { Name = "👍" },
        });

        var result = await waitTask;

        result.TimedOut.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result!.Emoji.Name.Should().Be("👍");
    }

    [Fact]
    public async Task WaitForReactionAsync_Times_Out_When_No_Reaction_Arrives()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 501UL, ChannelId = 1UL };
        var user    = new User    { Id = 42UL };

        var result = await message.WaitForReactionAsync(
            client, user, timeout: TimeSpan.FromMilliseconds(50));

        result.TimedOut.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForReactionAsync_Ignores_Reaction_From_Different_User()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 502UL, ChannelId = 1UL };
        var targetUser = new User { Id = 10UL };

        var waitTask = message.WaitForReactionAsync(
            client, targetUser, timeout: TimeSpan.FromMilliseconds(200));

        // Different user reacts
        await dispatcher.DispatchAsync("MESSAGE_REACTION_ADD", new MessageReactionAddEvent
        {
            UserId    = 99UL,   // ← not target user
            MessageId = 502UL,
            Emoji     = new Emoji { Name = "❤" },
        });

        var result = await waitTask;

        // Should have timed out since the correct user never reacted
        result.TimedOut.Should().BeTrue();
    }

    // ── Reaction.Me correctness ───────────────────────────────────────────────

    [Fact]
    public async Task WaitForReactionAsync_Sets_Me_True_When_Bot_Is_The_Reactor()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        // Simulate the bot's own ID being known via the CurrentUser field
        // (In production this is populated by the READY event.)
        var botUser = new User { Id = 777UL, Bot = true };
        typeof(DiscordClient)
            .GetProperty(nameof(DiscordClient.CurrentUser))!
            .SetValue(client, botUser);

        var message = new Message { Id = 503UL, ChannelId = 1UL };

        var waitTask = message.WaitForReactionAsync(
            client, botUser, timeout: TimeSpan.FromSeconds(3));

        await dispatcher.DispatchAsync("MESSAGE_REACTION_ADD", new MessageReactionAddEvent
        {
            UserId    = 777UL,   // ← the bot itself
            MessageId = 503UL,
            Emoji     = new Emoji { Name = "✅" },
        });

        var result = await waitTask;

        result.Result!.Me.Should().BeTrue("the bot reacted, so Me should be true");
    }

    [Fact]
    public async Task WaitForReactionAsync_Sets_Me_False_When_Other_User_Reacts()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var botUser  = new User { Id = 777UL, Bot = true };
        var humanUser = new User { Id = 888UL };

        typeof(DiscordClient)
            .GetProperty(nameof(DiscordClient.CurrentUser))!
            .SetValue(client, botUser);

        var message = new Message { Id = 504UL, ChannelId = 1UL };

        var waitTask = message.WaitForReactionAsync(
            client, humanUser, timeout: TimeSpan.FromSeconds(3));

        await dispatcher.DispatchAsync("MESSAGE_REACTION_ADD", new MessageReactionAddEvent
        {
            UserId    = 888UL,   // ← human user, not the bot
            MessageId = 504UL,
            Emoji     = new Emoji { Name = "🔥" },
        });

        var result = await waitTask;

        result.Result!.Me.Should().BeFalse("a non-bot user reacted, so Me should be false");
    }

    // ── WaitForButtonAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task WaitForButtonAsync_Returns_Result_On_Matching_Component_Click()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 600UL, ChannelId = 1UL };
        var user    = new User    { Id = 42UL };

        var waitTask = message.WaitForButtonAsync(
            client, user: user, customId: "confirm", timeout: TimeSpan.FromSeconds(5));

        await dispatcher.DispatchAsync("INTERACTION_CREATE", new InteractionCreateEvent
        {
            Type    = 3, // MESSAGE_COMPONENT
            Message = new Message { Id = 600UL },
            Member  = new GuildMember { User = user },
            Data    = new PawSharp.Gateway.Events.InteractionData
            {
                CustomId      = "confirm",
                ComponentType = 2, // Button
            },
        });

        var result = await waitTask;

        result.TimedOut.Should().BeFalse();
        result.Result!.Data!.CustomId.Should().Be("confirm");
    }

    [Fact]
    public async Task WaitForButtonAsync_Times_Out_When_No_Click_Arrives()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 601UL };
        var result  = await message.WaitForButtonAsync(
            client, timeout: TimeSpan.FromMilliseconds(50));

        result.TimedOut.Should().BeTrue();
    }

    // ── WaitForSelectAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task WaitForSelectAsync_Returns_Result_On_Matching_Selection()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 700UL, ChannelId = 1UL };

        var waitTask = message.WaitForSelectAsync(
            client, customId: "color-select", timeout: TimeSpan.FromSeconds(5));

        await dispatcher.DispatchAsync("INTERACTION_CREATE", new InteractionCreateEvent
        {
            Type    = 3, // MESSAGE_COMPONENT
            Message = new Message { Id = 700UL },
            Data    = new PawSharp.Gateway.Events.InteractionData
            {
                CustomId      = "color-select",
                ComponentType = 3, // String select
                Values        = new List<string> { "blue" },
            },
        });

        var result = await waitTask;

        result.TimedOut.Should().BeFalse();
        result.Result!.Data!.Values.Should().Contain("blue");
    }

    // ── WaitForModalAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task WaitForModalAsync_Returns_Result_On_Modal_Submit()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 800UL, ChannelId = 1UL };
        var user    = new User    { Id = 42UL };

        var waitTask = message.WaitForModalAsync(
            client, user: user, customId: "feedback-form", timeout: TimeSpan.FromSeconds(5));

        await dispatcher.DispatchAsync("INTERACTION_CREATE", new InteractionCreateEvent
        {
            Type    = 5, // ModalSubmit
            Member  = new GuildMember { User = user },
            Data    = new PawSharp.Gateway.Events.InteractionData
            {
                CustomId = "feedback-form",
                Components = new List<MessageComponent>(),
            },
        });

        var result = await waitTask;

        result.TimedOut.Should().BeFalse();
        result.Result!.Data!.CustomId.Should().Be("feedback-form");
    }

    [Fact]
    public async Task WaitForModalAsync_Times_Out_When_No_Submission_Arrives()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 801UL };
        var result  = await message.WaitForModalAsync(
            client, timeout: TimeSpan.FromMilliseconds(50));

        result.TimedOut.Should().BeTrue();
    }

    // ── WaitForReactionRemoveAsync ─────────────────────────────────────────────

    [Fact]
    public async Task WaitForReactionRemoveAsync_Returns_Result_On_Reaction_Remove()
    {
        var (client, dispatcher, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 900UL, ChannelId = 1UL };
        var user    = new User    { Id = 42UL };

        var waitTask = message.WaitForReactionRemoveAsync(
            client, user, emoji: "👍", timeout: TimeSpan.FromSeconds(5));

        await dispatcher.DispatchAsync("MESSAGE_REACTION_REMOVE", new MessageReactionRemoveEvent
        {
            UserId    = 42UL,
            MessageId = 900UL,
            ChannelId = 1UL,
            Emoji     = new Emoji { Name = "👍" },
        });

        var result = await waitTask;

        result.TimedOut.Should().BeFalse();
        result.Result!.Emoji.Name.Should().Be("👍");
    }

    [Fact]
    public async Task WaitForReactionRemoveAsync_Times_Out_When_No_Remove_Arrives()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 901UL };
        var user    = new User    { Id = 42UL };

        var result = await message.WaitForReactionRemoveAsync(
            client, user, timeout: TimeSpan.FromMilliseconds(50));

        result.TimedOut.Should().BeTrue();
    }

    // ── Poll methods ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPollAnswerVotersAsync_Throws_When_No_Poll()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 1000UL, Poll = null };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => message.GetPollAnswerVotersAsync(client, 1));
    }

    [Fact]
    public async Task GetPollAnswerVotersAsync_Returns_Voters_When_Poll_Exists()
    {
        var (client, _, restMock) = BuildTestClient();
        client.UseInteractivity();

        var expectedVoters = new List<User>
        {
            new User { Id = 1UL, Username = "user1" },
            new User { Id = 2UL, Username = "user2" },
        };

        restMock.Setup(r => r.GetAnswerVotersAsync(
                    It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<ulong?>()))
                .ReturnsAsync(expectedVoters);

        var message = new Message { Id = 1001UL, Poll = new Poll() };

        var voters = await message.GetPollAnswerVotersAsync(client, 1);

        voters.Should().HaveCount(2);
        voters![0].Username.Should().Be("user1");
    }

    [Fact]
    public async Task EndPollAsync_Throws_When_No_Poll()
    {
        var (client, _, _) = BuildTestClient();
        client.UseInteractivity();

        var message = new Message { Id = 1100UL, Poll = null };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => message.EndPollAsync(client));
    }

    [Fact]
    public async Task EndPollAsync_Calls_Rest_When_Poll_Exists()
    {
        var (client, _, restMock) = BuildTestClient();
        client.UseInteractivity();

        var updatedMessage = new Message { Id = 1101UL, Poll = new Poll { Results = new PollResults { IsFinalized = true } } };

        restMock.Setup(r => r.EndPollAsync(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(updatedMessage);

        var message = new Message { Id = 1101UL, Poll = new Poll() };

        var result = await message.EndPollAsync(client);

        result.Should().NotBeNull();
        result!.Poll!.Results!.IsFinalized.Should().BeTrue();
    }
}
