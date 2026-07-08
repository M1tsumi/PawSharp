# Interactions

The Interactions system handles slash commands, components, modals, autocomplete, and context menus through a unified routing system.

## Interaction Handler

PawSharp's `PawSharp.Interactions` package provides automatic routing of all interaction types:

```csharp
// Register handlers before connecting
client.Interactions.RegisterCommand("ping", HandlePingAsync);
client.Interactions.RegisterComponent("my_button", HandleButtonAsync);
client.Interactions.RegisterModal("feedback_modal", HandleModalAsync);
```

## Autocomplete

Provide dynamic choices for command options:

```csharp
client.Interactions.RegisterAutocomplete("color", async interaction =>
{
    var query = interaction.Data?.Options?.FirstOrDefault()?.Value?.ToString()?.ToLower() ?? "";
    var colors = new[] { "Red", "Green", "Blue", "Yellow", "Purple" };
    var matches = colors.Where(c => c.ToLower().Contains(query))
        .Select(c => new ApplicationCommandOptionChoice { Name = c, Value = c })
        .Take(25)
        .ToList();

    await client.Rest.CreateInteractionResponseAsync(
        interaction.Id, interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ApplicationCommandAutocompleteResult,
            Data = new InteractionCallbackData { Choices = matches },
        }
    );
});
```

## Context Menus

Register context menu commands for users or messages:

```csharp
// User context menu
await client.Rest.CreateGlobalApplicationCommandAsync(
    appId,
    new CreateApplicationCommandRequest
    {
        Name = "Get Avatar",
        Type = ApplicationCommandType.User,
    }
);

// Message context menu
await client.Rest.CreateGlobalApplicationCommandAsync(
    appId,
    new CreateApplicationCommandRequest
    {
        Name = "Mark as Spam",
        Type = ApplicationCommandType.Message,
    }
);
```

Handle context menu interactions:

```csharp
client.Interactions.RegisterCommand("Get Avatar", async interaction =>
{
    var userId = interaction.Data?.Resolved?.Users?.FirstOrDefault().Key ?? 0;
    var user = interaction.Data?.Resolved?.Users?.FirstOrDefault().Value;
    // Respond with the user's avatar URL
});
```

## Interaction Response Types

- **ChannelMessageWithSource** — Reply with a message
- **DeferredChannelMessageWithSource** — Acknowledge (for slow processing), then edit later
- **Modal** — Show a modal
- **ApplicationCommandAutocompleteResult** — Provide autocomplete choices
