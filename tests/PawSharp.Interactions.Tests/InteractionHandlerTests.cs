#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Gateway.Events;
using PawSharp.Interactions;
using Xunit;

namespace PawSharp.Interactions.Tests;

/// <summary>
/// Unit tests for <see cref="InteractionHandler"/> routing logic added in alpha11.
/// </summary>
public class InteractionHandlerTests
{
    private readonly Mock<IDiscordRestClient> _restMock = new();
    private readonly InteractionHandler _handler;

    public InteractionHandlerTests()
    {
        // CreateInteractionResponseAsync is called internally for autocomplete responses
        _restMock
            .Setup(r => r.CreateInteractionResponseAsync(
                It.IsAny<ulong>(),
                It.IsAny<string>(),
                It.IsAny<InteractionResponse>()))
            .ReturnsAsync(true);

        _handler = new InteractionHandler(_restMock.Object);
    }

    // ─────────────────────────────────────────────
    //  Slash command routing  (type=2, data.type=1)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_SlashCommand_To_Registered_Handler()
    {
        InteractionCreateEvent? received = null;
        _handler.RegisterCommand("ping", evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var interaction = BuildApplicationCommandInteraction("ping", commandType: 1);
        await _handler.HandleInteractionAsync(interaction);

        received.Should().NotBeNull();
        received!.Data!.Name.Should().Be("ping");
    }

    [Fact]
    public async Task HandleInteractionAsync_Does_Not_Throw_For_Unregistered_SlashCommand()
    {
        var interaction = BuildApplicationCommandInteraction("unknown", commandType: 1);
        // Should simply do nothing, not throw
        await _handler.Invoking(h => h.HandleInteractionAsync(interaction))
                      .Should().NotThrowAsync();
    }

    // ─────────────────────────────────────────────
    //  Message component routing  (type=3)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_MessageComponent_By_CustomId()
    {
        InteractionCreateEvent? received = null;
        _handler.RegisterComponent("my-button", evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var interaction = new InteractionCreateEvent
        {
            Type = (int)InteractionType.MessageComponent,
            Data = new InteractionData { CustomId = "my-button" }
        };

        await _handler.HandleInteractionAsync(interaction);

        received.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    //  Autocomplete routing  (type=4)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_Autocomplete_And_Responds_With_Choices()
    {
        var choices = new List<AutocompleteChoice>
        {
            new() { Name = "Option A", Value = "a" },
            new() { Name = "Option B", Value = "b" }
        };

        _handler.RegisterAutocomplete("search", _ => Task.FromResult(choices));

        var interaction = new InteractionCreateEvent
        {
            Id = 1UL,
            Token = "test-token",
            Type = (int)InteractionType.ApplicationCommandAutocomplete,
            Data = new InteractionData { Name = "search" }
        };

        await _handler.HandleInteractionAsync(interaction);

        // Verify the REST client was called with the autocomplete result response
        _restMock.Verify(r => r.CreateInteractionResponseAsync(
            1UL,
            "test-token",
            It.Is<InteractionResponse>(resp =>
                resp.Type == (int)InteractionResponseType.ApplicationCommandAutocompleteResult &&
                resp.Data != null &&
                resp.Data.Choices != null &&
                resp.Data.Choices.Count == 2)),
            Times.Once);
    }

    // ─────────────────────────────────────────────
    //  User context menu  (type=2, data.type=2)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_UserContextMenu()
    {
        InteractionCreateEvent? received = null;
        _handler.RegisterUserContextMenu("View Profile", evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var interaction = BuildApplicationCommandInteraction("View Profile", commandType: 2);
        await _handler.HandleInteractionAsync(interaction);

        received.Should().NotBeNull();
        received!.Data!.Name.Should().Be("View Profile");
    }

    // ─────────────────────────────────────────────
    //  Message context menu  (type=2, data.type=3)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_MessageContextMenu()
    {
        InteractionCreateEvent? received = null;
        _handler.RegisterMessageContextMenu("Report Message", evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var interaction = BuildApplicationCommandInteraction("Report Message", commandType: 3);
        await _handler.HandleInteractionAsync(interaction);

        received.Should().NotBeNull();
        received!.Data!.Name.Should().Be("Report Message");
    }

    // ─────────────────────────────────────────────
    //  Modal submit  (type=5)
    // ─────────────────────────────────────────────

    [Fact]
    public async Task HandleInteractionAsync_Routes_ModalSubmit_By_CustomId()
    {
        InteractionCreateEvent? received = null;
        _handler.RegisterModal("feedback-modal", evt =>
        {
            received = evt;
            return Task.CompletedTask;
        });

        var interaction = new InteractionCreateEvent
        {
            Type = (int)InteractionType.ModalSubmit,
            Data = new InteractionData { CustomId = "feedback-modal" }
        };

        await _handler.HandleInteractionAsync(interaction);

        received.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────
    //  Registration helpers
    // ─────────────────────────────────────────────

    [Fact]
    public async Task RegisterCommand_Overwrites_Previous_Handler()
    {
        var callCount = 0;
        _handler.RegisterCommand("test", _ => { callCount++; return Task.CompletedTask; });
        _handler.RegisterCommand("test", _ => { callCount += 10; return Task.CompletedTask; });

        var interaction = BuildApplicationCommandInteraction("test", commandType: 1);
        await _handler.HandleInteractionAsync(interaction);

        // Only the second handler should have run
        callCount.Should().Be(10);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    private static InteractionCreateEvent BuildApplicationCommandInteraction(string name, int commandType)
    {
        return new InteractionCreateEvent
        {
            Type = (int)InteractionType.ApplicationCommand,
            Data = new InteractionData
            {
                Name = name,
                Type = commandType
            }
        };
    }
}
