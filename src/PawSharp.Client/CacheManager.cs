#nullable enable
using Microsoft.Extensions.Logging;
using PawSharp.Cache.Interfaces;
using PawSharp.Core.Entities;
using PawSharp.Core.Enums;
using PawSharp.Gateway;
using PawSharp.Gateway.Events;

namespace PawSharp.Client;

/// <summary>
/// Automatically caches entities from gateway events.
/// </summary>
public class CacheManager
{
    private readonly IEntityCache _cache;
    private readonly ILogger<CacheManager>? _logger;

    public CacheManager(IEntityCache cache, ILogger<CacheManager>? logger = null)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to gateway events and automatically cache entities.
    /// </summary>
    public void SubscribeToGateway(IGatewayClient gateway)
    {
        // READY event
        gateway.Events.On<ReadyEvent>("READY", HandleReady);
        
        // Guild events
        gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", HandleGuildCreate);
        gateway.Events.On<GuildUpdateEvent>("GUILD_UPDATE", HandleGuildUpdate);
        gateway.Events.On<GuildDeleteEvent>("GUILD_DELETE", HandleGuildDelete);
        gateway.Events.On<GuildEmojisUpdateEvent>("GUILD_EMOJIS_UPDATE", HandleGuildEmojisUpdate);
        
        // Channel events
        gateway.Events.On<ChannelCreateEvent>("CHANNEL_CREATE", HandleChannelCreate);
        gateway.Events.On<ChannelUpdateEvent>("CHANNEL_UPDATE", HandleChannelUpdate);
        gateway.Events.On<ChannelDeleteEvent>("CHANNEL_DELETE", HandleChannelDelete);
        
        // Message events
        gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", HandleMessageCreate);
        gateway.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", HandleMessageUpdate);
        gateway.Events.On<MessageDeleteEvent>("MESSAGE_DELETE", HandleMessageDelete);
        
        // Member events
        gateway.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", HandleGuildMemberAdd);
        gateway.Events.On<GuildMemberUpdateEvent>("GUILD_MEMBER_UPDATE", HandleGuildMemberUpdate);
        gateway.Events.On<GuildMemberRemoveEvent>("GUILD_MEMBER_REMOVE", HandleGuildMemberRemove);

        // Role events
        gateway.Events.On<GuildRoleCreateEvent>("GUILD_ROLE_CREATE", HandleGuildRoleCreate);
        gateway.Events.On<GuildRoleUpdateEvent>("GUILD_ROLE_UPDATE", HandleGuildRoleUpdate);
        gateway.Events.On<GuildRoleDeleteEvent>("GUILD_ROLE_DELETE", HandleGuildRoleDelete);

        // Sticker events
        gateway.Events.On<GuildStickersUpdateEvent>("GUILD_STICKERS_UPDATE", HandleGuildStickersUpdate);

        // Thread events (treated as channels in the cache)
        gateway.Events.On<ThreadCreateEvent>("THREAD_CREATE", HandleThreadCreate);
        gateway.Events.On<ThreadUpdateEvent>("THREAD_UPDATE", HandleThreadUpdate);
        gateway.Events.On<ThreadDeleteEvent>("THREAD_DELETE", HandleThreadDelete);

        // User events
        gateway.Events.On<UserUpdateEvent>("USER_UPDATE", HandleUserUpdate);

        // Bulk member chunk — response to opcode 8 (Request Guild Members)
        gateway.Events.On<GuildMembersChunkEvent>("GUILD_MEMBERS_CHUNK", HandleGuildMembersChunk);
        
        _logger?.LogInformation("Cache manager subscribed to gateway events");
    }

    private void HandleReady(ReadyEvent e)
    {
        try
        {
            _logger?.LogInformation("Caching READY event data");

            // Cache the bot user
            _cache.CacheUser(e.User);

            // Cache all guilds (will be unavailable initially)
            foreach (var guild in e.Guilds)
            {
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling READY event");
        }
    }

    private void HandleGuildCreate(GuildCreateEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching guild: {Name} ({Id})", e.Name, e.Id);

            var guild = e.ToGuild();
            _cache.CacheGuildData(guild);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_CREATE event for guild {GuildId}", e.Id);
        }
    }

    private void HandleGuildUpdate(GuildUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached guild: {Id}", e.Id);

            var guild = _cache.GetGuild(e.Id);
            if (guild != null)
            {
                guild.Name = e.Name;
                guild.Icon = e.Icon;
                guild.OwnerId = e.OwnerId;
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_UPDATE event for guild {GuildId}", e.Id);
        }
    }

    private void HandleGuildDelete(GuildDeleteEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing guild from cache: {Id}", e.Id);
            _cache.RemoveGuild(e.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_DELETE event for guild {GuildId}", e.Id);
        }
    }

    private void HandleGuildEmojisUpdate(GuildEmojisUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached emojis for guild: {GuildId}", e.GuildId);

            var guild = _cache.GetGuild(e.GuildId);
            if (guild != null)
            {
                guild.Emojis = e.Emojis;
                _cache.CacheGuild(guild);

                // Cache individual emojis
                foreach (var emoji in e.Emojis)
                {
                    _cache.CacheEmoji(e.GuildId, emoji);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_EMOJIS_UPDATE event for guild {GuildId}", e.GuildId);
        }
    }

    private void HandleChannelCreate(ChannelCreateEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching channel: {Name} ({Id})", e.Name, e.Id);
            _cache.CacheChannel(e.ToChannel());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling CHANNEL_CREATE event for channel {ChannelId}", e.Id);
        }
    }

    private void HandleChannelUpdate(ChannelUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached channel: {Id}", e.Id);
            _cache.CacheChannel(e.ToChannel());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling CHANNEL_UPDATE event for channel {ChannelId}", e.Id);
        }
    }

    private void HandleChannelDelete(ChannelDeleteEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing channel from cache: {Id}", e.Id);
            _cache.Remove($"channel:{e.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling CHANNEL_DELETE event for channel {ChannelId}", e.Id);
        }
    }

    private void HandleMessageCreate(MessageCreateEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching message: {Id}", e.Id);

            var message = e.ToMessage();
            _cache.CacheMessage(message);

            // Cache the author
            _cache.CacheUser(e.Author);

            // Cache member if present
            if (e.GuildId.HasValue && e.Member != null)
            {
                _cache.CacheGuildMember(e.GuildId.Value, e.Member);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling MESSAGE_CREATE event for message {MessageId}", e.Id);
        }
    }

    private void HandleMessageUpdate(MessageUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached message: {Id}", e.Id);

            var message = _cache.GetMessage(e.Id);
            if (message != null)
            {
                if (e.Content != null)
                    message.Content = e.Content;
                if (e.EditedTimestamp.HasValue)
                    message.EditedTimestamp = e.EditedTimestamp;
                if (e.Embeds != null)
                    message.Embeds = e.Embeds;

                _cache.CacheMessage(message);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling MESSAGE_UPDATE event for message {MessageId}", e.Id);
        }
    }

    private void HandleMessageDelete(MessageDeleteEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing message from cache: {Id}", e.Id);
            _cache.Remove($"message:{e.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling MESSAGE_DELETE event for message {MessageId}", e.Id);
        }
    }

    private void HandleGuildMemberAdd(GuildMemberAddEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching guild member: {UserId} in guild {GuildId}", e.User?.Id, e.GuildId);

            if (e.User != null)
            {
                _cache.CacheUser(e.User);
                _cache.CacheGuildMember(e.GuildId, e.ToGuildMember());
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_MEMBER_ADD event for user {UserId} in guild {GuildId}", e.User?.Id, e.GuildId);
        }
    }

    private void HandleGuildMemberUpdate(GuildMemberUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached guild member: {UserId} in guild {GuildId}", e.User.Id, e.GuildId);

            var member = _cache.GetGuildMember(e.GuildId, e.User.Id);
            if (member != null)
            {
                member.Roles = e.Roles;
                member.Nick = e.Nick;
                member.Avatar = e.Avatar;
                if (e.PremiumSince.HasValue)
                    member.PremiumSince = e.PremiumSince;

                _cache.CacheGuildMember(e.GuildId, member);
            }

            // Always cache the updated user
            _cache.CacheUser(e.User);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_MEMBER_UPDATE event for user {UserId} in guild {GuildId}", e.User.Id, e.GuildId);
        }
    }

    private void HandleGuildMemberRemove(GuildMemberRemoveEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing guild member from cache: {UserId} from guild {GuildId}", e.User.Id, e.GuildId);
            _cache.Remove($"member:{e.GuildId}:{e.User.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_MEMBER_REMOVE event for user {UserId} in guild {GuildId}", e.User.Id, e.GuildId);
        }
    }

    // ── Role handlers ─────────────────────────────────────────────────────────

    private void HandleGuildRoleCreate(GuildRoleCreateEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching new role: {Name} ({Id}) in guild {GuildId}", e.Role.Name, e.Role.Id, e.GuildId);
            _cache.CacheRole(e.GuildId, e.Role);

            var guild = _cache.GetGuild(e.GuildId);
            if (guild != null)
            {
                guild.Roles ??= new();
                guild.Roles.RemoveAll(r => r.Id == e.Role.Id);
                guild.Roles.Add(e.Role);
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_ROLE_CREATE event for role {RoleId} in guild {GuildId}", e.Role.Id, e.GuildId);
        }
    }

    private void HandleGuildRoleUpdate(GuildRoleUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached role: {Id} in guild {GuildId}", e.Role.Id, e.GuildId);
            _cache.CacheRole(e.GuildId, e.Role);

            var guild = _cache.GetGuild(e.GuildId);
            if (guild != null)
            {
                guild.Roles ??= new();
                var idx = guild.Roles.FindIndex(r => r.Id == e.Role.Id);
                if (idx >= 0)
                    guild.Roles[idx] = e.Role;
                else
                    guild.Roles.Add(e.Role);
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_ROLE_UPDATE event for role {RoleId} in guild {GuildId}", e.Role.Id, e.GuildId);
        }
    }

    private void HandleGuildRoleDelete(GuildRoleDeleteEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing role from cache: {RoleId} from guild {GuildId}", e.RoleId, e.GuildId);
            _cache.Remove($"role:{e.RoleId}");

            var guild = _cache.GetGuild(e.GuildId);
            if (guild != null)
            {
                guild.Roles?.RemoveAll(r => r.Id == e.RoleId);
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_ROLE_DELETE event for role {RoleId} in guild {GuildId}", e.RoleId, e.GuildId);
        }
    }

    // ── Sticker handler ───────────────────────────────────────────────────────

    private void HandleGuildStickersUpdate(GuildStickersUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached stickers for guild: {GuildId}", e.GuildId);

            var guild = _cache.GetGuild(e.GuildId);
            if (guild != null)
            {
                guild.Stickers = e.Stickers;
                _cache.CacheGuild(guild);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_STICKERS_UPDATE event for guild {GuildId}", e.GuildId);
        }
    }

    // ── Thread handlers (threads are cached as channels) ─────────────────────

    private void HandleThreadCreate(ThreadCreateEvent e)
    {
        try
        {
            _logger?.LogDebug("Caching thread: {Name} ({Id})", e.Name, e.Id);
            _cache.CacheChannel(new Channel
            {
                Id = e.Id,
                Type = (ChannelType)e.Type,
                GuildId = e.GuildId,
                ParentId = e.ParentId,
                OwnerId = e.OwnerId,
                Name = e.Name,
                LastMessageId = e.LastMessageId,
                RateLimitPerUser = e.RateLimitPerUser
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling THREAD_CREATE event for thread {ThreadId}", e.Id);
        }
    }

    private void HandleThreadUpdate(ThreadUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating cached thread: {Id}", e.Id);
            _cache.CacheChannel(new Channel
            {
                Id = e.Id,
                Type = (ChannelType)e.Type,
                GuildId = e.GuildId,
                ParentId = e.ParentId,
                OwnerId = e.OwnerId,
                Name = e.Name,
                LastMessageId = e.LastMessageId,
                RateLimitPerUser = e.RateLimitPerUser
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling THREAD_UPDATE event for thread {ThreadId}", e.Id);
        }
    }

    private void HandleThreadDelete(ThreadDeleteEvent e)
    {
        try
        {
            _logger?.LogDebug("Removing thread from cache: {Id}", e.Id);
            _cache.Remove($"channel:{e.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling THREAD_DELETE event for thread {ThreadId}", e.Id);
        }
    }

    // ── User handler ──────────────────────────────────────────────────────────

    private void HandleUserUpdate(UserUpdateEvent e)
    {
        try
        {
            _logger?.LogDebug("Updating bot user in cache: {Id}", e.Id);
            var existing = _cache.GetUser(e.Id);
            if (existing != null)
            {
                existing.Username = e.Username;
                existing.Discriminator = e.Discriminator;
                existing.Avatar = e.Avatar;
                _cache.CacheUser(existing);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling USER_UPDATE event for user {UserId}", e.Id);
        }
    }

    private void HandleGuildMembersChunk(GuildMembersChunkEvent e)
    {
        try
        {
            _logger?.LogDebug(
                "Caching member chunk for guild {GuildId}: {Count} members (chunk {Index}/{Total})",
                e.GuildId, e.Members.Count, e.ChunkIndex + 1, e.ChunkCount);

            foreach (var member in e.Members)
            {
                if (member.User != null)
                    _cache.CacheUser(member.User);
                _cache.CacheGuildMember(e.GuildId, member);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling GUILD_MEMBERS_CHUNK event for guild {GuildId}", e.GuildId);
        }
    }
}
