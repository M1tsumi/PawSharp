#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PawSharp.Commands.Middleware;

/// <summary>
/// Manages the execution pipeline for command middleware.
/// Middleware are executed in the order they are added, with each middleware able to
/// wrap the next one in the chain.
/// </summary>
public class MiddlewarePipeline
{
    private readonly List<IMiddleware> _middlewares = new();

    /// <summary>
    /// Adds a middleware to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add to the pipeline.</param>
    /// <returns>The pipeline instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
    public MiddlewarePipeline Use(IMiddleware middleware)
    {
        if (middleware == null) throw new ArgumentNullException(nameof(middleware));
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Executes the middleware pipeline, running each middleware in order before the command.
    /// </summary>
    /// <param name="context">The command context containing information about the command being executed.</param>
    /// <param name="command">The command delegate to execute after all middleware have run.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Middleware are executed in the order they were added. Each middleware can choose
    /// to call the next middleware in the chain or short-circuit the pipeline.
    /// </remarks>
    public async Task ExecuteAsync(CommandContext context, Func<Task> command)
    {
        // Build the pipeline in reverse order
        Func<Task> pipeline = command;
        
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var current = pipeline;
            pipeline = () => middleware.InvokeAsync(context, current);
        }

        await pipeline();
    }

    /// <summary>
    /// Gets the number of middleware currently registered in the pipeline.
    /// </summary>
    public int Count => _middlewares.Count;
}
