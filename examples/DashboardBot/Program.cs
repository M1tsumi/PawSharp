using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Commands;
using PawSharp.Interactions;
using PawSharp.Core.Builders;
using PawSharp.Core.Entities;
using PawSharp.Core.Models;
using PawSharp.API.Models;

namespace DashboardBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();

        // Configure logging
        services.AddLogging(config => config.AddConsole());

        // Configure PawSharp
        var options = new PawSharpOptions
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
                ?? throw new InvalidOperationException("DISCORD_TOKEN environment variable is required"),
            Intents = PawSharp.Core.Enums.GatewayIntents.AllNonPrivileged
        };

        services.SetupPawSharp(options);
        services.AddCommands();
        services.AddInteractionHandler();

        var serviceProvider = services.BuildServiceProvider();

        // Get the Discord client and interaction handler
        var client = serviceProvider.GetRequiredService<IDiscordClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var interactionHandler = serviceProvider.GetRequiredService<InteractionHandler>();

        // Register slash command: ping
        interactionHandler.RegisterCommand("ping", async interaction =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await interactionHandler.RespondAsync(interaction.Id, interaction.Token, new InteractionResponse
            {
                Type = 4,
                Data = new InteractionCallbackData { Content = "Pong!" }
            });
            stopwatch.Stop();
            await interactionHandler.CreateFollowupAsync(
                interaction.ApplicationId.ToString(),
                interaction.Token,
                new CreateMessageRequest { Content = $"Latency: {stopwatch.ElapsedMilliseconds}ms" });
        });

        // Register slash command: serverinfo
        interactionHandler.RegisterCommand("serverinfo", async interaction =>
        {
            var guildId = interaction.GuildId;
            if (guildId == null)
            {
                await interactionHandler.RespondEphemeralAsync(interaction.Id, interaction.Token,
                    "This command can only be used in a server!");
                return;
            }

            var guild = await client.Rest.GetGuildAsync(guildId.Value, withCounts: true);
            if (guild == null)
            {
                await interactionHandler.RespondEphemeralAsync(interaction.Id, interaction.Token,
                    "Could not fetch server information.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"{guild.Name} Server Info")
                .AddField("Owner", $"<@{guild.OwnerId}>", true)
                .AddField("Created", guild.CreatedAt.ToString("R"), false)
                .WithColor(0x3498DB)
                .Build();

            await interactionHandler.RespondWithEmbedsAsync(interaction.Id, interaction.Token,
                null, new List<Embed> { embed });
        });

        // Connect and start
        logger.LogInformation("Starting Dashboard Bot...");
        await client.ConnectAsync();
        logger.LogInformation("Bot connected successfully!");

        // Keep the bot running
        await Task.Delay(-1);
    }
}