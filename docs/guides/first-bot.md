# Your First PawSharp Bot

This tutorial walks you through creating a Discord bot from scratch. You'll have a running bot that responds to messages and handles commands by the end.

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A Discord bot token ([create one](https://discord.com/developers/applications))
- Basic knowledge of C#, async/await, and the command line

---

## Step 1: Create the Project

```bash
dotnet new console -n MyDiscordBot
cd MyDiscordBot
```

Add the PawSharp client package and a logging provider:

```bash
dotnet add package PawSharp.Client --version 1.1.0-alpha.5
dotnet add package Microsoft.Extensions.Logging.Console
```

> 💡 `PawSharp.Client` is the all-in-one package. It includes Core (entities), API (REST), Gateway (WebSocket), Cache (in-memory), and Interactions (slash commands). You don't need separate packages for common bot scenarios.

Verify the project builds:

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

## Step 2: Get a Discord Bot Token

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **New Application** and give it a name
3. Navigate to **Bot** in the left sidebar
4. Click **Reset Token** (or copy the existing token)
5. Copy the token — you'll need it in the next step
6. Under **Privileged Gateway Intents**, enable:
   - **Message Content Intent** (required for reading message content)
   - **Server Members Intent** (optional, for member events)
   - **Presence Intent** (optional, for presence events)
7. Invite the bot to a server using the **OAuth2 > URL Generator** with `bot` and `applications.commands` scopes

> ⚠️ **Keep your token secret.** Never commit it to source control, share it, or expose it in client-side code. This tutorial uses an environment variable to keep it safe.

---

## Step 3: Write the Bot

Open `Program.cs` and replace its contents with:

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException(
        "Set the DISCORD_TOKEN environment variable before running.");

var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .UseConsoleLogging()
    .Build();

client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

client.OnMessageCreated(async evt =>
{
    if (evt.Author?.IsBot == true)
        return;

    if (evt.Content == "!ping")
        await client.SendMessageAsync(evt.ChannelId, "Pong!");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

### What this code does

| Line(s) | Purpose |
|---------|---------|
| `PawSharpClientBuilder` | Fluent builder pattern to configure the client |
| `.WithToken(token)` | Sets the bot's authentication token |
| `.WithIntents(...)` | Declares which gateway events to receive |
| `.UseConsoleLogging()` | Wires up `ILogger` to the console |
| `.Build()` | Constructs the `IDiscordClient` |
| `client.OnReady(...)` | Fires once when the gateway connection is established |
| `client.OnMessageCreated(...)` | Fires on every new message the bot can see |
| `client.ConnectAsync()` | Connects to Discord (REST + Gateway) |
| `Task.Delay(Timeout.Infinite)` | Keeps the process alive indefinitely |

---

## Step 4: Run the Bot

Set your token as an environment variable and run:

```powershell
# Windows PowerShell
$env:DISCORD_TOKEN="your_token_here"
dotnet run
```

```bash
# Linux / macOS
export DISCORD_TOKEN="your_token_here"
dotnet run
```

Expected console output:

```
info: PawSharp.Gateway.GatewayClient[0]
      Connected to gateway (session_id: abc123, shard: 0/1)
info: PawSharp.Client.DiscordClient[0]
      Client connected as MyBot#1234
Logged in as MyBot#1234
```

Now type `!ping` in any channel the bot can see:

```
> !ping
< Pong!
```

To stop the bot, press `Ctrl+C`.

> ✅ **The bot is working.** You have a functional Discord bot in under 20 lines of code.

---

## Step 5: Add a Command

Let's add a prefix command system. Install the Commands package:

```bash
dotnet add package PawSharp.Commands --version 1.1.0-alpha.5
```

Now create a command module. Create a file named `GreetingModule.cs`:

```csharp
using PawSharp.Commands;
using PawSharp.Commands.Attributes;

public sealed class GreetingModule : BaseCommandModule
{
    [Command("hello")]
    [Description("Says hello to you!")]
    public async Task HelloAsync(CommandContext ctx)
    {
        await ctx.RespondAsync($"Hello, {ctx.Author.Username}! 👋");
    }

    [Command("echo")]
    [Description("Repeats what you say")]
    public async Task EchoAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync($"You said: {text}");
    }
}
```

Update `Program.cs` to register the command system:

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException(
        "Set the DISCORD_TOKEN environment variable before running.");

var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .UseConsoleLogging()
    .Build();

// Register commands with auto-discovery
client.UseCommandsWithAutoDiscovery();

client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

Run the bot again:

```
> !hello
< Hello, User! 👋

> !echo Hello world
< You said: Hello world
```

> 💡 `UseCommandsWithAutoDiscovery()` scans the calling assembly for all classes inheriting `BaseCommandModule` and registers them automatically. You can also register modules manually with `.RegisterModulesInAssembly()`.

---

## Step 6: Handle an Event

Let's log when a user joins the server. Add a member-greeting handler before `ConnectAsync`:

```csharp
client.OnGuildMemberAdded(async evt =>
{
    var welcomeChannelId = 123456789012345678ul; // Replace with your welcome channel ID
    await client.SendMessageAsync(
        welcomeChannelId,
        $"Welcome to the server, {evt.Member.User.Username}!");
});
```

> ⚠️ Replace `123456789012345678` with the actual channel ID from your Discord server (enable Developer Mode in Discord settings, right-click the channel, and copy ID).

You also need the `GuildMembers` intent:

```csharp
.WithIntents(GatewayIntents.AllNonPrivileged
    | GatewayIntents.MessageContent
    | GatewayIntents.GuildMembers)
```

And enable **Server Members Intent** in the Discord Developer Portal under your bot's settings.

---

## Complete Code Listing

Here's the final `Program.cs` with commands, events, and logging:

```csharp
using PawSharp.Client;
using PawSharp.Core.Enums;

var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException(
        "Set the DISCORD_TOKEN environment variable before running.");

var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(
        GatewayIntents.AllNonPrivileged
        | GatewayIntents.MessageContent
        | GatewayIntents.GuildMembers)
    .UseConsoleLogging()
    .Build();

// Register prefix commands
client.UseCommandsWithAutoDiscovery();

// On ready
client.OnReady(ready =>
{
    Console.WriteLine($"Logged in as {ready.User.Username}");
    return Task.CompletedTask;
});

// Message handler
client.OnMessageCreated(async evt =>
{
    if (evt.Author?.IsBot == true)
        return;

    if (evt.Content == "!ping")
        await client.SendMessageAsync(evt.ChannelId, "Pong!");
});

// Member join handler
client.OnGuildMemberAdded(async evt =>
{
    Console.WriteLine(
        $"{evt.Member.User.Username} joined {evt.GuildId}");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

And `GreetingModule.cs`:

```csharp
using PawSharp.Commands;
using PawSharp.Commands.Attributes;

public sealed class GreetingModule : BaseCommandModule
{
    [Command("hello")]
    [Description("Says hello to you!")]
    public async Task HelloAsync(CommandContext ctx)
    {
        await ctx.RespondAsync($"Hello, {ctx.Author.Username}! 👋");
    }

    [Command("echo")]
    [Description("Repeats what you say")]
    public async Task EchoAsync(CommandContext ctx, [RemainingText] string text)
    {
        await ctx.RespondAsync($"You said: {text}");
    }

    [Command("ping")]
    [Description("Check if the bot is responsive")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong! 🏓");
    }
}
```

---

## Troubleshooting

### Bot doesn't come online
- Verify the token is correct and set as the `DISCORD_TOKEN` environment variable
- Check that the bot has the correct intents enabled in the Developer Portal
- Ensure the bot is invited to a server with the `bot` scope

### `!ping` doesn't respond
- **Message Content Intent** must be enabled in the Developer Portal
- The `GatewayIntents.MessageContent` flag must be set in `WithIntents()`
- Check that the bot has permission to send messages in the channel

### Command modules not found
- Verify the module class is `public` and inherits `BaseCommandModule`
- Call `client.UseCommandsWithAutoDiscovery()` before `ConnectAsync()`
- If registering manually, use `client.UseCommands(modules => modules.RegisterModulesInAssembly(...))`

### `OnGuildMemberAdded` not firing
- Enable **Server Members Intent** in the Developer Portal (under Bot > Privileged Gateway Intents)
- Add `GatewayIntents.GuildMembers` to `WithIntents()`
- The bot must have the `guilds` scope when invited

### Build errors
- Ensure .NET 10 SDK is installed: `dotnet --version` should be `10.0.x`
- Run `dotnet restore` before `dotnet build`
- Check all PawSharp package versions match

### Rate limited / 429 errors
- The built-in rate limiter handles this automatically with bucket tracking
- If you see repeated 429s, reduce your request frequency or check the `AdvancedRateLimiter` configuration

---

## Next Steps

- [Installation guide](../installation.md) &mdash; detailed package reference
- [Getting Started](../getting-started.md) &mdash; architecture and package overview
- [Gateway Events](../guides/gateway.md) &mdash; handling 60+ Discord events
- [Slash Commands](../slash-commands.md) &mdash; building modern interactions
- [Caching](../guides/caching.md) &mdash; optimizing with Redis or in-memory cache
- [Voice](../guides/voice.md) &mdash; audio streaming and DAVE E2EE
- [Error Handling](../guides/error-handling.md) &mdash; structured exception handling
