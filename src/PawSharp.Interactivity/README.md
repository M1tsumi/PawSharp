# PawSharp.Interactivity

PawSharp.Interactivity adds user-driven interaction helpers for long-running conversational flows.

It is especially useful for bots that need pagination, wait-for-input patterns, and guided multi-step user experiences.

## Why Use This Package

- Pagination helpers for long or structured outputs
- Waiters for reactions and component responses
- Timeouts and flow control for safer user prompts
- Cleaner UX for command-driven bots

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Interactivity --version 1.0.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Interactivity.Extensions;

var interactivity = client.UseInteractivity(new InteractivityConfiguration
{
    Timeout = TimeSpan.FromMinutes(2)
});

var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    await channel.SendMessageAsync("Thanks for confirming.");
}
```

## Typical Use Cases

- Multi-step prompts and onboarding flows
- Poll-like user input handling
- Readable pagination for help/guide output

## Related Packages

- `PawSharp.Client`: host client and event pipeline
- `PawSharp.Commands`: command triggers for interactive flows
- `PawSharp.Interactions`: slash and component-first UX

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
