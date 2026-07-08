# Permissions

## Permission Model

Discord uses a permission system based on roles. Each permission is a bit in a 64-bit flags value.

### Common Permissions

```csharp
[Flags]
public enum Permissions : ulong
{
    CreateInstantInvite = 1 << 0,
    KickMembers = 1 << 1,
    BanMembers = 1 << 2,
    Administrator = 1 << 3,
    ManageChannels = 1 << 4,
    ManageGuild = 1 << 5,
    AddReactions = 1 << 6,
    ViewAuditLog = 1 << 7,
    PrioritySpeaker = 1 << 8,
    Stream = 1 << 9,
    ViewChannel = 1 << 10,
    SendMessages = 1 << 11,
    SendTtsMessages = 1 << 12,
    ManageMessages = 1 << 13,
    EmbedLinks = 1 << 14,
    AttachFiles = 1 << 15,
    ReadMessageHistory = 1 << 16,
    MentionEveryone = 1 << 17,
    UseExternalEmojis = 1 << 18,
    Connect = 1 << 20,
    Speak = 1 << 21,
    MuteMembers = 1 << 22,
    DeafenMembers = 1 << 23,
    MoveMembers = 1 << 24,
    UseVad = 1 << 25,
    ChangeNickname = 1 << 26,
    ManageNicknames = 1 << 27,
    ManageRoles = 1 << 28,
    ManageWebhooks = 1 << 29,
    ManageEmojis = 1 << 30,
    // ... and more
}
```

## Checking Permissions

```csharp
// Check if a member has a specific permission
var member = await client.Rest.GetGuildMemberAsync(guildId, userId);
var permissions = member.Permissions;

if (permissions.HasFlag(Permissions.Administrator))
    Console.WriteLine("User is admin");

if (permissions.HasFlag(Permissions.KickMembers))
    Console.WriteLine("User can kick members");
```

## Role Hierarchy

```csharp
// Check if bot can moderate a target member
var botMember = await client.Rest.GetGuildMemberAsync(guildId, client.CurrentUser.Id);
var targetMember = await client.Rest.GetGuildMemberAsync(guildId, targetUserId);

var guild = await client.Rest.GetGuildAsync(guildId);
var botRoles = botMember.RoleIds.Select(id => guild.Roles.First(r => r.Id == id));
var targetRoles = targetMember.RoleIds.Select(id => guild.Roles.First(r => r.Id == id));

var highestBotRole = botRoles.OrderByDescending(r => r.Position).First();
var highestTargetRole = targetRoles.OrderByDescending(r => r.Position).First();

if (highestBotRole.Position > highestTargetRole.Position)
{
    // Bot can moderate this user
}
else
{
    Console.WriteLine("Target user has a higher role than the bot");
}
```

## Channel Permissions

```csharp
// Get channel permission overwrites
var channel = await client.Rest.GetChannelAsync(channelId);

foreach (var overwrite in channel.PermissionOverwrites)
{
    Console.WriteLine($"Overwrite for {(overwrite.Type == PermissionOverwriteType.Role ? "role" : "member")} {overwrite.Id}");
}
```
