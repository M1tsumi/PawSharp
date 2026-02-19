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
        
        _logger?.LogInformation("Cache manager subscribed to gateway events");
    }

    private void HandleReady(ReadyEvent e)
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

    private void HandleGuildCreate(GuildCreateEvent e)
    {
        _logger?.LogDebug($"Caching guild: {e.Name} ({e.Id})");
        
        var guild = e.ToGuild();
        _cache.CacheGuildData(guild);
    }

    private void HandleGuildUpdate(GuildUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached guild: {e.Id}");
        
        var guild = _cache.GetGuild(e.Id);
        if (guild != null)
        {
            guild.Name = e.Name;
            guild.Icon = e.Icon;
            guild.OwnerId = e.OwnerId;
            _cache.CacheGuild(guild);
        }
    }

    private void HandleGuildDelete(GuildDeleteEvent e)
    {
        _logger?.LogDebug($"Removing guild from cache: {e.Id}");
        _cache.RemoveGuild(e.Id);
    }

    private void HandleGuildEmojisUpdate(GuildEmojisUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached emojis for guild: {e.GuildId}");
        
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

    private void HandleChannelCreate(ChannelCreateEvent e)
    {
        _logger?.LogDebug($"Caching channel: {e.Name} ({e.Id})");
        _cache.CacheChannel(e.ToChannel());
    }

    private void HandleChannelUpdate(ChannelUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached channel: {e.Id}");
        _cache.CacheChannel(e.ToChannel());
    }

    private void HandleChannelDelete(ChannelDeleteEvent e)
    {
        _logger?.LogDebug($"Removing channel from cache: {e.Id}");
        _cache.Remove($"channel:{e.Id}");
    }

    private void HandleMessageCreate(MessageCreateEvent e)
    {
        _logger?.LogDebug($"Caching message: {e.Id}");
        
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

    private void HandleMessageUpdate(MessageUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached message: {e.Id}");
        
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

    private void HandleMessageDelete(MessageDeleteEvent e)
    {
        _logger?.LogDebug($"Removing message from cache: {e.Id}");
        _cache.Remove($"message:{e.Id}");
    }

    private void HandleGuildMemberAdd(GuildMemberAddEvent e)
    {
        _logger?.LogDebug($"Caching guild member: {e.User?.Id} in guild {e.GuildId}");
        
        if (e.User != null)
        {
            _cache.CacheUser(e.User);
            _cache.CacheGuildMember(e.GuildId, e.ToGuildMember());
        }
    }

    private void HandleGuildMemberUpdate(GuildMemberUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached guild member: {e.User.Id} in guild {e.GuildId}");
        
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

    private void HandleGuildMemberRemove(GuildMemberRemoveEvent e)
    {
        _logger?.LogDebug($"Removing guild member from cache: {e.User.Id} from guild {e.GuildId}");
        _cache.Remove($"member:{e.GuildId}:{e.User.Id}");
    }

    // ── Role handlers ─────────────────────────────────────────────────────────

    private void HandleGuildRoleCreate(GuildRoleCreateEvent e)
    {
        _logger?.LogDebug($"Caching new role: {e.Role.Name} ({e.Role.Id}) in guild {e.GuildId}");
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

    private void HandleGuildRoleUpdate(GuildRoleUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached role: {e.Role.Id} in guild {e.GuildId}");
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

    private void HandleGuildRoleDelete(GuildRoleDeleteEvent e)
    {
        _logger?.LogDebug($"Removing role from cache: {e.RoleId} from guild {e.GuildId}");
        _cache.Remove($"role:{e.RoleId}");

        var guild = _cache.GetGuild(e.GuildId);
        if (guild != null)
        {
            guild.Roles?.RemoveAll(r => r.Id == e.RoleId);
            _cache.CacheGuild(guild);
        }
    }

    // ── Sticker handler ───────────────────────────────────────────────────────

    private void HandleGuildStickersUpdate(GuildStickersUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached stickers for guild: {e.GuildId}");

        var guild = _cache.GetGuild(e.GuildId);
        if (guild != null)
        {
            guild.Stickers = e.Stickers;
            _cache.CacheGuild(guild);
        }
    }

    // ── Thread handlers (threads are cached as channels) ─────────────────────

    private void HandleThreadCreate(ThreadCreateEvent e)
    {
        _logger?.LogDebug($"Caching thread: {e.Name} ({e.Id})");
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

    private void HandleThreadUpdate(ThreadUpdateEvent e)
    {
        _logger?.LogDebug($"Updating cached thread: {e.Id}");
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

    private void HandleThreadDelete(ThreadDeleteEvent e)
    {
        _logger?.LogDebug($"Removing thread from cache: {e.Id}");
        _cache.Remove($"channel:{e.Id}");
    }

    // ── User handler ──────────────────────────────────────────────────────────

    private void HandleUserUpdate(UserUpdateEvent e)
    {
        _logger?.LogDebug($"Updating bot user in cache: {e.Id}");
        var existing = _cache.GetUser(e.Id);
        if (existing != null)
        {
            existing.Username = e.Username;
            existing.Discriminator = e.Discriminator;
            existing.Avatar = e.Avatar;
            _cache.CacheUser(existing);
        }
    }
}
