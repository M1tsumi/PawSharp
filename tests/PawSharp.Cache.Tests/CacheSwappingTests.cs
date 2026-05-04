using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using PawSharp.Cache.Exceptions;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Telemetry;
using PawSharp.Cache.Swapping;
using Xunit;

namespace PawSharp.Cache.Tests;

public class CacheSwappingTests
{
    private class MockCacheProvider : IEntityCache
    {
        public string Name { get; set; } = string.Empty;
        public bool ShouldFail { get; set; }
        public bool IsHealthyValue { get; set; } = true;
        public ICacheTelemetry? Telemetry { get; set; } = new CacheTelemetry();

        public event EventHandler<CacheInvalidationEventArgs>? EntityEvicted;
        public event EventHandler? CacheCleared;

        public void Add(string key, object entity) { }
        public object? Get(string key) => ShouldFail ? throw new CacheProviderUnavailableException(Name) : null;
        public void Remove(string key) { }
        public void Clear() { }
        public bool Exists(string key) => false;

        public void CacheUser(PawSharp.Core.Entities.User user) { }
        public PawSharp.Core.Entities.User? GetUser(ulong userId) => null;
        public void CacheGuild(PawSharp.Core.Entities.Guild guild) { }
        public PawSharp.Core.Entities.Guild? GetGuild(ulong guildId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.Guild> GetAllGuilds() => Enumerable.Empty<PawSharp.Core.Entities.Guild>();
        public void CacheChannel(PawSharp.Core.Entities.Channel channel) { }
        public PawSharp.Core.Entities.Channel? GetChannel(ulong channelId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.Channel> GetGuildChannels(ulong guildId) => Enumerable.Empty<PawSharp.Core.Entities.Channel>();
        public void CacheMessage(PawSharp.Core.Entities.Message message) { }
        public PawSharp.Core.Entities.Message? GetMessage(ulong messageId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.Message> GetChannelMessages(ulong channelId, int limit = 50) => Enumerable.Empty<PawSharp.Core.Entities.Message>();
        public void CacheGuildMember(ulong guildId, PawSharp.Core.Entities.GuildMember member) { }
        public PawSharp.Core.Entities.GuildMember? GetGuildMember(ulong guildId, ulong userId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.GuildMember> GetGuildMembers(ulong guildId) => Enumerable.Empty<PawSharp.Core.Entities.GuildMember>();
        public void CacheRole(ulong guildId, PawSharp.Core.Entities.Role role) { }
        public PawSharp.Core.Entities.Role? GetRole(ulong guildId, ulong roleId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.Role> GetGuildRoles(ulong guildId) => Enumerable.Empty<PawSharp.Core.Entities.Role>();
        public void CacheEmoji(ulong guildId, PawSharp.Core.Entities.Emoji emoji) { }
        public PawSharp.Core.Entities.Emoji? GetEmoji(ulong guildId, ulong emojiId) => null;
        public System.Collections.Generic.IEnumerable<PawSharp.Core.Entities.Emoji> GetGuildEmojis(ulong guildId) => Enumerable.Empty<PawSharp.Core.Entities.Emoji>();
        public void CacheGuildData(PawSharp.Core.Entities.Guild guild) { }
        public void RemoveGuild(ulong guildId) { }
        public void RemoveChannel(ulong channelId) { }
        public void RemoveMessage(ulong messageId) { }
        public void RemoveGuildMember(ulong guildId, ulong userId) { }
        public void RemoveRole(ulong guildId, ulong roleId) { }
        public int GetEntityCount() => 0;
        public long GetMemoryUsage() => 0;
        public CacheStats GetCacheStats() => new CacheStats();
        public bool IsHealthy() => IsHealthyValue;

        public Task<PawSharp.Core.Entities.User?> GetUserAsync(ulong userId) => Task.FromResult<PawSharp.Core.Entities.User?>(null);
        public Task<PawSharp.Core.Entities.Guild?> GetGuildAsync(ulong guildId) => Task.FromResult<PawSharp.Core.Entities.Guild?>(null);
        public Task<PawSharp.Core.Entities.Channel?> GetChannelAsync(ulong channelId) => Task.FromResult<PawSharp.Core.Entities.Channel?>(null);
        public Task<PawSharp.Core.Entities.Message?> GetMessageAsync(ulong messageId) => Task.FromResult<PawSharp.Core.Entities.Message?>(null);
        public Task<PawSharp.Core.Entities.GuildMember?> GetGuildMemberAsync(ulong guildId, ulong userId) => Task.FromResult<PawSharp.Core.Entities.GuildMember?>(null);
        public Task<PawSharp.Core.Entities.Role?> GetRoleAsync(ulong guildId, ulong roleId) => Task.FromResult<PawSharp.Core.Entities.Role?>(null);
        public Task<PawSharp.Core.Entities.Emoji?> GetEmojiAsync(ulong guildId, ulong emojiId) => Task.FromResult<PawSharp.Core.Entities.Emoji?>(null);
        public Task CacheUserAsync(PawSharp.Core.Entities.User user) => Task.CompletedTask;
        public Task CacheGuildAsync(PawSharp.Core.Entities.Guild guild) => Task.CompletedTask;
        public Task CacheChannelAsync(PawSharp.Core.Entities.Channel channel) => Task.CompletedTask;
        public Task CacheMessageAsync(PawSharp.Core.Entities.Message message) => Task.CompletedTask;
        public Task CacheGuildMemberAsync(ulong guildId, PawSharp.Core.Entities.GuildMember member) => Task.CompletedTask;
        public Task CacheRoleAsync(ulong guildId, PawSharp.Core.Entities.Role role) => Task.CompletedTask;
        public Task CacheEmojiAsync(ulong guildId, PawSharp.Core.Entities.Emoji emoji) => Task.CompletedTask;
        public Task CacheGuildDataAsync(PawSharp.Core.Entities.Guild guild) => Task.CompletedTask;
        public Task RemoveGuildAsync(ulong guildId) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
        public Task RemoveChannelAsync(ulong channelId) => Task.CompletedTask;
        public Task RemoveMessageAsync(ulong messageId) => Task.CompletedTask;
        public Task RemoveGuildMemberAsync(ulong guildId, ulong userId) => Task.CompletedTask;
        public Task RemoveRoleAsync(ulong guildId, ulong roleId) => Task.CompletedTask;
    }

    [Fact]
    public void CacheSwapper_RegistersProviderSuccessfully()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider, priority: 0);

        var providers = swapper.GetProviders();
        providers.Should().ContainSingle(p => p.Name == "test");
    }

    [Fact]
    public void CacheSwapper_RejectsDuplicateProviderName()
    {
        var swapper = new CacheSwapper();
        var provider1 = new MockCacheProvider { Name = "test" };
        var provider2 = new MockCacheProvider { Name = "test2" };

        swapper.RegisterProvider("test", provider1);

        Action act = () => swapper.RegisterProvider("test", provider2);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void CacheSwapper_RejectsEmptyProviderName()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        Action act = () => swapper.RegisterProvider("", provider);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void CacheSwapper_RejectsNullProvider()
    {
        var swapper = new CacheSwapper();

        Action act = () => swapper.RegisterProvider("test", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CacheSwapper_SetsFirstProviderAsActive()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider, priority: 0);

        var activeProvider = swapper.GetActiveProvider();
        activeProvider.Should().Be(provider);
    }

    [Fact]
    public void CacheSwapper_SwitchesActiveProvider()
    {
        var swapper = new CacheSwapper();
        var provider1 = new MockCacheProvider { Name = "provider1" };
        var provider2 = new MockCacheProvider { Name = "provider2" };

        swapper.RegisterProvider("provider1", provider1, priority: 0);
        swapper.RegisterProvider("provider2", provider2, priority: 1);

        swapper.SetActiveProvider("provider2");

        var activeProvider = swapper.GetActiveProvider();
        activeProvider.Should().Be(provider2);
    }

    [Fact]
    public void CacheSwapper_ThrowsWhenSwitchingToUnregisteredProvider()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider);

        Action act = () => swapper.SetActiveProvider("nonexistent");

        act.Should().Throw<CacheProviderNotRegisteredException>()
            .WithMessage("*not registered*");
    }

    [Fact]
    public void CacheSwapper_ThrowsWhenSwitchingToUnhealthyProvider()
    {
        var swapper = new CacheSwapper();
        var provider1 = new MockCacheProvider { Name = "provider1", IsHealthyValue = true };
        var provider2 = new MockCacheProvider { Name = "provider2", IsHealthyValue = false };

        swapper.RegisterProvider("provider1", provider1);
        swapper.RegisterProvider("provider2", provider2);

        Action act = () => swapper.SetActiveProvider("provider2");

        act.Should().Throw<CacheProviderUnavailableException>()
            .WithMessage("*not available or unhealthy*");
    }

    [Fact]
    public void CacheSwapper_UnregistersProvider()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider);
        swapper.UnregisterProvider("test");

        var providers = swapper.GetProviders();
        providers.Should().NotContain(p => p.Name == "test");
    }

    [Fact]
    public void CacheSwapper_AutoSwitchesWhenUnregisteringActiveProvider()
    {
        var swapper = new CacheSwapper();
        var provider1 = new MockCacheProvider { Name = "provider1" };
        var provider2 = new MockCacheProvider { Name = "provider2" };

        swapper.RegisterProvider("provider1", provider1);
        swapper.RegisterProvider("provider2", provider2);

        swapper.UnregisterProvider("provider1");

        var activeProvider = swapper.GetActiveProvider();
        activeProvider.Should().Be(provider2);
    }

    [Fact]
    public async Task CacheSwapper_PerformsHealthChecks()
    {
        var swapper = new CacheSwapper(new CacheSwapperOptions { HealthCheckInterval = TimeSpan.FromMilliseconds(100) });
        var healthyProvider = new MockCacheProvider { Name = "healthy", IsHealthyValue = true };
        var unhealthyProvider = new MockCacheProvider { Name = "unhealthy", IsHealthyValue = false };

        swapper.RegisterProvider("healthy", healthyProvider);
        swapper.RegisterProvider("unhealthy", unhealthyProvider);

        await swapper.PerformHealthChecksAsync();

        var providers = swapper.GetProviders();
        providers.First(p => p.Name == "healthy").IsHealthy.Should().BeTrue();
        providers.First(p => p.Name == "unhealthy").IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void CacheSwapper_StartsAndStopsHealthChecks()
    {
        var swapper = new CacheSwapper(new CacheSwapperOptions { HealthCheckInterval = TimeSpan.FromMinutes(1) });
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider);

        swapper.StartHealthChecks();
        swapper.StopHealthChecks();

        // Should not throw
    }

    [Fact]
    public void CacheSwapper_DelegatesOperationsToActiveProvider()
    {
        var swapper = new CacheSwapper();
        var provider = new MockCacheProvider { Name = "test" };

        swapper.RegisterProvider("test", provider);

        // Should not throw
        swapper.Add("key", new object());
        swapper.Get("key");
        swapper.Remove("key");
        swapper.Clear();
        swapper.Exists("key");
    }
}
