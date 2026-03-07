#nullable enable
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
/// Tests for the REST endpoints introduced in alpha13.
/// Uses a mocked <see cref="IDiscordRestClient"/> to verify interface contracts
/// for reactions, channel follows, guild metadata, invites, and guild templates.
/// </summary>
public class Alpha13EndpointsTests
{
    private readonly Mock<IDiscordRestClient> _mock = new();

    // ─── Reactions ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReactionsAsync_Returns_UserList()
    {
        var users = new List<User>
        {
            new() { Id = 1UL, Username = "Alice" },
            new() { Id = 2UL, Username = "Bob" },
        };
        _mock.Setup(r => r.GetReactionsAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<ulong?>(), It.IsAny<int?>()))
             .ReturnsAsync(users);

        var result = await _mock.Object.GetReactionsAsync(100UL, 200UL, "👍");

        result.Should().HaveCount(2);
        result![0].Username.Should().Be("Alice");
    }

    [Fact]
    public async Task GetReactionsAsync_Returns_Null_When_NotFound()
    {
        _mock.Setup(r => r.GetReactionsAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<ulong?>(), It.IsAny<int?>()))
             .ReturnsAsync((List<User>?)null);

        var result = await _mock.Object.GetReactionsAsync(0UL, 0UL, "❓");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReactionsAsync_Accepts_OptionalPaginationParams()
    {
        _mock.Setup(r => r.GetReactionsAsync(
                100UL, 200UL, "⭐", 0, 150UL, 25))
             .ReturnsAsync(new List<User>());

        var result = await _mock.Object.GetReactionsAsync(
            channelId: 100UL, messageId: 200UL, emoji: "⭐",
            type: 0, after: 150UL, limit: 25);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ─── Announcement Channel Follow ─────────────────────────────────────────

    [Fact]
    public async Task FollowAnnouncementChannelAsync_Returns_FollowedChannel()
    {
        var followed = new FollowedChannel
        {
            ChannelId = 111UL,
            WebhookId = 222UL,
        };
        _mock.Setup(r => r.FollowAnnouncementChannelAsync(111UL, 999UL))
             .ReturnsAsync(followed);

        var result = await _mock.Object.FollowAnnouncementChannelAsync(111UL, 999UL);

        result.Should().NotBeNull();
        result!.ChannelId.Should().Be(111UL);
        result.WebhookId.Should().Be(222UL);
    }

    [Fact]
    public async Task FollowAnnouncementChannelAsync_Returns_Null_On_Failure()
    {
        _mock.Setup(r => r.FollowAnnouncementChannelAsync(
                It.IsAny<ulong>(), It.IsAny<ulong>()))
             .ReturnsAsync((FollowedChannel?)null);

        var result = await _mock.Object.FollowAnnouncementChannelAsync(0UL, 0UL);

        result.Should().BeNull();
    }

    // ─── Guild Preview ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuildPreviewAsync_Returns_GuildPreview()
    {
        var preview = new GuildPreview
        {
            Id = 333UL,
            Name = "Preview Guild",
            ApproximateMemberCount = 5000,
        };
        _mock.Setup(r => r.GetGuildPreviewAsync(333UL)).ReturnsAsync(preview);

        var result = await _mock.Object.GetGuildPreviewAsync(333UL);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Preview Guild");
        result.ApproximateMemberCount.Should().Be(5000);
    }

    // ─── Guild Widget ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuildWidgetSettingsAsync_Returns_Settings()
    {
        var settings = new GuildWidgetSettings { Enabled = true, ChannelId = 444UL };
        _mock.Setup(r => r.GetGuildWidgetSettingsAsync(333UL)).ReturnsAsync(settings);

        var result = await _mock.Object.GetGuildWidgetSettingsAsync(333UL);

        result.Should().NotBeNull();
        result!.Enabled.Should().BeTrue();
        result.ChannelId.Should().Be(444UL);
    }

    [Fact]
    public async Task ModifyGuildWidgetAsync_Returns_Updated_Settings()
    {
        var updated = new GuildWidgetSettings { Enabled = false, ChannelId = null };
        _mock.Setup(r => r.ModifyGuildWidgetAsync(333UL, It.IsAny<ModifyGuildWidgetRequest>()))
             .ReturnsAsync(updated);

        var result = await _mock.Object.ModifyGuildWidgetAsync(
            333UL, new ModifyGuildWidgetRequest { Enabled = false });

        result.Should().NotBeNull();
        result!.Enabled.Should().BeFalse();
    }

    // ─── Guild Vanity URL ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuildVanityUrlAsync_Returns_VanityUrl()
    {
        var vanity = new VanityUrl { Code = "my-cool-server", Uses = 100 };
        _mock.Setup(r => r.GetGuildVanityUrlAsync(333UL)).ReturnsAsync(vanity);

        var result = await _mock.Object.GetGuildVanityUrlAsync(333UL);

        result.Should().NotBeNull();
        result!.Code.Should().Be("my-cool-server");
        result.Uses.Should().Be(100);
    }

    [Fact]
    public async Task GetGuildVanityUrlAsync_Returns_Null_When_NoVanity()
    {
        _mock.Setup(r => r.GetGuildVanityUrlAsync(It.IsAny<ulong>()))
             .ReturnsAsync((VanityUrl?)null);

        var result = await _mock.Object.GetGuildVanityUrlAsync(999UL);

        result.Should().BeNull();
    }

    // ─── Guild Welcome Screen ─────────────────────────────────────────────────

    [Fact]
    public async Task GetGuildWelcomeScreenAsync_Returns_WelcomeScreen()
    {
        var screen = new WelcomeScreen
        {
            Description = "Welcome!",
            WelcomeChannels = new List<WelcomeScreenChannel>
            {
                new() { ChannelId = 555UL, Description = "Start here" },
            },
        };
        _mock.Setup(r => r.GetGuildWelcomeScreenAsync(333UL)).ReturnsAsync(screen);

        var result = await _mock.Object.GetGuildWelcomeScreenAsync(333UL);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Welcome!");
        result.WelcomeChannels.Should().HaveCount(1);
        result.WelcomeChannels[0].Description.Should().Be("Start here");
    }

    [Fact]
    public async Task ModifyGuildWelcomeScreenAsync_Returns_Updated_Screen()
    {
        var updated = new WelcomeScreen
        {
            Description = "Updated welcome",
            WelcomeChannels = new List<WelcomeScreenChannel>(),
        };
        _mock.Setup(r => r.ModifyGuildWelcomeScreenAsync(
                333UL, It.IsAny<ModifyGuildWelcomeScreenRequest>()))
             .ReturnsAsync(updated);

        var request = new ModifyGuildWelcomeScreenRequest { Description = "Updated welcome" };
        var result = await _mock.Object.ModifyGuildWelcomeScreenAsync(333UL, request);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Updated welcome");
    }

    // ─── Guild Channel / Role Position Reorder ───────────────────────────────

    [Fact]
    public async Task ModifyGuildChannelPositionsAsync_Returns_True_On_Success()
    {
        _mock.Setup(r => r.ModifyGuildChannelPositionsAsync(
                It.IsAny<ulong>(), It.IsAny<IEnumerable<ModifyChannelPositionRequest>>()))
             .ReturnsAsync(true);

        var positions = new List<ModifyChannelPositionRequest>
        {
            new() { Id = 1UL, Position = 0 },
            new() { Id = 2UL, Position = 1 },
        };

        var result = await _mock.Object.ModifyGuildChannelPositionsAsync(333UL, positions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ModifyGuildRolePositionsAsync_Returns_Updated_Roles()
    {
        var roles = new List<Role>
        {
            new() { Id = 10UL, Name = "Admin" },
            new() { Id = 11UL, Name = "Member" },
        };
        _mock.Setup(r => r.ModifyGuildRolePositionsAsync(
                It.IsAny<ulong>(), It.IsAny<IEnumerable<ModifyRolePositionRequest>>()))
             .ReturnsAsync(roles);

        var positions = new List<ModifyRolePositionRequest>
        {
            new() { Id = 10UL, Position = 0 },
            new() { Id = 11UL, Position = 1 },
        };

        var result = await _mock.Object.ModifyGuildRolePositionsAsync(333UL, positions);

        result.Should().HaveCount(2);
        result![0].Name.Should().Be("Admin");
    }

    // ─── Invites ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInviteAsync_Returns_Invite()
    {
        var invite = new Invite { Code = "abc123" };
        _mock.Setup(r => r.GetInviteAsync("abc123", null, null, null))
             .ReturnsAsync(invite);

        var result = await _mock.Object.GetInviteAsync("abc123");

        result.Should().NotBeNull();
        result!.Code.Should().Be("abc123");
    }

    [Fact]
    public async Task GetInviteAsync_Accepts_OptionalParams()
    {
        var invite = new Invite { Code = "xyz", ApproximateMemberCount = 42 };
        _mock.Setup(r => r.GetInviteAsync("xyz", true, true, null))
             .ReturnsAsync(invite);

        var result = await _mock.Object.GetInviteAsync(
            "xyz", withCounts: true, withExpiration: true);

        result.Should().NotBeNull();
        result!.ApproximateMemberCount.Should().Be(42);
    }

    [Fact]
    public async Task GetInviteAsync_Returns_Null_When_NotFound()
    {
        _mock.Setup(r => r.GetInviteAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<ulong?>()))
             .ReturnsAsync((Invite?)null);

        var result = await _mock.Object.GetInviteAsync("doesnotexist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteInviteAsync_Returns_Invite_On_Success()
    {
        var invite = new Invite { Code = "abc123" };
        _mock.Setup(r => r.DeleteInviteAsync("abc123", null)).ReturnsAsync(invite);

        var result = await _mock.Object.DeleteInviteAsync("abc123");

        result.Should().NotBeNull();
        result!.Code.Should().Be("abc123");
    }

    [Fact]
    public async Task DeleteInviteAsync_Accepts_AuditLog_Reason()
    {
        var invite = new Invite { Code = "abc123" };
        _mock.Setup(r => r.DeleteInviteAsync("abc123", "Spam link")).ReturnsAsync(invite);

        var result = await _mock.Object.DeleteInviteAsync("abc123", reason: "Spam link");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteInviteAsync_Returns_Null_On_Failure()
    {
        _mock.Setup(r => r.DeleteInviteAsync(It.IsAny<string>(), It.IsAny<string?>()))
             .ReturnsAsync((Invite?)null);

        var result = await _mock.Object.DeleteInviteAsync("invalid");

        result.Should().BeNull();
    }

    // ─── Guild Templates ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetGuildTemplatesAsync_Returns_TemplateList()
    {
        var templates = new List<GuildTemplate>
        {
            new() { Code = "tmpl1", Name = "My Template" },
        };
        _mock.Setup(r => r.GetGuildTemplatesAsync(333UL)).ReturnsAsync(templates);

        var result = await _mock.Object.GetGuildTemplatesAsync(333UL);

        result.Should().HaveCount(1);
        result![0].Code.Should().Be("tmpl1");
    }

    [Fact]
    public async Task GetGuildTemplateAsync_Returns_Template()
    {
        var template = new GuildTemplate { Code = "tmpl1", Name = "My Template", UsageCount = 7 };
        _mock.Setup(r => r.GetGuildTemplateAsync("tmpl1")).ReturnsAsync(template);

        var result = await _mock.Object.GetGuildTemplateAsync("tmpl1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("My Template");
        result.UsageCount.Should().Be(7);
    }

    [Fact]
    public async Task CreateGuildFromTemplateAsync_Returns_NewGuild()
    {
        var guild = new Guild { Id = 888UL, Name = "From Template" };
        _mock.Setup(r => r.CreateGuildFromTemplateAsync(
                "tmpl1", It.IsAny<CreateGuildFromTemplateRequest>()))
             .ReturnsAsync(guild);

        var request = new CreateGuildFromTemplateRequest { Name = "From Template" };
        var result = await _mock.Object.CreateGuildFromTemplateAsync("tmpl1", request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("From Template");
    }

    [Fact]
    public async Task CreateGuildTemplateAsync_Returns_CreatedTemplate()
    {
        var template = new GuildTemplate { Code = "new_tmpl", Name = "Backup" };
        _mock.Setup(r => r.CreateGuildTemplateAsync(
                333UL, It.IsAny<CreateGuildTemplateRequest>()))
             .ReturnsAsync(template);

        var request = new CreateGuildTemplateRequest { Name = "Backup" };
        var result = await _mock.Object.CreateGuildTemplateAsync(333UL, request);

        result.Should().NotBeNull();
        result!.Code.Should().Be("new_tmpl");
    }

    [Fact]
    public async Task SyncGuildTemplateAsync_Returns_SyncedTemplate()
    {
        var template = new GuildTemplate { Code = "tmpl1", Name = "Synced Template" };
        _mock.Setup(r => r.SyncGuildTemplateAsync(333UL, "tmpl1")).ReturnsAsync(template);

        var result = await _mock.Object.SyncGuildTemplateAsync(333UL, "tmpl1");

        result.Should().NotBeNull();
        result!.Code.Should().Be("tmpl1");
    }

    [Fact]
    public async Task ModifyGuildTemplateAsync_Returns_UpdatedTemplate()
    {
        var updated = new GuildTemplate { Code = "tmpl1", Name = "Renamed Template" };
        _mock.Setup(r => r.ModifyGuildTemplateAsync(
                333UL, "tmpl1", It.IsAny<ModifyGuildTemplateRequest>()))
             .ReturnsAsync(updated);

        var request = new ModifyGuildTemplateRequest { Name = "Renamed Template" };
        var result = await _mock.Object.ModifyGuildTemplateAsync(333UL, "tmpl1", request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Renamed Template");
    }

    [Fact]
    public async Task DeleteGuildTemplateAsync_Returns_Null_After_Delete()
    {
        // The Discord API returns the deleted template; null signals failure/not found.
        _mock.Setup(r => r.DeleteGuildTemplateAsync(333UL, "tmpl1"))
             .ReturnsAsync((GuildTemplate?)null);

        var result = await _mock.Object.DeleteGuildTemplateAsync(333UL, "tmpl1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGuildTemplateAsync_Returns_Template_On_Success()
    {
        var deleted = new GuildTemplate { Code = "tmpl1", Name = "Deleted" };
        _mock.Setup(r => r.DeleteGuildTemplateAsync(333UL, "tmpl1")).ReturnsAsync(deleted);

        var result = await _mock.Object.DeleteGuildTemplateAsync(333UL, "tmpl1");

        result.Should().NotBeNull();
        result!.Code.Should().Be("tmpl1");
    }

    // ─── Request Model Completeness ───────────────────────────────────────────

    [Fact]
    public void CreateGuildTemplateRequest_Sets_Properties()
    {
        var req = new CreateGuildTemplateRequest
        {
            Name = "Server Backup",
            Description = "Complete backup of server settings",
        };

        req.Name.Should().Be("Server Backup");
        req.Description.Should().Be("Complete backup of server settings");
    }

    [Fact]
    public void ModifyGuildTemplateRequest_AllowsNullFields()
    {
        var req = new ModifyGuildTemplateRequest
        {
            Name = "New Name",
            Description = null,
        };

        req.Name.Should().Be("New Name");
        req.Description.Should().BeNull();
    }

    [Fact]
    public void ModifyGuildWidgetRequest_Sets_Properties()
    {
        var req = new ModifyGuildWidgetRequest
        {
            Enabled = true,
            ChannelId = 12345UL,
        };

        req.Enabled.Should().BeTrue();
        req.ChannelId.Should().Be(12345UL);
    }

    [Fact]
    public void ModifyGuildWelcomeScreenRequest_Sets_Channels()
    {
        var req = new ModifyGuildWelcomeScreenRequest
        {
            Enabled = true,
            Description = "Welcome!",
            WelcomeChannels = new List<WelcomeScreenChannelRequest>
            {
                new() { ChannelId = 777UL, Description = "Read me" },
            },
        };

        req.Enabled.Should().BeTrue();
        req.WelcomeChannels.Should().HaveCount(1);
        req.WelcomeChannels![0].ChannelId.Should().Be(777UL);
    }

    [Fact]
    public void ModifyChannelPositionRequest_Sets_Properties()
    {
        var req = new ModifyChannelPositionRequest
        {
            Id = 100UL,
            Position = 3,
            LockPermissions = true,
            ParentId = 50UL,
        };

        req.Id.Should().Be(100UL);
        req.Position.Should().Be(3);
        req.LockPermissions.Should().BeTrue();
        req.ParentId.Should().Be(50UL);
    }

    [Fact]
    public void ModifyRolePositionRequest_Sets_Properties()
    {
        var req = new ModifyRolePositionRequest { Id = 200UL, Position = 1 };

        req.Id.Should().Be(200UL);
        req.Position.Should().Be(1);
    }
}
