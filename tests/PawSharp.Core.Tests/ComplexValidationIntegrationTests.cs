using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using PawSharp.Core.Validation;
using PawSharp.Core.Entities;
using PawSharp.Core.Builders;
using PawSharp.Core.Exceptions;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Tests;

/// <summary>
/// Integration tests for complex validation scenarios that involve multiple components
/// working together (builders, validators, entities).
/// </summary>
public class ComplexValidationIntegrationTests
{
    [Fact]
    public void EmbedBuilder_WithAllFields_ValidatesSuccessfully()
    {
        // Arrange & Act
        var embed = new EmbedBuilder()
            .WithTitle("Test Title")
            .WithDescription("Test Description")
            .WithColor(0x5865F2)
            .WithAuthor("Author Name", "https://example.com", "https://example.com/avatar.png")
            .AddField("Field 1", "Value 1", true)
            .AddField("Field 2", "Value 2", false)
            .AddField("Field 3", "Value 3", true)
            .WithFooter("Footer Text", "https://example.com/icon.png")
            .WithImage("https://example.com/image.png")
            .WithThumbnail("https://example.com/thumb.png")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        // Assert
        embed.Should().NotBeNull();
        embed.Title.Should().Be("Test Title");
        embed.Description.Should().Be("Test Description");
        embed.Fields.Should().HaveCount(3);
    }

    [Fact]
    public void EmbedBuilder_ExceedsFieldLimit_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            var builder = new EmbedBuilder()
                .WithTitle("Test")
                .WithDescription("Description");
            
            // Add 26 fields (max is 25)
            for (int i = 0; i < 26; i++)
            {
                builder.AddField($"Field {i}", $"Value {i}");
            }
            
            builder.Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*field*");
    }

    [Fact]
    public void EmbedBuilder_ExceedsTotalLength_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            var builder = new EmbedBuilder()
                .WithTitle(new string('A', 256)) // Max title
                .WithDescription(new string('B', 4096)); // Max description
            
            // Add fields that push total over 6000
            for (int i = 0; i < 10; i++)
            {
                builder.AddField(new string('C', 256), new string('D', 1024));
            }
            
            builder.Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*length*");
    }

    [Fact]
    public void ComponentBuilder_ComplexNestedComponents_ValidatesSuccessfully()
    {
        // Arrange & Act
        var components = new ComponentBuilder()
            .WithActionRow(row => row
                .AddButton(button => button
                    .WithStyle(ButtonStyle.Primary)
                    .WithLabel("Button 1")
                    .WithCustomId("btn_1"))
                .AddButton(button => button
                    .WithStyle(ButtonStyle.Secondary)
                    .WithLabel("Button 2")
                    .WithCustomId("btn_2"))
                .AddSelectMenu(menu => menu
                    .WithPlaceholder("Choose an option")
                    .WithCustomId("select_1")
                    .AddOption(opt => opt
                        .WithLabel("Option 1")
                        .WithValue("val_1"))
                    .AddOption(opt => opt
                        .WithLabel("Option 2")
                        .WithValue("val_2"))))
            .WithActionRow(row => row
                .AddTextInput(input => input
                    .WithLabel("Enter text")
                    .WithCustomId("text_1")
                    .WithStyle(TextInputStyle.Short)
                    .WithPlaceholder("Type here...")))
            .Build();

        // Assert
        components.Should().HaveCount(2);
        components[0].Should().BeOfType<ActionRow>();
        components[1].Should().BeOfType<ActionRow>();
    }

    [Fact]
    public void ComponentBuilder_ExceedsComponentLimit_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            var builder = new ComponentBuilder()
                .WithActionRow(row => row
                    .AddButton(btn => btn.WithLabel("1").WithCustomId("1").WithStyle(ButtonStyle.Primary))
                    .AddButton(btn => btn.WithLabel("2").WithCustomId("2").WithStyle(ButtonStyle.Primary))
                    .AddButton(btn => btn.WithLabel("3").WithCustomId("3").WithStyle(ButtonStyle.Primary))
                    .AddButton(btn => btn.WithLabel("4").WithCustomId("4").WithStyle(ButtonStyle.Primary))
                    .AddButton(btn => btn.WithLabel("5").WithCustomId("5").WithStyle(ButtonStyle.Primary))
                    .AddButton(btn => btn.WithLabel("6").WithCustomId("6").WithStyle(ButtonStyle.Primary))); // 6 buttons (max is 5)
            
            builder.Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*component*");
    }

    [Fact]
    public void CommandBuilder_WithNestedOptions_ValidatesSuccessfully()
    {
        // Arrange & Act
        var command = new CommandBuilder()
            .WithType(ApplicationCommandType.ChatInput)
            .WithName("test")
            .WithDescription("Test command")
            .AddOption(opt => opt
                .WithType(ApplicationCommandOptionType.SubCommandGroup)
                .WithName("group1")
                .WithDescription("First group")
                .AddOption(subOpt => subOpt
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .WithName("sub1")
                    .WithDescription("First subcommand")
                    .AddChoice(choice => choice.WithName("choice1").WithValue("val1"))))
            .AddOption(opt => opt
                .WithType(ApplicationCommandOptionType.String)
                .WithName("param1")
                .WithDescription("A parameter")
                .WithRequired(true))
            .Build();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("test");
        command.Options.Should().HaveCount(2);
    }

    [Fact]
    public void CommandBuilder_ExceedsOptionLimit_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            var builder = new CommandBuilder()
                .WithName("test")
                .WithDescription("Test command");
            
            // Add 26 options (max is 25)
            for (int i = 0; i < 26; i++)
            {
                builder.AddOption(opt => opt
                    .WithType(ApplicationCommandOptionType.String)
                    .WithName($"param{i}")
                    .WithDescription($"Parameter {i}")
                    .WithRequired(i == 0));
            }
            
            builder.Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*option*");
    }

    [Fact]
    public void CommandBuilder_UserCommand_NoDescriptionRequired_BuildsSuccessfully()
    {
        // Arrange & Act
        var command = new CommandBuilder()
            .WithType(ApplicationCommandType.User)
            .WithName("userinfo")
            .WithDescription("") // Empty description is allowed for User commands
            .Build();

        // Assert
        command.Should().NotBeNull();
        command.Type.Should().Be(ApplicationCommandType.User);
        command.Description.Should().BeEmpty();
    }

    [Fact]
    public void CommandBuilder_ChatInputCommand_EmptyDescription_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            new CommandBuilder()
                .WithType(ApplicationCommandType.ChatInput)
                .WithName("test")
                .WithDescription("") // Empty description is NOT allowed for ChatInput
                .Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*description*");
    }

    [Fact]
    public void ComponentValidator_ComponentsV2_ContainerWithSections_ValidatesSuccessfully()
    {
        // Arrange
        var container = new Container
        {
            Components = new List<MessageComponent>
            {
                new Section
                {
                    Components = new List<MessageComponent>
                    {
                        new TextDisplay { Content = "Section text" }
                    },
                    Accessory = new ThumbnailComponent
                    {
                        Media = new UnfurledMediaItem { Url = "https://example.com/image.png" }
                    }
                },
                new MediaGallery
                {
                    Items = new List<MediaGalleryItem>
                    {
                        new MediaGalleryItem
                        {
                            Media = new UnfurledMediaItem { Url = "https://example.com/media1.png" }
                        },
                        new MediaGalleryItem
                        {
                            Media = new UnfurledMediaItem { Url = "https://example.com/media2.png" }
                        }
                    }
                }
            }
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateComponentHierarchy(container.Components);
        action.Should().NotThrow();
    }

    [Fact]
    public void ComponentValidator_MediaGallery_WithInvalidItemCount_ThrowsValidationException()
    {
        // Arrange
        var gallery = new MediaGallery
        {
            Items = new List<MediaGalleryItem>() // Empty (min is 1)
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateMediaGallery(gallery);
        action.Should().Throw<ValidationException>()
            .WithMessage("*item*");
    }

    [Fact]
    public void CommandValidator_WithChoices_ExceedsLimit_ThrowsValidationException()
    {
        // Arrange
        var option = new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.String,
            Name = "select",
            Description = "Select an option",
            Choices = new List<ApplicationCommandOptionChoice>()
        };

        // Add 26 choices (max is 25)
        for (int i = 0; i < 26; i++)
        {
            option.Choices.Add(new ApplicationCommandOptionChoice
            {
                Name = $"Choice {i}",
                Value = $"val{i}"
            });
        }

        // Act & Assert
        Action action = () => CommandValidator.ValidateCommandOption(option);
        action.Should().Throw<ValidationException>()
            .WithMessage("*choice*");
    }

    [Fact]
    public void UrlValidator_ImageUrl_WithUnsupportedExtension_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () => UrlValidator.ValidateImageUrl("https://example.com/file.pdf");

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*image format*");
    }

    [Fact]
    public void UrlValidator_ImageUrl_WithValidExtension_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Action action = () => UrlValidator.ValidateImageUrl("https://example.com/image.png");
        action.Should().NotThrow();
    }

    [Fact]
    public void CommandOption_MinLengthExceedsMaximum_ThrowsValidationException()
    {
        // Arrange
        var option = new ApplicationCommandOption
        {
            Type = ApplicationCommandOptionType.String,
            Name = "test",
            Description = "Test option",
            MinLength = 6001 // Max is 6000
        };

        // Act & Assert
        Action action = () => CommandValidator.ValidateCommandOption(option);
        action.Should().Throw<ValidationException>()
            .WithMessage("*minimum length*");
    }

    [Fact]
    public void TextInput_ExceedsMaxLength_ThrowsValidationException()
    {
        // Arrange
        var input = new TextInput
        {
            CustomId = "test_input",
            Label = "Test",
            MaxLength = 4001 // Max is 4000
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateTextInput(input);
        action.Should().Throw<ValidationException>()
            .WithMessage("*maximum length*");
    }

    [Fact]
    public void SelectMenu_ExceedsMaxValues_ThrowsValidationException()
    {
        // Arrange
        var menu = new SelectMenu
        {
            CustomId = "test_menu",
            MaxValues = 26 // Max is 25
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateSelectMenu(menu);
        action.Should().Throw<ValidationException>()
            .WithMessage("*maximum values*");
    }

    [Fact]
    public void Button_LinkButtonWithoutUrl_ThrowsValidationException()
    {
        // Arrange
        var button = new Button
        {
            Style = ButtonStyle.Link,
            Label = "Click me",
            Url = null // Link buttons must have URL
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateButton(button);
        action.Should().Throw<ValidationException>()
            .WithMessage("*URL*");
    }

    [Fact]
    public void Button_NonLinkButtonWithoutCustomId_ThrowsValidationException()
    {
        // Arrange
        var button = new Button
        {
            Style = ButtonStyle.Primary,
            Label = "Click me",
            CustomId = null // Non-link buttons must have custom_id
        };

        // Act & Assert
        Action action = () => ComponentValidator.ValidateButton(button);
        action.Should().Throw<ValidationException>()
            .WithMessage("*custom ID*");
    }

    [Fact]
    public void EmbedBuilder_WithoutContent_ThrowsValidationException()
    {
        // Arrange & Act
        Action action = () =>
        {
            new EmbedBuilder()
                .WithTitle("") // Empty
                .WithDescription("") // Empty
                .Build();
        };

        // Assert
        action.Should().Throw<ValidationException>()
            .WithMessage("*content*");
    }
}
