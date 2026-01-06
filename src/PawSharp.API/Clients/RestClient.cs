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