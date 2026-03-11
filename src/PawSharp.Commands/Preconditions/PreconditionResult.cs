#nullable enable

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// The result of a precondition check.
/// </summary>
public sealed class PreconditionResult
{
    /// <summary>
    /// Gets whether the precondition passed.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message when <see cref="IsSuccess"/> is <see langword="false"/>; otherwise <see langword="null"/>.
    /// </summary>
    public string? ErrorMessage { get; }

    private PreconditionResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess    = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Returns a successful result.
    /// </summary>
    public static PreconditionResult FromSuccess() => new(true, null);

    /// <summary>
    /// Returns a failed result with <paramref name="errorMessage"/>.
    /// </summary>
    public static PreconditionResult FromError(string errorMessage) => new(false, errorMessage);

    /// <inheritdoc/>
    public override string ToString()
        => IsSuccess ? "Success" : $"Failure: {ErrorMessage}";
}
