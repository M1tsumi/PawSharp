# PawSharp.Interactions

PawSharp.Interactions brings Discord's modern interaction model into your bot workflow.

Use it for slash commands, button/select interactions, and modal submissions with a clean structure that stays maintainable as your command surface grows.

## Why Use This Package

- Slash command and component interaction handling
- Support for modals and follow-up responses
- Strongly typed interaction data
- Clean extension workflow with PawSharp.Client

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Interactions --version 1.0.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Interactions;

var interactions = client.UseInteractions();

interactions.OnInteractionCreate += async interaction =>
{
    if (interaction.Type == InteractionType.ApplicationCommand)
    {
        await interaction.RespondAsync("Interaction received.");
    }
};
```

## Typical Use Cases

- Slash-first bot command experiences
- Rich UI flows with buttons, menus, and modals
- Hybrid bots using both commands and interactions

## Related Packages

- `PawSharp.Client`: recommended host for interaction handlers
- `PawSharp.Commands`: prefix command workflows
- `PawSharp.Interactivity`: user-response waiters and paginated UX

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
