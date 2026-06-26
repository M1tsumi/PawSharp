using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Client;
using PawSharp.Commands;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;

namespace PawSharp.Commands.Tests.PreconditionTests;

internal static class TestContextBuilder
{
    internal static CommandContext CreateContext(ulong? guildId = null, ulong userId = 1, ulong channelId = 1)
    {
        var options = new PawSharpOptions { Token = "Bot test.token" };
        var restMock = new Mock<IDiscordRestClient>();
        var cacheMock = new Mock<IEntityCache>();
        var gatewayMock = new Mock<IGatewayClient>();
        gatewayMock.SetupGet(g => g.Events).Returns(new EventDispatcher());
        var client = new DiscordClient(options, cacheMock.Object, NullLogger<DiscordClient>.Instance, restMock.Object, gatewayMock.Object);

        var member = guildId.HasValue ? new GuildMember { User = new User { Id = userId } } : null;
        return new CommandContext(
            client,
            new Message { Id = 1, ChannelId = channelId, GuildId = guildId, Author = new User { Id = userId } },
            "!", "test", System.Array.Empty<string>(), "",
            member);
    }
}
