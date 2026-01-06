using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;

namespace PawSharp.API.Clients;

/// <summary>
/// Implementation of Discord REST API client with rate limiting.
/// </summary>
public class DiscordRestClient : IDiscordRestClient
{
    private readonly HttpClient _httpClient;
    private readonly PawSharpOptions _options;
    private readonly ILogger<DiscordRestClient> _logger;
    private DateTimeOffset _globalReset = DateTimeOffset.MinValue;

    public DiscordRestClient(HttpClient httpClient, PawSharpOptions options, ILogger<DiscordRestClient> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        
        // Set base address and auth header
        _httpClient.BaseAddress = new Uri($"https://discord.com/api/v{_options.ApiVersion}/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PawSharp", "1.0"));
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, content);
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Put, endpoint, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Delete, endpoint, null);
    }
    
    public async Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Patch, endpoint, content);
    }

    public async Task<HttpResponseMessage> GetCurrentUserAsync()
    {
        return await GetAsync("users/@me");
    }
    
    // User operations
    public async Task<User?> GetUserAsync(ulong userId)
    {
        var response = await GetAsync($"users/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<User>();
        }
        return null;
    }
    
    public async Task<HttpResponseMessage> ModifyCurrentUserAsync(string? username = null, string? avatar = null)
    {
        var payload = new { username, avatar };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await PatchAsync("users/@me", content);
    }
    
    public async Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null)
    {
        var queryParams = new List<string>();
        if (limit != 200) queryParams.Add($"limit={limit}");
        if (before.HasValue) queryParams.Add($"before={before.Value}");
        if (after.HasValue) queryParams.Add($"after={after.Value}");
        
        var endpoint = "users/@me/guilds";
        if (queryParams.Any()) endpoint += "?" + string.Join("&", queryParams);
        
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Guild>>();
        }
        return null;
    }
    
    public async Task<bool> LeaveGuildAsync(ulong guildId)
    {
        var response = await DeleteAsync($"users/@me/guilds/{guildId}");
        return response.IsSuccessStatusCode;
    }
    
    // Message operations
    public async Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/messages", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }
    
    public async Task<Message?> GetMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await GetAsync($"channels/{channelId}/messages/{messageId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }
    
    public async Task<Message?> EditMessageAsync(ulong channelId, ulong messageId, EditMessageRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"channels/{channelId}/messages/{messageId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }
    
    public async Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null)
    {
        var queryParams = new List<string> { $"limit={Math.Min(limit, 100)}" };
        if (around.HasValue) queryParams.Add($"around={around.Value}");
        else if (before.HasValue) queryParams.Add($"before={before.Value}");
        else if (after.HasValue) queryParams.Add($"after={after.Value}");
        
        var response = await GetAsync($"channels/{channelId}/messages?{string.Join("&", queryParams)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Message>>();
        }
        return null;
    }
    
    public async Task<bool> BulkDeleteMessagesAsync(ulong channelId, List<ulong> messageIds)
    {
        var payload = new { messages = messageIds };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/messages/bulk-delete", content);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> PinMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await PutAsync($"channels/{channelId}/pins/{messageId}", null!);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await DeleteAsync($"channels/{channelId}/pins/{messageId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}/pins");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Message>>();
        }
        return null;
    }
    
    public async Task<bool> TriggerTypingIndicatorAsync(ulong channelId)
    {
        var response = await PostAsync($"channels/{channelId}/typing", null!);
        return response.IsSuccessStatusCode;
    }
    
    // Channel operations
    public async Task<Channel?> GetChannelAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"channels/{channelId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<bool> DeleteChannelAsync(ulong channelId)
    {
        var response = await DeleteAsync($"channels/{channelId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"guilds/{guildId}/channels", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}/invites");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Invite>>();
        }
        return null;
    }
    
    public async Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/invites", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Invite>();
        }
        return null;
    }
    
    public async Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId)
    {
        var response = await DeleteAsync($"channels/{channelId}/permissions/{overwriteId}");
        return response.IsSuccessStatusCode;
    }
    
    // Guild operations
    public async Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts = false)
    {
        var endpoint = $"guilds/{guildId}";
        if (withCounts)
            endpoint += "?with_counts=true";
        
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Guild>();
        }
        return null;
    }
    
    public async Task<Guild?> CreateGuildAsync(CreateGuildRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync("guilds", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Guild>();
        }
        return null;
    }
    
    public async Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Guild>();
        }
        return null;
    }
    
    public async Task<bool> DeleteGuildAsync(ulong guildId)
    {
        var response = await DeleteAsync($"guilds/{guildId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/channels");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Channel>>();
        }
        return null;
    }
    
    public async Task<List<GuildMember>?> GetGuildMembersAsync(ulong guildId, int limit = 1000)
    {
        var response = await GetAsync($"guilds/{guildId}/members?limit={limit}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GuildMember>>();
        }
        return null;
    }
    
    public async Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId)
    {
        var response = await GetAsync($"guilds/{guildId}/members/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }
        return null;
    }
    
    public async Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PutAsync($"guilds/{guildId}/members/{userId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }
        return null;
    }
    
    public async Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/members/{userId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }
        return null;
    }
    
    public async Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/members/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Ban>?> GetGuildBansAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/bans");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Ban>>();
        }
        return null;
    }
    
    public async Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId)
    {
        var response = await GetAsync($"guilds/{guildId}/bans/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Ban>();
        }
        return null;
    }
    
    public async Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null)
    {
        var payload = new { delete_message_days = deleteMessageDays, reason };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await PutAsync($"guilds/{guildId}/bans/{userId}", content);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/bans/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    // Role operations
    public async Task<List<Role>?> GetGuildRolesAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/roles");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Role>>();
        }
        return null;
    }
    
    public async Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"guilds/{guildId}/roles", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Role>();
        }
        return null;
    }
    
    public async Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/roles/{roleId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Role>();
        }
        return null;
    }
    
    public async Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/roles/{roleId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
    {
        var response = await PutAsync($"guilds/{guildId}/members/{userId}/roles/{roleId}", null!);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/members/{userId}/roles/{roleId}");
        return response.IsSuccessStatusCode;
    }
    
    // Interaction operations
    public async Task<bool> CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response)
    {
        var content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json");
        var httpResponse = await PostAsync($"interactions/{interactionId}/{interactionToken}/callback", content);
        return httpResponse.IsSuccessStatusCode;
    }
    
    public async Task<HttpResponseMessage> EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        return await PatchAsync($"webhooks/{applicationId}/{interactionToken}/messages/@original", content);
    }
    
    public async Task<bool> DeleteOriginalInteractionResponseAsync(string applicationId, string interactionToken)
    {
        var response = await DeleteAsync($"webhooks/{applicationId}/{interactionToken}/messages/@original");
        return response.IsSuccessStatusCode;
    }
    
    // Reaction operations
    public async Task<bool> CreateReactionAsync(ulong channelId, ulong messageId, string emoji)
    {
        var response = await PutAsync($"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}/@me", null!);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> DeleteOwnReactionAsync(ulong channelId, ulong messageId, string emoji)
    {
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}/@me");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> DeleteUserReactionAsync(ulong channelId, ulong messageId, string emoji, ulong userId)
    {
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    // Application Command operations
    public async Task<List<ApplicationCommand>?> GetGlobalApplicationCommandsAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/commands");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> CreateGlobalApplicationCommandAsync(ulong applicationId, CreateApplicationCommandRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"applications/{applicationId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> GetGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)
    {
        var response = await GetAsync($"applications/{applicationId}/commands/{commandId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> EditGlobalApplicationCommandAsync(ulong applicationId, ulong commandId, CreateApplicationCommandRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"applications/{applicationId}/commands/{commandId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<bool> DeleteGlobalApplicationCommandAsync(ulong applicationId, ulong commandId)
    {
        var response = await DeleteAsync($"applications/{applicationId}/commands/{commandId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<ApplicationCommand>?> GetGuildApplicationCommandsAsync(ulong applicationId, ulong guildId)
    {
        var response = await GetAsync($"applications/{applicationId}/guilds/{guildId}/commands");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> CreateGuildApplicationCommandAsync(ulong applicationId, ulong guildId, CreateApplicationCommandRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"applications/{applicationId}/guilds/{guildId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> GetGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)
    {
        var response = await GetAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<ApplicationCommand?> EditGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId, CreateApplicationCommandRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommand>();
        }
        return null;
    }
    
    public async Task<bool> DeleteGuildApplicationCommandAsync(ulong applicationId, ulong guildId, ulong commandId)
    {
        var response = await DeleteAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<ApplicationCommand>?> BulkOverwriteGlobalApplicationCommandsAsync(ulong applicationId, List<CreateApplicationCommandRequest> commands)
    {
        var content = new StringContent(JsonSerializer.Serialize(commands), Encoding.UTF8, "application/json");
        var response = await PutAsync($"applications/{applicationId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        return null;
    }
    
    public async Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands)
    {
        var content = new StringContent(JsonSerializer.Serialize(commands), Encoding.UTF8, "application/json");
        var response = await PutAsync($"applications/{applicationId}/guilds/{guildId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        return null;
    }
    
    // Thread operations
    public async Task<Channel?> CreateThreadAsync(ulong channelId, CreateThreadRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/messages/{messageId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<bool> JoinThreadAsync(ulong channelId)
    {
        var response = await PutAsync($"channels/{channelId}/thread-members/@me", null!);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId)
    {
        var response = await PutAsync($"channels/{channelId}/thread-members/{userId}", null!);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> LeaveThreadAsync(ulong channelId)
    {
        var response = await DeleteAsync($"channels/{channelId}/thread-members/@me");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RemoveThreadMemberAsync(ulong channelId, ulong userId)
    {
        var response = await DeleteAsync($"channels/{channelId}/thread-members/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<ThreadMember?> GetThreadMemberAsync(ulong channelId, ulong userId)
    {
        var response = await GetAsync($"channels/{channelId}/thread-members/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ThreadMember>();
        }
        return null;
    }
    
    public async Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}/thread-members");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ThreadMember>>();
        }
        return null;
    }
    
    public async Task<List<Channel>?> GetActiveThreadsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/threads/active");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Channel>>();
        }
        return null;
    }
    
    public async Task<List<Channel>?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue) query.Add($"before={before.Value.ToUnixTimeSeconds()}");
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/threads/archived/public{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Channel>>();
        }
        return null;
    }
    
    public async Task<List<Channel>?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue) query.Add($"before={before.Value.ToUnixTimeSeconds()}");
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/threads/archived/private{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Channel>>();
        }
        return null;
    }
    
    public async Task<List<Channel>?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue) query.Add($"before={before.Value.ToUnixTimeSeconds()}");
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/users/@me/threads/archived/private{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Channel>>();
        }
        return null;
    }
    
    // Webhook operations
    public async Task<Webhook?> CreateWebhookAsync(ulong channelId, CreateWebhookRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"channels/{channelId}/webhooks", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<List<Webhook>?> GetChannelWebhooksAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}/webhooks");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Webhook>>();
        }
        return null;
    }
    
    public async Task<List<Webhook>?> GetGuildWebhooksAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/webhooks");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Webhook>>();
        }
        return null;
    }
    
    public async Task<Webhook?> GetWebhookAsync(ulong webhookId)
    {
        var response = await GetAsync($"webhooks/{webhookId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<Webhook?> GetWebhookWithTokenAsync(ulong webhookId, string token)
    {
        var response = await GetAsync($"webhooks/{webhookId}/{token}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<Webhook?> ModifyWebhookAsync(ulong webhookId, ModifyWebhookRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"webhooks/{webhookId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<Webhook?> ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"webhooks/{webhookId}/{token}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<bool> DeleteWebhookAsync(ulong webhookId)
    {
        var response = await DeleteAsync($"webhooks/{webhookId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> DeleteWebhookWithTokenAsync(ulong webhookId, string token)
    {
        var response = await DeleteAsync($"webhooks/{webhookId}/{token}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<Message?> ExecuteWebhookAsync(ulong webhookId, string token, ExecuteWebhookRequest request, ulong? threadId = null)
    {
        var endpoint = $"webhooks/{webhookId}/{token}";
        if (threadId.HasValue) endpoint += $"?thread_id={threadId.Value}";
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync(endpoint, content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }
    
    // Scheduled Event operations
    public async Task<GuildScheduledEvent?> CreateGuildScheduledEventAsync(ulong guildId, CreateGuildScheduledEventRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"guilds/{guildId}/scheduled-events", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildScheduledEvent>();
        }
        return null;
    }
    
    public async Task<List<GuildScheduledEvent>?> GetGuildScheduledEventsAsync(ulong guildId, bool? withUserCount = null)
    {
        var query = withUserCount.HasValue ? $"?with_user_count={withUserCount.Value.ToString().ToLower()}" : "";
        var response = await GetAsync($"guilds/{guildId}/scheduled-events{query}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GuildScheduledEvent>>();
        }
        return null;
    }
    
    public async Task<GuildScheduledEvent?> GetGuildScheduledEventAsync(ulong guildId, ulong eventId, bool? withUserCount = null)
    {
        var query = withUserCount.HasValue ? $"?with_user_count={withUserCount.Value.ToString().ToLower()}" : "";
        var response = await GetAsync($"guilds/{guildId}/scheduled-events/{eventId}{query}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildScheduledEvent>();
        }
        return null;
    }
    
    public async Task<GuildScheduledEvent?> ModifyGuildScheduledEventAsync(ulong guildId, ulong eventId, ModifyGuildScheduledEventRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/scheduled-events/{eventId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildScheduledEvent>();
        }
        return null;
    }
    
    public async Task<bool> DeleteGuildScheduledEventAsync(ulong guildId, ulong eventId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/scheduled-events/{eventId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<User>?> GetGuildScheduledEventUsersAsync(ulong guildId, ulong eventId, int? limit = null, bool? withMember = null, ulong? before = null, ulong? after = null)
    {
        var query = new List<string>();
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        if (withMember.HasValue) query.Add($"with_member={withMember.Value.ToString().ToLower()}");
        if (before.HasValue) query.Add($"before={before.Value}");
        if (after.HasValue) query.Add($"after={after.Value}");
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"guilds/{guildId}/scheduled-events/{eventId}/users{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<User>>();
        }
        return null;
    }
    
    // Audit Log operations
    public async Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (userId.HasValue) query.Add($"user_id={userId.Value}");
        if (actionType.HasValue) query.Add($"action_type={(int)actionType.Value}");
        if (before.HasValue) query.Add($"before={before.Value}");
        if (limit.HasValue) query.Add($"limit={limit.Value}");
        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"guilds/{guildId}/audit-logs{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuditLog>();
        }
        return null;
    }
    
    // Auto Moderation operations
    public async Task<List<AutoModerationRule>?> ListAutoModerationRulesAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/auto-moderation/rules");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<AutoModerationRule>>();
        }
        return null;
    }
    
    public async Task<AutoModerationRule?> GetAutoModerationRuleAsync(ulong guildId, ulong ruleId)
    {
        var response = await GetAsync($"guilds/{guildId}/auto-moderation/rules/{ruleId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AutoModerationRule>();
        }
        return null;
    }
    
    public async Task<AutoModerationRule?> CreateAutoModerationRuleAsync(ulong guildId, CreateAutoModerationRuleRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"guilds/{guildId}/auto-moderation/rules", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AutoModerationRule>();
        }
        return null;
    }
    
    public async Task<AutoModerationRule?> ModifyAutoModerationRuleAsync(ulong guildId, ulong ruleId, ModifyAutoModerationRuleRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/auto-moderation/rules/{ruleId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AutoModerationRule>();
        }
        return null;
    }
    
    public async Task<bool> DeleteAutoModerationRuleAsync(ulong guildId, ulong ruleId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/auto-moderation/rules/{ruleId}");
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, HttpContent? content)
    {
        // Global rate limit check
        if (DateTimeOffset.UtcNow < _globalReset)
        {
            var delay = _globalReset - DateTimeOffset.UtcNow;
            _logger.LogWarning("Global rate limit hit, delaying {Delay}", delay);
            await Task.Delay(delay);
        }

        var request = new HttpRequestMessage(method, endpoint) { Content = content };
        var response = await _httpClient.SendAsync(request);

        // Handle rate limiting
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
            _logger.LogWarning("Rate limited, retrying after {RetryAfter}", retryAfter);
            await Task.Delay(retryAfter);
            return await SendRequestAsync(method, endpoint, content); // Retry
        }

        if (response.Headers.TryGetValues("X-RateLimit-Global", out var globalValues) && bool.Parse(globalValues.FirstOrDefault() ?? "false"))
        {
            _globalReset = DateTimeOffset.UtcNow.AddSeconds(double.Parse(response.Headers.GetValues("Retry-After").First()));
        }

        return response;
    }
}