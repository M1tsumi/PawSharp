#nullable enable
using System;

namespace PawSharp.API.Models;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value if the operation succeeded; otherwise, throws an exception.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException("Cannot access Value of a failed result. Check IsSuccess before accessing.");
            return _value!;
        }
    }

    /// <summary>
    /// Gets the error if the operation failed; otherwise, null.
    /// </summary>
    public Error? Error { get; }

    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return new Result<T>(true, value, null);
    }

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result<T> Failure(Error error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));
        return new Result<T>(false, default, error);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result<T> Failure(string message)
    {
        return Failure(new Error(message));
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Represents an error with a message and optional exception.
/// </summary>
public sealed class Error
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the associated exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the HTTP status code, if applicable.
    /// </summary>
    public int? StatusCode { get; }

    public Error(string message, Exception? exception = null, int? statusCode = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail with an error.
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the operation failed; otherwise, null.
    /// </summary>
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new Result(true, null);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result Failure(string message) => Failure(new Error(message));
}
