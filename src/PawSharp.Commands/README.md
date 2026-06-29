# PawSharp.Commands

PawSharp.Commands is a comprehensive, attribute-based command framework for Discord bots built on PawSharp.Client. It provides a clean, extensible API for prefix commands, slash commands, and context menu commands with advanced features like type conversion, middleware, dependency injection, and more.

## Features

### Core Features
- **Attribute-driven command definitions** - Use `[Command]`, `[SlashCommand]`, `[UserContextMenu]`, `[MessageContextMenu]` attributes
- **Async command handlers** - Full async/await support
- **Module organization** - Organize commands in modules with lifecycle hooks
- **Aliasing** - Multiple command names for the same handler
- **Descriptions** - Rich metadata for help systems

### Advanced Features
- **Type conversion** - Automatic conversion of string arguments to C# types (int, bool, ulong, DateTime, etc.)
- **Advanced argument parsing** - Quote handling, escape characters, variadic arguments
- **Optional parameters** - `[Optional]` attribute for parameters with default values
- **Remaining arguments** - `[Remaining]` attribute to capture all remaining text
- **Middleware pipeline** - Global and command-specific middleware for logging, auditing, timeouts
- **Dependency injection** - Full DI support with Microsoft.Extensions.DependencyInjection
- **Precondition system** - Reusable permission checks (guild, DM, NSFW, role, owner, cooldowns)
- **Compiled delegates** - Performance optimization using compiled delegates instead of reflection
- **Structured error handling** - Error codes and user-friendly error messages
- **Help system** - Built-in help command generation
- **Command discovery** - API for discovering and querying commands

### Slash Commands

- **Auto-registration** - Automatic Discord API registration with bulk operations
- **Rich options** - Choices, min/max values, min/max lengths, channel type restrictions
- **Subcommands and groups** - Hierarchical command organization
- **Autocomplete support** - `[SlashAutocomplete]` attribute
- **Type mapping** - Support for User, Channel, Role, Mentionable, Attachment types
- **Localization** - `[SlashLocalizedName]` and `[SlashLocalizedDescription]` attributes
- **NSFW support** - `[SlashNsfw]` attribute
- **DM permissions** - `[SlashDmPermission]` attribute
- **Default permissions** - `[SlashDefaultPermission]` attribute

### Context Menu Commands
- **User context menus** - Right-click on users
- **Message context menus** - Right-click on messages
- **Auto-registration** - Automatic Discord API registration

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`
- `PawSharp.API`
- `PawSharp.Core`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`

## Installation

```bash
dotnet add package PawSharp.Commands --version 1.1.0-alpha.4
```

## Quick Start

### Basic Prefix Commands

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

### Type Conversion

```csharp
[Command("ban")]
[Description("Ban a user by ID")]
public async Task BanAsync(CommandContext ctx, ulong userId, string reason = "No reason provided")
{
    // userId is automatically converted from string to ulong
    await ctx.ReplyAsync($"Banning user {userId} for: {reason}");
}
```

### Advanced Argument Parsing

```csharp
[Command("echo")]
[Description("Echo back a message (supports quotes)")]
public async Task EchoAsync(CommandContext ctx, [Remaining] string message)
{
    // Captures all remaining text, including quotes
    await ctx.ReplyAsync(message);
}

// Usage: !echo "hello world" -> outputs: hello world
```

### Optional Parameters

```csharp
[Command("greet")]
[Description("Greet a user")]
public async Task GreetAsync(CommandContext ctx, string name, [Optional] string title = "")
{
    var greeting = string.IsNullOrEmpty(title) ? $"Hello, {name}!" : $"Hello, {title} {name}!";
    await ctx.ReplyAsync(greeting);
}
```

### Slash Commands

```csharp
[SlashCommand("greet", "Greets a user")]
public async Task GreetAsync(
    InteractionCreateEvent interaction,
    [SlashOption("name", "User name")] string name,
    [SlashOption("title", "User title", Required = false)] string title = "")
{
    var greeting = string.IsNullOrEmpty(title) ? $"Hello, {name}!" : $"Hello, {title} {name}!";
    await interaction.ResponseAsync(new InteractionResponse
    {
        Type = InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionApplicationCommandCallbackData { Content = greeting }
    });
}

// Register slash commands
await commands.BulkRegisterSlashModulesAsync(client, modules, applicationId, guildId: guildId);
```

### Slash Commands with Choices

```csharp
[SlashCommand("role", "Assign a role")]
public async Task RoleAsync(
    InteractionCreateEvent interaction,
    [SlashOption("role", "Role to assign")]
    [SlashChoice("Admin", "admin")]
    [SlashChoice("Moderator", "mod")]
    [SlashChoice("Member", "member")]
    string role)
{
    await interaction.ResponseAsync(new InteractionResponse
    {
        Type = InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionApplicationCommandCallbackData { Content = $"Assigning role: {role}" }
    });
}
```

### Context Menu Commands

```csharp
[UserContextMenu("Ban User")]
public async Task BanUserAsync(InteractionCreateEvent interaction)
{
    var userId = interaction.Data?.TargetId;
    // Handle ban logic
    await interaction.ResponseAsync(new InteractionResponse
    {
        Type = InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionApplicationCommandCallbackData { Content = "User banned!" }
    });
}

[MessageContextMenu("Delete Message")]
public async Task DeleteMessageAsync(InteractionCreateEvent interaction)
{
    // Handle message deletion
    await interaction.ResponseAsync(new InteractionResponse
    {
        Type = InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionApplicationCommandCallbackData { Content = "Message deleted!" }
    });
}

// Register context menu commands
await commands.BulkRegisterContextMenuModulesAsync(client, modules, applicationId, guildId: guildId);
```

### Preconditions

```csharp
using PawSharp.Commands.Preconditions;
using PawSharp.Commands.Permissions;

[RequireGuild]
[RequirePermissions(DiscordPermissions.BanMembers | DiscordPermissions.KickMembers)]
[Cooldown(maxUses: 5, perSeconds: 60, bucketType: CooldownBucketType.User)]
[Command("mod")]
[Description("Moderator command")]
public async Task ModCommandAsync(CommandContext ctx)
{
    await ctx.ReplyAsync("Moderation command executed!");
}

[RequireNsfw]
[Command("nsfw")]
[Description("NSFW-only command")]
public async Task NsfwCommandAsync(CommandContext ctx)
{
    await ctx.ReplyAsync("NSFW content!");
}

[RequireOwner(123456789UL)]
[Command("admin")]
[Description("Owner-only command")]
public async Task AdminCommandAsync(CommandContext ctx)
{
    await ctx.ReplyAsync("Admin command executed!");
}
```

### Dependency Injection

```csharp
// Setup DI
var services = new ServiceCollection();
services.AddCommands(prefix: "!", options =>
{
    options.WithPrefix("!");
    options.WithCaseSensitivity(false);
    options.WithExecutionTimeout(TimeSpan.FromMinutes(5));
    options.WithLoggingMiddleware();
    options.WithAuditMiddleware();
});

// Register modules
services.AddTransient<MyCommandModule>();

var serviceProvider = services.BuildServiceProvider();
var commands = serviceProvider.GetRequiredService<CommandsExtension>();

// Register with client
commands.RegisterModuleAsync(client, serviceProvider.GetRequiredService<MyCommandModule>());
```

### Custom Middleware

```csharp
public class CustomMiddleware : IMiddleware
{
    public async Task InvokeAsync(CommandContext context, Func<Task> next)
    {
        // Pre-execution logic
        Console.WriteLine($"Command {context.CommandName} invoked by {context.User.Id}");
        
        await next();
        
        // Post-execution logic
        Console.WriteLine($"Command {context.CommandName} completed");
    }
}

// Register middleware
services.AddCommandMiddleware<CustomMiddleware>();
```

### Custom Type Converters

```csharp
public class TimeSpanConverter : SyncTypeConverter<TimeSpan>
{
    protected override TypeConverterResult<TimeSpan> ConvertSync(string value, CommandContext context)
    {
        if (TimeSpan.TryParse(value, out var result))
            return TypeConverterResult<TimeSpan>.FromSuccess(result);
        return TypeConverterResult<TimeSpan>.FromError($"Unable to parse '{value}' as a time span");
    }
}

// Register custom converter
commands.TypeConverterService.RegisterConverter(new TimeSpanConverter());
```

### Help System

```csharp
// The built-in HelpModule provides automatic help generation
services.AddTransient<HelpModule>();

// Register the module
commands.RegisterModule(client, serviceProvider.GetRequiredService<HelpModule>());

// Usage: !help - shows all commands
// Usage: !help ping - shows help for specific command
```

### Command Discovery

```csharp
var discovery = new CommandDiscoveryService(commands);

// Get all commands
var allCommands = discovery.GetAllCommands();

// Get specific command
var pingCommand = discovery.GetCommand("ping");

// Search commands
var matchingCommands = discovery.SearchCommands("ban");

// Discover command modules from assembly
var moduleTypes = CommandDiscoveryService.DiscoverCommandModules(Assembly.GetExecutingAssembly());
```

### Error Handling with Structured Errors

```csharp
using PawSharp.Commands.Errors;

commands.CommandErrored = async args =>
{
    var error = CommandError.UnexpectedError(args.Exception);
    await args.Context.RespondAsync($"Error: {error.Message} (Code: {error.Code})");
};
```

### Slash Command Localization

```csharp
[SlashCommand("greet", "Greets a user")]
[SlashLocalizedName("en-US", "greet")]
[SlashLocalizedName("fr-FR", "saluer")]
[SlashLocalizedDescription("en-US", "Greets a user")]
[SlashLocalizedDescription("fr-FR", "Salue un utilisateur")]
public async Task GreetAsync(
    InteractionCreateEvent interaction,
    [SlashOption("name", "User name")]
    [SlashLocalizedName("en-US", "name")]
    [SlashLocalizedName("fr-FR", "nom")]
    string name)
{
    // ...
}
```

### Slash Command Permissions

```csharp
[SlashCommand("admin", "Admin command")]
[SlashNsfw] // NSFW-only
[SlashDmPermission(false)] // Disable in DMs
[SlashDefaultPermission(false)] // Disabled by default
public async Task AdminCommandAsync(InteractionCreateEvent interaction)
{
    // ...
}
```

## Module Lifecycle

```csharp
public class MyModule : BaseCommandModule
{
    public override Task InitializeAsync()
    {
        // Called during module registration
        Console.WriteLine("Module initialized!");
        return Task.CompletedTask;
    }

    public override Task BeforeExecutionAsync(CommandContext ctx)
    {
        // Called before each command in this module
        Console.WriteLine($"Before: {ctx.CommandName}");
        return Task.CompletedTask;
    }

    public override Task AfterExecutionAsync(CommandContext ctx)
    {
        // Called after each command in this module
        Console.WriteLine($"After: {ctx.CommandName}");
        return Task.CompletedTask;
    }
}
```

## Error Handling

```csharp
commands.CommandErrored = async args =>
{
    var errorMessage = args.Exception.Message;
    await args.Context.ReplyAsync($"Error: {errorMessage}");
};
```

## Typical Use Cases

- **Complex prefix command bots** - With type conversion and advanced parsing
- **Moderation bots** - With permission checks and cooldowns
- **Slash command bots** - With rich options and autocomplete
- **Context menu bots** - For user and message interactions
- **Enterprise bots** - With DI, middleware, and logging

## Related Packages

- `PawSharp.Client`: the host client for command execution
- `PawSharp.Interactions`: slash command and component workflows
- `PawSharp.Core`: shared models and enums
- `PawSharp.API`: REST API client

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## Support

- Join the [PawSharp Discord](https://discord.gg/6Z8X8cCHXs) for help, discussion, and community.
- Report bugs or request features via [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues).
- Start a discussion on [GitHub Discussions](https://github.com/M1tsumi/PawSharp/discussions).

## License

MIT. See [../../LICENSE](../../LICENSE).
