namespace PawSharp.Core.Enums;

/// <summary>
/// Channel types that can be selected in a channel select menu.
/// </summary>
public enum SelectMenuChannelType
{
    /// <summary>Guild text channel.</summary>
    GuildText = 0,
    
    /// <summary>Direct message channel.</summary>
    Dm = 1,
    
    /// <summary>Guild voice channel.</summary>
    GuildVoice = 2,
    
    /// <summary>Group direct message channel.</summary>
    GroupDm = 3,
    
    /// <summary>Guild announcement channel.</summary>
    GuildAnnouncement = 5,
    
    /// <summary>Announcement thread channel.</summary>
    AnnouncementThread = 10,
    
    /// <summary>Public thread channel.</summary>
    PublicThread = 11,
    
    /// <summary>Private thread channel.</summary>
    PrivateThread = 12,
    
    /// <summary>Guild stage voice channel.</summary>
    GuildStageVoice = 13,
    
    /// <summary>Guild directory channel.</summary>
    GuildDirectory = 14,
    
    /// <summary>Guild forum channel.</summary>
    GuildForum = 15,
    
    /// <summary>Guild media channel.</summary>
    GuildMedia = 16,
}
