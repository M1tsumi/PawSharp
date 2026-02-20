using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Interfaces;
using PawSharp.API.RateLimit;
using PawSharp.API.Models;
using PawSharp.Core.Entities;
using PawSharp.Core.Exceptions;
using PawSharp.Core.Models;
using PawSharp.Core.Validation;

namespace PawSharp.API.Clients;

/// <summary>
/// Implementation of Discord REST API client with rate limiting.
/// </summary>
public class DiscordRestClient : IDiscordRestClient
{
    private readonly HttpClient _httpClient;
    private readonly PawSharpOptions _options;
    private readonly ILogger<DiscordRestClient> _logger;
    private readonly IAdvancedRateLimiter _rateLimiter;
    private DateTimeOffset _globalReset = DateTimeOffset.MinValue;

    public DiscordRestClient(HttpClient httpClient, PawSharpOptions options, ILogger<DiscordRestClient> logger, IAdvancedRateLimiter rateLimiter)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _rateLimiter = rateLimiter;
        
        // Set base address and auth header
        _httpClient.BaseAddress = new Uri($"https://discord.com/api/v{_options.ApiVersion}/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PawSharp", "1.0"));
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null);
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, content);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent content, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, content, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Put, endpoint, content);
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent content, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Put, endpoint, content, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Delete, endpoint, null);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Delete, endpoint, null, reason, cancellationToken);
    }
    
    public async Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content)
    {
        return await SendRequestAsync(HttpMethod.Patch, endpoint, content);
    }

    public async Task<HttpResponseMessage> PatchAsync(string endpoint, HttpContent content, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Patch, endpoint, content, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetCurrentUserAsync()
    {
        return await GetAsync("users/@me");
    }
    
    // User operations
    public async Task<User?> GetUserAsync(ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
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
        // Validate input
        if (limit < 1 || limit > 200)
        {
            throw new ValidationException("Limit must be between 1 and 200", nameof(limit), limit);
        }
        if (before.HasValue) SnowflakeValidator.ValidateSnowflake(before.Value, nameof(before));
        if (after.HasValue) SnowflakeValidator.ValidateSnowflake(after.Value, nameof(after));

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
        // Validate input
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        ContentValidator.ValidateMessageContent(request.Content);

        // Validate embeds if present
        if (request.Embeds != null)
        {
            foreach (var embed in request.Embeds)
            {
                ContentValidator.ValidateEmbedTitle(embed.Title);
                ContentValidator.ValidateEmbedDescription(embed.Description);
                EmbedValidator.ValidateEmbedFieldCount(embed.Fields?.Count ?? 0);
                EmbedValidator.ValidateEmbedHasContent(embed.Title, embed.Description, embed.Fields);
            }
        }

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
        // Validate input
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageId));
        if (request.Content != null)
        {
            ContentValidator.ValidateMessageContent(request.Content);
        }

        // Validate embeds if present
        if (request.Embeds != null)
        {
            foreach (var embed in request.Embeds)
            {
                ContentValidator.ValidateEmbedTitle(embed.Title);
                ContentValidator.ValidateEmbedDescription(embed.Description);
                EmbedValidator.ValidateEmbedFieldCount(embed.Fields?.Count ?? 0);
                EmbedValidator.ValidateEmbedHasContent(embed.Title, embed.Description, embed.Fields);
            }
        }

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
        // Validate input
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        if (limit < 1 || limit > 100)
        {
            throw new ValidationException("Limit must be between 1 and 100", nameof(limit), limit);
        }
        if (around.HasValue) SnowflakeValidator.ValidateSnowflake(around.Value, nameof(around));
        if (before.HasValue) SnowflakeValidator.ValidateSnowflake(before.Value, nameof(before));
        if (after.HasValue) SnowflakeValidator.ValidateSnowflake(after.Value, nameof(after));

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
        // Validate input
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        if (messageIds == null || messageIds.Count == 0 || messageIds.Count > 100)
        {
            throw new ValidationException("Message IDs list must contain between 1 and 100 IDs", nameof(messageIds), messageIds?.Count ?? 0);
        }
        foreach (var messageId in messageIds)
        {
            SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageIds));
        }

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
    
    // Application Command Permissions operations
    public async Task<List<ApplicationCommandPermissions>?> GetGuildApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId)
    {
        var response = await GetAsync($"applications/{applicationId}/guilds/{guildId}/commands/permissions");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommandPermissions>>();
        }
        return null;
    }
    
    public async Task<ApplicationCommandPermissions?> GetApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId)
    {
        var response = await GetAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}/permissions");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommandPermissions>();
        }
        return null;
    }
    
    public async Task<ApplicationCommandPermissions?> EditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, ulong commandId, List<ApplicationCommandPermission> permissions)
    {
        var content = new StringContent(JsonSerializer.Serialize(permissions), Encoding.UTF8, "application/json");
        var response = await PutAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}/permissions", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommandPermissions>();
        }
        return null;
    }
    
    public async Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions)
    {
        var content = new StringContent(JsonSerializer.Serialize(permissions), Encoding.UTF8, "application/json");
        var response = await PutAsync($"applications/{applicationId}/guilds/{guildId}/commands/permissions", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommandPermissions>>();
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

    // Stage Instance operations
    public async Task<StageInstance?> CreateStageInstanceAsync(CreateStageInstanceRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync("stage-instances", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        return null;
    }

    public async Task<StageInstance?> GetStageInstanceAsync(ulong channelId)
    {
        var response = await GetAsync($"stage-instances/{channelId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        return null;
    }

    public async Task<StageInstance?> ModifyStageInstanceAsync(ulong channelId, ModifyStageInstanceRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"stage-instances/{channelId}", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        return null;
    }

    public async Task<bool> DeleteStageInstanceAsync(ulong channelId)
    {
        var response = await DeleteAsync($"stage-instances/{channelId}");
        return response.IsSuccessStatusCode;
    }

    // Sticker operations
    public async Task<Sticker?> GetStickerAsync(ulong stickerId)
    {
        var response = await GetAsync($"stickers/{stickerId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Sticker>();
        return null;
    }

    public async Task<List<StickerPack>?> GetNitroStickerPacksAsync()
    {
        var response = await GetAsync("sticker-packs");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<StickerPack>>();
        return null;
    }

    public async Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/stickers");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<Sticker>>();
        return null;
    }

    public async Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId)
    {
        var response = await GetAsync($"guilds/{guildId}/stickers/{stickerId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Sticker>();
        return null;
    }

    public async Task<Sticker?> CreateGuildStickerAsync(ulong guildId, CreateGuildStickerRequest request)
    {
        // Sticker creation requires multipart/form-data with the file bytes
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(request.Name), "name");
        formContent.Add(new StringContent(request.Description), "description");
        formContent.Add(new StringContent(request.Tags), "tags");
        if (request.FileData != null && request.FileName != null)
        {
            var fileBytes = new ByteArrayContent(request.FileData);
            fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                request.ContentType ?? "image/png");
            formContent.Add(fileBytes, "file", request.FileName);
        }
        var response = await PostAsync($"guilds/{guildId}/stickers", formContent);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Sticker>();
        return null;
    }

    public async Task<Sticker?> ModifyGuildStickerAsync(ulong guildId, ulong stickerId, ModifyGuildStickerRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/stickers/{stickerId}", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Sticker>();
        return null;
    }

    public async Task<bool> DeleteGuildStickerAsync(ulong guildId, ulong stickerId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/stickers/{stickerId}");
        return response.IsSuccessStatusCode;
    }

    // DM operations
    public async Task<Channel?> CreateDmAsync(ulong recipientId)
    {
        var payload = new { recipient_id = recipientId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await PostAsync("users/@me/channels", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Channel>();
        return null;
    }

    // Gateway Bot info
    public async Task<GatewayBotInfo?> GetGatewayBotAsync()
    {
        var response = await GetAsync("gateway/bot");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GatewayBotInfo>();
        return null;
    }

    // Voice Region operations
    public async Task<List<VoiceRegion>?> GetVoiceRegionsAsync()
    {
        var response = await GetAsync("voice/regions");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<VoiceRegion>>();
        return null;
    }

    public async Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/regions");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<VoiceRegion>>();
        return null;
    }

    // Message crosspost
    public async Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await PostAsync($"channels/{channelId}/messages/{messageId}/crosspost", null!);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Message>();
        return null;
    }

    // Channel permission overwrites
    public async Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PutAsync($"channels/{channelId}/permissions/{overwriteId}", content);
        return response.IsSuccessStatusCode;
    }

    // Current user connections
    public async Task<List<UserConnection>?> GetCurrentUserConnectionsAsync()
    {
        var response = await GetAsync("users/@me/connections");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<UserConnection>>();
        return null;
    }

    // ── Alpha12 endpoints ─────────────────────────────────────────────────────

    // Guild member search
    public async Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int? limit = null)
    {
        var queryParams = new List<string> { $"query={Uri.EscapeDataString(query)}" };
        if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
        var response = await GetAsync($"guilds/{guildId}/members/search?{string.Join("&", queryParams)}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<GuildMember>>();
        return null;
    }

    // Modify current member
    public async Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick)
    {
        var payload = new { nick };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/members/@me", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        return null;
    }

    // Poll operations
    public async Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null)
    {
        var queryParams = new List<string>();
        if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
        if (after.HasValue) queryParams.Add($"after={after.Value}");
        var endpoint = $"channels/{channelId}/polls/{messageId}/answers/{answerId}";
        if (queryParams.Any()) endpoint += "?" + string.Join("&", queryParams);
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<PollVotersResponse>();
            return result?.Users;
        }
        return null;
    }

    public async Task<Message?> EndPollAsync(ulong channelId, ulong messageId)
    {
        var response = await PostAsync($"channels/{channelId}/polls/{messageId}/expire", new StringContent("{}", Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Message>();
        return null;
    }

    // SKU operations
    public async Task<List<Sku>?> ListSkusAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/skus");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<Sku>>();
        return null;
    }

    // Entitlement operations
    public async Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null)
    {
        var queryParams = new List<string>();
        if (userId.HasValue) queryParams.Add($"user_id={userId.Value}");
        if (skuIds?.Any() == true) queryParams.Add($"sku_ids={string.Join(",", skuIds)}");
        if (before.HasValue) queryParams.Add($"before={before.Value}");
        if (after.HasValue) queryParams.Add($"after={after.Value}");
        if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
        if (guildId.HasValue) queryParams.Add($"guild_id={guildId.Value}");
        if (excludeEnded.HasValue) queryParams.Add($"exclude_ended={excludeEnded.Value.ToString().ToLower()}");
        var endpoint = $"applications/{applicationId}/entitlements";
        if (queryParams.Any()) endpoint += "?" + string.Join("&", queryParams);
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<Entitlement>>();
        return null;
    }

    public async Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId)
    {
        var response = await GetAsync($"applications/{applicationId}/entitlements/{entitlementId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Entitlement>();
        return null;
    }

    public async Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"applications/{applicationId}/entitlements", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Entitlement>();
        return null;
    }

    public async Task<bool> DeleteTestEntitlementAsync(ulong applicationId, ulong entitlementId)
    {
        var response = await DeleteAsync($"applications/{applicationId}/entitlements/{entitlementId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ConsumeEntitlementAsync(ulong applicationId, ulong entitlementId)
    {
        var response = await PostAsync($"applications/{applicationId}/entitlements/{entitlementId}/consume", new StringContent("{}", Encoding.UTF8, "application/json"));
        return response.IsSuccessStatusCode;
    }

    // Subscription operations
    public async Task<List<Subscription>?> ListSkuSubscriptionsAsync(ulong skuId, ulong? before = null, ulong? after = null, int? limit = null, ulong? userId = null)
    {
        var queryParams = new List<string>();
        if (before.HasValue) queryParams.Add($"before={before.Value}");
        if (after.HasValue) queryParams.Add($"after={after.Value}");
        if (limit.HasValue) queryParams.Add($"limit={limit.Value}");
        if (userId.HasValue) queryParams.Add($"user_id={userId.Value}");
        var endpoint = $"skus/{skuId}/subscriptions";
        if (queryParams.Any()) endpoint += "?" + string.Join("&", queryParams);
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<Subscription>>();
        return null;
    }

    public async Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId)
    {
        var response = await GetAsync($"skus/{skuId}/subscriptions/{subscriptionId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Subscription>();
        return null;
    }

    // Soundboard operations
    public async Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync()
    {
        var response = await GetAsync("soundboard-default-sounds");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<SoundboardSound>>();
        return null;
    }

    public async Task<List<SoundboardSound>?> ListGuildSoundboardSoundsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/soundboard-sounds");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<GuildSoundboardSoundsResponse>();
            return result?.Items;
        }
        return null;
    }

    public async Task<SoundboardSound?> GetGuildSoundboardSoundAsync(ulong guildId, ulong soundId)
    {
        var response = await GetAsync($"guilds/{guildId}/soundboard-sounds/{soundId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        return null;
    }

    public async Task<SoundboardSound?> CreateGuildSoundboardSoundAsync(ulong guildId, CreateGuildSoundboardSoundRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PostAsync($"guilds/{guildId}/soundboard-sounds", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        return null;
    }

    public async Task<SoundboardSound?> ModifyGuildSoundboardSoundAsync(ulong guildId, ulong soundId, ModifyGuildSoundboardSoundRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PatchAsync($"guilds/{guildId}/soundboard-sounds/{soundId}", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        return null;
    }

    public async Task<bool> DeleteGuildSoundboardSoundAsync(ulong guildId, ulong soundId)
    {
        var response = await DeleteAsync($"guilds/{guildId}/soundboard-sounds/{soundId}");
        return response.IsSuccessStatusCode;
    }

    // Guild Onboarding operations
    public async Task<GuildOnboarding?> GetGuildOnboardingAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/onboarding");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GuildOnboarding>();
        return null;
    }

    public async Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request)
    {
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await PutAsync($"guilds/{guildId}/onboarding", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GuildOnboarding>();
        return null;
    }

    // Application Role Connection Metadata
    public async Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/role-connections/metadata");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ApplicationRoleConnectionMetadata>>();
        return null;
    }

    public async Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records)
    {
        var content = new StringContent(JsonSerializer.Serialize(records), Encoding.UTF8, "application/json");
        var response = await PutAsync($"applications/{applicationId}/role-connections/metadata", content);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ApplicationRoleConnectionMetadata>>();
        return null;
    }

    private const int MaxRateLimitRetries = 5;

    private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default, int retryCount = 0)
    {
        // Global rate limit check
        if (DateTimeOffset.UtcNow < _globalReset)
        {
            var delay = _globalReset - DateTimeOffset.UtcNow;
            _logger.LogWarning("Global rate limit hit, delaying {Delay}", delay);
            await Task.Delay(delay);
        }

        // Per-route rate limit coordination
        string? bucketHash = null;
        string route;
        try
        {
            var path = endpoint.Split('?')[0];
            route = $"{method.Method} {path}";
            await _rateLimiter.WaitForRateLimitAsync(route);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate limiter wait failed, proceeding with request");
            route = $"{method.Method} {endpoint.Split('?')[0]}";
        }

        var request = new HttpRequestMessage(method, endpoint) { Content = content };
        
        // Add audit log reason header if provided
        if (!string.IsNullOrEmpty(reason))
        {
            request.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
        }
        
        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Parse rate limit headers and update limiter
        ParseAndUpdateRateLimits(response, route, ref bucketHash);

        // Handle rate limiting
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
            _logger.LogWarning("Rate limited, retrying after {RetryAfter}", retryAfter);
            
            // Update limiter with 429 info for bucket-aware retry
            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var bucketValues))
            {
                bucketHash = bucketValues.FirstOrDefault();
            }
            var isGlobal = response.Headers.TryGetValues("X-RateLimit-Global", out var globalValues) && 
                          bool.Parse(globalValues.FirstOrDefault() ?? "false");
            _rateLimiter.UpdateRateLimits(route, bucketHash, 0, DateTimeOffset.UtcNow.Add(retryAfter), isGlobal);
            
            // Wait for rate limiter to allow retry
            await _rateLimiter.WaitForRateLimitAsync(route, bucketHash);

            if (retryCount >= MaxRateLimitRetries)
            {
                _logger.LogError("Rate limit retry limit ({Max}) exceeded for {Method} {Endpoint}", MaxRateLimitRetries, method, endpoint);
                return response; // Return the 429 response rather than looping forever
            }

            return await SendRequestAsync(method, endpoint, content, reason, cancellationToken, retryCount + 1); // Retry
        }

        // Mark request as complete
        _rateLimiter.MarkRequestComplete(route, bucketHash);

        if (response.Headers.TryGetValues("X-RateLimit-Global", out var globalVals) && bool.Parse(globalVals.FirstOrDefault() ?? "false"))
        {
            _globalReset = DateTimeOffset.UtcNow.AddSeconds(double.Parse(response.Headers.GetValues("Retry-After").First()));
        }

        return response;
    }

    private void ParseAndUpdateRateLimits(HttpResponseMessage response, string route, ref string? bucketHash)
    {
        try
        {
            string? bucket = null;
            int? remaining = null;
            DateTimeOffset? resetAt = null;
            bool isGlobal = false;

            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var bucketValues))
            {
                bucket = bucketValues.FirstOrDefault();
                bucketHash = bucket;
            }

            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
            {
                if (int.TryParse(remainingValues.FirstOrDefault(), out var rem))
                {
                    remaining = rem;
                }
            }

            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
            {
                if (double.TryParse(resetValues.FirstOrDefault(), out var resetTimestamp))
                {
                    resetAt = DateTimeOffset.FromUnixTimeSeconds((long)resetTimestamp);
                }
            }

            if (response.Headers.TryGetValues("X-RateLimit-Global", out var globalValues))
            {
                isGlobal = bool.Parse(globalValues.FirstOrDefault() ?? "false");
            }

            if (remaining.HasValue || resetAt.HasValue || isGlobal)
            {
                _rateLimiter.UpdateRateLimits(route, bucket, remaining, resetAt, isGlobal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse rate limit headers");
        }
    }
}