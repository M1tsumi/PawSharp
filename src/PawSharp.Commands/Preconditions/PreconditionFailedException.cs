#nullable enable
using System;

namespace PawSharp.Commands.Preconditions;

/// <summary>
/// Exception thrown when a precondition check fails.
/// This exception is used to surface precondition failures through the
/// <see cref="CommandsExtension.CommandErrored"/> event handler.
/// </summary>
public sealed class PreconditionFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreconditionFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message describing why the precondition failed.</param>
    public PreconditionFailedException(string message) : base(message)
    {
    }
}
