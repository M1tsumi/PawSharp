#nullable enable
#pragma warning disable IDE0011
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PawSharp.API.Exceptions;
using PawSharp.API.Interfaces;
using PawSharp.API.Models;
using PawSharp.API.RateLimit;
using PawSharp.API.Security;
using PawSharp.Core.Entities;
using PawSharp.Core.Exceptions;
using PawSharp.Core.Models;
using PawSharp.Core.Validation;

namespace PawSharp.API.Clients;

/// <summary>
/// Implementation of Discord REST API client with rate limiting.
/// </summary>
public class DiscordRestClient : IDiscordRestClient, IRateLimitTelemetrySource
{
    /// <summary>
    /// Shared serializer options: Discord's REST API requires lowercase snake_case field names.
    /// Using JsonNamingPolicy.SnakeCaseLower means C# properties like "GuildId" become
    /// "guild_id" automatically, even without [JsonPropertyName] on every request model.
    /// Null values are omitted to keep payloads minimal.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling              = JsonNumberHandling.AllowReadingFromString,
        // Enable source generator for better AOT compatibility
        TypeInfoResolver            = PawSharp.API.Serialization.PawSharpApiJsonContext.Default
    };

    /// <summary>Wraps an object as a UTF-8 JSON <see cref="StringContent"/> using Discord-compatible serializer options.</summary>
    private static StringContent JsonContent(object obj)
        => new(JsonSerializer.Serialize(obj, _jsonOptions), Encoding.UTF8, "application/json");

    private readonly HttpClient _httpClient;
    private readonly PawSharpOptions _options;
    private readonly ILogger<DiscordRestClient> _logger;
    private readonly IAdvancedRateLimiter _rateLimiter;
    private DateTimeOffset _globalReset = DateTimeOffset.MinValue;

    /// <inheritdoc />
    public event EventHandler<RateLimitTelemetryEvent>? RateLimitObserved;

    public DiscordRestClient(HttpClient httpClient, PawSharpOptions options, ILogger<DiscordRestClient> logger, IAdvancedRateLimiter rateLimiter)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _rateLimiter = rateLimiter;

        // Set base address and user-agent.
        // Authorization is NOT set on DefaultRequestHeaders; it is added per-request
        // in SendRequestAsync to scope credentials tightly and prevent accidental exposure.
        _httpClient.BaseAddress = new Uri($"https://discord.com/api/v{_options.ApiVersion}/");
        // Discord requires the User-Agent format:  DiscordBot ($url, $versionNumber)
        // Requests without a valid User-Agent may be blocked by Cloudflare.
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordBot (https://github.com/M1tsumi/Pawsharp, 1.1.0-alpha.1)");

        // Apply timeout configuration if specified
        if (_options.RestApi.TimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.RestApi.TimeoutSeconds);
        }
    }

    /// <summary>
    /// Convenience overload — creates a default <see cref="AdvancedRateLimiter"/> internally.
    /// Consumers that register <c>DiscordRestClient</c> directly (e.g. via <c>AddHttpClient</c>)
    /// without calling <c>AddAdvancedRateLimiter()</c> will use this overload automatically.
    /// </summary>
    public DiscordRestClient(HttpClient httpClient, PawSharpOptions options, ILogger<DiscordRestClient> logger)
        : this(httpClient, options, logger, new AdvancedRateLimiter())
    {
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null);
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Get, endpoint, null, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent? content)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, content);
    }

    public async Task<HttpResponseMessage> PostAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, content, reason, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent? content)
    {
        return await SendRequestAsync(HttpMethod.Put, endpoint, content);
    }

    public async Task<HttpResponseMessage> PutAsync(string endpoint, HttpContent? content, string? reason = null, CancellationToken cancellationToken = default)
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

    public async Task<HttpResponseMessage> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await GetAsync("users/@me", null, cancellationToken);
    }
    
    // User operations
    public async Task<User?> GetUserAsync(ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await GetAsync($"users/{userId}");
        return await HandleApiResponseAsync<User>("GetUserAsync", response);
    }

    public async Task<User?> GetUserAsync(ulong userId, CancellationToken cancellationToken)
    {
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await GetAsync($"users/{userId}", null, cancellationToken);
        return await HandleApiResponseAsync<User>("GetUserAsync", response);
    }
    
    public async Task<HttpResponseMessage> ModifyCurrentUserAsync(string? username = null, string? avatar = null, string? banner = null, string? avatarDecorationData = null)
    {
        var payload = new { username, avatar, banner, avatar_decoration_data = avatarDecorationData };
        var content = JsonContent(payload);
        return await PatchAsync("users/@me", content);
    }
    
    /// <summary>
    /// Gets the current user's guilds.
    /// </summary>
    /// <param name="limit">Maximum number of guilds to return (1-200). Default is 200.</param>
    /// <param name="before">Get guilds before this guild ID.</param>
    /// <param name="after">Get guilds after this guild ID.</param>
    /// <returns>A list of guilds, or null if the request fails.</returns>
    public async Task<List<Guild>?> GetCurrentUserGuildsAsync(int limit = 200, ulong? before = null, ulong? after = null)
    {
        // Validate input
        if (limit < 1 || limit > 200)
        {
            throw new ValidationException("Limit must be between 1 and 200", nameof(limit), limit);
        }
        if (before.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(before.Value, nameof(before));
        }

        if (after.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(after.Value, nameof(after));
        }

        var queryParams = new List<string>();
        if (limit != 200)
        {
            queryParams.Add($"limit={limit}");
        }

        if (before.HasValue)
        {
            queryParams.Add($"before={before.Value}");
        }

        if (after.HasValue)
        {
            queryParams.Add($"after={after.Value}");
        }

        var endpoint = "users/@me/guilds";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Guild>>();
        }
        return null;
    }
    
    public async Task<bool> LeaveGuildAsync(ulong guildId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var response = await DeleteAsync($"users/@me/guilds/{guildId}");
        return response.IsSuccessStatusCode;
    }
    
    // Message operations
    /// <summary>
    /// Creates a new message in a channel.
    /// </summary>
    /// <param name="channelId">The channel ID to send the message to.</param>
    /// <param name="request">The message creation request.</param>
    /// <returns>The created message, or null if the request fails.</returns>
    public async Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
    {
        ValidateSnowflake(channelId, nameof(channelId));

        // Content is optional when embeds, components, or a poll are present.
        // Only validate the text when it is explicitly supplied.
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

        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/messages", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }

    public async Task<Message?> CreateMessageAsync(ulong channelId, CreateMessageRequest request, CancellationToken cancellationToken)
    {
        ValidateSnowflake(channelId, nameof(channelId));

        // Content is optional when embeds, components, or a poll are present.
        // Only validate the text when it is explicitly supplied.
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

        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/messages", content, null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }

    /// <summary>
    /// Forwards a message from one channel to another.
    /// </summary>
    /// <param name="targetChannelId">The channel ID to forward the message to.</param>
    /// <param name="sourceChannelId">The channel ID of the source message.</param>
    /// <param name="sourceMessageId">The message ID to forward.</param>
    /// <param name="content">Optional content to add to the forwarded message.</param>
    /// <param name="failIfNotExists">Whether to fail if the source message doesn't exist.</param>
    /// <returns>The forwarded message, or null if the request fails.</returns>
    public async Task<Message?> ForwardMessageAsync(
        ulong targetChannelId,
        ulong sourceChannelId,
        ulong sourceMessageId,
        string? content = null,
        bool failIfNotExists = true)
    {
        SnowflakeValidator.ValidateSnowflake(targetChannelId, nameof(targetChannelId));
        SnowflakeValidator.ValidateSnowflake(sourceChannelId, nameof(sourceChannelId));
        SnowflakeValidator.ValidateSnowflake(sourceMessageId, nameof(sourceMessageId));

        if (content != null)
        {
            ContentValidator.ValidateMessageContent(content);
        }

        var request = new CreateMessageRequest
        {
            Content = content,
            MessageReference = MessageReference.Forward(sourceChannelId, sourceMessageId, failIfNotExists)
        };

        return await CreateMessageAsync(targetChannelId, request);
    }

    /// <summary>
    /// Sends a file to a channel.
    /// </summary>
    /// <param name="channelId">The channel ID to send the file to.</param>
    /// <param name="fileStream">The file stream to send.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="messageRequest">Optional message request to include with the file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created message, or null if the request fails.</returns>
    public async Task<Message?> SendFileAsync(
        ulong channelId,
        Stream fileStream,
        string fileName,
        CreateMessageRequest? messageRequest = null,
        CancellationToken cancellationToken = default)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));

        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "files[0]", fileName);

        if (messageRequest is not null)
        {
            var json = JsonSerializer.Serialize(messageRequest, _jsonOptions);
            form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");
        }

        var response = await PostAsync($"channels/{channelId}/messages", form, cancellationToken: cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>(_jsonOptions, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Sends up to 10 file attachments in a single message.
    /// Each element is a <c>(Stream stream, string fileName)</c> pair.
    /// </summary>
    public async Task<Message?> SendFilesAsync(
        ulong channelId,
        IEnumerable<(Stream Stream, string FileName)> files,
        CreateMessageRequest? messageRequest = null,
        CancellationToken cancellationToken = default)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));

        using var form = new MultipartFormDataContent();

        int index = 0;
        foreach (var (stream, fileName) in files)
        {
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, $"files[{index}]", fileName);
            index++;
        }

        if (messageRequest is not null)
        {
            var json = JsonSerializer.Serialize(messageRequest, _jsonOptions);
            form.Add(new StringContent(json, Encoding.UTF8, "application/json"), "payload_json");
        }

        var response = await PostAsync($"channels/{channelId}/messages", form, cancellationToken: cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>(_jsonOptions, cancellationToken);
        }

        return null;
    }

    public async Task<Message?> GetMessageAsync(ulong channelId, ulong messageId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageId));
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

        var content = JsonContent(request);
        var response = await PatchAsync($"channels/{channelId}/messages/{messageId}", content);
        return await HandleApiResponseAsync<Message>("EditMessageAsync", response);
    }
    
    public async Task<bool> DeleteMessageAsync(ulong channelId, ulong messageId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageId));
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Message>?> GetChannelMessagesAsync(ulong channelId, int limit = 50, ulong? around = null, ulong? before = null, ulong? after = null)
    {
        // Validate input
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        if (limit < 1 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100");
        }

        var queryParams = new List<string>();
        queryParams.Add($"limit={limit}");
        if (around.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(around.Value, nameof(around));
            queryParams.Add($"around={around.Value}");
        }
        if (before.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(before.Value, nameof(before));
            queryParams.Add($"before={before.Value}");
        }
        if (after.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(after.Value, nameof(after));
            queryParams.Add($"after={after.Value}");
        }

        var response = await GetAsync($"channels/{channelId}/messages?{string.Join("&", queryParams)}");
        return await HandleApiResponseAsync<List<Message>>("GetChannelMessagesAsync", response);
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
        var content = JsonContent(payload);
        var response = await PostAsync($"channels/{channelId}/messages/bulk-delete", content);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> PinMessageAsync(ulong channelId, ulong messageId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageId));
        var response = await PutAsync($"channels/{channelId}/pins/{messageId}", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> UnpinMessageAsync(ulong channelId, ulong messageId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        SnowflakeValidator.ValidateSnowflake(messageId, nameof(messageId));
        var response = await DeleteAsync($"channels/{channelId}/pins/{messageId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Message>?> GetPinnedMessagesAsync(ulong channelId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        var response = await GetAsync($"channels/{channelId}/pins");
        return await HandleApiResponseAsync<List<Message>>("GetPinnedMessagesAsync", response);
    }
    
    public async Task<bool> TriggerTypingIndicatorAsync(ulong channelId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        var response = await PostAsync($"channels/{channelId}/typing", null);
        return response.IsSuccessStatusCode;
    }
    
    // Channel operations
    public async Task<Channel?> GetChannelAsync(ulong channelId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        var response = await GetAsync($"channels/{channelId}");
        return await HandleApiResponseAsync<Channel>("GetChannelAsync", response);
    }

    public async Task<Channel?> GetChannelAsync(ulong channelId, CancellationToken cancellationToken)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        var response = await GetAsync($"channels/{channelId}", null, cancellationToken);
        return await HandleApiResponseAsync<Channel>("GetChannelAsync", response);
    }
    
    public async Task<Channel?> ModifyChannelAsync(ulong channelId, ModifyChannelRequest request)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        var content = JsonContent(request);
        var response = await PatchAsync($"channels/{channelId}", content);
        return await HandleApiResponseAsync<Channel>("ModifyChannelAsync", response);
    }
    
    public async Task<bool> DeleteChannelAsync(ulong channelId)
    {
        SnowflakeValidator.ValidateSnowflake(channelId, nameof(channelId));
        var response = await DeleteAsync($"channels/{channelId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<Channel?> CreateGuildChannelAsync(ulong guildId, CreateChannelRequest request)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/channels", content);
        return await HandleApiResponseAsync<Channel>("CreateGuildChannelAsync", response);
    }
    
    public async Task<List<Invite>?> GetChannelInvitesAsync(ulong channelId)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        var response = await GetAsync($"channels/{channelId}/invites");
        return await HandleApiResponseAsync<List<Invite>>("GetChannelInvitesAsync", response);
    }
    
    public async Task<Invite?> CreateChannelInviteAsync(ulong channelId, CreateInviteRequest request)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/invites", content);
        return await HandleApiResponseAsync<Invite>("CreateChannelInviteAsync", response);
    }
    
    public async Task<bool> DeleteChannelPermissionAsync(ulong channelId, ulong overwriteId)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        ValidateSnowflake(overwriteId, nameof(overwriteId));
        var response = await DeleteAsync($"channels/{channelId}/permissions/{overwriteId}");
        return response.IsSuccessStatusCode;
    }
    
    // Guild operations
    public async Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts = false)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var endpoint = $"guilds/{guildId}";
        if (withCounts)
        {
            endpoint += "?with_counts=true";
        }
        var response = await GetAsync(endpoint);
        return await HandleApiResponseAsync<Guild>("GetGuildAsync", response);
    }

    public async Task<Guild?> GetGuildAsync(ulong guildId, bool withCounts, CancellationToken cancellationToken)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var endpoint = $"guilds/{guildId}";
        if (withCounts)
        {
            endpoint += "?with_counts=true";
        }
        var response = await GetAsync(endpoint, null, cancellationToken);
        return await HandleApiResponseAsync<Guild>("GetGuildAsync", response);
    }
    
    public async Task<Guild?> CreateGuildAsync(CreateGuildRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync("guilds", content);
        return await HandleApiResponseAsync<Guild>("CreateGuildAsync", response);
    }
    
    public async Task<Guild?> ModifyGuildAsync(ulong guildId, ModifyGuildRequest request)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}", content);
        return await HandleApiResponseAsync<Guild>("ModifyGuildAsync", response);
    }
    
    public async Task<bool> DeleteGuildAsync(ulong guildId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var response = await DeleteAsync($"guilds/{guildId}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Modifies the guild's MFA level (requires the current user to be the guild owner).
    /// Returns the updated MFA level on success.
    /// </summary>
    public async Task<int?> ModifyGuildMfaLevelAsync(ulong guildId, int level)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var content = JsonContent(new ModifyGuildMfaLevelRequest { Level = level });
        var response = await PostAsync($"guilds/{guildId}/mfa", content);
        if (response.IsSuccessStatusCode)
        {
            using var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("level", out var lv))
            {
                return lv.GetInt32();
            }
        }
        return null;
    }
    
    public async Task<List<Channel>?> GetGuildChannelsAsync(ulong guildId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var response = await GetAsync($"guilds/{guildId}/channels");
        return await HandleApiResponseAsync<List<Channel>>("GetGuildChannelsAsync", response);
    }
    
    public async Task<List<GuildMember>?> ListGuildMembersAsync(ulong guildId, int limit = 1, ulong? after = null)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var queryParams = new List<string>();
        queryParams.Add($"limit={limit}");
        if (after.HasValue)
        {
            SnowflakeValidator.ValidateSnowflake(after.Value, nameof(after));
            queryParams.Add($"after={after.Value}");
        }
        var qs = string.Join("&", queryParams);
        var response = await GetAsync($"guilds/{guildId}/members?{qs}");
        return await HandleApiResponseAsync<List<GuildMember>>("ListGuildMembersAsync", response);
    }
    
    public async Task<GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await GetAsync($"guilds/{guildId}/members/{userId}");
        return await HandleApiResponseAsync<GuildMember>("GetGuildMemberAsync", response);
    }
    
    public async Task<GuildMember?> AddGuildMemberAsync(ulong guildId, ulong userId, AddGuildMemberRequest request)
    {
        var content = JsonContent(request);
        var response = await PutAsync($"guilds/{guildId}/members/{userId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }
        return null;
    }
    
    public async Task<GuildMember?> ModifyGuildMemberAsync(ulong guildId, ulong userId, ModifyGuildMemberRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/members/{userId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }
        return null;
    }
    
    public async Task<bool> RemoveGuildMemberAsync(ulong guildId, ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await DeleteAsync($"guilds/{guildId}/members/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<Ban>?> GetGuildBansAsync(ulong guildId, ulong? before = null, ulong? after = null, int? limit = null)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var qs = new System.Text.StringBuilder();
        if (before.HasValue)
        {
            qs.Append($"before={before}&");
        }

        if (after.HasValue)
        {
            qs.Append($"after={after}&");
        }

        if (limit.HasValue)
        {
            qs.Append($"limit={limit}&");
        }

        var query = qs.Length > 0 ? "?" + qs.ToString().TrimEnd('&') : string.Empty;
        var response = await GetAsync($"guilds/{guildId}/bans{query}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Ban>>();
        }
        return null;
    }
    
    public async Task<Ban?> GetGuildBanAsync(ulong guildId, ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await GetAsync($"guilds/{guildId}/bans/{userId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Ban>();
        }
        return null;
    }
    
    public async Task<bool> CreateGuildBanAsync(ulong guildId, ulong userId, int? deleteMessageDays = null, string? reason = null)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var payload = new { delete_message_days = deleteMessageDays, reason };
        var content = JsonContent(payload);
        var response = await PutAsync($"guilds/{guildId}/bans/{userId}", content);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RemoveGuildBanAsync(ulong guildId, ulong userId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        SnowflakeValidator.ValidateSnowflake(userId, nameof(userId));
        var response = await DeleteAsync($"guilds/{guildId}/bans/{userId}");
        return response.IsSuccessStatusCode;
    }
    
    // Role operations
    public async Task<List<Role>?> GetGuildRolesAsync(ulong guildId)
    {
        SnowflakeValidator.ValidateSnowflake(guildId, nameof(guildId));
        var response = await GetAsync($"guilds/{guildId}/roles");
        return await HandleApiResponseAsync<List<Role>>("GetGuildRolesAsync", response);
    }
    
    public async Task<Role?> CreateGuildRoleAsync(ulong guildId, CreateRoleRequest request)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/roles", content);
        return await HandleApiResponseAsync<Role>("CreateGuildRoleAsync", response);
    }
    
    public async Task<Role?> ModifyGuildRoleAsync(ulong guildId, ulong roleId, ModifyRoleRequest request)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        ValidateSnowflake(roleId, nameof(roleId));
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/roles/{roleId}", content);
        return await HandleApiResponseAsync<Role>("ModifyGuildRoleAsync", response);
    }
    
    public async Task<bool> DeleteGuildRoleAsync(ulong guildId, ulong roleId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        ValidateSnowflake(roleId, nameof(roleId));
        var response = await DeleteAsync($"guilds/{guildId}/roles/{roleId}");
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        ValidateSnowflake(userId, nameof(userId));
        ValidateSnowflake(roleId, nameof(roleId));
        var response = await PutAsync($"guilds/{guildId}/members/{userId}/roles/{roleId}", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> RemoveGuildMemberRoleAsync(ulong guildId, ulong userId, ulong roleId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        ValidateSnowflake(userId, nameof(userId));
        ValidateSnowflake(roleId, nameof(roleId));
        var response = await DeleteAsync($"guilds/{guildId}/members/{userId}/roles/{roleId}");
        return response.IsSuccessStatusCode;
    }
    
    // Interaction operations
    public async Task<bool> CreateInteractionResponseAsync(ulong interactionId, string interactionToken, InteractionResponse response)
    {
        var content = JsonContent(response);
        var httpResponse = await PostAsync($"interactions/{interactionId}/{interactionToken}/callback", content);
        return httpResponse.IsSuccessStatusCode;
    }

    public async Task<Message?> GetOriginalInteractionResponseAsync(string applicationId, string interactionToken)
    {
        var response = await GetAsync($"webhooks/{applicationId}/{interactionToken}/messages/@original");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }

        return null;
    }
    
    public async Task<HttpResponseMessage> EditOriginalInteractionResponseAsync(string applicationId, string interactionToken, EditMessageRequest request)
    {
        var content = JsonContent(request);
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
        ValidateSnowflake(channelId, nameof(channelId));
        ValidateSnowflake(messageId, nameof(messageId));
        var response = await PutAsync($"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}/@me", null);
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
        var content = JsonContent(request);
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
        var content = JsonContent(request);
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
        var content = JsonContent(request);
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
        var content = JsonContent(request);
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
        var content = JsonContent(commands);
        var response = await PutAsync($"applications/{applicationId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        await LogSanitizedApiErrorAsync("BulkOverwriteGlobalApplicationCommands failed", response);
        return null;
    }
    
    public async Task<List<ApplicationCommand>?> BulkOverwriteGuildApplicationCommandsAsync(ulong applicationId, ulong guildId, List<CreateApplicationCommandRequest> commands)
    {
        var content = JsonContent(commands);
        var response = await PutAsync($"applications/{applicationId}/guilds/{guildId}/commands", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationCommand>>();
        }
        await LogSanitizedApiErrorAsync("BulkOverwriteGuildApplicationCommands failed", response);
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
        var content = JsonContent(permissions);
        var response = await PutAsync($"applications/{applicationId}/guilds/{guildId}/commands/{commandId}/permissions", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationCommandPermissions>();
        }
        return null;
    }
    
    public async Task<List<ApplicationCommandPermissions>?> BatchEditApplicationCommandPermissionsAsync(ulong applicationId, ulong guildId, List<ApplicationCommandPermissions> permissions)
    {
        var content = JsonContent(permissions);
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
        ValidateSnowflake(channelId, nameof(channelId));
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<Channel?> CreateThreadFromMessageAsync(ulong channelId, ulong messageId, CreateThreadRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/messages/{messageId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<Channel?> CreateThreadInForumAsync(ulong channelId, CreateThreadRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/threads", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }
        return null;
    }
    
    public async Task<bool> JoinThreadAsync(ulong channelId)
    {
        var response = await PutAsync($"channels/{channelId}/thread-members/@me", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> AddThreadMemberAsync(ulong channelId, ulong userId)
    {
        var response = await PutAsync($"channels/{channelId}/thread-members/{userId}", null);
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
    
    public async Task<List<ThreadMember>?> GetThreadMembersAsync(ulong channelId, bool withMember = false, ulong? after = null, int? limit = null)
    {
        var qs = new System.Text.StringBuilder();
        if (withMember)
        {
            qs.Append("with_member=true&");
        }

        if (after.HasValue)
        {
            qs.Append($"after={after}&");
        }

        if (limit.HasValue)
        {
            qs.Append($"limit={limit}&");
        }

        var query = qs.Length > 0 ? "?" + qs.ToString().TrimEnd('&') : string.Empty;
        var response = await GetAsync($"channels/{channelId}/thread-members{query}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ThreadMember>>();
        }
        return null;
    }
    
    public async Task<ActiveThreadsResponse?> GetActiveThreadsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/threads/active");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ActiveThreadsResponse>(_jsonOptions);
        }

        return null;
    }
    
    public async Task<ArchivedThreadsResponse?> GetPublicArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue)
        {
            query.Add($"before={before.Value.UtcDateTime:O}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/threads/archived/public{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ArchivedThreadsResponse>(_jsonOptions);
        }

        return null;
    }
    
    public async Task<ArchivedThreadsResponse?> GetPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue)
        {
            query.Add($"before={before.Value.UtcDateTime:O}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/threads/archived/private{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ArchivedThreadsResponse>(_jsonOptions);
        }

        return null;
    }
    
    public async Task<ArchivedThreadsResponse?> GetJoinedPrivateArchivedThreadsAsync(ulong channelId, DateTimeOffset? before = null, int? limit = null)
    {
        var query = new List<string>();
        if (before.HasValue)
        {
            query.Add($"before={before.Value.UtcDateTime:O}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"channels/{channelId}/users/@me/threads/archived/private{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ArchivedThreadsResponse>(_jsonOptions);
        }

        return null;
    }
    
    // Webhook operations
    public async Task<Webhook?> CreateWebhookAsync(ulong channelId, CreateWebhookRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/webhooks", content);
        return await HandleApiResponseAsync<Webhook>("CreateWebhookAsync", response);
    }
    
    public async Task<List<Webhook>?> GetChannelWebhooksAsync(ulong channelId)
    {
        var response = await GetAsync($"channels/{channelId}/webhooks");
        return await HandleApiResponseAsync<List<Webhook>>("GetChannelWebhooksAsync", response);
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
        var content = JsonContent(request);
        var response = await PatchAsync($"webhooks/{webhookId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Webhook>();
        }
        return null;
    }
    
    public async Task<Webhook?> ModifyWebhookWithTokenAsync(ulong webhookId, string token, ModifyWebhookRequest request)
    {
        var content = JsonContent(request);
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
        var queryParts = new List<string>();
        if (threadId.HasValue)
        {
            queryParts.Add($"thread_id={threadId.Value}");
        }

        if (request.Wait)
        {
            queryParts.Add("wait=true");
        }

        var endpoint = $"webhooks/{webhookId}/{token}";
        if (queryParts.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParts);
        }

        var content = JsonContent(request);
        var response = await PostAsync(endpoint, content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }
        return null;
    }

    public async Task<Message?> GetWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null)
    {
        var endpoint = $"webhooks/{webhookId}/{token}/messages/{messageId}";
        if (threadId.HasValue)
        {
            endpoint += $"?thread_id={threadId.Value}";
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }

        return null;
    }

    public async Task<Message?> EditWebhookMessageAsync(ulong webhookId, string token, ulong messageId, EditMessageRequest request, ulong? threadId = null)
    {
        var endpoint = $"webhooks/{webhookId}/{token}/messages/{messageId}";
        if (threadId.HasValue)
        {
            endpoint += $"?thread_id={threadId.Value}";
        }

        var content = JsonContent(request);
        var response = await PatchAsync(endpoint, content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }

        return null;
    }

    public async Task<bool> DeleteWebhookMessageAsync(ulong webhookId, string token, ulong messageId, ulong? threadId = null)
    {
        var endpoint = $"webhooks/{webhookId}/{token}/messages/{messageId}";
        if (threadId.HasValue)
        {
            endpoint += $"?thread_id={threadId.Value}";
        }

        var response = await DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Executes a webhook using the Slack-compatible endpoint.</summary>
    public async Task<bool> ExecuteSlackCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false)
    {
        var endpoint = $"webhooks/{webhookId}/{token}/slack";
        if (wait)
        {
            endpoint += "?wait=true";
        }

        var content = JsonContent(payload);
        var response = await PostAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Executes a webhook using the GitHub-compatible endpoint.</summary>
    public async Task<bool> ExecuteGitHubCompatibleWebhookAsync(ulong webhookId, string token, object payload, bool wait = false)
    {
        var endpoint = $"webhooks/{webhookId}/{token}/github";
        if (wait)
        {
            endpoint += "?wait=true";
        }

        var content = JsonContent(payload);
        var response = await PostAsync(endpoint, content);
        return response.IsSuccessStatusCode;
    }
    
    // Scheduled Event operations
    public async Task<GuildScheduledEvent?> CreateGuildScheduledEventAsync(ulong guildId, CreateGuildScheduledEventRequest request)
    {
        var content = JsonContent(request);
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
        var content = JsonContent(request);
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
        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        if (withMember.HasValue)
        {
            query.Add($"with_member={withMember.Value.ToString().ToLower()}");
        }

        if (before.HasValue)
        {
            query.Add($"before={before.Value}");
        }

        if (after.HasValue)
        {
            query.Add($"after={after.Value}");
        }

        var queryString = query.Any() ? "?" + string.Join("&", query) : "";
        
        var response = await GetAsync($"guilds/{guildId}/scheduled-events/{eventId}/users{queryString}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<User>>();
        }
        return null;
    }
    
    // Audit Log operations
    public async Task<AuditLog?> GetGuildAuditLogsAsync(ulong guildId, ulong? userId = null, AuditLogEvent? actionType = null, ulong? before = null, ulong? after = null, int? limit = null)
    {
        var query = new List<string>();
        if (userId.HasValue)
        {
            query.Add($"user_id={userId.Value}");
        }

        if (actionType.HasValue)
        {
            query.Add($"action_type={(int)actionType.Value}");
        }

        if (before.HasValue)
        {
            query.Add($"before={before.Value}");
        }

        if (after.HasValue)
        {
            query.Add($"after={after.Value}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

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
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/auto-moderation/rules", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AutoModerationRule>();
        }
        return null;
    }
    
    public async Task<AutoModerationRule?> ModifyAutoModerationRuleAsync(ulong guildId, ulong ruleId, ModifyAutoModerationRuleRequest request)
    {
        var content = JsonContent(request);
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
        var content = JsonContent(request);
        var response = await PostAsync("stage-instances", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        }

        return null;
    }

    public async Task<StageInstance?> GetStageInstanceAsync(ulong channelId)
    {
        var response = await GetAsync($"stage-instances/{channelId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        }

        return null;
    }

    public async Task<StageInstance?> ModifyStageInstanceAsync(ulong channelId, ModifyStageInstanceRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"stage-instances/{channelId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StageInstance>();
        }

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
        {
            return await response.Content.ReadFromJsonAsync<Sticker>();
        }

        return null;
    }

    public async Task<List<StickerPack>?> GetNitroStickerPacksAsync()
    {
        var response = await GetAsync("sticker-packs");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<StickerPack>>();
        }

        return null;
    }

    public async Task<List<Sticker>?> GetGuildStickersAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/stickers");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Sticker>>();
        }

        return null;
    }

    public async Task<Sticker?> GetGuildStickerAsync(ulong guildId, ulong stickerId)
    {
        var response = await GetAsync($"guilds/{guildId}/stickers/{stickerId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Sticker>();
        }

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
        {
            return await response.Content.ReadFromJsonAsync<Sticker>();
        }

        return null;
    }

    public async Task<Sticker?> ModifyGuildStickerAsync(ulong guildId, ulong stickerId, ModifyGuildStickerRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/stickers/{stickerId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Sticker>();
        }

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
        var content = JsonContent(payload);
        var response = await PostAsync("users/@me/channels", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>();
        }

        return null;
    }

    // Gateway Bot info
    public async Task<GatewayBotInfo?> GetGatewayBotAsync()
    {
        var response = await GetAsync("gateway/bot");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GatewayBotInfo>();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<GatewayInfo?> GetGatewayAsync()
    {
        // GET /gateway does not require authentication
        var response = await _httpClient.GetAsync("gateway");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GatewayInfo>();
        }

        return null;
    }

    // Voice Region operations
    public async Task<List<VoiceRegion>?> GetVoiceRegionsAsync()
    {
        var response = await GetAsync("voice/regions");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<VoiceRegion>>();
        }

        return null;
    }

    public async Task<List<VoiceRegion>?> GetGuildVoiceRegionsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/regions");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<VoiceRegion>>();
        }

        return null;
    }

    // Message crosspost
    public async Task<Message?> GetMessageAsync(ulong channelId, ulong messageId)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        ValidateSnowflake(messageId, nameof(messageId));
        var response = await GetAsync($"channels/{channelId}/messages/{messageId}");
        return await HandleApiResponseAsync<Message>("GetMessageAsync", response);
    }

    public async Task<Message?> GetMessageAsync(ulong channelId, ulong messageId, CancellationToken cancellationToken)
    {
        ValidateSnowflake(channelId, nameof(channelId));
        ValidateSnowflake(messageId, nameof(messageId));
        var response = await GetAsync($"channels/{channelId}/messages/{messageId}", null, cancellationToken);
        return await HandleApiResponseAsync<Message>("GetMessageAsync", response);
    }

    public async Task<Message?> CrosspostMessageAsync(ulong channelId, ulong messageId)
    {
        var response = await PostAsync($"channels/{channelId}/messages/{messageId}/crosspost", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }

        return null;
    }

    // Channel permission overwrites
    public async Task<bool> EditChannelPermissionsAsync(ulong channelId, ulong overwriteId, EditChannelPermissionsRequest request)
    {
        var content = JsonContent(request);
        var response = await PutAsync($"channels/{channelId}/permissions/{overwriteId}", content);
        return response.IsSuccessStatusCode;
    }

    // Current user connections
    public async Task<List<UserConnection>?> GetCurrentUserConnectionsAsync()
    {
        var response = await GetAsync("users/@me/connections");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<UserConnection>>();
        }

        return null;
    }

    // -- Alpha12 endpoints -----------------------------------------------------

    // Guild member search
    public async Task<List<GuildMember>?> SearchGuildMembersAsync(ulong guildId, string query, int? limit = null)
    {
        var queryParams = new List<string> { $"query={Uri.EscapeDataString(query)}" };
        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        var response = await GetAsync($"guilds/{guildId}/members/search?{string.Join("&", queryParams)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GuildMember>>();
        }

        return null;
    }

    // Modify current member
    public async Task<GuildMember?> ModifyCurrentMemberAsync(ulong guildId, string? nick)
    {
        var payload = new { nick };
        var content = JsonContent(payload);
        var response = await PatchAsync($"guilds/{guildId}/members/@me", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>();
        }

        return null;
    }

    // Poll operations
    public async Task<List<User>?> GetAnswerVotersAsync(ulong channelId, ulong messageId, int answerId, int? limit = null, ulong? after = null)
    {
        var queryParams = new List<string>();
        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        if (after.HasValue)
        {
            queryParams.Add($"after={after.Value}");
        }

        var endpoint = $"channels/{channelId}/polls/{messageId}/answers/{answerId}";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

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
        {
            return await response.Content.ReadFromJsonAsync<Message>();
        }

        return null;
    }

    // SKU operations
    public async Task<List<Sku>?> ListSkusAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/skus");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Sku>>();
        }

        return null;
    }

    // Entitlement operations
    public async Task<List<Entitlement>?> ListEntitlementsAsync(ulong applicationId, ulong? userId = null, List<ulong>? skuIds = null, ulong? before = null, ulong? after = null, int? limit = null, ulong? guildId = null, bool? excludeEnded = null)
    {
        var queryParams = new List<string>();
        if (userId.HasValue)
        {
            queryParams.Add($"user_id={userId.Value}");
        }

        if (skuIds?.Any() == true)
        {
            queryParams.Add($"sku_ids={string.Join(",", skuIds)}");
        }

        if (before.HasValue)
        {
            queryParams.Add($"before={before.Value}");
        }

        if (after.HasValue)
        {
            queryParams.Add($"after={after.Value}");
        }

        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        if (guildId.HasValue)
        {
            queryParams.Add($"guild_id={guildId.Value}");
        }

        if (excludeEnded.HasValue)
        {
            queryParams.Add($"exclude_ended={excludeEnded.Value.ToString().ToLower()}");
        }

        var endpoint = $"applications/{applicationId}/entitlements";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Entitlement>>();
        }

        return null;
    }

    public async Task<Entitlement?> GetEntitlementAsync(ulong applicationId, ulong entitlementId)
    {
        var response = await GetAsync($"applications/{applicationId}/entitlements/{entitlementId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Entitlement>();
        }

        return null;
    }

    public async Task<Entitlement?> CreateTestEntitlementAsync(ulong applicationId, CreateTestEntitlementRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"applications/{applicationId}/entitlements", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Entitlement>();
        }

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
        if (before.HasValue)
        {
            queryParams.Add($"before={before.Value}");
        }

        if (after.HasValue)
        {
            queryParams.Add($"after={after.Value}");
        }

        if (limit.HasValue)
        {
            queryParams.Add($"limit={limit.Value}");
        }

        if (userId.HasValue)
        {
            queryParams.Add($"user_id={userId.Value}");
        }

        var endpoint = $"skus/{skuId}/subscriptions";
        if (queryParams.Any())
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Subscription>>();
        }

        return null;
    }

    public async Task<Subscription?> GetSkuSubscriptionAsync(ulong skuId, ulong subscriptionId)
    {
        var response = await GetAsync($"skus/{skuId}/subscriptions/{subscriptionId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Subscription>();
        }

        return null;
    }

    // Soundboard operations
    public async Task<List<SoundboardSound>?> ListDefaultSoundboardSoundsAsync()
    {
        var response = await GetAsync("soundboard-default-sounds");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<SoundboardSound>>();
        }

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
        {
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        }

        return null;
    }

    public async Task<SoundboardSound?> CreateGuildSoundboardSoundAsync(ulong guildId, CreateGuildSoundboardSoundRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/soundboard-sounds", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        }

        return null;
    }

    public async Task<SoundboardSound?> ModifyGuildSoundboardSoundAsync(ulong guildId, ulong soundId, ModifyGuildSoundboardSoundRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/soundboard-sounds/{soundId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<SoundboardSound>();
        }

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
        {
            return await response.Content.ReadFromJsonAsync<GuildOnboarding>();
        }

        return null;
    }

    public async Task<GuildOnboarding?> ModifyGuildOnboardingAsync(ulong guildId, ModifyGuildOnboardingRequest request)
    {
        var content = JsonContent(request);
        var response = await PutAsync($"guilds/{guildId}/onboarding", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildOnboarding>();
        }

        return null;
    }

    // Application Role Connection Metadata
    public async Task<List<ApplicationRoleConnectionMetadata>?> GetApplicationRoleConnectionMetadataAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/role-connections/metadata");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationRoleConnectionMetadata>>();
        }

        return null;
    }

    public async Task<List<ApplicationRoleConnectionMetadata>?> UpdateApplicationRoleConnectionMetadataAsync(ulong applicationId, List<ApplicationRoleConnectionMetadata> records)
    {
        var content = JsonContent(records);
        var response = await PutAsync($"applications/{applicationId}/role-connections/metadata", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<ApplicationRoleConnectionMetadata>>();
        }

        return null;
    }

    // -- Alpha13 additions -----------------------------------------------------

    // Reaction query
    public async Task<List<User>?> GetReactionsAsync(ulong channelId, ulong messageId, string emoji, int? type = null, ulong? after = null, int? limit = null)
    {
        var query = new List<string>();
        if (type.HasValue)
        {
            query.Add($"type={type.Value}");
        }

        if (after.HasValue)
        {
            query.Add($"after={after.Value}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        var endpoint = $"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}";
        if (query.Count > 0)
        {
            endpoint += "?" + string.Join("&", query);
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<User>>();
        }

        return null;
    }

    // Announcement channel follow
    public async Task<FollowedChannel?> FollowAnnouncementChannelAsync(ulong channelId, ulong webhookChannelId)
    {
        var payload = new { webhook_channel_id = webhookChannelId };
        var content = JsonContent(payload);
        var response = await PostAsync($"channels/{channelId}/followers", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FollowedChannel>();
        }

        return null;
    }

    // Guild preview
    public async Task<GuildPreview?> GetGuildPreviewAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/preview");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildPreview>();
        }

        return null;
    }

    // Guild widget
    public async Task<GuildWidgetSettings?> GetGuildWidgetSettingsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/widget");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildWidgetSettings>();
        }

        return null;
    }

    /// <summary>GET /guilds/{id}/widget.json — public rendered widget (no auth required).</summary>
    public async Task<GuildWidget?> GetGuildWidgetAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/widget.json");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildWidget>(_jsonOptions);
        }

        return null;
    }

    public async Task<GuildWidgetSettings?> ModifyGuildWidgetAsync(ulong guildId, ModifyGuildWidgetRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/widget", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildWidgetSettings>();
        }

        return null;
    }

    // Guild vanity URL
    public async Task<VanityUrl?> GetGuildVanityUrlAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/vanity-url");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<VanityUrl>();
        }

        return null;
    }

    // Guild welcome screen
    public async Task<WelcomeScreen?> GetGuildWelcomeScreenAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/welcome-screen");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WelcomeScreen>();
        }

        return null;
    }

    public async Task<WelcomeScreen?> ModifyGuildWelcomeScreenAsync(ulong guildId, ModifyGuildWelcomeScreenRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/welcome-screen", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WelcomeScreen>();
        }

        return null;
    }

    // Guild channel / role position reorder
    public async Task<bool> ModifyGuildChannelPositionsAsync(ulong guildId, IEnumerable<ModifyChannelPositionRequest> positions)
    {
        var content = JsonContent(positions);
        var response = await PatchAsync($"guilds/{guildId}/channels", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<Role>?> ModifyGuildRolePositionsAsync(ulong guildId, IEnumerable<ModifyRolePositionRequest> positions)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var content = JsonContent(positions);
        var response = await PatchAsync($"guilds/{guildId}/roles", content);
        return await HandleApiResponseAsync<List<Role>>("ModifyGuildRolePositionsAsync", response);
    }

    // Invite lookup and deletion
    public async Task<Invite?> GetInviteAsync(string inviteCode, bool? withCounts = null, bool? withExpiration = null, ulong? guildScheduledEventId = null)
    {
        var query = new List<string>();
        if (withCounts.HasValue)
        {
            query.Add($"with_counts={withCounts.Value.ToString().ToLower()}");
        }

        if (withExpiration.HasValue)
        {
            query.Add($"with_expiration={withExpiration.Value.ToString().ToLower()}");
        }

        if (guildScheduledEventId.HasValue)
        {
            query.Add($"guild_scheduled_event_id={guildScheduledEventId.Value}");
        }

        var endpoint = $"invites/{Uri.EscapeDataString(inviteCode)}";
        if (query.Count > 0)
        {
            endpoint += "?" + string.Join("&", query);
        }

        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Invite>();
        }

        return null;
    }

    public async Task<Invite?> DeleteInviteAsync(string inviteCode, string? reason = null)
    {
        var response = await DeleteAsync($"invites/{Uri.EscapeDataString(inviteCode)}", reason);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Invite>();
        }

        return null;
    }

    // Guild Templates
    public async Task<List<GuildTemplate>?> GetGuildTemplatesAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/templates");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GuildTemplate>>();
        }

        return null;
    }

    public async Task<GuildTemplate?> GetGuildTemplateAsync(string templateCode)
    {
        var response = await GetAsync($"guilds/templates/{Uri.EscapeDataString(templateCode)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildTemplate>();
        }

        return null;
    }

    public async Task<Guild?> CreateGuildFromTemplateAsync(string templateCode, CreateGuildFromTemplateRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/templates/{Uri.EscapeDataString(templateCode)}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Guild>();
        }

        return null;
    }

    public async Task<GuildTemplate?> CreateGuildTemplateAsync(ulong guildId, CreateGuildTemplateRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/templates", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildTemplate>();
        }

        return null;
    }

    public async Task<GuildTemplate?> SyncGuildTemplateAsync(ulong guildId, string templateCode)
    {
        var response = await PutAsync($"guilds/{guildId}/templates/{Uri.EscapeDataString(templateCode)}", null);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildTemplate>();
        }

        return null;
    }

    public async Task<GuildTemplate?> ModifyGuildTemplateAsync(ulong guildId, string templateCode, ModifyGuildTemplateRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/templates/{Uri.EscapeDataString(templateCode)}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildTemplate>();
        }

        return null;
    }

    public async Task<GuildTemplate?> DeleteGuildTemplateAsync(ulong guildId, string templateCode)
    {
        var response = await DeleteAsync($"guilds/{guildId}/templates/{Uri.EscapeDataString(templateCode)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildTemplate>();
        }

        return null;
    }

    // -- OAuth2 endpoints -----------------------------------------------------

    public async Task<Application?> GetCurrentBotApplicationInfoAsync()
    {
        var response = await GetAsync("oauth2/applications/@me");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Application>(_jsonOptions);
        }

        return null;
    }

    public async Task<OAuth2Info?> GetCurrentAuthorizationInfoAsync()
    {
        var response = await GetAsync("oauth2/@me");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OAuth2Info>(_jsonOptions);
        }

        return null;
    }

    // -- Interaction follow-up message endpoints -------------------------------

    public async Task<Message?> CreateFollowupMessageAsync(string applicationId, string interactionToken, CreateMessageRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"webhooks/{applicationId}/{interactionToken}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>(_jsonOptions);
        }

        return null;
    }

    public async Task<Message?> GetFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId)
    {
        var response = await GetAsync($"webhooks/{applicationId}/{interactionToken}/messages/{messageId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>(_jsonOptions);
        }

        return null;
    }

    public async Task<Message?> EditFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId, EditMessageRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"webhooks/{applicationId}/{interactionToken}/messages/{messageId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Message>(_jsonOptions);
        }

        return null;
    }

    public async Task<bool> DeleteFollowupMessageAsync(string applicationId, string interactionToken, ulong messageId)
    {
        var response = await DeleteAsync($"webhooks/{applicationId}/{interactionToken}/messages/{messageId}");
        return response.IsSuccessStatusCode;
    }

    // -- Application Management ------------------------------------------------

    public async Task<Application?> GetCurrentApplicationAsync()
    {
        var response = await GetAsync("applications/@me");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Application>(_jsonOptions);
        }

        return null;
    }

    public async Task<Application?> EditCurrentApplicationAsync(EditCurrentApplicationRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync("applications/@me", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Application>(_jsonOptions);
        }

        return null;
    }

    // -- Guild Emoji Operations ------------------------------------------------

    public async Task<List<Emoji>?> ListGuildEmojisAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/emojis");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Emoji>>(_jsonOptions);
        }

        return null;
    }

    public async Task<Emoji?> GetGuildEmojiAsync(ulong guildId, ulong emojiId)
    {
        var response = await GetAsync($"guilds/{guildId}/emojis/{emojiId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<Emoji?> CreateGuildEmojiAsync(ulong guildId, CreateGuildEmojiRequest request, string? reason = null)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/emojis", content, reason);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<Emoji?> ModifyGuildEmojiAsync(ulong guildId, ulong emojiId, ModifyGuildEmojiRequest request, string? reason = null)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/emojis/{emojiId}", content, reason);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<bool> DeleteGuildEmojiAsync(ulong guildId, ulong emojiId, string? reason = null)
    {
        var response = await DeleteAsync($"guilds/{guildId}/emojis/{emojiId}", reason);
        return response.IsSuccessStatusCode;
    }

    // -- Application Emoji Operations ------------------------------------------

    public async Task<List<Emoji>?> ListApplicationEmojisAsync(ulong applicationId)
    {
        var response = await GetAsync($"applications/{applicationId}/emojis");
        if (response.IsSuccessStatusCode)
        {
            var wrapper = await response.Content.ReadFromJsonAsync<ApplicationEmojiListResponse>(_jsonOptions);
            return wrapper?.Items;
        }
        return null;
    }

    public async Task<Emoji?> GetApplicationEmojiAsync(ulong applicationId, ulong emojiId)
    {
        var response = await GetAsync($"applications/{applicationId}/emojis/{emojiId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<Emoji?> CreateApplicationEmojiAsync(ulong applicationId, CreateApplicationEmojiRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"applications/{applicationId}/emojis", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<Emoji?> ModifyApplicationEmojiAsync(ulong applicationId, ulong emojiId, ModifyApplicationEmojiRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"applications/{applicationId}/emojis/{emojiId}", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Emoji>(_jsonOptions);
        }

        return null;
    }

    public async Task<bool> DeleteApplicationEmojiAsync(ulong applicationId, ulong emojiId)
    {
        var response = await DeleteAsync($"applications/{applicationId}/emojis/{emojiId}");
        return response.IsSuccessStatusCode;
    }

    // -- Guild Integration Operations ------------------------------------------

    public async Task<List<GuildIntegration>?> GetGuildIntegrationsAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/integrations");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<GuildIntegration>>(_jsonOptions);
        }

        return null;
    }

    public async Task<bool> DeleteGuildIntegrationAsync(ulong guildId, ulong integrationId, string? reason = null)
    {
        var response = await DeleteAsync($"guilds/{guildId}/integrations/{integrationId}", reason);
        return response.IsSuccessStatusCode;
    }

    // -- Guild Invite Operations -----------------------------------------------

    public async Task<List<Invite>?> GetGuildInvitesAsync(ulong guildId)
    {
        var response = await GetAsync($"guilds/{guildId}/invites");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<Invite>>(_jsonOptions);
        }

        return null;
    }

    // -- Guild Prune Operations ------------------------------------------------

    public async Task<GuildPruneResult?> GetGuildPruneCountAsync(ulong guildId, int? days = null, List<ulong>? includeRoles = null)
    {
        var query = new List<string>();
        if (days.HasValue)
        {
            query.Add($"days={days.Value}");
        }

        if (includeRoles is { Count: > 0 })
        {
            query.Add("include_roles=" + string.Join(",", includeRoles));
        }

        var endpoint = $"guilds/{guildId}/prune" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        var response = await GetAsync(endpoint);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildPruneResult>(_jsonOptions);
        }

        return null;
    }

    public async Task<GuildPruneResult?> BeginGuildPruneAsync(ulong guildId, BeginGuildPruneRequest request, string? reason = null)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/prune", content, reason);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildPruneResult>(_jsonOptions);
        }

        return null;
    }

    // -- Bulk Ban --------------------------------------------------------------

    public async Task<BulkGuildBanResponse?> BulkGuildBanAsync(ulong guildId, BulkGuildBanRequest request, string? reason = null)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"guilds/{guildId}/bulk-ban", content, reason);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BulkGuildBanResponse>(_jsonOptions);
        }

        return null;
    }

    // -- Guild Role Extras -----------------------------------------------------

    public async Task<Role?> GetGuildRoleAsync(ulong guildId, ulong roleId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        ValidateSnowflake(roleId, nameof(roleId));
        var response = await GetAsync($"guilds/{guildId}/roles/{roleId}");
        return await HandleApiResponseAsync<Role>("GetGuildRoleAsync", response);
    }

    public async Task<Dictionary<string, int>?> GetGuildRoleMemberCountsAsync(ulong guildId)
    {
        ValidateSnowflake(guildId, nameof(guildId));
        var response = await GetAsync($"guilds/{guildId}/roles/member-counts");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Dictionary<string, int>>(_jsonOptions);
        }

        return null;
    }

    // -- Guild Incident Actions ------------------------------------------------

    public async Task<GuildIncidentActionsResponse?> ModifyGuildIncidentActionsAsync(ulong guildId, ModifyGuildIncidentActionsRequest request)
    {
        var content = JsonContent(request);
        var response = await PutAsync($"guilds/{guildId}/incident-actions", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildIncidentActionsResponse>(_jsonOptions);
        }

        return null;
    }

    // -- Current User Guild Member ---------------------------------------------

    public async Task<GuildMember?> GetCurrentUserGuildMemberAsync(ulong guildId)
    {
        var response = await GetAsync($"users/@me/guilds/{guildId}/member");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GuildMember>(_jsonOptions);
        }

        return null;
    }

    // -- Reaction Extras -------------------------------------------------------

    public async Task<bool> DeleteAllReactionsAsync(ulong channelId, ulong messageId)
    {
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}/reactions");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAllReactionsForEmojiAsync(ulong channelId, ulong messageId, string emoji)
    {
        var response = await DeleteAsync($"channels/{channelId}/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}");
        return response.IsSuccessStatusCode;
    }

    // -- Soundboard -------------------------------------------------------

    /// <summary>POST /channels/{channel.id}/send-soundboard-sound</summary>
    public async Task<bool> SendSoundboardSoundAsync(ulong channelId, SendSoundboardSoundRequest request)
    {
        var content = JsonContent(request);
        var response = await PostAsync($"channels/{channelId}/send-soundboard-sound", content);
        return response.IsSuccessStatusCode;
    }

    // -- Voice States -----------------------------------------------------

    /// <summary>PATCH /guilds/{guild.id}/voice-states/@me</summary>
    public async Task<bool> ModifyCurrentUserVoiceStateAsync(ulong guildId, ModifyCurrentUserVoiceStateRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/voice-states/@me", content);
        return response.IsSuccessStatusCode;
    }

    /// <summary>PATCH /guilds/{guild.id}/voice-states/{user.id}</summary>
    public async Task<bool> ModifyUserVoiceStateAsync(ulong guildId, ulong userId, ModifyUserVoiceStateRequest request)
    {
        var content = JsonContent(request);
        var response = await PatchAsync($"guilds/{guildId}/voice-states/{userId}", content);
        return response.IsSuccessStatusCode;
    }

    // -- User Application Role Connection ---------------------------------

    /// <summary>GET /users/@me/applications/{application.id}/role-connection</summary>
    public async Task<ApplicationRoleConnection?> GetUserApplicationRoleConnectionAsync(ulong applicationId)
    {
        var response = await GetAsync($"users/@me/applications/{applicationId}/role-connection");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationRoleConnection>(_jsonOptions);
        }

        return null;
    }

    /// <summary>PUT /users/@me/applications/{application.id}/role-connection</summary>
    public async Task<ApplicationRoleConnection?> UpdateUserApplicationRoleConnectionAsync(ulong applicationId, UpdateUserApplicationRoleConnectionRequest request)
    {
        var content = JsonContent(request);
        var response = await PutAsync($"users/@me/applications/{applicationId}/role-connection", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApplicationRoleConnection>(_jsonOptions);
        }

        return null;
    }

    // -- OAuth2 token helpers -----------------------------------------------

    /// <summary>
    /// Exchanges an authorization code for an access token.
    /// Sends a direct <c>POST oauth2/token</c> with form-encoded body —
    /// the client's bot token is NOT attached to this request.
    /// </summary>
    public async Task<OAuth2TokenResponse?> ExchangeCodeAsync(
        string code,
        string clientId,
        string clientSecret,
        string redirectUri)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type",    "authorization_code"),
            new KeyValuePair<string, string>("code",          code),
            new KeyValuePair<string, string>("redirect_uri",  redirectUri),
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
        });

        var response = await SendRequestAsync(HttpMethod.Post, "oauth2/token", form, skipBotAuth: true);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>(_jsonOptions);
        }

        return null;
    }

    /// <summary>
    /// Uses a refresh token to obtain a new access token.
    /// Sends a direct <c>POST oauth2/token</c> with form-encoded body —
    /// the client's bot token is NOT attached to this request.
    /// </summary>
    public async Task<OAuth2TokenResponse?> RefreshTokenAsync(
        string refreshToken,
        string clientId,
        string clientSecret)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type",    "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
        });

        var response = await SendRequestAsync(HttpMethod.Post, "oauth2/token", form, skipBotAuth: true);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>(_jsonOptions);
        }

        return null;
    }

    /// <summary>
    /// Revokes an OAuth2 access or refresh token. POST /oauth2/token/revoke.
    /// The client's bot token is NOT attached to this request.
    /// </summary>
    public async Task<bool> RevokeTokenAsync(string token, string clientId, string clientSecret, string? tokenTypeHint = null)
    {
        var fields = new List<KeyValuePair<string, string>>
        {
            new("token",         token),
            new("client_id",     clientId),
            new("client_secret", clientSecret),
        };
        if (tokenTypeHint != null)
        {
            fields.Add(new("token_type_hint", tokenTypeHint));
        }

        var response = await SendRequestAsync(HttpMethod.Post, "oauth2/token/revoke", new FormUrlEncodedContent(fields), skipBotAuth: true);
        return response.IsSuccessStatusCode;
    }

    // -- Group DM ------------------------------------------------------------

    /// <summary>
    /// Creates a new Group DM channel. POST /users/@me/channels.
    /// Requires access tokens of the target users with the <c>gdm.join</c> OAuth2 scope.
    /// </summary>
    public async Task<Channel?> CreateGroupDmAsync(List<string> accessTokens, Dictionary<string, string>? nicks = null)
    {
        var body = new CreateGroupDmRequest { AccessTokens = accessTokens, Nicks = nicks };
        var content = JsonContent(body);
        var response = await PostAsync("users/@me/channels", content);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Channel>(_jsonOptions);
        }

        return null;
    }

    public async Task<ActivityInstance?> GetActivityInstanceAsync(ulong applicationId, string instanceId)
    {
        var response = await GetAsync($"applications/{applicationId}/activity-instances/{Uri.EscapeDataString(instanceId)}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ActivityInstance>(_jsonOptions);
        }

        await LogSanitizedApiErrorAsync("GetActivityInstanceAsync failed", response);
        return null;
    }

    private int MaxRateLimitRetries => _options.RestApi.MaxRateLimitRetries;

    /// <summary>
    /// Validates a snowflake ID parameter and throws if invalid.
    /// </summary>
    private void ValidateSnowflake(ulong id, string paramName)
    {
        SnowflakeValidator.ValidateSnowflake(id, paramName);
    }

    /// <summary>
    /// Helper method for consistent error handling across all API methods.
    /// Logs the error and optionally throws an exception based on configuration.
    /// </summary>
    private async Task<T?> HandleApiResponseAsync<T>(string operation, HttpResponseMessage response) where T : class
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        await LogSanitizedApiErrorAsync($"{operation} failed", response);

        if (_options.RestApi.ThrowOnApiError)
        {
            var statusCode = (System.Net.HttpStatusCode)response.StatusCode;
            var errorBody = await response.Content.ReadAsStringAsync();
            string? discordErrorCode = null;
            string? discordErrorMessage = null;

            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("code", out var codeElement))
                {
                    discordErrorCode = codeElement.GetInt32().ToString();
                }
                if (doc.RootElement.TryGetProperty("message", out var messageElement))
                {
                    discordErrorMessage = messageElement.GetString();
                }
            }
            catch { /* Ignore parse errors */ }

            throw DiscordApiException.FromResponse(statusCode, operation, discordErrorCode, discordErrorMessage);
        }

        return null;
    }

    /// <summary>
    /// Helper method for consistent error handling for operations returning HttpResponseMessage.
    /// </summary>
    private async Task<HttpResponseMessage> HandleApiResponseAsync(string operation, HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        await LogSanitizedApiErrorAsync($"{operation} failed", response);

        if (_options.RestApi.ThrowOnApiError)
        {
            var statusCode = (System.Net.HttpStatusCode)response.StatusCode;
            throw DiscordApiException.FromResponse(statusCode, operation);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        string? reason = null,
        CancellationToken cancellationToken = default,
        int retryCount = 0,
        byte[]? bufferedContentBytes = null,
        string? bufferedContentType = null,
        bool skipBotAuth = false)
    {
        var path = endpoint.Split('?')[0];
        var route = $"{method.Method} {path}";

        // Buffer request body once so retries can reconstruct fresh HttpContent.
        // HttpClient disposes the content object after SendAsync; reusing it throws
        // ObjectDisposedException on any rate-limited POST/PATCH/PUT retry.
        if (content is not null && bufferedContentBytes is null)
        {
            bufferedContentBytes = await content.ReadAsByteArrayAsync(cancellationToken);
            bufferedContentType  = content.Headers.ContentType?.ToString();
        }

        // Global rate limit check
        if (DateTimeOffset.UtcNow < _globalReset)
        {
            var delay = _globalReset - DateTimeOffset.UtcNow;
            _logger.LogWarning("Global rate limit hit, delaying {Delay}", delay);
            EmitRateLimitTelemetry(new RateLimitTelemetryEvent
            {
                Kind = RateLimitTelemetryKind.GlobalDelayApplied,
                Route = route,
                IsGlobal = true,
                RetryAfter = delay,
                ResetAt = _globalReset,
                RetryCount = retryCount
            });

            await Task.Delay(delay, cancellationToken);
        }

        // Per-route rate limit coordination
        string? bucketHash = null;
        try
        {
            await _rateLimiter.WaitForRateLimitAsync(route, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate limiter wait failed, proceeding with request");
        }

        // Build a fresh HttpRequestMessage per attempt.
        // Authorization is set here rather than on DefaultRequestHeaders so that
        // credentials are scoped to individual request objects.
        var request = new HttpRequestMessage(method, endpoint);
        if (!skipBotAuth)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _options.Token);
        }

        if (bufferedContentBytes is { Length: > 0 })
        {
            var bc = new ByteArrayContent(bufferedContentBytes);
            if (bufferedContentType is not null)
            {
                bc.Headers.TryAddWithoutValidation("Content-Type", bufferedContentType);
            }

            request.Content = bc;
        }

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
            var retryAfter = await GetRetryAfterDelayAsync(response, cancellationToken);
            _logger.LogWarning("Rate limited, retrying after {RetryAfter}", retryAfter);
            
            // Update limiter with 429 info for bucket-aware retry
            if (response.Headers.TryGetValues("X-RateLimit-Bucket", out var bucketValues))
            {
                bucketHash = bucketValues.FirstOrDefault();
            }
            var isGlobal = HeaderValueIsTrue(response, "X-RateLimit-Global");
            var resetAt = DateTimeOffset.UtcNow.Add(retryAfter);
            _rateLimiter.UpdateRateLimits(route, bucketHash, 0, resetAt, isGlobal);
            EmitRateLimitTelemetry(new RateLimitTelemetryEvent
            {
                Kind = RateLimitTelemetryKind.RetryScheduled,
                Route = route,
                BucketHash = bucketHash,
                Remaining = 0,
                ResetAt = resetAt,
                IsGlobal = isGlobal,
                RetryAfter = retryAfter,
                RetryCount = retryCount + 1
            });
            
            // Wait for rate limiter to allow retry
            await _rateLimiter.WaitForRateLimitAsync(route, bucketHash, cancellationToken);

            if (retryCount >= MaxRateLimitRetries)
            {
                _logger.LogError("Rate limit retry limit ({Max}) exceeded for {Method} {Endpoint}",
                    MaxRateLimitRetries, method, LogSanitizer.RedactSensitiveEndpoint(endpoint));
                return response; // Return the 429 response rather than looping forever
            }

            // Pass buffered bytes so the retry reconstructs a fresh HttpContent.
            return await SendRequestAsync(method, endpoint, null, reason, cancellationToken,
                retryCount + 1, bufferedContentBytes, bufferedContentType, skipBotAuth); // Retry
        }

        // Mark request as complete
        _rateLimiter.MarkRequestComplete(route, bucketHash);

        // Use TryGetValues to avoid InvalidOperationException when Retry-After is absent.
        if (HeaderValueIsTrue(response, "X-RateLimit-Global") &&
            response.Headers.TryGetValues("Retry-After", out var retryAfterVals) &&
            double.TryParse(retryAfterVals.FirstOrDefault(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var retryAfterSecs))
        {
            _globalReset = DateTimeOffset.UtcNow.AddSeconds(retryAfterSecs);
        }

        return response;
    }

    private static bool HeaderValueIsTrue(HttpResponseMessage response, string headerName)
        => response.Headers.TryGetValues(headerName, out var values)
           && bool.TryParse(values.FirstOrDefault(), out var parsed)
           && parsed;

    private static async Task<TimeSpan> GetRetryAfterDelayAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Headers.RetryAfter?.Delta is { } headerDelay && headerDelay > TimeSpan.Zero)
        {
            return headerDelay;
        }

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("retry_after", out var retryAfterElement))
                {
                    if (retryAfterElement.ValueKind == JsonValueKind.Number)
                    {
                        var retryAfterSeconds = retryAfterElement.GetDouble();
                        if (retryAfterSeconds > 0)
                        {
                            return TimeSpan.FromSeconds(retryAfterSeconds);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Ignore malformed/unexpected payloads and use a safe fallback.
            System.Diagnostics.Debug.WriteLine($"Rate limit parse error, using fallback: {ex.Message}");
        }

        return TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Logs API failures with sanitized response content to prevent accidental secret exposure.
    /// Keep response-body logging behind this helper for consistency across future call sites.
    /// </summary>
    private async Task LogSanitizedApiErrorAsync(string operation, HttpResponseMessage response)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        _logger.LogError("{Operation} ({Status}): {Body}",
            operation,
            (int)response.StatusCode,
            LogSanitizer.SanitizeHttpErrorBody(errorBody));
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

            isGlobal = HeaderValueIsTrue(response, "X-RateLimit-Global");

            if (remaining.HasValue || resetAt.HasValue || isGlobal)
            {
                _rateLimiter.UpdateRateLimits(route, bucket, remaining, resetAt, isGlobal);
                EmitRateLimitTelemetry(new RateLimitTelemetryEvent
                {
                    Kind = RateLimitTelemetryKind.HeaderUpdate,
                    Route = route,
                    BucketHash = bucket,
                    Remaining = remaining,
                    ResetAt = resetAt,
                    IsGlobal = isGlobal,
                    RetryCount = 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse rate limit headers");
        }
    }

    private void EmitRateLimitTelemetry(RateLimitTelemetryEvent telemetry)
    {
        var handler = RateLimitObserved;
        if (handler is null)
        {
            return;
        }

        try
        {
            handler.Invoke(this, telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "A rate-limit telemetry subscriber threw an exception");
        }
    }
}