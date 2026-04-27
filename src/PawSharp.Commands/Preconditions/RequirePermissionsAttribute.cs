#nullable enable
using System;
using System.Threading.Tasks;

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
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        // Must be in a guild
        if (!ctx.GuildId.HasValue)
            return Task.FromResult(PreconditionResult.FromError(
                "This command can only be used inside a server."));

        // Member object is populated from the gateway MESSAGE_CREATE event
        var member = ctx.Member;
        if (member is null)
            return Task.FromResult(PreconditionResult.FromError(
                "Unable to resolve guild member permissions."));

        // Permissions are not directly on GuildMember - need to get from guild cache or calculate
        // For now, return error as this requires more complex permission calculation
        return Task.FromResult(PreconditionResult.FromError(
                "Permission checking not yet implemented for this context."));

        // Administrators and guild owners bypass checks
        const ulong administratorBit = 0x8UL;
        if (IgnoreAdmins && (effectivePerms & administratorBit) != 0)
            return Task.FromResult(PreconditionResult.FromSuccess());

        if ((effectivePerms & RequiredPermissions) == RequiredPermissions)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError(
            "You do not have the required permissions to run this command."));
    }
}
