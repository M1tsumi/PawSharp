#nullable enable
using System.Threading.Tasks;

namespace PawSharp.Commands.Middleware;

/// <summary>
/// Interface for command middleware.
/// Middleware can be used to add cross-cutting concerns such as logging,
/// authentication, rate limiting, or performance monitoring to command execution.
/// </summary>
public interface IMiddleware
{
    /// <summary>
    /// Invokes the middleware, potentially wrapping the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The command context containing information about the command being executed.</param>
    /// <param name="next">A delegate representing the next middleware or the command itself.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Call <paramref name="next"/> to continue execution to the next middleware or command.
    /// You can add logic before and after calling <paramref name="next"/> to implement
    /// pre- and post-processing behavior.
    /// </remarks>
    Task InvokeAsync(CommandContext context, Func<Task> next);
}
