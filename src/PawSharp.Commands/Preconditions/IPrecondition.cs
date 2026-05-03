#nullable enable
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Interface for command precondition checks.
/// Preconditions are evaluated before command execution and can block execution
/// based on custom logic such as permissions, cooldowns, or user state.
/// </summary>
public interface IPrecondition
{
    /// <summary>
    /// Checks whether the command can be executed in the given context.
    /// </summary>
    /// <param name="ctx">The context of the command being invoked.</param>
    /// <returns>
    /// A <see cref="PreconditionResult"/> indicating whether execution should proceed.
    /// </returns>
    Task<PreconditionResult> CheckAsync(CommandContext ctx);
}
