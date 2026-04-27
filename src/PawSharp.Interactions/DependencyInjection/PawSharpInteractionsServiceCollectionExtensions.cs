#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;

namespace PawSharp.Interactions.DependencyInjection;

/// <summary>
/// Extension methods for registering PawSharp.Interactions services.
/// </summary>
public static class PawSharpInteractionsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the InteractionHandler as a singleton service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInteractionHandler(this IServiceCollection services)
    {
        services.AddSingleton<InteractionHandler>(sp =>
        {
            var restClient = sp.GetRequiredService<IDiscordRestClient>();
            var logger = sp.GetService<ILogger<InteractionHandler>>();
            return new InteractionHandler(restClient, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers the InteractionHandler as a singleton service with a custom factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">A factory function to create the InteractionHandler.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInteractionHandler(
        this IServiceCollection services,
        Func<IServiceProvider, InteractionHandler> factory)
    {
        services.AddSingleton(factory);
        return services;
    }
}
