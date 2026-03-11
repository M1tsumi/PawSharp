#nullable enable
using System;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Thrown when a command precondition check fails.
/// Delivered to <see cref="CommandsExtension.CommandErrored"/> so the bot can respond with
/// the specific <see cref="Exception.Message"/> (e.g. "You are on cooldown. Try again in 3.2 second(s).").
/// </summary>
public sealed class PreconditionFailedException : Exception
{
    /// <summary>
    /// Initialises a new instance with the precondition failure description.
    /// </summary>
    public PreconditionFailedException(string message) : base(message) { }
}
