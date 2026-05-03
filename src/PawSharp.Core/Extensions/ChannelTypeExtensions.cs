namespace PawSharp.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Enums.ChannelType"/> enum.
/// </summary>
public static class ChannelTypeExtensions
{
    /// <summary>
    /// Checks if the channel type is a guild channel (not a DM).
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is a guild channel.</returns>
    public static bool IsGuildChannel(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.GuildText or Enums.ChannelType.GuildVoice or
               Enums.ChannelType.GuildCategory or Enums.ChannelType.GuildAnnouncement or
               Enums.ChannelType.GuildStageVoice or Enums.ChannelType.GuildForum or
               Enums.ChannelType.GuildMedia;
    }

    /// <summary>
    /// Checks if the channel type is a DM channel.
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is a DM channel.</returns>
    public static bool IsDmChannel(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.DM or Enums.ChannelType.GroupDM;
    }

    /// <summary>
    /// Checks if the channel type is a thread.
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is a thread.</returns>
    public static bool IsThread(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.PublicThread or Enums.ChannelType.PrivateThread or
               Enums.ChannelType.AnnouncementThread;
    }

    /// <summary>
    /// Checks if the channel type is a text-based channel.
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is text-based.</returns>
    public static bool IsTextBased(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.GuildText or Enums.ChannelType.GuildAnnouncement or
               Enums.ChannelType.PublicThread or Enums.ChannelType.PrivateThread or
               Enums.ChannelType.AnnouncementThread or Enums.ChannelType.DM or
               Enums.ChannelType.GroupDM;
    }

    /// <summary>
    /// Checks if the channel type is a voice channel.
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is a voice channel.</returns>
    public static bool IsVoiceChannel(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.GuildVoice or Enums.ChannelType.GuildStageVoice;
    }

    /// <summary>
    /// Checks if the channel type is a forum or media channel.
    /// </summary>
    /// <param name="type">The channel type.</param>
    /// <returns>True if the channel is a forum or media channel.</returns>
    public static bool IsForumOrMedia(this Enums.ChannelType type)
    {
        return type is Enums.ChannelType.GuildForum or Enums.ChannelType.GuildMedia;
    }
}
