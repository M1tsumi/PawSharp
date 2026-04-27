#nullable enable
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Commands.Middleware;

/// <summary>
/// Built-in middleware implementations.
/// </summary>
internal static class BuiltInMiddleware
{
    /// <summary>
    /// Logging middleware that logs command execution.
    /// </summary>
    public sealed class LoggingMiddleware : IMiddleware
    {
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(CommandContext context, Func<Task> next)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Executing command {Command} by user {UserId} in channel {ChannelId}",
                context.CommandName, context.User.Id, context.ChannelId);

            try
            {
                await next();
                stopwatch.Stop();
                _logger.LogInformation("Command {Command} completed in {ElapsedMs}ms",
                    context.CommandName, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Command {Command} failed after {ElapsedMs}ms",
                    context.CommandName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// Execution timeout middleware that cancels long-running commands.
    /// </summary>
    public sealed class TimeoutMiddleware : IMiddleware
    {
        private readonly TimeSpan _timeout;
        private readonly ILogger<TimeoutMiddleware> _logger;

        public TimeoutMiddleware(TimeSpan timeout, ILogger<TimeoutMiddleware> logger)
        {
            _timeout = timeout;
            _logger = logger;
        }

        public async Task InvokeAsync(CommandContext context, Func<Task> next)
        {
            var task = next();
            
            if (await Task.WhenAny(task, Task.Delay(_timeout)) == task)
            {
                await task;
            }
            else
            {
                _logger.LogWarning("Command {Command} timed out after {TimeoutMs}ms",
                    context.CommandName, _timeout.TotalMilliseconds);
                throw new TimeoutException($"Command execution timed out after {_timeout.TotalSeconds} seconds");
            }
        }
    }

    /// <summary>
    /// Audit middleware that logs all command invocations for audit purposes.
    /// </summary>
    public sealed class AuditMiddleware : IMiddleware
    {
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(ILogger<AuditMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(CommandContext context, Func<Task> next)
        {
            _logger.LogInformation("[AUDIT] User {UserId} executed command {Command} with args: {Arguments}",
                context.User.Id, context.CommandName, string.Join(" ", context.Arguments));

            await next();
        }
    }
}
