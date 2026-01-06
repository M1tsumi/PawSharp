using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using PawSharp.API.Models;
using PawSharp.Core.Entities;

namespace PawSharp.API.Interfaces;

/// <summary>
/// Interface for Discord REST API client.
/// </summary>
public interface IDiscordRestClient
{
    /// <summary>
    /// Sends a GET request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string endpoint);
    
    /// <summary>
    /// Sends a POST request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content);
    
    /// <summary>
    /// Sends a PUT request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content);
    
    /// <summary>
    /// Sends a DELETE request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string endpoint);
    
    /// <summary>
    /// Sends a PATCH request to the Discord API.
    /// </summary>
    Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content);

    /// <summary>
    /// Gets the current bot user information.
    /// </summary>
    Task<HttpResponseMessage> GetCurrentUserAsync();
    
    // User operations
    Task<User?> GetUserAsync(ulong userId);
    Task<HttpResponseMessage> ModifyCurrentUserAsync(string? username = null, string? avatar = null);
    Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null);
    Task<bool> LeaveGuildAsync(ulong guildId);
    
    // Message operations
    Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request);
    Task<Message?> GetMessageAsync(ulong channelId, ulong messageId);
    Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request);
    Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId);
    Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null);
    Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds);
    Task<bool> PinMessageAsync(ulong channelId, ulong messageId);
    Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId);
    Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId);
    Task<bool> TriggerTypingIndicatorAsync(ulong channelId);
    
    // Channel operations
    Task<Channel?> GetChannelAsync(ulong channelId);
    Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request);
    Task<bool> DeleteChannelAsync(ulong channelId);
    Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request);
    Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId);
    Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request);
    Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId);
    
    // Guild operations
    Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts = false);
    Task<Guild?> CreateGuildAsync(CreateGuildRequest request);
    Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request);
    Task<bool> DeleteGuildAsync(ulong guildId);
    Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId);
    Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000);
    Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId);
    Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request);
    Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request);
    Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId);
    Task<List<Ban>?> GetGuildBansAsync(ulong guildId);
    Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId);
    Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null);
    Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId);
    
    // Role operations
    Task<List<Role>?> GetGuildRolesAsync(ulong guildId);
    Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request);
    Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request);
    Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId);
    Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);
    Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId);
    
    // Interaction operations
    Task<bool> CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response);
    Task<HttpResponseMessage> EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request);
    Task<bool> DeleteOriginalInteractionResponseAsync(string applicationId, string interactionToken);
    
    // Reaction operations
    Task<bool> CreateReactionAsync(ulong channelId, ulong messageId, string emoji);
    Task<bool> DeleteOwnReactionAsync(ulong channelId, ulong messageId, string emoji);
    Task<bool> DeleteUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId);
}