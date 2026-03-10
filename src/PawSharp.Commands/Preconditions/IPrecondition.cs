#nullable enable
using System.Threading.Tasks;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Defines a check that must pass before a command is executed.
/// Apply derived attributes to command methods; <see cref="CommandsExtension"/> evaluates
/// every <see cref="IPrecondition"/> attribute present before invoking the handler.
/// </summary>
public interface IPrecondition
{
    /// <summary>
    /// Evaluates the precondition for the given command context.
    /// </summary>
    /// <param name="ctx">The context of the command being invoked.</param>
    /// <returns>
    /// A <see cref="PreconditionResult"/> indicating whether execution should proceed.
    /// </returns>
    Task<PreconditionResult> CheckAsync(CommandContext ctx);
}
