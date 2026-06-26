#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using PawSharp.Gateway.Events;
using PawSharp.Interactions.Extensions;
using Xunit;
using InteractionOption = PawSharp.Gateway.Events.ApplicationCommandInteractionDataOption;

namespace PawSharp.Interactions.Tests;

public class InteractionExtensionsTests
{
    [Fact]
    public void GetOptionValue_NullData_ReturnsDefault()
    {
        var interaction = new InteractionCreateEvent();
        interaction.GetOptionValue<string>("name").Should().BeNull();
    }

    [Fact]
    public void GetOptionValue_FindsStringOption()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "name", Value = JsonSerializer.SerializeToElement("test") }
                }
            }
        };

        interaction.GetOptionValue<string>("name").Should().Be("test");
    }

    [Fact]
    public void GetOptionValue_NotFound_ReturnsDefault()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "other", Value = JsonSerializer.SerializeToElement("val") }
                }
            }
        };

        interaction.GetOptionValue<string>("name").Should().BeNull();
    }

    [Fact]
    public void GetOptionValue_CaseInsensitive()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "Name", Value = JsonSerializer.SerializeToElement("test") }
                }
            }
        };

        interaction.GetOptionValue<string>("name").Should().Be("test");
    }

    [Fact]
    public void GetOptionValue_IntOption()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "count", Value = JsonSerializer.SerializeToElement(5) }
                }
            }
        };

        interaction.GetOptionValue<int>("count").Should().Be(5);
        interaction.GetOptionValue<long>("count").Should().Be(5);
    }

    [Fact]
    public void GetOptionValue_BoolOption()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "enabled", Value = JsonSerializer.SerializeToElement(true) }
                }
            }
        };

        interaction.GetOptionValue<bool>("enabled").Should().BeTrue();
    }

    [Fact]
    public void GetOptionValue_DoubleOption()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "price", Value = JsonSerializer.SerializeToElement(9.99) }
                }
            }
        };

        interaction.GetOptionValue<double>("price").Should().Be(9.99);
    }

    [Fact]
    public void GetOptionValue_ULongFromNumber()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "id", Value = JsonSerializer.SerializeToElement(12345UL) }
                }
            }
        };

        interaction.GetOptionValue<ulong>("id").Should().Be(12345UL);
    }

    [Fact]
    public void GetOptionValue_ULongFromString()
    {
        var element = JsonSerializer.SerializeToElement("12345");
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "id", Value = element }
                }
            }
        };

        interaction.GetOptionValue<ulong>("id").Should().Be(12345UL);
    }

    [Fact]
    public void GetSubcommandName_NullOptions_ReturnsNull()
    {
        var interaction = new InteractionCreateEvent();
        interaction.GetSubcommandName().Should().BeNull();
    }

    [Fact]
    public void GetSubcommandName_FindsSubcommand()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "create", Type = 1, Options = new List<InteractionOption>() }
                }
            }
        };

        interaction.GetSubcommandName().Should().Be("create");
    }

    [Fact]
    public void GetSubcommandName_DrillsThroughGroup()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new()
                    {
                        Name = "admin", Type = 2,
                        Options = new List<InteractionOption>
                        {
                            new() { Name = "ban", Type = 1 }
                        }
                    }
                }
            }
        };

        interaction.GetSubcommandName().Should().Be("ban");
    }

    [Fact]
    public void FindOption_FindsTopLevel()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Options = new List<InteractionOption>
                {
                    new() { Name = "target", Value = JsonSerializer.SerializeToElement("user1") }
                }
            }
        };

        interaction.FindOption("target").Should().NotBeNull();
        interaction.FindOption("missing").Should().BeNull();
    }

    [Fact]
    public void GetInteractionContext_Guild_ReturnsGuild()
    {
        var interaction = new InteractionCreateEvent { GuildId = 1 };
        interaction.GetInteractionContext().Should().Be(Core.Enums.InteractionContextType.Guild);
    }

    [Fact]
    public void GetInteractionContext_BotDm_ReturnsBotDm()
    {
        var interaction = new InteractionCreateEvent
        {
            GuildId = null,
            User = new Core.Entities.User { Id = 1 }
        };
        interaction.GetInteractionContext().Should().Be(Core.Enums.InteractionContextType.BotDm);
    }

    [Fact]
    public void GetInteractionContext_PrivateChannel_ReturnsPrivate()
    {
        var interaction = new InteractionCreateEvent
        {
            GuildId = null,
            User = null,
            Member = new Core.Entities.GuildMember()
        };
        interaction.GetInteractionContext().Should().Be(Core.Enums.InteractionContextType.PrivateChannel);
    }

    [Fact]
    public void IsGuildInteraction_ReturnsTrueWhenGuildIdSet()
    {
        var interaction = new InteractionCreateEvent { GuildId = 1 };
        interaction.IsGuildInteraction().Should().BeTrue();
        interaction.IsDmInteraction().Should().BeFalse();
    }

    [Fact]
    public void IsDmInteraction_ReturnsTrueWhenNoGuild()
    {
        var interaction = new InteractionCreateEvent();
        interaction.IsDmInteraction().Should().BeTrue();
        interaction.IsGuildInteraction().Should().BeFalse();
    }

    [Fact]
    public void GetModalValue_NullComponents_ReturnsNull()
    {
        var interaction = new InteractionCreateEvent();
        interaction.GetModalValue("field1").Should().BeNull();
    }

    [Fact]
    public void GetModalValue_FindsValue()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Components = new List<Core.Entities.MessageComponent>
                {
                    new Core.Entities.ActionRow
                    {
                        Components = new List<Core.Entities.MessageComponent>
                        {
                            new Core.Entities.TextInput { CustomId = "field1", Value = "hello" }
                        }
                    }
                }
            }
        };

        interaction.GetModalValue("field1").Should().Be("hello");
        interaction.GetModalValue("missing").Should().BeNull();
    }

    [Fact]
    public void GetModalValues_ReturnsAllValues()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Components = new List<Core.Entities.MessageComponent>
                {
                    new Core.Entities.ActionRow
                    {
                        Components = new List<Core.Entities.MessageComponent>
                        {
                            new Core.Entities.TextInput { CustomId = "a", Value = "1" },
                            new Core.Entities.TextInput { CustomId = "b", Value = "2" }
                        }
                    }
                }
            }
        };

        var values = interaction.GetModalValues();
        values.Should().HaveCount(2);
        values["a"].Should().Be("1");
        values["b"].Should().Be("2");
    }

    [Fact]
    public void GetSelectedValues_ReturnsValues()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData
            {
                Values = new List<string> { "red", "blue" }
            }
        };

        interaction.GetSelectedValues().Should().BeEquivalentTo("red", "blue");
    }

    [Fact]
    public void GetComponentType_ReturnsType()
    {
        var interaction = new InteractionCreateEvent
        {
            Data = new InteractionData { ComponentType = 3 }
        };

        interaction.GetComponentType().Should().Be(3);
    }
}
