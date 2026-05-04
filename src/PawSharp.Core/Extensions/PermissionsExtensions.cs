#nullable enable
using PawSharp.Core.Enums;

namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Permissions"/> enum.
/// </summary>
public static class PermissionsExtensions
{
    /// <summary>
    /// Checks if the source has all of the specified permissions.
    /// </summary>
    /// <param name="source">The source permissions.</param>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the source has the permission.</returns>
    /// <example>
    /// <code>
    /// Permissions userPerms = Permissions.SendMessages | Permissions.EmbedLinks;
    /// bool canEmbed = userPerms.HasPermission(Permissions.EmbedLinks); // true
    /// </code>
    /// </example>
    public static bool HasPermission(this Permissions source, Permissions permission)
    {
        return (source & permission) == permission;
    }

    /// <summary>
    /// Checks if the source has any of the specified permissions.
    /// </summary>
    /// <param name="source">The source permissions.</param>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source has any of the permissions.</returns>
    public static bool HasAnyPermission(this Permissions source, Permissions permissions)
    {
        return (source & permissions) != 0;
    }

    /// <summary>
    /// Checks if the source can manage the server (has ManageGuild permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can manage the server.</returns>
    public static bool CanManageServer(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.ManageGuild);
    }

    /// <summary>
    /// Checks if the source can manage messages (has ManageMessages permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can manage messages.</returns>
    public static bool CanManageMessages(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.ManageMessages);
    }

    /// <summary>
    /// Checks if the source can manage roles (has ManageRoles permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can manage roles.</returns>
    public static bool CanManageRoles(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.ManageRoles);
    }

    /// <summary>
    /// Checks if the source can moderate members (has ModerateMembers permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can moderate members.</returns>
    public static bool CanModerateMembers(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.ModerateMembers);
    }

    /// <summary>
    /// Checks if the source can connect to voice channels (has Connect permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can connect to voice channels.</returns>
    public static bool CanConnect(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.Connect);
    }

    /// <summary>
    /// Checks if the source can speak in voice channels (has Speak permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source can speak in voice channels.</returns>
    public static bool CanSpeak(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.Speak);
    }

    /// <summary>
    /// Checks if the source is an administrator (has Administrator permission).
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if the source is an administrator.</returns>
    public static bool IsAdministrator(this Permissions permissions)
    {
        return permissions.HasPermission(Permissions.Administrator);
    }

    /// <summary>
    /// Adds a permission to the source.
    /// </summary>
    /// <param name="source">The source permissions.</param>
    /// <param name="permission">The permission to add.</param>
    /// <returns>The updated permissions.</returns>
    public static Permissions AddPermission(this Permissions source, Permissions permission)
    {
        return source | permission;
    }

    /// <summary>
    /// Removes a permission from the source.
    /// </summary>
    /// <param name="source">The source permissions.</param>
    /// <param name="permission">The permission to remove.</param>
    /// <returns>The updated permissions.</returns>
    public static Permissions RemovePermission(this Permissions source, Permissions permission)
    {
        return source & ~permission;
    }
}
