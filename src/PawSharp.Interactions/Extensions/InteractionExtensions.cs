#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using InteractionOption = PawSharp.Gateway.Events.ApplicationCommandInteractionDataOption;
using PawSharp.Gateway.Events;

namespace PawSharp.Interactions.Extensions;

/// <summary>
/// Extension methods to simplify working with Discord interaction option values.
/// </summary>
public static class InteractionExtensions
{
    /// <summary>
    /// Gets the value of a named slash command option, cast to <typeparamref name="T"/>.
    /// Supports <c>string</c>, <c>int</c>, <c>long</c>, <c>double</c>, <c>bool</c>, and <c>ulong</c>.
    /// Automatically walks into the first-level subcommand options when the interaction uses a subcommand.
    /// Returns <c>default</c> if the option is not found or the cast fails.
    /// </summary>
    /// <typeparam name="T">Desired return type.</typeparam>
    /// <param name="interaction">The interaction event.</param>
    /// <param name="name">Option name, case-insensitive.</param>
    public static T? GetOptionValue<T>(this InteractionCreateEvent interaction, string name)
    {
        var options = interaction.Data?.Options;
        if (options is null) return default;

        // If the first option is a SubCommand (type 1) or SubCommandGroup (type 2),
        // the actual user-supplied options are nested inside it.
        if (options.Count == 1 && options[0].Type is 1 or 2)
            options = options[0].Options;

        return GetOptionValueFromList<T>(options, name);
    }

    /// <summary>
    /// Returns the name of the invoked subcommand (type 1) or subcommand group option (type 2),
    /// or <c>null</c> if the interaction has no subcommand.
    /// </summary>
    public static string? GetSubcommandName(this InteractionCreateEvent interaction)
    {
        var options = interaction.Data?.Options;
        if (options is null || options.Count == 0) return null;

        var first = options[0];
        if (first.Type is 1 or 2) return first.Name;
        return null;
    }

    /// <summary>
    /// Gets the value of a named option from an explicit option list, cast to <typeparamref name="T"/>.
    /// Useful for subcommand options.
    /// </summary>
    public static T? GetOptionValue<T>(this IEnumerable<InteractionOption> options, string name)
        => GetOptionValueFromList<T>(options as List<InteractionOption> ?? new List<InteractionOption>(options), name);

    // ── Option finding ────────────────────────────────────────────────────────

    /// <summary>Finds a named option (top-level only).</summary>
    public static InteractionOption? FindOption(this InteractionCreateEvent interaction, string name)
    {
        var options = interaction.Data?.Options;
        if (options is null) return null;
        return FindInList(options, name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static T? GetOptionValueFromList<T>(List<InteractionOption>? options, string name)
    {
        if (options is null) return default;

        var option = FindInList(options, name);
        if (option?.Value is null) return default;

        return ConvertValue<T>(option.Value);
    }

    private static InteractionOption? FindInList(List<InteractionOption> options, string name)
    {
        foreach (var opt in options)
        {
            if (string.Equals(opt.Name, name, StringComparison.OrdinalIgnoreCase))
                return opt;
        }
        return null;
    }

    private static T? ConvertValue<T>(object raw)
    {
        // System.Text.Json deserializes object? fields as JsonElement
        if (raw is JsonElement element)
        {
            try
            {
                Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (target == typeof(string))   return (T?)(object?)element.GetString();
                if (target == typeof(bool))     return (T?)(object?)element.GetBoolean();
                if (target == typeof(int))      return (T?)(object?)(int)element.GetInt64();
                if (target == typeof(long))     return (T?)(object?)element.GetInt64();
                if (target == typeof(ulong))
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                        return (T?)(object?)element.GetUInt64();
                    if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = element.GetString();
                        if (ulong.TryParse(s, out var ulongParsed))
                            return (T?)(object?)ulongParsed;
                    }
                    return default;
                }
                if (target == typeof(double))   return (T?)(object?)element.GetDouble();
                if (target == typeof(float))    return (T?)(object?)(float)element.GetDouble();

                // Generic fallback through JsonSerializer
                return element.Deserialize<T>();
            }
            catch
            {
                return default;
            }
        }

        // Raw CLR value (unit-test mocks etc.)
        if (raw is T direct) return direct;

        try { return (T?)Convert.ChangeType(raw, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)); }
        catch { return default; }
    }
}
