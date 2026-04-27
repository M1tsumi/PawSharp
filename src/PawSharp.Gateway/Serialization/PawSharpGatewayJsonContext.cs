#nullable enable
using System.Text.Json.Serialization;
using PawSharp.Gateway.Events;

namespace PawSharp.Gateway.Serialization;

/// <summary>
/// Source-generated JSON serialization context for PawSharp Gateway events.
/// Enables Native AOT compatibility by eliminating reflection-based serialization.
/// </summary>
[JsonSerializable(typeof(ReadyEvent))]
[JsonSerializable(typeof(MessageCreateEvent))]
[JsonSerializable(typeof(MessageUpdateEvent))]
[JsonSerializable(typeof(MessageDeleteEvent))]
[JsonSerializable(typeof(GuildCreateEvent))]
[JsonSerializable(typeof(GuildUpdateEvent))]
[JsonSerializable(typeof(GuildDeleteEvent))]
[JsonSerializable(typeof(GuildEmojisUpdateEvent))]
[JsonSerializable(typeof(ChannelCreateEvent))]
[JsonSerializable(typeof(ChannelUpdateEvent))]
[JsonSerializable(typeof(ChannelDeleteEvent))]
[JsonSerializable(typeof(GuildMemberAddEvent))]
[JsonSerializable(typeof(GuildMemberUpdateEvent))]
[JsonSerializable(typeof(GuildMemberRemoveEvent))]
[JsonSerializable(typeof(InteractionCreateEvent))]
[JsonSerializable(typeof(TypingStartEvent))]
[JsonSerializable(typeof(MessageReactionAddEvent))]
[JsonSerializable(typeof(MessageReactionRemoveEvent))]
[JsonSerializable(typeof(MessageReactionRemoveAllEvent))]
[JsonSerializable(typeof(PresenceUpdateEvent))]
[JsonSerializable(typeof(ChannelPinsUpdateEvent))]
[JsonSerializable(typeof(GuildBanAddEvent))]
[JsonSerializable(typeof(GuildBanRemoveEvent))]
[JsonSerializable(typeof(VoiceStateUpdateEvent))]
[JsonSerializable(typeof(VoiceServerUpdateEvent))]
[JsonSerializable(typeof(ThreadCreateEvent))]
[JsonSerializable(typeof(MessagePollVoteAddEvent))]
[JsonSerializable(typeof(GuildScheduledEventCreateEvent))]
[JsonSerializable(typeof(InviteCreateEvent))]
[JsonSerializable(typeof(UserUpdateEvent))]
public partial class PawSharpGatewayJsonContext : JsonSerializerContext
{
}
