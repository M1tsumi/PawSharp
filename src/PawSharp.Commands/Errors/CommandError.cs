#nullable enable
using System;

namespace PawSharp.Commands.Errors;

/// <summary>
/// Represents a command execution error with structured information.
/// </summary>
public class CommandError
{
    /// <summary>Gets the error code.</summary>
    public string Code { get; }
    
    /// <summary>Gets the user-friendly error message.</summary>
    public string Message { get; }
    
    /// <summary>Gets the underlying exception, if any.</summary>
    public Exception? Exception { get; }
    
    /// <summary>Gets additional error details.</summary>
    public object? Details { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandError"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The user-friendly message.</param>
    /// <param name="exception">The underlying exception.</param>
    /// <param name="details">Additional details.</param>
    public CommandError(string code, string message, Exception? exception = null, object? details = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
        Details = details;
    }
    
    /// <summary>
    /// Creates a command not found error.
    /// </summary>
    public static CommandError CommandNotFound(string commandName)
        => new CommandError(CommandErrorCodes.CommandNotFound, $"Command '{commandName}' not found.");
    
    /// <summary>
    /// Creates an argument parse error.
    /// </summary>
    public static CommandError ArgumentParseFailed(string reason)
        => new CommandError(CommandErrorCodes.ArgumentParseFailed, $"Failed to parse arguments: {reason}");
    
    /// <summary>
    /// Creates a type conversion error.
    /// </summary>
    public static CommandError TypeConversionFailed(string typeName, string value)
        => new CommandError(CommandErrorCodes.TypeConversionFailed, $"Unable to convert '{value}' to {typeName}.");
    
    /// <summary>
    /// Creates a precondition failed error.
    /// </summary>
    public static CommandError PreconditionFailed(string reason)
        => new CommandError(CommandErrorCodes.PreconditionFailed, reason);
    
    /// <summary>
    /// Creates an insufficient permissions error.
    /// </summary>
    public static CommandError InsufficientPermissions()
        => new CommandError(CommandErrorCodes.InsufficientPermissions, "You don't have permission to use this command.");
    
    /// <summary>
    /// Creates a cooldown error.
    /// </summary>
    public static CommandError CooldownActive(TimeSpan remaining)
        => new CommandError(CommandErrorCodes.CooldownActive, 
            $"This command is on cooldown. Try again in {remaining.TotalSeconds:F1} seconds.",
            details: remaining);
    
    /// <summary>
    /// Creates a guild-only error.
    /// </summary>
    public static CommandError GuildOnly()
        => new CommandError(CommandErrorCodes.GuildOnly, "This command can only be used in a server.");
    
    /// <summary>
    /// Creates a DM-only error.
    /// </summary>
    public static CommandError DmOnly()
        => new CommandError(CommandErrorCodes.DmOnly, "This command can only be used in direct messages.");
    
    /// <summary>
    /// Creates an NSFW-only error.
    /// </summary>
    public static CommandError NsfwOnly()
        => new CommandError(CommandErrorCodes.NsfwOnly, "This command can only be used in NSFW channels.");
    
    /// <summary>
    /// Creates a not owner error.
    /// </summary>
    public static CommandError NotOwner()
        => new CommandError(CommandErrorCodes.NotOwner, "Only the bot owner can use this command.");
    
    /// <summary>
    /// Creates a missing role error.
    /// </summary>
    public static CommandError MissingRole()
        => new CommandError(CommandErrorCodes.MissingRole, "You don't have the required role to use this command.");
    
    /// <summary>
    /// Creates an execution timeout error.
    /// </summary>
    public static CommandError ExecutionTimeout(TimeSpan timeout)
        => new CommandError(CommandErrorCodes.ExecutionTimeout, 
            $"Command execution timed out after {timeout.TotalSeconds:F1} seconds.");
    
    /// <summary>
    /// Creates an unexpected error.
    /// </summary>
    public static CommandError UnexpectedError(Exception exception)
        => new CommandError(CommandErrorCodes.UnexpectedError, 
            "An unexpected error occurred while executing the command.", exception);
}
