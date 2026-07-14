#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Core.Entities;

namespace PawSharp.API.Interfaces;

/// <summary>
/// Interface for Discord REST API client.
/// </summary>
/// <example>
/// <code>
/// var message = await restClient.CreateMessageAsync(channelId, new CreateMessageRequest
/// {
///     Content = "Hello from PawSharp!",
///     Embeds = new List&lt;Embed&gt;
///     {
///         new Embed { Title = "PawSharp", Description = "A Discord API wrapper" }
///     }
/// });
/// </code>
/// </example>
public interface IDiscordRestClient
{
    
    /// <summary>
    /// Sends a GET request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Sends a POST request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Sends a PUT request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Sends a DELETE request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Sends a PATCH request to the Discord API with audit log reason and cancellation support.
    /// </summary>
    Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current bot user information.
    /// </summary>
    Task<HttpResponseMessage> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    
    // User operations
    /// <summary>
    /// Gets user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user or null if the request fails.</returns>

    Task<User?> GetUserAsync(ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies current user.
    /// </summary>
    /// <param name="username">The new username.</param>
    /// <param name="avatar">The avatar image data.</param>
    /// <param name="banner">The banner image data.</param>
    /// <param name="avatarDecorationData">The avatar decoration data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The raw HTTP response message.</returns>

    Task<HttpResponseMessage> ModifyCurrentUserAsync(string? username = null, string? avatar = null, string? banner = null, string? avatarDecorationData = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets current user guilds.
    /// </summary>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild or null if the request fails.</returns>

    Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Leaves guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> LeaveGuildAsync(ulong guildId, CancellationToken cancellationToken = default);
    
    // Message operations
    /// <summary>
    /// Creates and sends a message in the specified channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel to send the message to.</param>
    /// <param name="request">The message content, embeds, components, and other options.</param>
    /// <returns>The created message, or <see langword="null"/> if the operation failed.</returns>
    /// <example>
    /// <code>
    /// var msg = await client.CreateMessageAsync(channelId, new CreateMessageRequest
    /// {
    ///     Content = "Hello, world!",
    ///     Tts = false
    /// });
    /// </code>
    /// </example>
    Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the forward message operation.
    /// </summary>
    /// <param name="targetChannelId">The ID of the target channel.</param>
    /// <param name="sourceChannelId">The ID of the source channel.</param>
    /// <param name="sourceMessageId">The ID of the source message.</param>
    /// <param name="content">The HTTP content.</param>
    /// <param name="failIfNotExists">Whether to fail if the source message does not exist.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> ForwardMessageAsync(ulong targetChannelId, ulong sourceChannelId, ulong sourceMessageId, string? content = null, bool failIfNotExists = true, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sends file.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="fileStream">The file stream to send.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="messageRequest">Optional message request to include with the file(s).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> SendFileAsync(ulong channelId, Stream fileStream, string fileName, CreateMessageRequest? messageRequest = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sends files.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="files">The file attachments to send.</param>
    /// <param name="messageRequest">Optional message request to include with the file(s).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> SendFilesAsync(ulong channelId, IEnumerable<(Stream Stream, string FileName)> files, CreateMessageRequest? messageRequest = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets message.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> GetMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits message.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes message.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets channel messages.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="around">Get items around this value.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of message or null if the request fails.</returns>

    Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs a bulk delete messages.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageIds">The message ids.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the pin message operation.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> PinMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the unpin message operation.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets pinned messages.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of message or null if the request fails.</returns>

    Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Triggers typing indicator.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> TriggerTypingIndicatorAsync(ulong channelId, CancellationToken cancellationToken = default);
    
    // Channel operations
    /// <summary>
    /// Gets channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> GetChannelAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteChannelAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild channel.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets channel invites.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of invite or null if the request fails.</returns>

    Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates channel invite.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The invite or null if the request fails.</returns>

    Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes channel permission.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="overwriteId">The ID of the overwrite.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the status of a voice channel.
    /// </summary>
    /// <param name="channelId">The voice channel ID.</param>
    /// <returns>The voice channel status text, or null if none is set.</returns>
    Task<string?> GetVoiceChannelStatusAsync(ulong channelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets or clears the status of a voice channel.
    /// </summary>
    /// <param name="channelId">The voice channel ID.</param>
    /// <param name="status">The status text (max 500 characters), or null to clear.</param>
    /// <returns>The updated channel object.</returns>
    Task<Channel?> SetVoiceChannelStatusAsync(ulong channelId, string? status, CancellationToken cancellationToken = default);
    
    // Guild operations
    /// <summary>
    /// Gets guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="withCounts">Whether to include approximate member and presence counts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild or null if the request fails.</returns>

    Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild or null if the request fails.</returns>

    Task<Guild?> CreateGuildAsync(CreateGuildRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild or null if the request fails.</returns>

    Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild mfa level.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="level">The MFA level.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The int or null if the request fails.</returns>

    Task<int?> ModifyGuildMfaLevelAsync(ulong guildId, int level, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild channels.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of channel or null if the request fails.</returns>

    Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild members.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild member or null if the request fails.</returns>

    Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000, ulong? after = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild member or null if the request fails.</returns>

    Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds guild member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild member or null if the request fails.</returns>

    Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild member or null if the request fails.</returns>

    Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes guild member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild bans.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of ban or null if the request fails.</returns>

    Task<List<Ban>?> GetGuildBansAsync(ulong guildId, ulong? before = null, ulong? after = null, int? limit = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild ban.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The ban or null if the request fails.</returns>

    Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild ban.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="deleteMessageDays">Number of days of messages to delete.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes guild ban.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId, CancellationToken cancellationToken = default);
    
    // Role operations
    /// <summary>
    /// Gets guild roles.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of role or null if the request fails.</returns>

    Task<List<Role>?> GetGuildRolesAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The role or null if the request fails.</returns>

    Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The role or null if the request fails.</returns>

    Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds guild member role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes guild member role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId, CancellationToken cancellationToken = default);
    
    // Interaction operations
    /// <summary>
    /// Creates interaction response.
    /// </summary>
    /// <param name="interactionId">The ID of the interaction.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets original interaction response.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> GetOriginalInteractionResponseAsync(string applicationId, string interactionToken, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits original interaction response.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The raw HTTP response message.</returns>

    Task<HttpResponseMessage> EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes original interaction response.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteOriginalInteractionResponseAsync(string applicationId, string interactionToken, CancellationToken cancellationToken = default);
    
    // Interaction follow-up message operations
    /// <summary>
    /// Creates followup message.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> CreateFollowupMessageAsync(string applicationId, string interactionToken, CreateMessageRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets followup message.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> GetFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits followup message.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> EditFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId, EditMessageRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes followup message.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="interactionToken">The interaction token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId, CancellationToken cancellationToken = default);
    
    // Reaction operations
    /// <summary>
    /// Creates reaction.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> CreateReactionAsync(ulong channelId, ulong messageId, string emoji, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes own reaction.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteOwnReactionAsync(ulong channelId, ulong messageId, string emoji, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes user reaction.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId, CancellationToken cancellationToken = default);
    
    // Application Command operations
    /// <summary>
    /// Gets global application commands.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command or null if the request fails.</returns>

    Task<List<ApplicationCommand>?> GetGlobalApplicationCommandsAsync(ulong applicationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates global application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets global application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits global application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes global application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild application commands.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command or null if the request fails.</returns>

    Task<List<ApplicationCommand>?> GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits guild application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command or null if the request fails.</returns>

    Task<ApplicationCommand?> EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild application command.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs a bulk overwrite global application commands.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="commands">The command list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command or null if the request fails.</returns>

    Task<List<ApplicationCommand>?> BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs a bulk overwrite guild application commands.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commands">The command list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command or null if the request fails.</returns>

    Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands, CancellationToken cancellationToken = default);
    
    // Application Command Permissions operations
    /// <summary>
    /// Gets guild application command permissions.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command permissions or null if the request fails.</returns>

    Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets application command permissions.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command permissions or null if the request fails.</returns>

    Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits application command permissions.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="permissions">The permission list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application command permissions or null if the request fails.</returns>

    Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the batch edit application command permissions operation.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="permissions">The permission list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application command permissions or null if the request fails.</returns>

    Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions, CancellationToken cancellationToken = default);
    
    // Thread operations
    /// <summary>
    /// Creates thread.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateThreadAsync(ulong channelId, CreateThreadRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates thread from message.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates thread in forum.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Joins thread.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> JoinThreadAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds thread member.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Leaves thread.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> LeaveThreadAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Removes thread member.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> RemoveThreadMemberAsync(ulong channelId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets thread member.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The thread member or null if the request fails.</returns>

    Task<ThreadMember?> GetThreadMemberAsync(ulong channelId, ulong userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets thread members.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="withMember">Whether to include member objects.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of thread member or null if the request fails.</returns>

    Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId, bool withMember = false, ulong? after = null, int? limit = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets active threads.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The active threads response or null if the request fails.</returns>

    Task<ActiveThreadsResponse?> GetActiveThreadsAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets public archived threads.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The archived threads response or null if the request fails.</returns>

    Task<ArchivedThreadsResponse?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets private archived threads.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The archived threads response or null if the request fails.</returns>

    Task<ArchivedThreadsResponse?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets joined private archived threads.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The archived threads response or null if the request fails.</returns>

    Task<ArchivedThreadsResponse?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null, CancellationToken cancellationToken = default);
    
    // Webhook operations
    /// <summary>
    /// Creates webhook.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The webhook or null if the request fails.</returns>

    Task<Webhook?> CreateWebhookAsync(ulong channelId, CreateWebhookRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets channel webhooks.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of webhook or null if the request fails.</returns>

    Task<List<Webhook>?> GetChannelWebhooksAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild webhooks.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of webhook or null if the request fails.</returns>

    Task<List<Webhook>?> GetGuildWebhooksAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The webhook or null if the request fails.</returns>

    Task<Webhook?> GetWebhookAsync(ulong webhookId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets webhook with token.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The webhook or null if the request fails.</returns>

    Task<Webhook?> GetWebhookWithTokenAsync(ulong webhookId, string token, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The webhook or null if the request fails.</returns>

    Task<Webhook?> ModifyWebhookAsync(ulong webhookId, ModifyWebhookRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies webhook with token.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The webhook or null if the request fails.</returns>

    Task<Webhook?> ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteWebhookAsync(ulong webhookId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes webhook with token.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteWebhookWithTokenAsync(ulong webhookId, string token, CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> ExecuteWebhookAsync(ulong webhookId, string token, ExecuteWebhookRequest request, ulong? threadId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets webhook message.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> GetWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Edits webhook message.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> EditWebhookMessageAsync(ulong webhookId, string token, ulong messageId, EditMessageRequest request, ulong? threadId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes webhook message.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="threadId">The thread ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes slack compatible webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="payload">The payload to send.</param>
    /// <param name="wait">Whether to wait for server confirmation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ExecuteSlackCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false, CancellationToken cancellationToken = default);
    /// <summary>
    /// Executes git hub compatible webhook.
    /// </summary>
    /// <param name="webhookId">The ID of the webhook.</param>
    /// <param name="token">The token.</param>
    /// <param name="payload">The payload to send.</param>
    /// <param name="wait">Whether to wait for server confirmation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ExecuteGitHubCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false, CancellationToken cancellationToken = default);
    
    // Scheduled Event operations
    /// <summary>
    /// Creates guild scheduled event.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild scheduled event or null if the request fails.</returns>

    Task<GuildScheduledEvent?> CreateGuildScheduledEventAsync(ulong guildId, CreateGuildScheduledEventRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild scheduled events.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="withUserCount">Whether to include the user count.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild scheduled event or null if the request fails.</returns>

    Task<List<GuildScheduledEvent>?> GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild scheduled event.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="withUserCount">Whether to include the user count.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild scheduled event or null if the request fails.</returns>

    Task<GuildScheduledEvent?> GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild scheduled event.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild scheduled event or null if the request fails.</returns>

    Task<GuildScheduledEvent?> ModifyGuildScheduledEventAsync(ulong guildId, ulong eventId, ModifyGuildScheduledEventRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild scheduled event.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild scheduled event users.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="eventId">The ID of the event.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="withMember">Whether to include member objects.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of user or null if the request fails.</returns>

    Task<List<User>?> GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit = null, bool? withMember = null, ulong? before = null, ulong? after = null, CancellationToken cancellationToken = default);
    
    // Audit Log operations
    /// <summary>
    /// Gets guild audit logs.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="actionType">The action type.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The audit log or null if the request fails.</returns>

    Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, ulong? after = null, int? limit = null, CancellationToken cancellationToken = default);
    
    // Auto Moderation operations
    /// <summary>
    /// Lists auto moderation rules.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of auto moderation rule or null if the request fails.</returns>

    Task<List<AutoModerationRule>?> ListAutoModerationRulesAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets auto moderation rule.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="ruleId">The ID of the rule.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The auto moderation rule or null if the request fails.</returns>

    Task<AutoModerationRule?> GetAutoModerationRuleAsync(ulong guildId, ulong ruleId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates auto moderation rule.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The auto moderation rule or null if the request fails.</returns>

    Task<AutoModerationRule?> CreateAutoModerationRuleAsync(ulong guildId, CreateAutoModerationRuleRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies auto moderation rule.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="ruleId">The ID of the rule.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The auto moderation rule or null if the request fails.</returns>

    Task<AutoModerationRule?> ModifyAutoModerationRuleAsync(ulong guildId, ulong ruleId, ModifyAutoModerationRuleRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes auto moderation rule.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="ruleId">The ID of the rule.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId, CancellationToken cancellationToken = default);

    // Stage Instance operations
    /// <summary>
    /// Creates stage instance.
    /// </summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stage instance or null if the request fails.</returns>

    Task<StageInstance?> CreateStageInstanceAsync(CreateStageInstanceRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets stage instance.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stage instance or null if the request fails.</returns>

    Task<StageInstance?> GetStageInstanceAsync(ulong channelId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies stage instance.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The stage instance or null if the request fails.</returns>

    Task<StageInstance?> ModifyStageInstanceAsync(ulong channelId, ModifyStageInstanceRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes stage instance.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteStageInstanceAsync(ulong channelId, CancellationToken cancellationToken = default);

    // Sticker operations
    /// <summary>
    /// Gets sticker.
    /// </summary>
    /// <param name="stickerId">The ID of the sticker.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The sticker or null if the request fails.</returns>

    Task<Sticker?> GetStickerAsync(ulong stickerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets nitro sticker packs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of sticker pack or null if the request fails.</returns>

    Task<List<StickerPack>?> GetNitroStickerPacksAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild stickers.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of sticker or null if the request fails.</returns>

    Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild sticker.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="stickerId">The ID of the sticker.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The sticker or null if the request fails.</returns>

    Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild sticker.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The sticker or null if the request fails.</returns>

    Task<Sticker?> CreateGuildStickerAsync(ulong guildId, CreateGuildStickerRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild sticker.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="stickerId">The ID of the sticker.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The sticker or null if the request fails.</returns>

    Task<Sticker?> ModifyGuildStickerAsync(ulong guildId, ulong stickerId, ModifyGuildStickerRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild sticker.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="stickerId">The ID of the sticker.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildStickerAsync(ulong guildId, ulong stickerId, CancellationToken cancellationToken = default);

    // DM operations
    /// <summary>
    /// Creates dm.
    /// </summary>
    /// <param name="recipientId">The ID of the recipient.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateDmAsync(ulong recipientId, CancellationToken cancellationToken = default);

    // Gateway Bot info
    /// <summary>
    /// Gets gateway connection information for the bot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The gateway bot info or null if the request fails.</returns>

    Task<GatewayBotInfo?> GetGatewayBotAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the gateway URL for WebSocket connections. Does not require authentication.
    /// </summary>
    Task<GatewayInfo?> GetGatewayAsync(CancellationToken cancellationToken = default);

    // Voice Region operations
    /// <summary>
    /// Gets voice regions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of voice region or null if the request fails.</returns>

    Task<List<VoiceRegion>?> GetVoiceRegionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild voice regions.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of voice region or null if the request fails.</returns>

    Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Message crosspost
    /// <summary>
    /// Crossposts message.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);

    // Channel permission overwrites
    /// <summary>
    /// Edits channel permissions.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="overwriteId">The ID of the overwrite.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request, CancellationToken cancellationToken = default);

    // Current user connections
    /// <summary>
    /// Gets current user connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of user connection or null if the request fails.</returns>

    Task<List<UserConnection>?> GetCurrentUserConnectionsAsync(CancellationToken cancellationToken = default);

    // Guild member search
    /// <summary>
    /// Searches for guild members.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="query">The search query.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild member or null if the request fails.</returns>

    Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int? limit = null, CancellationToken cancellationToken = default);

    // Modify current member (e.g. nick)
    /// <summary>
    /// Modifies current member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="nick">The nick.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild member or null if the request fails.</returns>

    Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick, CancellationToken cancellationToken = default);

    // Poll operations
    /// <summary>
    /// Gets answer voters.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="answerId">The answer id.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of user or null if the request fails.</returns>

    Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Ends poll.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The message or null if the request fails.</returns>

    Task<Message?> EndPollAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);

    // SKU operations
    /// <summary>
    /// Lists skus.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of sku or null if the request fails.</returns>

    Task<List<Sku>?> ListSkusAsync(ulong applicationId, CancellationToken cancellationToken = default);

    // Entitlement operations
    /// <summary>
    /// Lists entitlements.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="skuIds">The sku ids.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="excludeEnded">The exclude ended.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of entitlement or null if the request fails.</returns>

    Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets entitlement.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="entitlementId">The ID of the entitlement.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The entitlement or null if the request fails.</returns>

    Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates test entitlement.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The entitlement or null if the request fails.</returns>

    Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes test entitlement.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="entitlementId">The ID of the entitlement.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteTestEntitlementAsync(ulong applicationId, ulong entitlementId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Consumes entitlement.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="entitlementId">The ID of the entitlement.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ConsumeEntitlementAsync(ulong applicationId, ulong entitlementId, CancellationToken cancellationToken = default);

    // Subscription operations
    /// <summary>
    /// Lists sku subscriptions.
    /// </summary>
    /// <param name="skuId">The ID of the sku.</param>
    /// <param name="before">Get items before this value.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of subscription or null if the request fails.</returns>

    Task<List<Subscription>?> ListSkuSubscriptionsAsync(ulong skuId, ulong? before = null, ulong? after = null, int? limit = null, ulong? userId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets sku subscription.
    /// </summary>
    /// <param name="skuId">The ID of the sku.</param>
    /// <param name="subscriptionId">The ID of the subscription.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The subscription or null if the request fails.</returns>

    Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId, CancellationToken cancellationToken = default);

    // Soundboard operations
    /// <summary>
    /// Lists default soundboard sounds.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of soundboard sound or null if the request fails.</returns>

    Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Lists guild soundboard sounds.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of soundboard sound or null if the request fails.</returns>

    Task<List<SoundboardSound>?> ListGuildSoundboardSoundsAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild soundboard sound.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="soundId">The ID of the sound.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The soundboard sound or null if the request fails.</returns>

    Task<SoundboardSound?> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild soundboard sound.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The soundboard sound or null if the request fails.</returns>

    Task<SoundboardSound?> CreateGuildSoundboardSoundAsync(ulong guildId, CreateGuildSoundboardSoundRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild soundboard sound.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="soundId">The ID of the sound.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The soundboard sound or null if the request fails.</returns>

    Task<SoundboardSound?> ModifyGuildSoundboardSoundAsync(ulong guildId, ulong soundId, ModifyGuildSoundboardSoundRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild soundboard sound.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="soundId">The ID of the sound.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId, CancellationToken cancellationToken = default);

    // Guild Onboarding operations
    /// <summary>
    /// Gets guild onboarding.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild onboarding or null if the request fails.</returns>

    Task<GuildOnboarding?> GetGuildOnboardingAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild onboarding.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild onboarding or null if the request fails.</returns>

    Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request, CancellationToken cancellationToken = default);

    // Application Role Connection Metadata
    /// <summary>
    /// Gets application role connection metadata.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application role connection metadata or null if the request fails.</returns>

    Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates application role connection metadata.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="records">The metadata records.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of application role connection metadata or null if the request fails.</returns>

    Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records, CancellationToken cancellationToken = default);

    // ── Alpha13 additions ─────────────────────────────────────────────────────

    // Reaction query (GET reactions on a message)
    /// <summary>
    /// Gets reactions.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="type">The type.</param>
    /// <param name="after">Get items after this value.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of user or null if the request fails.</returns>

    Task<List<User>?> GetReactionsAsync(ulong channelId, ulong messageId, string emoji, int? type = null, ulong? after = null, int? limit = null, CancellationToken cancellationToken = default);

    // Announcement channel follow
    /// <summary>
    /// Follows announcement channel.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="webhookChannelId">The ID of the webhook channel.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The followed channel or null if the request fails.</returns>

    Task<FollowedChannel?> FollowAnnouncementChannelAsync(ulong channelId, ulong webhookChannelId, CancellationToken cancellationToken = default);

    // Guild preview
    /// <summary>
    /// Gets guild preview.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild preview or null if the request fails.</returns>

    Task<GuildPreview?> GetGuildPreviewAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Guild widget
    /// <summary>
    /// Gets guild widget settings.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild widget settings or null if the request fails.</returns>

    Task<GuildWidgetSettings?> GetGuildWidgetSettingsAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild widget.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild widget or null if the request fails.</returns>

    Task<GuildWidget?> GetGuildWidgetAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild widget.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild widget settings or null if the request fails.</returns>

    Task<GuildWidgetSettings?> ModifyGuildWidgetAsync(ulong guildId, ModifyGuildWidgetRequest request, CancellationToken cancellationToken = default);

    // Guild vanity URL
    /// <summary>
    /// Gets guild vanity url.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The vanity url or null if the request fails.</returns>

    Task<VanityUrl?> GetGuildVanityUrlAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Guild welcome screen
    /// <summary>
    /// Gets guild welcome screen.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The welcome screen or null if the request fails.</returns>

    Task<WelcomeScreen?> GetGuildWelcomeScreenAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild welcome screen.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The welcome screen or null if the request fails.</returns>

    Task<WelcomeScreen?> ModifyGuildWelcomeScreenAsync(ulong guildId, ModifyGuildWelcomeScreenRequest request, CancellationToken cancellationToken = default);

    // Guild channel / role position reorder
    /// <summary>
    /// Modifies guild channel positions.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="positions">The position list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ModifyGuildChannelPositionsAsync(ulong guildId, IEnumerable<ModifyChannelPositionRequest> positions, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild role positions.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="positions">The position list.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of role or null if the request fails.</returns>

    Task<List<Role>?> ModifyGuildRolePositionsAsync(ulong guildId, IEnumerable<ModifyRolePositionRequest> positions, CancellationToken cancellationToken = default);

    // Invite lookup and deletion
    /// <summary>
    /// Gets invite.
    /// </summary>
    /// <param name="inviteCode">The invite code.</param>
    /// <param name="withCounts">Whether to include approximate member and presence counts.</param>
    /// <param name="withExpiration">The with expiration.</param>
    /// <param name="guildScheduledEventId">The ID of the guild scheduled event.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The invite or null if the request fails.</returns>

    Task<Invite?> GetInviteAsync(string inviteCode, bool? withCounts = null, bool? withExpiration = null, ulong? guildScheduledEventId = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes invite.
    /// </summary>
    /// <param name="inviteCode">The invite code.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The invite or null if the request fails.</returns>

    Task<Invite?> DeleteInviteAsync(string inviteCode, string? reason = null, CancellationToken cancellationToken = default);

    // Guild Templates
    /// <summary>
    /// Gets guild templates.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild template or null if the request fails.</returns>

    Task<List<GuildTemplate>?> GetGuildTemplatesAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild template.
    /// </summary>
    /// <param name="templateCode">The template code.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild template or null if the request fails.</returns>

    Task<GuildTemplate?> GetGuildTemplateAsync(string templateCode, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild from template.
    /// </summary>
    /// <param name="templateCode">The template code.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild or null if the request fails.</returns>

    Task<Guild?> CreateGuildFromTemplateAsync(string templateCode, CreateGuildFromTemplateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild template.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild template or null if the request fails.</returns>

    Task<GuildTemplate?> CreateGuildTemplateAsync(ulong guildId, CreateGuildTemplateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Syncs guild template.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="templateCode">The template code.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild template or null if the request fails.</returns>

    Task<GuildTemplate?> SyncGuildTemplateAsync(ulong guildId, string templateCode, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild template.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="templateCode">The template code.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild template or null if the request fails.</returns>

    Task<GuildTemplate?> ModifyGuildTemplateAsync(ulong guildId, string templateCode, ModifyGuildTemplateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild template.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="templateCode">The template code.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild template or null if the request fails.</returns>

    Task<GuildTemplate?> DeleteGuildTemplateAsync(ulong guildId, string templateCode, CancellationToken cancellationToken = default);

    // OAuth2 operations
    /// <summary>Returns the bot's application object.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application object, or null if the request fails.</returns>
    Task<Application?> GetCurrentBotApplicationInfoAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns info about the current authorization. Requires a Bearer token.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The OAuth2 info, or null if the request fails.</returns>
    Task<OAuth2Info?> GetCurrentAuthorizationInfoAsync(CancellationToken cancellationToken = default);

    // Application management
    /// <summary>Returns the current application.</summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application object, or null if the request fails.</returns>
    Task<Application?> GetCurrentApplicationAsync(CancellationToken cancellationToken = default);
    /// <summary>Edits properties of the current application.</summary>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated application object, or null if the request fails.</returns>
    Task<Application?> EditCurrentApplicationAsync(EditCurrentApplicationRequest request, CancellationToken cancellationToken = default);

    // Guild emoji operations
    /// <summary>
    /// Lists guild emojis.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of emoji or null if the request fails.</returns>

    Task<List<Emoji>?> ListGuildEmojisAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild emoji.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> GetGuildEmojiAsync(ulong guildId, ulong emojiId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates guild emoji.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> CreateGuildEmojiAsync(ulong guildId, CreateGuildEmojiRequest request, string? reason = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies guild emoji.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> ModifyGuildEmojiAsync(ulong guildId, ulong emojiId, ModifyGuildEmojiRequest request, string? reason = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild emoji.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildEmojiAsync(ulong guildId, ulong emojiId, string? reason = null, CancellationToken cancellationToken = default);

    // Application emoji operations
    /// <summary>
    /// Lists application emojis.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of emoji or null if the request fails.</returns>

    Task<List<Emoji>?> ListApplicationEmojisAsync(ulong applicationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets application emoji.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Creates application emoji.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> CreateApplicationEmojiAsync(ulong applicationId, CreateApplicationEmojiRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies application emoji.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The emoji or null if the request fails.</returns>

    Task<Emoji?> ModifyApplicationEmojiAsync(ulong applicationId, ulong emojiId, ModifyApplicationEmojiRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes application emoji.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="emojiId">The ID of the emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId, CancellationToken cancellationToken = default);

    // Guild integration operations
    /// <summary>
    /// Gets guild integrations.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of guild integration or null if the request fails.</returns>

    Task<List<GuildIntegration>?> GetGuildIntegrationsAsync(ulong guildId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes guild integration.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="integrationId">The ID of the integration.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId, string? reason = null, CancellationToken cancellationToken = default);

    // Guild invite operations
    /// <summary>
    /// Gets guild invites.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of invite or null if the request fails.</returns>

    Task<List<Invite>?> GetGuildInvitesAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Guild prune operations
    /// <summary>
    /// Gets guild prune count.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="days">Number of days to count inactivity.</param>
    /// <param name="includeRoles">Roles to include in the prune count.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild prune result or null if the request fails.</returns>

    Task<GuildPruneResult?> GetGuildPruneCountAsync(ulong guildId, int? days = null, List<ulong>? includeRoles = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Begins guild prune.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild prune result or null if the request fails.</returns>

    Task<GuildPruneResult?> BeginGuildPruneAsync(ulong guildId, BeginGuildPruneRequest request, string? reason = null, CancellationToken cancellationToken = default);

    // Bulk ban
    /// <summary>
    /// Performs a bulk guild ban.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="reason">The audit log reason.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The bulk guild ban response or null if the request fails.</returns>

    Task<BulkGuildBanResponse?> BulkGuildBanAsync(ulong guildId, BulkGuildBanRequest request, string? reason = null, CancellationToken cancellationToken = default);

    // Guild role extras
    /// <summary>
    /// Gets guild role.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="roleId">The ID of the role.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The role or null if the request fails.</returns>

    Task<Role?> GetGuildRoleAsync(ulong guildId, ulong roleId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets guild role member counts.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dictionary or null if the request fails.</returns>

    Task<Dictionary<string, int>?> GetGuildRoleMemberCountsAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Guild incident actions
    /// <summary>
    /// Modifies guild incident actions.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild incident actions response or null if the request fails.</returns>

    Task<GuildIncidentActionsResponse?> ModifyGuildIncidentActionsAsync(ulong guildId, ModifyGuildIncidentActionsRequest request, CancellationToken cancellationToken = default);

    // Current user guild member
    /// <summary>
    /// Gets current user guild member.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The guild member or null if the request fails.</returns>

    Task<GuildMember?> GetCurrentUserGuildMemberAsync(ulong guildId, CancellationToken cancellationToken = default);

    // Reaction extras
    /// <summary>
    /// Deletes all reactions.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteAllReactionsAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes all reactions for emoji.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="messageId">The ID of the message.</param>
    /// <param name="emoji">The emoji.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> DeleteAllReactionsForEmojiAsync(ulong channelId, ulong messageId, string emoji, CancellationToken cancellationToken = default);

    // Soundboard
    /// <summary>
    /// Sends soundboard sound.
    /// </summary>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> SendSoundboardSoundAsync(ulong channelId, SendSoundboardSoundRequest request, CancellationToken cancellationToken = default);

    // Voice states
    /// <summary>
    /// Modifies current user voice state.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ModifyCurrentUserVoiceStateAsync(ulong guildId, ModifyCurrentUserVoiceStateRequest request, CancellationToken cancellationToken = default);
    /// <summary>
    /// Modifies user voice state.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> ModifyUserVoiceStateAsync(ulong guildId, ulong userId, ModifyUserVoiceStateRequest request, CancellationToken cancellationToken = default);

    // User application role connection
    /// <summary>
    /// Gets user application role connection.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application role connection or null if the request fails.</returns>

    Task<ApplicationRoleConnection?> GetUserApplicationRoleConnectionAsync(ulong applicationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Updates user application role connection.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The application role connection or null if the request fails.</returns>

    Task<ApplicationRoleConnection?> UpdateUserApplicationRoleConnectionAsync(ulong applicationId, UpdateUserApplicationRoleConnectionRequest request, CancellationToken cancellationToken = default);

    // OAuth2
    /// <summary>
    /// Exchanges code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="redirectUri">The redirect uri.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The o auth2 token response or null if the request fails.</returns>

    Task<OAuth2TokenResponse?> ExchangeCodeAsync(string code, string clientId, string clientSecret, string redirectUri, CancellationToken cancellationToken = default);
    /// <summary>
    /// Refreshes token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The o auth2 token response or null if the request fails.</returns>

    Task<OAuth2TokenResponse?> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret, CancellationToken cancellationToken = default);
    /// <summary>
    /// Revokes token.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="tokenTypeHint">The token type hint.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>true if successful; otherwise, false.</returns>

    Task<bool> RevokeTokenAsync(string token, string clientId, string clientSecret, string? tokenTypeHint = null, CancellationToken cancellationToken = default);

    // Group DM
    /// <summary>
    /// Creates group dm.
    /// </summary>
    /// <param name="accessTokens">The OAuth2 access tokens.</param>
    /// <param name="nicks">Optional nicknames mapped to user IDs.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The channel or null if the request fails.</returns>

    Task<Channel?> CreateGroupDmAsync(List<string> accessTokens, Dictionary<string, string>? nicks = null, CancellationToken cancellationToken = default);

    // Application Activity Instances
    /// <summary>
    /// Fetches a running embedded-application (Activity) instance.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="instanceId">The ID of the activity instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The activity instance, or null if the request fails.</returns>
    Task<ActivityInstance?> GetActivityInstanceAsync(ulong applicationId, string instanceId, CancellationToken cancellationToken = default);
}