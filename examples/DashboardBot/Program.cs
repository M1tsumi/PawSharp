using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawSharp.Client;
using PawSharp.Commands;
using PawSharp.Interactions;
using PawSharp.Core.Models;

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
        services.AddPawSharpCommands();
        services.AddPawSharpInteractions();

        var serviceProvider = services.BuildServiceProvider();

        // Get the Discord client
        var client = serviceProvider.GetRequiredService<DiscordClient>();

        // Register slash commands
        var interactionService = serviceProvider.GetRequiredService<InteractionService>();
        await interactionService.RegisterCommandsAsync();

        // Connect and start
        await client.ConnectAsync();

        // Keep the bot running
        await Task.Delay(-1);
    }
}

public class DashboardCommands : InteractionModule
{
    [SlashCommand("serverinfo", "Display server information")]
    public async Task ServerInfoAsync()
    {
        var guild = Context.Guild;
        if (guild == null)
        {
            await RespondAsync("This command can only be used in a server!");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"{guild.Name} Server Info")
            .AddField("Owner", $"<@{guild.OwnerId}>", true)
            .AddField("Members", guild.MemberCount.ToString(), true)
            .AddField("Channels", guild.Channels?.Count.ToString() ?? "0", true)
            .AddField("Roles", guild.Roles?.Count.ToString() ?? "0", true)
            .AddField("Created", guild.CreatedAt.ToString("R"), false)
            .WithColor(Color.Blue)
            .WithThumbnail(guild.IconUrl);

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("userinfo", "Display user information")]
    public async Task UserInfoAsync(
        [Description("The user to get info for (defaults to yourself)")] IUser? user = null)
    {
        user ??= Context.User;

        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username}#{user.Discriminator}")
            .AddField("ID", user.Id.ToString(), true)
            .AddField("Bot", user.IsBot ? "Yes" : "No", true)
            .AddField("Joined Discord", user.CreatedAt.ToString("R"), false)
            .WithColor(user.IsBot ? Color.Red : Color.Green)
            .WithThumbnail(user.AvatarUrl);

        if (Context.Guild != null)
        {
            var member = await Context.Guild.GetMemberAsync(user.Id);
            if (member != null)
            {
                embed.AddField("Joined Server", member.JoinedAt?.ToString("R") ?? "Unknown", false);
                if (member.Roles?.Count > 0)
                {
                    embed.AddField("Roles", string.Join(", ", member.Roles.Select(r => r.Name)), false);
                }
            }
        }

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("ping", "Check bot latency")]
    public async Task PingAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await RespondAsync("Pong!", ephemeral: true);
        stopwatch.Stop();

        await FollowupAsync($"Latency: {stopwatch.ElapsedMilliseconds}ms", ephemeral: true);
    }

    [SlashCommand("stats", "Display bot statistics")]
    public async Task StatsAsync()
    {
        var client = Context.Client;

        var embed = new EmbedBuilder()
            .WithTitle("Bot Statistics")
            .AddField("Servers", client.Guilds.Count.ToString(), true)
            .AddField("Users", client.Guilds.Sum(g => g.MemberCount).ToString(), true)
            .AddField("Uptime", "Running", true)
            .AddField("Version", "PawSharp 1.1.0-alpha.1", false)
            .WithColor(Color.Purple);

        await RespondAsync(embed: embed.Build());
    }
}