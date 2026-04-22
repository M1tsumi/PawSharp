#nullable enable
using System.Text.Json.Serialization;

namespace PawSharp.Core.Serialization;

/// <summary>
/// Source-generated JSON serialization context for PawSharp entities.
/// Enables Native AOT compatibility by eliminating reflection-based serialization.
/// </summary>
[JsonSerializable(typeof(PawSharp.Core.Entities.MessageComponent))]
[JsonSerializable(typeof(PawSharp.Core.Entities.ActionRow))]
[JsonSerializable(typeof(PawSharp.Core.Entities.Button))]
[JsonSerializable(typeof(PawSharp.Core.Entities.SelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.StringSelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.UserSelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.RoleSelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.MentionableSelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.ChannelSelectMenu))]
[JsonSerializable(typeof(PawSharp.Core.Entities.TextInput))]
[JsonSerializable(typeof(PawSharp.Core.Entities.UnknownComponent))]
[JsonSerializable(typeof(PawSharp.Core.Entities.SelectOption))]
[JsonSerializable(typeof(PawSharp.Core.Entities.SelectDefaultValue))]
[JsonSerializable(typeof(PawSharp.Core.Entities.Section))]
[JsonSerializable(typeof(PawSharp.Core.Entities.TextDisplay))]
[JsonSerializable(typeof(PawSharp.Core.Entities.ThumbnailComponent))]
[JsonSerializable(typeof(PawSharp.Core.Entities.MediaGallery))]
[JsonSerializable(typeof(PawSharp.Core.Entities.FileComponent))]
[JsonSerializable(typeof(PawSharp.Core.Entities.Separator))]
[JsonSerializable(typeof(PawSharp.Core.Entities.Container))]
[JsonSerializable(typeof(PawSharp.Core.Entities.UnfurledMediaItem))]
[JsonSerializable(typeof(PawSharp.Core.Entities.MediaGalleryItem))]
[JsonSerializable(typeof(PawSharp.Core.Entities.Emoji))]
public partial class PawSharpJsonContext : JsonSerializerContext
{
}
