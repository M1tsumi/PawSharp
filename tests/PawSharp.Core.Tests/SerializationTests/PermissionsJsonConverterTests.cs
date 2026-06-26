#nullable enable
using System.Text.Json;
using FluentAssertions;
using PawSharp.Core.Enums;
using PawSharp.Core.Serialization;
using Xunit;

namespace PawSharp.Core.Tests.SerializationTests;

public class PermissionsJsonConverterTests
{
    [Fact]
    public void Read_StringValue_ReturnsPermissions()
    {
        var json = "\"8\"";
        var options = new JsonSerializerOptions { Converters = { new PermissionsJsonConverter() } };
        var result = JsonSerializer.Deserialize<Permissions>(json, options);
        result.Should().Be((Permissions)8);
    }

    [Fact]
    public void Read_NumberValue_ReturnsPermissions()
    {
        var json = "8";
        var options = new JsonSerializerOptions { Converters = { new PermissionsJsonConverter() } };
        var result = JsonSerializer.Deserialize<Permissions>(json, options);
        result.Should().Be((Permissions)8);
    }

    [Fact]
    public void Read_EmptyString_ReturnsNone()
    {
        var json = "\"\"";
        var options = new JsonSerializerOptions { Converters = { new PermissionsJsonConverter() } };
        var result = JsonSerializer.Deserialize<Permissions>(json, options);
        result.Should().Be(Permissions.None);
    }

    [Fact]
    public void Write_WritesStringValue()
    {
        var options = new JsonSerializerOptions { Converters = { new PermissionsJsonConverter() } };
        var json = JsonSerializer.Serialize((Permissions)8, options);
        json.Should().Be("\"8\"");
    }
}

public class NullablePermissionsJsonConverterTests
{
    [Fact]
    public void Read_Null_ReturnsNull()
    {
        var options = new JsonSerializerOptions { Converters = { new NullablePermissionsJsonConverter() } };
        var result = JsonSerializer.Deserialize<Permissions?>("null", options);
        result.Should().BeNull();
    }

    [Fact]
    public void Read_EmptyString_ReturnsNone()
    {
        var options = new JsonSerializerOptions { Converters = { new NullablePermissionsJsonConverter() } };
        var result = JsonSerializer.Deserialize<Permissions?>("\"\"", options);
        result.Should().Be(Permissions.None);
    }

    [Fact]
    public void Write_Value_WritesString()
    {
        var options = new JsonSerializerOptions { Converters = { new NullablePermissionsJsonConverter() } };
        var json = JsonSerializer.Serialize<Permissions?>((Permissions)8, options);
        json.Should().Be("\"8\"");
    }

    [Fact]
    public void Write_Null_WritesNull()
    {
        var options = new JsonSerializerOptions { Converters = { new NullablePermissionsJsonConverter() } };
        var json = JsonSerializer.Serialize<Permissions?>(null, options);
        json.Should().Be("null");
    }
}
