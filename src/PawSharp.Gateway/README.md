# PawSharp.Gateway

PawSharp.Gateway is the real-time WebSocket layer for Discord events in the PawSharp ecosystem.

It handles the moving parts you do not want to rebuild repeatedly: identify/resume flow, heartbeat management, reconnect strategy, and event dispatch.

## Why Use This Package

- Reliable gateway lifecycle management
- Built-in reconnect and session resume behavior
- Typed event handling patterns
- Sharding support for larger bots
- Works well standalone or with PawSharp.Client

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Gateway --version 1.0.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.Gateway;

var gateway = new GatewayClient(new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
});

gateway.Events.On<MessageCreateEvent>(async evt =>
{
    Console.WriteLine($"Message in {evt.ChannelId}: {evt.Content}");
});

await gateway.ConnectAsync();
```

## Typical Use Cases

- Bots that need direct control over gateway behavior
- Event-driven processing pipelines
- Sharded deployments at scale

## Related Packages

- `PawSharp.API`: REST operations paired with gateway events
- `PawSharp.Client`: higher-level orchestration around gateway + REST
- `PawSharp.Core`: shared event and entity models

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
