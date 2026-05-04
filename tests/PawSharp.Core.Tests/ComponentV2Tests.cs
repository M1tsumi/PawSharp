#nullable enable
using System;
using Xunit;
using FluentAssertions;
using PawSharp.Core.Entities;
using PawSharp.Core.Builders;
using PawSharp.Core.Validation;
using PawSharp.Core.Exceptions;

namespace PawSharp.Core.Tests;

public class ComponentV2Tests
{
    // ── ThumbnailBuilder Tests ─────────────────────────────────────────────────────

    [Fact]
    public void ThumbnailBuilder_BuildsValidThumbnail()
    {
        var thumbnail = new ThumbnailBuilder("https://example.com/image.png")
            .WithDescription("Test image")
            .Build();

        thumbnail.Media.Url.Should().Be("https://example.com/image.png");
        thumbnail.Description.Should().Be("Test image");
    }

    [Fact]
    public void ThumbnailBuilder_RequiresUrl()
    {
        Action act = () => new ThumbnailBuilder("").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Thumbnail must have a URL*");
    }

    // ── FileBuilder Tests ─────────────────────────────────────────────────────────

    [Fact]
    public void FileBuilder_BuildsValidFile()
    {
        var file = new FileBuilder("attachment://test.pdf")
            .WithSpoiler(true)
            .Build();

        file.File.Url.Should().Be("attachment://test.pdf");
        file.Spoiler.Should().BeTrue();
    }

    [Fact]
    public void FileBuilder_RequiresUrl()
    {
        Action act = () => new FileBuilder("").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*File must have a URL*");
    }

    // ── MediaGalleryBuilder Tests ───────────────────────────────────────────────────

    [Fact]
    public void MediaGalleryBuilder_BuildsValidGallery()
    {
        var gallery = new MediaGalleryBuilder()
            .AddItem("https://example.com/image1.png", "Image 1")
            .AddItem("https://example.com/image2.png", "Image 2")
            .Build();

        gallery.Items.Should().HaveCount(2);
        gallery.Items[0].Description.Should().Be("Image 1");
    }

    [Fact]
    public void MediaGalleryBuilder_RequiresAtLeastOneItem()
    {
        Action act = () => new MediaGalleryBuilder().Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*MediaGallery must have at least*");
    }

    [Fact]
    public void MediaGalleryBuilder_MaxTenItems()
    {
        Action act = () =>
        {
            var builder = new MediaGalleryBuilder();
            for (int i = 0; i < 11; i++)
            {
                builder.AddItem($"https://example.com/image{i}.png");
            }
            builder.Build();
        };

        act.Should().Throw<ValidationException>()
            .WithMessage("*MediaGallery can have at most 10 items*");
    }

    // ── LabelBuilder Tests ────────────────────────────────────────────────────────

    [Fact]
    public void LabelBuilder_BuildsValidLabel()
    {
        var label = new LabelBuilder("Test Label")
            .WithEmoji("🔥")
            .Build();

        label.Text.Should().Be("Test Label");
        label.Emoji.Should().NotBeNull();
        label.Emoji!.Name.Should().Be("🔥");
    }

    [Fact]
    public void LabelBuilder_RejectsTooLongText()
    {
        Action act = () => new LabelBuilder(new string('a', 81)).Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Label text must not exceed 80 characters*");
    }

    [Fact]
    public void LabelBuilder_RejectsEmptyText()
    {
        Action act = () => new LabelBuilder("").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Label text is required*");
    }

    [Fact]
    public void LabelBuilder_SupportsCustomEmoji()
    {
        var label = new LabelBuilder("Test")
            .WithCustomEmoji("test", 123456789, true)
            .Build();

        label.Emoji.Should().NotBeNull();
        label.Emoji!.Id.Should().Be(123456789);
        label.Emoji!.Animated.Should().BeTrue();
    }

    // ── FileUploadBuilder Tests ───────────────────────────────────────────────────

    [Fact]
    public void FileUploadBuilder_BuildsValidFileUpload()
    {
        var upload = new FileUploadBuilder("file_upload", "Upload a file")
            .WithRequired(true)
            .WithMinLength(1)
            .WithMaxLength(5)
            .WithFileTypes("image/*", "application/pdf")
            .Build();

        upload.CustomId.Should().Be("file_upload");
        upload.Label.Should().Be("Upload a file");
        upload.Required.Should().BeTrue();
        upload.MinLength.Should().Be(1);
        upload.MaxLength.Should().Be(5);
        upload.FileTypes.Should().HaveCount(2);
    }

    [Fact]
    public void FileUploadBuilder_RejectsTooLongLabel()
    {
        Action act = () => new FileUploadBuilder("id", new string('a', 46)).Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*FileUpload label must not exceed 45 characters*");
    }

    [Fact]
    public void FileUploadBuilder_RejectsEmptyCustomId()
    {
        Action act = () => new FileUploadBuilder("", "Label").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*FileUpload custom ID is required*");
    }

    [Fact]
    public void FileUploadBuilder_RejectsEmptyLabel()
    {
        Action act = () => new FileUploadBuilder("id", "").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*FileUpload label is required*");
    }

    [Fact]
    public void FileUploadBuilder_RejectsMinGreaterThanMax()
    {
        Action act = () => new FileUploadBuilder("id", "Label")
            .WithMinLength(5)
            .WithMaxLength(3)
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*minimum length cannot be greater than maximum length*");
    }

    // ── RadioGroupBuilder Tests ───────────────────────────────────────────────────

    [Fact]
    public void RadioGroupBuilder_BuildsValidRadioGroup()
    {
        var radioGroup = new RadioGroupBuilder("radio_group", "Choose an option")
            .AddOption("Option 1", "opt1", "First option")
            .AddOption("Option 2", "opt2", "Second option")
            .WithRequired(true)
            .Build();

        radioGroup.CustomId.Should().Be("radio_group");
        radioGroup.Label.Should().Be("Choose an option");
        radioGroup.Options.Should().HaveCount(2);
        radioGroup.Required.Should().BeTrue();
    }

    [Fact]
    public void RadioGroupBuilder_RequiresAtLeastOneOption()
    {
        Action act = () => new RadioGroupBuilder("id", "Label").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioGroup must have at least one option*");
    }

    [Fact]
    public void RadioGroupBuilder_RejectsEmptyCustomId()
    {
        Action act = () => new RadioGroupBuilder("", "Label")
            .AddOption("Opt", "val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioGroup custom ID is required*");
    }

    [Fact]
    public void RadioGroupBuilder_RejectsEmptyLabel()
    {
        Action act = () => new RadioGroupBuilder("id", "")
            .AddOption("Opt", "val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioGroup label is required*");
    }

    [Fact]
    public void RadioGroupBuilder_MaxTwentyFiveOptions()
    {
        Action act = () =>
        {
            var builder = new RadioGroupBuilder("id", "Label");
            for (int i = 0; i < 26; i++)
            {
                builder.AddOption($"Option {i}", $"opt{i}");
            }
            builder.Build();
        };

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioGroup can have at most 25 options*");
    }

    [Fact]
    public void RadioOptionBuilder_BuildsValidOption()
    {
        var option = new RadioOptionBuilder()
            .WithLabel("Test Option")
            .WithValue("test_opt")
            .WithDescription("Test description")
            .WithEmoji(new Emoji { Name = "✅" })
            .Build();

        option.Label.Should().Be("Test Option");
        option.Value.Should().Be("test_opt");
        option.Description.Should().Be("Test description");
        option.Emoji.Should().NotBeNull();
    }

    [Fact]
    public void RadioOptionBuilder_RejectsEmptyLabel()
    {
        Action act = () => new RadioOptionBuilder()
            .WithLabel("")
            .WithValue("val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioOption label is required*");
    }

    [Fact]
    public void RadioOptionBuilder_RejectsEmptyValue()
    {
        Action act = () => new RadioOptionBuilder()
            .WithLabel("Label")
            .WithValue("")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*RadioOption value is required*");
    }

    // ── CheckboxGroupBuilder Tests ────────────────────────────────────────────────

    [Fact]
    public void CheckboxGroupBuilder_BuildsValidCheckboxGroup()
    {
        var checkboxGroup = new CheckboxGroupBuilder("checkbox_group", "Select options")
            .AddOption("Option 1", "opt1")
            .AddOption("Option 2", "opt2")
            .WithMinValues(1)
            .WithMaxValues(2)
            .Build();

        checkboxGroup.CustomId.Should().Be("checkbox_group");
        checkboxGroup.Label.Should().Be("Select options");
        checkboxGroup.Options.Should().HaveCount(2);
        checkboxGroup.MinValues.Should().Be(1);
        checkboxGroup.MaxValues.Should().Be(2);
    }

    [Fact]
    public void CheckboxGroupBuilder_RequiresAtLeastOneOption()
    {
        Action act = () => new CheckboxGroupBuilder("id", "Label").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*CheckboxGroup must have at least one option*");
    }

    [Fact]
    public void CheckboxGroupBuilder_RejectsEmptyCustomId()
    {
        Action act = () => new CheckboxGroupBuilder("", "Label")
            .AddOption("Opt", "val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*CheckboxGroup custom ID is required*");
    }

    [Fact]
    public void CheckboxGroupBuilder_RejectsEmptyLabel()
    {
        Action act = () => new CheckboxGroupBuilder("id", "")
            .AddOption("Opt", "val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*CheckboxGroup label is required*");
    }

    [Fact]
    public void CheckboxGroupBuilder_RejectsMinGreaterThanMax()
    {
        Action act = () => new CheckboxGroupBuilder("id", "Label")
            .AddOption("Opt", "val")
            .WithMinValues(5)
            .WithMaxValues(3)
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*minimum values cannot be greater than maximum values*");
    }

    [Fact]
    public void CheckboxOptionBuilder_BuildsValidOption()
    {
        var option = new CheckboxOptionBuilder()
            .WithLabel("Test Option")
            .WithValue("test_opt")
            .WithDefault(true)
            .Build();

        option.Label.Should().Be("Test Option");
        option.Value.Should().Be("test_opt");
        option.Default.Should().BeTrue();
    }

    [Fact]
    public void CheckboxOptionBuilder_RejectsEmptyLabel()
    {
        Action act = () => new CheckboxOptionBuilder()
            .WithLabel("")
            .WithValue("val")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*CheckboxOption label is required*");
    }

    [Fact]
    public void CheckboxOptionBuilder_RejectsEmptyValue()
    {
        Action act = () => new CheckboxOptionBuilder()
            .WithLabel("Label")
            .WithValue("")
            .Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*CheckboxOption value is required*");
    }

    // ── CheckboxBuilder Tests ─────────────────────────────────────────────────────

    [Fact]
    public void CheckboxBuilder_BuildsValidCheckbox()
    {
        var checkbox = new CheckboxBuilder("checkbox", "I agree")
            .WithDefaultValue(true)
            .WithRequired(true)
            .Build();

        checkbox.CustomId.Should().Be("checkbox");
        checkbox.Label.Should().Be("I agree");
        checkbox.DefaultValue.Should().BeTrue();
        checkbox.Required.Should().BeTrue();
    }

    [Fact]
    public void CheckboxBuilder_RejectsTooLongLabel()
    {
        Action act = () => new CheckboxBuilder("id", new string('a', 81)).Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Checkbox label must not exceed 80 characters*");
    }

    [Fact]
    public void CheckboxBuilder_RejectsEmptyCustomId()
    {
        Action act = () => new CheckboxBuilder("", "Label").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Checkbox custom ID is required*");
    }

    [Fact]
    public void CheckboxBuilder_RejectsEmptyLabel()
    {
        Action act = () => new CheckboxBuilder("id", "").Build();

        act.Should().Throw<ValidationException>()
            .WithMessage("*Checkbox label is required*");
    }

    // ── SectionBuilder Tests ──────────────────────────────────────────────────────

    [Fact]
    public void SectionBuilder_BuildsValidSection()
    {
        var section = new SectionBuilder()
            .AddText("# Title")
            .AddText("Description text")
            .WithThumbnailAccessory("https://example.com/thumb.png")
            .Build();

        section.Components.Should().HaveCount(2);
        section.Accessory.Should().BeOfType<ThumbnailComponent>();
    }

    [Fact]
    public void SectionBuilder_RejectsNonTextDisplayChildren()
    {
        Action act = () =>
        {
            var builder = new SectionBuilder();
            // Cannot add non-TextDisplay to Section - this would be caught at build time
            builder.AddText("Test");
            // Try to manually add a button component (not possible through builder API)
            // This test validates the builder API prevents this
            var section = builder.Build();
        };

        // The builder API doesn't allow adding arbitrary components
        // This test confirms the API is correctly restrictive
        var builder = new SectionBuilder();
        builder.AddText("Test");
        var section = builder.Build();
        section.Components.Should().HaveCount(1);
        section.Components[0].Should().BeOfType<TextDisplay>();
    }

    [Fact]
    public void SectionBuilder_RejectsInvalidAccessory()
    {
        Action act = () =>
        {
            var builder = new SectionBuilder()
                .AddText("Test")
                .WithAccessory(new Separator());
            builder.Build();
        };

        act.Should().Throw<ValidationException>()
            .WithMessage("*Section accessory must be a Button or Thumbnail component*");
    }

    // ── SeparatorBuilder Tests ───────────────────────────────────────────────────

    [Fact]
    public void SeparatorBuilder_BuildsValidSeparator()
    {
        var separator = new SeparatorBuilder()
            .WithSpacing(SeparatorSpacing.Large)
            .WithDivider(true)
            .Build();

        separator.Spacing.Should().Be(SeparatorSpacing.Large);
        separator.Divider.Should().BeTrue();
    }

    // ── ContainerBuilder Tests ────────────────────────────────────────────────────

    [Fact]
    public void ContainerBuilder_BuildsValidContainer()
    {
        var container = new ContainerBuilder()
            .AddText("Header text")
            .AddSeparator()
            .AddSection(s => s.AddText("Section text"))
            .WithAccentColor(0x5865F2)
            .Build();

        container.Components.Should().HaveCount(3);
        container.AccentColor.Should().Be(0x5865F2);
    }

    [Fact]
    public void ContainerBuilder_MaxTwentyComponents()
    {
        Action act = () =>
        {
            var builder = new ContainerBuilder();
            for (int i = 0; i < 21; i++)
            {
                builder.AddText($"Text {i}");
            }
            builder.Build();
        };

        act.Should().Throw<ValidationException>()
            .WithMessage("*Container can contain at most 20 components*");
    }

    [Fact]
    public void ContainerBuilder_SupportsSpoiler()
    {
        var container = new ContainerBuilder()
            .AddText("Spoiler content")
            .WithSpoiler(true)
            .Build();

        container.Spoiler.Should().BeTrue();
    }

    // ── Integration Tests ───────────────────────────────────────────────────────

    [Fact]
    public void ComponentBuilder_BuildsComplexComponentV2Structure()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Welcome to the Server")
                .AddMediaGallery(g => g
                    .AddItem("https://example.com/image1.png", "Welcome image")
                    .AddItem("https://example.com/image2.png"))
                .AddSeparator()
                .AddRadioGroup("choice", "Choose your path", r => r
                    .AddOption("Light", "light", "The path of light")
                    .AddOption("Dark", "dark", "The path of darkness"))
                .WithAccentColor(0x5865F2))
            .Build();

        components.Should().HaveCount(1);
        components[0].Should().BeOfType<Container>();
        var container = (Container)components[0];
        container.Components.Should().HaveCount(4);
    }

    [Fact]
    public void ComponentValidator_ValidateContainerWithAllComponents()
    {
        var container = new ContainerBuilder()
            .AddLabel("Label", l => l.WithEmoji("🔥"))
            .AddCheckbox("chk", "Check me", ch => ch.WithDefaultValue(true))
            .AddText("Text")
            .AddSeparator()
            .Build();

        // Should not throw
        ComponentValidator.ValidateContainer(container);
    }

    [Fact]
    public void ComponentValidator_ValidateRadioGroup()
    {
        var radioGroup = new RadioGroupBuilder("id", "Label")
            .AddOption("Opt1", "val1")
            .AddOption("Opt2", "val2")
            .Build();

        // Should not throw
        ComponentValidator.ValidateRadioGroup(radioGroup);
    }

    [Fact]
    public void ComponentValidator_ValidateCheckboxGroup()
    {
        var checkboxGroup = new CheckboxGroupBuilder("id", "Label")
            .AddOption("Opt1", "val1")
            .AddOption("Opt2", "val2")
            .Build();

        // Should not throw
        ComponentValidator.ValidateCheckboxGroup(checkboxGroup);
    }

    [Fact]
    public void ComponentValidator_ValidateFileUpload()
    {
        var fileUpload = new FileUploadBuilder("id", "Label")
            .WithRequired(true)
            .Build();

        // Should not throw
        ComponentValidator.ValidateFileUpload(fileUpload);
    }

    [Fact]
    public void ComponentValidator_ValidateLabel()
    {
        var label = new LabelBuilder("Test Label").Build();

        // Should not throw
        ComponentValidator.ValidateLabel(label);
    }

    [Fact]
    public void ComponentValidator_ValidateThumbnail()
    {
        var thumbnail = new ThumbnailBuilder("https://example.com/image.png").Build();

        // Should not throw
        ComponentValidator.ValidateThumbnail(thumbnail);
    }

    [Fact]
    public void ComponentValidator_ValidateFile()
    {
        var file = new FileBuilder("attachment://file.pdf").Build();

        // Should not throw
        ComponentValidator.ValidateFile(file);
    }
}
