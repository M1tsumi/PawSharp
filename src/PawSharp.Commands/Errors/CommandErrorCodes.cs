namespace PawSharp.Commands.Errors;

/// <summary>
/// Structured error codes for command execution failures.
/// </summary>
public static class CommandErrorCodes
{
    /// <summary>Command was not found.</summary>
    public const string CommandNotFound = "COMMAND_NOT_FOUND";
    
    /// <summary>Argument parsing failed.</summary>
    public const string ArgumentParseFailed = "ARGUMENT_PARSE_FAILED";
    
    /// <summary>Type conversion failed.</summary>
    public const string TypeConversionFailed = "TYPE_CONVERSION_FAILED";
    
    /// <summary>Precondition check failed.</summary>
    public const string PreconditionFailed = "PRECONDITION_FAILED";
    
    /// <summary>User lacks required permissions.</summary>
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";
    
    /// <summary>Command is on cooldown.</summary>
    public const string CooldownActive = "COOLDOWN_ACTIVE";
    
    /// <summary>Command can only be used in a guild.</summary>
    public const string GuildOnly = "GUILD_ONLY";
    
    /// <summary>Command can only be used in DMs.</summary>
    public const string DmOnly = "DM_ONLY";
    
    /// <summary>Command can only be used in NSFW channels.</summary>
    public const string NsfwOnly = "NSFW_ONLY";
    
    /// <summary>User is not the bot owner.</summary>
    public const string NotOwner = "NOT_OWNER";
    
    /// <summary>User lacks required role.</summary>
    public const string MissingRole = "MISSING_ROLE";
    
    /// <summary>Command execution timed out.</summary>
    public const string ExecutionTimeout = "EXECUTION_TIMEOUT";
    
    /// <summary>Unexpected error during command execution.</summary>
    public const string UnexpectedError = "UNEXPECTED_ERROR";
    
    /// <summary>Invalid argument count.</summary>
    public const string InvalidArgumentCount = "INVALID_ARGUMENT_COUNT";
    
    /// <summary>Required argument is missing.</summary>
    public const string MissingRequiredArgument = "MISSING_REQUIRED_ARGUMENT";
}
