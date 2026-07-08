# Permissions

Discord's permission system controls what users and bots can do in a server. PawSharp models this with a `[Flags]` enum and provides helpers for common checking patterns.

> **Prerequisites:** [Roles & Guilds](../guides/sending-messages.md#guilds)

---

## Permission Model

Permissions are stored as a 64-bit bitmask. Each bit represents one permission. Discord computes effective permissions by combining:
1. **@everyone role** permissions
2. **Individual role** permissions (OR-combined)
3. **Channel overwrites** for roles (allow/deny)
4. **Channel overwrites** for the specific member (allow/deny)
5. **Administrator** — overrides everything

`Administrator` implicitly grants every other permission and bypasses all channel overwrites.

---

## Permission Enum

All 50 permission flags:

```csharp
[Flags]
public enum Permissions : ulong
{
    None                                          = 0,
    CreateInstantInvite                           = 1UL << 0,
    KickMembers                                   = 1UL << 1,
    BanMembers                                    = 1UL << 2,
    Administrator                                 = 1UL << 3,
    ManageChannels                                = 1UL << 4,
    ManageGuild                                   = 1UL << 5,
    AddReactions                                  = 1UL << 6,
    ViewAuditLog                                  = 1UL << 7,
    PrioritySpeaker                               = 1UL << 8,
    Stream                                        = 1UL << 9,
    ViewChannel                                   = 1UL << 10,
    SendMessages                                  = 1UL << 11,
    SendTTSMessages                               = 1UL << 12,
    ManageMessages                                = 1UL << 13,
    EmbedLinks                                    = 1UL << 14,
    AttachFiles                                   = 1UL << 15,
    ReadMessageHistory                            = 1UL << 16,
    MentionEveryone                               = 1UL << 17,
    UseExternalEmojis                             = 1UL << 18,
    ViewGuildInsights                             = 1UL << 19,
    Connect                                       = 1UL << 20,
    Speak                                         = 1UL << 21,
    MuteMembers                                   = 1UL << 22,
    DeafenMembers                                 = 1UL << 23,
    MoveMembers                                   = 1UL << 24,
    UseVAD                                        = 1UL << 25,
    ChangeNickname                                = 1UL << 26,
    ManageNicknames                               = 1UL << 27,
    ManageRoles                                   = 1UL << 28,
    ManageWebhooks                                = 1UL << 29,
    ManageGuildExpressions                        = 1UL << 30,
    UseApplicationCommands                        = 1UL << 31,
    RequestToSpeak                                = 1UL << 32,
    ManageEvents                                  = 1UL << 33,
    ManageThreads                                 = 1UL << 34,
    CreatePublicThreads                           = 1UL << 35,
    CreatePrivateThreads                          = 1UL << 36,
    UseExternalStickers                           = 1UL << 37,
    SendMessagesInThreads                         = 1UL << 38,
    UseEmbeddedActivities                         = 1UL << 39,
    ModerateMembers                               = 1UL << 40,
    ViewCreatorMonetizationAnalytics              = 1UL << 41,
    UseSoundboard                                 = 1UL << 42,
    CreateGuildExpressions                        = 1UL << 43,
    CreateEvents                                  = 1UL << 44,
    UseExternalSounds                             = 1UL << 45,
    SendVoiceMessages                             = 1UL << 46,
    SetVoiceChannelStatus                         = 1UL << 47,
    SendPolls                                     = 1UL << 48,
    UseExternalApps                               = 1UL << 49,
}
```

---

## Checking Permissions

### Checking the Bot's Permissions

Use the `Permissions` property on the bot's guild member object:

```csharp
// Get the bot's guild member
var botMember = await rest.GetGuildMemberAsync(guildId, client.CurrentUser.Id);

// Check individual permission
if (botMember.Permissions.HasFlag(Permissions.Administrator))
    Console.WriteLine("Bot is admin");

if (botMember.Permissions.HasFlag(Permissions.ManageMessages))
    Console.WriteLine("Bot can manage messages");

// Check multiple permissions
var required = Permissions.ManageMessages | Permissions.ManageRoles;
if (botMember.Permissions.HasFlag(required))
    Console.WriteLine("Bot has both ManageMessages and ManageRoles");
```

### Checking a User's Permissions

```csharp
var member = await rest.GetGuildMemberAsync(guildId, userId);

if (member.Permissions.HasFlag(Permissions.KickMembers))
    Console.WriteLine("User can kick members");
else
    Console.WriteLine("User cannot kick members");
```

⚠️ **`Permissions` on a member object is the computed permission set for that member in the guild** (all roles + @everyone combined, but NOT channel-specific overwrites).

### Checking Channel-Specific Permissions

For channel-level checks, you need to compute permissions manually from overwrites:

```csharp
public bool CanUserAccessChannel(GuildMember member, Channel channel, Permissions required)
{
    // Administrator bypasses all checks
    if (member.Permissions.HasFlag(Permissions.Administrator))
        return true;

    // Check @everyone overwrites
    var everyoneOverwrite = channel.PermissionOverwrites
        .FirstOrDefault(o => o.Id == guildId && o.Type == PermissionOverwriteType.Role);

    // Check role overwrites
    var roleOverwrites = channel.PermissionOverwrites
        .Where(o => member.RoleIds.Contains(o.Id) && o.Type == PermissionOverwriteType.Role);

    // Check member overwrite
    var memberOverwrite = channel.PermissionOverwrites
        .FirstOrDefault(o => o.Id == member.User.Id && o.Type == PermissionOverwriteType.Member);

    // Apply deny bits first, then allow bits
    // (Simplified — full permission computation follows Discord's hierarchy)
    return true; // Full implementation depends on your needs
}
```

---

## Permission Hierarchy (Role Order)

The highest role a member has determines their effective position:

```csharp
public class RoleHierarchy
{
    private readonly IDiscordRestClient _rest;

    public RoleHierarchy(IDiscordRestClient rest) => _rest = rest;

    /// <summary>Returns true if the bot can moderate the target member.</summary>
    public async Task<bool> CanBotModerateAsync(ulong guildId, ulong targetUserId)
    {
        var botId = client.CurrentUser.Id;
        var botMember = await _rest.GetGuildMemberAsync(guildId, botId);
        var targetMember = await _rest.GetGuildMemberAsync(guildId, targetUserId);
        var guild = await _rest.GetGuildAsync(guildId);

        // Sort roles by position (descending)
        var botRoles = guild.Roles
            .Where(r => botMember.RoleIds.Contains(r.Id))
            .OrderByDescending(r => r.Position);

        var targetRoles = guild.Roles
            .Where(r => targetMember.RoleIds.Contains(r.Id))
            .OrderByDescending(r => r.Position);

        var botHighest = botRoles.FirstOrDefault()?.Position ?? 0;
        var targetHighest = targetRoles.FirstOrDefault()?.Position ?? 0;

        // Bot can moderate if its highest role is above the target's highest role
        // AND the bot has the relevant permission (KickMembers, BanMembers, ModerateMembers)
        return botHighest > targetHighest
            && botMember.Permissions.HasFlag(Permissions.ModerateMembers);
    }

    /// <summary>Returns the highest role position for a member.</summary>
    public async Task<int> GetHighestRolePositionAsync(ulong guildId, ulong userId)
    {
        var member = await _rest.GetGuildMemberAsync(guildId, userId);
        var guild = await _rest.GetGuildAsync(guildId);

        return guild.Roles
            .Where(r => member.RoleIds.Contains(r.Id))
            .OrderByDescending(r => r.Position)
            .FirstOrDefault()?.Position ?? 0;
    }
}
```

✅ **Key rule:** A bot cannot moderate (kick/ban/timeout) a user whose highest role is at or above the bot's highest role.

---

## Permission Validation for Commands

Use inline checks at the start of command handlers:

```csharp
handler.RegisterCommand("ban", async interaction =>
{
    // Validate bot permissions
    var botMember = await rest.GetGuildMemberAsync(interaction.GuildId!.Value, client.CurrentUser.Id);
    if (!botMember.Permissions.HasFlag(Permissions.BanMembers))
    {
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            "❌ I don't have the `BanMembers` permission.");
        return;
    }

    // Validate user permissions
    var userMember = await rest.GetGuildMemberAsync(interaction.GuildId!.Value,
        interaction.Member?.User.Id ?? interaction.User?.Id);
    if (!userMember.Permissions.HasFlag(Permissions.BanMembers))
    {
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            "❌ You don't have permission to ban members.");
        return;
    }

    // Validate hierarchy
    var targetId = /* parse from command options */;
    var targetMember = await rest.GetGuildMemberAsync(interaction.GuildId!.Value, targetId);
    var guild = await rest.GetGuildAsync(interaction.GuildId!.Value);

    var botHighest = guild.Roles
        .Where(r => botMember.RoleIds.Contains(r.Id))
        .MaxBy(r => r.Position)?.Position ?? 0;
    var targetHighest = guild.Roles
        .Where(r => targetMember.RoleIds.Contains(r.Id))
        .MaxBy(r => r.Position)?.Position ?? 0;

    if (botHighest <= targetHighest)
    {
        await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
            "❌ I cannot ban that user — their highest role is at or above mine.");
        return;
    }

    // Proceed with ban
    await rest.CreateGuildBanAsync(interaction.GuildId!.Value, targetId, 0, "Banned by moderator");
    await handler.RespondEphemeralAsync(interaction.Id, interaction.Token,
        $"✅ Banned <@{targetId}>.");
});
```

---

## Common Scenarios

### Require Permission for Slash Command

```csharp
new SlashCommandBuilder("kick", "Kick a member")
    .SetDefaultMemberPermissions(Permissions.KickMembers)
    .AddUserOption("user", "The user to kick", required: true)
    .AddStringOption("reason", "Reason for the kick")
```

`SetDefaultMemberPermissions` makes the command visible only to members with that permission.

### Check Administrator

```csharp
if (member.Permissions.HasFlag(Permissions.Administrator))
{
    // Skip all other permission checks
}
```

### Build Permission Bitmask

```csharp
var allowed = Permissions.ViewChannel
    | Permissions.SendMessages
    | Permissions.ReadMessageHistory
    | Permissions.AddReactions;
```

---

## Tips

💡 **Always check bot permissions first.** Users get a clearer error when told the bot lacks permissions vs. a cryptic API error.

💡 **Cache guild roles and member permissions** for performance — avoid fetching on every interaction.

💡 **The `Administrator` permission overrides everything**, including channel-specific deny overwrites. Check it early.

💡 **Role hierarchy matters for moderation actions.** Always verify the bot's highest role position is above the target's.

---

## Related Guides

- [Slash Commands](./slash-commands.md) — `SetDefaultMemberPermissions`
- [Moderation Patterns](../guides/advanced.md#moderation) — Kick/ban with logging
