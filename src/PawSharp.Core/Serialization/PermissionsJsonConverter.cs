using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PawSharp.Core.Enums;

namespace PawSharp.Core.Serialization;

/// <summary>
/// JSON converter for Discord permission bitfields.
/// Discord sends permissions as strings (e.g., "8") but we want to use the Permissions enum.
/// </summary>
public class PermissionsJsonConverter : JsonConverter<Permissions>
{
    public override Permissions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return Permissions.None;
            
            if (ulong.TryParse(value, out var ulongValue))
                return (Permissions)ulongValue;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return (Permissions)reader.GetUInt64();
        }
        
        return Permissions.None;
    }

    public override void Write(Utf8JsonWriter writer, Permissions value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(((ulong)value).ToString());
    }
}

/// <summary>
/// JSON converter for nullable Discord permission bitfields.
/// </summary>
public class NullablePermissionsJsonConverter : JsonConverter<Permissions?>
{
    public override Permissions? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return Permissions.None;
            
            if (ulong.TryParse(value, out var ulongValue))
                return (Permissions)ulongValue;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return (Permissions)reader.GetUInt64();
        }
        
        return Permissions.None;
    }

    public override void Write(Utf8JsonWriter writer, Permissions? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(((ulong)value.Value).ToString());
        else
            writer.WriteNullValue();
    }
}
