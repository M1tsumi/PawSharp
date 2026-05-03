# PawSharp.Interactivity

PawSharp.Interactivity adds user-driven interaction helpers for long-running conversational flows.

It is especially useful for bots that need pagination, wait-for-input patterns, and guided multi-step user experiences.

## Why Use This Package

- Pagination helpers for long or structured outputs (reaction and button-based)
- Waiters for reactions, buttons, select menus, and modals
- Native Discord Poll result retrieval and management
- Custom reaction polls with result tracking
- Timeouts and flow control for safer user prompts
- Input dialogs for text collection with validation
- Builder patterns for complex multi-step flows
- Cleaner UX for command-driven bots

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Interactivity --version 1.1.0-alpha.1
```

## Quick Start

```csharp
using PawSharp.Interactivity.Extensions;

var interactivity = client.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2)
});

// Wait for a reaction
var result = await message.WaitForReactionAsync(client, user, "👍");
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

Send paginated messages with modern button-based navigation:

```csharp
var pages = interactivity.GeneratePagesInContent(longText, maxLength: 2000);
await channel.SendButtonPaginatedMessageAsync(client, user, pages, TimeSpan.FromMinutes(5));
```

Customize pagination with callbacks:

```csharp
var config = new InteractivityConfiguration
{
    PaginationCallbacks = new PaginationCallbacks
    {
        OnPageChanged = async (pageIndex, page) =>
        {
            Console.WriteLine($"User navigated to page {pageIndex}");
        },
        OnTimeout = async () =>
        {
            Console.WriteLine("Pagination timed out");
        },
        OnStopped = async () =>
        {
            Console.WriteLine("User stopped pagination");
        }
    }
};
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

Wait for any of multiple emojis:

```csharp
var result = await message.WaitForAnyReactionAsync(client, user, new[] { "👍", "👎", "🤔" });
if (!result.TimedOut)
{
    // User reacted with one of the specified emojis
}
```

Wait for all specified users to react:

```csharp
var result = await message.WaitForAllReactionsAsync(client, users, "✅");
if (!result.TimedOut)
{
    // All users have reacted with ✅
    var reactedUsers = result.Result;
}
```

Wait for reaction removal:

```csharp
var result = await message.WaitForReactionRemoveAsync(client, user, emoji: "👍");
```

Collect reactions over time:

```csharp
var reactionCounts = await message.CollectReactionsAsync(client, TimeSpan.FromMinutes(5));
// Returns Dictionary<string, int> mapping emoji to count
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

Get poll results (vote counts):

```csharp
var results = await message.GetPollResultsAsync(client, new[] { "Red", "Blue", "Green" });
// Returns Dictionary<string, int> mapping option to vote count
foreach (var (option, count) in results)
{
    Console.WriteLine($"{option}: {count} votes");
}
```

Get poll voters (who voted for each option):

```csharp
var voters = await message.GetPollVotersAsync(client, new[] { "Red", "Blue", "Green" });
// Returns Dictionary<string, List<User>> mapping option to list of voters
foreach (var (option, voterList) in voters)
{
    Console.WriteLine($"{option}: {voterList.Count} votes");
}
```

### Message Waiting

Wait for the next message in a channel:

```csharp
var result = await channel.GetNextMessageAsync(client, msg => msg.Content.StartsWith("!"));
```

Wait for message in the same channel as a specific message:

```csharp
var result = await message.WaitForMessageAsync(client, msg => msg.Author.Id == userId);
```

### Input Dialogs

Collect simple text input from users:

```csharp
var result = await channel.GetInputAsync(client, user, "Please enter your name:");
if (!result.TimedOut)
{
    var name = result.Result;
    await channel.SendMessageAsync($"Hello, {name}!");
}
```

Collect validated text input:

```csharp
var result = await channel.GetValidInputAsync(
    client,
    user,
    "Please enter your age:",
    input => int.TryParse(input, out var age) && age > 0 && age < 120,
    "Please enter a valid age between 1 and 119.",
    maxAttempts: 3);

if (!result.TimedOut)
{
    var age = int.Parse(result.Result);
}
```

### Complex Flows

Use the builder pattern for multi-step interactions:

```csharp
using PawSharp.Interactivity.Builders;

var results = await channel.CreateFlow(client, user, TimeSpan.FromMinutes(5))
    .WithMessageInput("What is your name?")
    .WithConfirmation("Are you sure this is correct?")
    .WithMessageInput("How can I help you today?")
    .ExecuteAsync<string>();

// results contains the outcome of each step
```

### Interaction Bridge

Bridge between Interactions and Interactivity for seamless workflows:

```csharp
using PawSharp.Interactivity.Extensions;

// Respond with buttons and wait for click
var result = await interaction.RespondAndWaitForButtonAsync(
    client,
    new InteractionResponse { /* button response */ },
    targetCustomId: "confirm");

// Defer and wait for message
var messageResult = await interaction.DeferAndWaitForMessageAsync(
    client,
    channel);

// Show modal and wait for submission
var modalResult = await interaction.ShowModalAndWaitAsync(
    client,
    new InteractionCallbackData { /* modal config */ });
```

### Components V2 Support

Discord's Components V2 includes new modal components like Radio Groups, Checkbox Groups, and Checkboxes. The package provides ergonomic waiters for these:

```csharp
using PawSharp.Interactivity.Extensions;

// Wait for RadioGroup selection
var radioResult = await message.WaitForRadioGroupAsync(client, user, customId: "class_selection");
if (!radioResult.TimedOut)
{
    var selectedClass = radioResult.Result; // The selected value
}

// Wait for CheckboxGroup selections
var checkboxGroupResult = await message.WaitForCheckboxGroupAsync(client, user, customId: "days_selection");
if (!checkboxGroupResult.TimedOut)
{
    var selectedDays = checkboxGroupResult.Result; // List of selected values
}

// Wait for Checkbox toggle
var checkboxResult = await message.WaitForCheckboxAsync(client, user, customId: "agreement");
if (!checkboxResult.TimedOut)
{
    var agreed = checkboxResult.Result; // true or false
}
```

When sending messages with Components V2, use the IS_COMPONENTS_V2 flag helper:

```csharp
var request = new CreateMessageRequest
{
    Content = "Choose your options:",
    Components = new List<MessageComponent> { /* Components V2 layout */ }
}.WithComponentsV2(); // Sets the required IS_COMPONENTS_V2 flag (32768)

await client.Rest.CreateMessageAsync(channel.Id, request);
```

Check if a message has Components V2:

```csharp
if (message.HasComponentsV2())
{
    // Message uses Components V2 layout
}
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
    },
    PaginationButtonLabels = new PaginationButtonLabels
    {
        First = "⏮ First",
        Previous = "◀ Previous",
        Stop = "⏹ Stop",
        Next = "▶ Next",
        Last = "⏭ Last"
    },
    PaginationCallbacks = new PaginationCallbacks
    {
        OnPageChanged = async (pageIndex, page) => { /* handle page change */ },
        OnTimeout = async () => { /* handle timeout */ },
        OnStopped = async () => { /* handle stop */ }
    }
};
```

## Validation

The package includes validation helpers for improved error messages:

```csharp
using PawSharp.Interactivity.Validation;

// These throw descriptive exceptions on validation failure
InteractivityValidation.RequireNotNull(value, nameof(value));
InteractivityValidation.RequireNotNullOrEmpty(text, nameof(text));
InteractivityValidation.RequireNotEmpty(collection, nameof(collection));
InteractivityValidation.RequireCountBetween(collection, min, max, nameof(collection));
InteractivityValidation.RequirePositive(number, nameof(number));
```

## Typical Use Cases

- Multi-step prompts and onboarding flows
- Poll-like user input handling
- Readable pagination for help/guide output
- Modal-based forms for data collection
- Interactive confirmation dialogs
- User input collection with validation
- Complex multi-step conversation flows

## Related Packages

- `PawSharp.Client`: host client and event pipeline
- `PawSharp.Commands`: command triggers for interactive flows
- `PawSharp.Interactions`: slash and component-first UX

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
