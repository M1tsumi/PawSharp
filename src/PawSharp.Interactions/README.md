# PawSharp.Interactions

PawSharp.Interactions brings Discord's modern interaction model into your bot workflow.

Use it for slash commands, button/select interactions, and modal submissions with a clean structure that stays maintainable as your command surface grows.

## Why Use This Package

- Slash command and component interaction handling
- Support for modals and follow-up responses
- Strongly typed interaction data
- Clean extension workflow with PawSharp.Client
- Webhook signature verification for HTTP interactions
- Full support for Components v2 (Labels, RadioGroups, CheckboxGroups, etc.)

## Requirements

- .NET 10 (`net10.0`)
- `PawSharp.Client`
- `PawSharp.API`
- `PawSharp.Gateway`
- `PawSharp.Core`

## Installation

```bash
dotnet add package PawSharp.Interactions --version 1.1.0-alpha.3
```

## Quick Start

```csharp
using PawSharp.Interactions;

var interactions = client.UseInteractions();

interactions.OnInteractionCreate += async interaction =>
{
    if (interaction.Type == InteractionType.ApplicationCommand)
    {
        await interaction.RespondAsync("Interaction received.");
    }
};
```

## Features

### Interaction Types

- **ApplicationCommand**: Slash commands, user context menus, message context menus
- **MessageComponent**: Buttons, select menus (string, user, role, mentionable, channel)
- **ModalSubmit**: Modal form submissions
- **ApplicationCommandAutocomplete**: Autocomplete suggestions for slash commands

### Response Types

- `Pong`: Respond to ping interactions
- `ChannelMessageWithSource`: Send a message response
- `DeferredChannelMessageWithSource`: Defer with loading state
- `UpdateMessage`: Update the component's message
- `DeferredUpdateMessage`: Defer component update without loading state
- `ApplicationCommandAutocompleteResult`: Send autocomplete choices
- `Modal`: Show a modal dialog
- `PremiumRequired`: Show premium upgrade button (deprecated)
- `LaunchActivity`: Launch a Discord Activity

### Component Builders

The package includes fluent builders for all Discord component types:

#### Basic Components
- `ButtonBuilder`: Create buttons with styles, emojis, and URLs
- `SelectMenuBuilder`: Create string select menus with options
- `UserSelectMenuBuilder`: Select from guild users
- `RoleSelectMenuBuilder`: Select from guild roles
- `MentionableSelectMenuBuilder`: Select from users and roles
- `ChannelSelectMenuBuilder`: Select from channels with type filtering
- `ActionRowBuilder`: Container for up to 5 components
- `ModalBuilder`: Create modal dialogs with text inputs

#### Components v2
- `LabelBuilder`: Display text with optional emoji
- `TextDisplayBuilder`: Render markdown text
- `ThumbnailBuilder`: Display images as accessories
- `MediaGalleryBuilder`: Display collections of media
- `FileBuilder`: Render file attachments
- `SeparatorBuilder`: Add visual dividers
- `ContainerBuilder`: Top-level container with accent colors
- `SectionBuilder`: Group text displays with accessories
- `FileUploadBuilder`: Allow file uploads in modals
- `RadioGroupBuilder`: Single-select option groups
- `CheckboxGroupBuilder`: Multi-select option groups
- `CheckboxBuilder`: Single toggleable checkbox

## Usage Examples

### Slash Command Handler

```csharp
var handler = new InteractionHandler(restClient, logger);

handler.RegisterCommand("ping", async interaction =>
{
    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, "Pong!");
});

await handler.HandleInteractionAsync(interactionEvent);
```

### Button Click Handler

```csharp
handler.RegisterComponent("confirm_button", async interaction =>
{
    await handler.RespondAsync(interaction.Id, interaction.Token, new InteractionResponse
    {
        Type = (int)InteractionResponseType.ChannelMessageWithSource,
        Data = new InteractionCallbackData
        {
            Content = "Button clicked!",
            Flags = 64 // Ephemeral
        }
    });
});
```

### Modal Submission Handler

```csharp
handler.RegisterModal("feedback_modal", async interaction =>
{
    var feedback = interaction.GetModalValue("feedback_input");
    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, 
        $"Thanks for your feedback: {feedback}");
});
```

### Creating a Modal

```csharp
var modal = new ModalBuilder()
    .WithCustomId("feedback_modal")
    .WithTitle("Feedback")
    .AddTextInput("Your Feedback", "feedback_input", 
        TextInputStyle.Paragraph, placeholder: "Share your thoughts...")
    .BuildResponse();

await handler.RespondAsync(interaction.Id, interaction.Token, modal);
```

### Building Buttons with ActionRow

```csharp
var response = new InteractionResponseBuilder()
    .WithContent("Choose an option:")
    .AddActionRow(row =>
    {
        row.AddButton(new ButtonBuilder("accept", "Accept", ButtonStyle.Success));
        row.AddButton(new ButtonBuilder("decline", "Decline", ButtonStyle.Danger));
    })
    .AsEphemeral()
    .Build();

await handler.RespondAsync(interaction.Id, interaction.Token, response);
```

### Using Extension Methods

```csharp
// Get option values from slash commands
var userId = interaction.GetOptionValue<ulong>("user");
var reason = interaction.GetOptionValue<string>("reason");

// Get subcommand name
var subcommand = interaction.GetSubcommandName();

// Check interaction context
if (interaction.IsGuildInteraction())
{
    // Handle guild interaction
}

// Get modal values
var feedback = interaction.GetModalValue("feedback_input");
var allValues = interaction.GetModalValues();
```

### Follow-up Messages

```csharp
// Send a follow-up message
var followup = await handler.CreateFollowupAsync(applicationId, interactionToken, 
    new CreateMessageRequest { Content = "Additional info" });

// Edit the follow-up
await handler.EditFollowupAsync(applicationId, interactionToken, followup.Id,
    new EditMessageRequest { Content = "Updated info" });

// Delete the follow-up
await handler.DeleteFollowupAsync(applicationId, interactionToken, followup.Id);
```

### Webhook Verification (HTTP Interactions)

```csharp
var verifier = new WebhookVerifier("your_public_key_hex");

// In your HTTP endpoint
var signature = Request.Headers["X-Signature-Ed25519"];
var timestamp = Request.Headers["X-Signature-Timestamp"];
var body = await new StreamReader(Request.Body).ReadToEndAsync();

if (!verifier.Verify(signature, timestamp, body))
{
    return StatusCode(401);
}

// Process the interaction
```

### Autocomplete Handler

```csharp
handler.RegisterAutocomplete("search", async interaction =>
{
    var query = interaction.GetOptionValue<string>("query");
    var choices = new List<AutocompleteChoice>
    {
        new AutocompleteChoice { Name = "Option 1", Value = "opt1" },
        new AutocompleteChoice { Name = "Option 2", Value = "opt2" }
    };
    return choices;
});
```

### Context Menu Handlers

```csharp
// User context menu (right-click on user)
handler.RegisterUserContextMenu("Ban User", async interaction =>
{
    var targetId = interaction.Data?.TargetId;
    // Handle ban logic
});

// Message context menu (right-click on message)
handler.RegisterMessageContextMenu("Pin Message", async interaction =>
{
    var targetId = interaction.Data?.TargetId;
    // Handle pin logic
});
```

## Dependency Injection

```csharp
// In ConfigureServices
services.AddInteractionHandler();

// Inject into your service
public class MyService
{
    private readonly InteractionHandler _handler;

    public MyService(InteractionHandler handler)
    {
        _handler = handler;
    }
}
```

## Typical Use Cases

- Slash-first bot command experiences
- Rich UI flows with buttons, menus, and modals
- Hybrid bots using both commands and interactions
- HTTP-based interaction endpoints with webhook verification
- Context menu integrations for users and messages

## Related Packages

- `PawSharp.Client`: recommended host for interaction handlers
- `PawSharp.Commands`: prefix command workflows
- `PawSharp.Interactivity`: user-response waiters and paginated UX
- `PawSharp.API`: REST API client for Discord
- `PawSharp.Gateway`: WebSocket gateway client
- `PawSharp.Core`: Shared entities and enums

## Documentation

- Main repository guide: [../../README.md](../../README.md)
- Package source: [./](./)

## Support

- Join the [PawSharp Discord](https://discord.gg/6Z8X8cCHXs) for help, discussion, and community.
- Report bugs or request features via [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues).
- Start a discussion on [GitHub Discussions](https://github.com/M1tsumi/PawSharp/discussions).

## License

MIT. See [../../LICENSE](../../LICENSE).
