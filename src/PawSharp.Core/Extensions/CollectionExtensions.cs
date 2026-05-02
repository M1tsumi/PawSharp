namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for collections and dictionaries.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Gets a value from a dictionary, or null if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type (must be a reference type).</typeparam>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value if found, otherwise null.</returns>
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : class
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Gets a value from a dictionary, or a specified default value if the key doesn't exist.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key doesn't exist.</param>
    /// <returns>The value if found, otherwise the default value.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets a value from a snowflake-keyed dictionary, or a default value if the key doesn't exist.
    /// Useful for Discord entities indexed by snowflake IDs.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="dict">The dictionary with snowflake keys.</param>
    /// <param name="id">The snowflake ID to look up.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value if found, otherwise the default value.</returns>
    public static TValue GetSnowflakeValue<TValue>(this IDictionary<ulong, TValue> dict, ulong id, TValue defaultValue)
    {
        return dict.TryGetValue(id, out var value) ? value : defaultValue;
    }
}
