using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Models;
using PawSharp.Core.Enums;
using PawSharp.Cache.Providers;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Gateway.Events;
using PawSharp.API.Models;
using PawSharp.Interactions.Builders;

namespace PawSharp.Examples;

/// <summary>
/// Comprehensive example showing all major PawSharp features.
/// </summary>
class AdvancedExample
{
    static async Task Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(config => 
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Configure PawSharp options
        services.AddSingleton(new PawSharpOptions
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? "your-token-here",
            Intents = GatewayIntents.Guilds | 
                     GatewayIntents.GuildMessages | 
                     GatewayIntents.MessageContent |
                     GatewayIntents.GuildMembers,
            Shards = 1,
            ShardCount = 1,
            ApiVersion = 10
        });
        
        // Add caching
        services.AddSingleton<IEntityCache, MemoryCacheProvider>();
        
        // Add REST client
        services.AddHttpClient<IDiscordRestClient, DiscordRestClient>();
        
        // Add Discord client
        services.AddSingleton<DiscordClient>();
        services.AddSingleton<CacheManager>();
        
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<DiscordClient>();
        var cache = serviceProvider.GetRequiredService<IEntityCache>();
        var cacheManager = serviceProvider.GetRequiredService<CacheManager>();
        var restClient = serviceProvider.GetRequiredService<IDiscordRestClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<AdvancedExample>>();
        
        // Subscribe cache manager to gateway events
        cacheManager.SubscribeToGateway(client._gatewayClient);
        
        // Register event handlers
        RegisterEventHandlers(client, cache, restClient, logger);
        
        // Connect to Discord
        logger.LogInformation("Starting PawSharp advanced example...");
        await client.ConnectAsync();
        
        logger.LogInformation("Bot is running. Press Ctrl+C to exit.");
        await Task.Delay(-1);
    }
    
    static void RegisterEventHandlers(
        DiscordClient client, 
        IEntityCache cache, 
        IDiscordRestClient restClient,
        ILogger logger)
    {
        // READY event - Bot is connected
        client._gatewayClient.Events.On<ReadyEvent>("READY", async (e) =>
        {
            logger.LogInformation($"Logged in as {e.User.Username}#{e.User.Discriminator}");
            logger.LogInformation($"Connected to {e.Guilds.Count} guilds");
        });
        
        // MESSAGE_CREATE event - Handle new messages
        client._gatewayClient.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async (e) =>
        {
            // Ignore bot messages
            if (e.Author.Bot == true) return;
            
            logger.LogInformation($"Message from {e.Author.Username}: {e.Content}");
            
            // Example: Respond to !ping command
            if (e.Content.ToLower() == "!ping")
            {
                var response = await restClient.CreateMessageAsync(e.ChannelId, new CreateMessageRequest
                {
                    Content = "🏓 Pong!"
                });
                logger.LogInformation("Sent pong response!");
            }
            
            // Example: Respond with button components
            if (e.Content.ToLower() == "!button")
            {
                var button = new ButtonBuilder("click_me", "Click Me!", ButtonStyle.Primary)
                    .SetEmoji("👋")
                    .Build();
                    
                var actionRow = new ActionRowBuilder()
                    .AddComponent(button)
                    .Build();
                
                await restClient.CreateMessageAsync(e.ChannelId, new CreateMessageRequest
                {
                    Content = "Here's a button!",
                    Components = new System.Collections.Generic.List<object> { actionRow }
                });
            }
            
            // Example: Show cached data
            if (e.Content.ToLower() == "!stats")
            {
                var guilds = cache.GetAllGuilds();
                var guildCount = guilds.Count();
                var entityCount = cache.GetEntityCount();
                
                await restClient.CreateMessageAsync(e.ChannelId, new CreateMessageRequest
                {
                    Content = $"📊 **Cache Stats**\\n" +
                             $"Guilds: {guildCount}\\n" +
                             $"Total Entities: {entityCount}"
                });
            }
        });
        
        // GUILD_CREATE event - Bot joins a guild or guild becomes available
        client._gatewayClient.Events.On<GuildCreateEvent>("GUILD_CREATE", async (e) =>
        {
            logger.LogInformation($"Guild available: {e.Name} (ID: {e.Id})");
            logger.LogInformation($"  Members: {e.Members.Count}");
            logger.LogInformation($"  Channels: {e.Channels.Count}");
        });
        
        // INTERACTION_CREATE event - Handle slash commands and components
        client._gatewayClient.Events.On<InteractionCreateEvent>("INTERACTION_CREATE", async (e) =>
        {
            logger.LogInformation($"Interaction received: Type {e.Type}");
            
            // Respond to interactions
            await restClient.CreateInteractionResponseAsync(e.Id, e.Token, new InteractionResponse
            {
                Type = 4, // CHANNEL_MESSAGE_WITH_SOURCE
                Data = new InteractionCallbackData
                {
                    Content = "Interaction received!"
                }
            });
        });
    }
}

// Helper extension for DiscordClient to access gateway
public static class DiscordClientExtensions
{
    public static PawSharp.Gateway.GatewayClient GetGateway(this DiscordClient client)
    {
        var field = typeof(DiscordClient).GetField("_gatewayClient", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (PawSharp.Gateway.GatewayClient)field!.GetValue(client)!;
    }
}
