# PawSharp.Cache

PawSharp.Cache provides caching primitives for PawSharp-based applications.

Use it when you need faster reads, fewer REST calls, and a cleaner way to keep frequently accessed Discord data close to your bot or service.

## Why Use This Package

- In-memory caching for low-latency access
- Pluggable cache provider model for custom backends
- Designed to work with gateway-driven updates
- Helpful for large bots that need predictable performance

## Requirements

- .NET 10 (`net10.0`)

## Installation

```bash
dotnet add package PawSharp.Cache --version 1.0.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Cache.Providers;

var cache = new MemoryCacheProvider(new CacheOptions
{
    MaxGuilds = 1000,
    MaxChannels = 5000,
    MaxUsers = 20000
});

cache.CacheGuild(guild);
var cachedGuild = cache.GetGuild(guild.Id);
```

## Typical Use Cases

- Reducing repeat API calls for entity lookups
- Keeping active guild/member/channel data in memory
- Building custom cache providers for distributed deployments

## Related Packages

- `PawSharp.Client`: high-level client with caching integration
- `PawSharp.Gateway`: real-time events used to keep cached data fresh
- `PawSharp.Core`: shared models for cached entities

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
