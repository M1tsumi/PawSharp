namespace PawSharp.Core.Enums;

/// <summary>Identifies whether a channel permission overwrite targets a role or a member.</summary>
public enum OverwriteType
{
    /// <summary>Overwrite applies to a role.</summary>
    Role   = 0,
    /// <summary>Overwrite applies to a guild member.</summary>
    Member = 1,
}
