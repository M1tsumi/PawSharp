# PawSharp.Commands

Modern command framework for Discord bots with attribute-based registration and async support.

PawSharp.Commands provides a clean, extensible command system for Discord bots. Built with modern .NET patterns, it supports async operations, dependency injection, and modular command organization.

## Features

- Attribute-based command registration
- Full async/await support with RegisterModuleAsync()
- Automatic command enumeration with GetRegisteredCommands()
- Smart argument parsing with type conversion
- Modular command organization
- Alias support for commands
- Execution hooks (before/after)
- Dependency injection in command modules
- Strongly-typed command contexts

## 📦 Installation

```bash
dotnet add package PawSharp.Commands --version 6.1.0-alpha-1
```

## 🚀 Quick Start

```csharp
using PawSharp.Client;
using PawSharp.Commands.Extensions;

// Create your Discord client
var client = new DiscordClient(new PawSharpOptions { Token = "your-token" });

// Enable commands with prefix
var commands = client.UseCommands("!");

// Create a command module
public class GeneralCommands : BaseCommandModule
{
    [Command("ping")]
    [Description("Check bot latency")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong! 🏓");
    }

    [Command("echo")]
    [Aliases("say", "repeat")]
    [Description("Echo back the provided text")]
    public async Task EchoAsync(CommandContext ctx, string text)
    {
        await ctx.RespondAsync(text);
    }

    [Command("userinfo")]
    [Description("Get information about a user")]
    public async Task UserInfoAsync(CommandContext ctx, User? user = null)
    {
        user ??= ctx.User;
        await ctx.RespondAsync($"User: {user.Username}#{user.Discriminator}");
    }
}

// Register the module
await commands.RegisterModuleAsync(client, new GeneralCommands());
```

## 📋 Command Registration

### Basic Commands

```csharp
[Command("greet")]
public async Task GreetAsync(CommandContext ctx)
{
    await ctx.RespondAsync($"Hello, {ctx.User.Username}!");
}
```

### Commands with Parameters

```csharp
[Command("ban")]
[Description("Ban a user from the guild")]
public async Task BanAsync(CommandContext ctx, User user, string reason = "No reason provided")
{
    // Implementation here
    await ctx.RespondAsync($"Banned {user.Username} for: {reason}");
}
```

### Commands with Aliases

```csharp
[Command("avatar")]
[Aliases("pfp", "profilepic")]
[Description("Get a user's avatar")]
public async Task AvatarAsync(CommandContext ctx, User? user = null)
{
    user ??= ctx.User;
    await ctx.RespondAsync(user.AvatarUrl);
}
```

## 🔧 Advanced Features

### Async Module Initialization

```csharp
public class DatabaseCommands : BaseCommandModule
{
    private readonly MyDatabaseService _database;

    public DatabaseCommands(MyDatabaseService database)
    {
        _database = database;
    }

    public override async Task InitializeAsync()
    {
        // Load command data from database
        await _database.ConnectAsync();
        await base.InitializeAsync();
    }

    [Command("stats")]
    public async Task StatsAsync(CommandContext ctx)
    {
        var stats = await _database.GetStatsAsync();
        await ctx.RespondAsync($"Total users: {stats.UserCount}");
    }
}
```

### Command Discovery

```csharp
// Get all registered commands
var registeredCommands = commands.GetRegisteredCommands();

foreach (var cmd in registeredCommands)
{
    Console.WriteLine($"{cmd.Name}: {cmd.Description}");
    if (cmd.Aliases.Any())
    {
        Console.WriteLine($"  Aliases: {string.Join(", ", cmd.Aliases)}");
    }
}
```

### Execution Hooks

```csharp
public class LoggingModule : BaseCommandModule
{
    public override async Task BeforeExecuteAsync(CommandContext ctx)
    {
        Console.WriteLine($"{ctx.User.Username} executed: {ctx.CommandName}");
        await base.BeforeExecuteAsync(ctx);
    }

    public override async Task AfterExecuteAsync(CommandContext ctx)
    {
        Console.WriteLine($"Command {ctx.CommandName} completed");
        await base.AfterExecuteAsync(ctx);
    }
}
```

## 📖 Command Context

The `CommandContext` provides access to:

```csharp
public async Task ExampleAsync(CommandContext ctx)
{
    // Message information
    var message = ctx.Message;
    var channel = ctx.Channel;
    var guild = ctx.Guild;

    // User information
    var user = ctx.User;
    var member = ctx.Member;

    // Command information
    var commandName = ctx.CommandName;
    var rawArgs = ctx.RawArguments;
    var parsedArgs = ctx.Arguments;

    // Response methods
    await ctx.RespondAsync("Reply to the user");
    await ctx.RespondDMAsync("Send a DM");
}
```

## 🔄 Dependency Injection

```csharp
// Register services
services.AddSingleton<MyService>();

// Inject into command modules
public class MyCommands : BaseCommandModule
{
    private readonly MyService _service;

    public MyCommands(MyService service)
    {
        _service = service;
    }

    [Command("service")]
    public async Task UseServiceAsync(CommandContext ctx)
    {
        var result = await _service.DoSomethingAsync();
        await ctx.RespondAsync(result);
    }
}
```

## ⚙️ Configuration

```csharp
// Configure command system
var commands = client.UseCommands(new CommandConfiguration
{
    Prefix = "!",
    CaseSensitive = false,
    EnableMentionPrefix = true,
    IgnoreBots = true,
    RequiredPermissions = Permissions.UseSlashCommands
});
```

## 🛠️ Error Handling

```csharp
public class ErrorHandlingModule : BaseCommandModule
{
    public override async Task OnErrorAsync(CommandContext ctx, Exception ex)
    {
        await ctx.RespondAsync($"❌ An error occurred: {ex.Message}");
        await base.OnErrorAsync(ctx, ex);
    }
}
```

## 🤝 Dependencies

- **PawSharp.Client** - Discord client integration
- **PawSharp.Core** - Entity models
- **.NET 8.0** - Modern runtime
- **Microsoft.Extensions.DependencyInjection** - DI container

## 📚 Related Packages

- **[PawSharp.Interactions](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Interactions)** - Slash commands
- **[PawSharp.Interactivity](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Interactivity)** - Interactive components
- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - Main client

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details.