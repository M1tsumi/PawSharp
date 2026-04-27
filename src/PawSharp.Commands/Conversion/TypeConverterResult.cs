#nullable enable

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Represents the result of a type conversion operation.
/// </summary>
/// <typeparam name="T">The target type.</typeparam>
public sealed class TypeConverterResult<T>
{
    /// <summary>
    /// Gets whether the conversion was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the converted value when successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message when the conversion failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private TypeConverterResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful conversion result.
    /// </summary>
    /// <param name="value">The converted value.</param>
    /// <returns>A successful result.</returns>
    public static TypeConverterResult<T> FromSuccess(T value)
    {
        return new TypeConverterResult<T>(true, value, null);
    }

    /// <summary>
    /// Creates a failed conversion result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static TypeConverterResult<T> FromError(string errorMessage)
    {
        return new TypeConverterResult<T>(false, default, errorMessage);
    }
}
