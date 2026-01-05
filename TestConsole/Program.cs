using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Core.Models;
using PawSharp.Cache.Providers;
using PawSharp.API.Clients;
using PawSharp.API.Interfaces;
using PawSharp.Cache.Interfaces;
using PawSharp.Gateway.Events;
using PawSharp.API.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up DI
        var services = new ServiceCollection();
        services.AddLogging(config => config.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<PawSharpOptions>(new PawSharpOptions
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? "your-token-here",
            Intents = PawSharp.Core.Enums.GatewayIntents.Guilds | 
                     PawSharp.Core.Enums.GatewayIntents.GuildMessages | 
                     PawSharp.Core.Enums.GatewayIntents.MessageContent
        });
        services.AddSingleton<IEntityCache, MemoryCacheProvider>();
        services.AddHttpClient<IDiscordRestClient, DiscordRestClient>();
        services.AddSingleton<DiscordClient>();
        services.AddSingleton<CacheManager>();

        var serviceProvider = services.BuildServiceProvider();

        var client = serviceProvider.GetRequiredService<DiscordClient>();
        var cacheManager = serviceProvider.GetRequiredService<CacheManager>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting PawSharp test...");
            
            // Subscribe cache manager to automatically cache entities
            cacheManager.SubscribeToGateway(client.Gateway);
            
            // Register event handlers
            client.Gateway.Events.On<ReadyEvent>("READY", (e) =>
            {
                logger.LogInformation($"✅ Logged in as {e.User.Username}!");
                logger.LogInformation($"Connected to {e.Guilds.Count} guilds");
            });
            
            client.Gateway.Events.On<MessageCreateEvent>("MESSAGE_CREATE", async (e) =>
            {
                if (e.Author.Bot == true) return;
                
                logger.LogInformation($"💬 {e.Author.Username}: {e.Content}");
                
                // Respond to !ping
                if (e.Content.ToLower() == "!ping")
                {
                    await client.Rest.CreateMessageAsync(e.ChannelId, new CreateMessageRequest
                    {
                        Content = "🏓 Pong!"
                    });
                }
            });
            
            client.Gateway.Events.On<GuildCreateEvent>("GUILD_CREATE", (e) =>
            {
                logger.LogInformation($"🏰 Guild received: {e.Id}");
            });
            
            // Fallback raw handler for events that fail to deserialize
            client.Gateway.Events.OnRaw("GUILD_CREATE", (json) =>
            {
                // This will catch events that failed typed deserialization
                if (!json.Contains("\"unavailable\":true"))
                {
                    logger.LogDebug("📦 Raw GUILD_CREATE event received (fallback)");
                }
            });
            
            // Connect to Discord
            await client.ConnectAsync();
            
            logger.LogInformation("✅ Bot is running! Press Ctrl+C to exit.");
            logger.LogInformation("💡 Try sending '!ping' in a channel the bot can see!");

            // Keep running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Fatal error occurred");
        }
    }
}