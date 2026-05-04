using System;
using FluentAssertions;
using PawSharp.Core.Entities;
using Xunit;

namespace PawSharp.Core.Tests;

public class NewFeatureTests
{
    // Role Gradient Color Tests
    [Fact]
    public void RoleColors_PrimaryColor_StoresValue()
    {
        var colors = new RoleColors { PrimaryColor = 0xFF0000 };
        colors.PrimaryColor.Should().Be(0xFF0000);
    }

    [Fact]
    public void RoleColors_SecondaryColor_StoresValue()
    {
        var colors = new RoleColors { SecondaryColor = 0x00FF00 };
        colors.SecondaryColor.Should().Be(0x00FF00);
    }

    [Fact]
    public void RoleColors_TertiaryColor_StoresValue()
    {
        var colors = new RoleColors { TertiaryColor = 0x0000FF };
        colors.TertiaryColor.Should().Be(0x0000FF);
    }

    [Fact]
    public void RoleColors_IsGradient_ReturnsTrueWhenSecondaryColorSet()
    {
        var colors = new RoleColors { SecondaryColor = 0x00FF00 };
        colors.IsGradient.Should().BeTrue();
    }

    [Fact]
    public void RoleColors_IsGradient_ReturnsFalseWhenSecondaryColorNull()
    {
        var colors = new RoleColors();
        colors.IsGradient.Should().BeFalse();
    }

    [Fact]
    public void RoleColors_IsHolographic_ReturnsTrueWhenTertiaryColorSet()
    {
        var colors = new RoleColors { TertiaryColor = 0x0000FF };
        colors.IsHolographic.Should().BeTrue();
    }

    [Fact]
    public void RoleColors_IsHolographic_ReturnsFalseWhenTertiaryColorNull()
    {
        var colors = new RoleColors();
        colors.IsHolographic.Should().BeFalse();
    }

    [Fact]
    public void RoleColors_GetPrimaryColorHex_ReturnsCorrectFormat()
    {
        var colors = new RoleColors { PrimaryColor = 0xFF0000 };
        colors.GetPrimaryColorHex().Should().Be("FF0000");
    }

    [Fact]
    public void RoleColors_GetSecondaryColorHex_ReturnsCorrectFormat()
    {
        var colors = new RoleColors { SecondaryColor = 0x00FF00 };
        colors.GetSecondaryColorHex().Should().Be("00FF00");
    }

    [Fact]
    public void RoleColors_GetSecondaryColorHex_ReturnsNullWhenNotSet()
    {
        var colors = new RoleColors();
        colors.GetSecondaryColorHex().Should().BeNull();
    }

    [Fact]
    public void RoleColors_GetTertiaryColorHex_ReturnsCorrectFormat()
    {
        var colors = new RoleColors { TertiaryColor = 0x0000FF };
        colors.GetTertiaryColorHex().Should().Be("0000FF");
    }

    [Fact]
    public void RoleColors_GetTertiaryColorHex_ReturnsNullWhenNotSet()
    {
        var colors = new RoleColors();
        colors.GetTertiaryColorHex().Should().BeNull();
    }

    // User Primary Guild Tests
    [Fact]
    public void UserPrimaryGuild_IdentityGuildId_StoresValue()
    {
        var primaryGuild = new UserPrimaryGuild { IdentityGuildId = 123456789UL };
        primaryGuild.IdentityGuildId.Should().Be(123456789UL);
    }

    [Fact]
    public void UserPrimaryGuild_IdentityEnabled_StoresValue()
    {
        var primaryGuild = new UserPrimaryGuild { IdentityEnabled = true };
        primaryGuild.IdentityEnabled.Should().BeTrue();
    }

    [Fact]
    public void UserPrimaryGuild_Tag_StoresValue()
    {
        var primaryGuild = new UserPrimaryGuild { Tag = "DISC" };
        primaryGuild.Tag.Should().Be("DISC");
    }

    [Fact]
    public void UserPrimaryGuild_Badge_StoresValue()
    {
        var primaryGuild = new UserPrimaryGuild { Badge = "badgehash" };
        primaryGuild.Badge.Should().Be("badgehash");
    }

    [Fact]
    public void UserPrimaryGuild_IsDisplayed_ReturnsTrueWhenEnabledAndTagSet()
    {
        var primaryGuild = new UserPrimaryGuild
        {
            IdentityEnabled = true,
            Tag = "DISC"
        };
        primaryGuild.IsDisplayed.Should().BeTrue();
    }

    [Fact]
    public void UserPrimaryGuild_IsDisplayed_ReturnsFalseWhenDisabled()
    {
        var primaryGuild = new UserPrimaryGuild
        {
            IdentityEnabled = false,
            Tag = "DISC"
        };
        primaryGuild.IsDisplayed.Should().BeFalse();
    }

    [Fact]
    public void UserPrimaryGuild_IsDisplayed_ReturnsFalseWhenTagEmpty()
    {
        var primaryGuild = new UserPrimaryGuild
        {
            IdentityEnabled = true,
            Tag = ""
        };
        primaryGuild.IsDisplayed.Should().BeFalse();
    }

    // Invite Flags Tests
    [Fact]
    public void InviteFlags_Guest_HasCorrectValue()
    {
        ((int)InviteFlags.Guest).Should().Be(1 << 2);
    }

    [Fact]
    public void Invite_IsGuest_ReturnsTrueWhenGuestFlagSet()
    {
        var invite = new Invite { Flags = (InviteFlags)(1 << 2) };
        invite.IsGuest.Should().BeTrue();
    }

    [Fact]
    public void Invite_IsGuest_ReturnsFalseWhenGuestFlagNotSet()
    {
        var invite = new Invite { Flags = InviteFlags.None };
        invite.IsGuest.Should().BeFalse();
    }

    [Fact]
    public void Invite_IsGuest_ReturnsFalseWhenFlagsNull()
    {
        var invite = new Invite { Flags = null };
        invite.IsGuest.Should().BeFalse();
    }

    // Role Colors Integration Tests
    [Fact]
    public void Role_CanHaveGradientColors()
    {
        var role = new Role
        {
            Colors = new RoleColors
            {
                PrimaryColor = 0xFF0000,
                SecondaryColor = 0x00FF00
            }
        };

        role.Colors.Should().NotBeNull();
        role.Colors!.IsGradient.Should().BeTrue();
        role.Colors!.IsHolographic.Should().BeFalse();
    }

    [Fact]
    public void Role_CanHaveHolographicColors()
    {
        var role = new Role
        {
            Colors = new RoleColors
            {
                PrimaryColor = 11127295,
                SecondaryColor = 16759788,
                TertiaryColor = 16761760
            }
        };

        role.Colors.Should().NotBeNull();
        role.Colors!.IsGradient.Should().BeTrue();
        role.Colors!.IsHolographic.Should().BeTrue();
    }

    // User Primary Guild Integration Tests
    [Fact]
    public void User_CanHavePrimaryGuild()
    {
        var user = new User
        {
            PrimaryGuild = new UserPrimaryGuild
            {
                IdentityGuildId = 123456789UL,
                IdentityEnabled = true,
                Tag = "DISC"
            }
        };

        user.PrimaryGuild.Should().NotBeNull();
        user.PrimaryGuild!.IsDisplayed.Should().BeTrue();
    }

    // Message Snapshot Tests
    [Fact]
    public void MessageSnapshot_CanStorePartialMessage()
    {
        var snapshot = new MessageSnapshot
        {
            Message = new PartialMessage
            {
                Type = Enums.MessageType.Default,
                Content = "Test message",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        snapshot.Message.Should().NotBeNull();
        snapshot.Message!.Content.Should().Be("Test message");
    }

    [Fact]
    public void PartialMessage_CanHaveEmbeds()
    {
        var partial = new PartialMessage
        {
            Embeds = new List<Embed> { new Embed { Title = "Test Embed" } }
        };

        partial.Embeds.Should().HaveCount(1);
        partial.Embeds![0].Title.Should().Be("Test Embed");
    }

    [Fact]
    public void PartialMessage_CanHaveAttachments()
    {
        var partial = new PartialMessage
        {
            Attachments = new List<Attachment> { new Attachment { Filename = "test.png" } }
        };

        partial.Attachments.Should().HaveCount(1);
        partial.Attachments![0].Filename.Should().Be("test.png");
    }

    [Fact]
    public void PartialMessage_CanHaveEditedTimestamp()
    {
        var editedTime = DateTimeOffset.UtcNow;
        var partial = new PartialMessage
        {
            EditedTimestamp = editedTime
        };

        partial.EditedTimestamp.Should().Be(editedTime);
    }

    [Fact]
    public void PartialMessage_CanHaveFlags()
    {
        var partial = new PartialMessage
        {
            Flags = Enums.MessageFlags.Urgent
        };

        partial.Flags.Should().Be(Enums.MessageFlags.Urgent);
    }

    [Fact]
    public void PartialMessage_CanHaveComponents()
    {
        var partial = new PartialMessage
        {
            Components = new List<MessageComponent> { new ActionRow() }
        };

        partial.Components.Should().HaveCount(1);
    }

    [Fact]
    public void PartialMessage_CanHaveStickerItems()
    {
        var partial = new PartialMessage
        {
            StickerItems = new List<StickerItem> { new StickerItem() }
        };

        partial.StickerItems.Should().HaveCount(1);
    }
}
