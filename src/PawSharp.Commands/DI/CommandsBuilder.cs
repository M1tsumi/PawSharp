#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PawSharp.Commands.Conversion;
using PawSharp.Commands.Middleware;
using PawSharp.Client;

namespace PawSharp.Commands.DependencyInjection;

/// <summary>
/// Builder for configuring PawSharp.Commands with dependency injection.
/// </summary>
public class CommandsBuilder
{
    private readonly IServiceCollection _services;
    private readonly CommandsOptions _options = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandsBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public CommandsBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Sets the command prefix.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithPrefix(string prefix)
    {
        _options.Prefix = prefix;
        return this;
    }

    /// <summary>
    /// Sets whether commands are case-sensitive.
    /// </summary>
    /// <param name="caseSensitive">Whether case-sensitive.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithCaseSensitivity(bool caseSensitive)
    {
        _options.CaseSensitive = caseSensitive;
        return this;
    }

    /// <summary>
    /// Sets the command execution timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithExecutionTimeout(TimeSpan timeout)
    {
        _options.ExecutionTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables built-in logging middleware.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithLoggingMiddleware()
    {
        _options.EnableLoggingMiddleware = true;
        return this;
    }

    /// <summary>
    /// Enables built-in audit middleware.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithAuditMiddleware()
    {
        _options.EnableAuditMiddleware = true;
        return this;
    }

    /// <summary>
    /// Adds a custom middleware to the pipeline.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithMiddleware<TMiddleware>()
        where TMiddleware : class, IMiddleware
    {
        _services.AddScoped<TMiddleware>();
        return this;
    }

    /// <summary>
    /// Registers a custom type converter.
    /// </summary>
    /// <typeparam name="TConverter">The converter type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public CommandsBuilder WithTypeConverter<TConverter>()
        where TConverter : class, ITypeConverter
    {
        _services.AddSingleton<ITypeConverter, TConverter>();
        return this;
    }

    /// <summary>
    /// Builds the commands extension with the configured services.
    /// </summary>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection Build()
    {
        _services.AddSingleton(_options);
        _services.AddSingleton<MiddlewarePipeline>();

        // Register TypeConverterService with DI-registered converters
        _services.AddSingleton<TypeConverterService>(sp =>
        {
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<TypeConverterService>>();
            var service = new TypeConverterService(logger);

            // Get all DI-registered custom type converters
            var customConverters = sp.GetServices<ITypeConverter>();
            foreach (var converter in customConverters)
            {
                service.RegisterConverterFromInterface(converter);
            }

            return service;
        });

        if (_options.EnableLoggingMiddleware)
        {
            _services.AddCommandMiddleware<Middleware.BuiltInMiddleware.LoggingMiddleware>();
        }

        if (_options.EnableAuditMiddleware)
        {
            _services.AddCommandMiddleware<Middleware.BuiltInMiddleware.AuditMiddleware>();
        }

        if (_options.ExecutionTimeout > TimeSpan.Zero)
        {
            _services.AddSingleton<IMiddleware>(sp => new Middleware.BuiltInMiddleware.TimeoutMiddleware(
                _options.ExecutionTimeout,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Middleware.BuiltInMiddleware.TimeoutMiddleware>>()));
        }

        return _services;
    }
}
