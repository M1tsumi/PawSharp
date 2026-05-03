#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using InteractionOption = PawSharp.Gateway.Events.ApplicationCommandInteractionDataOption;
using PawSharp.Core.Enums;
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

        // Unwrap SubCommand (type 1) and SubCommandGroup (type 2) layers so that
        // options nested inside groups or subcommands are found correctly.
        while (options.Count >= 1 && options[0].Type is 1 or 2 && options[0].Options is { } nested)
            options = nested;

        return GetOptionValueFromList<T>(options, name);
    }

    /// <summary>
    /// Returns the name of the invoked subcommand (type 1), drilling through a
    /// subcommand group (type 2) when present, or <c>null</c> if no subcommand
    /// was used.
    /// </summary>
    public static string? GetSubcommandName(this InteractionCreateEvent interaction)
    {
        var options = interaction.Data?.Options;
        if (options is null || options.Count == 0) return null;

        var first = options[0];
        // SubCommandGroup (type 2) wraps the actual subcommand one level deeper.
        if (first.Type == 2 && first.Options is { Count: > 0 })
            first = first.Options[0];

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

    // ── Interaction context ─────────────────────────────────────────────────────

    /// <summary>
    /// Determines the interaction context type based on where the interaction was triggered.
    /// </summary>
    /// <returns>
    /// <see cref="InteractionContextType.Guild"/> if triggered in a server,
    /// <see cref="InteractionContextType.BotDm"/> if in a DM with the bot user,
    /// <see cref="InteractionContextType.PrivateChannel"/> if in a group DM or other private channel.
    /// </returns>
    public static InteractionContextType GetInteractionContext(this InteractionCreateEvent interaction)
    {
        if (interaction.GuildId.HasValue)
            return InteractionContextType.Guild;

        // If there's no guild and the user is present but member is not,
        // it's likely a DM with the bot (BotDm) vs another private channel
        if (interaction.User is not null && interaction.Member is null)
            return InteractionContextType.BotDm;

        return InteractionContextType.PrivateChannel;
    }

    /// <summary>
    /// Checks if the interaction was triggered in a guild/server context.
    /// </summary>
    public static bool IsGuildInteraction(this InteractionCreateEvent interaction)
        => interaction.GuildId.HasValue;

    /// <summary>
    /// Checks if the interaction was triggered in a DM context (either Bot DM or private channel).
    /// </summary>
    public static bool IsDmInteraction(this InteractionCreateEvent interaction)
        => !interaction.GuildId.HasValue;

    // ── Modal value retrieval ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets the value of a text input field from a modal submission by its custom ID.
    /// </summary>
    /// <param name="interaction">The modal submit interaction event.</param>
    /// <param name="customId">The custom ID of the text input field.</param>
    /// <returns>The submitted text value, or null if not found.</returns>
    public static string? GetModalValue(this InteractionCreateEvent interaction, string customId)
    {
        if (interaction.Data?.Components is null) return null;

        foreach (var actionRow in interaction.Data.Components)
        {
            if (actionRow.Components is null) continue;

            foreach (var component in actionRow.Components)
            {
                if (component is PawSharp.Core.Entities.TextInput textInput &&
                    textInput.CustomId == customId)
                {
                    return textInput.Value;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all modal input values as a dictionary keyed by custom ID.
    /// </summary>
    /// <param name="interaction">The modal submit interaction event.</param>
    /// <returns>A dictionary of custom ID to submitted value.</returns>
    public static Dictionary<string, string> GetModalValues(this InteractionCreateEvent interaction)
    {
        var values = new Dictionary<string, string>();

        if (interaction.Data?.Components is null) return values;

        foreach (var actionRow in interaction.Data.Components)
        {
            if (actionRow.Components is null) continue;

            foreach (var component in actionRow.Components)
            {
                if (component is PawSharp.Core.Entities.TextInput textInput)
                {
                    values[textInput.CustomId] = textInput.Value ?? string.Empty;
                }
            }
        }

        return values;
    }

    // ── Component value retrieval ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets the selected values from a select menu component interaction.
    /// </summary>
    /// <param name="interaction">The component interaction event.</param>
    /// <returns>A list of selected string values, or empty list if not found.</returns>
    public static List<string> GetSelectedValues(this InteractionCreateEvent interaction)
    {
        return interaction.Data?.Values ?? new List<string>();
    }

    /// <summary>
    /// Gets the component type from a component interaction.
    /// </summary>
    /// <param name="interaction">The component interaction event.</param>
    /// <returns>The component type as an integer, or null if not found.</returns>
    public static int? GetComponentType(this InteractionCreateEvent interaction)
    {
        return interaction.Data?.ComponentType;
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
