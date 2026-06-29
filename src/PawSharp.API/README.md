# PawSharp.API

PawSharp.API is the low-level REST client for Discord API v10 in the PawSharp ecosystem.

It is designed for teams that want direct control over HTTP calls while still getting the practical essentials out of the box: bucket-aware rate limiting, typed request/response models, and predictable error handling.

## Why Use This Package

- Full REST-first workflow for Discord endpoints
- Built-in rate limit handling and retry support
- Typed models from PawSharp.Core
- DI-friendly architecture for production services
- Clean fit for bots, dashboards, and backend workers

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.API --version 1.1.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.API.Clients;
using PawSharp.Core;

var options = new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!
};

var rest = new DiscordRestClient(options);

var me = await rest.GetCurrentUserAsync();
Console.WriteLine($"Connected as {me.Username}");
```

## Typical Use Cases

- Building custom API wrappers on top of Discord REST routes
- Running scheduled moderation or data sync jobs
- Integrating Discord actions into ASP.NET services
- Handling high request volumes with safer rate limit behavior

## Related Packages

- `PawSharp.Core`: shared entities, enums, and utilities
- `PawSharp.Gateway`: real-time event transport via WebSocket
- `PawSharp.Client`: all-in-one high-level client

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## Support

- Join the [PawSharp Discord](https://discord.gg/6Z8X8cCHXs) for help, discussion, and community.
- Report bugs or request features via [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues).
- Start a discussion on [GitHub Discussions](https://github.com/M1tsumi/PawSharp/discussions).

## License

MIT. See [../../LICENSE](../../LICENSE).
