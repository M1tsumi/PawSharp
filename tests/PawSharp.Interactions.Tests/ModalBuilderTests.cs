#nullable enable
using FluentAssertions;
using PawSharp.Interactions.Builders;
using Xunit;

namespace PawSharp.Interactions.Tests;

public class ModalBuilderTests
{
    [Fact]
    public void BuildResponse_ReturnsModalResponse()
    {
        var response = new ModalBuilder()
            .WithCustomId("feedback")
            .WithTitle("Feedback Form")
            .AddTextInput("Your name", "name", required: true)
            .BuildResponse();

        response.Type.Should().Be(9);
        response.Data.Should().NotBeNull();
        response.Data!.CustomId.Should().Be("feedback");
        response.Data.Title.Should().Be("Feedback Form");
    }

    [Fact]
    public void Build_ReturnsCallbackData()
    {
        var data = new ModalBuilder()
            .WithCustomId("form")
            .WithTitle("Form")
            .AddTextInput("Field 1", "field1")
            .AddTextInput("Field 2", "field2", style: Core.Entities.TextInputStyle.Paragraph, required: false)
            .Build();

        data.CustomId.Should().Be("form");
        data.Components.Should().HaveCount(2);
    }

    [Fact]
    public void AddTextInput_SetsProperties()
    {
        var data = new ModalBuilder()
            .WithCustomId("test")
            .WithTitle("Test")
            .AddTextInput("Label", "cid", Core.Entities.TextInputStyle.Paragraph, false, "placeholder", 1, 100)
            .Build();

        data.Components.Should().HaveCount(1);
    }
}
