#nullable enable
using System.Text.Json.Serialization;
using PawSharp.Core.Entities;

namespace PawSharp.Core.Serialization;

/// <summary>
/// Source-generated JSON serialization context for PawSharp Core entities.
/// Enables Native AOT compatibility by eliminating reflection-based serialization.
/// </summary>
[JsonSerializable(typeof(MessageComponent))]
[JsonSerializable(typeof(ActionRow))]
[JsonSerializable(typeof(Button))]
[JsonSerializable(typeof(SelectMenu))]
[JsonSerializable(typeof(StringSelectMenu))]
[JsonSerializable(typeof(UserSelectMenu))]
[JsonSerializable(typeof(RoleSelectMenu))]
[JsonSerializable(typeof(MentionableSelectMenu))]
[JsonSerializable(typeof(ChannelSelectMenu))]
[JsonSerializable(typeof(TextInput))]
[JsonSerializable(typeof(UnknownComponent))]
[JsonSerializable(typeof(SelectOption))]
[JsonSerializable(typeof(SelectDefaultValue))]
[JsonSerializable(typeof(Section))]
[JsonSerializable(typeof(TextDisplay))]
[JsonSerializable(typeof(ThumbnailComponent))]
[JsonSerializable(typeof(MediaGallery))]
[JsonSerializable(typeof(FileComponent))]
[JsonSerializable(typeof(Separator))]
[JsonSerializable(typeof(Container))]
[JsonSerializable(typeof(UnfurledMediaItem))]
[JsonSerializable(typeof(MediaGalleryItem))]
[JsonSerializable(typeof(Emoji))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Guild))]
[JsonSerializable(typeof(Channel))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(Role))]
[JsonSerializable(typeof(Presence))]
[JsonSerializable(typeof(VoiceState))]
[JsonSerializable(typeof(Integration))]
[JsonSerializable(typeof(Webhook))]
[JsonSerializable(typeof(Invite))]
[JsonSerializable(typeof(StageInstance))]
[JsonSerializable(typeof(Entities.Thread))]
[JsonSerializable(typeof(Sticker))]
[JsonSerializable(typeof(SoundboardSound))]
[JsonSerializable(typeof(Sku))]
[JsonSerializable(typeof(Entitlement))]
[JsonSerializable(typeof(Subscription))]
[JsonSerializable(typeof(Poll))]
[JsonSerializable(typeof(GuildScheduledEvent))]
[JsonSerializable(typeof(GuildOnboarding))]
[JsonSerializable(typeof(GuildTemplate))]
[JsonSerializable(typeof(Overwrite))]
[JsonSerializable(typeof(OAuth2Application))]
[JsonSerializable(typeof(Interaction))]
public partial class PawSharpJsonContext : JsonSerializerContext
{
}
