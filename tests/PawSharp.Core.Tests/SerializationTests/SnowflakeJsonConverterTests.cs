#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using PawSharp.Core.Serialization;
using Xunit;

namespace PawSharp.Core.Tests.SerializationTests;

public class SnowflakeJsonConverterTests
{
    [Fact]
    public void Read_StringValue_ReturnsULong()
    {
        var json = "\"123456789\"";
        var result = JsonSerializer.Deserialize<ulong>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeJsonConverter() }
        });
        result.Should().Be(123456789UL);
    }

    [Fact]
    public void Read_NumberValue_ReturnsULong()
    {
        var json = "123456789";
        var result = JsonSerializer.Deserialize<ulong>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeJsonConverter() }
        });
        result.Should().Be(123456789UL);
    }

    [Fact]
    public void Read_InvalidString_ReturnsZero()
    {
        var json = "\"not-a-number\"";
        var result = JsonSerializer.Deserialize<ulong>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeJsonConverter() }
        });
        result.Should().Be(0UL);
    }

    [Fact]
    public void Write_ULong_WritesString()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new SnowflakeJsonConverter() }
        };
        var json = JsonSerializer.Serialize(123456789UL, options);
        json.Should().Be("\"123456789\"");
    }
}

public class NullableSnowflakeJsonConverterTests
{
    [Fact]
    public void Read_Null_ReturnsNull()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<ulong?>(json, new JsonSerializerOptions
        {
            Converters = { new NullableSnowflakeJsonConverter() }
        });
        result.Should().BeNull();
    }

    [Fact]
    public void Read_EmptyString_ReturnsNull()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<ulong?>(json, new JsonSerializerOptions
        {
            Converters = { new NullableSnowflakeJsonConverter() }
        });
        result.Should().BeNull();
    }

    [Fact]
    public void Read_ValidString_ReturnsValue()
    {
        var json = "\"42\"";
        var result = JsonSerializer.Deserialize<ulong?>(json, new JsonSerializerOptions
        {
            Converters = { new NullableSnowflakeJsonConverter() }
        });
        result.Should().Be(42UL);
    }

    [Fact]
    public void Write_Null_WritesNull()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new NullableSnowflakeJsonConverter() }
        };
        var json = JsonSerializer.Serialize<ulong?>(null, options);
        json.Should().Be("null");
    }

    [Fact]
    public void Write_Value_WritesString()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new NullableSnowflakeJsonConverter() }
        };
        var json = JsonSerializer.Serialize<ulong?>(42UL, options);
        json.Should().Be("\"42\"");
    }
}

public class SnowflakeListJsonConverterTests
{
    [Fact]
    public void Read_ArrayOfStrings_ReturnsList()
    {
        var json = "[\"1\",\"2\",\"3\"]";
        var result = JsonSerializer.Deserialize<List<ulong>>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeListJsonConverter() }
        });
        result.Should().BeEquivalentTo(new ulong[] { 1, 2, 3 });
    }

    [Fact]
    public void Read_ArrayOfNumbers_ReturnsList()
    {
        var json = "[1,2,3]";
        var result = JsonSerializer.Deserialize<List<ulong>>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeListJsonConverter() }
        });
        result.Should().BeEquivalentTo(new ulong[] { 1, 2, 3 });
    }

    [Fact]
    public void Read_InvalidString_SkipsEntry()
    {
        var json = "[\"1\",\"abc\",\"3\"]";
        var result = JsonSerializer.Deserialize<List<ulong>>(json, new JsonSerializerOptions
        {
            Converters = { new SnowflakeListJsonConverter() }
        });
        result.Should().BeEquivalentTo(new ulong[] { 1, 3 });
    }

    [Fact]
    public void Write_ListOfULongs_WritesArrayOfStrings()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new SnowflakeListJsonConverter() }
        };
        var json = JsonSerializer.Serialize(new List<ulong> { 1, 2, 3 }, options);
        json.Should().Be("[\"1\",\"2\",\"3\"]");
    }
}
