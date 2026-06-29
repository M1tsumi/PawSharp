#nullable enable
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// Sets up PawSharp using a bot token with optional fluent builder configuration.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="token">The Discord bot token.</param>
    /// <param name="configure">Optional delegate to configure the <see cref="PawSharpClientBuilder"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection SetupPawSharp(
        this IServiceCollection services,
        string token,
        Action<PawSharpClientBuilder>? configure = null)
    {
        var builder = new PawSharpClientBuilder().WithToken(token);
        configure?.Invoke(builder);
        var client = builder.Build();
        services.AddSingleton<IDiscordClient>(client);
        services.AddSingleton((DiscordClient)client);
        return services;
    }

    /// <summary>
    /// Sets up PawSharp by binding <see cref="PawSharpOptions"/> from configuration and optionally
    /// applying additional fluent builder configuration.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The configuration source to bind <see cref="PawSharpOptions"/> from.</param>
    /// <param name="configure">Optional delegate to configure the <see cref="PawSharpClientBuilder"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection SetupPawSharp(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<PawSharpClientBuilder>? configure = null)
    {
        var options = new PawSharpOptions();
        configuration.Bind(options);
        var builder = new PawSharpClientBuilder();
        ApplyOptions(builder, options);
        configure?.Invoke(builder);
        var client = builder.Build();
        services.AddSingleton<IDiscordClient>(client);
        services.AddSingleton((DiscordClient)client);
        return services;
    }

    /// <summary>
    /// Binds <see cref="PawSharpOptions"/> from the provided <see cref="IConfigurationSection"/>
    /// using the standard <c>IOptions&lt;T&gt;</c> pattern.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="section">The configuration section to bind options from.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection ConfigurePawSharp(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        services.Configure<PawSharpOptions>(section);
        return services;
    }

    private static void ApplyOptions(PawSharpClientBuilder builder, PawSharpOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Token))
            builder.WithToken(options.Token);

        builder.WithIntents(options.Intents);
        builder.WithApiVersion(options.ApiVersion);

        if (options.Shards != 1 || options.ShardCount != 1)
            builder.WithSharding(options.Shards, options.ShardCount);

        if (options.EnableCompression)
            builder.UseCompression();

        if (options.Presence != null)
            builder.WithPresence(options.Presence.ActivityName, options.Presence.ActivityType, options.Presence.Status, options.Presence.StreamUrl);
    }

    /// <summary>
    /// Registers all PawSharp services — REST client, gateway, cache, interaction handler, and <see cref="DiscordClient"/> —
    /// with the supplied service collection using the provided options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="options">Bot configuration (token, intents, etc.).</param>
    /// <param name="cacheFactory">
    /// Optional factory for supplying a custom <see cref="IEntityCache"/> implementation.
    /// When <c>null</c>, an in-memory <see cref="MemoryCacheProvider"/> is registered automatically.
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

        // Cache defaults to the in-memory provider unless a custom cache is supplied.
        services.AddSingleton<IEntityCache>(sp => cacheFactory?.Invoke(sp) ?? new MemoryCacheProvider(
            logger: sp.GetService<ILogger<MemoryCacheProvider>>()));

        // Interaction handler
        services.AddSingleton<InteractionHandler>(sp =>
            new InteractionHandler(
                sp.GetRequiredService<IDiscordRestClient>(),
                sp.GetService<ILogger<InteractionHandler>>()));

        // Top-level Discord client
        services.AddSingleton<IDiscordClient>(sp =>
            new DiscordClient(
                sp.GetRequiredService<PawSharpOptions>(),
                sp.GetRequiredService<IEntityCache>(),
                sp.GetRequiredService<ILogger<DiscordClient>>(),
                sp.GetRequiredService<IDiscordRestClient>(),
                sp.GetRequiredService<IGatewayClient>()));
        services.AddSingleton<DiscordClient>(sp =>
            (DiscordClient)sp.GetRequiredService<IDiscordClient>());

        return services;
    }

    /// <summary>
    /// Registers all PawSharp services with an in-memory entity cache.
    /// </summary>
    public static IServiceCollection AddPawSharpWithMemoryCache(
        this IServiceCollection services,
        PawSharpOptions options)
        => services.AddPawSharp(options, _ => new MemoryCacheProvider());
}
