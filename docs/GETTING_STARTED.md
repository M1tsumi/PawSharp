# Getting Started with PawSharp

Welcome to PawSharp! This guide will help you create your first Discord bot using the PawSharp library.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A Discord bot token (create one at [Discord Developer Portal](https://discord.com/developers/applications))

## Installation

Create a new console application:

```bash
dotnet new console -n MyFirstBot
cd MyFirstBot
dotnet add package PawSharp.Client
```

## Your First Bot

Replace the contents of `Program.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Models;

namespace MyFirstBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole());

        // Configure PawSharp
        var options = new PawSharpOptions
        {
            Token = "YOUR_BOT_TOKEN_HERE",
            Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged
        };

        services.AddSingleton(options);
        services.AddPawSharpClient();

        var serviceProvider = services.BuildServiceProvider();

        // Get the Discord client
        var client = serviceProvider.GetRequiredService<DiscordClient>();

        // Handle message events
        client.MessageCreate += async (message) =>
        {
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
        };

        // Connect to Discord
        await client.ConnectAsync();

        // Keep the bot running
        await Task.Delay(-1);
    }
}
```

## Running Your Bot

1. Replace `YOUR_BOT_TOKEN_HERE` with your actual bot token
2. Run the bot:
   ```bash
   dotnet run
   ```
3. Invite the bot to your server and type `!ping`

## Adding Commands

For more advanced bots, use the command framework:

```csharp
using PawSharp.Commands;

// In your Program.cs, add:
services.AddPawSharpCommands();

// Create a command module:
[CommandModule("util")]
public class UtilityCommands : CommandModule
{
    [Command("echo")]
    [Description("Echo a message back")]
    public async Task EchoAsync([Description("The message to echo")] string message)
    {
        await ReplyAsync(message);
    }

    [Command("userinfo")]
    [Description("Get user information")]
    public async Task UserInfoAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{Context.User.Username}#{Context.User.Discriminator}")
            .AddField("ID", Context.User.Id.ToString())
            .WithColor(Color.Blue);

        await ReplyAsync(embed: embed.Build());
    }
}

// Register the module:
var commandService = serviceProvider.GetRequiredService<CommandService>();
await commandService.AddModuleAsync<UtilityCommands>();
```

## Slash Commands

For modern Discord interactions:

```csharp
using PawSharp.Interactions;

// Add to services:
services.AddPawSharpInteractions();

// Create interaction module:
public class SlashCommands : InteractionModule
{
    [SlashCommand("hello", "Say hello to someone")]
    public async Task HelloAsync(
        [Description("Who to greet")] string name)
    {
        await RespondAsync($"Hello, {name}!");
    }
}

// Register commands:
var interactionService = serviceProvider.GetRequiredService<InteractionService>();
await interactionService.RegisterCommandsAsync();
```

## Configuration

PawSharp supports various configuration options:

```csharp
var options = new PawSharpOptions
{
    Token = "your-token",
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
    Shards = 1,  // For large bots
    EnableCompression = true,
    MaxMissedHeartbeatAcks = 3
};
```

## Next Steps

- Explore the [examples/](examples/) directory for more complex bots
- Read the [API documentation](api/) for detailed reference
- Join our community for support and questions
- Check out [SHARDING.md](SHARDING.md) for scaling your bot

## Troubleshooting

**Bot doesn't respond:**
- Check your bot token is correct
- Ensure the bot has proper permissions in your server
- Verify intents are set correctly

**Compilation errors:**
- Make sure you're using .NET 8.0 or later
- Check that all NuGet packages are restored

**Connection issues:**
- Verify your internet connection
- Check Discord status at [discordstatus.com](https://discordstatus.com)

## Support

- [GitHub Issues](https://github.com/your-org/PawSharp/issues) for bug reports
- [Documentation](docs/) for detailed guides
- Community Discord server (coming soon)