#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PawSharp.Commands.Middleware;

/// <summary>
/// Manages the execution pipeline for command middleware.
/// </summary>
public class MiddlewarePipeline
{
    private readonly List<IMiddleware> _middlewares = new();

    /// <summary>
    /// Adds a middleware to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    /// <returns>The pipeline for chaining.</returns>
    public MiddlewarePipeline Use(IMiddleware middleware)
    {
        if (middleware == null) throw new ArgumentNullException(nameof(middleware));
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Executes the middleware pipeline.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="command">The command delegate to execute after middleware.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
    /// Gets the number of middleware in the pipeline.
    /// </summary>
    public int Count => _middlewares.Count;
}
