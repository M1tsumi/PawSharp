#nullable enable
using System;
using System.Collections.Generic;
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
public class CacheManager : IDisposable
{
    private readonly IEntityCache _cache;
    private readonly ILogger<CacheManager>? _logger;
    private readonly List<IDisposable> _subscriptions = new();
    private bool _disposed;

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
        _subscriptions.Add(gateway.Events.On<ReadyEvent>("READY", HandleReady));
        
        // Guild events
        _subscriptions.Add(gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", HandleGuildCreate));
        _subscriptions.Add(gateway.Events.On<GuildUpdateEvent>("GUILD_UPDATE", HandleGuildUpdate));
        _subscriptions.Add(gateway.Events.On<GuildDeleteEvent>("GUILD_DELETE", HandleGuildDelete));
        _subscriptions.Add(gateway.Events.On<GuildEmojisUpdateEvent>("GUILD_EMOJIS_UPDATE", HandleGuildEmojisUpdate));
        
        // Channel events
        _subscriptions.Add(gateway.Events.On<ChannelCreateEvent>("CHANNEL_CREATE", HandleChannelCreate));
        _subscriptions.Add(gateway.Events.On<ChannelUpdateEvent>("CHANNEL_UPDATE", HandleChannelUpdate));
        _subscriptions.Add(gateway.Events.On<ChannelDeleteEvent>("CHANNEL_DELETE", HandleChannelDelete));
        
        // Message events
        _subscriptions.Add(gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", HandleMessageCreate));
        _subscriptions.Add(gateway.Events.On<MessageUpdateEvent>("MESSAGE_UPDATE", HandleMessageUpdate));
        _subscriptions.Add(gateway.Events.On<MessageDeleteEvent>("MESSAGE_DELETE", HandleMessageDelete));
        
        // Member events
        _subscriptions.Add(gateway.Events.On<GuildMemberAddEvent>("GUILD_MEMBER_ADD", HandleGuildMemberAdd));
        _subscriptions.Add(gateway.Events.On<GuildMemberUpdateEvent>("GUILD_MEMBER_UPDATE", HandleGuildMemberUpdate));
        _subscriptions.Add(gateway.Events.On<GuildMemberRemoveEvent>("GUILD_MEMBER_REMOVE", HandleGuildMemberRemove));

        // Role events
        _subscriptions.Add(gateway.Events.On<GuildRoleCreateEvent>("GUILD_ROLE_CREATE", HandleGuildRoleCreate));
        _subscriptions.Add(gateway.Events.On<GuildRoleUpdateEvent>("GUILD_ROLE_UPDATE", HandleGuildRoleUpdate));
        _subscriptions.Add(gateway.Events.On<GuildRoleDeleteEvent>("GUILD_ROLE_DELETE", HandleGuildRoleDelete));

        // Sticker events
        _subscriptions.Add(gateway.Events.On<GuildStickersUpdateEvent>("GUILD_STICKERS_UPDATE", HandleGuildStickersUpdate));

        // Thread events (treated as channels in the cache)
        _subscriptions.Add(gateway.Events.On<ThreadCreateEvent>("THREAD_CREATE", HandleThreadCreate));
        _subscriptions.Add(gateway.Events.On<ThreadUpdateEvent>("THREAD_UPDATE", HandleThreadUpdate));
        _subscriptions.Add(gateway.Events.On<ThreadDeleteEvent>("THREAD_DELETE", HandleThreadDelete));

        // User events
        _subscriptions.Add(gateway.Events.On<UserUpdateEvent>("USER_UPDATE", HandleUserUpdate));

        // Bulk member chunk — response to opcode 8 (Request Guild Members)
        _subscriptions.Add(gateway.Events.On<GuildMembersChunkEvent>("GUILD_MEMBERS_CHUNK", HandleGuildMembersChunk));
        
        _logger?.LogInformation("Cache manager subscribed to gateway events");
    }

    /// <summary>
    /// Unsubscribe from all gateway events and dispose subscriptions.
    /// </summary>
    public void UnsubscribeFromGateway()
    {
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        _logger?.LogInformation("Cache manager unsubscribed from gateway events");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnsubscribeFromGateway();
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
                guild.Splash = e.Splash;
                guild.DiscoverySplash = e.DiscoverySplash;
                guild.Banner = e.Banner;
                guild.OwnerId = e.OwnerId;
                guild.Region = e.Region;
                guild.AfkChannelId = e.AfkChannelId;
                guild.AfkTimeout = e.AfkTimeout;
                guild.WidgetEnabled = e.WidgetEnabled;
                guild.WidgetChannelId = e.WidgetChannelId;
                guild.VerificationLevel = e.VerificationLevel;
                guild.DefaultMessageNotifications = e.DefaultMessageNotifications;
                guild.ExplicitContentFilter = e.ExplicitContentFilter;
                guild.MfaLevel = e.MfaLevel;
                guild.ApplicationId = e.ApplicationId;
                guild.SystemChannelId = e.SystemChannelId;
                guild.SystemChannelFlags = e.SystemChannelFlags;
                guild.RulesChannelId = e.RulesChannelId;
                guild.MaxPresences = e.MaxPresences;
                guild.MaxMembers = e.MaxMembers;
                guild.VanityUrlCode = e.VanityUrlCode;
                guild.Description = e.Description;
                guild.PremiumTier = e.PremiumTier;
                guild.PremiumSubscriptionCount = e.PremiumSubscriptionCount;
                guild.PreferredLocale = e.PreferredLocale;
                guild.PublicUpdatesChannelId = e.PublicUpdatesChannelId;
                guild.MaxVideoChannelUsers = e.MaxVideoChannelUsers;
                guild.ApproximateMemberCount = e.ApproximateMemberCount ?? e.MemberCount;
                guild.ApproximatePresenceCount = e.ApproximatePresenceCount;
                guild.SafetyAlertsChannelId = e.SafetyAlertsChannelId;
                guild.HomeHeader = e.HomeHeader;
                guild.LatestOnboardingQuestionId = e.LatestOnboardingQuestionId;

                if (e.Features != null)
                    guild.Features = e.Features;
                if (e.Roles != null)
                    guild.Roles = e.Roles;
                if (e.Emojis != null)
                    guild.Emojis = e.Emojis;
                if (e.Stickers != null)
                    guild.Stickers = e.Stickers;

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
            _cache.RemoveChannel(e.Id);
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
                if (e.Components != null)
                    message.Components = e.Components;
                if (e.Flags.HasValue)
                    message.Flags = e.Flags;
                if (e.Attachments != null)
                    message.Attachments = e.Attachments;
                if (e.Mentions != null)
                    message.Mentions = e.Mentions;
                if (e.MentionRoles != null)
                    message.MentionRoles = e.MentionRoles;
                if (e.MentionChannels != null)
                    message.MentionChannels = e.MentionChannels;
                if (e.Poll != null)
                    message.Poll = e.Poll;
                if (e.Reactions != null)
                    message.Reactions = e.Reactions;
                if (e.StickerItems != null)
                    message.StickerItems = e.StickerItems;
                if (e.MessageSnapshots != null)
                    message.MessageSnapshots = e.MessageSnapshots;

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
            _cache.RemoveMessage(e.Id);
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
            if (e.User == null)
            {
                _logger?.LogWarning("Received GUILD_MEMBER_UPDATE with null user");
                return;
            }

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
            _cache.RemoveGuildMember(e.GuildId, e.User.Id);
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
            _cache.RemoveRole(e.GuildId, e.RoleId);

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
            _cache.RemoveChannel(e.Id);
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
