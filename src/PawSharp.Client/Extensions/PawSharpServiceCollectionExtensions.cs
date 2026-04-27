#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.API.RateLimit;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Providers;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Interactions;

namespace PawSharp.Client.Extensions;

/// <summary>
/// Convenience extensions for registering PawSharp services with .NET's dependency injection container.
/// </summary>
public static class PawSharpServiceCollectionExtensions
{
    /// <summary>
    /// Preferred one-call setup for PawSharp with safe defaults.
    /// Registers all core PawSharp services and an in-memory entity cache.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">Bot configuration (token, intents, etc.).</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection SetupPawSharp(
        this IServiceCollection services,
        PawSharpOptions options)
        => services.AddPawSharpWithMemoryCache(options);

    /// <summary>
    /// Registers all PawSharp services — REST client, gateway, cache, interaction handler, and <see cref="DiscordClient"/> —
    /// with the supplied service collection using the provided options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">Bot configuration (token, intents, etc.).</param>
    /// <param name="cacheFactory">
    /// Optional factory for supplying a custom <see cref="IEntityCache"/> implementation.
    /// When <c>null</c>, the in-memory provider from <c>PawSharp.Cache</c> is expected to be
    /// registered separately (e.g. via <c>services.AddMemoryCache()</c>).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddPawSharp(
        this IServiceCollection services,
        PawSharpOptions options,
        Func<IServiceProvider, IEntityCache>? cacheFactory = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        // Register options as a singleton so downstream services can inject them
        services.AddSingleton(options);

        // HTTP client for REST
        services.AddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient();
            return client;
        });

        // Rate limiter
        services.AddSingleton<IAdvancedRateLimiter>(_ => new AdvancedRateLimiter());

        // REST client
        services.AddSingleton<IDiscordRestClient>(sp =>
            new DiscordRestClient(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<ILogger<DiscordRestClient>>(),
                sp.GetRequiredService<IAdvancedRateLimiter>()));

        // Gateway client
        services.AddSingleton<IGatewayClient>(sp =>
            new GatewayClient(
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<ILogger<GatewayClient>>()));

        // Cache — use factory if provided, otherwise expect the consumer to register IEntityCache themselves
        if (cacheFactory != null)
        {
            services.AddSingleton<IEntityCache>(cacheFactory);
        }

        // Interaction handler
        services.AddSingleton<InteractionHandler>(sp =>
            new InteractionHandler(
                sp.GetRequiredService<IDiscordRestClient>(),
                sp.GetService<ILogger<InteractionHandler>>()));

        // Top-level Discord client
        services.AddSingleton<DiscordClient>(sp =>
            new DiscordClient(
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<IEntityCache>(),
                sp.GetRequiredService<ILogger<DiscordClient>>(),
                sp.GetRequiredService<IDiscordRestClient>(),
                sp.GetRequiredService<IGatewayClient>()));

        return services;
    }

    /// <summary>
    /// Registers all PawSharp services with an in-memory entity cache.
    /// </summary>
    public static IServiceCollection AddPawSharpWithMemoryCache(
        this IServiceCollection services,
        PawSharpOptions options)
        => services.AddPawSharp(options, _ => new MemoryCacheProvider());

    /// <summary>
    /// Backward-compatible alias that registers PawSharp with in-memory cache.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">Bot configuration (token, intents, etc.).</param>
    [Obsolete("Use SetupPawSharp(options) or AddPawSharpWithMemoryCache(options) instead.")]
    public static IServiceCollection AddPawSharpClient(
        this IServiceCollection services,
        PawSharpOptions options)
        => services.SetupPawSharp(options);

    /// <summary>
    /// Backward-compatible overload that uses an already-registered <see cref="PawSharpOptions"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no concrete <see cref="PawSharpOptions"/> instance has been registered in the service collection.
    /// </exception>
    [Obsolete("Use SetupPawSharp(options) or AddPawSharpWithMemoryCache(options) instead.")]
    public static IServiceCollection AddPawSharpClient(this IServiceCollection services)
    {
        var options = services
            .LastOrDefault(d => d.ServiceType == typeof(PawSharpOptions))
            ?.ImplementationInstance as PawSharpOptions;

        if (options == null)
        {
            throw new InvalidOperationException(
                "AddPawSharpClient() requires a concrete PawSharpOptions instance to be registered first. " +
                "Call services.SetupPawSharp(options) or services.AddPawSharpWithMemoryCache(options) instead.");
        }

        return services.SetupPawSharp(options);
    }
}
