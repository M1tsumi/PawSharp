# PawSharp Quick Reference

A comprehensive cheat sheet for common PawSharp tasks and patterns.

## Setup & Configuration

### Basic Bot Setup
```csharp
using PawSharp.Client;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton(new PawSharpOptions
{
    Token = "your-bot-token",
    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
});
services.AddPawSharpClient();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<DiscordClient>();
```

### Advanced Configuration
```csharp
var options = new PawSharpOptions
{
    Token = botToken,
    Intents = GatewayIntents.AllNonPrivileged,
    Shards = 2,                    // For sharding
    ShardCount = 2,
    EnableCompression = true,
    MaxMissedHeartbeatAcks = 3,
    ApiVersion = 10
};
```

## REST API Operations

### Messages
```csharp
// Send message
var message = await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Hello World!",
    Embeds = new[] { new Embed { Title = "Test", Description = "Description" } }
});

// Edit message
await client.Rest.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
    Content = "Updated content"
});

// Delete message
await client.Rest.DeleteMessageAsync(channelId, messageId);

// Get messages
var messages = await client.Rest.GetChannelMessagesAsync(channelId, new GetChannelMessagesRequest
{
    Limit = 50,
    Before = messageId
});
```

### Channels
```csharp
// Get channel
var channel = await client.Rest.GetChannelAsync(channelId);

// Create channel
var newChannel = await client.Rest.CreateChannelAsync(guildId, new CreateChannelRequest
{
    Name = "new-channel",
    Type = ChannelType.GuildText
});

// Modify channel
await client.Rest.ModifyChannelAsync(channelId, new ModifyChannelRequest
{
    Name = "updated-name"
});
```

### Guilds
```csharp
// Get guild
var guild = await client.Rest.GetGuildAsync(guildId);

// Get members
var members = await client.Rest.GetGuildMembersAsync(guildId, new GetGuildMembersRequest
{
    Limit = 100
});

// Create role
var role = await client.Rest.CreateGuildRoleAsync(guildId, new CreateGuildRoleRequest
{
    Name = "New Role",
    Permissions = PermissionFlags.SendMessages | PermissionFlags.ReadMessageHistory
});

// Add role to member
await client.Rest.AddGuildMemberRoleAsync(guildId, userId, roleId);
```

## Gateway Events

### Event Handling
```csharp
// Message events
client.Gateway.OnMessageCreate += async message =>
{
    if (message.Content == "!ping")
    {
        await message.Channel.SendMessageAsync("Pong!");
    }
};

client.Gateway.OnMessageUpdate += async (oldMessage, newMessage) =>
{
    Console.WriteLine($"Message edited: {oldMessage.Content} -> {newMessage.Content}");
};

// Guild events
client.Gateway.OnGuildCreate += async guild =>
{
    Console.WriteLine($"Joined guild: {guild.Name}");
};

client.Gateway.OnGuildMemberAdd += async member =>
{
    Console.WriteLine($"{member.User.Username} joined {member.Guild.Name}");
};

// Ready event
client.Gateway.OnReady += async ready =>
{
    Console.WriteLine($"Bot ready! Logged in as {ready.User.Username}");
};
```

## Commands Framework

### Basic Commands
```csharp
using PawSharp.Commands;

var commands = client.UseCommands("!");

[Command("ping")]
[Description("Check bot latency")]
public async Task PingAsync(CommandContext ctx)
{
    await ctx.RespondAsync("Pong!");
}

[Command("echo")]
[Description("Echo a message")]
public async Task EchoAsync(CommandContext ctx, string message)
{
    await ctx.RespondAsync(message);
}

[Command("userinfo")]
[Description("Get user info")]
public async Task UserInfoAsync(CommandContext ctx, IUser user = null)
{
    user ??= ctx.User;
    var embed = new Embed
    {
        Title = $"{user.Username}#{user.Discriminator}",
        Fields = new[]
        {
            new EmbedField { Name = "ID", Value = user.Id.ToString() },
            new EmbedField { Name = "Created", Value = user.CreatedAt.ToString("R") }
        }
    };
    await ctx.RespondAsync(embed: embed);
}
```

## Slash Commands & Interactions

### Registering Slash Commands
```csharp
using PawSharp.Interactions;

client.Interactions.RegisterCommand("ping", async interaction =>
{
    var response = new InteractionResponse
    {
        Type = (int)InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionCallbackData
        {
            Content = "Pong! 🏓"
        }
    };
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token, response);
});

// With options
client.Interactions.RegisterCommand("say", async interaction =>
{
    var option = interaction.Data.Options.FirstOrDefault(o => o.Name == "message");
    var content = option?.Value?.ToString() ?? "Nothing to say";

    var response = new InteractionResponse
    {
        Type = (int)InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionCallbackData { Content = content }
    };
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token, response);
});
```

### Component Interactions
```csharp
// Register button handler
client.Interactions.RegisterComponent("my_button", async interaction =>
{
    var response = new InteractionResponse
    {
        Type = (int)InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionCallbackData
        {
            Content = "Button clicked!",
            Flags = (int)InteractionCallbackDataFlags.Ephemeral
        }
    };
    await client.Interactions.RespondAsync(interaction.Id, interaction.Token, response);
});

// Create message with button
var message = await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Click the button!",
    Components = new[]
    {
        new ActionRow
        {
            Components = new[]
            {
                new Button
                {
                    CustomId = "my_button",
                    Label = "Click Me",
                    Style = ButtonStyle.Primary
                }
            }
        }
    }
});
```

## Interactivity

### Reaction Waiting
```csharp
using PawSharp.Interactivity.Extensions;

var interactivity = client.UseInteractivity();

// Wait for specific reaction
var result = await message.WaitForReactionAsync(user, "👍");
if (!result.TimedOut)
{
    await message.RespondAsync("Thanks for the thumbs up!");
}

// Collect multiple reactions
var reactions = await message.CollectReactionsAsync(client, TimeSpan.FromMinutes(5));
Console.WriteLine($"Collected {reactions.Count} reactions");
```

### Pagination
```csharp
// Paginate long content
var pages = interactivity.GeneratePagesInEmbed(longText);
await channel.SendPaginatedMessageAsync(user, pages);
```

### Polls
```csharp
// Create poll
await message.CreatePollAsync("Favorite language?",
    new[] { "C#", "Python", "JavaScript", "Rust" });
```

## Caching

### Cache Operations
```csharp
// Get cached entities
var guild = client.Cache.GetGuild(guildId);
var user = client.Cache.GetUser(userId);
var channel = client.Cache.GetChannel(channelId);

// Cache statistics
var stats = client.Cache.GetStats();
Console.WriteLine($"Cache hits: {stats.Hits}, Misses: {stats.Misses}");
```

## Voice (Experimental)

### Basic Voice Connection
```csharp
using PawSharp.Voice;

var voice = client.UseVoice();
var connection = await voice.ConnectAsync(voiceChannel);

// Start capturing (experimental)
connection.StartCapture();

// Play audio (experimental)
await connection.PlayAudioAsync(audioData);

// Disconnect
await connection.DisconnectAsync();
```

## Sharding

### Basic Sharding
```csharp
using PawSharp.Gateway;

var options = new PawSharpOptions
{
    Token = token,
    Shards = 2,        // Shards for this instance
    ShardCount = 2     // Total shards
};

var shardManager = new ShardManager(options, logger);
await shardManager.ConnectAllAsync();

// Monitor shards
var status = shardManager.GetShardStatus(0);
Console.WriteLine($"Shard 0 status: {status}");
```

## Error Handling

### Comprehensive Error Handling
```csharp
try
{
    await client.Rest.CreateMessageAsync(channelId, request);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
    // Fix input and retry
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, retry in {ex.RetryAfter}s");
    await Task.Delay(ex.RetryAfter * 1000);
}
catch (DiscordApiException ex)
{
    Console.WriteLine($"Discord error {ex.StatusCode}: {ex.Message}");
    // Handle specific status codes
}
catch (GatewayException ex)
{
    Console.WriteLine($"Gateway error: {ex.Message}");
    // May need to reconnect
}
```

## Best Practices

### Async/Await
```csharp
// ✅ Good
client.OnMessageCreate += async message =>
{
    await message.Channel.SendMessageAsync("Response");
};

// ❌ Bad - blocks thread
client.OnMessageCreate += message =>
{
    message.Channel.SendMessageAsync("Response").Wait();
};
```

### Dependency Injection
```csharp
// ✅ Use DI
services.AddSingleton<DiscordClient>();
var client = provider.GetRequiredService<DiscordClient>();

// ❌ Avoid manual instantiation
var client = new DiscordClient(options, cache, logger, restClient);
```

### Resource Management
```csharp
// ✅ Proper cleanup
await using var client = provider.GetRequiredService<DiscordClient>();
await client.ConnectAsync();
// Bot logic
await client.DisconnectAsync();
```

### Logging
```csharp
// ✅ Structured logging
_logger.LogInformation("Bot connected to {GuildCount} guilds", guilds.Count);
_logger.LogError(ex, "Failed to send message to channel {ChannelId}", channelId);
```

---

See the [examples/](../examples/) directory for complete working code samples.