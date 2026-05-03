#nullable enable
using System;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed in NSFW channels.
/// Apply this attribute to a command method or class to ensure the command
/// can only be used in channels marked as NSFW (age-restricted).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireNsfwAttribute : Attribute, IPrecondition
{
    /// <inheritdoc/>
    public async Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        // Must be in a guild
        if (!ctx.GuildId.HasValue)
            return PreconditionResult.FromError("This command can only be used inside a server.");

        try
        {
            var channel = await ctx.Client.Rest.GetChannelAsync(ctx.ChannelId);
            if (channel is null)
                return PreconditionResult.FromError("Unable to resolve channel information.");

            if (!channel.Nsfw.GetValueOrDefault())
                return PreconditionResult.FromError("This command can only be used in NSFW channels.");

            return PreconditionResult.FromSuccess();
        }
        catch
        {
            return PreconditionResult.FromError("Unable to verify channel NSFW status.");
        }
    }
}
