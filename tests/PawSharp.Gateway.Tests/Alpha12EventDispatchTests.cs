#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Gateway.Events;
using Xunit;

namespace PawSharp.Gateway.Tests;

/// <summary>
/// Tests for gateway event classes and EventDispatcher routing added in alpha12.
/// Covers: scheduled events, auto-moderation, stage instances, audit log,
/// entitlements, polls, soundboard, subscriptions, invites, webhooks, and bulk delete.
/// </summary>
public class Alpha12EventDispatchTests
{
    private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

    // ─── Scheduled Events ────────────────────────────────────────────────────

    [Fact]
    public void GuildScheduledEventCreateEvent_Deserializes_Correctly()
    {
        var json = """
            {
                "id": "100000000000000001",
                "guild_id": "200000000000000002",
                "name": "Community Night",
                "status": 1,
                "creator": { "id": "300000000000000003", "username": "Alice", "discriminator": "0001" }
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildScheduledEventCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be(100000000000000001UL);
        evt.GuildId.Should().Be(200000000000000002UL);
        evt.Name.Should().Be("Community Night");
        evt.Status.Should().Be(1);
        evt.Creator.Should().NotBeNull();
        evt.Creator!.Username.Should().Be("Alice");
    }

    [Fact]
    public void GuildScheduledEventUpdateEvent_Deserializes_Status()
    {
        var json = """
            {
                "id": "100000000000000001",
                "guild_id": "200000000000000002",
                "name": "Community Night",
                "status": 2
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildScheduledEventUpdateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Status.Should().Be(2);
    }

    [Fact]
    public void GuildScheduledEventDeleteEvent_Deserializes_IdsAndName()
    {
        var json = """
            {
                "id": "111111111111111111",
                "guild_id": "222222222222222222",
                "name": "Cancelled Event"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildScheduledEventDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Name.Should().Be("Cancelled Event");
    }

    [Fact]
    public void GuildScheduledEventUserAddEvent_Deserializes_UserAndEventIds()
    {
        var json = """
            {
                "guild_scheduled_event_id": "100000000000000001",
                "user_id": "400000000000000004",
                "guild_id": "200000000000000002"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildScheduledEventUserAddEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.GuildScheduledEventId.Should().Be(100000000000000001UL);
        evt.UserId.Should().Be(400000000000000004UL);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    [Fact]
    public void GuildScheduledEventUserRemoveEvent_Deserializes_UserAndEventIds()
    {
        var json = """
            {
                "guild_scheduled_event_id": "100000000000000001",
                "user_id": "400000000000000004",
                "guild_id": "200000000000000002"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildScheduledEventUserRemoveEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.UserId.Should().Be(400000000000000004UL);
    }

    // ─── Auto-Moderation ─────────────────────────────────────────────────────

    [Fact]
    public void AutoModerationRuleCreateEvent_Deserializes_NameAndTrigger()
    {
        var json = """
            {
                "id": "500000000000000005",
                "guild_id": "200000000000000002",
                "name": "Block Spam",
                "trigger_type": 3,
                "enabled": true
            }
            """;

        var evt = JsonSerializer.Deserialize<AutoModerationRuleCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Name.Should().Be("Block Spam");
        evt.TriggerType.Should().Be(3);
        evt.Enabled.Should().BeTrue();
    }

    [Fact]
    public void AutoModerationRuleUpdateEvent_Deserializes_EnabledState()
    {
        var json = """
            {
                "id": "500000000000000005",
                "guild_id": "200000000000000002",
                "name": "Block Spam",
                "trigger_type": 3,
                "enabled": false
            }
            """;

        var evt = JsonSerializer.Deserialize<AutoModerationRuleUpdateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Enabled.Should().BeFalse();
    }

    [Fact]
    public void AutoModerationRuleDeleteEvent_Deserializes_GuildAndId()
    {
        var json = """
            {
                "id": "500000000000000005",
                "guild_id": "200000000000000002",
                "name": "Old Rule"
            }
            """;

        var evt = JsonSerializer.Deserialize<AutoModerationRuleDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be(500000000000000005UL);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    [Fact]
    public void AutoModerationActionExecutionEvent_Deserializes_RequiredFields()
    {
        var json = """
            {
                "guild_id": "200000000000000002",
                "action": { "type": 1, "metadata": null },
                "rule_id": "500000000000000005",
                "rule_trigger_type": 1,
                "user_id": "400000000000000004",
                "channel_id": "600000000000000006",
                "message_id": "700000000000000007",
                "content": "bad word here",
                "matched_keyword": "bad",
                "matched_content": "bad word here"
            }
            """;

        var evt = JsonSerializer.Deserialize<AutoModerationActionExecutionEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(200000000000000002UL);
        evt.RuleId.Should().Be(500000000000000005UL);
        evt.UserId.Should().Be(400000000000000004UL);
        evt.MatchedKeyword.Should().Be("bad");
        evt.Content.Should().Be("bad word here");
        evt.Action.Should().NotBeNull();
        evt.Action.Type.Should().Be(1);
    }

    // ─── Stage Instances ─────────────────────────────────────────────────────

    [Fact]
    public void StageInstanceCreateEvent_Deserializes_TopicAndPrivacy()
    {
        var json = """
            {
                "id": "800000000000000008",
                "guild_id": "200000000000000002",
                "channel_id": "600000000000000006",
                "topic": "Dev Talk",
                "privacy_level": 2,
                "discoverable_disabled": false
            }
            """;

        var evt = JsonSerializer.Deserialize<StageInstanceCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Topic.Should().Be("Dev Talk");
        evt.PrivacyLevel.Should().Be(2);
        evt.DiscoverableDisabled.Should().BeFalse();
        evt.GuildScheduledEventId.Should().BeNull();
    }

    [Fact]
    public void StageInstanceUpdateEvent_Deserializes_UpdatedTopic()
    {
        var json = """
            {
                "id": "800000000000000008",
                "guild_id": "200000000000000002",
                "channel_id": "600000000000000006",
                "topic": "Updated Topic",
                "privacy_level": 2
            }
            """;

        var evt = JsonSerializer.Deserialize<StageInstanceUpdateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Topic.Should().Be("Updated Topic");
    }

    [Fact]
    public void StageInstanceDeleteEvent_Deserializes_ChannelAndGuildId()
    {
        var json = """
            {
                "id": "800000000000000008",
                "guild_id": "200000000000000002",
                "channel_id": "600000000000000006"
            }
            """;

        var evt = JsonSerializer.Deserialize<StageInstanceDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.ChannelId.Should().Be(600000000000000006UL);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    // ─── Audit Log ───────────────────────────────────────────────────────────

    [Fact]
    public void GuildAuditLogEntryCreateEvent_Deserializes_ActionTypeAndReason()
    {
        var json = """
            {
                "id": "900000000000000009",
                "guild_id": "200000000000000002",
                "action_type": 20,
                "user_id": "400000000000000004",
                "target_id": "111000000000000000",
                "reason": "Rule violation"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildAuditLogEntryCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.ActionType.Should().Be(20);
        evt.UserId.Should().Be(400000000000000004UL);
        evt.Reason.Should().Be("Rule violation");
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    // ─── Entitlements ────────────────────────────────────────────────────────

    [Fact]
    public void EntitlementCreateEvent_Deserializes_SkuAndUserId()
    {
        var json = """
            {
                "id": "123000000000000001",
                "sku_id": "456000000000000002",
                "application_id": "789000000000000003",
                "user_id": "400000000000000004",
                "type": 8,
                "deleted": false,
                "starts_at": "2026-02-01T00:00:00+00:00",
                "ends_at": "2027-02-01T00:00:00+00:00"
            }
            """;

        var evt = JsonSerializer.Deserialize<EntitlementCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.SkuId.Should().Be(456000000000000002UL);
        evt.UserId.Should().Be(400000000000000004UL);
        evt.Type.Should().Be(8);
        evt.StartsAt.Should().NotBeNull();
    }

    [Fact]
    public void EntitlementDeleteEvent_Deserializes_MinimalFields()
    {
        var json = """
            {
                "id": "123000000000000001",
                "sku_id": "456000000000000002",
                "application_id": "789000000000000003",
                "user_id": "400000000000000004"
            }
            """;

        var evt = JsonSerializer.Deserialize<EntitlementDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be(123000000000000001UL);
        evt.UserId.Should().Be(400000000000000004UL);
    }

    // ─── Polls ───────────────────────────────────────────────────────────────

    [Fact]
    public void MessagePollVoteAddEvent_Deserializes_AnswerIdAndChannelId()
    {
        var json = """
            {
                "user_id": "400000000000000004",
                "channel_id": "600000000000000006",
                "message_id": "700000000000000007",
                "guild_id": "200000000000000002",
                "answer_id": 2
            }
            """;

        var evt = JsonSerializer.Deserialize<MessagePollVoteAddEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.UserId.Should().Be(400000000000000004UL);
        evt.AnswerId.Should().Be(2);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    [Fact]
    public void MessagePollVoteRemoveEvent_Deserializes_DmContext()
    {
        var json = """
            {
                "user_id": "400000000000000004",
                "channel_id": "600000000000000006",
                "message_id": "700000000000000007",
                "answer_id": 1
            }
            """;

        var evt = JsonSerializer.Deserialize<MessagePollVoteRemoveEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.AnswerId.Should().Be(1);
        evt.GuildId.Should().BeNull();
    }

    // ─── Soundboard ──────────────────────────────────────────────────────────

    [Fact]
    public void GuildSoundboardSoundCreateEvent_Deserializes_NameAndVolume()
    {
        var json = """
            {
                "sound_id": "111222333444555666",
                "name": "Tada",
                "volume": 1.0,
                "emoji_id": null,
                "emoji_name": "🎉",
                "guild_id": "200000000000000002",
                "available": true
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildSoundboardSoundCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Name.Should().Be("Tada");
        evt.Volume.Should().Be(1.0);
        evt.EmojiName.Should().Be("🎉");
        evt.Available.Should().BeTrue();
    }

    [Fact]
    public void GuildSoundboardSoundDeleteEvent_Deserializes_SoundAndGuildId()
    {
        var json = """
            {
                "sound_id": "111222333444555666",
                "guild_id": "200000000000000002"
            }
            """;

        var evt = JsonSerializer.Deserialize<GuildSoundboardSoundDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.SoundId.Should().Be(111222333444555666UL);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    // ─── Subscriptions ───────────────────────────────────────────────────────

    [Fact]
    public void SubscriptionCreateEvent_Deserializes_UserAndSkuIds()
    {
        var json = """
            {
                "id": "999000000000000001",
                "user_id": "400000000000000004",
                "sku_ids": ["456000000000000002"],
                "status": 0,
                "current_period_start": "2026-02-01T00:00:00+00:00",
                "current_period_end": "2026-03-01T00:00:00+00:00"
            }
            """;

        var evt = JsonSerializer.Deserialize<SubscriptionCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.UserId.Should().Be(400000000000000004UL);
        evt.SkuIds.Should().ContainSingle().Which.Should().Be(456000000000000002UL);
        evt.Status.Should().Be(0);
    }

    [Fact]
    public void SubscriptionDeleteEvent_Deserializes_IdAndUserId()
    {
        var json = """
            {
                "id": "999000000000000001",
                "user_id": "400000000000000004"
            }
            """;

        var evt = JsonSerializer.Deserialize<SubscriptionDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be(999000000000000001UL);
        evt.UserId.Should().Be(400000000000000004UL);
    }

    // ─── Invites ─────────────────────────────────────────────────────────────

    [Fact]
    public void InviteCreateEvent_Deserializes_CodeAndMaxAge()
    {
        var json = """
            {
                "channel_id": "600000000000000006",
                "code": "xKzA2B",
                "created_at": "2026-02-20T12:00:00+00:00",
                "guild_id": "200000000000000002",
                "max_age": 86400,
                "max_uses": 10,
                "temporary": false,
                "uses": 0
            }
            """;

        var evt = JsonSerializer.Deserialize<InviteCreateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Code.Should().Be("xKzA2B");
        evt.MaxAge.Should().Be(86400);
        evt.MaxUses.Should().Be(10);
        evt.Temporary.Should().BeFalse();
    }

    [Fact]
    public void InviteDeleteEvent_Deserializes_CodeAndChannelId()
    {
        var json = """
            {
                "channel_id": "600000000000000006",
                "guild_id": "200000000000000002",
                "code": "xKzA2B"
            }
            """;

        var evt = JsonSerializer.Deserialize<InviteDeleteEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Code.Should().Be("xKzA2B");
        evt.ChannelId.Should().Be(600000000000000006UL);
    }

    // ─── Webhooks & Bulk Delete ───────────────────────────────────────────────

    [Fact]
    public void WebhooksUpdateEvent_Deserializes_GuildAndChannelId()
    {
        var json = """
            {
                "guild_id": "200000000000000002",
                "channel_id": "600000000000000006"
            }
            """;

        var evt = JsonSerializer.Deserialize<WebhooksUpdateEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.GuildId.Should().Be(200000000000000002UL);
        evt.ChannelId.Should().Be(600000000000000006UL);
    }

    [Fact]
    public void MessageDeleteBulkEvent_Deserializes_IdsAndChannelId()
    {
        var json = """
            {
                "ids": ["700000000000000007", "700000000000000008", "700000000000000009"],
                "channel_id": "600000000000000006",
                "guild_id": "200000000000000002"
            }
            """;

        var evt = JsonSerializer.Deserialize<MessageDeleteBulkEvent>(json, _opts);

        evt.Should().NotBeNull();
        evt!.Ids.Should().HaveCount(3);
        evt.ChannelId.Should().Be(600000000000000006UL);
        evt.GuildId.Should().Be(200000000000000002UL);
    }

    // ─── EventDispatcher routing ──────────────────────────────────────────────

    [Fact]
    public async Task EventDispatcher_Routes_MessagePollVoteAdd_To_Handler()
    {
        var dispatcher = new EventDispatcher();
        MessagePollVoteAddEvent? captured = null;

        dispatcher.On<MessagePollVoteAddEvent>("MESSAGE_POLL_VOTE_ADD", e => captured = e);

        var json = """
            {
                "user_id": "400000000000000004",
                "channel_id": "600000000000000006",
                "message_id": "700000000000000007",
                "answer_id": 3
            }
            """;

        await dispatcher.DispatchFromJsonAsync<MessagePollVoteAddEvent>("MESSAGE_POLL_VOTE_ADD", json);

        captured.Should().NotBeNull();
        captured!.AnswerId.Should().Be(3);
    }

    [Fact]
    public async Task EventDispatcher_Routes_GuildScheduledEventCreate_To_Handler()
    {
        var dispatcher = new EventDispatcher();
        GuildScheduledEventCreateEvent? captured = null;

        dispatcher.On<GuildScheduledEventCreateEvent>("GUILD_SCHEDULED_EVENT_CREATE", e => captured = e);

        var json = """
            {
                "id": "100000000000000001",
                "guild_id": "200000000000000002",
                "name": "Movie Night",
                "status": 1
            }
            """;

        await dispatcher.DispatchFromJsonAsync<GuildScheduledEventCreateEvent>("GUILD_SCHEDULED_EVENT_CREATE", json);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Movie Night");
    }

    [Fact]
    public async Task EventDispatcher_Routes_InviteCreate_To_Handler()
    {
        var dispatcher = new EventDispatcher();
        InviteCreateEvent? captured = null;

        dispatcher.On<InviteCreateEvent>("INVITE_CREATE", e => captured = e);

        var json = """
            {
                "channel_id": "600000000000000006",
                "code": "abc123",
                "created_at": "2026-02-20T00:00:00+00:00",
                "max_age": 0,
                "max_uses": 0,
                "temporary": false,
                "uses": 0
            }
            """;

        await dispatcher.DispatchFromJsonAsync<InviteCreateEvent>("INVITE_CREATE", json);

        captured.Should().NotBeNull();
        captured!.Code.Should().Be("abc123");
    }
}
