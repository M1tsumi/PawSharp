#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using Xunit;

namespace PawSharp.API.Tests;

/// <summary>
/// Tests for the REST endpoints introduced in alpha12.
/// Uses mocked <see cref="IDiscordRestClient"/> to verify interface contracts
/// for polls, monetization, soundboard, onboarding, role connections, and member helpers.
/// </summary>
public class Alpha12EndpointsTests
{
    private readonly Mock<IDiscordRestClient> _mock = new();

    // ─────────────────────────────────────────────
    //  Polls
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetAnswerVotersAsync_Returns_UserList()
    {
        var users = new List<User>
        {
            new() { Id = 1UL, Username = "Alice" },
            new() { Id = 2UL, Username = "Bob" }
        };
        _mock.Setup(r => r.GetAnswerVotersAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<int>(),
                It.IsAny<int?>(), It.IsAny<ulong?>()))
             .ReturnsAsync(users);

        var result = await _mock.Object.GetAnswerVotersAsync(
            channelId: 100UL, messageId: 200UL, answerId: 1);

        result.Should().HaveCount(2);
        result![0].Username.Should().Be("Alice");
    }

    [Fact]
    public async Task GetAnswerVotersAsync_Returns_Null_When_NotFound()
    {
        _mock.Setup(r => r.GetAnswerVotersAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<int>(),
                It.IsAny<int?>(), It.IsAny<ulong?>()))
             .ReturnsAsync((List<User>?)null);

        var result = await _mock.Object.GetAnswerVotersAsync(0UL, 0UL, 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EndPollAsync_Returns_Updated_Message()
    {
        var message = new Message { Id = 200UL, Content = string.Empty };
        _mock.Setup(r => r.EndPollAsync(100UL, 200UL)).ReturnsAsync(message);

        var result = await _mock.Object.EndPollAsync(100UL, 200UL);

        result.Should().NotBeNull();
        result!.Id.Should().Be(200UL);
    }

    [Fact]
    public async Task CreateMessageRequest_Carries_Poll_Field()
    {
        var request = new CreateMessageRequest
        {
            Content = "Vote now!",
            Poll = new CreatePollRequest
            {
                Question = new PollMediaRequest { Text = "Best language?" },
                Answers = new List<PollAnswerRequest>
                {
                    new() { PollMedia = new PollMediaRequest { Text = "C#" } },
                    new() { PollMedia = new PollMediaRequest { Text = "F#" } }
                },
                Duration = 24,
                AllowMultiselect = false
            }
        };

        request.Poll.Should().NotBeNull();
        request.Poll!.Question.Text.Should().Be("Best language?");
        request.Poll.Answers.Should().HaveCount(2);
        request.Poll.Duration.Should().Be(24);
    }

    // ─────────────────────────────────────────────
    //  SKUs
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ListSkusAsync_Returns_SkuList()
    {
        var skus = new List<Sku>
        {
            new() { Id = 10UL, Name = "Premium Monthly", Type = SkuType.Subscription },
            new() { Id = 11UL, Name = "Premium Annual", Type = SkuType.Subscription }
        };
        _mock.Setup(r => r.ListSkusAsync(It.IsAny<ulong>())).ReturnsAsync(skus);

        var result = await _mock.Object.ListSkusAsync(999UL);

        result.Should().HaveCount(2);
        result![0].Name.Should().Be("Premium Monthly");
        result[0].Type.Should().Be(SkuType.Subscription);
    }

    // ─────────────────────────────────────────────
    //  Entitlements
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ListEntitlementsAsync_Returns_EntitlementList()
    {
        var entitlements = new List<Entitlement>
        {
            new() { Id = 50UL, SkuId = 10UL, ApplicationId = 999UL, Type = EntitlementType.ApplicationSubscription }
        };
        _mock.Setup(r => r.ListEntitlementsAsync(
                It.IsAny<ulong>(), It.IsAny<ulong?>(), It.IsAny<List<ulong>?>(),
                It.IsAny<ulong?>(), It.IsAny<ulong?>(), It.IsAny<int?>(),
                It.IsAny<ulong?>(), It.IsAny<bool?>()))
             .ReturnsAsync(entitlements);

        var result = await _mock.Object.ListEntitlementsAsync(999UL);

        result.Should().HaveCount(1);
        result![0].Type.Should().Be(EntitlementType.ApplicationSubscription);
    }

    [Fact]
    public async Task GetEntitlementAsync_Returns_Correct_Entitlement()
    {
        var entitlement = new Entitlement
        {
            Id = 50UL,
            SkuId = 10UL,
            ApplicationId = 999UL,
            Type = EntitlementType.ApplicationSubscription
        };
        _mock.Setup(r => r.GetEntitlementAsync(999UL, 50UL)).ReturnsAsync(entitlement);

        var result = await _mock.Object.GetEntitlementAsync(999UL, 50UL);

        result.Should().NotBeNull();
        result!.Id.Should().Be(50UL);
    }

    [Fact]
    public async Task CreateTestEntitlementAsync_Returns_TestEntitlement()
    {
        var entitlement = new Entitlement { Id = 55UL, Type = EntitlementType.TestModePurchase };
        _mock.Setup(r => r.CreateTestEntitlementAsync(
                It.IsAny<ulong>(), It.IsAny<CreateTestEntitlementRequest>()))
             .ReturnsAsync(entitlement);

        var result = await _mock.Object.CreateTestEntitlementAsync(999UL,
            new CreateTestEntitlementRequest { SkuId = 10UL, OwnerId = 777UL, OwnerType = 2 });

        result.Should().NotBeNull();
        result!.Type.Should().Be(EntitlementType.TestModePurchase);
    }

    [Fact]
    public async Task DeleteTestEntitlementAsync_Returns_True_On_Success()
    {
        _mock.Setup(r => r.DeleteTestEntitlementAsync(999UL, 55UL)).ReturnsAsync(true);

        var result = await _mock.Object.DeleteTestEntitlementAsync(999UL, 55UL);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeEntitlementAsync_Returns_True_On_Success()
    {
        _mock.Setup(r => r.ConsumeEntitlementAsync(999UL, 50UL)).ReturnsAsync(true);

        var result = await _mock.Object.ConsumeEntitlementAsync(999UL, 50UL);

        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Subscriptions
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ListSkuSubscriptionsAsync_Returns_SubscriptionList()
    {
        var subs = new List<Subscription>
        {
            new()
            {
                Id = 60UL,
                UserId = 400UL,
                SkuIds = new List<ulong> { 10UL },
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = DateTimeOffset.UtcNow,
                CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1)
            }
        };
        _mock.Setup(r => r.ListSkuSubscriptionsAsync(
                It.IsAny<ulong>(), It.IsAny<ulong?>(), It.IsAny<ulong?>(),
                It.IsAny<int?>(), It.IsAny<ulong?>()))
             .ReturnsAsync(subs);

        var result = await _mock.Object.ListSkuSubscriptionsAsync(10UL);

        result.Should().HaveCount(1);
        result![0].Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetSkuSubscriptionAsync_Returns_Subscription()
    {
        var sub = new Subscription
        {
            Id = 60UL,
            UserId = 400UL,
            Status = SubscriptionStatus.Ending,
            CurrentPeriodStart = DateTimeOffset.UtcNow,
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1)
        };
        _mock.Setup(r => r.GetSkuSubscriptionAsync(10UL, 60UL)).ReturnsAsync(sub);

        var result = await _mock.Object.GetSkuSubscriptionAsync(10UL, 60UL);

        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Ending);
    }

    // ─────────────────────────────────────────────
    //  Soundboard
    // ─────────────────────────────────────────────

    [Fact]
    public async Task ListDefaultSoundboardSoundsAsync_Returns_Sounds()
    {
        var sounds = new List<SoundboardSound>
        {
            new() { Name = "quack", Volume = 1.0, Available = true },
            new() { Name = "bark",  Volume = 0.8, Available = true }
        };
        _mock.Setup(r => r.ListDefaultSoundboardSoundsAsync()).ReturnsAsync(sounds);

        var result = await _mock.Object.ListDefaultSoundboardSoundsAsync();

        result.Should().HaveCount(2);
        result![0].Name.Should().Be("quack");
    }

    [Fact]
    public async Task ListGuildSoundboardSoundsAsync_Returns_GuildSounds()
    {
        var sounds = new List<SoundboardSound>
        {
            new() { Id = 1UL, Name = "tada", Volume = 1.0, GuildId = 200UL, Available = true }
        };
        _mock.Setup(r => r.ListGuildSoundboardSoundsAsync(200UL)).ReturnsAsync(sounds);

        var result = await _mock.Object.ListGuildSoundboardSoundsAsync(200UL);

        result.Should().HaveCount(1);
        result![0].GuildId.Should().Be(200UL);
    }

    [Fact]
    public async Task CreateGuildSoundboardSoundAsync_Returns_Created_Sound()
    {
        var sound = new SoundboardSound { Id = 2UL, Name = "woohoo", Volume = 1.0 };
        _mock.Setup(r => r.CreateGuildSoundboardSoundAsync(
                It.IsAny<ulong>(), It.IsAny<CreateGuildSoundboardSoundRequest>()))
             .ReturnsAsync(sound);

        var result = await _mock.Object.CreateGuildSoundboardSoundAsync(200UL,
            new CreateGuildSoundboardSoundRequest { Name = "woohoo", Sound = "base64data" });

        result.Should().NotBeNull();
        result!.Name.Should().Be("woohoo");
    }

    [Fact]
    public async Task DeleteGuildSoundboardSoundAsync_Returns_True_On_Success()
    {
        _mock.Setup(r => r.DeleteGuildSoundboardSoundAsync(200UL, 2UL)).ReturnsAsync(true);

        var result = await _mock.Object.DeleteGuildSoundboardSoundAsync(200UL, 2UL);

        result.Should().BeTrue();
    }

    // ─────────────────────────────────────────────
    //  Guild Onboarding
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetGuildOnboardingAsync_Returns_Onboarding()
    {
        var onboarding = new GuildOnboarding
        {
            GuildId = 200UL,
            Enabled = true,
            Mode = OnboardingMode.OnboardingDefault,
            DefaultChannelIds = new List<ulong> { 600UL }
        };
        _mock.Setup(r => r.GetGuildOnboardingAsync(200UL)).ReturnsAsync(onboarding);

        var result = await _mock.Object.GetGuildOnboardingAsync(200UL);

        result.Should().NotBeNull();
        result!.Enabled.Should().BeTrue();
        result.DefaultChannelIds.Should().Contain(600UL);
        result.Mode.Should().Be(OnboardingMode.OnboardingDefault);
    }

    [Fact]
    public async Task ModifyGuildOnboardingAsync_Returns_Updated_Onboarding()
    {
        var updated = new GuildOnboarding
        {
            GuildId = 200UL,
            Enabled = false,
            Mode = OnboardingMode.OnboardingAdvanced
        };
        _mock.Setup(r => r.ModifyGuildOnboardingAsync(
                It.IsAny<ulong>(), It.IsAny<ModifyGuildOnboardingRequest>()))
             .ReturnsAsync(updated);

        var result = await _mock.Object.ModifyGuildOnboardingAsync(200UL,
            new ModifyGuildOnboardingRequest { Enabled = false, Mode = (int)OnboardingMode.OnboardingAdvanced });

        result.Should().NotBeNull();
        result!.Enabled.Should().BeFalse();
        result.Mode.Should().Be(OnboardingMode.OnboardingAdvanced);
    }

    // ─────────────────────────────────────────────
    //  Application Role Connection Metadata
    // ─────────────────────────────────────────────

    [Fact]
    public async Task GetApplicationRoleConnectionMetadataAsync_Returns_Records()
    {
        var records = new List<ApplicationRoleConnectionMetadata>
        {
            new()
            {
                Key = "total_games",
                Name = "Total Games Played",
                Description = "Number of games played",
                Type = ApplicationRoleConnectionMetadataType.IntegerGreaterThanOrEqual
            }
        };
        _mock.Setup(r => r.GetApplicationRoleConnectionMetadataAsync(999UL)).ReturnsAsync(records);

        var result = await _mock.Object.GetApplicationRoleConnectionMetadataAsync(999UL);

        result.Should().HaveCount(1);
        result![0].Key.Should().Be("total_games");
        result[0].Type.Should().Be(ApplicationRoleConnectionMetadataType.IntegerGreaterThanOrEqual);
    }

    [Fact]
    public async Task UpdateApplicationRoleConnectionMetadataAsync_Returns_Upserted_Records()
    {
        var records = new List<ApplicationRoleConnectionMetadata>
        {
            new() { Key = "win_rate", Name = "Win Rate", Description = "Win percentage", Type = ApplicationRoleConnectionMetadataType.IntegerGreaterThanOrEqual }
        };
        _mock.Setup(r => r.UpdateApplicationRoleConnectionMetadataAsync(999UL, It.IsAny<List<ApplicationRoleConnectionMetadata>>()))
             .ReturnsAsync(records);

        var result = await _mock.Object.UpdateApplicationRoleConnectionMetadataAsync(999UL, records);

        result.Should().HaveCount(1);
        result![0].Key.Should().Be("win_rate");
    }

    // ─────────────────────────────────────────────
    //  Guild Member Helpers
    // ─────────────────────────────────────────────

    [Fact]
    public async Task SearchGuildMembersAsync_Returns_MatchingMembers()
    {
        var members = new List<GuildMember>
        {
            new() { User = new User { Id = 400UL, Username = "alicewonder" }, Nick = "Alice" },
            new() { User = new User { Id = 401UL, Username = "alice123" }, Nick = null }
        };
        _mock.Setup(r => r.SearchGuildMembersAsync(200UL, "alice", It.IsAny<int?>()))
             .ReturnsAsync(members);

        var result = await _mock.Object.SearchGuildMembersAsync(200UL, "alice");

        result.Should().HaveCount(2);
        result![0].User!.Username.Should().StartWith("alice");
    }

    [Fact]
    public async Task ModifyCurrentMemberAsync_Returns_Updated_Member()
    {
        var member = new GuildMember { Nick = "CoolBot", User = new User { Id = 1UL, Username = "MyBot" } };
        _mock.Setup(r => r.ModifyCurrentMemberAsync(200UL, "CoolBot")).ReturnsAsync(member);

        var result = await _mock.Object.ModifyCurrentMemberAsync(200UL, "CoolBot");

        result.Should().NotBeNull();
        result!.Nick.Should().Be("CoolBot");
    }

    [Fact]
    public async Task ModifyCurrentMemberAsync_Returns_Null_On_Failure()
    {
        _mock.Setup(r => r.ModifyCurrentMemberAsync(It.IsAny<ulong>(), It.IsAny<string?>()))
             .ReturnsAsync((GuildMember?)null);

        var result = await _mock.Object.ModifyCurrentMemberAsync(200UL, null);

        result.Should().BeNull();
    }
}
