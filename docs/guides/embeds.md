# Embeds

Embeds are rich, structured content blocks that make your bot's messages stand out. They support formatted text, images, fields, footers, and more.

> **Prerequisites:** [Messages](../guides/sending-messages.md#messages)

---

## What Are Discord Embeds?

An embed is a richly formatted card attached to a message. Discord renders embeds with a colored sidebar, structured fields, and optional media.

```
┌─────────────────────────────────────┐
│ ┌────┐ │
│ │icon│ Author Name (hyperlink) │
│ └────┘ │
│ ┌──────────────────────────────────┐│
│ │ Title - hyperlink to URL ││
│ │━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━││
│ │ Description text with ││
│ │ **Markdown** support ││
│ │━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━││
│ │ Field 1 (inline) Field 2 (inln) ││
│ │ Value 1 Value 2 ││
│ │━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━││
│ │ Footer text ── Timestamp ││
│ └──────────────────────────────────┘│
└─────────────────────────────────────┘
```

---

## EmbedBuilder

`EmbedBuilder` provides a fluent API for constructing `Embed` objects:

```csharp
using PawSharp.Core.Builders;

var embed = new EmbedBuilder()
 .WithTitle("PawSharp Bot")
 .WithDescription("Welcome to the server!")
 .WithUrl("https://pawsharp.dev")
 .WithColor(0x5865F2)
 .WithAuthor("PawSharp Team", iconUrl: "https://pawsharp.dev/icon.png")
 .WithThumbnail("https://pawsharp.dev/logo.png")
 .WithImage("https://pawsharp.dev/banner.png")
 .WithFooter("v2.0.0", iconUrl: "https://pawsharp.dev/favicon.ico")
 .WithCurrentTimestamp()
 .AddField("Library", "PawSharp", inline: true)
 .AddField("Language", "C#", inline: true)
 .AddField("Description", "A modern Discord API wrapper for .NET", inline: false)
 .Build();

// Send with an interaction response
await handler.RespondWithEmbedsAsync(interaction.Id, interaction.Token,
 "Here's your info:", new List<Embed> { embed });

// Or in a regular message
await rest.CreateMessageAsync(channelId, new CreateMessageRequest
{
 Content = "Check this out:",
 Embeds = new List<Embed> { embed }
});
```

### All Builder Methods

| Method | Description | Max |
|---|---|---|
| `WithTitle(string)` | Bold heading at the top | 256 chars |
| `WithDescription(string)` | Body text (Markdown supported) | 4096 chars |
| `WithUrl(string)` | Makes the title a hyperlink | - |
| `WithColor(int)` | 24-bit RGB sidebar color (e.g. `0x5865F2`) | - |
| `WithColor(byte r, byte g, byte b)` | Color from components | - |
| `WithColor(uint)` | Color from hex literal | - |
| `WithTimestamp(DateTimeOffset?)` | Footer timestamp | - |
| `WithCurrentTimestamp()` | Sets to UTC now | - |
| `WithFooter(string, iconUrl?)` | Small text at the bottom | 2048 chars |
| `WithImage(string)` | Large image in the body | - |
| `WithThumbnail(string)` | Small image top-right | - |
| `WithAuthor(string, url?, iconUrl?)` | Author block at the top | 256 chars |
| `AddField(name, value, inline)` | Named field | 256/1024 chars |
| `AddInlineField(name, value)` | Shortcut for inline field | 256/1024 chars |

### Color Presets

```csharp
new EmbedBuilder()
 .WithBlurpleColor() // 0x5865F2
 .WithGreenColor() // 0x57F287
 .WithYellowColor() // 0xFEE75C
 .WithRedColor() // 0xED4245
 .WithWhiteColor() // 0xFFFFFF
 .WithBlackColor() // 0x000000
```

---

## Embed Limits

Discord enforces strict size limits. `EmbedBuilder` validates all of them at build time:

| Limit | Value |
|---|---|
| Title | 256 characters |
| Description | 4096 characters |
| Fields | 25 max |
| Field name | 256 characters |
| Field value | 1024 characters |
| Footer text | 2048 characters |
| Author name | 256 characters |
| **Total** (title + desc + fields + footer + author) | **6000 characters** |
| Embeds per message | 10 |

Exceeding any limit throws `ArgumentException` or `InvalidOperationException`.

---

## Rich Embeds with Multiple Fields

```csharp
var serverInfo = new EmbedBuilder()
 .WithTitle("Server Information")
 .WithDescription("Stats for **PawSharp Community**")
 .WithBlurpleColor()
 .WithThumbnail(guild.IconUrl)
 .AddField("Owner", guild.Owner?.Mention ?? "Unknown", inline: true)
 .AddField("Members", guild.MemberCount.ToString(), inline: true)
 .AddField("Channels", guild.Channels?.Count.ToString() ?? "0", inline: true)
 .AddField("Roles", guild.Roles?.Count.ToString() ?? "0", inline: true)
 .AddField("Boosts", guild.PremiumSubscriptionCount?.ToString() ?? "0", inline: true)
 .AddField("Created", guild.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown", inline: true)
 .WithFooter($"ID: {guild.Id}")
 .WithCurrentTimestamp()
 .Build();
```

### Inline vs Non-Inline Fields

- **Inline fields** display side-by-side (up to 3 per row depending on content width).
- **Non-inline fields** each take a full row.

```csharp
// These three will be on the same row (if short enough)
embed.AddField("Name", "Alice", inline: true);
embed.AddField("Age", "30", inline: true);
embed.AddField("Role", "Admin", inline: true);

// This takes its own row
embed.AddField("Biography", "Alice is a long-time community member...", inline: false);
```

---

## Complete Examples

### Command: `/userinfo`

```csharp
handler.RegisterCommand("userinfo", async interaction =>
{
 var userId = interaction.Data?.Resolved?.Users?.Keys.FirstOrDefault()
 ?? interaction.Member?.User.Id ?? interaction.User?.Id;

 var user = await rest.GetUserAsync(userId!.Value);

 var embed = new EmbedBuilder()
 .WithTitle($"{user.Username}")
 .WithDescription($"Discord user information")
 .WithThumbnail(user.GetAvatarUrl())
 .AddField("User ID", user.Id.ToString(), inline: true)
 .AddField("Bot", user.IsBot ? "Yes" : "No", inline: true)
 .AddField("Created", user.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown", inline: true)
 .AddField("Flags", string.Join(", ", user.Flags?.ToString() ?? "None") ?? "None", inline: false)
 .WithBlurpleColor()
 .WithFooter($"Requested by {interaction.User?.Username ?? interaction.Member?.User.Username}")
 .WithCurrentTimestamp()
 .Build();

 await handler.RespondWithEmbedsAsync(interaction.Id, interaction.Token,
 null, new List<Embed> { embed }, ephemeral: true);
});
```

### Command: `/serverinfo`

```csharp
handler.RegisterCommand("serverinfo", async interaction =>
{
 var guildId = interaction.GuildId;
 if (guildId == null)
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 "This command can only be used in a server.");
 return;
 }

 var guild = await rest.GetGuildAsync(guildId.Value);

 var embed = new EmbedBuilder()
 .WithTitle(guild.Name)
 .WithThumbnail(guild.IconUrl)
 .AddField("Owner", $"<@{guild.OwnerId}>", inline: true)
 .AddField("Members", guild.ApproximateMemberCount?.ToString() ?? "?", inline: true)
 .AddField("Boosts", guild.PremiumSubscriptionCount?.ToString() ?? "0", inline: true)
 .AddField("Channels", guild.ApproximatePresenceCount?.ToString() ?? "?", inline: true)
 .AddField("Roles", guild.Roles?.Count.ToString() ?? "0", inline: true)
 .AddField("Created", guild.CreatedAt?.ToString("yyyy-MM-dd") ?? "Unknown", inline: true)
 .WithBlurpleColor()
 .WithFooter($"ID: {guild.Id}")
 .WithCurrentTimestamp()
 .Build();

 await handler.RespondWithEmbedsAsync(interaction.Id, interaction.Token,
 null, new List<Embed> { embed });
});
```

### Error Embeds

```csharp
var errorEmbed = new EmbedBuilder()
 .WithTitle("Error")
 .WithDescription(ex.Message)
 .WithRedColor()
 .WithFooter("If this persists, contact support.")
 .WithCurrentTimestamp()
 .Build();
```

### Success Embeds

```csharp
var successEmbed = new EmbedBuilder()
 .WithTitle("Success")
 .WithDescription("Your changes have been saved.")
 .WithGreenColor()
 .WithCurrentTimestamp()
 .Build();
```

---

## Performance Notes

- Embeds add to message payload size. Keep descriptions concise.
- 10 embeds per message maximum; stay at 1 - 3 for readability.
- Image URLs should be static (CDN) - avoid redirect chains.
- The 6000-character total limit adds up quickly with rich fields. Use `Build()` to validate during development.

---

## Related Guides

- [Attachments](./attachments.md) - Sending files alongside embeds
- [Components](./components.md) - Adding buttons and select menus to embed messages
- [Messages](../guides/sending-messages.md#messages) - Full message creation reference
