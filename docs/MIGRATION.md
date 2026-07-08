# Migration Guide

How to migrate between PawSharp versions and from other Discord libraries.

---

## Table of Contents

1. [Migrating from DSharpPlus](#migrating-from-dsharpplus)
2. [Migrating from Discord.Net](#migrating-from-discordnet)
3. [PawSharp Version History](#pawsharp-version-history)
4. [Breaking Changes by Version](#breaking-changes-by-version)
5. [General Migration Notes](#general-migration-notes)

---

## Migrating from DSharpPlus

### Conceptual mapping

| DSharpPlus | PawSharp |
|---|---|
| `DiscordClient` | `IDiscordClient` / `DiscordClient` |
| `DiscordConfiguration` | `PawSharpOptions` / `PawSharpClientBuilder` |
| `DiscordShardedClient` | Built-in sharding via `.WithSharding()` |
| `DiscordMessage` | `Message` (from `PawSharp.Core.Entities`) |
| `DiscordUser` | `User` |
| `DiscordGuild` | `Guild` |
| `DiscordChannel` | `Channel` |
| `DiscordEmbed` | `Embed` |
| `DiscordMessageBuilder` | `MessageProperties` |
| `DiscordButtonComponent` | `ButtonComponent` |
| `DiscordSelectComponent` | `SelectMenu` |
| `DiscordModal` | `Modal` |
| `DiscordInteraction` | `Interaction` |
| `CommandsNextExtension` | `CommandsExtension` |
| `SlashCommandsExtension` | `InteractionHandler` |
| `Events` | `OnMessageCreated()`, etc. |
| `Socket.MessageCreated` | C# event pattern |
| `DiscordVoiceClient` | `VoiceClient` |
| `VoiceNextExtension` | Built into `PawSharp.Voice` |

### Key differences

**Builder pattern instead of configuration object:**

```csharp
// DSharpPlus
var config = new DiscordConfiguration
{
    Token = token,
    TokenType = TokenType.Bot,
    Intents = DiscordIntents.AllUnprivileged
};
var client = new DiscordClient(config);

// PawSharp
var client = new PawSharpClientBuilder()
    .WithToken(token)
    .WithIntents(GatewayIntents.AllNonPrivileged)
    .Build();
```

**Typed events instead of socket handlers:**

```csharp
// DSharpPlus
client.MessageCreated += (s, e) =>
{
    Console.WriteLine(e.Message.Content);
    return Task.CompletedTask;
};

// PawSharp
client.OnMessageCreated(async evt =>
{
    Console.WriteLine(evt.Content);
});
```

**Rest client is separate and explicit:**

```csharp
// DSharpPlus
await client.SendMessageAsync(channelId, "Hello");

// PawSharp — via the Rest property
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest { Content = "Hello" });
// Or via the convenience method on DiscordClient
await client.SendMessageAsync(channelId, "Hello");
```

**Commands — attribute-based, not command-next:**

```csharp
// DSharpPlus
public class MyModule : BaseCommandModule
{
    [Command("ping")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }
}

// PawSharp
public class MyModule : BaseCommandModule
{
    [Command("ping")]
    public async Task PingAsync(CommandContext ctx)
    {
        await ctx.RespondAsync("Pong!");
    }
}
```

The API surface is intentionally similar for commands, but wiring differs:

```csharp
// DSharpPlus — separate registration
var commands = client.UseCommandsNext(config);
commands.RegisterCommands<MyModule>();

// PawSharp — extensions on the client
var commands = client.GetCommandsExtension();
await commands.RegisterModulesInAssembly(typeof(MyModule).Assembly);
```

### Package equivalents

| DSharpPlus package | PawSharp package |
|---|---|
| `DSharpPlus` | `PawSharp.Client` |
| `DSharpPlus.CommandsNext` | `PawSharp.Commands` |
| `DSharpPlus.SlashCommands` | `PawSharp.Interactions` |
| `DSharpPlus.Interactivity` | `PawSharp.Interactivity` |
| `DSharpPlus.VoiceNext` | `PawSharp.Voice` |
| (none) | `PawSharp.Cache` |
| (none) | `PawSharp.API` |

---

## Migrating from Discord.Net

### Conceptual mapping

| Discord.Net | PawSharp |
|---|---|
| `DiscordSocketClient` | `IDiscordClient` |
| `DiscordRestClient` | `IDiscordRestClient` |
| `SocketMessage` | `Message` |
| `SocketGuildUser` | `GuildMember` / `User` |
| `SocketGuild` | `Guild` |
| `SocketTextChannel` | `Channel` (with type discriminator) |
| `IMessageChannel` | Uses concrete channel IDs (`ulong`) |
| `SocketSlashCommand` | `Interaction` |
| `CommandService` | `CommandsExtension` |
| `InteractionService` | `InteractionHandler` |
| `SocketReaction` | `Reaction` |
| `LogSeverity` / `LogMessage` | `ILogger<T>` (Microsoft.Extensions.Logging) |

### Key differences

**No socket/generic entity model** — PawSharp uses concrete typed entities instead of socket/rest discrimination:

```csharp
// Discord.Net — socket vs rest duality
SocketGuild guild = client.GetGuild(id); // socket variant
RestGuild restGuild = await restClient.GetGuildAsync(id); // rest variant

// PawSharp — single entity type
Guild guild = await client.Rest.GetGuildAsync(guildId);
```

**No `IMessageChannel` abstraction** — methods take `ulong` channel IDs:

```csharp
// Discord.Net
var channel = await client.GetChannelAsync(channelId) as IMessageChannel;
await channel.SendMessageAsync("Hello");

// PawSharp
await client.SendMessageAsync(channelId, "Hello");
```

**Logger setup is standard `Microsoft.Extensions.Logging`:**

```csharp
// Discord.Net — custom LogSeverity
client.Log += (msg) => { Console.WriteLine(msg.Message); };

// PawSharp — standard .NET logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

**Gateway events use subscription pattern, not C# events:**

```csharp
// Discord.Net — C# event handler
client.MessageReceived += HandleMessage;

// PawSharp — subscription pattern
client.OnMessageCreated(HandleMessage);

// PawSharp — unsubscribe via disposable
var disposable = client.OnMessageCreated(HandleMessage);
disposable.Dispose();
```

**Intents are flags, not strings:**

```csharp
// Discord.Net — string-based
client.MessageReceived += handler; // implicit intent

// PawSharp — explicit flags
.WithIntents(GatewayIntents.GuildMessages | GatewayIntents.MessageContent)
```

---

## PawSharp Version History

### Version scheme

PawSharp follows [SemVer 2.0](https://semver.org/). Pre-release versions use dot notation:

```
1.1.0-alpha.5   ← current (APIs may change)
1.0.0-alpha.x   ← previous series
0.11.0-alpha.x  ← legacy (.NET 8)
```

⚠️ **We are in alpha** — breaking changes are expected between minor versions. The `../CHANGELOG.md` and this guide track all breaking changes.

### Release timeline

| Version | Date | .NET | Notes |
|---|---|---|---|
| `0.11.0-alpha.1` | 2026-03-10 | 8.0 | Last .NET 8 release |
| `1.0.0-alpha.1` | 2026-03-11 | 10.0 | First .NET 10 release |
| `1.0.0-alpha.2` | 2026-04-08 | 10.0 | DI improvements, rate-limit telemetry |
| `1.0.0-alpha.3` | 2026-04-15 | 10.0 | Bug fixes, no breaking changes |
| `1.0.0-alpha.4` | 2026-04-22 | 10.0 | Cache fixes, voice stabilization |
| `1.1.0-alpha.1` | 2026-05-01 | 10.0 | Cache metrics, autocomplete, new entities |
| `1.1.0-alpha.2` | 2026-05-03 | 10.0 | Telemetry, health checks |
| `1.1.0-alpha.3` | 2026-06-15 | 10.0 | IDiscordClient, global exception handlers, command auto-discovery |
| `1.1.0-alpha.4` | 2026-06-24 | 10.0 | DAVE E2EE P-256 migration, thread safety, ConfigureAwait(false) |
| `1.1.0-alpha.5` | 2026-07-08 | 10.0 | Gateway state races fixed, rate limiter leak, voice double-dispatch |

---

## Breaking Changes by Version

### 0.11.0-alpha.1 → 1.0.0-alpha.1

**Target framework: .NET 8 → .NET 10**

All packages now target `net10.0`. Update your project:

```xml
<TargetFramework>net10.0</TargetFramework>
```

**`InteractionResolvedData` keys: `string` → `ulong`**

```csharp
// Before
var user = resolvedData.Users["123456789"];

// After
var user = resolvedData.Users[123456789ul];
```

Affects `.Users`, `.Members`, `.Roles`, `.Channels`, `.Messages`, `.Attachments`.

**`DeleteInvokeAsync` now returns `Task<Invite?>` instead of `Task<bool>`**

**`GetActiveThreadsAsync` now returns `Task<ActiveThreadsResponse?>` instead of `Task<List<Channel>?>`**

**REST methods now throw exceptions instead of returning null**

```csharp
// Before — null check
var msg = await client.Rest.CreateMessageAsync(id, req);
if (msg == null) { /* unknown failure */ }

// After — typed exceptions
try { var msg = await client.Rest.CreateMessageAsync(id, req); }
catch (ValidationException ex) { /* input error */ }
catch (RateLimitException ex) { /* rate limited */ }
catch (DiscordApiException ex) { /* API error */ }
```

**Archived thread methods now return `ArchivedThreadsResponse?`**

**`HeartbeatManager` constructor now requires `ILogger` parameter (can be null)**

### 1.0.0-alpha.1 → 1.0.0-alpha.2

No breaking changes.

### 1.0.0-alpha.2 → 1.0.0-alpha.3

No breaking changes.

### 1.0.0-alpha.3 → 1.0.0-alpha.4

No breaking changes.

### 1.0.0-alpha.4 → 1.1.0-alpha.1

No breaking changes.

### 1.1.0-alpha.1 → 1.1.0-alpha.2

No breaking changes.

### 1.1.0-alpha.2 → 1.1.0-alpha.3

No breaking changes.

### 1.1.0-alpha.3 → 1.1.0-alpha.4

**Removed `[Obsolete]` methods:**

| Removed | Replacement |
|---|---|
| `services.AddPawSharpClient(PawSharpOptions)` | `services.SetupPawSharp(options)` |
| `services.AddPawSharpClient()` | `services.SetupPawSharp(options)` or `services.AddPawSharpWithMemoryCache(options)` |

**Exception hierarchy consolidated:**

- `PawSharp.Core.Exceptions.DiscordApiException` now inherits from `PawSharp.API.Exceptions.DiscordApiException`
- `PawSharp.Cache.Exceptions.CacheException` now inherits from `DiscordException` instead of `Exception`

**`ConfigureAwait(false)` added project-wide** — every `await` in library code now uses `.ConfigureAwait(false)`. If you capture `SynchronizationContext` inside event handlers, wrap continuations in `Task.Run()`.

**`HeartbeatManager.maxMissedAcks` default: 2 → 3** (matches `PawSharpOptions.MaxMissedHeartbeatAcks`)

**`VoiceConnection` no longer hardcodes `?v=8`** — protocol version resolved at runtime via `VoiceProtocolVersion` constant.

**Voice `PlayAudioAsync()` / `PlayAudioFromPcmAsync()` now throw `ObjectDisposedException` on disposed connections.**

### 1.1.0-alpha.4 → 1.1.0-alpha.5

No breaking changes.

---

## General Migration Notes

### Namespace changes

Component model classes (`MessageComponent`, `ActionRow`, `Button`, `SelectMenu`, `SelectOption`, `TextInput`) moved from `PawSharp.API.Models` to `PawSharp.Core.Entities`.

### Type changes

- `Message.Components`: `List<object>?` → `List<MessageComponent>?`
- `Message.Flags`: `int?` → `MessageFlags?`
- `ModalBuilder.AddTextInput` `style` parameter: `int` → `TextInputStyle`
- `TextInput.Style`: `int` → `TextInputStyle`
- `CreateAutoModerationRuleRequest.EventType` / `TriggerType`: `int` → typed enums
- `CreateStageInstanceRequest.PrivacyLevel`: `int?` → `StageInstancePrivacyLevel?`
- `ArchivedThreadsResponse.Threads`: `List<Channel>` → `List<Thread>`

### Event API changes

- `EventDispatcher.DispatchFromJson()` → `DispatchFromJsonAsync()`
- `EventDispatcher.On()` now returns `IDisposable` for unsubscription
- `EventDispatcher.Use()` supports middleware (all handlers fire after middleware)

### Package upgrades

All `Microsoft.Extensions.*` packages are on `10.0.0`. `StackExchange.Redis` is `2.8.16`.

---

## Versioning Policy

PawSharp follows **SemVer 2.0.0**. Pre-release versions (`-alpha.N`) indicate APIs may change. Breaking changes in alpha are documented in the changelog and this guide.

Key rules:
- All 9 library packages share the same version via `src/Directory.Build.props`
- Pre-release versions go to NuGet's pre-release feed only
- The `main` branch is always buildable with passing tests
- Release branches follow the pattern `release/vX.Y.Z`
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/)

---

## Need Help?

- [Full changelog](../../CHANGELOG.md)
- [FAQ](faq.md)
- [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues)
- [Discord](https://discord.gg/6Z8X8cCHXs)
