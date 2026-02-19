#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

/// <summary>
/// Tests for gateway event class deserialization and EventDispatcher routing
/// added during the alpha11 release.
/// </summary>
public class GatewayEventDispatchTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────────────────────────────
    //  GuildRoleCreateEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void GuildRoleCreateEvent_Deserializes_GuildId_And_Role()
    {
        var json = """
            {
                "guild_id": "123456789012345678",
                "role": {
                    "id": "987654321098765432",
                    "name": "Moderator",
                    "color": 255,
                    "hoist": true,
                    "position": 3,
                    "permissions": "8",
                    "managed": false,
                    "mentionable": true
                }
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildRoleCreateEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(123456789012345678UL);
        evt.Role.Should().NotBeNull();
        evt.Role!.Name.Should().Be("Moderator");
    }

    [Fact]
    public void GuildRoleUpdateEvent_Deserializes_GuildId_And_Role()
    {
        var json = """
            {
                "guild_id": "111111111111111111",
                "role": {
                    "id": "222222222222222222",
                    "name": "Admin",
                    "color": 16711680,
                    "hoist": false,
                    "position": 1,
                    "permissions": "2147483647",
                    "managed": false,
                    "mentionable": false
                }
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildRoleUpdateEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(111111111111111111UL);
        evt.Role!.Name.Should().Be("Admin");
    }

    [Fact]
    public void GuildRoleDeleteEvent_Deserializes_GuildId_And_RoleId()
    {
        var json = """
            {
                "guild_id": "333333333333333333",
                "role_id": "444444444444444444"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildRoleDeleteEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(333333333333333333UL);
        evt.RoleId.Should().Be(444444444444444444UL);
    }

    // ─────────────────────────────────────────────
    //  GuildMembersChunkEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void GuildMembersChunkEvent_Deserializes_Members_And_Chunk_Info()
    {
        var json = """
            {
                "guild_id": "555555555555555555",
                "members": [
                    { "user": { "id": "111", "username": "Alice", "discriminator": "0001" }, "roles": [] },
                    { "user": { "id": "222", "username": "Bob",   "discriminator": "0002" }, "roles": [] }
                ],
                "chunk_index": 0,
                "chunk_count": 1,
                "not_found": [],
                "nonce": "abc123"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildMembersChunkEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(555555555555555555UL);
        evt.Members.Should().HaveCount(2);
        evt.ChunkIndex.Should().Be(0);
        evt.ChunkCount.Should().Be(1);
        evt.Nonce.Should().Be("abc123");
    }

    // ─────────────────────────────────────────────
    //  GuildStickersUpdateEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void GuildStickersUpdateEvent_Deserializes_GuildId_And_Stickers()
    {
        var json = """
            {
                "guild_id": "666666666666666666",
                "stickers": [
                    { "id": "777777777777777777", "name": "wave", "tags": "wave", "type": 1, "format_type": 1 }
                ]
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildStickersUpdateEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(666666666666666666UL);
        evt.Stickers.Should().HaveCount(1);
        evt.Stickers[0].Name.Should().Be("wave");
    }

    // ─────────────────────────────────────────────
    //  UserUpdateEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void UserUpdateEvent_Deserializes_All_Fields()
    {
        var json = """
            {
                "id": "888888888888888888",
                "username": "BotUser",
                "discriminator": "0000",
                "avatar": "abc123hash",
                "bot": true,
                "email": null,
                "verified": true
            }
            """;

        var evt = JsonSerializer.Deserialize<UserUpdateEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be(888888888888888888UL);
        evt.Username.Should().Be("BotUser");
        evt.Discriminator.Should().Be("0000");
        evt.Avatar.Should().Be("abc123hash");
        evt.Bot.Should().BeTrue();
        evt.Verified.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  GuildIntegrationsUpdateEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void GuildIntegrationsUpdateEvent_Deserializes_GuildId()
    {
        var json = """{ "guild_id": "999999999999999999" }""";

        var evt = JsonSerializer.Deserialize<GuildIntegrationsUpdateEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(999999999999999999UL);
    }

    // ─────────────────────────────────────────────
    //  MessageReactionRemoveEmojiEvent
    // ─────────────────────────────────────────────

    [Fact]
    public void MessageReactionRemoveEmojiEvent_Deserializes_All_Fields()
    {
        var json = """
            {
                "channel_id": "100000000000000001",
                "guild_id": "100000000000000002",
                "message_id": "100000000000000003",
                "emoji": { "id": null, "name": "👋" }
            }
            """;

        var evt = JsonSerializer.Deserialize<MessageReactionRemoveEmojiEvent>(json, _jsonOptions);

        evt.Should().NotBeNull();
        evt!.ChannelId.Should().Be(100000000000000001UL);
        evt.GuildId.Should().Be(100000000000000002UL);
        evt.MessageId.Should().Be(100000000000000003UL);
        evt.Emoji.Should().NotBeNull();
        evt.Emoji.Name.Should().Be("👋");
    }

    // ─────────────────────────────────────────────
    //  EventDispatcher subscription + dispatch
    // ─────────────────────────────────────────────

    [Fact]
    public async System.Threading.Tasks.Task EventDispatcher_Subscribes_And_Dispatches_Event()
    {
        var dispatcher = new EventDispatcher();
        GuildRoleCreateEvent? received = null;

        dispatcher.On<GuildRoleCreateEvent>("GUILD_ROLE_CREATE", evt =>
        {
            received = evt;
        });

        var testEvent = new GuildRoleCreateEvent
        {
            GuildId = 12345UL,
            Role = new PawSharp.Core.Entities.Role { Id = 99999UL }
        };

        await dispatcher.DispatchAsync("GUILD_ROLE_CREATE", testEvent);

        received.Should().NotBeNull();
        received!.GuildId.Should().Be(12345UL);
        received.Role!.Id.Should().Be(99999UL);
    }

    [Fact]
    public async System.Threading.Tasks.Task EventDispatcher_Does_Not_Fire_Unsubscribed_Event()
    {
        var dispatcher = new EventDispatcher();
        var fired = false;

        dispatcher.On<UserUpdateEvent>("USER_UPDATE", _ =>
        {
            fired = true;
        });

        // Dispatch a different event type — subscriber should NOT fire
        await dispatcher.DispatchAsync("GUILD_INTEGRATIONS_UPDATE", new GuildIntegrationsUpdateEvent { GuildId = 1UL });

        fired.Should().BeFalse();
    }

    [Fact]
    public async System.Threading.Tasks.Task EventDispatcher_DispatchFromJsonAsync_Deserializes_And_Routes()
    {
        var dispatcher = new EventDispatcher();
        UserUpdateEvent? received = null;

        dispatcher.On<UserUpdateEvent>("USER_UPDATE", evt => { received = evt; });

        var json = """{"id":"123456789012345678","username":"TestBot","discriminator":"0000"}""";
        await dispatcher.DispatchFromJsonAsync<UserUpdateEvent>("USER_UPDATE", json);

        received.Should().NotBeNull();
        received!.Username.Should().Be("TestBot");
    }
}
