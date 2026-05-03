#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed when the bot has the specified permissions.
/// Apply this attribute to a command method or class to ensure the bot has the required
/// permissions before executing the command.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireBotPermissionAttribute : Attribute, IPrecondition
{
    /// <summary>
    /// Gets the required permissions for the bot.
    /// </summary>
    public ulong RequiredPermissions { get; }

    /// <summary>
    /// Gets or sets whether to ignore the bot's administrator permission bypass.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool IgnoreAdmins { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireBotPermissionAttribute"/> class.
    /// </summary>
    /// <param name="requiredPermissions">The required permissions for the bot.</param>
    public RequireBotPermissionAttribute(ulong requiredPermissions)
    {
        RequiredPermissions = requiredPermissions;
    }

    /// <inheritdoc/>
    public async Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        // Must be in a guild
        if (!ctx.GuildId.HasValue)
            return PreconditionResult.FromError(
                "This command can only be used inside a server.");

        var guildId = ctx.GuildId.Value;

        var guild = ctx.Client.Cache.GetGuild(guildId)
            ?? await ctx.Client.Rest.GetGuildAsync(guildId);

        if (guild == null)
        {
            return PreconditionResult.FromError("Unable to resolve guild data for permission checks.");
        }

        // Check if bot is the guild owner (bot should never be owner, but handle edge case)
        if (guild.OwnerId == ctx.Client.CurrentUser?.Id)
        {
            return PreconditionResult.FromSuccess();
        }

        var botMember = ctx.Client.Cache.GetGuildMember(guildId, ctx.Client.CurrentUser?.Id ?? 0)
            ?? await ctx.Client.Rest.GetGuildMemberAsync(guildId, ctx.Client.CurrentUser?.Id ?? 0);

        if (botMember is null)
        {
            return PreconditionResult.FromError("Unable to resolve bot's guild member permissions.");
        }

        var permissionsResult = await ResolveBotPermissionsAsync(ctx, guild, botMember);
        if (permissionsResult == null)
        {
            return PreconditionResult.FromError("Unable to resolve bot's guild member permissions.");
        }

        var effectivePermissions = permissionsResult.Value;
        var adminBit = (ulong)PawSharp.Core.Enums.Permissions.Administrator;

        if (IgnoreAdmins)
        {
            if ((effectivePermissions & adminBit) == adminBit)
                return PreconditionResult.FromSuccess();
        }

        return (effectivePermissions & RequiredPermissions) == RequiredPermissions
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("The bot does not have the required permissions to run this command.");
    }

    private static async Task<ulong?> ResolveBotPermissionsAsync(CommandContext ctx, Guild guild, GuildMember botMember)
    {
        if (botMember.Permissions.HasValue)
        {
            return (ulong)botMember.Permissions.Value;
        }

        var channel = ctx.Client.Cache.GetChannel(ctx.ChannelId)
            ?? await ctx.Client.Rest.GetChannelAsync(ctx.ChannelId);

        if (channel?.Permissions.HasValue == true)
        {
            return (ulong)channel.Permissions.Value;
        }

        var basePermissions = ComputeBasePermissions(guild, botMember, ctx.Client.CurrentUser?.Id ?? 0);
        if (basePermissions == null)
        {
            return null;
        }

        if (channel == null)
        {
            return basePermissions;
        }

        return ApplyChannelOverwrites(basePermissions.Value, channel, botMember, ctx.Client.CurrentUser?.Id ?? 0, guild.Id);
    }

    private static ulong? ComputeBasePermissions(Guild guild, GuildMember member, ulong botId)
    {
        if (guild.Roles == null || guild.Roles.Count == 0)
        {
            return null;
        }

        var everyoneRole = guild.Roles.FirstOrDefault(r => r.Id == guild.Id);
        var permissions = ParsePermissions(everyoneRole?.Permissions);

        foreach (var roleId in member.Roles ?? Array.Empty<ulong>())
        {
            var role = guild.Roles.FirstOrDefault(r => r.Id == roleId);
            if (role != null)
            {
                permissions |= ParsePermissions(role.Permissions);
            }
        }

        return permissions;
    }

    private static ulong ApplyChannelOverwrites(ulong basePermissions, Channel channel, GuildMember member, ulong botId, ulong guildId)
    {
        var permissions = basePermissions;

        // Apply @everyone overwrites
        var everyoneOverwrite = channel.PermissionOverwrites?.FirstOrDefault(o => o.Id == guildId);
        if (everyoneOverwrite != null)
        {
            permissions &= ~everyoneOverwrite.Deny;
            permissions |= everyoneOverwrite.Allow;
        }

        // Apply role overwrites
        if (member.Roles != null)
        {
            foreach (var roleId in member.Roles)
            {
                var roleOverwrite = channel.PermissionOverwrites?.FirstOrDefault(o => o.Id == roleId);
                if (roleOverwrite != null)
                {
                    permissions &= ~roleOverwrite.Deny;
                    permissions |= roleOverwrite.Allow;
                }
            }
        }

        // Apply member-specific overwrites
        var memberOverwrite = channel.PermissionOverwrites?.FirstOrDefault(o => o.Id == botId);
        if (memberOverwrite != null)
        {
            permissions &= ~memberOverwrite.Deny;
            permissions |= memberOverwrite.Allow;
        }

        return permissions;
    }

    private static ulong ParsePermissions(ulong? permissions)
    {
        return permissions ?? 0;
    }
}
