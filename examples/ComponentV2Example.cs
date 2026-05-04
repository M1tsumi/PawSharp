#nullable enable
using PawSharp.Core.Entities;
using PawSharp.Core.Builders;

namespace PawSharp.Examples;

/// <summary>
/// Examples demonstrating the Componentv2 builder API with fluent ergonomics.
/// Componentv2 is Discord's new component system released in 2025, enabling richer message layouts.
/// </summary>
public class ComponentV2Example
{
    /// <summary>
    /// Creates a simple Container with text content and accent color.
    /// </summary>
    public static List<MessageComponent> SimpleContainer()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Welcome to the Server!")
                .AddText("This is a Componentv2 message with an accent color.")
                .WithAccentColor(0x5865F2)) // Discord blurple
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a rich Container with media gallery, sections, and interactive elements.
    /// </summary>
    public static List<MessageComponent> RichContainer()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Game Update v7.3")
                .AddMediaGallery(g => g
                    .AddItem("https://example.com/update-preview.png", "Update preview image")
                    .AddItem("https://example.com/new-feature.png", "New feature screenshot"))
                .AddSeparator()
                .AddSection(s => s
                    .AddText("## What's New")
                    .AddText("- Fixed treasure chest bugs\n- Improved server stability\n- Added gravity mechanics")
                    .WithButtonAccessory(b => b
                        .WithLabel("Read Full Notes")
                        .WithStyle(ButtonStyle.Link)
                        .WithUrl("https://example.com/notes")))
                .AddSeparator()
                .AddRadioGroup(r => r
                    .WithCustomId("feedback_type")
                    .WithLabel("What do you think?")
                    .AddOption("Love it!", "love", "I really enjoy this update")
                    .AddOption("It's okay", "okay", "Some good, some bad")
                    .AddOption("Not great", "bad", "Needs improvement"))
                .WithAccentColor(0x57F287)) // Discord green
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a modal with Componentv2 form elements (FileUpload, CheckboxGroup, etc.).
    /// </summary>
    public static List<MessageComponent> ModalForm()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddLabel("Bug Report", l => l
                    .WithEmoji("🐛"))
                .AddFileUpload("screenshot", "Upload Screenshot", f => f
                    .WithPlaceholder("Attach an image showing the bug")
                    .WithRequired(true)
                    .WithMinLength(1)
                    .WithMaxLength(3)
                    .WithFileTypes("image/*"))
                .AddCheckboxGroup("severity", "Severity Level", cb => cb
                    .AddOption("Critical", "critical", "Breaks core functionality")
                    .AddOption("Major", "major", "Significant impact")
                    .AddOption("Minor", "minor", "Cosmetic or small issue")
                    .WithMinValues(1)
                    .WithMaxValues(1))
                .AddCheckbox("reproducible", "I can reproduce this bug consistently", ch => ch
                    .WithDefaultValue(true)))
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a Section with a thumbnail accessory.
    /// </summary>
    public static List<MessageComponent> SectionWithThumbnail()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddSection(s => s
                    .AddText("# User Profile")
                    .AddText("**Level:** 42")
                    .AddText("**XP:** 15,420 / 20,000")
                    .WithThumbnailAccessory("https://example.com/avatar.png", "User avatar"))
                .WithAccentColor(0xED4245)) // Discord red
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a Container with a File component.
    /// </summary>
    public static List<MessageComponent> FileAttachment()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Document Download")
                .AddFile("attachment://document.pdf", f => f
                    .WithSpoiler(false))
                .AddSeparator()
                .AddText("Click the button below to download."))
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a complex form with multiple Componentv2 interactive elements.
    /// </summary>
    public static List<MessageComponent> ComplexForm()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Server Settings Configuration")
                .AddSeparator()
                .AddRadioGroup("verification_level", "Verification Level", r => r
                    .AddOption("None", "none", "No verification required")
                    .AddOption("Low", "low", "Must have verified email")
                    .AddOption("Medium", "medium", "Must be registered for 5 minutes")
                    .AddOption("High", "high", "Must be a member for 10 minutes")
                    .AddOption("Highest", "highest", "Must have verified phone"))
                .AddSeparator()
                .AddCheckboxGroup("features", "Enable Features", cb => cb
                    .AddOption("Welcome Messages", "welcome", "Send welcome message to new members")
                    .AddOption("Auto-moderation", "automod", "Enable automatic moderation")
                    .AddOption("Leveling System", "leveling", "Enable XP and levels")
                    .WithMinValues(0)
                    .WithMaxValues(3))
                .AddSeparator()
                .AddCheckbox("agree_rules", "I have read and agree to the server rules", ch => ch
                    .WithRequired(true))
                .WithAccentColor(0x5865F2))
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a Container with nested ActionRows for interactive buttons.
    /// </summary>
    public static List<MessageComponent> ContainerWithActionRows()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# What would you like to do?")
                .AddActionRow(ar => ar
                    .AddPrimaryButton("Create", "create")
                    .AddSecondaryButton("Edit", "edit")
                    .AddSuccessButton("Save", "save")
                    .AddDangerButton("Delete", "delete"))
                .AddSeparator()
                .AddText("Or select from the options below:")
                .AddActionRow(ar => ar
                    .AddStringSelect(s => s
                        .WithCustomId("options")
                        .WithPlaceholder("Choose an option...")
                        .AddOption("View Profile", "profile")
                        .AddOption("Settings", "settings")
                        .AddOption("Help", "help")))
                .WithAccentColor(0xFEE75C)) // Discord yellow
            .Build();

        return components;
    }

    /// <summary>
    /// Demonstrates error handling with Componentv2 builders.
    /// </summary>
    public static void ErrorHandlingExample()
    {
        try
        {
            // This will throw because the label is too long
            var label = new LabelBuilder(new string('a', 81)).Build();
        }
        catch (PawSharp.Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
            Console.WriteLine($"Parameter: {ex.ParameterName}");
            Console.WriteLine($"Value: {ex.Value}");
        }

        try
        {
            // This will throw because RadioGroup has no options
            var radioGroup = new RadioGroupBuilder("id", "Label").Build();
        }
        catch (PawSharp.Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }

        try
        {
            // This will throw because Container has too many components
            var container = new ContainerBuilder();
            for (int i = 0; i < 21; i++)
            {
                container.AddText($"Text {i}");
            }
            container.Build();
        }
        catch (PawSharp.Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"Validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates the fluent builder API with method chaining.
    /// </summary>
    public static List<MessageComponent> FluentApiExample()
    {
        // Method chaining provides excellent ergonomics
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .WithAccentColor(0x5865F2)
                .WithSpoiler(false)
                .AddText("# Welcome")
                .AddSeparator(s => s.WithSpacing(SeparatorSpacing.Large).WithDivider(true))
                .AddSection(s => s
                    .AddText("## Information")
                    .AddText("Details go here")
                    .WithThumbnailAccessory(t => t
                        .WithUrl("https://example.com/image.png")
                        .WithDescription("Thumbnail")
                        .WithSpoiler(false)))
                .AddRadioGroup("choice", "Choose", r => r
                    .WithRequired(true)
                    .AddOption("A", "a")
                    .AddOption("B", "b")))
            .Build();

        return components;
    }

    /// <summary>
    /// Creates a media-rich message with MediaGallery and File components.
    /// </summary>
    public static List<MessageComponent> MediaRichMessage()
    {
        var components = new ComponentBuilder()
            .AddContainer(c => c
                .AddText("# Photo Gallery")
                .AddMediaGallery(g => g
                    .AddItem("https://example.com/photo1.jpg", "Sunset at the beach", spoiler: false)
                    .AddItem("https://example.com/photo2.jpg", "Mountain view")
                    .AddItem("https://example.com/photo3.jpg", "City lights", spoiler: false)
                    .AddItem("https://example.com/photo4.jpg", "Forest path", spoiler: false))
                .AddSeparator()
                .AddFile("attachment://album.zip", f => f
                    .WithSpoiler(false))
                .AddText("Download the full album as a ZIP file."))
            .Build();

        return components;
    }
}
