# Webhooks

## Creating Webhooks

```csharp
var webhook = await client.Rest.CreateWebhookAsync(channelId, new CreateWebhookRequest
{
    Name = "My Webhook",
    Avatar = "base64_encoded_image", // Optional
});
```

## Getting Webhooks

```csharp
// Get all webhooks for a channel
var webhooks = await client.Rest.GetChannelWebhooksAsync(channelId);

// Get all webhooks for a guild
var guildWebhooks = await client.Rest.GetGuildWebhooksAsync(guildId);

// Get a specific webhook
var webhook = await client.Rest.GetWebhookAsync(webhookId);

// Get webhook by token
var webhook = await client.Rest.GetWebhookWithTokenAsync(webhookId, webhookToken);
```

## Executing Webhooks

```csharp
await client.Rest.ExecuteWebhookAsync(
    webhookId,
    webhookToken,
    new ExecuteWebhookRequest
    {
        Content = "Hello from webhook!",
        Username = "Custom Name",     // Override username
        AvatarUrl = "https://...",    // Override avatar
        Embeds = new List<Embed> { embed },
    }
);
```

## Editing and Deleting

```csharp
// Edit webhook
await client.Rest.ModifyWebhookAsync(webhookId, new ModifyWebhookRequest
{
    Name = "Updated Name",
});

// Edit webhook message
await client.Rest.EditWebhookMessageAsync(webhookId, webhookToken, messageId, new()
{
    Content = "Updated content",
});

// Delete webhook
await client.Rest.DeleteWebhookAsync(webhookId);
```

## Slack & GitHub Compatible Webhooks

Discord supports webhooks compatible with Slack and GitHub formats:

```csharp
// Execute Slack-compatible webhook
await client.Rest.ExecuteSlackWebhookAsync(webhookId, webhookToken, slackJson);

// Execute GitHub-compatible webhook
await client.Rest.ExecuteGitHubWebhookAsync(webhookId, webhookToken, githubJson);
```
