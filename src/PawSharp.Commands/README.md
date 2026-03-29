# PawSharp.Commands

PawSharp.Commands adds a clean, attribute-based prefix command framework on top of PawSharp.Client.

It is built for maintainable bot command modules with async support, readable command metadata, and guardrails such as permissions and preconditions.

## Why Use This Package

- Attribute-driven command definitions
- Async command handlers and module registration
- Aliases, descriptions, and structured command metadata
- Permission and precondition support
- Great fit for mature prefix-command bots

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`

## Installation

```bash
dotnet add package PawSharp.Commands --version 1.0.0-alpha.2
```

## Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Commands;

var commands = client.UseCommands(prefix: "!");

public sealed class GeneralCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Check whether the bot is responsive")]
    public async Task PingAsync(CommandContext ctx)
        => await ctx.ReplyAsync("Pong!");
}

commands.RegisterModule(client, new GeneralCommands());
```

## Typical Use Cases

- Prefix command bots with multiple modules
- Moderator/admin command suites
- Bots that need reusable precondition logic

## Related Packages

- `PawSharp.Client`: the host client for command execution
- `PawSharp.Interactions`: slash command and component workflows
- `PawSharp.Core`: shared models and enums

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## License

MIT. See [../../LICENSE](../../LICENSE).
