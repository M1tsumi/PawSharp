namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Entities.InteractionType"/> enum.
/// </summary>
public static class InteractionTypeExtensions
{
    /// <summary>
    /// Checks if the interaction type is an application command (slash command or autocomplete).
    /// </summary>
    /// <param name="type">The interaction type.</param>
    /// <returns>True if the interaction is an application command.</returns>
    public static bool IsApplicationCommand(this Entities.InteractionType type)
    {
        return type is Entities.InteractionType.ApplicationCommand or
               Entities.InteractionType.ApplicationCommandAutocomplete;
    }

    /// <summary>
    /// Checks if the interaction type is a component interaction (button, select menu, etc.).
    /// </summary>
    /// <param name="type">The interaction type.</param>
    /// <returns>True if the interaction is a component interaction.</returns>
    public static bool IsComponent(this Entities.InteractionType type)
    {
        return type == Entities.InteractionType.MessageComponent;
    }

    /// <summary>
    /// Checks if the interaction type is a modal submit.
    /// </summary>
    /// <param name="type">The interaction type.</param>
    /// <returns>True if the interaction is a modal submit.</returns>
    public static bool IsModal(this Entities.InteractionType type)
    {
        return type == Entities.InteractionType.ModalSubmit;
    }

    /// <summary>
    /// Checks if the interaction type is a ping.
    /// </summary>
    /// <param name="type">The interaction type.</param>
    /// <returns>True if the interaction is a ping.</returns>
    public static bool IsPing(this Entities.InteractionType type)
    {
        return type == Entities.InteractionType.Ping;
    }
}
