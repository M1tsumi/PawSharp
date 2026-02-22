#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using PawSharp.Core.Builders;
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;
using Xunit;

namespace PawSharp.Core.Tests;

/// <summary>
/// Unit tests for alpha13 additions:
/// typed message components, flags enums, EmbedBuilder, and new entity deserialization.
/// </summary>
public class Alpha13CoreTests
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ─── EmbedBuilder ─────────────────────────────────────────────────────────

    [Fact]
    public void EmbedBuilder_Builds_TitleAndDescription()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Test Title")
            .WithDescription("Test Description")
            .Build();

        embed.Title.Should().Be("Test Title");
        embed.Description.Should().Be("Test Description");
    }

    [Fact]
    public void EmbedBuilder_Builds_ColorFromInt()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Color Test")
            .WithColor(0x5865F2)
            .Build();

        embed.Color.Should().Be(0x5865F2);
    }

    [Fact]
    public void EmbedBuilder_Builds_ColorFromRgb()
    {
        var embed = new EmbedBuilder()
            .WithTitle("RGB Color")
            .WithColor(88, 101, 242) // 0x5865F2
            .Build();

        embed.Color.Should().Be(0x5865F2);
    }

    [Fact]
    public void EmbedBuilder_Builds_FooterAndAuthor()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Footer + Author")
            .WithFooter("Footer text", "https://example.com/icon.png")
            .WithAuthor("Bot Author", url: "https://example.com", iconUrl: "https://example.com/avatar.png")
            .Build();

        embed.Footer.Should().NotBeNull();
        embed.Footer!.Text.Should().Be("Footer text");
        embed.Footer.IconUrl.Should().Be("https://example.com/icon.png");

        embed.Author.Should().NotBeNull();
        embed.Author!.Name.Should().Be("Bot Author");
        embed.Author.Url.Should().Be("https://example.com");
        embed.Author.IconUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public void EmbedBuilder_Builds_Fields()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Fields")
            .AddField("Name1", "Value1", inline: true)
            .AddField("Name2", "Value2", inline: false)
            .Build();

        embed.Fields.Should().HaveCount(2);
        embed.Fields![0].Name.Should().Be("Name1");
        embed.Fields[0].Value.Should().Be("Value1");
        embed.Fields[0].Inline.Should().Be(true);
        embed.Fields[1].Inline.Should().Be(false);
    }

    [Fact]
    public void EmbedBuilder_Builds_WithTimestamp()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var embed = new EmbedBuilder()
            .WithTitle("Timestamp")
            .WithTimestamp()
            .Build();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        embed.Timestamp.Should().NotBeNull();
        embed.Timestamp!.Value.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void EmbedBuilder_Builds_WithExplicitTimestamp()
    {
        var ts = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var embed = new EmbedBuilder()
            .WithTitle("Explicit TS")
            .WithTimestamp(ts)
            .Build();

        embed.Timestamp.Should().Be(ts);
    }

    [Fact]
    public void EmbedBuilder_Builds_ImageAndThumbnail()
    {
        var embed = new EmbedBuilder()
            .WithImage("https://example.com/image.png")
            .WithThumbnail("https://example.com/thumb.png")
            .Build();

        embed.Image.Should().NotBeNull();
        embed.Image!.Url.Should().Be("https://example.com/image.png");
        embed.Thumbnail.Should().NotBeNull();
        embed.Thumbnail!.Url.Should().Be("https://example.com/thumb.png");
    }

    [Fact]
    public void EmbedBuilder_Throws_WhenEmpty()
    {
        var builder = new EmbedBuilder();
        builder.Invoking(b => b.Build()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EmbedBuilder_Throws_WhenTitleTooLong()
    {
        var builder = new EmbedBuilder();
        builder.Invoking(b => b.WithTitle(new string('x', EmbedBuilder.MaxTitleLength + 1)))
               .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmbedBuilder_Throws_WhenTooManyFields()
    {
        var builder = new EmbedBuilder().WithTitle("Many Fields");
        for (int i = 0; i < EmbedBuilder.MaxFields; i++)
            builder.AddField($"F{i}", "v");

        builder.Invoking(b => b.AddField("overflow", "oops"))
               .Should().Throw<InvalidOperationException>();
    }

    // ─── MessageFlags ─────────────────────────────────────────────────────────

    [Fact]
    public void MessageFlags_Ephemeral_HasCorrectValue()
    {
        ((int)MessageFlags.Ephemeral).Should().Be(64); // 1 << 6
    }

    [Fact]
    public void MessageFlags_CanCombine_Flags()
    {
        var flags = MessageFlags.Ephemeral | MessageFlags.SuppressEmbeds;
        flags.HasFlag(MessageFlags.Ephemeral).Should().BeTrue();
        flags.HasFlag(MessageFlags.SuppressEmbeds).Should().BeTrue();
        flags.HasFlag(MessageFlags.Urgent).Should().BeFalse();
    }

    // ─── ChannelFlags ─────────────────────────────────────────────────────────

    [Fact]
    public void ChannelFlags_RequireTag_HasCorrectValue()
    {
        ((int)ChannelFlags.RequireTag).Should().Be(16); // 1 << 4
    }

    // ─── GuildMemberFlags ────────────────────────────────────────────────────

    [Fact]
    public void GuildMemberFlags_CompletedOnboarding_HasCorrectValue()
    {
        ((int)GuildMemberFlags.CompletedOnboarding).Should().Be(2); // 1 << 1
    }

    // ─── Typed Component Deserialization ─────────────────────────────────────

    [Fact]
    public void MessageComponent_Deserializes_ActionRow()
    {
        var json = """
            {
                "type": 1,
                "components": [
                    { "type": 2, "style": 1, "label": "Click me", "custom_id": "btn_1" }
                ]
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<ActionRow>();
        var row = (ActionRow)component!;
        row.Components.Should().HaveCount(1);
        row.Components[0].Should().BeOfType<Button>();
    }

    [Fact]
    public void MessageComponent_Deserializes_Button()
    {
        var json = """
            {
                "type": 2,
                "style": 3,
                "label": "Confirm",
                "custom_id": "confirm_action",
                "disabled": false
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<Button>();
        var btn = (Button)component!;
        btn.Style.Should().Be(ButtonStyle.Success);
        btn.Label.Should().Be("Confirm");
        btn.CustomId.Should().Be("confirm_action");
        btn.Disabled.Should().BeFalse();
    }

    [Fact]
    public void MessageComponent_Deserializes_LinkButton()
    {
        var json = """
            {
                "type": 2,
                "style": 5,
                "label": "Visit Site",
                "url": "https://example.com"
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<Button>();
        var btn = (Button)component!;
        btn.Style.Should().Be(ButtonStyle.Link);
        btn.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void MessageComponent_Deserializes_SelectMenu()
    {
        var json = """
            {
                "type": 3,
                "custom_id": "color_picker",
                "placeholder": "Pick a colour",
                "min_values": 1,
                "max_values": 2,
                "options": [
                    { "label": "Red",   "value": "red" },
                    { "label": "Blue",  "value": "blue" },
                    { "label": "Green", "value": "green" }
                ]
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<SelectMenu>();
        var menu = (SelectMenu)component!;
        menu.CustomId.Should().Be("color_picker");
        menu.Placeholder.Should().Be("Pick a colour");
        menu.MinValues.Should().Be(1);
        menu.MaxValues.Should().Be(2);
        menu.Options.Should().HaveCount(3);
        menu.Options[0].Label.Should().Be("Red");
        menu.Options[0].Value.Should().Be("red");
    }

    [Fact]
    public void MessageComponent_Deserializes_TextInput()
    {
        var json = """
            {
                "type": 4,
                "custom_id": "feedback",
                "style": 2,
                "label": "Your feedback",
                "min_length": 10,
                "max_length": 500,
                "required": true,
                "placeholder": "Tell us what you think..."
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<TextInput>();
        var input = (TextInput)component!;
        input.CustomId.Should().Be("feedback");
        input.Style.Should().Be(TextInputStyle.Paragraph);
        input.MinLength.Should().Be(10);
        input.MaxLength.Should().Be(500);
        input.Placeholder.Should().Be("Tell us what you think...");
    }

    [Fact]
    public void MessageComponent_Deserializes_UserSelectMenu()
    {
        var json = """
            {
                "type": 5,
                "custom_id": "assign_user",
                "placeholder": "Select a user",
                "max_values": 1
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<UserSelectMenu>();
        var menu = (UserSelectMenu)component!;
        menu.CustomId.Should().Be("assign_user");
        menu.MaxValues.Should().Be(1);
    }

    [Fact]
    public void MessageComponent_Deserializes_ChannelSelectMenu()
    {
        var json = """
            {
                "type": 8,
                "custom_id": "pick_channel",
                "channel_types": [0, 5]
            }
            """;

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<ChannelSelectMenu>();
        var menu = (ChannelSelectMenu)component!;
        menu.ChannelTypes.Should().BeEquivalentTo(new[] { 0, 5 });
    }

    [Fact]
    public void MessageComponent_Deserializes_UnknownType()
    {
        var json = """{ "type": 99 }""";

        var component = JsonSerializer.Deserialize<MessageComponent>(json, _opts);

        component.Should().BeOfType<UnknownComponent>();
    }

    // ─── ComponentType enum values ────────────────────────────────────────────

    [Fact]
    public void ComponentType_Values_MatchDiscordSpec()
    {
        ((int)ComponentType.ActionRow).Should().Be(1);
        ((int)ComponentType.Button).Should().Be(2);
        ((int)ComponentType.StringSelect).Should().Be(3);
        ((int)ComponentType.TextInput).Should().Be(4);
        ((int)ComponentType.UserSelect).Should().Be(5);
        ((int)ComponentType.RoleSelect).Should().Be(6);
        ((int)ComponentType.MentionableSelect).Should().Be(7);
        ((int)ComponentType.ChannelSelect).Should().Be(8);
    }

    [Fact]
    public void ButtonStyle_Values_MatchDiscordSpec()
    {
        ((int)ButtonStyle.Primary).Should().Be(1);
        ((int)ButtonStyle.Secondary).Should().Be(2);
        ((int)ButtonStyle.Success).Should().Be(3);
        ((int)ButtonStyle.Danger).Should().Be(4);
        ((int)ButtonStyle.Link).Should().Be(5);
        ((int)ButtonStyle.Premium).Should().Be(6);
    }

    [Fact]
    public void TextInputStyle_Values_MatchDiscordSpec()
    {
        ((int)TextInputStyle.Short).Should().Be(1);
        ((int)TextInputStyle.Paragraph).Should().Be(2);
    }

    // ─── GuildMisc entity deserialization ────────────────────────────────────

    [Fact]
    public void GuildPreview_Deserializes_Correctly()
    {
        var json = """
            {
                "id": "123456789012345678",
                "name": "Cool Server",
                "icon": null,
                "approximate_member_count": 1500,
                "approximate_presence_count": 200,
                "description": "A very cool server.",
                "features": ["COMMUNITY"],
                "emojis": [],
                "stickers": []
            }
            """;

        var preview = JsonSerializer.Deserialize<GuildPreview>(json, _opts);

        preview.Should().NotBeNull();
        preview!.Name.Should().Be("Cool Server");
        preview.ApproximateMemberCount.Should().Be(1500);
        preview.ApproximatePresenceCount.Should().Be(200);
        preview.Description.Should().Be("A very cool server.");
        preview.Features.Should().ContainSingle("COMMUNITY");
    }

    [Fact]
    public void GuildWidgetSettings_Deserializes_Correctly()
    {
        var json = """{ "enabled": true, "channel_id": "111111111111111111" }""";

        var widget = JsonSerializer.Deserialize<GuildWidgetSettings>(json, _opts);

        widget.Should().NotBeNull();
        widget!.Enabled.Should().BeTrue();
        widget.ChannelId.Should().Be(111111111111111111UL);
    }

    [Fact]
    public void WelcomeScreen_Deserializes_Correctly()
    {
        var json = """
            {
                "description": "Welcome to the server!",
                "welcome_channels": [
                    {
                        "channel_id": "222222222222222222",
                        "description": "General chat",
                        "emoji_id": null,
                        "emoji_name": "\ud83d\udc4b"
                    }
                ]
            }
            """;

        var screen = JsonSerializer.Deserialize<WelcomeScreen>(json, _opts);

        screen.Should().NotBeNull();
        screen!.Description.Should().Be("Welcome to the server!");
        screen.WelcomeChannels.Should().HaveCount(1);
        screen.WelcomeChannels[0].Description.Should().Be("General chat");
        screen.WelcomeChannels[0].ChannelId.Should().Be(222222222222222222UL);
    }

    [Fact]
    public void FollowedChannel_Deserializes_Correctly()
    {
        var json = """
            {
                "channel_id": "333333333333333333",
                "webhook_id": "444444444444444444"
            }
            """;

        var followed = JsonSerializer.Deserialize<FollowedChannel>(json, _opts);

        followed.Should().NotBeNull();
        followed!.ChannelId.Should().Be(333333333333333333UL);
        followed.WebhookId.Should().Be(444444444444444444UL);
    }

    [Fact]
    public void VanityUrl_Deserializes_Correctly()
    {
        var json = """{ "code": "my-server", "uses": 42 }""";

        var vanity = JsonSerializer.Deserialize<VanityUrl>(json, _opts);

        vanity.Should().NotBeNull();
        vanity!.Code.Should().Be("my-server");
        vanity.Uses.Should().Be(42);
    }
}
