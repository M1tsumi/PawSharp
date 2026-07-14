# Attachments

Attachments let you send files - images, documents, audio - alongside your Discord messages.

> **Prerequisites:** [Messages](../guides/sending-messages.md#messages)

---

## File Size Limits

| Tier | Max File Size |
|---|---|
| **Free** (no Boost) | **25 MB** |
| **Boost Level 1** | 50 MB |
| **Boost Level 2** | 100 MB |
| **Boost Level 3** | 200 MB |

---

## Sending Files with CreateMessageRequest

To send attachments, add them to the `Attachments` list of `CreateMessageRequest`. Each entry references a local file or stream.

```csharp
using PawSharp.API.Models;

var request = new CreateMessageRequest
{
 Content = "Here's a file:",
 Attachments = new List<CreateAttachment>
 {
 new()
 {
 FileName = "report.pdf",
 ContentType = "application/pdf",
 Data = await File.ReadAllBytesAsync(@"C:\docs\report.pdf"),
 Description = "Monthly report"
 }
 }
};

var message = await rest.CreateMessageAsync(channelId, request);
```

### Multiple Attachments

```csharp
var request = new CreateMessageRequest
{
 Content = "Gallery upload:",
 Attachments = new List<CreateAttachment>
 {
 new()
 {
 FileName = "photo1.jpg",
 ContentType = "image/jpeg",
 Data = await File.ReadAllBytesAsync("photo1.jpg"),
 Description = "Sunset"
 },
 new()
 {
 FileName = "photo2.jpg",
 ContentType = "image/jpeg",
 Data = await File.ReadAllBytesAsync("photo2.jpg"),
 Description = "Beach"
 }
 }
};
```

 **Limit:** Up to 10 attachments per message.

---

## Sending Files with Streams

For large files, use a stream to avoid loading everything into memory:

```csharp
var request = new CreateMessageRequest
{
 Content = "Streaming a large file:",
 Attachments = new List<CreateAttachment>
 {
 new()
 {
 FileName = "bigfile.mp4",
 ContentType = "video/mp4",
 Stream = File.OpenRead(@"D:\videos\demo.mp4"),
 Description = "Demo video"
 }
 }
};
```

---

## FileBuilder / FileUploadBuilder (Components v2)

PawSharp's Components v2 system includes `File` (display an uploaded file) and `FileUpload` (let users upload). These live inside `Container` components.

### FileBuilder (Display Existing Attachment)

Reference an attachment that was already uploaded with the message:

```csharp
new ComponentBuilder()
 .AddContainer(container => container
 .AddFile("attachment://report.pdf", file => file
 .WithSpoiler(false)));
```

The `attachment://` URL references an attachment included in the same `Attachments` list.

```csharp
var request = new CreateMessageRequest
{
 Content = "See attached file:",
 Attachments = new List<CreateAttachment>
 {
 new()
 {
 FileName = "report.pdf",
 ContentType = "application/pdf",
 Data = pdfBytes
 }
 },
 Components = new ComponentBuilder()
 .AddContainer(container => container
 .AddFile("attachment://report.pdf"))
 .Build()
};
```

### FileUploadBuilder (User File Upload)

Lets users click a button to select and upload files:

```csharp
new ComponentBuilder()
 .AddContainer(container => container
 .AddFileUpload("avatar_upload", "Upload Avatar", upload => upload
 .WithRequired(true)
 .WithMaxLength(1)
 .WithFileTypes("image/png", "image/jpeg", "image/gif")
 .WithPlaceholder("Click to upload...")));
```

| Property | Limit | Description |
|---|---|---|
| `CustomId` | 100 chars | Used in the component handler |
| `Label` | 45 chars | Button label text |
| `Required` | - | Whether upload is mandatory |
| `MinLength` | 0 - 10 | Min number of files |
| `MaxLength` | 1 - 10 | Max number of files |
| `FileTypes` | - | MIME type filters (e.g. `"image/*"`) |
| `Placeholder` | 100 chars | Tooltip text |

Handle the file upload submission:

```csharp
handler.RegisterComponent("avatar_upload", async interaction =>
{
 var attachment = interaction.Data?.Resolved?.Attachments?.Values?.FirstOrDefault();
 if (attachment != null)
 {
 await handler.RespondUpdateAsync(interaction.Id, interaction.Token,
 $" Received `{attachment.Filename}` ({attachment.Size} bytes, type: {attachment.ContentType})");
 }
 else
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 " No file received.");
 }
});
```

 **Files uploaded via FileUpload are sent with the interaction.** You do not need to upload them separately. Access them from `interaction.Data.Resolved.Attachments`.

---

## Receiving Attachments in Messages

When reading messages, attachments arrive as `Attachment` objects:

```csharp
var message = await rest.GetChannelMessageAsync(channelId, messageId);

foreach (var attachment in message.Attachments)
{
 Console.WriteLine($"File: {attachment.Filename}");
 Console.WriteLine($"Size: {attachment.Size} bytes");
 Console.WriteLine($"URL: {attachment.Url}");
 Console.WriteLine($"Proxy URL: {attachment.ProxyUrl}");
 Console.WriteLine($"Type: {attachment.ContentType}");
 Console.WriteLine($"Dimensions: {attachment.Width}x{attachment.Height}");
}
```

### Downloading an Attachment

```csharp
using var httpClient = new HttpClient();

foreach (var attachment in message.Attachments)
{
 var bytes = await httpClient.GetByteArrayAsync(attachment.Url);
 await File.WriteAllBytesAsync($"downloads/{attachment.Filename}", bytes);
}
```

---

## File Size Validation

Always check file sizes before processing to avoid OOM or bandwidth issues:

```csharp
const int MaxFileSize = 25 * 1024 * 1024; // 25 MB

handler.RegisterComponent("file_upload", async interaction =>
{
 var attachment = interaction.Data?.Resolved?.Attachments?.Values?.FirstOrDefault();
 if (attachment == null)
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token, "No file.");
 return;
 }

 if (attachment.Size > MaxFileSize)
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 $" File too large ({attachment.Size / 1024 / 1024} MB). Max is 25 MB.");
 return;
 }

 await handler.RespondUpdateAsync(interaction.Id, interaction.Token,
 $" File `{attachment.Filename}` accepted ({attachment.Size} bytes).");
});
```

---

## Complete Example: Avatar Upload

```csharp
// Step 1: Send message with upload button
handler.RegisterCommand("avatar", async interaction =>
{
 var components = new ComponentBuilder()
 .AddContainer(container => container
 .AddFileUpload("avatar_upload", "Choose Avatar", upload => upload
 .WithRequired(true)
 .WithMaxLength(1)
 .WithFileTypes("image/png", "image/jpeg", "image/gif")))
 .Build();

 var response = new InteractionResponseBuilder()
 .WithContent("Upload your new avatar:")
 .AsEphemeral()
 .Build();

 response.Data.Components = components;
 await handler.RespondAsync(interaction.Id, interaction.Token, response);
});

// Step 2: Handle the upload
handler.RegisterComponent("avatar_upload", async interaction =>
{
 var attachment = interaction.Data?.Resolved?.Attachments?.Values?.FirstOrDefault();
 if (attachment == null)
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 " No file uploaded.");
 return;
 }

 // Validate
 if (!attachment.ContentType?.StartsWith("image/") ?? true)
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 " Only image files are allowed.");
 return;
 }

 if (attachment.Size > 8 * 1024 * 1024) // 8 MB
 {
 await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
 " Image must be under 8 MB.");
 return;
 }

 // Download the image
 using var http = new HttpClient();
 var imageBytes = await http.GetByteArrayAsync(attachment.Url);

 // Process (e.g., resize, store)...
 // await SaveAvatarAsync(interaction.Member?.User.Id ?? interaction.User?.Id, imageBytes);

 var embed = new EmbedBuilder()
 .WithTitle("Avatar Updated!")
 .WithImage(attachment.Url)
 .WithGreenColor()
 .Build();

 await handler.RespondUpdateAsync(interaction.Id, interaction.Token,
 " Your avatar has been updated.",
 embeds: new List<Embed> { embed });
});
```

---

## Related Guides

- [Components](./components.md) - File and FileUpload component reference
- [Embeds](./embeds.md) - Embedding images with `WithImage` and `WithThumbnail`
- [Slash Commands](./slash-commands.md) - Attachment option type for commands
