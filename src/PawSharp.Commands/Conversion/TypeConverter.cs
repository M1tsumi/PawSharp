#nullable enable
using System.Threading.Tasks;

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Marker interface for type converters. Used for DI registration.
/// Implement <see cref="ITypeConverter{T}"/> for actual conversion logic.
/// </summary>
public interface ITypeConverter { }

/// <summary>
/// Defines a type converter for converting string arguments to specific types.
/// Type converters are used by the command framework to automatically convert
/// string command arguments into strongly-typed C# objects.
/// </summary>
/// <typeparam name="T">The target type to convert to.</typeparam>
public interface ITypeConverter<T> : ITypeConverter
{
    /// <summary>
    /// Converts a string argument to the target type.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <param name="context">The command context for the conversion, which can provide access to cache or client.</param>
    /// <returns>A conversion result indicating success or failure with the converted value or error message.</returns>
    Task<TypeConverterResult<T>> ConvertAsync(string value, CommandContext context);
}

/// <summary>
/// Base class for type converters providing common functionality.
/// Inherit from this class to create custom type converters for command arguments.
/// </summary>
/// <typeparam name="T">The target type to convert to.</typeparam>
public abstract class TypeConverter<T> : ITypeConverter<T>
{
    /// <inheritdoc/>
    public abstract Task<TypeConverterResult<T>> ConvertAsync(string value, CommandContext context);
}

/// <summary>
/// Synchronous type converter base class for simple conversions that don't require async operations.
/// Inherit from this class when your conversion logic is synchronous for better performance.
/// </summary>
/// <typeparam name="T">The target type to convert to.</typeparam>
public abstract class SyncTypeConverter<T> : TypeConverter<T>
{
    /// <inheritdoc/>
    public sealed override Task<TypeConverterResult<T>> ConvertAsync(string value, CommandContext context)
    {
        return Task.FromResult(ConvertSync(value, context));
    }

    /// <summary>
    /// Synchronously converts a string argument to the target type.
    /// Implement this method instead of <see cref="ConvertAsync"/> for synchronous conversions.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <param name="context">The command context for the conversion, which can provide access to cache or client.</param>
    /// <returns>A conversion result indicating success or failure with the converted value or error message.</returns>
    protected abstract TypeConverterResult<T> ConvertSync(string value, CommandContext context);
}
