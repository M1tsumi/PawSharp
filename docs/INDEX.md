# PawSharp Developer Documentation Index

Welcome to PawSharp! This is your complete guide to building Discord bots with .NET 8.0+.

## 📚 Getting Started (Start Here!)

**New to PawSharp?** Start with these guides in order:

1. **[DEVELOPERS_GUIDE.md](./DEVELOPERS_GUIDE.md)** ⭐ **START HERE**
   - Installation & setup
   - Your first bot (under 50 lines!)
   - Core concepts
   - Basic error handling
   - Best practices

2. **[REST_API_GUIDE.md](./REST_API_GUIDE.md)**
   - Sending messages with embeds
   - Managing guilds, members, roles
   - Channels and threads
   - Webhooks and reactions
   - 140+ API endpoints documented

3. **[GATEWAY_GUIDE.md](./GATEWAY_GUIDE.md)**
   - Real-time event handling
   - 40+ Discord events
   - Connection management
   - Sharding for large bots
   - Event patterns and middleware

4. **[CACHING_GUIDE.md](./CACHING_GUIDE.md)**
   - In-memory caching
   - Redis distributed caching
   - Cache strategies
   - Scaling for large bots
   - Performance optimization

5. **[PATTERNS_GUIDE.md](./PATTERNS_GUIDE.md)**
   - Command handling
   - Moderation systems
   - Logging & monitoring
   - User interactions
   - Real-world examples

6. **[TROUBLESHOOTING.md](./TROUBLESHOOTING.md)**
   - Common issues & solutions
   - Debugging tips
   - Performance troubleshooting
   - Getting help

---

## 🎯 Quick Links by Task

### I want to...

**Send a message**
→ [REST_API_GUIDE.md - Sending Messages](./REST_API_GUIDE.md#sending-messages)

**Listen for events**
→ [GATEWAY_GUIDE.md - Subscribing to Events](./GATEWAY_GUIDE.md#subscribing-to-events)

**Create a command system**
→ [PATTERNS_GUIDE.md - Command Handling](./PATTERNS_GUIDE.md#command-handling)

**Moderate my server**
→ [PATTERNS_GUIDE.md - Moderation](./PATTERNS_GUIDE.md#moderation)

**Scale my bot**
→ [CACHING_GUIDE.md - Scaling](./CACHING_GUIDE.md#scaling-for-large-bots)

**Handle errors properly**
→ [DEVELOPERS_GUIDE.md - Error Handling](./DEVELOPERS_GUIDE.md#error-handling)

**Fix a problem**
→ [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)

---

## 📋 API Reference

Detailed API documentation is embedded in the XML doc-comments in each source project.
For a structured overview by module:

- **PawSharp.Core** — Base entities (`Guild`, `Channel`, `Message`, `User`, `Role`, `Embed`), enums, exceptions, `EmbedBuilder`
- **PawSharp.API** — `IDiscordRestClient` with 140+ typed endpoints; `RestClient`, rate-limit layer
- **PawSharp.Gateway** — `GatewayClient`, `EventDispatcher`, `HeartbeatManager`, `ReconnectionManager`, `ShardManager`
- **PawSharp.Cache** — `IEntityCache`, `MemoryCacheProvider`, `RedisCacheProvider`
- **PawSharp.Client** — `DiscordClient` (unified facade), `CacheManager`, DI extension `AddPawSharp()`
- **PawSharp.Commands** — `CommandsExtension`, `BaseCommandModule`, `[Command]`, `[Aliases]`, `[Description]`
- **PawSharp.Interactions** — `InteractionHandler`, slash commands, components, autocomplete, context menus
- **PawSharp.Interactivity** — Reaction pagination, `InteractivityExtension`
- **PawSharp.Voice** — `VoiceClient`, `VoiceConnection` (experimental)

---

## 🚀 Common Scenarios

### Basic Bot in 5 Minutes

```csharp
// 1. Add NuGet package
// dotnet add package PawSharp.Client

// 2. Create bot
var services = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton(new PawSharpOptions
    {
        Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!,
        Intents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
    })
    .AddPawSharp();

var client = services.BuildServiceProvider().GetRequiredService<DiscordClient>();

// 3. Add command
client.OnMessageCreated(msg =>
{
    if (msg.Content == "!ping")
        return client.Rest.CreateMessageAsync(msg.ChannelId, new() { Content = "🏓 Pong!" });
    return Task.CompletedTask;
});

// 4. Run
await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

→ More details: [DEVELOPERS_GUIDE.md - Your First Bot](./DEVELOPERS_GUIDE.md#your-first-bot)

### Moderation Bot

Kick, ban, and moderate members automatically.

→ See: [PATTERNS_GUIDE.md - Moderation](./PATTERNS_GUIDE.md#moderation)

### Large-Scale Bot (2500+ Guilds)

Scale with sharding and Redis.

→ See: [CACHING_GUIDE.md - Scaling](./CACHING_GUIDE.md#scaling-for-large-bots)

### Real-Time Logging

Log all events to database.

→ See: [PATTERNS_GUIDE.md - Logging](./PATTERNS_GUIDE.md#logging--monitoring)

---

## 🔧 Configuration Reference

### PawSharpOptions

```csharp
var options = new PawSharpOptions
{
    // Authentication
    Token = "your-bot-token",
    ApiVersion = 10,

    // Gateway
    Intents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,

    // Sharding
    Shards = ShardingStrategy.Auto,

    // Reconnection
    ReconnectTimeout = TimeSpan.FromSeconds(1),
    MaxReconnectAttempts = 5,
};
```

→ Full reference: [DEVELOPERS_GUIDE.md - Configuration](./DEVELOPERS_GUIDE.md#configuration)

---

## 📦 Installation

### Simple Installation

```bash
# Just REST API
dotnet add package PawSharp.API

# Just Gateway
dotnet add package PawSharp.Gateway

# Everything (recommended)
dotnet add package PawSharp.Client

# With commands
dotnet add package PawSharp.Commands

# With interactions (slash commands)
dotnet add package PawSharp.Interactions
```

→ More: [DEVELOPERS_GUIDE.md - Installation](./DEVELOPERS_GUIDE.md#installation--setup)

---

## ❓ FAQ

**Q: Can I use PawSharp with .NET 7?**
A: No, PawSharp requires .NET 8.0+.

**Q: How many guilds can a single bot instance handle?**
A: Typically 2500+ guilds per shard. Use sharding for larger bots.

**Q: Do I need Redis?**
A: For small bots (< 500 guilds), in-memory cache is fine. For larger bots, Redis recommended.

**Q: How do I read message content?**
A: Enable the `MessageContent` intent and request it in Developer Portal.

**Q: Can I use voice?**
A: Voice is experimental and lacks full features. Use DSharpPlus/Discord.NET for production voice.

**Q: Where's the source code?**
A: Visit [GitHub](https://github.com/pawsharp/pawsharp)

→ More FAQs: [TROUBLESHOOTING.md](./TROUBLESHOOTING.md)

---

## 🐛 Troubleshooting

Having issues? Check these first:

1. **Connection problems** → [TROUBLESHOOTING.md - Gateway & Connection](./TROUBLESHOOTING.md#gateway--connection)
2. **Rate limiting** → [TROUBLESHOOTING.md - REST API Errors](./TROUBLESHOOTING.md#rest-api-errors)
3. **Events not firing** → [TROUBLESHOOTING.md - Event Handling](./TROUBLESHOOTING.md#event-handling-problems)
4. **Memory usage high** → [TROUBLESHOOTING.md - Performance Issues](./TROUBLESHOOTING.md#performance-issues)
5. **Still stuck?** → [TROUBLESHOOTING.md - Getting Help](./TROUBLESHOOTING.md#getting-help)

---

## 📊 REST API Endpoint Coverage

PawSharp implements **140+ Discord API endpoints**:

### Completeness by Category

| Category | Coverage | Notes |
|----------|----------|-------|
| **Messages** | ✅ 100% | All message operations |
| **Channels** | ✅ 100% | All channel types |
| **Guilds** | ✅ 100% | Full guild management |
| **Members** | ✅ 100% | Member management |
| **Roles** | ✅ 100% | Role CRUD and assignment |
| **Webhooks** | ✅ 100% | Webhook creation and execution |
| **Threads** | ✅ 100% | Thread management |
| **Reactions** | ✅ 100% | Message reactions |
| **Slash Commands** | ✅ 100% | Application commands |
| **Interactions** | ✅ 100% | Interaction responses |
| **Audit Logs** | ✅ 100% | Guild audit logs |
| **Auto-Moderation** | ✅ 100% | Auto-moderation rules |
| **Scheduled Events** | ✅ 100% | Guild events |
| **Voice** | ⚠️ Partial | Experimental, see docs |

→ Full endpoint list: [REST_API_GUIDE.md - Endpoints](./REST_API_GUIDE.md#core-concepts)

---

## 🎓 Learning Path

**Beginner:**
1. [DEVELOPERS_GUIDE.md - Installation](./DEVELOPERS_GUIDE.md#installation--setup)
2. [DEVELOPERS_GUIDE.md - Your First Bot](./DEVELOPERS_GUIDE.md#your-first-bot)
3. [REST_API_GUIDE.md - Messages](./REST_API_GUIDE.md#messages)
4. [GATEWAY_GUIDE.md - Basic Events](./GATEWAY_GUIDE.md#connection-events)

**Intermediate:**
5. [PATTERNS_GUIDE.md - Command Handling](./PATTERNS_GUIDE.md#command-handling)
6. [REST_API_GUIDE.md - Guild Management](./REST_API_GUIDE.md#guilds)
7. [GATEWAY_GUIDE.md - Event Patterns](./GATEWAY_GUIDE.md#event-handling-patterns)
8. [CACHING_GUIDE.md - In-Memory Cache](./CACHING_GUIDE.md#in-memory-cache)

**Advanced:**
9. [CACHING_GUIDE.md - Redis & Scaling](./CACHING_GUIDE.md#redis-distributed-cache)
10. [PATTERNS_GUIDE.md - Real-World Patterns](./PATTERNS_GUIDE.md)
11. [GATEWAY_GUIDE.md - Sharding](./GATEWAY_GUIDE.md#sharded-gateway)
12. [DEVELOPERS_GUIDE.md - Best Practices](./DEVELOPERS_GUIDE.md#best-practices)

---

## 📞 Support

### Documentation
- 📖 You're reading it! All guides above
- 🔍 Use Ctrl+F to search for specific topics

### Community
- 💬 [GitHub Discussions](https://github.com/pawsharp/pawsharp/discussions)
- 🐛 [GitHub Issues](https://github.com/pawsharp/pawsharp/issues)

### External Resources
- 📚 [Discord.js Guide](https://discordjs.guide/) (similar concepts)
- 🔗 [Discord API Documentation](https://discord.com/developers/docs)
- 💻 [Stack Overflow `discord-api` tag](https://stackoverflow.com/questions/tagged/discord-api)

---

## 📝 Documentation Versions

**Latest:** 1.0.0-alpha.4 (April 22, 2026)

Documentation covers:
- ✅ 1.0.0-alpha.1 and later
- ⚠️ May contain breaking changes in alpha versions
- 🎯 Preparing for 1.0.0 stable release

---

## 🤝 Contributing

Want to improve the docs?

1. **Report issues** - Found an error? [Open an issue](https://github.com/pawsharp/pawsharp/issues)
2. **Suggest changes** - Have an idea? [Start a discussion](https://github.com/pawsharp/pawsharp/discussions)
3. **Submit PRs** - Fix typos or improve guides directly
4. **Add examples** - Create real-world examples for other developers

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

---

## 📄 License

PawSharp documentation is available under the MIT License.

---

## 🎉 Next Steps

1. Read [DEVELOPERS_GUIDE.md](./DEVELOPERS_GUIDE.md)
2. Create your first bot
3. Join the community
4. Build something awesome!

**Happy coding!** 🚀

---

*Last updated: March 29, 2026*  
*PawSharp Version: 1.0.0-alpha.4*  
*For the latest documentation, visit [github.com/pawsharp/pawsharp/docs](https://github.com/pawsharp/pawsharp/docs)*
