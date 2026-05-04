#nullable enable

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Represents the result of a precondition check.
/// </summary>
public sealed class PreconditionResult
{
    /// <summary>
    /// Gets whether the precondition passed.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the error message if the check failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private PreconditionResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful precondition result indicating the command can proceed.
    /// </summary>
    /// <returns>A successful precondition result.</returns>
    public static PreconditionResult FromSuccess()
    {
        return new PreconditionResult(true, null);
    }

    /// <summary>
    /// Creates a failed precondition result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message describing why the precondition failed.</param>
    /// <returns>A failed precondition result.</returns>
    public static PreconditionResult FromError(string errorMessage)
    {
        return new PreconditionResult(false, errorMessage);
    }

    /// <inheritdoc/>
    public override string ToString()
        => IsSuccess ? "Success" : $"Failure: {ErrorMessage}";
}
