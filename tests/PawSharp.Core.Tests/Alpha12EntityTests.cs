#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using PawSharp.Core.Entities;
using Xunit;

namespace PawSharp.Core.Tests;

/// <summary>
/// JSON deserialization tests for the new entity types introduced in alpha12:
/// Poll, GuildOnboarding, and ApplicationRoleConnectionMetadata.
/// </summary>
public class Alpha12EntityTests
{
    // ─────────────────────────────────────────────
    //  Shared JsonSerializerOptions
    // ─────────────────────────────────────────────

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────────────────────────────
    //  Poll
    // ─────────────────────────────────────────────

    [Fact]
    public void Poll_Deserializes_BasicFields()
    {
        const string json = """
            {
              "question":         { "text": "Favourite runtime?" },
              "answers":          [],
              "allow_multiselect": false,
              "layout_type":      1
            }
            """;

        var poll = JsonSerializer.Deserialize<Poll>(json, _options);

        poll.Should().NotBeNull();
        poll!.Question.Text.Should().Be("Favourite runtime?");
        poll.AllowMultiselect.Should().BeFalse();
        poll.LayoutType.Should().Be(PollLayoutType.Default);
        poll.Expiry.Should().BeNull();
        poll.Results.Should().BeNull();
    }

    [Fact]
    public void Poll_Deserializes_With_Answers()
    {
        const string json = """
            {
              "question": { "text": "Best language?" },
              "answers": [
                { "answer_id": 1, "poll_media": { "text": "C#" } },
                { "answer_id": 2, "poll_media": { "text": "F#" } }
              ],
              "allow_multiselect": true,
              "layout_type": 1
            }
            """;

        var poll = JsonSerializer.Deserialize<Poll>(json, _options);

        poll!.Answers.Should().HaveCount(2);
        poll.Answers[0].AnswerId.Should().Be(1);
        poll.Answers[0].PollMedia.Text.Should().Be("C#");
        poll.Answers[1].AnswerId.Should().Be(2);
        poll.Answers[1].PollMedia.Text.Should().Be("F#");
        poll.AllowMultiselect.Should().BeTrue();
    }

    [Fact]
    public void Poll_Deserializes_With_Expiry()
    {
        const string json = """
            {
              "question": { "text": "Will it blend?" },
              "answers":  [],
              "allow_multiselect": false,
              "layout_type": 1,
              "expiry": "2099-01-01T00:00:00+00:00"
            }
            """;

        var poll = JsonSerializer.Deserialize<Poll>(json, _options);

        poll!.Expiry.Should().NotBeNull();
        poll.Expiry!.Value.Year.Should().Be(2099);
    }

    [Fact]
    public void Poll_Deserializes_With_FinalizedResults()
    {
        const string json = """
            {
              "question": { "text": "Pick one" },
              "answers":  [],
              "allow_multiselect": false,
              "layout_type": 1,
              "results": {
                "is_finalized": true,
                "answer_counts": [
                  { "id": 1, "count": 42, "me_voted": false },
                  { "id": 2, "count": 7,  "me_voted": true  }
                ]
              }
            }
            """;

        var poll = JsonSerializer.Deserialize<Poll>(json, _options);

        poll!.Results.Should().NotBeNull();
        poll.Results!.IsFinalized.Should().BeTrue();
        poll.Results.AnswerCounts.Should().HaveCount(2);
        poll.Results.AnswerCounts[0].Count.Should().Be(42);
        poll.Results.AnswerCounts[1].MeVoted.Should().BeTrue();
    }

    [Fact]
    public void PollLayoutType_Default_HasValue_One()
    {
        ((int)PollLayoutType.Default).Should().Be(1);
    }

    // ─────────────────────────────────────────────
    //  GuildOnboarding
    // ─────────────────────────────────────────────

    [Fact]
    public void GuildOnboarding_Deserializes_BasicFields()
    {
        const string json = """
            {
              "guild_id":           "200000000000000000",
              "prompts":            [],
              "default_channel_ids": ["600000000000000001", "600000000000000002"],
              "enabled":            true,
              "mode":               0
            }
            """;

        var onboarding = JsonSerializer.Deserialize<GuildOnboarding>(json, _options);

        onboarding.Should().NotBeNull();
        onboarding!.GuildId.Should().Be(200000000000000000UL);
        onboarding.Enabled.Should().BeTrue();
        onboarding.Mode.Should().Be(OnboardingMode.OnboardingDefault);
        onboarding.DefaultChannelIds.Should().HaveCount(2);
    }

    [Fact]
    public void GuildOnboarding_Deserializes_With_Prompts()
    {
        const string json = """
            {
              "guild_id": "200000000000000000",
              "prompts": [
                {
                  "id": "300000000000000001",
                  "type": 0,
                  "options": [
                    {
                      "id": "400000000000000001",
                      "channel_ids": [],
                      "role_ids":    [],
                      "title":       "Gaming",
                      "description": "Join gaming channels"
                    }
                  ],
                  "title":         "What do you like?",
                  "single_select": true,
                  "required":      true,
                  "in_onboarding": true
                }
              ],
              "default_channel_ids": [],
              "enabled": true,
              "mode":    1
            }
            """;

        var onboarding = JsonSerializer.Deserialize<GuildOnboarding>(json, _options);

        onboarding!.Prompts.Should().HaveCount(1);
        var prompt = onboarding.Prompts[0];
        prompt.Id.Should().Be(300000000000000001UL);
        prompt.Type.Should().Be(OnboardingPromptType.MultipleChoice);
        prompt.Title.Should().Be("What do you like?");
        prompt.SingleSelect.Should().BeTrue();
        prompt.Required.Should().BeTrue();
        prompt.InOnboarding.Should().BeTrue();
        prompt.Options.Should().HaveCount(1);
        prompt.Options[0].Title.Should().Be("Gaming");
        prompt.Options[0].Description.Should().Be("Join gaming channels");
        onboarding.Mode.Should().Be(OnboardingMode.OnboardingAdvanced);
    }

    [Fact]
    public void OnboardingMode_Values_AreCorrect()
    {
        ((int)OnboardingMode.OnboardingDefault).Should().Be(0);
        ((int)OnboardingMode.OnboardingAdvanced).Should().Be(1);
    }

    [Fact]
    public void OnboardingPromptType_Values_AreCorrect()
    {
        ((int)OnboardingPromptType.MultipleChoice).Should().Be(0);
        ((int)OnboardingPromptType.Dropdown).Should().Be(1);
    }

    // ─────────────────────────────────────────────
    //  ApplicationRoleConnectionMetadata
    // ─────────────────────────────────────────────

    [Fact]
    public void ApplicationRoleConnectionMetadata_Deserializes_BasicFields()
    {
        const string json = """
            {
              "type":        2,
              "key":         "total_games",
              "name":        "Total Games Played",
              "description": "The number of games played"
            }
            """;

        var record = JsonSerializer.Deserialize<ApplicationRoleConnectionMetadata>(json, _options);

        record.Should().NotBeNull();
        record!.Type.Should().Be(ApplicationRoleConnectionMetadataType.IntegerGreaterThanOrEqual);
        record.Key.Should().Be("total_games");
        record.Name.Should().Be("Total Games Played");
        record.Description.Should().Be("The number of games played");
    }

    [Fact]
    public void ApplicationRoleConnectionMetadata_Deserializes_With_Localizations()
    {
        const string json = """
            {
              "type":        2,
              "key":         "games",
              "name":        "Games",
              "description": "Games played",
              "name_localizations": {
                "de": "Spiele",
                "fr": "Jeux"
              },
              "description_localizations": {
                "de": "Gespielte Spiele",
                "fr": "Jeux joués"
              }
            }
            """;

        var record = JsonSerializer.Deserialize<ApplicationRoleConnectionMetadata>(json, _options);

        record!.NameLocalizations.Should().NotBeNull();
        record.NameLocalizations.Should().ContainKey("de").WhoseValue.Should().Be("Spiele");
        record.NameLocalizations.Should().ContainKey("fr").WhoseValue.Should().Be("Jeux");
        record.DescriptionLocalizations.Should().ContainKey("de");
    }

    [Fact]
    public void ApplicationRoleConnectionMetadataType_AllValues_Defined()
    {
        // Validates the full enum surface matches Discord API spec
        ((int)ApplicationRoleConnectionMetadataType.IntegerLessThanOrEqual).Should().Be(1);
        ((int)ApplicationRoleConnectionMetadataType.IntegerGreaterThanOrEqual).Should().Be(2);
        ((int)ApplicationRoleConnectionMetadataType.IntegerEqual).Should().Be(3);
        ((int)ApplicationRoleConnectionMetadataType.IntegerNotEqual).Should().Be(4);
        ((int)ApplicationRoleConnectionMetadataType.DatetimeLessThanOrEqual).Should().Be(5);
        ((int)ApplicationRoleConnectionMetadataType.DatetimeGreaterThanOrEqual).Should().Be(6);
        ((int)ApplicationRoleConnectionMetadataType.BooleanEqual).Should().Be(7);
        ((int)ApplicationRoleConnectionMetadataType.BooleanNotEqual).Should().Be(8);
    }

    // ─────────────────────────────────────────────
    //  Cross-entity: Message.Poll property
    // ─────────────────────────────────────────────

    [Fact]
    public void Message_Deserializes_WithPoll_Populated()
    {
        const string json = """
            {
              "id":          "123456789012345678",
              "channel_id":  "987654321098765432",
              "content":     "Check out this poll!",
              "timestamp":   "2024-01-01T00:00:00+00:00",
              "edited_timestamp": null,
              "tts":         false,
              "mention_everyone": false,
              "mentions":    [],
              "mention_roles": [],
              "attachments": [],
              "embeds":      [],
              "pinned":      false,
              "type":        0,
              "poll": {
                "question":          { "text": "Inline poll question" },
                "answers":           [],
                "allow_multiselect": false,
                "layout_type":       1
              }
            }
            """;

        var message = JsonSerializer.Deserialize<Message>(json, _options);

        message.Should().NotBeNull();
        message!.Poll.Should().NotBeNull();
        message.Poll!.Question.Text.Should().Be("Inline poll question");
    }

    [Fact]
    public void Message_Deserializes_WithPoll_Null_When_Absent()
    {
        const string json = """
            {
              "id":          "123456789012345678",
              "channel_id":  "987654321098765432",
              "content":     "No poll here",
              "timestamp":   "2024-01-01T00:00:00+00:00",
              "edited_timestamp": null,
              "tts":         false,
              "mention_everyone": false,
              "mentions":    [],
              "mention_roles": [],
              "attachments": [],
              "embeds":      [],
              "pinned":      false,
              "type":        0
            }
            """;

        var message = JsonSerializer.Deserialize<Message>(json, _options);

        message.Should().NotBeNull();
        message!.Poll.Should().BeNull();
    }
}
