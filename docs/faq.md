# Frequently Asked Questions

Common developer questions about building Discord bots with PawSharp.

---

## General

### Q: What is PawSharp?

PawSharp is a modular Discord API wrapper for **.NET 10**. It provides a fluent builder, async-first APIs, source-generated JSON for native AOT, typed events, automatic rate limiting, and pure .NET voice with DAVE E2EE. Built from the ground up for modern .NET without native dependencies.

### Q: How is PawSharp different from DSharpPlus or Discord.Net?

PawSharp targets **.NET 10 only**, not net6.0/8.0. This lets it use the latest BCL features (e.g. `System.Net.WebSockets.Managed`, `AesGcm`, `TimeProvider`) without polyfills.

| Feature | PawSharp | Discord.Net | DSharpPlus |
|---|---|---|---|
| Target framework | .NET 10 | .NET 8+ | .NET 8+ |
| Native AOT / trimming |  Full |  |  |
| Source-gen JSON |  All types |  Newtonsoft |  Newtonsoft |
| Pure .NET voice |  Opus via Concentus |  libopus native |  libopus native |
| DAVE E2EE |  MLS (RFC 9420) |  |  |
| Modular packages |  9 packages |  monolithic |  partial |
| Fluent builder |  `PawSharpClientBuilder` |  |  |
| Request | `PawSharp.Gateway` | `Socket/WebSocket` | `DiscordClient` |
| Gateway events | Typed C# classes | `SocketMessage` | `DiscordMessage` |

### Q: Do I need .NET 10?

Yes. All PawSharp packages target `net10.0`. You cannot use them from older runtimes. Install the [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

### Q: What packages are available?

| Package | Description |
|---|---|
| `PawSharp.Core` | Core entities, enums, exceptions, validation |
| `PawSharp.API` | REST client with automatic rate limiting |
| `PawSharp.Gateway` | WebSocket gateway, sharding, events |
| `PawSharp.Cache` | In-memory / Redis caching providers |
| `PawSharp.Client` | High-level client (bundles Core + API + Gateway + Cache + Interactions) |
| `PawSharp.Interactions` | Slash commands, buttons, modals, autocomplete |
| `PawSharp.Commands` | Attribute-based command framework |
| `PawSharp.Interactivity` | Pagination, polls, confirmation dialogs |
| `PawSharp.Voice` | Opus encode/decode, RTP, DAVE E2EE |

`PawSharp.Client` is the recommended starting point - it includes everything most bots need.

### Q: Is PawSharp production-ready?

We're at **1.1.0-alpha.5** - core pieces work, APIs are settling, breaking changes still happen. Follow the [migration guide](migration.md) when updating. Production use is possible but expect occasional breaking changes until 1.0.0 stable.

---

## Setup

### Q: How do I get a bot token?

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Create an application, then go to the **Bot** tab
3. Click **Reset Token** (or copy the existing one)
4. Never commit it to source control - use environment variables:

```csharp
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
 ?? throw new InvalidOperationException("DISCORD_TOKEN not set");
```

### Q: What intents do I need?

At minimum you need `GatewayIntents.Guilds` to receive basic guild events. Enable intents in the Developer Portal under **Bot > Privileged Gateway Intents**, then configure them:

```csharp
var client = new PawSharpClientBuilder()
 .WithToken(token)
 .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
 .Build();
```

 Use `GatewayIntents.AllNonPrivileged` during development, then trim down for production.

 `MessageContent`, `GuildMembers`, and `GuildPresences` are **privileged** - you must enable them in the Developer Portal AND pass them to `.WithIntents()`.

### Q: How do I use dependency injection?

PawSharp integrates with `Microsoft.Extensions.DependencyInjection`:

```csharp
var services = new ServiceCollection();

services.SetupPawSharp(options =>
{
 options.Token = token;
 options.Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent;
});

services.AddLogging(builder =>
{
 builder.AddConsole();
 builder.SetMinimumLevel(LogLevel.Information);
});

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IDiscordClient>();

await client.ConnectAsync();
// ...
```

`SetupPawSharp()` registers `IDiscordClient`, `DiscordClient`, `IDiscordRestClient`, `IEntityCache`, `IGatewayClient`, and related services.

### Q: How do I configure logging?

PawSharp uses `Microsoft.Extensions.Logging`. Use the builder for quick setup:

```csharp
var client = new PawSharpClientBuilder()
 .WithToken(token)
 .UseConsoleLogging(LogLevel.Debug) // Quick console logging
 .Build();
```

Or with DI for custom configuration:

```csharp
services.AddLogging(builder =>
{
 builder.AddConsole();
 builder.AddDebug();
 builder.AddEventLog();
 builder.SetMinimumLevel(LogLevel.Debug);
});
services.SetupPawSharp(options => { /* ... */ });
```

---

## Gateway

### Q: Why does my bot keep disconnecting?

Common causes:

| Symptom | Likely cause |
|---|---|
| Disconnects every ~60s | Missing or invalid intents - check Developer Portal |
| Disconnects after resume | Heartbeat ack timeout - check network stability |
| Random disconnects under load | Rate limit on gateway commands - reduce command frequency |
| Disconnects at startup | Firewall blocking port 443 WebSocket |

Enable debug logging to diagnose:

```csharp
.UseConsoleLogging(LogLevel.Trace)
```

Check gateway state in your ready handler:

```csharp
client.OnReady(evt =>
{
 Console.WriteLine($"Connected as {evt.User?.Username}, shard {evt.ShardId}");
 return Task.CompletedTask;
});
```

### Q: How does session resumption work?

The gateway automatically resumes disconnected sessions using the `session_id` from the `READY` payload. On disconnect, the library reconnects and sends a `RESUME` opcode. If resumption succeeds, you get a `RESUMED` event instead of a full `READY`.

Resumption is best-effort. After ~15 seconds or a gateway reconnect, Discord invalidates the session and forces a full re-identify. The library handles this transparently.

### Q: What's a zombie connection?

A zombie connection is one where the WebSocket appears open but no heartbeats are being acknowledged. The library detects this via `HeartbeatManager` - if 3 consecutive heartbeats go unacknowledged, it terminates and reconnects. This is normal and handled automatically.

Monitor missed acks in logs by setting log level to `Debug`:

```
[PawSharp.Gateway.HeartbeatManager] Missed heartbeat ack #3, reconnecting...
```

### Q: How do I shard my bot?

Sharding is built-in. Configure shard count or use automatic recommendation:

```csharp
// Automatic shard count (recommended)
var client = new PawSharpClientBuilder()
 .WithToken(token)
 .WithIntents(GatewayIntents.AllNonPrivileged)
 .WithSharding(auto: true)
 .Build();

// Or manual shard count
.WithSharding(shardCount: 16)
```

The `ShardManager` distributes guilds across shards. Subscribe to shard events:

```csharp
client.OnShardConnected(shardId => { /* ... */ });
client.OnShardDisconnected((shardId, ex) => { /* ... */ });
```

---

## Events

### Q: Why aren't my events firing?

**Most common cause: missing intents.** Each event type requires a specific intent:

| Event | Required Intent |
|---|---|
| `OnMessageCreated` | `GatewayIntents.GuildMessages` + `MessageContent` |
| `OnGuildMemberAdded` | `GatewayIntents.GuildMembers` (privileged) |
| `OnPresenceUpdated` | `GatewayIntents.GuildPresences` (privileged) |
| `OnGuildCreated` | `GatewayIntents.Guilds` |
| `OnVoiceStateUpdated` | `GatewayIntents.GuildVoiceStates` |
| `OnReactionAdded` | `GatewayIntents.GuildMessageReactions` |
| `OnTypingStarted` | `GatewayIntents.GuildMessageTyping` |

 `MessageContent` intent is **privileged** - if enabled in code but not in the Developer Portal, `msg.Content` will be empty.

Use `ValidateIntents()` to check before connecting:

```csharp
var result = client.ValidateIntents();
if (!result.IsValid)
{
 foreach (var (evt, req, missing) in result.Issues)
 Console.WriteLine($"{evt} requires {missing}");
}
```

### Q: Should I use convenience methods or the low-level dispatcher?

Use convenience methods (`OnMessageCreated`, `OnGuildUpdated`, etc.) unless you need:

- Dynamic event subscription by name
- Middleware via `EventDispatcher.Use()`
- Event filtering / replay

```csharp
// Preferred - strongly typed
client.OnMessageCreated(async evt => { /* ... */ });

// Low-level - for dynamic scenarios
client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", evt => { /* ... */ });
```

### Q: How do I unsubscribe from an event?

Convenience methods return an `IDisposable`:

```csharp
var subscription = client.OnMessageCreated(handler);

// Later:
subscription.Dispose();
```

---

## Commands

### Q: How do I register slash commands?

Use `PawSharp.Commands` with the module pattern:

```csharp
public class MyModule : BaseCommandModule
{
 [Command("ping")]
 [Description("Responds with pong")]
 public async Task PingAsync(CommandContext ctx)
 {
 await ctx.RespondAsync("Pong!");
 }
}

// Register during setup:
services.AddCommands(typeof(MyModule).Assembly);

// Or at runtime:
var commands = client.GetCommandsExtension();
await commands.RegisterSlashModulesInAssemblyAsync(typeof(MyModule).Assembly);
```

### Q: How do I use autocomplete?

Add an `AutocompleteHandler` for your slash command option:

```csharp
[SlashCommand("search", "Search for something")]
public async Task SearchAsync(
 CommandContext ctx,
 [Autocomplete(typeof(MyAutocompleteProvider))]
 string query)
{
 // ...
}

public class MyAutocompleteProvider : IAutocompleteProvider
{
 public async ValueTask<IEnumerable<AutocompleteChoice>> GetChoicesAsync(
 AutocompleteContext context)
 {
 var input = context.FocusedOption.Value?.ToString() ?? "";
 return Enumerable.Range(1, 5)
 .Select(i => new AutocompleteChoice($"{input} result {i}", $"{input}-{i}"));
 }
}
```

### Q: How do I use buttons, modals, and select menus?

These are handled by `PawSharp.Interactions`. Create a component interaction handler:

```csharp
[ComponentInteraction("confirm:*")]
public async Task HandleConfirmAsync(string id, ComponentInteractionContext ctx)
{
 await ctx.UpdateAsync(new MessageProperties()
 .WithContent($"Confirmed: {id}"));
}

// Or listen globally:
client.OnButtonExecuted(async ctx =>
{
 if (ctx.CustomId == "my_button")
 await ctx.RespondAsync("Button clicked!");
});
```

---

## Permissions

### Q: How do I check if a user has a permission?

```csharp
var guild = await client.Rest.GetGuildAsync(guildId);
var member = await client.Rest.GetGuildMemberAsync(guildId, userId);

if (member.Permissions.HasFlag(Permissions.KickMembers))
{
 // User can kick members
}
```

For commands, use the `[RequirePermissions]` precondition:

```csharp
[Command("kick")]
[RequirePermissions(Permissions.KickMembers)]
public async Task KickAsync(CommandContext ctx, IGuildMember member)
{
 // The framework checks permissions before invoking
}
```

### Q: What's the permission hierarchy?

1. Owner (guild owner) - bypasses all permission checks
2. Administrator permission - bypasses all permission checks
3. Role hierarchy - a member cannot act on anyone with a higher role position
4. Specific permissions - KickMembers, BanMembers, ManageMessages, etc.

The library enforces **Discord-side checks**. Some operations (like banning a member with a higher role) will fail at the API level with a 403 error regardless of permissions.

### Q: How do I check role hierarchy before moderation actions?

```csharp
var botMember = await client.Rest.GetGuildMemberAsync(guildId, client.CurrentUserId);
var targetMember = await client.Rest.GetGuildMemberAsync(guildId, targetUserId);

var botHighestRole = guild.Roles
 .Where(r => botMember.RoleIds.Contains(r.Id))
 .MaxBy(r => r.Position);

var targetHighestRole = guild.Roles
 .Where(r => targetMember.RoleIds.Contains(r.Id))
 .MaxBy(r => r.Position);

if (botHighestRole?.Position <= targetHighestRole?.Position)
 throw new InvalidOperationException("Cannot moderate a user with equal or higher role.");
```

---

## Performance

### Q: How do I optimize my bot?

1. **Use intents sparingly** - only enable the intents you actually need. Each extra intent increases gateway traffic.
2. **Avoid blocking in event handlers** - always use async:

 ```csharp
 //  Bad
 client.OnMessageCreated(msg => {
 var result = client.Rest.GetChannelAsync(msg.ChannelId).Result;
 });

 //  Good
 client.OnMessageCreated(async msg => {
 var channel = await client.Rest.GetChannelAsync(msg.ChannelId);
 });
 ```

3. **Offload expensive work**:

 ```csharp
 client.OnMessageCreated(async msg =>
 {
 // Quick reply
 await msg.RespondAsync("Processing...");
 // Heavy work in background
 _ = Task.Run(() => AnalyzeMessageAsync(msg));
 });
 ```

4. **Use caching** - `PawSharp.Cache` avoids redundant API calls:

 ```csharp
 // First call fetches from API, subsequent calls use cache
 var guild = await client.Rest.GetGuildAsync(guildId);
 ```

5. **Configure logging level** - `Information` or `Warning` for production:

 ```csharp
 .UseConsoleLogging(LogLevel.Warning)
 ```

### Q: When should I shard?

Discord **requires** sharding at 2500 guilds. Shard earlier if:
- You're experiencing latency issues
- Your bot sends a high volume of commands
- You want to distribute load across processes

Use the automatic shard count:

```csharp
.WithSharding(auto: true)
```

### Q: Do I need Redis?

Redis is useful when:
- Running multiple bot instances behind a load balancer
- Sharing cache state across processes
- Persistent cache that survives restarts

**For a single-instance bot**, the in-memory cache provider is sufficient:

```csharp
// In-memory (default)
services.AddPawSharpWithMemoryCache(options => { /* ... */ });

// Redis
services.SetupPawSharp(options => { /* ... */ });
services.AddSingleton<IEntityCache>(
 new RedisCacheProvider("localhost:6379"));
```

---

## Voice

### Q: Is voice stable?

Voice is **functional but experimental**. The library supports:
- Joining and leaving voice channels
- Playing Opus-encoded audio (via Concentus, no native DLLs)
- Receiving and decoding audio
- DAVE E2EE (MLS / RFC 9420) for encrypted voice

Known limitations:
- DAVE E2EE is a new specification - Discord's implementation is still evolving
- Voice receive may have higher latency than native alternatives
- Not all voice regions are equally stable

### Q: What is DAVE E2EE?

DAVE (Discord Audio & Video Encryption) is Discord's end-to-end encryption standard for voice. It uses the MLS (Messaging Layer Security) protocol specified in RFC 9420 with a P-256 ciphersuite.

PawSharp implements the full DAVE v1.1 specification:
- HPKE (RFC 9180) for key encapsulation
- MLS key schedule for epoch management
- AES-128-GCM frame encryption with per-sender keys
- Binary WebSocket message protocol (ops 21 - 31)

You don't need to do anything special - DAVE is handled transparently by `VoiceConnection` when available.

```csharp
var voice = client.GetVoiceClient();
await voice.JoinChannelAsync(guildId, voiceChannelId);
// DAVE negotiation happens automatically
await voice.PlayAudioAsync(audioStream);
```

---

## Troubleshooting

### Q: Common errors and solutions

| Error | Likely cause | Solution |
|---|---|---|
| `GatewayException: Invalid token` | Wrong or malformed token | Re-copy token from Developer Portal, trim whitespace |
| `DiscordApiException: 401 Unauthorized` | Token expired or invalid | Reset token in Developer Portal |
| `DiscordApiException: 429 Too Many Requests` | Rate limit hit | Implement backoff - the library handles this automatically, but burst sends can still trigger it |
| `DiscordApiException: 403 Forbidden` | Missing permissions | Check bot role position and permissions |
| `ValidationException` | Invalid input (content too long, etc.) | Validate inputs before sending |
| Events not firing | Missing intents | Enable required intents in Portal AND code |
| `ObjectDisposedException` in Voice | Called `PlayAudioAsync` after disconnect | Check `IsConnected` before calling voice methods |
| `SemaphoreFullException` in rate limiter | Concurrent access (fixed in alpha.5) | Update to latest version |
| `TaskCanceledException` on connect | Network timeout | Check firewall, WebSocket access to `gateway.discord.gg:443` |

### Q: How do I capture debug logs?

```csharp
var client = new PawSharpClientBuilder()
 .WithToken(token)
 .UseConsoleLogging(LogLevel.Trace)
 .Build();
```

Or with DI:

```csharp
services.AddLogging(builder =>
{
 builder.AddConsole();
 builder.AddDebug();
 builder.SetMinimumLevel(LogLevel.Trace);
});
```

### Q: How do I report a bug?

Open an issue at [github.com/M1tsumi/PawSharp/issues](https://github.com/M1tsumi/PawSharp/issues) with:

- PawSharp version (from `Directory.Build.props`)
- .NET version (`dotnet --info`)
- Operating system
- What you expected vs what happened
- Full exception and stack trace
- Minimal reproduction code

### Q: Where can I get help?

- [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues) - bug reports and feature requests
- [Discord Server](https://discord.gg/6Z8X8cCHXs) - community chat
- [Documentation](https://M1tsumi.github.io/PawSharp/) - full API reference

---

> Still stuck? Enable `Trace` logging and look for warning/error lines. Most issues are configuration problems, not library bugs.
