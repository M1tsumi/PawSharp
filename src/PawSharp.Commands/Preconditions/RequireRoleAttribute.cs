#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command to users who have at least one of the specified roles.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IPrecondition
{
    /// <summary>
    /// Gets the role IDs that are required.
    /// </summary>
    public ulong[] RoleIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class.
    /// </summary>
    /// <param name="roleIds">The role IDs that are required.</param>
    public RequireRoleAttribute(params ulong[] roleIds)
    {
        RoleIds = roleIds ?? throw new ArgumentNullException(nameof(roleIds));
        if (roleIds.Length == 0)
            throw new ArgumentException("At least one role ID must be specified.", nameof(roleIds));
    }

    /// <inheritdoc/>
    public async Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        // Must be in a guild
        if (!ctx.GuildId.HasValue)
            return PreconditionResult.FromError("This command can only be used inside a server.");

        var member = ctx.Member;
        if (member is null)
            return PreconditionResult.FromError("Unable to resolve guild member roles.");

        // Get member roles from cache or API
        var roles = await GetMemberRolesAsync(ctx, member);
        if (roles is null)
            return PreconditionResult.FromError("Unable to resolve guild member roles.");

        // Check if member has any of the required roles
        var hasRole = roles.Any(r => RoleIds.Contains(r.Id));
        if (hasRole)
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError("You do not have the required role to run this command.");
    }

    private async Task<IReadOnlyList<Role>?> GetMemberRolesAsync(CommandContext ctx, GuildMember member)
    {
        try
        {
            // Try to get roles from cache
            var guild = ctx.Client.Cache.GetGuild(ctx.GuildId!.Value);
            if (guild != null && member.Roles != null)
            {
                var roleList = new List<Role>();
                foreach (var roleId in member.Roles)
                {
                    var role = guild.Roles?.FirstOrDefault(r => r.Id == roleId);
                    if (role != null)
                        roleList.Add(role);
                }
                return roleList;
            }

            // Fallback to API call
            var memberRoles = await ctx.Client.Rest.GetGuildMemberRolesAsync(ctx.GuildId.Value, member.User.Id);
            return memberRoles;
        }
        catch
        {
            return null;
        }
    }
}
