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
using PawSharp.Commands;
using PawSharp.Commands.Preconditions;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="CommandsExtension"/> prefix command framework.
/// </summary>
public class CommandsExtensionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal <see cref="DiscordClient"/> backed by a real <see cref="EventDispatcher"/>
    /// and mock stubs for all network-bound dependencies (REST, cache, gateway).
    /// The returned dispatcher can be used to fire synthetic gateway events in tests.
    /// </summary>
    private static (DiscordClient client, EventDispatcher dispatcher) BuildTestClient()
    {
        var dispatcher = new EventDispatcher();
        var restMock   = new Mock<IDiscordRestClient>();
        var cacheMock  = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();

        // Wire the real dispatcher so event subscriptions actually work
        gatewayMock.SetupGet(g => g.Events).Returns(dispatcher);
        gatewayMock.Setup(g => g.CurrentState).Returns(GatewayState.Connected);

        // Stub REST calls that CommandsExtension / DiscordClient fire internally
        restMock.Setup(r => r.CreateMessageAsync(It.IsAny<ulong>(), It.IsAny<CreateMessageRequest>()))
                .ReturnsAsync(new Message { Id = 1UL });

        var options = new PawSharpOptions { Token = "Bot test.token.value" };

        var client = new DiscordClient(
            options,
            cacheMock.Object,
            NullLogger<DiscordClient>.Instance,
            restMock.Object,
            gatewayMock.Object);

        return (client, dispatcher);
    }

    /// <summary>
    /// Synthesises a <see cref="MessageCreateEvent"/> that looks like a human typing a bot command.
    /// </summary>
    private static MessageCreateEvent BuildMessageEvent(string content, ulong channelId = 1UL, ulong guildId = 100UL)
        => new()
        {
            Id        = 999UL,
            ChannelId = channelId,
            GuildId   = guildId,
            Content   = content,
            Author    = new User { Id = 42UL, Username = "Tester", Bot = false },
        };

    // ── Registration tests ────────────────────────────────────────────────────

    [Fact]
    public void RegisterModule_Discovers_CommandAttribute_Methods()
    {
        var ext = new CommandsExtension("!");
        var (client, _) = BuildTestClient();

        ext.RegisterModule(client, new SimpleModule());

        var commands = ext.GetRegisteredCommands();
        commands.Should().ContainSingle(c => c.Name == "ping");
    }

    [Fact]
    public void RegisterModule_Registers_Aliases()
    {
        var ext = new CommandsExtension("!");
        var (client, _) = BuildTestClient();

        ext.RegisterModule(client, new AliasModule());

        var commands = ext.GetRegisteredCommands();
        // Distinct() is applied in GetRegisteredCommands — aliases share the same Command object
        commands.Should().ContainSingle(c => c.Name == "hello");
        commands.First().Aliases.Should().Contain("hi").And.Contain("hey");
    }

    [Fact]
    public void RegisterModule_Registers_Description()
    {
        var ext = new CommandsExtension("!");
        var (client, _) = BuildTestClient();

        ext.RegisterModule(client, new SimpleModule());

        var commands = ext.GetRegisteredCommands();
        commands.Should().ContainSingle(c => c.Name == "ping" && c.Description == "Pings the bot.");
    }

    [Fact]
    public void UnregisterModule_Removes_Commands()
    {
        var ext = new CommandsExtension("!");
        var (client, _) = BuildTestClient();
        var module = new SimpleModule();

        ext.RegisterModule(client, module);
        ext.UnregisterModule(module);

        ext.GetRegisteredCommands().Should().BeEmpty();
    }

    // ── Execution tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task OnMessageCreate_Executes_Matching_Command()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();
        var module = new TrackingModule();

        ext.RegisterModule(client, module);

        await dispatcher.DispatchAsync(
            "MESSAGE_CREATE",
            BuildMessageEvent("!track some-arg"));

        module.LastContext.Should().NotBeNull();
        module.LastContext!.CommandName.Should().Be("track");
    }

    [Fact]
    public async Task OnMessageCreate_Ignores_Messages_Without_Prefix()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();
        var module = new TrackingModule();

        ext.RegisterModule(client, module);

        await dispatcher.DispatchAsync("MESSAGE_CREATE", BuildMessageEvent("hello world"));

        module.LastContext.Should().BeNull();
    }

    [Fact]
    public async Task OnMessageCreate_Ignores_Bot_Messages()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();
        var module = new TrackingModule();

        ext.RegisterModule(client, module);

        var botEvent = BuildMessageEvent("!track");
        botEvent.Author = new User { Id = 99UL, Username = "AnotherBot", Bot = true };

        await dispatcher.DispatchAsync("MESSAGE_CREATE", botEvent);

        module.LastContext.Should().BeNull();
    }

    [Fact]
    public async Task OnMessageCreate_Parses_Arguments_Correctly()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();
        var module = new TrackingModule();

        ext.RegisterModule(client, module);

        await dispatcher.DispatchAsync(
            "MESSAGE_CREATE",
            BuildMessageEvent("!track hello world"));

        module.LastContext!.Arguments.Should().Equal("hello", "world");
        module.LastContext.RawArguments.Should().Be("hello world");
    }

    [Fact]
    public async Task OnMessageCreate_Routes_Via_Alias()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();
        var module = new AliasModule();

        ext.RegisterModule(client, module);

        await dispatcher.DispatchAsync("MESSAGE_CREATE", BuildMessageEvent("!hi"));

        module.Invoked.Should().BeTrue();
    }

    // ── Error handling tests ──────────────────────────────────────────────────

    [Fact]
    public async Task CommandErrored_Is_Invoked_When_Command_Throws()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();

        CommandErrorEventArgs? capturedArgs = null;
        ext.CommandErrored = args =>
        {
            capturedArgs = args;
            return Task.CompletedTask;
        };

        ext.RegisterModule(client, new FaultyModule());

        await dispatcher.DispatchAsync("MESSAGE_CREATE", BuildMessageEvent("!boom"));

        capturedArgs.Should().NotBeNull();
        capturedArgs!.Exception.Should().BeOfType<InvalidOperationException>();
        capturedArgs.Context.CommandName.Should().Be("boom");
    }

    [Fact]
    public async Task RequireGuild_Precondition_Blocks_DM_Messages()
    {
        var ext = new CommandsExtension("!");
        var (client, dispatcher) = BuildTestClient();

        CommandErrorEventArgs? errorArgs = null;
        ext.CommandErrored = args =>
        {
            errorArgs = args;
            return Task.CompletedTask;
        };

        ext.RegisterModule(client, new GuildOnlyModule());

        // DM messages have no GuildId
        var dmEvent = new MessageCreateEvent
        {
            Id        = 1UL,
            ChannelId = 50UL,
            GuildId   = null,          // ← no guild
            Content   = "!guildonly",
            Author    = new User { Id = 7UL, Bot = false },
        };

        await dispatcher.DispatchAsync("MESSAGE_CREATE", dmEvent);

        errorArgs.Should().NotBeNull("the RequireGuild precondition should have fired CommandErrored");
        errorArgs!.Exception.Message.Should().Contain("server");
    }

    // ── SlashCommand scanner tests ────────────────────────────────────────────

    [Fact]
    public async Task RegisterSlashModuleAsync_Calls_CreateGlobal_For_Each_SlashCommand()
    {
        var restMock    = new Mock<IDiscordRestClient>();
        var cacheMock   = new Mock<IEntityCache>();
        var dispatcher  = new EventDispatcher();
        var gatewayMock = new Mock<IGatewayClient>();

        gatewayMock.SetupGet(g => g.Events).Returns(dispatcher);
        gatewayMock.Setup(g => g.CurrentState).Returns(GatewayState.Connected);

        restMock.Setup(r => r.CreateMessageAsync(It.IsAny<ulong>(), It.IsAny<CreateMessageRequest>()))
                .ReturnsAsync(new Message { Id = 1UL });
        restMock.Setup(r => r.CreateGlobalApplicationCommandAsync(
                    It.IsAny<ulong>(),
                    It.IsAny<CreateApplicationCommandRequest>()))
                .ReturnsAsync(new ApplicationCommand { Id = 1UL, Name = "greet" });

        var options = new PawSharpOptions { Token = "Bot test.token.value" };
        var client  = new DiscordClient(
            options, cacheMock.Object, NullLogger<DiscordClient>.Instance,
            restMock.Object, gatewayMock.Object);

        var ext = new CommandsExtension("!");
        ext.RegisterModule(client, new SlashModule());

        await ext.RegisterSlashModuleAsync(client, new SlashModule(), applicationId: 12345UL);

        restMock.Verify(
            r => r.CreateGlobalApplicationCommandAsync(
                12345UL,
                It.Is<CreateApplicationCommandRequest>(req =>
                    req.Name == "greet" && req.Description == "Greet someone")),
            Times.Once,
            "RegisterSlashModuleAsync should POST the slash command to the global command endpoint");
    }

    [Fact]
    public async Task RegisterSlashModuleAsync_Wires_Interaction_Handler()
    {
        var restMock    = new Mock<IDiscordRestClient>();
        var cacheMock   = new Mock<IEntityCache>();
        var dispatcher  = new EventDispatcher();
        var gatewayMock = new Mock<IGatewayClient>();

        gatewayMock.SetupGet(g => g.Events).Returns(dispatcher);
        gatewayMock.Setup(g => g.CurrentState).Returns(GatewayState.Connected);

        restMock.Setup(r => r.CreateMessageAsync(It.IsAny<ulong>(), It.IsAny<CreateMessageRequest>()))
                .ReturnsAsync(new Message { Id = 1UL });
        restMock.Setup(r => r.CreateGlobalApplicationCommandAsync(
                    It.IsAny<ulong>(), It.IsAny<CreateApplicationCommandRequest>()))
                .ReturnsAsync(new ApplicationCommand { Id = 1UL, Name = "greet" });
        restMock.Setup(r => r.CreateInteractionResponseAsync(
                    It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<InteractionResponse>()))
                .ReturnsAsync(true);

        var options = new PawSharpOptions { Token = "Bot test.token.value" };
        var client  = new DiscordClient(
            options, cacheMock.Object, NullLogger<DiscordClient>.Instance,
            restMock.Object, gatewayMock.Object);

        var module = new SlashModule();
        var ext    = new CommandsExtension("!");

        await ext.RegisterSlashModuleAsync(client, module, applicationId: 12345UL);

        // Firing an INTERACTION_CREATE should route through client.Interactions → SlashModule handler
        var slashEvent = new InteractionCreateEvent
        {
            Id            = 1UL,
            Token         = "tok",
            Type          = 2, // APPLICATION_COMMAND
            Data          = new PawSharp.Gateway.Events.InteractionData { Name = "greet", Type = 1 },
            ChannelId     = 1UL,
        };

        await dispatcher.DispatchAsync("INTERACTION_CREATE", slashEvent);

        module.SlashInvoked.Should().BeTrue("the slash command handler should have been called");
    }
}

// ── Test command modules ──────────────────────────────────────────────────────

internal class SimpleModule : BaseCommandModule
{
    [Command("ping")]
    [Description("Pings the bot.")]
    public Task PingAsync(CommandContext ctx) => Task.CompletedTask;
}

internal class TrackingModule : BaseCommandModule
{
    public CommandContext? LastContext { get; private set; }

    [Command("track")]
    public Task TrackAsync(CommandContext ctx)
    {
        LastContext = ctx;
        return Task.CompletedTask;
    }
}

internal class AliasModule : BaseCommandModule
{
    public bool Invoked { get; private set; }

    [Command("hello")]
    [Aliases("hi", "hey")]
    public Task HelloAsync(CommandContext ctx)
    {
        Invoked = true;
        return Task.CompletedTask;
    }
}

internal class FaultyModule : BaseCommandModule
{
    [Command("boom")]
    public Task BoomAsync(CommandContext ctx)
        => throw new InvalidOperationException("Intentional test failure");
}

internal class GuildOnlyModule : BaseCommandModule
{
    [Command("guildonly")]
    [RequireGuild]
    public Task GuildOnlyAsync(CommandContext ctx) => Task.CompletedTask;
}

internal class SlashModule : BaseCommandModule
{
    public bool SlashInvoked { get; private set; }

    [SlashCommand("greet", "Greet someone")]
    public Task GreetAsync(
        PawSharp.Gateway.Events.InteractionCreateEvent interaction,
        [SlashOption("name", "The person to greet")] string name = "World")
    {
        SlashInvoked = true;
        return Task.CompletedTask;
    }
}
