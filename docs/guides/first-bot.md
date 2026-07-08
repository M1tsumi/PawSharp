# Your First Bot

## Prerequisites

- .NET 10.0 SDK or later
- A Discord bot token
- Basic C# knowledge

## Create a New Project

```bash
dotnet new console -n MyDiscordBot
cd MyDiscordBot
dotnet add package PawSharp.Client
dotnet add package Microsoft.Extensions.Logging.Console
```

## Complete Bot (DI Approach)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Models;
using PawSharp.Gateway.Events;

var options = new PawSharpOptions
{
    Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
        ?? throw new InvalidOperationException("Set DISCORD_TOKEN env var"),
    Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged
        | PawSharp.Core.Enums.GatewayIntents.MessageContent,
};
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .SetupPawSharp(options);

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();

client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

client.OnMessageCreated(msg =>
{
    if (msg.Content == "!ping")
        return client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "Pong!" });
    return Task.CompletedTask;
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

## Non-DI Approach (PawSharpClientBuilder)

```csharp
var client = new PawSharpClientBuilder()
    .WithToken(Environment.GetEnvironmentVariable("DISCORD_TOKEN")!)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .UseConsoleLogging()
    .UseMemoryCache()
    .Build();

client.OnMessageCreated(async msg =>
{
    if (!msg.Author.IsBot && msg.Content == "!hello")
        await client.SendMessageAsync(msg.ChannelId, $"Hello, {msg.Author.Username}!");
});

await client.ConnectAsync();
```

## Set Your Token

```bash
$env:DISCORD_TOKEN="your_token_here"
dotnet run
```

## Core Concepts

- **DiscordClient** — Main entry point; access `client.Rest` (API), `client.Gateway` (events), `client.Cache` (cached data)
- **Intents** — Control which events you receive; enable only what you need
- **Async/Await** — All I/O operations are async
- **Snowflakes** — Discord uses `ulong` for all IDs

See the [Gateway](./gateway.md) and [Events](./events.md) guides for more details.
