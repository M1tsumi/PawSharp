# Installation

## System Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A Discord bot token ([create one here](https://discord.com/developers/applications))
- Windows, macOS, or Linux

>  **.NET 10 is required.** PawSharp targets `net10.0` and uses APIs from the .NET 10 Base Class Library. Older framework versions are not supported.

## Create a New Project

```bash
dotnet new console -n MyDiscordBot
cd MyDiscordBot
```

## Add NuGet Packages

PawSharp is distributed as nine NuGet packages. The recommended approach is to install `PawSharp.Client`, which aggregates everything most bots need:

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.5
```

### All Packages

| Package | Command | Purpose |
|---------|---------|---------|
| **PawSharp.Client** | `dotnet add package PawSharp.Client` | All-in-one facade (recommended) |
| PawSharp.Core | `dotnet add package PawSharp.Core` | Base entities, enums, builders |
| PawSharp.API | `dotnet add package PawSharp.API` | REST API client with rate limiting |
| PawSharp.Gateway | `dotnet add package PawSharp.Gateway` | WebSocket gateway, events, sharding |
| PawSharp.Cache | `dotnet add package PawSharp.Cache` | In-memory and Redis caching |
| PawSharp.Commands | `dotnet add package PawSharp.Commands` | Prefix command framework |
| PawSharp.Interactions | `dotnet add package PawSharp.Interactions` | Slash commands, components, modals |
| PawSharp.Interactivity | `dotnet add package PawSharp.Interactivity` | Pagination, polls, user prompts |
| PawSharp.Voice | `dotnet add package PawSharp.Voice` | Voice connections, Opus, DAVE E2EE |

### Package Dependency Tree

```
PawSharp.Core (foundation - none depend on it)
├── PawSharp.API (REST)
├── PawSharp.Cache (caching)
├── PawSharp.Interactions (slash commands, components)
├── PawSharp.Interactivity (pagination, polls)
├── PawSharp.Commands (prefix commands)
└── PawSharp.Voice (voice)

PawSharp.API + PawSharp.Cache + PawSharp.Gateway
 └── PawSharp.Client (unified facade)

PawSharp.Client + PawSharp.API + PawSharp.Core
 ├── PawSharp.Commands (depends on Client)
 ├── PawSharp.Interactivity (depends on Client)
 └── PawSharp.Voice (depends on Client, Gateway, API)
```

### When to Install Which Package

| If you want to... | Install |
|------------------|---------|
| Build a normal Discord bot | `PawSharp.Client` |
| Use only the REST API (no Gateway) | `PawSharp.API` |
| Add prefix text commands | `PawSharp.Client` + `PawSharp.Commands` |
| Add slash commands and components | `PawSharp.Client` (includes Interactions) |
| Add pagination, polls, prompts | `PawSharp.Client` + `PawSharp.Interactivity` |
| Add voice support | `PawSharp.Client` + `PawSharp.Voice` |
| Use Redis caching | `PawSharp.Client` + `PawSharp.Cache` (Redis provider) |
| Listen to gateway events only | `PawSharp.Gateway` |
| Build a lightweight REST-only service | `PawSharp.API` + `PawSharp.Cache` |

>  **`PawSharp.Client` is the right starting point for 90% of bots.** It includes Core, API, Gateway, Cache, and Interactions in one package reference. You only need additional packages for Commands, Interactivity, or Voice.

## Add Logging (Recommended)

```bash
dotnet add package Microsoft.Extensions.Logging.Console
```

All PawSharp packages log through `Microsoft.Extensions.Logging`. With `UseConsoleLogging()` on the builder, or by adding `ILogger` via DI, you get structured output:

```
info: PawSharp.Gateway.GatewayClient[0]
 Connected to gateway (session_id: abc123, shard: 0/1)
info: PawSharp.Client.DiscordClient[0]
 Client connected as MyBot#1234
```

## Nightly / Pre-release Versions

Pre-release versions (alpha, beta, release candidates) are published to NuGet with the `prerelease` flag. To install the latest pre-release:

```bash
dotnet add package PawSharp.Client --prerelease
```

Or specify the exact version:

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.5
```

>  **Alpha caveats:** Breaking changes may occur between alpha releases. Always pin your version and review [../CHANGELOG.md](../../CHANGELOG.md) before upgrading.

## Building from Source

If you need to modify PawSharp or use the latest unreleased changes:

```bash
git clone https://github.com/M1tsumi/PawSharp.git
cd PawSharp
dotnet restore
dotnet build
```

The built NuGet packages will be placed in the `nupkgs/` directory. To reference them locally in your project:

```bash
# Create or edit nuget.config in your project directory
```

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
 <clear />
 <add key="local" value="..\path\to\PawSharp\nupkgs" />
 <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
 </packageSources>
</configuration>
```

Then add the package as usual:

```bash
dotnet add package PawSharp.Client
```

## Troubleshooting Installation

### `The SDK 'Microsoft.NET.Sdk' specified could not be found`

Your .NET SDK version is too old. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet --version
# Should return 10.0.xxx or later
```

### NU1103: Unable to find package

- Ensure you spelled the package name correctly (case-sensitive for some NuGet sources).
- If using a local feed, verify the path in `nuget.config`.
- Try `dotnet nuget list source` to check configured sources.

### NU1603: Dependency specified was not found

You may be missing a transitive dependency. Add `PawSharp.Core` explicitly, or upgrade all PawSharp packages to the same version:

```bash
dotnet add package PawSharp.Core --version 1.1.0-alpha.5
```

### Build warnings about package version mismatch

All PawSharp packages should be the same version. Mixing `1.1.0-alpha.4` with `1.1.0-alpha.5` may produce warnings or runtime errors.

### `dotnet build` fails with cryptic errors

```bash
dotnet restore
dotnet build --no-restore
```

If `dotnet restore` succeeds but build fails, ensure you meet the .NET 10 system requirements and have no conflicting global.json settings.

## Verify Installation

After adding packages, verify everything compiles:

```bash
dotnet build
```

Expected output:

```
Build succeeded.
 0 Warning(s)
 0 Error(s)
```

---

## Next Steps

- [Getting Started](./getting-started.md) &mdash; create your first bot
- [Your First Bot](./guides/first-bot.md) &mdash; step-by-step tutorial
- [Package READMEs](../src/PawSharp.Client/README.md) &mdash; per-package documentation
