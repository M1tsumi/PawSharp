#nullable enable
using Microsoft.Extensions.Logging;
using PawSharp.Cache.Interfaces;
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
    public void SubscribeToGateway(GatewayClient gateway)
    {
        // READY event
        gateway.Events.On<ReadyEvent>("READY", HandleReady);
        
        // Guild events
        gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", HandleGuildCreate);
        gateway.Events.On<GuildUpdateEvent>("GUILD_UPDATE", HandleGuildUpdate);
        gateway.Events.On<GuildDeleteEvent>("GUILD_DELETE", HandleGuildDelete);
        
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
}
