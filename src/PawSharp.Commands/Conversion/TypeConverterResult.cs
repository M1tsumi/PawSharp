#nullable enable

namespace PawSharp.Commands.Conversion;

/// <summary>
/// Represents the result of a type conversion operation.
/// </summary>
/// <typeparam name="T">The target type that was attempted to convert to.</typeparam>
public sealed class TypeConverterResult<T>
{
    /// <summary>
    /// Gets whether the conversion was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the converted value when successful. Contains the default value of T when conversion failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message when the conversion failed. Null when conversion succeeded.
    /// </summary>
    public string? ErrorMessage { get; }

    private TypeConverterResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful conversion result with the converted value.
    /// </summary>
    /// <param name="value">The successfully converted value.</param>
    /// <returns>A successful conversion result.</returns>
    public static TypeConverterResult<T> FromSuccess(T value)
    {
        return new TypeConverterResult<T>(true, value, null);
    }

    /// <summary>
    /// Creates a failed conversion result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the conversion failed.</param>
    /// <returns>A failed conversion result.</returns>
    public static TypeConverterResult<T> FromError(string errorMessage)
    {
        return new TypeConverterResult<T>(false, default, errorMessage);
    }
}
