#nullable enable
using System;
using Microsoft.Extensions.DependencyInjection;
using PawSharp.Commands.Conversion;
using PawSharp.Commands.Middleware;
using PawSharp.Client;

namespace PawSharp.Commands.DependencyInjection;

/// <summary>
/// Extension methods for configuring PawSharp.Commands with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PawSharp.Commands services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="prefix">The command prefix (default: "!").</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommands(
        this IServiceCollection services,
        string prefix = "!",
        Action<CommandsOptions>? configureOptions = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var options = new CommandsOptions { Prefix = prefix };
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<TypeConverterService>();
        services.AddSingleton<MiddlewarePipeline>();

        return services;
    }

    /// <summary>
    /// Adds a middleware to the commands pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCommandMiddleware<TMiddleware>(this IServiceCollection services)
        where TMiddleware : class, IMiddleware
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddScoped<TMiddleware>();
        services.AddSingleton<IMiddleware>(sp => sp.GetRequiredService<TMiddleware>());

        return services;
    }
}

/// <summary>
/// Configuration options for PawSharp.Commands.
/// </summary>
public class CommandsOptions
{
    /// <summary>Gets or sets the command prefix.</summary>
    public string Prefix { get; set; } = "!";

    /// <summary>Gets or sets whether commands are case-sensitive.</summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>Gets or sets the command execution timeout.</summary>
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Gets or sets whether to enable built-in logging middleware.</summary>
    public bool EnableLoggingMiddleware { get; set; } = true;

    /// <summary>Gets or sets whether to enable built-in audit middleware.</summary>
    public bool EnableAuditMiddleware { get; set; } = false;
}
