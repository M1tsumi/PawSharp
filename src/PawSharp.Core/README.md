# PawSharp.Core

Core entities and models for the PawSharp Discord API wrapper.

PawSharp.Core provides the fundamental data models and entities that represent Discord's API objects. This package is the foundation for all other PawSharp packages and contains everything you need to work with Discord data in a type-safe, modern .NET way.

## Features

- Complete Discord API v10 entity models
- Type-safe with nullable reference types
- Snowflake ID handling
- JSON serialization support
- Input validation and data integrity checks
- Thread-safe, immutable models for shared data
- Additional properties and helper methods for common operations
- Typed message component hierarchy with polymorphic JSON deserialization
- Fluent `EmbedBuilder` with Discord limit enforcement
- `MessageFlags`, `ChannelFlags`, `AttachmentFlags`, `GuildMemberFlags` bitfield enums

## ?? Installation

```bash
dotnet add package PawSharp.Core --version 1.0.0-alpha.1
```

## ?? Quick Start

```csharp
using PawSharp.Core.Entities;

// Work with Discord entities
var user = new User
{
    Id = 123456789012345678,
    Username = "example_user",
    Discriminator = "1234",
    Avatar = "avatar_hash_here"
};

// Snowflake IDs are handled automatically
Snowflake userId = user.Id;
Console.WriteLine($"User ID: {userId}"); // User ID: 123456789012345678

// JSON serialization works out of the box
string json = System.Text.Json.JsonSerializer.Serialize(user);
```

## ?? What's Included

### Core Entities
- `User` - Discord users with avatar handling
- `Guild` - Servers with member counts and features
- `Channel` - All channel types (text, voice, DM, etc.)
- `Message` - Rich message content with embeds, attachments, and typed components
- `Member` - Guild members with roles and permissions
- `Role` - Guild roles with permissions and colors

### Component Models (alpha13)
- `MessageComponent` / `ActionRow` / `Button` / `SelectMenu` / `TextInput` - fully typed hierarchy
- `ComponentType`, `ButtonStyle`, `TextInputStyle` enums
- `SelectOption`, `SelectDefaultValue` supporting types
- `GuildPreview`, `GuildWidgetSettings`, `WelcomeScreen`, `FollowedChannel`, `VanityUrl` entities

### Advanced Models
- `Interaction` - Slash commands and component interactions
- `ApplicationCommand` - Slash command definitions
- `Webhook` - Webhook configurations
- `Invite` - Guild and channel invites
- `Emoji` - Custom and unicode emojis

### Builders
- `EmbedBuilder` - Fluent builder for `Embed` objects; enforces all Discord character and field-count limits at build time

### Utility Types
- `Snowflake` - Discord's ID system
- `Permissions` - Permission bitfields
- `MessageFlags`, `ChannelFlags`, `AttachmentFlags`, `GuildMemberFlags` - typed flag enums
- `Color` - Role and embed colors
- `Timestamp` - Discord timestamp handling

## ?? Usage Examples

### Working with Permissions

```csharp
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;

// Check permissions
var permissions = Permissions.Administrator;
bool canManage = permissions.HasFlag(Permissions.ManageGuild);

// Combine permissions
var modPermissions = Permissions.KickMembers | Permissions.BanMembers;
```

### Handling Snowflakes

```csharp
using PawSharp.Core.Entities;

// Create from various sources
Snowflake id1 = 123456789012345678;
Snowflake id2 = "123456789012345678";
Snowflake id3 = ulong.Parse("123456789012345678");

// Get timestamp (Discord epoch)
DateTimeOffset createdAt = id1.CreatedAt;
Console.WriteLine($"Created: {createdAt}"); // Created: 2015-10-19T10:00:00.0000000+00:00
```

### JSON Serialization

```csharp
using PawSharp.Core.Entities;
using System.Text.Json;

// Automatic serialization with custom converters
var embed = new Embed
{
    Title = "Example Embed",
    Description = "This is an example embed",
    Color = Color.Blue
};

string json = JsonSerializer.Serialize(embed);
// {"title":"Example Embed","description":"This is an example embed","color":3447003}
```

## ?? Dependencies

- **.NET 10** - Modern .NET runtime
- **System.Text.Json** - High-performance JSON serialization

## ?? Related Packages

- **[PawSharp.API](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.API)** - REST API client
- **[PawSharp.Gateway](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Gateway)** - WebSocket gateway client
- **[PawSharp.Client](https://github.com/yourorg/PawSharp/tree/main/src/PawSharp.Client)** - High-level client combining API and Gateway

## ?? License

MIT License - see [LICENSE](../LICENSE) for details.