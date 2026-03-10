#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed inside a guild (server), not in DMs.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireGuildAttribute : Attribute, IPrecondition
{
    /// <inheritdoc/>
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        return ctx.GuildId.HasValue
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("This command can only be used inside a server."));
    }
}
