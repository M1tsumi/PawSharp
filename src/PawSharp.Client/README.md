# PawSharp.Client

PawSharp.Client is the high-level entry point for building Discord bots with PawSharp.

It brings REST, gateway, caching, commands, interactions, and voice extensions into one cohesive developer experience while still letting you opt into only what you need.

## Why Use This Package

- Fastest way to build a full-featured bot
- Unified client surface for common Discord workflows
- Extension model for commands, interactions, interactivity, and voice
- Clean integration with dependency injection and hosted services

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Client --version 1.0.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .Build();

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

## Typical Use Cases

- Standalone Discord bots
- Multi-service bot architectures with DI
- Bots that combine message commands, slash commands, and voice

## Related Packages

- `PawSharp.Commands`: prefix command framework
- `PawSharp.Interactions`: slash commands and component interactions
- `PawSharp.Interactivity`: waiters, pagination, and input workflows
- `PawSharp.Voice`: voice connectivity and audio pipeline

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
