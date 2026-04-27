# PawSharp.Interactivity

PawSharp.Interactivity adds user-driven interaction helpers for long-running conversational flows.

It is especially useful for bots that need pagination, wait-for-input patterns, and guided multi-step user experiences.

## Why Use This Package

- Pagination helpers for long or structured outputs
- Waiters for reactions, buttons, select menus, and modals
- Native Discord Poll result retrieval and management
- Timeouts and flow control for safer user prompts
- Cleaner UX for command-driven bots

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Interactivity --version 1.0.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.Interactivity.Extensions;

var interactivity = client.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2)
});

// Wait for a reaction
var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    await channel.SendMessageAsync("Thanks for confirming.");
}

// Wait for a button click
var buttonResult = await message.WaitForButtonAsync(client, user, customId: "confirm");

// Wait for a modal submission
var modalResult = await message.WaitForModalAsync(client, user, customId: "feedback-form");
```

## Features

### Pagination

Send paginated messages with reaction-based navigation:

```csharp
var pages = interactivity.GeneratePagesInContent(longText, maxLength: 2000);
await channel.SendPaginatedMessageAsync(client, user, pages, TimeSpan.FromMinutes(5));
```

### Reaction Waiting

Wait for specific reactions from users:

```csharp
var result = await message.WaitForReactionAsync(client, user, emoji: "👍");
if (!result.TimedOut)
{
    // User reacted with 👍
}
```

Wait for reaction removal:

```csharp
var result = await message.WaitForReactionRemoveAsync(client, user, emoji: "👍");
```

### Component Interactions

Wait for button clicks:

```csharp
var result = await message.WaitForButtonAsync(client, user, customId: "delete");
```

Wait for select menu selections:

```csharp
var result = await message.WaitForSelectAsync(client, user, customId: "role-select");
```

Wait for modal submissions:

```csharp
var result = await message.WaitForModalAsync(client, user, customId: "ticket-form");
if (!result.TimedOut)
{
    var formData = result.Result.Data.Components;
    // Process form data
}
```

### Discord Native Polls

Retrieve poll voters:

```csharp
var voters = await message.GetPollAnswerVotersAsync(client, answerId: 0, limit: 25);
```

End a poll early:

```csharp
var updatedMessage = await message.EndPollAsync(client);
```

### Custom Reaction Polls

Create reaction-based polls with auto-cleanup:

```csharp
await message.CreatePollAsync(client, "What's your favorite color?",
    new[] { "Red", "Blue", "Green" },
    TimeSpan.FromMinutes(10));
```

### Message Waiting

Wait for the next message in a channel:

```csharp
var result = await channel.GetNextMessageAsync(client, msg => msg.Content.StartsWith("!"));
```

## Configuration

Configure interactivity behavior:

```csharp
var config = new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2),
    PollBehaviour = PollBehaviour.DeleteEmojis,
    PaginationEmojis = new PaginationEmojis
    {
        Left = "⬅",
        Right = "➡",
        SkipLeft = "⏮",
        SkipRight = "⏭",
        Stop = "⏹"
    }
};
```

## Typical Use Cases

- Multi-step prompts and onboarding flows
- Poll-like user input handling
- Readable pagination for help/guide output
- Modal-based forms for data collection
- Interactive confirmation dialogs

## Related Packages

- `PawSharp.Client`: host client and event pipeline
- `PawSharp.Commands`: command triggers for interactive flows
- `PawSharp.Interactions`: slash and component-first UX

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
