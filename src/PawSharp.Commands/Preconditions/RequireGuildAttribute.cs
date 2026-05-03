#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed inside a guild (server), not in DMs.
/// Apply this attribute to a command method or class to ensure the command
/// can only be used within Discord servers.
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
