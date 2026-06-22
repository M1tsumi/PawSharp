# PawSharp.Core

PawSharp.Core contains the foundational types used across the PawSharp package family.

If you are building integrations, middleware, or custom abstractions, this package gives you the shared entities, enums, and utility types needed to work with Discord data consistently.

## Why Use This Package

- Shared entity models used across all PawSharp packages
- Discord-centric enums and flag types
- Utility primitives for IDs, validation, and common helpers
- Stable foundation for package-to-package compatibility

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Core --version 1.1.0-alpha.4
```

## Quick Start

```csharp
using PawSharp.Core.Entities;

var user = new User
{
    Id = 123456789012345678,
    Username = "example_user"
};

Console.WriteLine($"User: {user.Username} ({user.Id})");
```

## Typical Use Cases

- Reusing PawSharp models in your own services or libraries
- Sharing strongly typed Discord entities between projects
- Building custom features on top of PawSharp APIs

## Related Packages

- `PawSharp.API`: REST client built on Core models
- `PawSharp.Gateway`: event stream with Core entities
- `PawSharp.Client`: all-in-one client built on Core

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## Support

- Join the [PawSharp Discord](https://discord.gg/6Z8X8cCHXs) for help, discussion, and community.
- Report bugs or request features via [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues).
- Start a discussion on [GitHub Discussions](https://github.com/M1tsumi/PawSharp/discussions).

## License

MIT. See [../../LICENSE](../../LICENSE).
