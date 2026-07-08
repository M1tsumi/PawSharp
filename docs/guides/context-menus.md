# Context Menus

Learn how to create and handle Discord context menu commands (right-click menus) using PawSharp.

## Table of Contents

1. [What Are Context Menu Commands?](#what-are-context-menu-commands)
2. [User Context Menus](#user-context-menus)
3. [Message Context Menus](#message-context-menus)
4. [Registering Commands with Discord](#registering-commands-with-discord)
5. [Handling Context Menu Interactions](#handling-context-menu-interactions)
6. [Attribute-Based Modules (Commands Extension)](#attribute-based-modules-commands-extension)
7. [Bulk Registration](#bulk-registration)
8. [Complete Walkthrough](#complete-walkthrough)

---

## What Are Context Menu Commands?

Context menu commands appear when a user right-clicks on a **user** or a **message** in Discord. They are a type of application command, similar to slash commands, but without argument options — the target (user or message) is implicit.

| Type | `ApplicationCommandType` | Trigger |
|------|--------------------------|---------|
| User | `User` (2) | Right-click on a user's name/avatar |
| Message | `Message` (3) | Right-click on a message |

---

## User Context Menus

User context menu commands operate on a **user** target. The interaction contains the target user's ID, username, and guild member data.

### Programmatic Registration

```csharp
handler.RegisterUserContextMenu("View Profile", async interaction =>
{
    var targetUserId = interaction.Data?.TargetId;
    var user = await client.Rest.GetUserAsync(targetUserId!.Value);

    if (user != null)
    {
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            $"**{user.Username}**\nID: {user.Id}\nCreated: {user.CreatedAt:yyyy-MM-dd}");
    }
});
```

### Accessing Target Data

Inside a user context menu handler, the target user is available via:

```csharp
interaction.Data.TargetId          // ulong — the target user's ID
interaction.Data.Resolved?.Users   // Dictionary<ulong, User>? — resolved users
interaction.Data.Resolved?.Members // Dictionary<ulong, GuildMember>? — resolved members
```

```csharp
handler.RegisterUserContextMenu("Member Info", async interaction =>
{
    var userId = interaction.Data!.TargetId!.Value;
    var guildId = interaction.GuildId!.Value;

    var member = interaction.Data.Resolved?.Members?.GetValueOrDefault(userId)
                 ?? await client.Rest.GetGuildMemberAsync(guildId, userId);

    if (member != null)
    {
        var roles = string.Join(", ", member.Roles);
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            $"**{member.User?.Username}**\nRoles: {roles}\nJoined: {member.JoinedAt:yyyy-MM-dd}");
    }
});
```

---

## Message Context Menus

Message context menu commands operate on a **message** target. The interaction contains the target message's ID, channel ID, content, and resolved data.

### Programmatic Registration

```csharp
handler.RegisterMessageContextMenu("Report Message", async interaction =>
{
    var messageId = interaction.Data?.TargetId!.Value;
    var channelId = interaction.ChannelId;

    var msg = await client.Rest.GetMessageAsync(channelId, messageId!.Value);

    if (msg != null)
    {
        // Log the report
        Console.WriteLine($"Reported by {interaction.Member?.User.Username}: {msg.Content}");

        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            "✅ Message reported to moderators.");
    }
});
```

### Accessing Target Message

```csharp
interaction.Data.TargetId                 // ulong — the target message's ID
interaction.Data.Resolved?.Messages       // Dictionary<ulong, Message>? — resolved messages
```

---

## Registering Commands with Discord

Before a context menu command works, you must **register it** with Discord via the REST API. This tells Discord the command's name and type.

### Global Command

```csharp
await client.Rest.CreateGlobalApplicationCommandAsync(applicationId, new()
{
    Name = "View Profile",
    Type = 2,  // ApplicationCommandType.User
});
```

### Guild Command (for testing)

```csharp
await client.Rest.CreateGuildApplicationCommandAsync(applicationId, guildId, new()
{
    Name = "View Profile",
    Type = 3,  // ApplicationCommandType.Message
});
```

| Command Type | `Type` Value |
|--------------|--------------|
| Chat Input (slash) | 1 |
| User Context Menu | 2 |
| Message Context Menu | 3 |
| Primary Entry Point | 4 |

⚠️ Global commands may take **up to 1 hour** to propagate. Guild commands update instantly — use them during development.

---

## Handling Context Menu Interactions

### Via `InteractionHandler`

The `InteractionHandler` automatically routes context menu interactions to the correct handler:

```csharp
var handler = new InteractionHandler(client.Rest);

// Register handlers
handler.RegisterUserContextMenu("View Profile", HandleUserProfile);
handler.RegisterMessageContextMenu("Report Message", HandleReport);

// Wire the gateway event
client.OnInteractionCreated(async interaction =>
{
    await handler.HandleInteractionAsync(interaction);
});

async Task HandleUserProfile(InteractionCreateEvent interaction)
{
    var userId = interaction.Data!.TargetId!.Value;
    var user = await client.Rest.GetUserAsync(userId);
    var embed = new EmbedBuilder()
        .WithTitle(user?.Username ?? "Unknown")
        .WithDescription($"ID: {userId}")
        .Build();

    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
        embeds: new List<Embed> { embed });
}

async Task HandleReport(InteractionCreateEvent interaction)
{
    var msg = interaction.Data?.Resolved?.Messages?.FirstOrDefault();
    // ... report logic ...

    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, "Reported!");
}
```

### Responding to Context Menu Interactions

```csharp
// Ephemeral response (only the user sees it)
await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, "Done!");

// Deferred ephemeral response (for slow operations)
await handler.RespondDeferredEphemeralAsync(interaction.Id, interaction.Token);
// ... do work ...
await handler.EditOriginalResponseAsync(applicationId, interaction.Token, new()
{
    Content = "Finished processing!",
});
```

---

## Attribute-Based Modules (Commands Extension)

For the attribute-based approach using `CommandsExtension`, decorate methods in a module:

### User Context Menu Attribute

```csharp
using PawSharp.Commands.Attributes;

public class ModerationModule : BaseCommandModule
{
    [UserContextMenu("View Profile")]
    public async Task ViewProfileAsync(InteractionCreateEvent interaction)
    {
        var userId = interaction.Data!.TargetId!.Value;
        // ...
        await interaction.RespondAsync(/* ... */);
    }
}
```

### Message Context Menu Attribute

```csharp
public class ModerationModule : BaseCommandModule
{
    [MessageContextMenu("Copy to Clipboard")]
    public async Task CopyMessageAsync(InteractionCreateEvent interaction)
    {
        var msg = interaction.Data?.Resolved?.Messages?.FirstOrDefault().Value;
        if (msg != null)
        {
            await interaction.RespondAsync($"```\n{msg.Content}\n```");
        }
    }
}
```

### Registering the Module

```csharp
var commands = client.UseCommands(new CommandsConfiguration());

await commands.RegisterContextMenuModuleAsync(
    client,
    new ModerationModule(),
    applicationId,
    guildId: null);  // null = global, or provide guildId for guild-scoped
```

This method:
1. Scans the module for `[UserContextMenu]` and `[MessageContextMenu]` attributes
2. Creates the application command via REST
3. Wires the local interaction handler

---

## Bulk Registration

Register multiple context menu modules in a single API call:

```csharp
var modules = new BaseCommandModule[]
{
    new ModerationModule(),
    new UtilityModule(),
};

await commands.BulkRegisterContextMenuModulesAsync(
    client,
    modules,
    applicationId,
    guildId: testGuildId);
```

This uses `BulkOverwriteGuildApplicationCommandsAsync` / `BulkOverwriteGlobalApplicationCommandsAsync` — all existing commands are replaced with the provided set.

⚠️ **Bulk overwrite replaces ALL commands** for the scope (guild or global). Make sure to include all slash commands in the bulk registration or register them separately.

---

## Interaction Lifecycle

```
User right-clicks → Selects command → Discord sends INTERACTION_CREATE
                                            │
                                            ▼
                              client.OnInteractionCreated fires
                                            │
                                            ▼
                            InteractionHandler.HandleInteractionAsync
                                            │
                                   ┌────────┴────────┐
                                   ▼                  ▼
                        User (Type=2)           Message (Type=3)
                                   │                  │
                                   ▼                  ▼
                     _userContextMenuHandlers   _messageContextMenuHandlers
                                   │                  │
                                   ▼                  ▼
                         Your handler runs     Your handler runs
                                   │                  │
                                   ▼                  ▼
                     Respond via interaction token  (ephemeral or not)
```

---

## Complete Walkthrough

```csharp
using PawSharp.Client;
using PawSharp.Interactions;
using PawSharp.API.Models;
using PawSharp.Commands.Attributes;
using PawSharp.Core.Entities;

// ─── Programmatic Approach ─────────────────────────────────────────────

var client = new PawSharpClientBuilder()
    .WithToken("Bot YOUR_TOKEN")
    .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
    .Build();

const ulong applicationId = 123456789;
const ulong testGuildId = 987654321;

var handler = new InteractionHandler(client.Rest);

// Register the commands with Discord (guild-scoped for instant updates)
await client.Rest.CreateGuildApplicationCommandAsync(applicationId, testGuildId, new()
{
    Name = "User Info",
    Type = 2, // USER
});

await client.Rest.CreateGuildApplicationCommandAsync(applicationId, testGuildId, new()
{
    Name = "Get Message Link",
    Type = 3, // MESSAGE
});

// Wire handlers
handler.RegisterUserContextMenu("User Info", async interaction =>
{
    var userId = interaction.Data!.TargetId!.Value;
    var guildId = interaction.GuildId!.Value;
    var member = await client.Rest.GetGuildMemberAsync(guildId, userId);

    if (member != null)
    {
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            $"**{member.User?.Username}** | Joined: {member.JoinedAt:d}\n" +
            $"Roles: {member.Roles.Count}");
    }
});

handler.RegisterMessageContextMenu("Get Message Link", async interaction =>
{
    var msgId = interaction.Data!.TargetId!.Value;
    var channelId = interaction.ChannelId;
    var guildId = interaction.GuildId!.Value;

    var link = $"https://discord.com/channels/{guildId}/{channelId}/{msgId}";
    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, link);
});

// Wire gateway
client.OnInteractionCreated(async interaction =>
{
    await handler.HandleInteractionAsync(interaction);
});

// ─── Attribute-Based Alternative ───────────────────────────────────────
// (See "Attribute-Based Modules" section above for the module class)

// var commands = client.UseCommands(new CommandsConfiguration());
// await commands.RegisterContextMenuModuleAsync(
//     client, new ModerationModule(), applicationId, testGuildId);

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

**More guides:** [Slash Commands](./slash-commands.md) | [Interactions](./interactions.md) | [Gateway](../guides/gateway.md)
