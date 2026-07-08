# Slash Commands

## Registering Slash Commands

Use the REST API to register global or guild-specific commands:

```csharp
var appId = client.CurrentUser.Id;

await client.Rest.CreateGlobalApplicationCommandAsync(
    appId,
    new CreateApplicationCommandRequest
    {
        Name = "ping",
        Description = "Responds with pong",
        Type = ApplicationCommandType.ChatInput,
    }
);
```

## Handling Slash Commands

```csharp
client.Interactions.RegisterCommand("ping", async interaction =>
{
    await client.Rest.CreateInteractionResponseAsync(
        interaction.Id,
        interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = "Pong!" },
        }
    );
});
```

## Command Options

```csharp
await client.Rest.CreateGlobalApplicationCommandAsync(
    appId,
    new CreateApplicationCommandRequest
    {
        Name = "greet",
        Description = "Greet a user",
        Options = new List<ApplicationCommandOption>
        {
            new()
            {
                Name = "user",
                Description = "The user to greet",
                Type = ApplicationCommandOptionType.User,
                Required = true,
            },
            new()
            {
                Name = "message",
                Description = "Custom message",
                Type = ApplicationCommandOptionType.String,
                Required = false,
            },
        },
    }
);
```

## Guild Commands

Register commands for a specific guild (instant update, no caching delay):

```csharp
await client.Rest.CreateGuildApplicationCommandAsync(
    appId, guildId,
    new CreateApplicationCommandRequest
    {
        Name = "modonly",
        Description = "A moderation command",
    }
);
```

## Permissions

Restrict commands to specific roles or users using Discord's permission system, or check permissions in the handler:

```csharp
client.Interactions.RegisterCommand("ban", async interaction =>
{
    // Check permissions in handler
    var permissions = await client.Rest.GetGuildMemberPermissionsAsync(guildId, interaction.Member.User.Id);
    if (!permissions.HasFlag(Permissions.BanMembers))
    {
        await RespondWithError(interaction, "You don't have permission to ban members");
        return;
    }
    // Execute ban...
});
```
