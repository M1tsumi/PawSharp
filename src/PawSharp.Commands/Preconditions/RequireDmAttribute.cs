#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed in DMs, not in guilds.
/// Apply this attribute to a command method or class to ensure the command
/// can only be used in direct messages with the bot.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireDmAttribute : Attribute, IPrecondition
{
    /// <inheritdoc/>
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        return !ctx.GuildId.HasValue
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("This command can only be used in direct messages."));
    }
}
