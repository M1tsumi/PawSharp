# Components

Message Components (buttons, select menus, modals) allow rich interactive experiences.

## Buttons

Build and send a message with buttons:

```csharp
var components = new List<Component>
{
    new()
    {
        Type = ComponentType.ActionRow,
        Components = new List<Component>
        {
            new()
            {
                Type = ComponentType.Button,
                Style = ButtonStyle.Primary,
                Label = "Click Me!",
                CustomId = "my_button",
            },
            new()
            {
                Type = ComponentType.Button,
                Style = ButtonStyle.Danger,
                Label = "Delete",
                CustomId = "delete_button",
            },
        },
    },
};

await client.Rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
    Content = "Choose an action:",
    Components = components,
});
```

Handle button clicks:

```csharp
client.Interactions.RegisterComponent("my_button", async interaction =>
{
    await client.Rest.CreateInteractionResponseAsync(
        interaction.Id, interaction.Token,
        new InteractionResponse
        {
            Type = (int)InteractionResponseType.ChannelMessageWithSource,
            Data = new InteractionCallbackData { Content = "Button clicked!" },
        }
    );
});
```

## Select Menus

```csharp
var components = new List<Component>
{
    new()
    {
        Type = ComponentType.ActionRow,
        Components = new List<Component>
        {
            new()
            {
                Type = ComponentType.StringSelect,
                CustomId = "color_select",
                Placeholder = "Choose a color",
                Options = new List<SelectOption>
                {
                    new() { Label = "Red", Value = "red", Description = "The color red" },
                    new() { Label = "Green", Value = "green" },
                    new() { Label = "Blue", Value = "blue" },
                },
            },
        },
    },
};
```

## Modals

Send a modal (must be triggered by an interaction):

```csharp
await client.Rest.CreateInteractionResponseAsync(
    interaction.Id, interaction.Token,
    new InteractionResponse
    {
        Type = (int)InteractionResponseType.Modal,
        Data = new InteractionCallbackData
        {
            CustomId = "feedback_modal",
            Title = "Send Feedback",
            Components = new List<Component>
            {
                new()
                {
                    Type = ComponentType.ActionRow,
                    Components = new List<Component>
                    {
                        new()
                        {
                            Type = ComponentType.TextInput,
                            CustomId = "feedback_text",
                            Style = TextInputStyle.Paragraph,
                            Label = "Your feedback",
                            Required = true,
                        },
                    },
                },
            },
        },
    }
);
```

Handle modal submissions:

```csharp
client.Interactions.RegisterModal("feedback_modal", async interaction =>
{
    var feedback = interaction.Data?.Components?.FirstOrDefault()?.Components?.FirstOrDefault()?.Value;
    Console.WriteLine($"Feedback received: {feedback}");
});
```
