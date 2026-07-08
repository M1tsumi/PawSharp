# Frequently Asked Questions

**Q: Can I use PawSharp with .NET 9?**
A: No, PawSharp requires .NET 10.0+. The library targets `net10.0` and uses APIs from the .NET 10 BCL.

**Q: Can I use dependency injection?**
A: Yes. PawSharp integrates with `Microsoft.Extensions.DependencyInjection` out of the box. Call `services.SetupPawSharp(options)` and everything is wired up. You can also use the lightweight `PawSharpClientBuilder` for non-DI scenarios.

**Q: How do I test my bot logic?**
A: PawSharp provides `IDiscordClient`, `IDiscordRestClient`, `IGatewayClient`, and `IEntityCache` interfaces — all mockable.

**Q: How do I auto-discover command modules?**
A: Call `client.UseCommandsWithAutoDiscovery()` to scan the calling assembly for all `BaseCommandModule` subclasses and register them automatically.

**Q: How many guilds can a single bot instance handle?**
A: Typically 2500+ guilds per shard period. Use sharding for larger bots.

**Q: Do I need Redis?**
A: For small bots (< 500 guilds), in-memory cache is fine. For larger bots, Redis is recommended.

**Q: How do I read message content?**
A: Enable the `MessageContent` intent in code and request it in the Discord Developer Portal under Bot > Privileged Gateway Intents.

**Q: Can I use voice?**
A: Yes — PawSharp.Voice implements the Discord Voice Protocol with Opus audio and DAVE end-to-end encryption (MLS / RFC 9420). Voice is still alpha but functional.

**Q: Events not firing?**
A: Check that you've enabled the required intents both in code (`GatewayIntents.MessageContent`, etc.) and in the Discord Developer Portal.

**Q: Getting rate limited?**
A: PawSharp includes an `AdvancedRateLimiter` that handles bucket-based rate limiting automatically. If you're still hitting limits, consider implementing request queuing with `SemaphoreSlim`.

**Q: Where's the source code?**
A: Visit [GitHub](https://github.com/M1tsumi/PawSharp).

**Q: How do I report a bug?**
A: Open a [GitHub issue](https://github.com/M1tsumi/PawSharp/issues) with your code snippet, error message, stack trace, and steps to reproduce.
