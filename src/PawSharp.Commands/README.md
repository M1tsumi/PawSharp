# PawSharp.Commands

Command framework for PawSharp Discord library.

## Features

- Attribute-based command registration
- Command aliases and descriptions
- Automatic command parsing
- Modular command organization
- Before/after execution hooks

## Installation

```bash
dotnet add package PawSharp.Commands
```

## Usage

```csharp
using PawSharp.Commands.Extensions;

// Enable commands
var commands = client.UseCommands("!");

// Create a command module
public class FunCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Responds with pong!")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }

    [Command("echo")]
    [Aliases("say")]
    [Description("Echoes the provided text")]
    public async Task EchoAsync(CommandContext ctx)
    {
        if (ctx.Arguments.Length == 0)
        {
            await ctx.RespondAsync("Please provide text to echo!");
            return;
        }

        await ctx.RespondAsync(string.Join(" ", ctx.Arguments));
    }
}

// Register the module
commands.RegisterModule(client, new FunCommands());
```

Commands can then be used like:
- `!ping` -> "Pong!"
- `!echo Hello World` -> "Hello World"
- `!say Hi there` -> "Hi there"