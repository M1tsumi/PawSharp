# PawSharp.Interactions

Slash commands and component interactions for Discord bots.

PawSharp.Interactions provides a complete framework for handling Discord's modern interaction system, including slash commands, buttons, select menus, and modal dialogs.

## Features

- Slash command registration and handling
- Component interaction support (buttons, select menus)
- Automatic interaction response handling
- Followup message support
- Modal dialog handling
- Type-safe interaction data parsing
- Permission checking for commands
- Fully typed component hierarchy — `ActionRow`, `Button`, `SelectMenu` variants, `TextInput`
- `ModalBuilder` with fluent `AddTextInput(label, customId, TextInputStyle, …)` API

## Installation

```bash
dotnet add package PawSharp.Interactions --version 0.6.1-alpha1
```

## Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Interactions;

// Create Discord client
var client = new DiscordClient(new PawSharpOptions { Token = "your-token" });

// Enable interactions
var interactions = client.UseInteractions();

// Handle slash commands
interactions.OnInteractionCreate += async (interaction) =>
{
    if (interaction.Type == InteractionType.ApplicationCommand)
    {
        var commandData = interaction.Data as ApplicationCommandInteractionData;
        if (commandData?.Name == "ping")
        {
            await interaction.RespondAsync("Pong!");
        }
    }
};

// Register slash commands
await client.Rest.CreateGuildApplicationCommandAsync(guildId, new ApplicationCommand
{
    Name = "ping",
    Description = "Responds with pong",
    Type = ApplicationCommandType.ChatInput
});
```

## Interaction Types

### Slash Commands

```csharp
interactions.OnInteractionCreate += async (interaction) =>
{
    if (interaction.Type == InteractionType.ApplicationCommand)
    {
        var data = interaction.Data as ApplicationCommandInteractionData;
        switch (data?.Name)
        {
            case "ping":
                await interaction.RespondAsync("Pong!");
                break;
            case "echo":
                var text = data.Options?.FirstOrDefault()?.Value as string;
                await interaction.RespondAsync(text);
                break;
        }
    }
};
```

### Button Interactions

```csharp
interactions.OnInteractionCreate += async (interaction) =>
{
    if (interaction.Type == InteractionType.MessageComponent)
    {
        var data = interaction.Data as MessageComponentInteractionData;
        if (data?.ComponentType == ComponentType.Button)
        {
            switch (data.CustomId)
            {
                case "primary_button":
                    await interaction.RespondAsync("Primary button clicked!");
                    break;
                case "secondary_button":
                    await interaction.UpdateMessageAsync("Secondary button clicked!");
                    break;
            }
        }
    }
};
```

### Select Menu Interactions

```csharp
interactions.OnInteractionCreate += async (interaction) =>
{
    if (interaction.Type == InteractionType.MessageComponent)
    {
        var data = interaction.Data as MessageComponentInteractionData;
        if (data?.ComponentType == ComponentType.SelectMenu)
        {
            var selectedValue = data.Values?.FirstOrDefault();
            await interaction.RespondAsync($"You selected: {selectedValue}");
        }
    }
};
```

## Modal Dialogs

```csharp
// Build a modal using the fluent ModalBuilder
var modal = new ModalBuilder()
    .WithCustomId("feedback_modal")
    .WithTitle("Share your feedback")
    .AddTextInput("Your name",     "name_input",     TextInputStyle.Short,     placeholder: "Jane Doe")
    .AddTextInput("Your feedback", "feedback_input",  TextInputStyle.Paragraph, minLength: 10, maxLength: 500)
    .Build();

await interaction.RespondWithModalAsync(modal);
```

> **Note (alpha13 breaking change):** `AddTextInput` now accepts `TextInputStyle` instead of `int` for the `style` parameter.
> Replace `style: 1` / `style: 2` with `TextInputStyle.Short` / `TextInputStyle.Paragraph`.

## EmbedBuilder

```csharp
using PawSharp.Core.Builders;

var embed = new EmbedBuilder()
    .WithTitle("Result")
    .WithDescription("Operation completed successfully.")
    .WithColor(0x57F287) // green
    .AddField("Duration", "42 ms", inline: true)
    .WithFooter("PawSharp")
    .WithTimestamp()
    .Build();

await interaction.RespondAsync(embed: embed);
```

## Typed Components

Components received in interactions are now fully typed:

```csharp
// Message.Components is List<MessageComponent>? — deserializes automatically
foreach (var row in message.Components ?? [])
{
    if (row is ActionRow actionRow)
    {
        foreach (var component in actionRow.Components)
        {
            if (component is Button btn)
                Console.WriteLine($"Button: {btn.Label} ({btn.Style})");
            else if (component is SelectMenu menu)
                Console.WriteLine($"Select: {menu.CustomId}, options: {menu.Options.Count}");
        }
    }
}
```

## Response Types

### Immediate Response

```csharp
// Respond immediately (within 3 seconds)
await interaction.RespondAsync("Quick response!");
```

### Deferred Response

```csharp
// Defer response for later
await interaction.DeferAsync();

// Do some work...
await Task.Delay(2000);

// Send followup
await interaction.FollowupAsync("Deferred response!");
```

### Update Message

```csharp
// Update the original message (for components)
await interaction.UpdateMessageAsync("Message updated!");
```

## Command Registration

### Global Commands

```csharp
var command = new ApplicationCommand
{
    Name = "globalcommand",
    Description = "A global slash command",
    Type = ApplicationCommandType.ChatInput
};

await client.Rest.CreateGlobalApplicationCommandAsync(command);
```

### Guild Commands

```csharp
var command = new ApplicationCommand
{
    Name = "guildcommand",
    Description = "A guild-specific slash command",
    Type = ApplicationCommandType.ChatInput,
    Options = new[]
    {
        new ApplicationCommandOption
        {
            Name = "text",
            Description = "Text to echo",
            Type = ApplicationCommandOptionType.String,
            Required = true
        }
    }
};

await client.Rest.CreateGuildApplicationCommandAsync(guildId, command);
```

## Permission Management

```csharp
// Set command permissions
var permissions = new ApplicationCommandPermissions
{
    Permissions = new[]
    {
        new ApplicationCommandPermission
        {
            Id = roleId,
            Type = ApplicationCommandPermissionType.Role,
            Permission = true
        }
    }
};

await client.Rest.EditApplicationCommandPermissionsAsync(applicationId, guildId, commandId, permissions);
```

## Dependencies

- PawSharp.Core - Entity models
- PawSharp.Client - Discord client integration
- .NET 8.0 - Modern runtime

## Related Packages

- PawSharp.Client - Main client
- PawSharp.Commands - Traditional commands

## License

MIT License - see [LICENSE](../LICENSE) for details.