namespace PawSharp.Core.Enums;

/// <summary>
/// Identifies where an application can be installed (installation context).
/// </summary>
public enum ApplicationIntegrationType
{
    /// <summary>App is installable to servers.</summary>
    GuildInstall = 0,
    /// <summary>App is installable to users.</summary>
    UserInstall  = 1,
}
