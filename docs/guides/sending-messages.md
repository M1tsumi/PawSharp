# Sending Messages

PawSharp provides multiple ways to send messages and interact with Discord channels. All operations go through the REST API - the Gateway only *receives* events.

---

## CreateMessageAsync

`DiscordClient.SendMessageAsync()` is the primary method for sending messages. It delegates to `DiscordRestClient.CreateMessageAsync()`.

### Overloads

```csharp
// 1. Plain text only
Message? msg = await client.SendMessageAsync(channelId, "Hello, world!");

// 2. Text with a single embed
Message? msg = await client.SendMessageAsync(channelId, "Check this:", embed);

// 3. Full request object (embeds, components, polls, etc.)
Message? msg = await client.SendMessageAsync(channelId, new CreateMessageRequest
{
 Content = "Hello!",
 Embeds = embeds,
 Components = components,
 Tts = false,
});

// 4. Embed only (convenience)
Message? msg = await client.SendEmbedAsync(channelId, embed);

// 5. TrySendMessageAsync - returns null on failure instead of throwing
Message? msg = await client.TrySendMessageAsync(channelId, "Hello!");
if (msg == null) Console.WriteLine("Failed to send message");
```

### Return Value

All send methods return `Message?` - `null` if the request failed (rate limited, permissions, etc.). The returned `Message` contains the server-assigned `Id`, `Timestamp`, and any computed fields.

>  **Tip:** Check the returned `Message.Id` to store or reference the sent message for later editing.

---

## Message Content

```csharp
// Simple text
await client.SendMessageAsync(channelId, "Hello, Discord!");

// With formatting
await client.SendMessageAsync(channelId, "**Bold** *italic* __underline__ ~~strikethrough~~ `code`");

// With mentions
await client.SendMessageAsync(channelId, "<@123456789012345678> check this out!");
```

### Content Limits

| Constraint | Limit |
|-----------|-------|
| Max characters | 2,000 |
| Max embeds per message | 10 |
| Max fields per embed | 25 |
| Max components per action row | 5 |
| Total embed characters | 6,000 |

>  **Warning:** Content is validated before sending. `ContentValidator.ValidateMessageContent()` throws `ValidationException` if content exceeds 2,000 characters or contains invalid characters.

---

## Embeds

Embeds are rich, structured content blocks. You can build them manually or use the fluent builder.

### Using the Fluent EmbedBuilder

```csharp
using PawSharp.Core.Builders;

var embed = new EmbedBuilder()
 .WithTitle("Welcome to PawSharp!") // max 256 chars
 .WithDescription("A C# Discord API wrapper") // max 4096 chars
 .WithUrl("https://github.com/M1tsumi/Pawsharp")
 .WithColor(0x5865F2) // Discord blurple
 .WithAuthor("PawSharp Docs", iconUrl: "https://example.com/icon.png")
 .WithThumbnail("https://example.com/thumb.png")
 .WithImage("https://example.com/image.png")
 .AddField("Version", "1.1.0", inline: true) // max 25 fields
 .AddField("Language", "C# 12", inline: true)
 .AddField("License", "MIT")
 .WithFooter("Happy coding!", iconUrl: "https://example.com/footer.png")
 .WithTimestamp()
 .Build();

await client.SendMessageAsync(channelId, new CreateMessageRequest
{
 Content = "Here's the info you requested:",
 Embeds = new List<Embed> { embed },
});
```

### Manual Embed Building

```csharp
var embed = new Embed
{
 Title = "Server Status",
 Description = "All systems operational",
 Color = 0x2ECC71, // Green
 Timestamp = DateTime.UtcNow,
 Footer = new EmbedFooter { Text = "Last checked" },
 Fields = new List<EmbedField>
 {
 new() { Name = "API", Value = " Online", Inline = true },
 new() { Name = "Database", Value = " Online", Inline = true },
 new() { Name = "Latency", Value = "12ms", Inline = true },
 },
};

await client.SendEmbedAsync(channelId, embed);
```

### Common Embed Patterns

```csharp
// Success
public static Embed Success(string message) => new EmbedBuilder()
 .WithTitle("Success")
 .WithDescription(message)
 .WithColor(0x2ECC71)
 .WithTimestamp()
 .Build();

// Error
public static Embed Error(string message) => new EmbedBuilder()
 .WithTitle("Error")
 .WithDescription(message)
 .WithColor(0xE74C3C)
 .WithTimestamp()
 .Build();

// Info
public static Embed Info(string title, string description) => new EmbedBuilder()
 .WithTitle(title)
 .WithDescription(description)
 .WithColor(0x3498DB)
 .WithTimestamp()
 .Build();
```

---

## Components

Add interactive buttons, select menus, and text inputs to messages.

```csharp
// Message with a button
await client.SendMessageAsync(channelId, new CreateMessageRequest
{
 Content = "Click the button below:",
 Components = new List<MessageComponent>
 {
 new()
 {
 Type = ComponentType.ActionRow,
 Components = new List<MessageComponent>
 {
 new()
 {
 Type = ComponentType.Button,
 Style = ButtonStyle.Primary,
 Label = "Click Me!",
 CustomId = "my_button",
 },
 },
 },
 },
});
```

>  **Tip:** See the [Components Guide](./components.md) for detailed documentation on building interactive components.

---

## Reply to Messages

Replying creates a threaded reply visible in the Discord UI, with a reference line to the original message.

```csharp
// In your message handler
client.OnMessageCreated(async msg =>
{
 if (msg.Content == "!ping")
 {
 // Reply with text
 await client.ReplyAsync(msg, "Pong!");

 // Reply with embed
 var embed = new EmbedBuilder()
 .WithTitle("Pong!")
 .WithColor(0x5865F2)
 .Build();

 await client.ReplyAsync(msg, "Pong!", embed);

 // Reply with full request
 await client.ReplyAsync(msg, new CreateMessageRequest
 {
 Content = "Pong!",
 Components = components,
 });
 }
});
```

### TryReplyAsync - Safe Reply

```csharp
client.OnMessageCreated(async msg =>
{
 var reply = await client.TryReplyAsync(msg, "Processing...");
 if (reply == null)
 {
 _logger.LogWarning("Failed to reply to {MessageId}", msg.Id);
 return;
 }

 var result = await ProcessAsync();
 await client.EditMessageAsync(reply.ChannelId, reply.Id, result);
});
```

>  **Tip:** Use `TryReplyAsync` when you don't want to crash the event handler on failure. Log the failure and move on.

---

## Forward Messages

Discord supports forwarding messages between channels using a message snapshot.

```csharp
// Simple forward
var forwarded = await client.ForwardMessageAsync(
 targetChannelId: 987654321098765432,
 sourceChannelId: 123456789012345678,
 sourceMessageId: 111111111111111111);

if (forwarded != null)
 Console.WriteLine($"Forwarded: {forwarded.Id}");

// Forward with additional content
await client.ForwardMessageAsync(
 targetChannelId: 987654321098765432,
 sourceChannelId: 123456789012345678,
 sourceMessageId: 111111111111111111,
 content: "Check out this message:");

// Forward with a full request (embeds + content)
await client.ForwardMessageAsync(
 targetChannelId: 987654321098765432,
 sourceChannelId: 123456789012345678,
 sourceMessageId: 111111111111111111,
 new CreateMessageRequest
 {
 Content = "Look at this!",
 Embeds = new List<Embed> { myEmbed },
 },
 failIfNotExists: false); // Silent fail if source deleted
```

---

## Crosspost Announcement Messages

Publish a message in an announcement channel to all subscribed channels.

```csharp
var crossposted = await client.CrosspostMessageAsync(
 channelId: announcementChannelId,
 messageId: messageId);

if (crossposted != null)
 Console.WriteLine($"Crossposted message: {crossposted.Id}");
```

>  **Warning:** Crossposting has a 5-minute cooldown per channel. Discord returns a 429 if you exceed this.

---

## Sending Files

Attach files to messages using `SendFileAsync` or `SendFilesAsync`.

```csharp
// Single file
await using var image = File.OpenRead("screenshot.png");
var msg = await client.SendFileAsync(
 channelId,
 image,
 "screenshot.png");

Console.WriteLine($"Sent file: {msg?.Attachments?.FirstOrDefault()?.Url}");

// Single file with text content
await using var report = File.OpenRead("report.pdf");
await client.SendFileAsync(channelId, report, "report.pdf",
 new CreateMessageRequest { Content = "Monthly report attached:" });

// Multiple files (up to 10)
var files = new[]
{
 (File.OpenRead("image1.png") as Stream, "image1.png"),
 (File.OpenRead("image2.png") as Stream, "image2.png"),
};

await client.SendFilesAsync(channelId, files,
 new CreateMessageRequest { Content = "Here are the images:" });
```

>  **Tip:** Max attachment size is 25MB (50MB with Nitro). Use `SendFilesAsync` to send up to 10 files in one message.

---

## Rate Limit Implications

Every message send consumes rate limit budget. PawSharp's REST client handles rate limits automatically with `IAdvancedRateLimiter`.

```csharp
// Monitor rate limit telemetry
if (client.SupportsRateLimitTelemetry)
{
 client.RateLimitObserved += (sender, telemetry) =>
 {
 Console.WriteLine(
 $"Rate limit on {telemetry.Endpoint}: " +
 $"reset in {telemetry.RetryAfterMs}ms");
 };
}
```

### Best Practices for High-Volume Sending

```csharp
// Use a semaphore to control concurrency
private readonly SemaphoreSlim _sendGate = new(3, 3);

public async Task<Message?> ThrottledSendAsync(ulong channelId, string content)
{
 await _sendGate.WaitAsync();
 try
 {
 return await client.TrySendMessageAsync(channelId, content);
 }
 finally
 {
 await Task.Delay(200); // Space requests
 _sendGate.Release();
 }
}
```

---

## Editing and Deleting Messages

```csharp
// Edit content
await client.EditMessageAsync(channelId, messageId, "Updated content");

// Edit with full request
await client.EditMessageAsync(channelId, messageId, new EditMessageRequest
{
 Content = "New text",
 Embeds = new List<Embed> { newEmbed },
 Components = newComponents,
});

// Delete
bool deleted = await client.DeleteMessageAsync(channelId, messageId);

// Bulk delete (2-100 messages)
var ids = new List<ulong> { id1, id2, id3 };
await client.BulkDeleteMessagesAsync(channelId, ids);

// Pin / Unpin
await client.PinMessageAsync(channelId, messageId);
await client.UnpinMessageAsync(channelId, messageId);
```

---

## Direct Messages

Send a direct message to a user. This automatically creates or reuses the DM channel.

```csharp
var dm = await client.SendDirectMessageAsync(
 userId: 123456789012345678,
 content: "Hello from PawSharp!");

if (dm == null)
 Console.WriteLine("Failed to send DM - user may have DMs disabled.");
```

>  **Warning:** You cannot DM users who share no mutual guilds with the bot unless the bot's `DM_MESSAGES` intent is enabled and the user has allowed DMs from server members.

---

## Polls

Create polls with multiple answers.

```csharp
await client.SendMessageAsync(channelId, new CreateMessageRequest
{
 Poll = new CreatePollRequest
 {
 Question = "What's your favorite color?",
 Duration = 24, // Hours (max 32 days = 768 hours)
 AllowMultiselect = false,
 Answers = new List<PollAnswer>
 {
 new() { Answer = "Red", Emoji = new Emoji { Name = "" } },
 new() { Answer = "Blue", Emoji = new Emoji { Name = "" } },
 new() { Answer = "Green", Emoji = new Emoji { Name = "" } },
 },
 },
});

// End a poll early
var ended = await client.EndPollAsync(channelId, messageId);

// Get voters for a specific answer
var voters = await client.GetAnswerVotersAsync(channelId, messageId, answerId: 0);
```

---

## Performance Notes

| Operation | Limit | Notes |
|-----------|-------|-------|
| Message content | 2,000 chars | Exceeding throws `ValidationException` |
| Embeds per message | 10 | Discord ignores extras |
| Embed total characters | 6,000 | `EmbedBuilder.Build()` enforces this |
| Embed fields | 25 | Per embed |
| Components per action row | 5 | Discord limits row to 5 items |
| Action rows per message | 5 | Max 5 rows, 25 total components |
| Attachment size | 25 MB (50 MB Nitro) | Per file |
| Attachments per message | 10 | |
| File sends per minute | Varies | Check Discord rate limits |
| Poll duration | 1 - 768 hours | Max 32 days |

```csharp
// Example: Sending a message with maximum embeds
var embeds = new List<Embed>();
for (int i = 0; i < 10; i++)
 embeds.Add(new Embed { Title = $"Embed {i + 1}", Description = new string('x', 400) });

await client.SendMessageAsync(channelId, new CreateMessageRequest
{
 Embeds = embeds,
 // Some embeds may be silently dropped by Discord if total > 6000 chars
});
```

>  **Good:** Build a helper that validates embed counts before sending.

>  **Bad:** Sending a message with all 10 embeds at maximum field count - Discord will silently truncate to 25 fields and 6000 total chars.

---

## Full Example - Command Response with Rich Content

```csharp
client.OnMessageCreated(async msg =>
{
 if (msg.Author.IsBot) return;

 switch (msg.Content)
 {
 case "!info":
 var embed = new EmbedBuilder()
 .WithTitle("Bot Info")
 .WithColor(0x5865F2)
 .AddField("Uptime", _uptime.Elapsed.ToString(@"d\.hh\:mm\:ss"), true)
 .AddField("Guilds", client.Gateway.CurrentState == GatewayState.Ready
 ? "Connected" : "Unknown", true)
 .AddField("Latency",
 $"{client.Gateway.LastHeartbeatLatency?.TotalMilliseconds:F0}ms", true)
 .WithTimestamp()
 .Build();

 await client.ReplyAsync(msg, new CreateMessageRequest
 {
 Embeds = new List<Embed> { embed },
 Components = new List<MessageComponent>
 {
 new()
 {
 Type = ComponentType.ActionRow,
 Components = new List<MessageComponent>
 {
 new()
 {
 Type = ComponentType.Button,
 Style = ButtonStyle.Link,
 Label = "GitHub",
 Url = "https://github.com/M1tsumi/Pawsharp",
 },
 },
 },
 },
 });
 break;

 case "!announce":
 var crossposted = await client.CrosspostMessageAsync(
 msg.ChannelId, msg.Reference?.MessageId ?? 0);
 if (crossposted != null)
 await client.ReplyAsync(msg, "Announcement published!");
 break;
 }
});
```

---

## Related Guides

- [Receiving Messages](./receiving-messages.md) - Handling incoming message events
- [Events](./events.md) - Event dispatch pipeline and intents
- [Components](./components.md) - Building interactive components
- [Gateway Connection](./gateway.md) - Connection lifecycle
