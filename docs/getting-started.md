# Getting Started with PawSharp

## What is PawSharp?

PawSharp is a modular Discord API wrapper for **.NET 10** — REST, Gateway, caching, slash commands, prefix commands, interactivity, and voice with full DAVE E2EE.

**Current version:** `1.1.0-alpha.5` | **Discord API:** v10

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A Discord bot token ([create an application](https://discord.com/developers/applications))
- Basic C# knowledge

## Creating a Project

```bash
dotnet new console -n MyFirstBot
cd MyFirstBot
dotnet add package PawSharp.Client
dotnet add package Microsoft.Extensions.Logging.Console
```

## Ping Bot in ~15 Lines

```csharp
var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .WithPresence("pinging", status: "online")
    .UseConsoleLogging()
    .Build();

client.OnMessageCreated(async msg =>
{
    if (msg.Author?.Bot == true) return;
    if (msg.Content == "!ping")
        await client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Pong!" });
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

Set your token and run:

```bash
$env:DISCORD_TOKEN="your_token_here"
dotnet run
```

## Next Steps

- Read the [full first bot walkthrough](./guides/first-bot.md)
- Learn about [installation options](./installation.md)
- Explore the [API reference](../api/index.md)
- Check the [FAQ](./faq.md) for common questions
