#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command to members who hold at least the specified Discord permission bits.
/// </summary>
/// <remarks>
/// Discord includes a computed <c>permissions</c> bitfield string on the <c>member</c> object
/// embedded in <c>MESSAGE_CREATE</c> gateway events.  This attribute parses that value and
/// checks it against <see cref="RequiredPermissions"/>.
/// Guild owners implicitly hold the <c>ADMINISTRATOR</c> bit; they bypass all checks when
/// <see cref="IgnoreAdmins"/> is <see langword="true"/> (the default).
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionsAttribute : Attribute, IPrecondition
{
    /// <summary>
    /// The Discord permission bits that must be present.
    /// </summary>
    public ulong RequiredPermissions { get; }

    /// <summary>
    /// When <see langword="true"/> (default) the <c>ADMINISTRATOR</c> permission (bit 3)
    /// and the guild owner bypass the check.
    /// </summary>
    public bool IgnoreAdmins { get; set; } = true;

    /// <summary>
    /// Initialises the attribute with the required permission bit mask.
    /// </summary>
    /// <param name="permissions">Bitwise OR of the Discord permission values to require.</param>
    public RequirePermissionsAttribute(ulong permissions)
    {
        RequiredPermissions = permissions;
    }

    /// <inheritdoc/>
    public async Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        // Must be in a guild
        if (!ctx.GuildId.HasValue)
            return PreconditionResult.FromError(
                "This command can only be used inside a server.");

        var guildId = ctx.GuildId.Value;
        var cacheKey = (guildId, ctx.User.Id, ctx.ChannelId);
        var now = DateTimeOffset.UtcNow;

        // Check cache first
        lock (_cacheLock)
        {
            if (_permissionCache.TryGetValue(cacheKey, out var cached) && cached.expiry > now)
            {
                var cachedPermissions = cached.permissions;
                var cachedAdminBit = (ulong)PawSharp.Core.Enums.Permissions.Administrator;

                if (IgnoreAdmins)
                {
                    var cachedGuild = ctx.Client.Cache.GetGuild(guildId);
                    if (cachedGuild != null && (cachedGuild.OwnerId == ctx.User.Id || (cachedPermissions & cachedAdminBit) == cachedAdminBit))
                        return PreconditionResult.FromSuccess();
                }

                return (cachedPermissions & RequiredPermissions) == RequiredPermissions
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("You do not have the required permissions to run this command.");
            }
        }

        // Cache miss - fetch from API
        var guild = ctx.Client.Cache.GetGuild(guildId)
            ?? await ctx.Client.Rest.GetGuildAsync(guildId);

        if (guild == null)
        {
            return PreconditionResult.FromError("Unable to resolve guild data for permission checks.");
        }

        var member = ctx.Member
            ?? ctx.Client.Cache.GetGuildMember(guildId, ctx.User.Id)
            ?? await ctx.Client.Rest.GetGuildMemberAsync(guildId, ctx.User.Id);

        if (member is null)
        {
            return PreconditionResult.FromError("Unable to resolve guild member permissions.");
        }

        var permissionsResult = await ResolveMemberPermissionsAsync(ctx, guild, member);
        if (permissionsResult == null)
        {
            return PreconditionResult.FromError("Unable to resolve guild member permissions.");
        }

        var effectivePermissions = permissionsResult.Value;
        var adminBit = (ulong)PawSharp.Core.Enums.Permissions.Administrator;

        // Cache the result
        lock (_cacheLock)
        {
            _permissionCache[cacheKey] = (effectivePermissions, now.Add(CacheTtl));
            
            // Periodic cleanup of expired cache entries
            if (_permissionCache.Count > 1000)
            {
                var expiredKeys = _permissionCache
                    .Where(kvp => kvp.Value.expiry <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredKeys)
                {
                    _permissionCache.Remove(key);
                }
            }
        }

        if (IgnoreAdmins)
        {
            if (guild.OwnerId == ctx.User.Id || (effectivePermissions & adminBit) == adminBit)
                return PreconditionResult.FromSuccess();
        }

        return (effectivePermissions & RequiredPermissions) == RequiredPermissions
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You do not have the required permissions to run this command.");
    }

    private static async Task<ulong?> ResolveMemberPermissionsAsync(CommandContext ctx, Guild guild, GuildMember member)
    {
        if (member.Permissions.HasValue)
        {
            return (ulong)member.Permissions.Value;
        }

        var channel = ctx.Client.Cache.GetChannel(ctx.ChannelId)
            ?? await ctx.Client.Rest.GetChannelAsync(ctx.ChannelId);

        if (channel?.Permissions.HasValue == true)
        {
            return (ulong)channel.Permissions.Value;
        }

        var basePermissions = ComputeBasePermissions(guild, member, ctx.User.Id);
        if (basePermissions == null)
        {
            return null;
        }

        if (channel == null)
        {
            return basePermissions;
        }

        return ApplyChannelOverwrites(basePermissions.Value, channel, member, ctx.User.Id, guild.Id);
    }

    // Permission cache with TTL to reduce API calls
    private static readonly Dictionary<(ulong guildId, ulong userId, ulong channelId), (ulong permissions, DateTimeOffset expiry)> 
        _permissionCache = new();
    private static readonly object _cacheLock = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private static ulong? ComputeBasePermissions(Guild guild, GuildMember member, ulong userId)
    {
        if (guild.Roles == null || guild.Roles.Count == 0)
        {
            return null;
        }

        var everyoneRole = guild.Roles.FirstOrDefault(r => r.Id == guild.Id);
        var permissions = ParsePermissions(everyoneRole?.Permissions);

        foreach (var roleId in member.Roles)
        {
            var role = guild.Roles.FirstOrDefault(r => r.Id == roleId);
            permissions |= ParsePermissions(role?.Permissions);
        }

        return permissions;
    }

    private static ulong ApplyChannelOverwrites(
        ulong permissions,
        Channel channel,
        GuildMember member,
        ulong userId,
        ulong guildId)
    {
        var adminBit = (ulong)PawSharp.Core.Enums.Permissions.Administrator;
        if ((permissions & adminBit) == adminBit)
        {
            return permissions;
        }

        var overwrites = channel.PermissionOverwrites;
        if (overwrites == null || overwrites.Count == 0)
        {
            return permissions;
        }

        var everyoneOverwrite = overwrites.FirstOrDefault(o => o.Type == OverwriteType.Role && o.Id == guildId);
        if (everyoneOverwrite != null)
        {
            ApplyOverwrite(ref permissions, everyoneOverwrite);
        }

        ulong roleAllow = 0;
        ulong roleDeny = 0;
        foreach (var roleId in member.Roles)
        {
            var overwrite = overwrites.FirstOrDefault(o => o.Type == OverwriteType.Role && o.Id == roleId);
            if (overwrite == null)
                continue;

            roleAllow |= ParsePermissions(overwrite.Allow);
            roleDeny |= ParsePermissions(overwrite.Deny);
        }

        permissions &= ~roleDeny;
        permissions |= roleAllow;

        var memberOverwrite = overwrites.FirstOrDefault(o => o.Type == OverwriteType.Member && o.Id == userId);
        if (memberOverwrite != null)
        {
            ApplyOverwrite(ref permissions, memberOverwrite);
        }

        return permissions;
    }

    private static void ApplyOverwrite(ref ulong permissions, Overwrite overwrite)
    {
        var allow = ParsePermissions(overwrite.Allow);
        var deny = ParsePermissions(overwrite.Deny);
        permissions &= ~deny;
        permissions |= allow;
    }

    private static ulong ParsePermissions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        return ulong.TryParse(value, out var permissions) ? permissions : 0;
    }
}
