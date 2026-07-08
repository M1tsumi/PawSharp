# Serialization

PawSharp uses `System.Text.Json` with source-generated contexts for maximum performance and Native AOT compatibility.

---

## System.Text.Json Source Generation

Two source-generated contexts provide compile-time metadata:

### PawSharpJsonContext (Core Entities)

`src/PawSharp.Core/Serialization/PawSharpJsonContext.cs` — 80+ types:

```csharp
[JsonSerializable(typeof(Guild))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Channel))]
[JsonSerializable(typeof(Role))]
// ...
public partial class PawSharpJsonContext : JsonSerializerContext { }
```

### PawSharpApiJsonContext (API Models)

`src/PawSharp.API/Serialization/PawSharpApiJsonContext.cs` — request/response types:

```csharp
[JsonSerializable(typeof(CreateMessageRequest))]
[JsonSerializable(typeof(EditMessageRequest))]
[JsonSerializable(typeof(InteractionResponse))]
// ...
public partial class PawSharpApiJsonContext : JsonSerializerContext { }
```

Both are combined in the shared options:

```csharp
TypeInfoResolver = JsonTypeInfoResolver.Combine(
    PawSharpApiJsonContext.Default,
    PawSharpJsonContext.Default)
```

---

## SnowflakeJsonConverter

Discord snowflake IDs are serialized as strings in JSON (to preserve precision) but stored as `ulong` in C#:

```csharp
public class SnowflakeJsonConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return ulong.Parse(reader.GetString()!);
        return reader.GetUInt64();
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

---

## SnakeCase Naming Policy

```csharp
PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
```

C# `GuildId` → JSON `"guild_id"`, `ChannelId` → `"channel_id"`.

---

## AOT Compatibility

`Directory.Build.props` enables trimming and AOT:

```xml
<IsAotCompatible>true</IsAotCompatible>
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>link</TrimMode>
```

Source generators replace reflection, making the library publishable as a trimmed, AOT-compiled native binary.

---

## Custom JSON Converters

Add custom converters to `_jsonOptions.Converters`:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    Converters = { new SnowflakeJsonConverter() },
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

---

## Multipart Form Data for File Uploads

File uploads use `MultipartFormDataContent`:

```csharp
using var form = new MultipartFormDataContent();
var fileContent = new StreamContent(fileStream);
fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
form.Add(fileContent, "files[0]", fileName);

if (messageRequest is not null)
{
    var json = JsonSerializer.Serialize(messageRequest, _jsonOptions);
    form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");
}

var response = await PostAsync($"channels/{channelId}/messages", form, ct);
```

---

## Performance Considerations

| Aspect | Detail |
|--------|--------|
| Source generation | Zero reflection at runtime |
| `SnakeCaseLower` | No custom `JsonPropertyName` attributes needed |
| `WhenWritingNull` | Reduces payload size |
| `AllowReadingFromString` | Handles Discord's string-encoded numbers |
| Combined resolver | Fallback chain for serialization |

---

## Common Mistakes

| Mistake | Solution |
|---------|----------|
| Not using source-generated contexts | Falls back to reflection, breaks AOT |
| Forgetting `SnakeCaseLower` | Discord expects `guild_id` not `GuildId` |
| Setting `IgnoreReadOnlyProperties = true` | May skip required fields |
| Using `Newtonsoft.Json` interop | Stick to `System.Text.Json` for AOT |
| Not handling string-encoded numbers | Configure `NumberHandling = AllowReadingFromString` |
