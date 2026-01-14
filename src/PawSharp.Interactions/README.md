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

## Installation

```bash
dotnet add package PawSharp.Interactions --version 0.5.0-alpha9
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