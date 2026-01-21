# PawSharp API Reference

This document provides a comprehensive reference for the PawSharp API, including namespaces, classes, methods, and usage patterns.

## Core Namespaces

### PawSharp.Core

#### Entities
- **Guild**: Represents a Discord guild/server with properties like Id, Name, OwnerId, etc.
- **Channel**: Base class for all channel types (text, voice, category, etc.)
- **Message**: Discord message with content, author, embeds, attachments
- **User**: Discord user with username, discriminator, avatar, etc.
- **Role**: Guild role with permissions, color, position
- **Member**: Guild member combining user and guild-specific data
- **Emoji**: Custom emoji with id, name, animated status

#### Enums
- **PermissionFlags**: Discord permission flags (SendMessages, ManageRoles, etc.)
- **MessageType**: Types of messages (Default, Reply, ApplicationCommand, etc.)
- **ChannelType**: Channel types (GuildText, GuildVoice, DM, etc.)
- **GatewayIntents**: Gateway intents for event subscriptions

#### Exceptions
- **ValidationException**: Thrown for invalid input parameters
- **RateLimitException**: Thrown when hitting Discord's rate limits (includes RetryAfter)
- **DiscordApiException**: Thrown for Discord API errors (includes StatusCode)
- **GatewayException**: Thrown for WebSocket/gateway connection issues
- **DeserializationException**: Thrown when JSON parsing fails

#### Models
- **PawSharpOptions**: Configuration options (Token, Intents, Shards, etc.)
- **CreateMessageRequest**: Request model for sending messages
- **Embed**: Rich embed structure for messages

### PawSharp.API

#### RestClient
Main HTTP client for Discord API endpoints.

**Key Methods:**
- `CreateMessageAsync(channelId, request)`: Send a message
- `EditMessageAsync(channelId, messageId, request)`: Edit a message
- `DeleteMessageAsync(channelId, messageId)`: Delete a message
- `GetChannelAsync(channelId)`: Get channel information
- `CreateChannelAsync(guildId, request)`: Create a channel
- `GetGuildAsync(guildId)`: Get guild information
- `GetGuildMembersAsync(guildId, options)`: List guild members
- `CreateGuildRoleAsync(guildId, request)`: Create a role
- `AddGuildMemberRoleAsync(guildId, userId, roleId)`: Assign role
- `GetUserAsync(userId)`: Get user information
- `CreateDMAsync(userId)`: Create DM channel

#### Rate Limiting
- **AdvancedRateLimiter**: Per-route bucket management
- Automatic 429 handling with retry logic
- Configurable timeouts and cancellation

### PawSharp.Cache

#### Interfaces
- **ICacheProvider**: Interface for cache implementations
- **IEntityCache**: High-level entity caching

#### Providers
- **MemoryCacheProvider**: In-memory LRU cache with TTL
  - Configurable size limits per entity type
  - Automatic cleanup and eviction
  - Thread-safe operations

**CacheStats**: Monitoring class with hit/miss counts, memory usage

### PawSharp.Gateway

#### GatewayClient
WebSocket client for real-time events.

**Key Methods:**
- `ConnectAsync()`: Establish gateway connection
- `DisconnectAsync()`: Close connection gracefully
- `SendAsync(opcode, data)`: Send gateway payload

**Events:**
- `OnMessageCreate`: Fired when a message is created
- `OnMessageUpdate`: Fired when a message is edited
- `OnMessageDelete`: Fired when a message is deleted
- `OnGuildCreate`: Fired when bot joins a guild
- `OnGuildUpdate`: Fired when guild is updated
- `OnGuildDelete`: Fired when bot leaves a guild
- `OnChannelCreate/Update/Delete`: Channel events
- `OnGuildMemberAdd/Update/Remove`: Member events
- `OnReady`: Fired when gateway is ready
- `OnResumed`: Fired when session is resumed

#### ShardManager
Manages multiple gateway shards for large bots.

**Key Methods:**
- `ConnectAllAsync()`: Connect all shards
- `DisconnectAllAsync()`: Disconnect all shards
- `GetShardStatus(shardId)`: Get status of specific shard
- `ReconnectShardAsync(shardId)`: Reconnect individual shard
- `CalculateRecommendedShardCount(guildCount)`: Auto-sharding helper

**Events:**
- `OnShardConnected`: Fired when shard connects
- `OnShardDisconnected`: Fired when shard disconnects
- `OnShardFailed`: Fired when shard fails

### PawSharp.Interactions

#### InteractionHandler
Manages slash commands and component interactions.

**Key Methods:**
- `RegisterCommand(name, handler)`: Register slash command
- `RegisterComponent(customId, handler)`: Register component handler
- `RespondAsync(interactionId, token, response)`: Send interaction response

**Models:**
- **InteractionResponse**: Response structure for interactions
- **InteractionCallbackData**: Callback data for responses
- **ApplicationCommand**: Slash command definition

### PawSharp.Commands

#### CommandsExtension
Attribute-based command framework.

**Key Methods:**
- `UseCommands(prefix)`: Enable command processing
- `RegisterModule(module)`: Register command module

**BaseCommandModule**: Base class for command modules
- `[Command("name")]`: Command attribute
- `[Description("desc")]`: Description attribute
- `[Alias("alias")]`: Command alias

### PawSharp.Interactivity

#### InteractivityExtension
Framework for interactive experiences.

**Key Methods:**
- `UseInteractivity()`: Enable interactivity
- `WaitForReactionAsync(user, emoji)`: Wait for user reaction
- `CollectReactionsAsync(message, timeout)`: Collect multiple reactions
- `CreatePollAsync(message, options)`: Create reaction poll
- `GeneratePagesInEmbed(content)`: Paginate long content

### PawSharp.Voice

#### VoiceClient/VoiceConnection
Voice channel connectivity (experimental).

**Key Methods:**
- `ConnectAsync(voiceChannel)`: Connect to voice channel
- `StartCapture()`: Begin audio capture
- `PlayAudioAsync(audioData)`: Play audio data
- `DisconnectAsync()`: Disconnect from voice

**Note:** Voice support is experimental and limited. Full Opus encoding/decoding and DAVE E2EE not implemented.

---

## Configuration Options

### PawSharpOptions
```csharp
public class PawSharpOptions
{
    public string Token { get; set; } // Bot token
    public GatewayIntents Intents { get; set; } // Gateway intents
    public int Shards { get; set; } = 1; // Number of shards for this instance
    public int ShardCount { get; set; } = 1; // Total shards across instances
    public int ShardOffset { get; set; } = 0; // Shard offset for multi-process
    public bool EnableCompression { get; set; } = true; // Zlib compression
    public int MaxMissedHeartbeatAcks { get; set; } = 3; // Heartbeat tolerance
    public int ApiVersion { get; set; } = 10; // Discord API version
}
```

### CacheOptions
```csharp
public class CacheOptions
{
    public int MaxGuilds { get; set; } = 1000;
    public int MaxChannels { get; set; } = 5000;
    public int MaxUsers { get; set; } = 5000;
    public int MaxMessages { get; set; } = 10000;
    public int MaxMembersPerGuild { get; set; } = 1000;
    public int MaxEmojisPerGuild { get; set; } = 100;
}
```

---

## Dependency Injection

PawSharp is designed for .NET DI:

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(new PawSharpOptions { Token = token });
services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
services.AddSingleton<DiscordClient>();
// Add other services as needed

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

---

## Error Handling Patterns

All PawSharp methods throw typed exceptions:

```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)
{
    // Invalid input (wrong ID format, content too long, etc.)
}
catch (RateLimitException ex)
{
    // Rate limited - wait ex.RetryAfter seconds
    await Task.Delay(ex.RetryAfter * 1000);
}
catch (DiscordApiException ex)
{
    // API error - check ex.StatusCode
}
catch (GatewayException ex)
{
    // Connection issue
}
```

---

## Best Practices

1. **Always handle exceptions** - PawSharp throws instead of returning null
2. **Use dependency injection** - All components support DI
3. **Configure intents properly** - Only request needed intents
4. **Implement rate limit handling** - Respect Discord's limits
5. **Monitor cache usage** - Use CacheStats for performance monitoring
6. **Handle reconnections gracefully** - Gateway may disconnect
7. **Use async/await throughout** - All operations are async
8. **Validate input early** - Catch ValidationException before API calls

---

For detailed XML documentation, see the source code or generated API docs. For examples, check the [examples/](../examples/) directory.