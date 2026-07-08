# Auto Moderation

Learn how to configure and handle Discord's Auto Moderation system using PawSharp.

## Table of Contents

1. [What Is Discord Auto Moderation?](#what-is-discord-auto-moderation)
2. [Rule Structure](#rule-structure)
3. [Trigger Types](#trigger-types)
4. [Actions](#actions)
5. [Creating Rules](#creating-rules)
6. [Retrieving & Modifying Rules](#retrieving--modifying-rules)
7. [Exemptions](#exemptions)
8. [Handling Auto Moderation Events via Gateway](#handling-auto-moderation-events-via-gateway)
9. [Complete Example](#complete-example)

---

## What Is Discord Auto Moderation?

Auto Moderation (AutoMod) is a server-level feature that lets guild administrators define **rules** to automatically detect and act on unwanted content. Rules can:

- Block messages containing keywords
- Flag spam or mention raids
- Alert moderators via a log channel
- Time out offending users
- Use pre-defined word lists (profanity, slurs, sexual content)

PawSharp exposes the full Auto Moderation API through REST endpoints and Gateway events.

---

## Rule Structure

```csharp
public class AutoModerationRule : DiscordEntity
{
    public ulong GuildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ulong CreatorId { get; set; }
    public AutoModerationEventType EventType { get; set; }
    public AutoModerationTriggerType TriggerType { get; set; }
    public AutoModerationTriggerMetadata TriggerMetadata { get; set; } = null!;
    public List<AutoModerationAction> Actions { get; set; } = new();
    public bool Enabled { get; set; }
    public List<ulong> ExemptRoles { get; set; } = new();       // max 20
    public List<ulong> ExemptChannels { get; set; } = new();     // max 50
}
```

### Event Type

```csharp
public enum AutoModerationEventType
{
    MessageSend = 1  // triggers when a member sends or edits a message
}
```

⚠️ Currently Discord only supports `MessageSend` as the event type. This may expand in the future.

---

## Trigger Types

| Trigger | Value | Description |
|---------|-------|-------------|
| `Keyword` | 1 | Match against a custom keyword list (max 1000 entries) |
| `Spam` | 3 | Detect generic spam patterns (no metadata needed) |
| `KeywordPreset` | 4 | Use pre-defined word sets (profanity, sexual content, slurs) |
| `MentionSpam` | 5 | Limit the number of unique mentions per message (max 50) |

### Trigger Metadata

```csharp
public class AutoModerationTriggerMetadata
{
    public List<string>? KeywordFilter { get; set; }      // max 1000 keywords
    public List<string>? RegexPatterns { get; set; }       // max 10 regex patterns
    public List<AutoModerationKeywordPresetType>? Presets { get; set; }
    public List<string>? AllowList { get; set; }           // max 100 (or 1000) allow words
    public int? MentionTotalLimit { get; set; }            // max 50 mentions
    public bool? MentionRaidProtectionEnabled { get; set; }
}
```

### Keyword Preset Types

```csharp
public enum AutoModerationKeywordPresetType
{
    Profanity = 1,     // swearing and cursing
    SexualContent = 2, // sexually explicit behavior
    Slurs = 3          // hate speech and personal insults
}
```

---

## Actions

Each rule can have **up to 3 actions** that execute in order when a trigger fires:

| Action | Value | Description |
|--------|-------|-------------|
| `BlockMessage` | 1 | Prevent the message from being sent |
| `SendAlertMessage` | 2 | Log the content to a channel |
| `Timeout` | 3 | Time out the user (max 28 days = 2,419,200 seconds) |

### Action Metadata

```csharp
public class AutoModerationActionMetadata
{
    public ulong? ChannelId { get; set; }        // alert destination
    public int? DurationSeconds { get; set; }    // timeout duration
    public string? CustomMessage { get; set; }   // shown to user on block
}
```

✅ **Recommended:** Always include a `BlockMessage` + `SendAlertMessage` pair so users know their message was blocked and moderators can review.

```csharp
var blockAction = new AutoModerationAction
{
    Type = AutoModerationActionType.BlockMessage,
    Metadata = new AutoModerationActionMetadata
    {
        CustomMessage = "Your message was blocked by server rules."
    }
};

var alertAction = new AutoModerationAction
{
    Type = AutoModerationActionType.SendAlertMessage,
    Metadata = new AutoModerationActionMetadata
    {
        ChannelId = logChannelId
    }
};
```

---

## Creating Rules

Requires `MANAGE_GUILD` permission.

### Keyword Rule

```csharp
var rule = await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
{
    Name = "Block Bad Words",
    EventType = AutoModerationEventType.MessageSend,
    TriggerType = AutoModerationTriggerType.Keyword,
    TriggerMetadata = new AutoModerationTriggerMetadata
    {
        KeywordFilter = new List<string>
        {
            "badword1", "badword2", "spam_link.*"
        },
        RegexPatterns = new List<string>
        {
            @"(discord\.gg|dsc\.gg)\/\S+"
        },
        AllowList = new List<string>
        {
            "badword1_is_actually_ok"
        }
    },
    Actions = new List<AutoModerationAction>
    {
        new() { Type = AutoModerationActionType.BlockMessage },
        new() { Type = AutoModerationActionType.SendAlertMessage,
                Metadata = new() { ChannelId = modLogChannelId } }
    },
    Enabled = true,
});
```

### Spam Rule

```csharp
var rule = await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
{
    Name = "Anti-Spam",
    EventType = AutoModerationEventType.MessageSend,
    TriggerType = AutoModerationTriggerType.Spam,
    Actions = new List<AutoModerationAction>
    {
        new() { Type = AutoModerationActionType.BlockMessage },
        new() { Type = AutoModerationActionType.Timeout,
                Metadata = new() { DurationSeconds = 600 } } // 10 min
    },
    Enabled = true,
});
```

⚠️ Spam trigger type requires no `TriggerMetadata`. Discord's ML model handles detection.

### Keyword Preset Rule

```csharp
var rule = await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
{
    Name = "Block Hate Speech",
    EventType = AutoModerationEventType.MessageSend,
    TriggerType = AutoModerationTriggerType.KeywordPreset,
    TriggerMetadata = new AutoModerationTriggerMetadata
    {
        Presets = new List<AutoModerationKeywordPresetType>
        {
            AutoModerationKeywordPresetType.Profanity,
            AutoModerationKeywordPresetType.Slurs
        }
    },
    Actions = new List<AutoModerationAction>
    {
        new() { Type = AutoModerationActionType.BlockMessage },
    },
    Enabled = true,
});
```

### Mention Spam Rule

```csharp
var rule = await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
{
    Name = "Mention Limit",
    EventType = AutoModerationEventType.MessageSend,
    TriggerType = AutoModerationTriggerType.MentionSpam,
    TriggerMetadata = new AutoModerationTriggerMetadata
    {
        MentionTotalLimit = 10,
        MentionRaidProtectionEnabled = true,
    },
    Actions = new List<AutoModerationAction>
    {
        new() { Type = AutoModerationActionType.BlockMessage },
        new() { Type = AutoModerationActionType.Timeout,
                Metadata = new() { DurationSeconds = 3600 } }
    },
    Enabled = true,
});
```

---

## Retrieving & Modifying Rules

### List All Rules

```csharp
var rules = await client.Rest.ListAutoModerationRulesAsync(guildId);
```

### Get a Specific Rule

```csharp
var rule = await client.Rest.GetAutoModerationRuleAsync(guildId, ruleId);
```

### Modify a Rule

```csharp
var updated = await client.Rest.ModifyAutoModerationRuleAsync(guildId, ruleId, new()
{
    Name = "Updated Rule Name",
    Enabled = false,                 // disable temporarily
});
```

### Delete a Rule

```csharp
bool deleted = await client.Rest.DeleteAutoModerationRuleAsync(guildId, ruleId);
```

---

## Exemptions

Both `CreateAutoModerationRuleRequest` and `ModifyAutoModerationRuleRequest` support exemptions:

| Property | Max | Description |
|----------|-----|-------------|
| `ExemptRoles` | 20 | Roles that bypass the rule |
| `ExemptChannels` | 50 | Channels where the rule doesn't apply |

```csharp
new CreateAutoModerationRuleRequest
{
    Name = "Strict for @everyone, lenient for mods",
    // ...
    ExemptRoles = new List<ulong> { moderatorRoleId, adminRoleId },
    ExemptChannels = new List<ulong> { staffChannelId },
};
```

---

## Handling Auto Moderation Events via Gateway

When a rule triggers, Discord sends an `AUTO_MODERATION_ACTION_EXECUTION` event. Subscribe using the low-level dispatcher:

```csharp
client.Gateway.Events.On<AutoModerationActionExecutionEvent>(
    "AUTO_MODERATION_ACTION_EXECUTION", async action =>
{
    Console.WriteLine($"[AutoMod] Rule {action.RuleId} triggered in {action.GuildId}");
    Console.WriteLine($"  Action: {action.Action.Type}");
    Console.WriteLine($"  Content: {action.Content}");
    Console.WriteLine($"  User: {action.UserId}");
    Console.WriteLine($"  Channel: {action.ChannelId}");

    if (action.Action.Type == AutoModerationActionType.BlockMessage)
    {
        // Optionally DM the user explaining why
        await NotifyUserAsync(action.UserId, action.RuleTriggerReason);
    }
});
```

💡 The auto moderation event is **not** sent via a convenience method on `DiscordClient`. You must use `client.Gateway.Events.On<T>("EVENT_NAME", handler)`.

---

## Complete Example

```csharp
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.API.Models;

var client = new PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN")
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .Build();

const ulong guildId = 123456789;
const ulong modLogChannelId = 987654321;

client.OnReady(async _ =>
{
    Console.WriteLine("Creating auto-moderation rules...");

    // Keyword rule
    await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
    {
        Name = "Block Links",
        EventType = AutoModerationEventType.MessageSend,
        TriggerType = AutoModerationTriggerType.Keyword,
        TriggerMetadata = new AutoModerationTriggerMetadata
        {
            KeywordFilter = new List<string>
            {
                "discord.gg/", "dsc.gg/", "invite.gg/"
            },
        },
        Actions = new List<AutoModerationAction>
        {
            new() { Type = AutoModerationActionType.BlockMessage,
                    Metadata = new() { CustomMessage = "No invite links!" } },
            new() { Type = AutoModerationActionType.SendAlertMessage,
                    Metadata = new() { ChannelId = modLogChannelId } },
        },
        Enabled = true,
    });

    // Mention spam rule
    await client.Rest.CreateAutoModerationRuleAsync(guildId, new()
    {
        Name = "Mention Protection",
        EventType = AutoModerationEventType.MessageSend,
        TriggerType = AutoModerationTriggerType.MentionSpam,
        TriggerMetadata = new AutoModerationTriggerMetadata
        {
            MentionTotalLimit = 5,
            MentionRaidProtectionEnabled = true,
        },
        Actions = new List<AutoModerationAction>
        {
            new() { Type = AutoModerationActionType.BlockMessage },
            new() { Type = AutoModerationActionType.Timeout,
                    Metadata = new() { DurationSeconds = 300 } }, // 5 min
        },
        Enabled = true,
    });
});

// Listen for auto-mod actions
client.Gateway.Events.On<AutoModerationActionExecutionEvent>(
    "AUTO_MODERATION_ACTION_EXECUTION", async action =>
{
    Console.WriteLine($"[AutoMod] Rule {action.RuleId} triggered on user {action.UserId}");

    if (action.Action.Type == AutoModerationActionType.BlockMessage)
    {
        Console.WriteLine($"  Blocked content: {action.Content}");
    }
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

**More guides:** [Gateway Events](../guides/gateway.md) | [REST API](../guides/sending-messages.md) | [Scheduled Events](./scheduled-events.md)
