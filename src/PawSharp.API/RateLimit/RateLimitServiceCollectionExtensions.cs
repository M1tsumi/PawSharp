#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace PawSharp.API.RateLimit;

/// <summary>
/// Service collection helpers for rate limiting.
/// </summary>
public static class RateLimitServiceCollectionExtensions
{
    /// <summary>
    /// Registers the advanced rate limiter implementation.
    /// </summary>
    public static IServiceCollection AddAdvancedRateLimiter(this IServiceCollection services)
    {
        services.AddSingleton<IAdvancedRateLimiter, AdvancedRateLimiter>();
        return services;
    }
}
