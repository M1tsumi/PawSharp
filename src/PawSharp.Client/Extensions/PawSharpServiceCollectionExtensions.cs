#nullable enable
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.API.RateLimit;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Models;
using PawSharp.Gateway;
using PawSharp.Interactions;

using PawSharp.Cache.Providers;

namespace PawSharp.Client.Extensions;

/// <summary>
/// Convenience extensions for registering PawSharp services with .NET's dependency injection container.
/// </summary>
public static class PawSharpServiceCollectionExtensions
{
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
        services.TryAddSingleton(options);

        // HTTP client for REST
        services.TryAddSingleton<HttpClient>(_ =>
        {
            var client = new HttpClient();
            return client;
        });

        // Rate limiter
        services.TryAddSingleton<IAdvancedRateLimiter>(_ => new AdvancedRateLimiter());

        // REST client
        services.TryAddSingleton<IDiscordRestClient>(sp =>
            new DiscordRestClient(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<ILogger<DiscordRestClient>>(),
                sp.GetRequiredService<IAdvancedRateLimiter>()));

        // Gateway client
        services.TryAddSingleton<IGatewayClient>(sp =>
            new GatewayClient(
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<ILogger<GatewayClient>>()));

        // Cache — use factory if provided, otherwise expect the consumer to register IEntityCache themselves
        if (cacheFactory != null)
        {
            services.TryAddSingleton<IEntityCache>(cacheFactory);
        }

        // Interaction handler
        services.TryAddSingleton<InteractionHandler>(sp =>
            new InteractionHandler(sp.GetRequiredService<IDiscordRestClient>()));

        // Top-level Discord client
        services.TryAddSingleton<DiscordClient>(sp =>
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
    /// Equivalent to calling <see cref="AddPawSharp(IServiceCollection,PawSharpOptions,Func{IServiceProvider,IEntityCache}?)"/>
    /// with <c>cacheFactory: _ =&gt; new MemoryCacheProvider()</c>.
    /// </summary>
    public static IServiceCollection AddPawSharpWithMemoryCache(
        this IServiceCollection services,
        PawSharpOptions options)
        => services.AddPawSharp(options, _ => new MemoryCacheProvider());
}
