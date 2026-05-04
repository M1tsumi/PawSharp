#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Restricts a command so it can only be executed in specific channels.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequireChannelAttribute : Attribute, IPrecondition
{
    /// <summary>
    /// Gets the channel IDs where the command is allowed.
    /// </summary>
    public ulong[] ChannelIds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireChannelAttribute"/> class.
    /// </summary>
    /// <param name="channelIds">The channel IDs where the command is allowed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="channelIds"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="channelIds"/> is empty.</exception>
    public RequireChannelAttribute(params ulong[] channelIds)
    {
        ChannelIds = channelIds ?? throw new ArgumentNullException(nameof(channelIds));
        if (channelIds.Length == 0)
            throw new ArgumentException("At least one channel ID must be specified.", nameof(channelIds));
    }

    /// <inheritdoc/>
    public Task<PreconditionResult> CheckAsync(CommandContext ctx)
    {
        if (ChannelIds.Contains(ctx.ChannelId))
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError("This command can only be used in specific channels."));
    }
}
