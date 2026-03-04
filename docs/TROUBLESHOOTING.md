# Troubleshooting Guide

Common issues and their solutions when developing with PawSharp.

## Table of Contents

1. [Authentication Issues](#authentication-issues)
2. [Gateway & Connection](#gateway--connection)
3. [REST API Errors](#rest-api-errors)
4. [Performance Issues](#performance-issues)
5. [Event Handling Problems](#event-handling-problems)
6. [Caching Issues](#caching-issues)
7. [Getting Help](#getting-help)

---

## Authentication Issues

### "Invalid Token" Error

**Symptom:**
```
GatewayException: Invalid token
```

**Causes & Solutions:**

```csharp
// ❌ Problem 1: Typo in token
var options = new PawSharpOptions 
{ 
    Token = "MzI4ODk1NzQ..." // Wrong token
};

// ✅ Solution: Verify token
// 1. Go to Discord Developer Portal
// 2. Select your bot application
// 3. Go to "Bot" section
// 4. Copy the token carefully
// 5. No extra spaces or characters!
var token = "MzI4ODk1NzQ2NTkzNTkwNzUy.XX.XXXX";

// ❌ Problem 2: Hardcoded token
var options = new PawSharpOptions 
{ 
    Token = "your-secret-token-in-source"  // Security risk!
};

// ✅ Solution: Use environment variables
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
    ?? throw new InvalidOperationException("DISCORD_TOKEN not set");

var options = new PawSharpOptions { Token = token };

// ❌ Problem 3: Token has spaces
var token = "MzI4ODk1NzQ... "; // Trailing space!

// ✅ Solution: Trim the token
var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")?.Trim()
    ?? throw new InvalidOperationException("DISCORD_TOKEN not set");
```

### "Unauthorized" Error

**Symptom:**
```
DiscordApiException: (401) Unauthorized
```

**Causes:**
- Token expired
- Token is malformed
- Using user token instead of bot token

**Solutions:**
```csharp
// ✅ Always use bot tokens, not user tokens
// Bot tokens look like: MzI4ODk1NzQ...
// User tokens look like: different_format

// If token keeps expiring:
// 1. Reset bot token in Developer Portal
// 2. Update your environment variable
// 3. Redeploy bot
```

---

## Gateway & Connection

### Bot Won't Connect

**Symptom:**
```
No errors, but bot never connects
```

**Debugging:**

```csharp
// Add logging to see what's happening
var client = provider.GetRequiredService<DiscordClient>();

Console.WriteLine($"Connecting...");
try
{
    await client.ConnectAsync();
    Console.WriteLine($"Connected: {client.Gateway.IsConnected}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

// Check connection status
if (!client.Gateway.IsConnected)
{
    Console.WriteLine("Connection failed");
    Console.WriteLine($"Gateway state: {client.Gateway}");
}
```

### Connection Keeps Dropping

**Symptom:**
```
Bot connects, then disconnects repeatedly
```

**Causes & Solutions:**

```csharp
// Cause 1: Invalid intents
var options = new PawSharpOptions
{
    Intents = GatewayIntents.DirectMessages,  // Not enough intents
};

// Solution: Use comprehensive intents
var options = new PawSharpOptions
{
    Intents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
};

// Cause 2: Network issues
// Solution: Monitor reconnection
client.OnReady(ready =>
{
    Console.WriteLine("Connected and ready");
    return Task.CompletedTask;
});

// ResumedEvent has no convenience method — use low-level dispatcher
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Reconnected after disconnect");
    return Task.CompletedTask;
});

// Cause 3: Firewall blocking WebSocket
// Solution: Check firewall rules, enable port 443
```

### Gateway Timeout

**Symptom:**
```
Connection times out after ~30 seconds
```

**Solutions:**

```csharp
// Cause 1: Network latency
// Solution: Increase timeout in options (if configurable)
var options = new PawSharpOptions
{
    Token = token,
    // Gateway will use Discord's recommended timeouts
};

// Cause 2: Firewall/proxy blocking
// Solution: Test connectivity
try
{
    var client = new HttpClient();
    var response = await client.GetAsync("https://discord.com/api/v10/gateway");
    Console.WriteLine($"Gateway accessible: {response.IsSuccessStatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"Cannot reach gateway: {ex.Message}");
}

// Cause 3: Running behind proxy
// Solution: Configure proxy if needed
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy:8080"),
    UseProxy = true,
};
```

---

## REST API Errors

### Rate Limit Exceeded

**Symptom:**
```
DiscordApiException: (429) Too Many Requests
RateLimitException: Rate limited, retry after X ms
```

**Solutions:**

```csharp
// Solution 1: Catch and retry
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, waiting {ex.RetryAfter.TotalMilliseconds}ms");
    await Task.Delay(ex.RetryAfter);
    await client.Rest.CreateMessageAsync(channelId, request);
}

// Solution 2: Implement request queuing
public class RequestQueue
{
    private readonly SemaphoreSlim _semaphore = new(5);  // 5 concurrent requests
    private readonly IDiscordRestClient _rest;

    public async Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _rest.CreateMessageAsync(channelId, request);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

// Solution 3: Batch operations
// Instead of: for (int i = 0; i < 100; i++) { await CreateMessage(...); }
// Do this:
var messages = new List<CreateMessageRequest>();
foreach (var i in range)
{
    messages.Add(new CreateMessageRequest { ... });
}
// Then send in batches with delays
foreach (var batch in messages.Chunk(10))
{
    Parallel.ForEach(batch, msg => client.Rest.CreateMessageAsync(channelId, msg));
    await Task.Delay(1000);  // Wait between batches
}
```

### "Invalid JSON"

**Symptom:**
```
DiscordApiException: (400) Bad Request - Invalid JSON
```

**Solutions:**

```csharp
// Cause: Malformed request body
// Solution: Validate before sending

// ❌ Problem: Null values
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = null,  // Can't send null content
    Embeds = null,   // Same
});

// ✅ Solution: Provide required fields
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Hello",  // Required
    Embeds = new List<Embed>(),  // Can be empty list
});

// ❌ Problem: Invalid enum values
var embed = new Embed
{
    Color = -1,  // Invalid (negative)
};

// ✅ Solution: Use valid values
var embed = new Embed
{
    Color = 0xFF5733,  // Valid hex color
};

// ❌ Problem: String too long
await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = new string('x', 2001),  // Max 2000
});

// ✅ Solution: Validate length
var content = longText;
if (content.Length > 2000)
{
    content = content.Substring(0, 2000);
}
```

### "Missing Permissions"

**Symptom:**
```
DiscordApiException: (403) Forbidden - Missing Permissions
```

**Solutions:**

```csharp
// Check if bot has required permissions
var guild = await client.Rest.GetGuildAsync(guildId);
var botRole = guild.Roles.FirstOrDefault(r => r.Name == "@everyone");

// ✅ Ensure bot has high role
// 1. In Discord server settings, drag bot role above others
// 2. Give bot role necessary permissions

// ❌ Common issue: Trying to modify moderator
// Can only modify members with lower roles
await client.Rest.ModifyGuildMemberAsync(guildId, adminUserId, request);
// ^ Won't work if admin is higher than bot

// ✅ Solution: Check role hierarchy
var member = await client.Rest.GetGuildMemberAsync(guildId, targetUserId);
var botRoles = botMember.RoleIds.Select(id => guild.Roles.First(r => r.Id == id));
var targetRoles = member.RoleIds.Select(id => guild.Roles.First(r => r.Id == id));

var highestBotRole = botRoles.OrderByDescending(r => r.Position).First();
var highestTargetRole = targetRoles.OrderByDescending(r => r.Position).First();

if (highestBotRole.Position > highestTargetRole.Position)
{
    // Safe to modify
}
```

---

## Performance Issues

### High Memory Usage

**Symptom:**
```
Memory usage keeps growing
Process uses 1GB+
```

**Solutions:**

```csharp
// Cause: Unbounded cache
// Solution: Set cache limits

var settings = new CacheSettings
{
    MaxCachedMessages = 10000,  // Set limits
    MaxCachedUsers = 5000,
    MaxCachedGuilds = 2000,
    MessageCacheTTL = TimeSpan.FromHours(1),  // Auto-cleanup
};

// Cause: Memory leaks from event handlers
// Solution: Dispose the subscription token returned by event registration

var subscription = client.OnMessageCreated(msg =>
{
    Console.WriteLine(msg.Content);
    return Task.CompletedTask;
});

// Later: unsubscribe by disposing the token
subscription.Dispose();

// Cause: Large objects retained
// Solution: Clear references
client.OnMessageCreated(msg =>
{
    var largeData = new byte[1000000];  // 1MB
    // Do something
    // largeData goes out of scope and is garbage collected
    return Task.CompletedTask;
});

// Monitor memory
var process = System.Diagnostics.Process.GetCurrentProcess();
Console.WriteLine($"Memory: {process.WorkingSet64 / 1024 / 1024}MB");
```

### Slow Message Processing

**Symptom:**
```
Messages processed with noticeable delay
```

**Solutions:**

```csharp
// Cause: Blocking I/O in event handler
// ❌ Wrong
client.OnMessageCreated(msg =>
{
    var result = client.Rest.GetChannelAsync(msg.ChannelId).Result;  // Blocking!
    return Task.CompletedTask;
});

// ✅ Correct: Async all the way
client.OnMessageCreated(async msg =>
{
    var channel = await client.Rest.GetChannelAsync(msg.ChannelId);
});

// Cause: Expensive operations in handler
// Solution: Offload to background task
client.OnMessageCreated(async msg =>
{
    // Quick response
    if (msg.Content == "!slow")
    {
        await client.Rest.CreateMessageAsync(msg.ChannelId, new()
        {
            Content = "Processing...",
        });
    }

    // Expensive operation in background
    _ = ProcessExpensiveTaskAsync(msg);
});

private async Task ProcessExpensiveTaskAsync(MessageCreateEvent msg)
{
    // Do heavy lifting here
    // Results can be posted back later
}
```

---

## Event Handling Problems

### Events Not Firing

**Symptom:**
```
Event handler never called
Message events not appearing
```

**Solutions:**

```csharp
// Cause 1: Intents not enabled
// ❌ Problem
var options = new PawSharpOptions
{
    Intents = GatewayIntents.Guilds,  // Missing MessageContent intent
};

// ✅ Solution
var options = new PawSharpOptions
{
    Intents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
};

// Cause 2: Using low-level API without required event name string
// ❌ Problem: On<T>() requires both type AND event name — missing string arg
client.Gateway.Events.On<MessageCreateEvent>(msg =>  // Won't compile!
{
    return Task.CompletedTask;
});

// ✅ Solution A: Use the convenience method (recommended)
client.OnMessageCreated(msg =>
{
    Console.WriteLine($"Message: {msg.Content}");
    return Task.CompletedTask;
});

// ✅ Solution B: Low-level — provide the event name string
client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", msg =>
{
    Console.WriteLine($"Message: {msg.Content}");
    return Task.CompletedTask;
});

// Cause 3: Handler throws exception
// ❌ Problem
client.OnMessageCreated(msg =>
{
    throw new Exception("Oops!");  // Silently swallowed
});

// ✅ Solution: Add error handling
client.OnMessageCreated(async msg =>
{
    try
    {
        // Your logic
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex}");
    }
});

// Cause 4: Global error visibility via middleware
// ✅ Solution: Use middleware for logging (no next() — all handlers always fire)
client.Gateway.Events.Use(async (eventName, eventData) =>
{
    Console.WriteLine($"[{eventName}] Processing event");
    await Task.CompletedTask;
    // Wrap individual handlers in try/catch for resilience
});
```

### Event Processing Order

**Symptom:**
```
Events processed in unexpected order
```

**Information:**

```csharp
// Events are processed in subscription order
client.OnMessageCreated(Handler1);  // Runs first
client.OnMessageCreated(Handler2);  // Runs second

// If Handler1 throws, Handler2 may not run
// Solution: Add error handling in each handler
client.OnMessageCreated(async msg =>
{
    try { await Handler1(msg); } catch { }
});

client.OnMessageCreated(async msg =>
{
    try { await Handler2(msg); } catch { }
});
```

---

## Caching Issues

### Stale Cache Data

**Symptom:**
```
Guild name changed but cache still shows old name
```

**Solutions:**

```csharp
// Cause: Cache not updated
// Solution: Subscribe to update events

client.OnGuildUpdated(async guild =>
{
    // Guild automatically updated in cache
    Console.WriteLine($"Guild updated: {guild.Name}");
});

// Solution: Clear specific cache entry
// (Not directly exposed - implement if needed)

// Solution: Use Redis (distributed) instead of in-memory
services.AddSingleton<IEntityCache>(
    new RedisCacheProvider("localhost:6379")
);
```

### Redis Connection Failed

**Symptom:**
```
Cannot connect to Redis
Caching not working
```

**Solutions:**

```csharp
// Cause 1: Redis not running
// Solution: Start Redis
// Windows: redis-server.exe
// Docker: docker run -d -p 6379:6379 redis

// Cause 2: Wrong connection string
// ❌ Problem
var cache = new RedisCacheProvider("wrong:host");

// ✅ Solution: Verify connection
var cache = new RedisCacheProvider("localhost:6379");

// Test connection
try
{
    var stats = cache.GetStatistics();
    Console.WriteLine("Redis connected");
}
catch (Exception ex)
{
    Console.WriteLine($"Redis error: {ex.Message}");
}

// Cause 3: Network/firewall
// Solution: Check firewall allows port 6379
// Test with Redis CLI:
// redis-cli ping
// Should return: PONG
```

---

## Getting Help

### Before Posting an Issue

1. **Check documentation**
   - [DEVELOPERS_GUIDE.md](./DEVELOPERS_GUIDE.md)
   - [GATEWAY_GUIDE.md](./GATEWAY_GUIDE.md)
   - [REST_API_GUIDE.md](./REST_API_GUIDE.md)
   - [PATTERNS_GUIDE.md](./PATTERNS_GUIDE.md)

2. **Enable debug logging**
   ```csharp
   services.AddLogging(builder =>
   {
       builder.SetMinimumLevel(LogLevel.Debug);
       builder.AddConsole();
   });
   ```

3. **Check Discord status**
   - Visit https://status.discord.com/
   - Is Discord having issues?

4. **Search existing issues**
   - GitHub issues
   - GitHub discussions
   - Stack Overflow

### Creating a Good Issue Report

Include:
```
**Version:** 0.6.1-alpha1
**Environment:** Windows 11, .NET 8.0
**Intents Used:** [list intents]

**Problem:**
[Clear description of issue]

**Code:**
[Minimal reproducible example]

**Error Message:**
[Full exception and stack trace]

**Expected Behavior:**
[What should happen]

**Actual Behavior:**
[What actually happens]
```

### Getting Debugging Info

```csharp
// Check versions
Console.WriteLine($"PawSharp: {typeof(DiscordClient).Assembly.GetName().Version}");
Console.WriteLine($".NET: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

// Check connection details
if (client.Gateway.IsConnected)
{
    Console.WriteLine("✅ Connected to gateway");
}
else
{
    Console.WriteLine("❌ Not connected to gateway");
}

// Check cache
var stats = client.Cache.GetStatistics();
Console.WriteLine($"Cache: {stats.CachedGuilds} guilds, {stats.CachedMessages} messages");

// Enable full HTTP logging (if available)
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
});
```

---

**Still stuck?** Open an issue on [GitHub](https://github.com/pawsharp/pawsharp/issues) with the information above!
