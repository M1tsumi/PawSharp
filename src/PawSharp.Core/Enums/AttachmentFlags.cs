using System;

namespace PawSharp.Core.Enums;

/// <summary>
/// Bitfield flags that can be set on a Discord attachment.
/// See: https://discord.com/developers/docs/resources/message#attachment-object-attachment-flags
/// </summary>
[Flags]
public enum AttachmentFlags
{
    None         = 0,
    /// <summary>This attachment has been edited using the remix feature on mobile.</summary>
    IsRemix      = 1 << 2,
}
