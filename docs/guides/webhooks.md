# Webhooks

Learn how to create, execute, and manage Discord webhooks with PawSharp.

## Table of Contents

1. [What Are Webhooks?](#what-are-webhooks)
2. [Webhook Types](#webhook-types)
3. [Creating Webhooks](#creating-webhooks)
4. [Retrieving Webhooks](#retrieving-webhooks)
5. [Executing Webhooks](#executing-webhooks)
6. [Editing & Deleting Webhooks](#editing--deleting-webhooks)
7. [Webhook Security](#webhook-security)
8. [Following Announcement Channels](#following-announcement-channels)
9. [Slack & GitHub Compatible Webhooks](#slack--github-compatible-webhooks)
10. [Complete Example](#complete-example)

---

## What Are Webhooks?

Webhooks are a way to send messages to Discord channels **without a bot user**. They use a fixed URL containing a token that grants permission to post. Webhooks can:

- Post messages with custom usernames and avatars per-call
- Include embeds, components, and files
- Be used by external services (GitHub, CI/CD, monitoring tools)
- Create threads in forum channels

---

## Webhook Types

```csharp
public enum WebhookType
{
    Incoming = 1,           // Standard webhook — posts to a channel
    ChannelFollower = 2,    // Auto-generated when following an announcement channel
    Application = 3         // Used by interactions (slash commands)
}
```

| Property | Description |
|----------|-------------|
| `Id` | Snowflake ID (inherited from `DiscordEntity`) |
| `Type` | `Incoming`, `ChannelFollower`, or `Application` |
| `GuildId` | Guild the webhook belongs to |
| `ChannelId` | Channel the webhook posts to |
| `User` | Creator (not returned when using token auth) |
| `Name` | Default name |
| `Avatar` | Default avatar hash |
| `Token` | Secure token for `Incoming` webhooks |
| `Url` | Webhook URL (returned from OAuth2 flow) |
| `SourceGuild` | Followed guild (channel follower only) |
| `SourceChannel` | Followed channel (channel follower only) |

---

## Creating Webhooks

Requires `MANAGE_WEBHOOKS` permission on the channel.

```csharp
var webhook = await client.Rest.CreateWebhookAsync(channelId, new CreateWebhookRequest
{
    Name = "My Webhook",
    Avatar = "data:image/png;base64,iVBOR...", // optional, base64-encoded image
});

Console.WriteLine($"Webhook created: {webhook?.Id}");
Console.WriteLine($"Token: {webhook?.Token}");   // store this!
```

### ✅ Do

- Store the `Token` securely — it's shown only once in the response
- Set a descriptive `Name` so channel moderators can identify it

### ❌ Don't

- Log or expose the webhook token in client-side code or logs
- Use the bot token in `Avatar` — encode the actual image data

⚠️ **Avatar format:** Base64-encoded image data with a data URI prefix: `data:image/png;base64,...`

---

## Retrieving Webhooks

```csharp
// Get all webhooks in a channel
var webhooks = await client.Rest.GetChannelWebhooksAsync(channelId);
foreach (var wh in webhooks ?? new())
    Console.WriteLine($"{wh.Name} (type {wh.Type})");

// Get all webhooks in a guild
var guildWebhooks = await client.Rest.GetGuildWebhooksAsync(guildId);

// Get a specific webhook by ID (requires bot auth)
var webhook = await client.Rest.GetWebhookAsync(webhookId);

// Get a webhook using its token (no auth required — anyone with the URL)
var webhook = await client.Rest.GetWebhookWithTokenAsync(webhookId, token);
```

---

## Executing Webhooks

```csharp
await client.Rest.ExecuteWebhookAsync(
    webhookId,
    webhookToken,
    new ExecuteWebhookRequest
    {
        Content = "Hello from webhook!",
        Username = "Custom Name",           // override default name
        AvatarUrl = "https://example.com/avatar.png", // override avatar
        Embeds = new List<Embed>
        {
            new Embed { Title = "Embedded!", Description = "In a webhook" }
        },
        Tts = false,
    });
```

### Wait Parameter

Set `Wait = true` to receive the created `Message` object in the response:

```csharp
var msg = await client.Rest.ExecuteWebhookAsync(
    webhookId, webhookToken,
    new ExecuteWebhookRequest
    {
        Content = "I need the message back!",
        Wait = true,  // adds ?wait=true to the request
    });

Console.WriteLine($"Message ID: {msg?.Id}");
```

⚠️ When `Wait = true`, Discord returns the message and the webhook **token is omitted** from the response.

### Executing into a Thread

```csharp
var msg = await client.Rest.ExecuteWebhookAsync(
    webhookId, webhookToken,
    new ExecuteWebhookRequest
    {
        Content = "Posting into a thread",
    },
    threadId: 123456789012345678);  // optional thread_id parameter
```

💡 The `threadId` parameter appends `?thread_id=...` to the URL. The webhook must already have permission to post in that thread.

```csharp
// The full request model supports:
public class ExecuteWebhookRequest
{
    public string? Content { get; set; }
    public List<Embed>? Embeds { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? Tts { get; set; }
    public List<MessageComponent>? Components { get; set; }
    public bool Wait { get; set; }              // [JsonIgnore] — query param
    public string? ThreadName { get; set; }     // for forum/media channels
}
```

### Managing Webhook Messages

```csharp
// Get a webhook message
var msg = await client.Rest.GetWebhookMessageAsync(webhookId, token, messageId);

// Edit a webhook message
await client.Rest.EditWebhookMessageAsync(webhookId, token, messageId, new()
{
    Content = "Updated content",
    Embeds = new List<Embed> { updatedEmbed },
});

// Delete a webhook message
await client.Rest.DeleteWebhookMessageAsync(webhookId, token, messageId);
```

---

## Editing & Deleting Webhooks

### Modify Webhook Properties (requires bot auth)

```csharp
var updated = await client.Rest.ModifyWebhookAsync(webhookId, new ModifyWebhookRequest
{
    Name = "Renamed Webhook",
    Avatar = "data:image/png;base64,...",
    ChannelId = newChannelId,    // move to a different channel
});
```

### Modify with Token (no bot auth needed)

```csharp
var updated = await client.Rest.ModifyWebhookWithTokenAsync(
    webhookId, token,
    new ModifyWebhookRequest { Name = "Updated via Token" });
```

### Delete Webhook

```csharp
bool deleted = await client.Rest.DeleteWebhookAsync(webhookId);

// Or with token (no bot auth needed)
bool deleted = await client.Rest.DeleteWebhookWithTokenAsync(webhookId, token);
```

---

## Webhook Security

⚠️ **Treat webhook tokens like passwords.**

| Rule | Reason |
|------|--------|
| Never expose tokens in client-side code | Anyone with the URL can post as the webhook |
| Store tokens in environment variables or a vault | Prevent accidental leaks in source control |
| Use token-based operations when possible | `ModifyWebhookWithTokenAsync` / `DeleteWebhookWithTokenAsync` don't require `MANAGE_WEBHOOKS` |

✅ **Best practice:** Store webhook ID and token in configuration:

```csharp
// appsettings.json
{
  "Webhooks": {
    "LogChannel": { "Id": "123456789", "Token": "abc123..." }
  }
}

// Usage
var webhookSection = config.GetSection("Webhooks:LogChannel");
var id = ulong.Parse(webhookSection["Id"]!);
var token = webhookSection["Token"]!;
```

---

## Following Announcement Channels

Following an announcement channel creates a **Channel Follower** webhook automatically:

```csharp
var followed = await client.Rest.FollowAnnouncementChannelAsync(
    announcementChannelId,    // the announcement/news channel to follow
    targetChannelId           // where updates will be posted
);
```

✅ Requires `MANAGE_WEBHOOKS` on the target channel.
❌ Cannot be done via webhook token — requires bot authentication.

---

## Slack & GitHub Compatible Webhooks

Discord accepts payloads in Slack and GitHub formats for simple webhook integrations:

```csharp
// Slack-compatible format
await client.Rest.ExecuteSlackCompatibleWebhookAsync(
    webhookId, token,
    new { text = "Hello from Slack format!", username = "Slack Bot" },
    wait: false);

// GitHub-compatible format
await client.Rest.ExecuteGitHubCompatibleWebhookAsync(
    webhookId, token,
    new { content = "Commit pushed!", embeds = new[] { new { title = "Update" } } },
    wait: false);
```

---

## Complete Example

```csharp
using PawSharp.Client;
using PawSharp.API.Models;

var client = new PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN")
    .Build();

const ulong channelId = 123456789012345678;

// Create the webhook
var webhook = await client.Rest.CreateWebhookAsync(channelId, new()
{
    Name = "Announcement Bot",
    Avatar = "data:image/png;base64,iVBORw0KGgo...",
});

if (webhook?.Token is null)
{
    Console.WriteLine("Failed to create webhook");
    return;
}

Console.WriteLine($"Webhook {webhook.Id} created with token {webhook.Token}");

// Execute with custom name and embed
var msg = await client.Rest.ExecuteWebhookAsync(
    webhook.Id, webhook.Token,
    new ExecuteWebhookRequest
    {
        Username = "News Flash",
        AvatarUrl = "https://example.com/news-icon.png",
        Embeds = new List<Embed>
        {
            new Embed
            {
                Title = "New Release v2.0",
                Description = "Check out the latest features!",
                Color = 0x00FF00,
                Timestamp = DateTimeOffset.UtcNow,
            }
        },
        Wait = true,
    });

Console.WriteLine($"Message sent: {msg?.Id}");

// Edit the webhook name
await client.Rest.ModifyWebhookWithTokenAsync(webhook.Id, webhook.Token, new()
{
    Name = "Updated Announcement Bot",
});
```

---

**More guides:** [Threads](./threads.md) | [REST API](../guides/sending-messages.md) | [Gateway](../guides/gateway.md)
