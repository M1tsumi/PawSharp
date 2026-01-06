# PawSharp

A Discord API wrapper for .NET 8.0 focused on modularity and transparency. This project is in active development and not yet ready for production use.

## Current Status

Version 0.5.0-alpha2 (Pre-release)

PawSharp now provides comprehensive support for modern Discord features including message components, slash commands, and thread/forum operations. The library maintains backwards compatibility with v0.5.0-alpha1 while adding significant new functionality.

### Officially Supported Features (v0.5.0-alpha2)

**Gateway & Events:**
- Gateway connection with WebSocket and automatic heartbeat
- Event handling for core events: READY, MESSAGE_CREATE, MESSAGE_UPDATE, MESSAGE_DELETE, GUILD_CREATE, GUILD_UPDATE, GUILD_DELETE, CHANNEL_CREATE, CHANNEL_UPDATE, CHANNEL_DELETE, GUILD_MEMBER_ADD, GUILD_MEMBER_UPDATE, GUILD_MEMBER_REMOVE, INTERACTION_CREATE
- Additional events: TYPING_START, MESSAGE_REACTION_ADD, MESSAGE_REACTION_REMOVE, MESSAGE_REACTION_REMOVE_ALL, PRESENCE_UPDATE, CHANNEL_PINS_UPDATE, GUILD_BAN_ADD, GUILD_BAN_REMOVE, VOICE_STATE_UPDATE
- **NEW:** Thread events: THREAD_CREATE, THREAD_UPDATE, THREAD_DELETE, THREAD_LIST_SYNC, THREAD_MEMBER_UPDATE, THREAD_MEMBERS_UPDATE

**REST API Endpoints:**
- **Users:** GetUser, ModifyCurrentUser, GetCurrentUserGuilds, LeaveGuild
- **Messages:** CreateMessage, GetMessage, EditMessage, DeleteMessage, GetChannelMessages (with pagination), BulkDeleteMessages, PinMessage, UnpinMessage, GetPinnedMessages, TriggerTypingIndicator
- **Channels:** GetChannel, ModifyChannel, DeleteChannel, CreateGuildChannel, GetChannelInvites, CreateChannelInvite, DeleteChannelPermission
- **Guilds:** GetGuild, CreateGuild, ModifyGuild, DeleteGuild, GetGuildChannels, GetGuildMembers, GetGuildMember, AddGuildMember, ModifyGuildMember, RemoveGuildMember, GetGuildBans, GetGuildBan, CreateGuildBan, RemoveGuildBan
- **Roles:** GetGuildRoles, CreateGuildRole, ModifyGuildRole, DeleteGuildRole, AddGuildMemberRole, RemoveGuildMemberRole
- **Interactions:** CreateInteractionResponse, EditOriginalInteractionResponse, DeleteOriginalInteractionResponse
- **Reactions:** CreateReaction, DeleteOwnReaction, DeleteUserReaction
- **NEW: Application Commands:** GetGlobalApplicationCommands, CreateGlobalApplicationCommand, GetGlobalApplicationCommand, EditGlobalApplicationCommand, DeleteGlobalApplicationCommand, GetGuildApplicationCommands, CreateGuildApplicationCommand, GetGuildApplicationCommand, EditGuildApplicationCommand, DeleteGuildApplicationCommand, BulkOverwriteGlobalApplicationCommands, BulkOverwriteGuildApplicationCommands
- **NEW: Threads:** CreateThread, CreateThreadFromMessage, CreateThreadInForum, JoinThread, AddThreadMember, LeaveThread, RemoveThreadMember, GetThreadMember, GetThreadMembers, GetActiveThreads, GetPublicArchivedThreads, GetPrivateArchivedThreads, GetJoinedPrivateArchivedThreads

**Core Features:**
- In-memory entity caching
- Dependency injection support
- Core entity models (Users, Guilds, Channels, Messages, Roles, Members, Bans, Invites)
- **NEW:** Message components (Buttons, Select Menus, Action Rows)
- **NEW:** Slash commands and interactions with full type safety
- **NEW:** Thread and forum entities with comprehensive metadata
- **NEW:** Application command models with options and choices
- Snowflake ID serialization
- Basic rate limiting

### Backwards Compatibility

v0.5.0-alpha2 maintains full backwards compatibility with v0.5.0-alpha1. All existing APIs and features continue to work as expected.
- Stage channels
- Webhook management
- Audit logs
- Comprehensive tests
- Redis caching
- Any other advanced Discord features not listed above

## Known Issues

The library doesn't handle all edge cases gracefully and may throw unexpected exceptions. The advanced rate limiting needs more testing in production environments. Reconnection logic is basic and might not handle all scenarios. Cache eviction hasn't been stress-tested with large guilds. Documentation is incomplete, and we only have basic examples.

## Prerequisites

- .NET 8.0 SDK
- Discord Bot Token (get one from the [Discord Developer Portal](https://discord.com/developers/applications))
- Understanding of async/await in C#

## Installation

PawSharp isn't on NuGet yet. You'll need to clone and build from source:

```bash
git clone <repository-url>
cd PawSharp
dotnet build
```

Then reference the projects you need in your application.

### Basic Bot Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;in your application.

## Basicp.Cache.Providers;
using PawSharp.API.Clients;
using PawSharp.Gateway.Events;
using PawSharp.API.Models;

// Set up dependency injection
var services = new ServiceCollection();
services.AddLogging(config => config.AddConsole());
services.AddSingleton<PawSharpOptions>(new PawSharpOptions
{
    Token = "YOUR_BOT_TOKEN",
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});
services.AddSingleton<IEntityCache, MemoryCacheProvider>();
services.AddHttpClient<IDiscordRestClient, DiscordRestClient>();
services.AddSingleton<DiscordClient>();
services.AddSingleton<CacheManager>();

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<DiscordClient>();
var cacheManager = serviceProvider.GetRequiredService<CacheManager>();

// Enable auto-caching
cacheManager.SubscribeToGateway(client.Gateway);

// Register event handlers
client.Gateway.Events.On<ReadyEvent>("READY", (e) =>
{
    Console.WriteLine($"✅ Logged in as {e.User.Username}!");
});

client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async (e) =>
{
    if (e.Author.Bot == true) return;
    
    if (e.Content.ToLoweLogged in as {e.User.Username}");
});

client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async (e) =>
{
    if (e.Author.Bot == true) return;
    
    if (e.Content.ToLower() == "!ping")
    {
        await client.Rest.CreateMessageAsync(e.ChannelId, new CreateMessageRequest
        {
            Content = "Pong!"
        });
    }
});

// Connect to Discord
await client.ConnectAsync();
await Task.Delay(-1); // Keep running
```

Note: This example shows the happy path. Production bots need proper error handling. Gateway events can have nullable fields, so check before accessing nested properties. The cache needs to be populated by gateway events before you try to retrieve entities.

##wSharp.API** - REST API client with HTTP abstractions and rate limiting
- **PawSharp.Gateway** - WebSocket gateway, heartbeat manager, and event dispatcher
- **PawSharp.Cache** - Entity caching with pluggable providers (memory, Redis-ready)
- **PawSharp.Client** - High-level orchestration layer that ties everything together

### Supplementary Packages

- **PawSharp.Interactions** - Builders for slash commands and message components (partial)
- **PawSharp.Voice** - Voice gateway (planned, not started)

### Design Philosophy

1. **Modular** - Use only what you need. Want just the REST client? Reference only PawSharp.API.
2. **Transparent** - Raw JSON events are accessible alongside parsed objects.
3. **DI-First** - Designed to work seamlessly with Microsoft.Extensions.DependencyInjection.
4. **Type-Safe** - Strongly-typed entities with nullable reference types enabled.

## 📚 Basic Usage Guide

### Gateway Events

The event system supports both strongly-typed and raw JSON handlers:

```csharp
// Typed event handling (recommended)
client.Gateway.Events.On<ReadyEvent>("READY", (e) =>
{
    Console.Wrs
client.Gateway.Events.On<ReadyEvent>("READY", (e) =>
{
    Console.WriteLine($"Connected as {e.User.Username}");
    Console.WriteLine($"Guilds: {e.Guilds.Count}");
});

client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async (e) =>
{
    if (e.Author.Bot == true) return;
    Console.WriteLine($"[{e.ChannelId}] {e.Author.Username}: {e.Content}");
});

// Raw JSON (useful for debugging or unsupported events)
client.Gateway.Events.OnRaw("MESSAGE_REACTION_ADD", (json) =>
{
    Console.WriteLine($"Raw: {json}");
});
```

### REST API
var message = await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Hello from PawSharp!"
});

// Edit a message
if (message != null)
{
    await client.Rest.EditMessageAsync(channelId, message.Id, new EditMessageRequest
    {
        Content = "Updated message"
    });
}

// Get channel information
var channel = await client.Rest.GetChannelAsync(channelId);
if (channel != null)
{
    Console.WriteLine($"Channel: {channel.Name}");
}

// Manage guild members
await client.Rest.ModifyGuildMemberAsync(guildId, userId, new ModifyGuildMemberRequest
{
    Nick = "NewNickname"
});
```

### Working with the Cache

```csharp
// Cache is automatically populated from gateway events when using CacheManager
var CachingProvider.GetRequiredService<CacheManager>();
cacheManager.SubscribeToGateway(client.Gateway);

// Retrieauto-populates from gateway events
var cacheManager = serviceProvider.GetRequiredService<CacheManager>();
cacheManager.SubscribeToGateway(client.Gateway);

// Retrieve entities
var user = client.Cache.GetUser(userId);
var guild = client.Cache.GetGuild(guildId);
var allGuilds = client.Cache.GetAllGuilds();

// Stats
var entityCount = client.Cache.GetEntityCount();
Console.WriteLine($"Cached {entityCount} entities");
```

## Roadmap

Short term (v0.6-0.7): Complete interaction handling refinements, add proper error handling, write comprehensive tests, improve reconnection logic, add sharding support

Medium term (v0.8-0.9): Webhooks, audit logs, modals, scheduled events, better sharding, voice gateway foundation

Long term (v1.0+): Voice support, auto-moderation, stage channels, Redis caching, performance optimizations, documentation site, NuGet release

## Performance Notes

Memory usage is reasonable for bots under 1000 guilds. Larger bots should implement custom caching. Rate limiting respects Discord's limits but isn't optimized for burst requests. Event handlers run sequentially per type - parallel processing planned for later.

##t test

# Run specific test project
dotnet test tests/PawSharp.Core.Tests
dotnet test tests/PawSharp.API.Tests
```

**Current Test Coverage**: ~15% (needs significant improvement)

## 🤝 Contributing

Contributions are welcome, especially in these areas:

1. **Testing** - Write integration and unit tests
2. **Documentation** - Improve XML docs and add more examples
3. **Bug Fixes** - Report and fix issues you encounter
4. **Feature Implementation** - Help complete partially-implemented features

### Development Setup

1. Clone the repository
2. Install .NET 8.0 SDK
3. Run `dotnet build` to ensure everything compiles
4. Run `dotnet test` to check existing tests
5. Create a feature branch and submit a PR
dotnet test  # run all tests
dotnet test tests/PawSharp.Core.Tests  # specific project
```

Current test coverage is around 15%, which needs significant improvement.

## Contributing

We need help with testing, documentation, bug fixes, and completing partial features. To contribute:

1. Clone the repo and install .NET 8.0 SDK
2. Run `dotnet build` and `dotnet test`
3. Create a feature branch
4. Submit a PR

See [CONTRIBUTING.md](CONTRIBUTING.md) for more details.

## License

Apache License 2.0 - see [LICENSE](LICENSE) file.

## Resources

- [Discord API Documentation](https://discord.com/developers/docs/intro)
- [Discord Developer Portal](https://discord.com/developers/applications)
- [Discord API Discord Server](https://discord.gg/discord-api)

## Support

Report bugs and request features via GitHub Issues. Ask questions in GitHub Discussions. Community Discord coming soon.

## Disclaimer

This is an independent project, not affiliated with Discord Inc. Use at your own risk and follow Discord's Terms of Service and Developer Policy.

## Credits

Built with inspiration from Discord.NET and DSharpPlus. Thanks to the Discord API community.

---

Status: Alpha 1 | Stable for Supported Features