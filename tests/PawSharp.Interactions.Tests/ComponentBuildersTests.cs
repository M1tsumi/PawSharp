#nullable enable
using System;
using FluentAssertions;
using PawSharp.Core.Entities;
using PawSharp.Interactions.Builders;
using Xunit;

namespace PawSharp.Interactions.Tests;

public class ComponentBuildersTests
{
    [Fact]
    public void ButtonBuilder_BuildsButton()
    {
        var btn = new ButtonBuilder("click", "Click me", ButtonStyle.Success)
            .SetDisabled(true)
            .SetEmoji("🔥")
            .Build();

        btn.CustomId.Should().Be("click");
        btn.Label.Should().Be("Click me");
        btn.Style.Should().Be(ButtonStyle.Success);
        btn.Disabled.Should().BeTrue();
        btn.Emoji!.Name.Should().Be("🔥");
    }

    [Fact]
    public void ButtonBuilder_SetUrl_SetsLinkStyle()
    {
        var btn = new ButtonBuilder("id", "label")
            .SetUrl("https://example.com")
            .Build();

        btn.Style.Should().Be(ButtonStyle.Link);
        btn.Url.Should().Be("https://example.com");
        btn.CustomId.Should().BeNull();
    }

    [Fact]
    public void ButtonBuilder_SetSkuId_SetsPremiumStyle()
    {
        var btn = new ButtonBuilder("id", "label")
            .SetSkuId(12345UL)
            .Build();

        btn.Style.Should().Be(ButtonStyle.Premium);
        btn.SkuId.Should().Be(12345UL);
    }

    [Fact]
    public void ButtonBuilder_SetCustomEmoji_SetsEmoji()
    {
        var btn = new ButtonBuilder("id", "label")
            .SetCustomEmoji("paws", 123UL, true)
            .Build();

        btn.Emoji!.Name.Should().Be("paws");
        btn.Emoji.Id.Should().Be(123UL);
        btn.Emoji.Animated.Should().BeTrue();
    }

    [Fact]
    public void SelectMenuBuilder_BuildsSelectMenu()
    {
        var menu = new SelectMenuBuilder("colors", "Pick a color")
            .AddOption("Red", "red", "The red option")
            .AddOption("Blue", "blue", isDefault: true)
            .SetMinValues(1)
            .SetMaxValues(2)
            .SetDisabled(false)
            .Build();

        menu.CustomId.Should().Be("colors");
        menu.Placeholder.Should().Be("Pick a color");
        menu.Options.Should().HaveCount(2);
        menu.MinValues.Should().Be(1);
        menu.MaxValues.Should().Be(2);
        menu.Options[1].Default.Should().BeTrue();
    }

    [Fact]
    public void UserSelectMenuBuilder_Builds()
    {
        var menu = new UserSelectMenuBuilder("users", "Select user")
            .SetMinValues(1)
            .SetMaxValues(5)
            .SetDisabled(true)
            .Build();

        menu.CustomId.Should().Be("users");
        menu.MinValues.Should().Be(1);
        menu.MaxValues.Should().Be(5);
        menu.Disabled.Should().BeTrue();
    }

    [Fact]
    public void RoleSelectMenuBuilder_Builds()
    {
        var menu = new RoleSelectMenuBuilder("roles", "Select role")
            .SetMinValues(1)
            .Build();

        menu.CustomId.Should().Be("roles");
        menu.MinValues.Should().Be(1);
    }

    [Fact]
    public void MentionableSelectMenuBuilder_Builds()
    {
        var menu = new MentionableSelectMenuBuilder("mentions")
            .SetMaxValues(3)
            .Build();

        menu.CustomId.Should().Be("mentions");
        menu.MaxValues.Should().Be(3);
    }

    [Fact]
    public void ChannelSelectMenuBuilder_Builds()
    {
        var menu = new ChannelSelectMenuBuilder("channels")
            .SetChannelTypes(0, 2)
            .SetMinValues(1)
            .Build();

        menu.CustomId.Should().Be("channels");
        menu.ChannelTypes.Should().BeEquivalentTo(new[] { 0, 2 });
        menu.MinValues.Should().Be(1);
    }

    [Fact]
    public void ActionRowBuilder_AddsComponents()
    {
        var row = new ActionRowBuilder()
            .AddButton(new ButtonBuilder("b1", "B1"))
            .AddSelectMenu(new SelectMenuBuilder("menu", "Pick"))
            .Build();

        row.Components.Should().HaveCount(2);
    }

    [Fact]
    public void ActionRowBuilder_Max5Components_Throws()
    {
        var builder = new ActionRowBuilder();
        for (int i = 0; i < 5; i++)
            builder.AddComponent(new Button { CustomId = $"b{i}" });

        Action act = () => builder.AddComponent(new Button());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TextDisplayBuilder_Builds()
    {
        var td = new TextDisplayBuilder("Hello")
            .SetContent("World")
            .Build();

        td.Content.Should().Be("World");
    }

    [Fact]
    public void SeparatorBuilder_Builds()
    {
        var sep = new SeparatorBuilder()
            .SetDivider(true)
            .SetSpacing(SeparatorSpacing.Small)
            .Build();

        sep.Divider.Should().BeTrue();
        sep.Spacing.Should().Be(SeparatorSpacing.Small);
    }

    [Fact]
    public void MediaGalleryBuilder_Builds()
    {
        var gallery = new MediaGalleryBuilder()
            .AddItem("https://example.com/img.png", "An image")
            .AddItem("https://example.com/img2.png", spoiler: true)
            .Build();

        gallery.Items.Should().HaveCount(2);
        gallery.Items[0].Media.Url.Should().Be("https://example.com/img.png");
        gallery.Items[0].Description.Should().Be("An image");
        gallery.Items[1].Spoiler.Should().BeTrue();
    }

    [Fact]
    public void SectionBuilder_Builds()
    {
        var section = new SectionBuilder()
            .AddText("Hello")
            .SetThumbnailAccessory("https://example.com/thumb.png")
            .Build();

        section.Components.Should().HaveCount(1);
        section.Accessory.Should().NotBeNull();
    }

    [Fact]
    public void ThumbnailBuilder_Builds()
    {
        var thumb = new ThumbnailBuilder("https://example.com/img.png")
            .SetDescription("description")
            .SetSpoiler(true)
            .Build();

        thumb.Media.Url.Should().Be("https://example.com/img.png");
        thumb.Description.Should().Be("description");
        thumb.Spoiler.Should().BeTrue();
    }

    [Fact]
    public void FileBuilder_Builds()
    {
        var file = new FileBuilder("attachment://test.png")
            .SetSpoiler(true)
            .Build();

        file.File.Url.Should().Be("attachment://test.png");
        file.Spoiler.Should().BeTrue();
    }

    [Fact]
    public void ContainerBuilder_Builds()
    {
        var container = new ContainerBuilder()
            .AddTextDisplay("Hello")
            .AddSeparator()
            .AddFile("attachment://file.png")
            .SetAccentColor(0xFF0000)
            .SetSpoiler(true)
            .Build();

        container.Components.Should().HaveCount(3);
        container.AccentColor.Should().Be(0xFF0000);
        container.Spoiler.Should().BeTrue();
    }

    [Fact]
    public void LabelBuilder_Builds()
    {
        var label = new LabelBuilder("Hello")
            .SetEmoji("🔥")
            .Build();

        label.Text.Should().Be("Hello");
        label.Emoji!.Name.Should().Be("🔥");
    }

    [Fact]
    public void LabelBuilder_SetCustomEmoji()
    {
        var label = new LabelBuilder("Test")
            .SetCustomEmoji("paws", 1UL)
            .Build();

        label.Emoji!.Name.Should().Be("paws");
        label.Emoji.Id.Should().Be(1UL);
    }

    [Fact]
    public void FileUploadBuilder_Builds()
    {
        var upload = new FileUploadBuilder("upload", "Upload a file")
            .SetRequired(true)
            .SetPlaceholder("Select file")
            .SetFileTypes(".png", ".jpg")
            .Build();

        upload.CustomId.Should().Be("upload");
        upload.Required.Should().BeTrue();
        upload.FileTypes.Should().BeEquivalentTo(".png", ".jpg");
    }

    [Fact]
    public void RadioGroupBuilder_Builds()
    {
        var group = new RadioGroupBuilder("choice", "Choose one")
            .AddOption("A", "a", "Option A")
            .AddOption("B", "b", isDefault: true)
            .SetRequired(true)
            .Build();

        group.Options.Should().HaveCount(2);
        group.Options[1].Default.Should().BeTrue();
    }

    [Fact]
    public void CheckboxGroupBuilder_Builds()
    {
        var group = new CheckboxGroupBuilder("choices", "Choose")
            .AddOption("X", "x")
            .AddOption("Y", "y", isDefault: true)
            .SetMinValues(1)
            .SetMaxValues(2)
            .Build();

        group.Options.Should().HaveCount(2);
        group.MinValues.Should().Be(1);
        group.MaxValues.Should().Be(2);
    }

    [Fact]
    public void CheckboxBuilder_Builds()
    {
        var cb = new CheckboxBuilder("agree", "I agree")
            .SetDefaultValue(true)
            .SetRequired(false)
            .Build();

        cb.CustomId.Should().Be("agree");
        cb.DefaultValue.Should().BeTrue();
        cb.Required.Should().BeFalse();
    }
}
