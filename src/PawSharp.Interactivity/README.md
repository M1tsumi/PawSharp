# PawSharp.Interactivity

Interactive command framework for PawSharp Discord library.

## Features

- Paginated messages with reaction controls
- Reaction waiting and collection
- Message polling
- Interactive command flows

## Installation

```bash
dotnet add package PawSharp.Interactivity
```

## Usage

```csharp
using PawSharp.Interactivity.Extensions;

// Enable interactivity
var interactivity = client.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(5)
});

// Send paginated message
var pages = interactivity.GeneratePagesInEmbed(longText);
await channel.SendPaginatedMessageAsync(user, pages);

// Wait for reaction
var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    // User reacted with thumbs up
}

// Create poll
await message.CreatePollAsync("What's your favorite color?",
    new[] { "Red", "Blue", "Green" });
```