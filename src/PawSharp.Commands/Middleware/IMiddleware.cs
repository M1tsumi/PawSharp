#nullable enable
using System.Threading.Tasks;

namespace PawSharp.Commands.Middleware;

/// <summary>
/// Defines middleware that can intercept and modify command execution.
/// </summary>
public interface IMiddleware
{
    /// <summary>
    /// Executes the middleware logic before the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="next">The next middleware or command in the pipeline.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeAsync(CommandContext context, Func<Task> next);
}
