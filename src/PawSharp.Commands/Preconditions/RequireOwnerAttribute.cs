#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed by the bot owner.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireOwnerAttribute : Attribute, IPrecondition
{
    private readonly ulong _ownerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireOwnerAttribute"/> class.
    /// </summary>
    /// <param name="ownerId">The owner's user ID.</param>
    public RequireOwnerAttribute(ulong ownerId)
    {
        _ownerId = ownerId;
    }

    /// <inheritdoc/>
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        return ctx.User.Id == _ownerId
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("Only the bot owner can use this command."));
    }
}
