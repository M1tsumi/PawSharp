using System;
using System.Collections.Generic;
using System.Linq;
using PawSharp.Core.Enums;
using PawSharp.Core.Events;
using PawSharp.Gateway.Events;

namespace PawSharp.Client;

/// <summary>
/// Extension methods for <see cref="DiscordClient"/> to support event interest validation.
/// </summary>
/// <remarks>
/// These extensions provide ergonomic APIs for validating that handlers' required intents
/// are enabled before connecting to the gateway, catching configuration bugs early.
/// </remarks>
public static class DiscordClientIntentValidationExtensions
{
    /// <summary>
    /// Validates that all registered gateway event handlers have their required intents enabled.
    /// Logs warnings for any mismatches and collects detailed validation results.
    /// </summary>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// var client = new PawSharpClientBuilder()
    ///     .WithToken(token)
    ///     .WithIntents(GatewayIntents.AllNonPrivileged)
    ///     .Build();
    /// 
    /// // Register handlers...
    /// client.OnMessageCreated(async msg => { ... });
    /// 
    /// // Validate intent configuration
    /// var enabledIntents = GatewayIntents.AllNonPrivileged;
    /// var issues = client.ValidateIntents(enabledIntents);
    /// if (!issues.IsValid)
    /// {
    ///     foreach (var (eventType, required, missing) in issues.Issues)
    ///         Console.WriteLine($"{eventType} needs {missing}");
    /// }
    /// 
    /// await client.ConnectAsync();
    /// </code>
    /// </remarks>
    /// <param name="client">The Discord client</param>
    /// <param name="enabledIntents">The intents that are currently enabled</param>
    /// <returns>A summary of intent validation issues (empty if all valid)</returns>
    public static IntentValidationResult ValidateIntents(
        this DiscordClient client,
        GatewayIntents enabledIntents)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var result = new IntentValidationResult(enabledIntents);

        // Collect handler intent requirements in the gateway
        if (client.Gateway?.Events != null)
        {
            // Collect detailed results
            var registeredIntents = client.Gateway.Events.GetRegisteredIntents();
            foreach (var (eventType, requiredIntents) in registeredIntents)
            {
                var missing = requiredIntents & ~enabledIntents;
                if (missing != (GatewayIntents)0)
                {
                    result.AddMissingIntent(eventType, requiredIntents, missing);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the recommended set of intents based on all currently registered handlers.
    /// Useful for suggesting minimal working intent configuration.
    /// </summary>
    /// <remarks>
    /// Returns only the intents needed for registered handlers, excluding extra intents.
    /// This is useful when you want to determine the minimal permission set your bot needs.
    /// </remarks>
    /// <param name="client">The Discord client</param>
    /// <returns>Recommended intents, or (GatewayIntents)0 if no handlers are registered</returns>
    public static GatewayIntents GetRecommendedIntents(this DiscordClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        return client.Gateway?.Events?.GetRecommendedIntents() ?? (GatewayIntents)0;
    }

    /// <summary>
    /// Logs a summary of which intents are currently enabled and which are recommended.
    /// Useful for debugging intent configuration at startup.
    /// </summary>
    /// <param name="client">The Discord client</param>
    /// <param name="enabledIntents">The intents that are currently enabled</param>
    public static void LogIntentSummary(this DiscordClient client, GatewayIntents enabledIntents)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var recommended = client.GetRecommendedIntents();
        var registeredIntents = client.Gateway?.Events?.GetRegisteredIntents() ?? 
            new List<(string, GatewayIntents)>();

        var message = $"Intent configuration: enabled={enabledIntents}, recommended={recommended}";
        Console.WriteLine($"[INFO] {message}");

        if (registeredIntents.Count > 0)
        {
            Console.WriteLine($"[INFO] Registered {registeredIntents.Count} event type(s) with intent requirements:");
            foreach (var (eventType, requiredIntents) in registeredIntents.OrderBy(x => x.Item1))
            {
                var isMissing = (requiredIntents & ~enabledIntents) != (GatewayIntents)0;
                var status = isMissing ? "[WARN]" : "[OK]";
                Console.WriteLine($"  {status} {eventType}: {requiredIntents}");
            }
        }
    }
}

/// <summary>
/// Result of an intent validation check, containing summary of any mismatches.
/// </summary>
public sealed class IntentValidationResult
{
    private readonly List<(string EventType, GatewayIntents Required, GatewayIntents Missing)> _issues =
        new();

    /// <summary>
    /// The intents that were enabled during validation.
    /// </summary>
    public GatewayIntents EnabledIntents { get; }

    /// <summary>
    /// Number of issues found (events with missing intents).
    /// </summary>
    public int Count => _issues.Count;

    /// <summary>
    /// True if validation passed (no issues found).
    /// </summary>
    public bool IsValid => Count == 0;

    /// <summary>
    /// Gets a summary of all issues found.
    /// </summary>
    public IReadOnlyList<(string EventType, GatewayIntents Required, GatewayIntents Missing)> Issues =>
        _issues.AsReadOnly();

    internal IntentValidationResult(GatewayIntents enabledIntents)
    {
        EnabledIntents = enabledIntents;
    }

    internal void AddMissingIntent(string eventType, GatewayIntents required, GatewayIntents missing)
    {
        _issues.Add((eventType, required, missing));
    }

    /// <summary>
    /// Gets a human-readable summary of validation results.
    /// </summary>
    public override string ToString()
    {
        if (IsValid)
        {
            return $"Intents valid: {EnabledIntents}";
        }

        var summary = $"Intent validation failed ({Count} issue{(Count != 1 ? "s" : "")}):";
        return summary + " " + string.Join("; ", Issues.Select(i => $"{i.EventType} needs {i.Missing}"));
    }
}
