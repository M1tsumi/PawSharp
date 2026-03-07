#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PawSharp.Core.Serialization;

/// <summary>
/// <see cref="JsonConverterFactory"/> for <c>Dictionary&lt;ulong, TValue&gt;</c> where
/// the JSON object has Discord snowflake string keys
/// (e.g. <c>"1439300942167146508": {...}</c>).
/// </summary>
/// <remarks>
/// Discord sends resolved-data maps and similar objects with snowflake IDs as JSON string
/// keys to preserve precision in JavaScript.  Apply this factory via
/// <c>[JsonConverter(typeof(SnowflakeDictionaryJsonConverterFactory))]</c> on any
/// <c>Dictionary&lt;ulong, T&gt;</c> property to get transparent ulong keying.
/// </remarks>
public class SnowflakeDictionaryJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>)) return false;
        return typeToConvert.GetGenericArguments()[0] == typeof(ulong);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[1];
        var converterType = typeof(SnowflakeDictionaryJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}

/// <summary>
/// JSON converter for <c>Dictionary&lt;ulong, TValue&gt;</c> where JSON keys are
/// Discord snowflake strings.
/// </summary>
internal sealed class SnowflakeDictionaryJsonConverter<TValue> : JsonConverter<Dictionary<ulong, TValue>>
{
    public override Dictionary<ulong, TValue>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject, got {reader.TokenType}.");

        var result = new Dictionary<ulong, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName, got {reader.TokenType}.");

            var key = reader.GetString()!;
            reader.Read();

            var value = JsonSerializer.Deserialize<TValue>(ref reader, options);
            if (ulong.TryParse(key, out var id) && value is not null)
                result[id] = value;
        }

        throw new JsonException("Unexpected end of JSON object.");
    }

    public override void Write(
        Utf8JsonWriter writer, Dictionary<ulong, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (k, v) in value)
        {
            writer.WritePropertyName(k.ToString());
            JsonSerializer.Serialize(writer, v, options);
        }
        writer.WriteEndObject();
    }
}
