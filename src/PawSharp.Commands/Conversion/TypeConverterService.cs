#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Service for managing and using type converters.
/// This service maintains a registry of type converters and handles the conversion
/// of string command arguments to strongly-typed C# objects.
/// </summary>
public class TypeConverterService
{
    private readonly ConcurrentDictionary<Type, object> _converters = new();
    private readonly ILogger<TypeConverterService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeConverterService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public TypeConverterService(ILogger<TypeConverterService>? logger = null)
    {
        _logger = logger;
        RegisterBuiltInConverters();
    }

    /// <summary>
    /// Registers a type converter for a specific type.
    /// If a converter for the type already exists, it will be replaced.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="converter">The converter instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
    public void RegisterConverter<T>(ITypeConverter<T> converter)
    {
        if (converter == null) throw new ArgumentNullException(nameof(converter));
        _converters[typeof(T)] = converter;
        _logger?.LogDebug("Registered type converter for {Type}", typeof(T).Name);
    }

    /// <summary>
    /// Registers a type converter from an ITypeConverter interface (used for DI).
    /// Uses reflection to determine the target type from the implemented generic interface.
    /// </summary>
    /// <param name="converter">The converter instance implementing ITypeConverter{T}.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
    /// <remarks>
    /// This method is useful when registering converters via dependency injection
    /// where the generic type parameter is not known at compile time.
    /// </remarks>
    public void RegisterConverterFromInterface(ITypeConverter converter)
    {
        if (converter == null) throw new ArgumentNullException(nameof(converter));

        // Find the ITypeConverter<T> interface to determine the target type
        var converterType = converter.GetType();
        var genericInterface = converterType.GetInterface("ITypeConverter`1");

        if (genericInterface != null)
        {
            var targetType = genericInterface.GetGenericArguments()[0];
            _converters[targetType] = converter;
            _logger?.LogDebug("Registered DI type converter for {Type}", targetType.Name);
        }
        else
        {
            _logger?.LogWarning("Converter type {ConverterType} does not implement ITypeConverter<T>", converterType.Name);
        }
    }

    /// <summary>
    /// Registers a converter for a specific enum type.
    /// </summary>
    /// <typeparam name="T">The enum type to register a converter for.</typeparam>
    public void RegisterEnumConverter<T>() where T : struct, Enum
    {
        var converter = new BuiltInConverters.GenericEnumConverter<T>();
        _converters[typeof(T)] = converter;
        _logger?.LogDebug("Registered enum converter for {Type}", typeof(T).Name);
    }

    /// <summary>
    /// Attempts to convert a string value to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The string value to convert.</param>
    /// <param name="context">The command context.</param>
    /// <returns>A conversion result.</returns>
    public async Task<TypeConverterResult<T>> ConvertAsync<T>(string value, CommandContext context)
    {
        var targetType = typeof(T);
        
        if (_converters.TryGetValue(targetType, out var converterObj))
        {
            if (converterObj is ITypeConverter<T> converter)
            {
                return await converter.ConvertAsync(value, context);
            }
            return TypeConverterResult<T>.FromError($"Invalid converter registered for type {targetType.Name}.");
        }

        // Try to find a converter for the underlying type if it's nullable
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null && _converters.TryGetValue(underlyingType, out var underlyingConverterObj))
        {
            // This is a simplified approach - in a full implementation, we'd need to handle the nullable wrapper
            _logger?.LogWarning("No direct converter for nullable type {Type}, but found converter for {UnderlyingType}", targetType.Name, underlyingType.Name);
        }

        return TypeConverterResult<T>.FromError($"No type converter registered for type {targetType.Name}.");
    }

    /// <summary>
    /// Attempts to convert a string value to the specified type using reflection.
    /// </summary>
    /// <param name="targetType">The target type.</param>
    /// <param name="value">The string value to convert.</param>
    /// <param name="context">The command context.</param>
    /// <returns>A conversion result.</returns>
    public async Task<object?> ConvertAsync(Type targetType, string value, CommandContext context)
    {
        // Handle nullable types by unwrapping to the underlying type
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        var method = typeof(TypeConverterService).GetMethod(nameof(ConvertAsync), 1, new[] { typeof(string), typeof(CommandContext) });
        if (method == null)
            return null;

        var genericMethod = method.MakeGenericMethod(underlyingType);
        var result = await (dynamic)genericMethod.Invoke(this, new object[] { value, context });
        
        // Check if conversion was successful
        var isSuccessProp = result?.GetType().GetProperty("IsSuccess");
        if (isSuccessProp != null && (bool)isSuccessProp.GetValue(result) == true)
        {
            var convertedValue = result?.GetType().GetProperty("Value")?.GetValue(result);
            
            // If the original type was nullable, wrap the converted value appropriately
            if (underlyingType != targetType)
            {
                // For nullable reference types, the value is already correct
                // For nullable value types, we need to handle the wrapping
                if (targetType.IsValueType)
                {
                    return convertedValue;
                }
            }
            
            return convertedValue;
        }
        
        // Conversion failed
        return null;
    }

    /// <summary>
    /// Checks if a converter is registered for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True if a converter is registered; otherwise, false.</returns>
    public bool HasConverter<T>()
    {
        return _converters.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Registers all built-in type converters.
    /// </summary>
    private void RegisterBuiltInConverters()
    {
        RegisterConverter<string>(new BuiltInConverters.StringTypeConverter());
        RegisterConverter<int>(new BuiltInConverters.Int32Converter());
        RegisterConverter<long>(new BuiltInConverters.Int64Converter());
        RegisterConverter<ulong>(new BuiltInConverters.UInt64Converter());
        RegisterConverter<bool>(new BuiltInConverters.BooleanConverter());
        RegisterConverter<double>(new BuiltInConverters.DoubleConverter());
        RegisterConverter<float>(new BuiltInConverters.FloatConverter());
        RegisterConverter<decimal>(new BuiltInConverters.DecimalConverter());
        RegisterConverter<DateTime>(new BuiltInConverters.DateTimeConverter());
        RegisterConverter<DateTimeOffset>(new BuiltInConverters.DateTimeOffsetConverter());
        RegisterConverter<TimeSpan>(new BuiltInConverters.TimeSpanConverter());
        RegisterConverter<Guid>(new BuiltInConverters.GuidConverter());
        RegisterConverter<Uri>(new BuiltInConverters.UriConverter());
        RegisterConverter<sbyte>(new BuiltInConverters.SByteConverter());
        RegisterConverter<byte>(new BuiltInConverters.ByteConverter());
        RegisterConverter<short>(new BuiltInConverters.Int16Converter());
        RegisterConverter<ushort>(new BuiltInConverters.UInt16Converter());
        RegisterConverter<uint>(new BuiltInConverters.UInt32Converter());
        RegisterConverter<PawSharp.Core.Entities.User>(new BuiltInConverters.UserConverter());
        RegisterConverter<PawSharp.Core.Entities.Channel>(new BuiltInConverters.ChannelConverter());
        RegisterConverter<PawSharp.Core.Entities.Role>(new BuiltInConverters.RoleConverter());
        RegisterConverter<PawSharp.Core.Entities.GuildMember>(new BuiltInConverters.GuildMemberConverter());
        
        _logger?.LogDebug("Registered {Count} built-in type converters", _converters.Count);
    }
}
