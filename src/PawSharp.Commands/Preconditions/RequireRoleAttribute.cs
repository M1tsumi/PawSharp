#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using PawSharp.Core.Entities;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command to users who have at least one of the specified roles.
/// Apply this attribute to a command method or class to ensure only users with
/// the specified role(s) can execute the command.
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
    /// <param name="roleIds">The role IDs that are required. The user must have at least one of these roles.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="roleIds"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="roleIds"/> is empty.</exception>
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
            var guild = ctx.Client.Cache.GetGuild(ctx.GuildId!.Value);

            // Determine which role IDs to resolve: from the member or from a fresh API call
            IReadOnlyList<ulong>? roleIds = member.Roles;

            if (roleIds is null || roleIds.Count == 0)
            {
                if (member.User is null)
                    return null;

                var guildMember = await ctx.Client.Rest.GetGuildMemberAsync(ctx.GuildId.Value, member.User.Id);
                roleIds = guildMember?.Roles;
            }

            if (roleIds is null || roleIds.Count == 0)
                return null;

            // Resolve IDs to Role objects if possible; fall back to ID-only stubs
            var roleList = new List<Role>(roleIds.Count);
            foreach (var roleId in roleIds)
            {
                var role = guild?.Roles?.FirstOrDefault(r => r.Id == roleId)
                           ?? new Role { Id = roleId, Name = $"<@{roleId}>" };
                roleList.Add(role);
            }
            return roleList;
        }
        catch
        {
            return null;
        }
    }
}
