#nullable enable
using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PawSharp.Cache.Interfaces;
using PawSharp.Cache.Providers;
using PawSharp.Client;
using PawSharp.Client.Extensions;
using PawSharp.Core.Models;
using Xunit;

namespace PawSharp.Client.Tests;

public class PawSharpServiceCollectionExtensionsTests
{
    [Fact]
    public void SetupPawSharp_ResolvesDiscordClientAndDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.SetupPawSharp(new PawSharpOptions { Token = "Bot abc.def.ghi" });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<DiscordClient>();
        var cache = provider.GetRequiredService<IEntityCache>();

        client.Should().NotBeNull();
        cache.Should().NotBeNull();
    }

    [Fact]
    public void AddPawSharpWithMemoryCache_ResolvesDiscordClientAndDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPawSharpWithMemoryCache(new PawSharpOptions { Token = "Bot abc.def.ghi" });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<DiscordClient>();
        var cache = provider.GetRequiredService<IEntityCache>();

        client.Should().NotBeNull();
        cache.Should().NotBeNull();
    }

    [Fact]
    public void AddPawSharp_WithoutCacheFactory_RegistersDefaultMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPawSharp(new PawSharpOptions { Token = "Bot abc.def.ghi" });

        using var provider = services.BuildServiceProvider();

        var cache = provider.GetRequiredService<IEntityCache>();
        var client = provider.GetRequiredService<DiscordClient>();

        cache.Should().BeOfType<MemoryCacheProvider>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddPawSharp_WithCustomCacheFactory_RegistersCustomCacheSingleton()
    {
        var customCache = new Mock<IEntityCache>();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddPawSharp(new PawSharpOptions { Token = "Bot abc.def.ghi" }, _ => customCache.Object);

        using var provider = services.BuildServiceProvider();

        var resolvedCache = provider.GetRequiredService<IEntityCache>();
        var resolvedClient = provider.GetRequiredService<DiscordClient>();

        resolvedCache.Should().BeSameAs(customCache.Object);
        resolvedClient.Should().NotBeNull();
    }

    [Fact]
    public void SetupPawSharp_ThrowsOnNullOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        Action act = () => services.SetupPawSharp(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPawSharpWithMemoryCache_ResolvesAllServices()
    {
        var options = new PawSharpOptions { Token = "Bot abc.def.ghi" };
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(options);
        services.AddPawSharpWithMemoryCache(options);

        using var provider = services.BuildServiceProvider();

        var resolvedOptions = provider.GetRequiredService<PawSharpOptions>();
        var resolvedClient = provider.GetRequiredService<DiscordClient>();

        resolvedOptions.Should().BeSameAs(options);
        resolvedClient.Should().NotBeNull();
    }
}
