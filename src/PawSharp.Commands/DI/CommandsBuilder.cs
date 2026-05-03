#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PawSharp.Commands.Conversion;
using PawSharp.Commands.Middleware;
using PawSharp.Client;

namespace PawSharp.Commands.DependencyInjection;

/// <summary>
/// Builder for configuring the commands extension with dependency injection.
/// Provides a fluent API for setting up command prefix, case sensitivity, middleware, and type converters.
/// </summary>
public class CommandsBuilder
{
    private string _prefix = "!";
    private bool _caseSensitive = false;
    private TimeSpan? _executionTimeout;
    private bool _enableLoggingMiddleware = true;
    private bool _enableAuditMiddleware = false;
    private readonly List<IMiddleware> _customMiddleware = new();
    private readonly List<ITypeConverter> _customConverters = new();
    private readonly ILogger<CommandsBuilder>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsBuilder"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public CommandsBuilder(ILogger<CommandsBuilder>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sets the command prefix used for prefix-based commands.
    /// </summary>
    /// <param name="prefix">The prefix string (default: "!").</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandsBuilder WithPrefix(string prefix)
    {
        _prefix = prefix;
        return this;
    }

    /// <summary>
    /// Sets whether command names are case-sensitive.
    /// </summary>
    /// <param name="caseSensitive">True for case-sensitive, false for case-insensitive (default).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandsBuilder WithCaseSensitivity(bool caseSensitive)
    {
        _caseSensitive = caseSensitive;
        return this;
    }

    /// <summary>
    /// Sets the execution timeout for commands.
    /// Commands exceeding this duration will be cancelled.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandsBuilder WithExecutionTimeout(TimeSpan timeout)
    {
        _executionTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables the built-in logging middleware.
    /// </summary>
    /// <param name="enable">True to enable logging middleware (default: true).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandsBuilder WithLoggingMiddleware(bool enable)
    {
        _enableLoggingMiddleware = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables the built-in audit middleware.
    /// </summary>
    /// <param name="enable">True to enable audit middleware (default: false).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CommandsBuilder WithAuditMiddleware(bool enable)
    {
        _enableAuditMiddleware = enable;
        return this;
    }

    /// <summary>
    /// Adds a custom middleware to the command execution pipeline.
    /// </summary>
    /// <param name="middleware">The middleware instance to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
    public CommandsBuilder AddMiddleware(IMiddleware middleware)
    {
        _customMiddleware.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds a custom type converter for converting command arguments.
    /// </summary>
    /// <param name="converter">The type converter instance to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
    public CommandsBuilder AddConverter(ITypeConverter converter)
    {
        _customConverters.Add(converter);
        return this;
    }

    /// <summary>
    /// Builds the configuration and registers all services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public void Build(IServiceCollection services)
    {
        services.AddSingleton(new TypeConverterService(_logger));
        services.AddSingleton(new MiddlewarePipeline());

        foreach (var converter in _customConverters)
        {
            var typeConverterService = services.BuildServiceProvider().GetRequiredService<TypeConverterService>();
            typeConverterService.RegisterConverterFromInterface(converter);
        }

        var pipeline = services.BuildServiceProvider().GetRequiredService<MiddlewarePipeline>();
        if (_enableLoggingMiddleware)
        {
            pipeline.Use(new BuiltInMiddleware.LoggingMiddleware(
                services.BuildServiceProvider().GetRequiredService<ILogger<BuiltInMiddleware.LoggingMiddleware>>()));
        }
        if (_enableAuditMiddleware)
        {
            pipeline.Use(new BuiltInMiddleware.AuditMiddleware(
                services.BuildServiceProvider().GetRequiredService<ILogger<BuiltInMiddleware.AuditMiddleware>>()));
        }
        if (_executionTimeout.HasValue)
        {
            pipeline.Use(new BuiltInMiddleware.TimeoutMiddleware(
                _executionTimeout.Value,
                services.BuildServiceProvider().GetRequiredService<ILogger<BuiltInMiddleware.TimeoutMiddleware>>()));
        }

        foreach (var middleware in _customMiddleware)
        {
            pipeline.Use(middleware);
        }

        services.AddSingleton(new CommandsConfiguration
        {
            Prefix = _prefix,
            CaseSensitive = _caseSensitive,
            ExecutionTimeout = _executionTimeout
        });
    }
}
